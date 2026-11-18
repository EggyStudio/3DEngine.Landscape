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
    public Heightfield Build(CancellationToken cancel = default)
    {
        var field = new Heightfield(Columns, Rows, Spacing);
        foreach (var step in Steps)
        {
            cancel.ThrowIfCancellationRequested();
            step.Apply(field);
        }

        return field;
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
