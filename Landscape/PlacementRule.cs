namespace Landscape;

/// <summary>
/// Where a kind of plant may stand, a range of height and a range of steepness, each with a width
/// beyond it over which the plant thins out, as a <see cref="SplatRule"/> gives a material's.
/// </summary>
/// <remarks>
/// A place's chance is its height's share times its steepness's share times the density map's
/// share there. A share is one inside its range and falls by Hermite's smooth step to nothing over
/// its blend beyond either end.
/// </remarks>
public sealed record PlacementRule
{
    private readonly SplatRule _ranges;

    /// <summary>A plant's rule.</summary>
    /// <param name="lowest">The lowest height in meters it grows at whole.</param>
    /// <param name="highest">
    /// The highest height in meters it grows at whole, at or above the lowest.
    /// </param>
    /// <param name="heightBlend">How many meters beyond either height it thins out over.</param>
    /// <param name="leastSteep">The least steepness it grows on whole, a rise over a run.</param>
    /// <param name="mostSteep">The most steepness it grows on whole, at or above the least.</param>
    /// <param name="steepBlend">How far in steepness beyond either end it thins out over.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// A range runs downward or is a NaN, or a blend is below nothing or not finite.
    /// </exception>
    public PlacementRule(
        float lowest = float.NegativeInfinity,
        float highest = float.PositiveInfinity,
        float heightBlend = 0f,
        float leastSteep = 0f,
        float mostSteep = float.PositiveInfinity,
        float steepBlend = 0f)
    {
        _ranges = new SplatRule("plant", lowest, highest, heightBlend, leastSteep, mostSteep, steepBlend);
    }

    /// <summary>The lowest height in meters it grows at whole.</summary>
    public float Lowest => _ranges.Lowest;

    /// <summary>The highest height in meters it grows at whole.</summary>
    public float Highest => _ranges.Highest;

    /// <summary>How many meters beyond either height it thins out over.</summary>
    public float HeightBlend => _ranges.HeightBlend;

    /// <summary>The least steepness it grows on whole.</summary>
    public float LeastSteep => _ranges.LeastSteep;

    /// <summary>The most steepness it grows on whole.</summary>
    public float MostSteep => _ranges.MostSteep;

    /// <summary>How far in steepness beyond either end it thins out over.</summary>
    public float SteepBlend => _ranges.SteepBlend;

    /// <summary>The chance from the ranges alone, before the density map's share.</summary>
    /// <param name="height">The ground's height in meters.</param>
    /// <param name="steepness">The ground's steepness, a rise over a run.</param>
    public float Chance(float height, float steepness) => _ranges.Weight(height, steepness);

    /// <summary>Whether two rules hold the same ranges and blends.</summary>
    /// <param name="other">The other rule.</param>
    public bool Equals(PlacementRule? other) => other is not null && _ranges.Equals(other._ranges);

    /// <summary>A hash of the ranges and blends, equal for equal rules.</summary>
    public override int GetHashCode() => _ranges.GetHashCode();
}
