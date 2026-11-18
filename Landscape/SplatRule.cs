namespace Landscape;

/// <summary>
/// Where a material of a splat map lies, a range of height and a range of steepness, each with a
/// width beyond it over which the material gives way.
/// </summary>
/// <remarks>
/// A material's weight at a point is its height's share times its steepness's share. A share is one
/// inside its range and falls by Hermite's smooth step to nothing over its blend beyond either end,
/// a blend of nothing ending it at once. An end of infinity leaves the range open on that side.
/// </remarks>
public sealed record SplatRule
{
    /// <summary>A material's rule.</summary>
    /// <param name="material">The material's name, which an engine draws the material by.</param>
    /// <param name="lowest">The lowest height in meters it covers whole.</param>
    /// <param name="highest">
    /// The highest height in meters it covers whole, at or above the lowest.
    /// </param>
    /// <param name="heightBlend">How many meters beyond either height it gives way over.</param>
    /// <param name="leastSteep">
    /// The least steepness it covers whole, as <see cref="Heightfield.Steepness(int, int)"/> gives
    /// a rise over a run.
    /// </param>
    /// <param name="mostSteep">The most steepness it covers whole, at or above the least.</param>
    /// <param name="steepBlend">How far in steepness beyond either end it gives way over.</param>
    /// <exception cref="ArgumentException">The material has no name.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// A range runs downward or is a NaN, or a blend is below nothing or not finite.
    /// </exception>
    public SplatRule(
        string material,
        float lowest = float.NegativeInfinity,
        float highest = float.PositiveInfinity,
        float heightBlend = 0f,
        float leastSteep = 0f,
        float mostSteep = float.PositiveInfinity,
        float steepBlend = 0f)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(material);
        if (!(lowest <= highest)) throw new ArgumentOutOfRangeException(nameof(highest), highest, $"A rule's highest height is at or above its lowest, {lowest}.");
        if (!(leastSteep <= mostSteep)) throw new ArgumentOutOfRangeException(nameof(mostSteep), mostSteep, $"A rule's most steepness is at or above its least, {leastSteep}.");
        if (!(heightBlend >= 0f) || !float.IsFinite(heightBlend)) throw new ArgumentOutOfRangeException(nameof(heightBlend), heightBlend, "A blend is a finite number at or above nothing.");
        if (!(steepBlend >= 0f) || !float.IsFinite(steepBlend)) throw new ArgumentOutOfRangeException(nameof(steepBlend), steepBlend, "A blend is a finite number at or above nothing.");
        (Material, Lowest, Highest, HeightBlend, LeastSteep, MostSteep, SteepBlend) = (material, lowest, highest, heightBlend, leastSteep, mostSteep, steepBlend);
    }

    /// <summary>The material's name, which an engine draws the material by.</summary>
    public string Material { get; }

    /// <summary>The lowest height in meters it covers whole.</summary>
    public float Lowest { get; }

    /// <summary>The highest height in meters it covers whole.</summary>
    public float Highest { get; }

    /// <summary>How many meters beyond either height it gives way over.</summary>
    public float HeightBlend { get; }

    /// <summary>The least steepness it covers whole, a rise over a run.</summary>
    public float LeastSteep { get; }

    /// <summary>The most steepness it covers whole, a rise over a run.</summary>
    public float MostSteep { get; }

    /// <summary>How far in steepness beyond either end it gives way over.</summary>
    public float SteepBlend { get; }

    /// <summary>
    /// The material's weight before the weights are brought to one, from nothing to one.
    /// </summary>
    /// <param name="height">The ground's height in meters.</param>
    /// <param name="steepness">The ground's steepness, a rise over a run.</param>
    public float Weight(float height, float steepness) =>
        Share(height, Lowest, Highest, HeightBlend) * Share(steepness, LeastSteep, MostSteep, SteepBlend);

    private static float Share(float value, float low, float high, float blend)
    {
        if (value >= low && value <= high) return 1f;
        if (blend == 0f) return 0f;
        var beyond = value < low ? low - value : value - high;
        if (beyond >= blend) return 0f;
        var t = 1f - beyond / blend;
        return t * t * (3f - 2f * t);
    }
}
