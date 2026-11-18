using System.Text.Json;

namespace Landscape;

/// <summary>
/// A field's size and the ordered steps that make its heights, which a level keeps in place of the
/// heights and builds again after each change.
/// </summary>
/// <remarks>
/// A build starts from a flat field at height zero and applies the steps in their order, so the
/// same recipe builds the same heights each time. It runs on whichever thread calls it, which an
/// engine chooses, and stops at the step after a cancellation is asked for.
/// </remarks>
public sealed record Recipe
{
    /// <summary>A recipe of a field's size and its steps.</summary>
    /// <param name="columns">How many points a row has along X, two or more.</param>
    /// <param name="rows">How many rows of points the field has along Z, two or more.</param>
    /// <param name="spacing">The distance in meters between two points beside each other.</param>
    /// <param name="steps">The steps in the order they are applied.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// A count is below two, or the spacing is not a finite number above zero.
    /// </exception>
    /// <exception cref="ArgumentException">A step is missing.</exception>
    public Recipe(int columns, int rows, float spacing, IReadOnlyList<LandscapeStep> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);
        Heightfield.Check(columns, rows, spacing);
        if (steps.Any(step => step is null)) throw new ArgumentException("A recipe's steps are all there.", nameof(steps));
        Columns = columns;
        Rows = rows;
        Spacing = spacing;
        Steps = [.. steps];
    }

    /// <summary>How many points a row of the field has along X.</summary>
    public int Columns { get; }

    /// <summary>How many rows of points the field has along Z.</summary>
    public int Rows { get; }

    /// <summary>The distance in meters between two points beside each other.</summary>
    public float Spacing { get; }

    /// <summary>The steps in the order they are applied.</summary>
    public IReadOnlyList<LandscapeStep> Steps { get; }

    /// <summary>Builds the field from flat ground by the steps in their order.</summary>
    /// <param name="cancel">Asks the build to stop, which it does before the next step.</param>
    /// <exception cref="OperationCanceledException">The build was asked to stop.</exception>
    /// <exception cref="InvalidOperationException">
    /// A step stamps a picture, which a build with no pictures has none of.
    /// </exception>
    public Heightfield Build(CancellationToken cancel = default) => Build(null, cancel);

    /// <summary>
    /// Builds the field from flat ground, its picture stamps read through the engine's pictures.
    /// </summary>
    /// <param name="pictures">
    /// The engine's answer for the pictures the recipe's stamps name.
    /// </param>
    /// <param name="cancel">Asks the build to stop, which it does before the next step.</param>
    /// <exception cref="OperationCanceledException">The build was asked to stop.</exception>
    /// <exception cref="InvalidOperationException">
    /// A step stamps a picture the engine holds none of.
    /// </exception>
    public Heightfield Build(ILandscapePictures? pictures, CancellationToken cancel = default) =>
        Build(pictures, BuildOptions.Default, cancel);

    /// <summary>
    /// Builds the field from flat ground across the threads the options allow, reporting each
    /// step as it ends.
    /// </summary>
    /// <param name="pictures">
    /// The engine's answer for the pictures the recipe's stamps name.
    /// </param>
    /// <param name="options">
    /// The threads the build may use and where it reports its progress.
    /// </param>
    /// <param name="cancel">Asks the build to stop, which it does before the next step.</param>
    /// <exception cref="OperationCanceledException">The build was asked to stop.</exception>
    /// <exception cref="InvalidOperationException">
    /// A step stamps a picture the engine holds none of.
    /// </exception>
    public Heightfield Build(ILandscapePictures? pictures, BuildOptions options, CancellationToken cancel = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        var (field, context) = (new Heightfield(Columns, Rows, Spacing), new BuildContext(pictures, options));
        for (var step = 0; step < Steps.Count; step++)
        {
            cancel.ThrowIfCancellationRequested();
            Steps[step].Apply(field, context);
            options.Progress?.Report(new BuildProgress(step + 1, Steps.Count));
        }

        return field;
    }

    /// <summary>
    /// The format a recipe is written in, which <see cref="Read"/> takes and no other.
    /// </summary>
    public const int Format = 1;

    /// <summary>
    /// Writes the recipe as JSON, its format, its size and its steps each by its kind.
    /// </summary>
    /// <param name="stream">Where the JSON goes, left open.</param>
    /// <remarks>
    /// Each number is written as the shortest text that reads back to the same float, so a recipe
    /// read back builds the same heights to the bit as the one written.
    /// </remarks>
    public void Write(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        JsonSerializer.Serialize(stream, new RecipeFile(Format, Columns, Rows, Spacing, Steps), RecipeJson.Default.RecipeFile);
    }

    /// <summary>Reads a recipe <see cref="Write"/> wrote.</summary>
    /// <param name="stream">Where the JSON comes from.</param>
    /// <exception cref="JsonException">
    /// The JSON is not a recipe of this format, names a step the module has not, holds a property
    /// no step has, or gives a number a step refuses, which the exception's inner one names.
    /// </exception>
    public static Recipe Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        RecipeFile file;
        try
        {
            file = JsonSerializer.Deserialize(stream, RecipeJson.Default.RecipeFile)
                ?? throw new JsonException("The JSON holds null where a recipe belongs.");
        }
        catch (ArgumentException refused)
        {
            throw new JsonException($"The recipe gives a number a step refuses. {refused.Message}", refused);
        }

        if (file.Format != Format) throw new JsonException($"The recipe is of format {file.Format}, and this module reads format {Format}.");
        try
        {
            return new Recipe(file.Columns, file.Rows, file.Spacing, file.Steps ?? []);
        }
        catch (ArgumentException refused)
        {
            throw new JsonException($"The recipe's size or steps are refused. {refused.Message}", refused);
        }
    }

    /// <summary>
    /// Whether two recipes hold the same size and the same steps in the same order.
    /// </summary>
    /// <param name="other">The other recipe.</param>
    public bool Equals(Recipe? other) =>
        other is not null && Columns == other.Columns && Rows == other.Rows && Spacing.Equals(other.Spacing)
        && Steps.SequenceEqual(other.Steps);

    /// <summary>A hash of the size and the steps, equal for equal recipes.</summary>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Columns);
        hash.Add(Rows);
        hash.Add(Spacing);
        foreach (var step in Steps) hash.Add(step);
        return hash.ToHashCode();
    }
}
