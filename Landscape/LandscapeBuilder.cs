namespace Landscape;

/// <summary>
/// Builds a recipe and builds it again after each change, keeping the field each step left so a
/// change applies again the step it reaches and the steps after it alone.
/// </summary>
/// <remarks>
/// <para>
/// A rebuild finds the first step of the new recipe that differs from the old, by the steps' own
/// equality, and starts from the field the step before it left. The result is the same to the bit
/// as building the new recipe from flat ground, since every step reads only the field it is given.
/// A recipe of another size builds whole.
/// </para>
/// <para>
/// A field is kept for every step, so the builder holds as many fields as the recipe has steps, a
/// recipe of 20 steps over 1,025 by 1,025 points 84 MB at four bytes a height. It is not safe for
/// two threads at once; an engine rebuilds on one worker and reads <see cref="Field"/> once the
/// rebuild returns.
/// </para>
/// </remarks>
public sealed class LandscapeBuilder
{
    // The field each step left, the last of them the builder's own and Field a copy of it, so an
    // engine that writes into Field changes nothing a rebuild starts from.
    private readonly List<Heightfield> _after = [];
    private Heightfield _last;
    private readonly BuildContext _context;

    /// <summary>A builder that builds a recipe from flat ground.</summary>
    /// <param name="recipe">The recipe.</param>
    /// <param name="cancel">Asks the build to stop, which it does before the next step.</param>
    /// <exception cref="OperationCanceledException">The build was asked to stop.</exception>
    public LandscapeBuilder(Recipe recipe, CancellationToken cancel = default) : this(recipe, null, cancel)
    {
    }

    /// <summary>
    /// A builder that reads the pictures its recipes' stamps name through the engine's answer.
    /// </summary>
    /// <param name="recipe">The recipe.</param>
    /// <param name="pictures">
    /// The engine's answer for the pictures the stamps name, kept for every rebuild.
    /// </param>
    /// <param name="cancel">Asks the build to stop, which it does before the next step.</param>
    /// <exception cref="OperationCanceledException">The build was asked to stop.</exception>
    public LandscapeBuilder(Recipe recipe, ILandscapePictures? pictures, CancellationToken cancel = default)
        : this(recipe, pictures, BuildOptions.Default, cancel)
    {
    }

    /// <summary>
    /// A builder that builds across the threads the options allow and reports each step it applies.
    /// </summary>
    /// <param name="recipe">The recipe.</param>
    /// <param name="pictures">
    /// The engine's answer for the pictures the stamps name, kept for every rebuild.
    /// </param>
    /// <param name="options">
    /// The threads a build may use and where it reports, kept for every rebuild.
    /// </param>
    /// <param name="cancel">Asks the build to stop, which it does before the next step.</param>
    /// <exception cref="OperationCanceledException">The build was asked to stop.</exception>
    /// <remarks>
    /// A rebuild reports the steps it applies, from the first that changed, each as the count of
    /// the recipe's steps that stand applied, so its last report is the recipe's whole.
    /// </remarks>
    public LandscapeBuilder(Recipe recipe, ILandscapePictures? pictures, BuildOptions options, CancellationToken cancel = default)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        ArgumentNullException.ThrowIfNull(options);
        _context = new BuildContext(pictures, options);
        Recipe = new Recipe(recipe.Columns, recipe.Rows, recipe.Spacing, []);
        _last = new Heightfield(recipe.Columns, recipe.Rows, recipe.Spacing);
        Field = _last.Copy();
        Rebuild(recipe, cancel);
    }

    /// <summary>The recipe the field was last built from.</summary>
    public Recipe Recipe { get; private set; }

    /// <summary>
    /// The field the recipe builds, a copy of the builder's own, which a rebuild replaces and never
    /// changes.
    /// </summary>
    public Heightfield Field { get; private set; }

    /// <summary>Builds a changed recipe from the first step that changed.</summary>
    /// <param name="recipe">The recipe as it stands after the change.</param>
    /// <param name="cancel">
    /// Asks the rebuild to stop, which it does before the next step, the builder keeping the field
    /// and the recipe it had.
    /// </param>
    /// <returns>Which steps were applied and the points whose heights changed.</returns>
    /// <exception cref="OperationCanceledException">The rebuild was asked to stop.</exception>
    public LandscapeChange Rebuild(Recipe recipe, CancellationToken cancel = default)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        var sameSize = recipe.Columns == Recipe.Columns && recipe.Rows == Recipe.Rows && recipe.Spacing.Equals(Recipe.Spacing);
        var first = 0;
        if (sameSize)
        {
            while (first < recipe.Steps.Count && first < Recipe.Steps.Count && recipe.Steps[first].Equals(Recipe.Steps[first])) first++;
        }

        var built = new List<Heightfield>(recipe.Steps.Count);
        if (sameSize) built.AddRange(_after.Take(first));
        var field = first == 0 ? new Heightfield(recipe.Columns, recipe.Rows, recipe.Spacing) : built[first - 1].Copy();
        for (var step = first; step < recipe.Steps.Count; step++)
        {
            cancel.ThrowIfCancellationRequested();
            recipe.Steps[step].Apply(field, _context);
            _context.Options.Progress?.Report(new BuildProgress(step + 1, recipe.Steps.Count));
            built.Add(field);
            if (step + 1 < recipe.Steps.Count) field = field.Copy();
        }

        var changed = sameSize ? Changed(_last, field) : new LandscapeArea(0, 0, field.Columns - 1, field.Rows - 1);
        _after.Clear();
        _after.AddRange(built);
        (Recipe, _last, Field) = (recipe, field, field.Copy());
        return new LandscapeChange(first, recipe.Steps.Count - first, changed);
    }

    // The rectangle about every point whose height differs between two fields of one size, by its
    // bits, or nothing where none does.
    private static LandscapeArea? Changed(Heightfield before, Heightfield after)
    {
        var (first, front, last, back) = (int.MaxValue, int.MaxValue, -1, -1);
        for (var row = 0; row < after.Rows; row++)
        {
            for (var column = 0; column < after.Columns; column++)
            {
                if (BitConverter.SingleToInt32Bits(before[column, row]) == BitConverter.SingleToInt32Bits(after[column, row])) continue;
                (first, last) = (Math.Min(first, column), Math.Max(last, column));
                (front, back) = (Math.Min(front, row), Math.Max(back, row));
            }
        }

        // A cell that opens or closes as a hole changes what its four corners draw, so they join
        // the rectangle though no height of theirs changed.
        if (before.Holes is not null || after.Holes is not null)
        {
            for (var row = 0; row < after.Rows - 1; row++)
            {
                for (var column = 0; column < after.Columns - 1; column++)
                {
                    if (before.IsHole(column, row) == after.IsHole(column, row)) continue;
                    (first, last) = (Math.Min(first, column), Math.Max(last, column + 1));
                    (front, back) = (Math.Min(front, row), Math.Max(back, row + 1));
                }
            }
        }

        return last < 0 ? null : new LandscapeArea(first, front, last, back);
    }
}

/// <summary>What a rebuild did, which steps it applied and which points it changed.</summary>
public readonly record struct LandscapeChange
{
    /// <summary>
    /// A rebuild's account, its first step applied, how many it applied and the points it changed.
    /// </summary>
    /// <param name="firstStep">
    /// The first step applied, the first that differed from the recipe before.
    /// </param>
    /// <param name="stepsApplied">
    /// How many steps were applied, the first and those after it.
    /// </param>
    /// <param name="changed">
    /// The rectangle about the points whose heights changed, or nothing where none did.
    /// </param>
    public LandscapeChange(int firstStep, int stepsApplied, LandscapeArea? changed)
    {
        (FirstStep, StepsApplied, Changed) = (firstStep, stepsApplied, changed);
    }

    /// <summary>The first step applied, the first that differed from the recipe before.</summary>
    public int FirstStep { get; init; }

    /// <summary>How many steps were applied, the first and those after it.</summary>
    public int StepsApplied { get; init; }

    /// <summary>
    /// The rectangle about the points whose heights changed, or nothing where none did.
    /// </summary>
    public LandscapeArea? Changed { get; init; }
}
