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
    private readonly ILandscapePictures? _pictures;

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
    {
        ArgumentNullException.ThrowIfNull(recipe);
        _pictures = pictures;
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
            recipe.Steps[step].Apply(field, _pictures);
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

        return last < 0 ? null : new LandscapeArea(first, front, last, back);
    }
}

/// <summary>What a rebuild did, which steps it applied and which points it changed.</summary>
/// <param name="FirstStep">
/// The first step applied, the first that differed from the recipe before.
/// </param>
/// <param name="StepsApplied">How many steps were applied, the first and those after it.</param>
/// <param name="Changed">
/// The rectangle about the points whose heights changed, or nothing where none did.
/// </param>
public readonly record struct LandscapeChange(int FirstStep, int StepsApplied, LandscapeArea? Changed);
