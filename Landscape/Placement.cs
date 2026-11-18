using System.Numerics;

namespace Landscape;

/// <summary>
/// Which of the places a planting gives stand on a field, kept by a rule's ranges of height and
/// steepness and a density map, and lifted onto the ground.
/// </summary>
/// <remarks>
/// <para>
/// The places come as a planting gives them, a place across the ground and a seed each, as
/// Vegetation's forest gives its trees, with the height the planting stood them at, which is
/// replaced by the ground's. The module takes them as values and references no planting's project,
/// so the engine hands one module's places to the other; a grass clump's root, which a planting
/// gives about its patch's middle, is moved by the patch's middle before it comes here.
/// </para>
/// <para>
/// A place is kept where a number drawn from its own seed falls below its chance, so the same seed
/// at the same place on the same ground stands or does not in every engine, and a seed's draw does
/// not hang on the places before it. Two rules over the same seeds draw the same numbers, so a
/// planting that runs two rules over one set gives each its own seeds.
/// </para>
/// </remarks>
public static class Placement
{
    /// <summary>Keeps the places that stand on a field by a rule and a density map.</summary>
    /// <param name="field">The ground.</param>
    /// <param name="places">The places and their seeds, in the planting's order.</param>
    /// <param name="rule">The plant's ranges of height and steepness.</param>
    /// <param name="density">
    /// The plant's density map, or none for a share of one everywhere.
    /// </param>
    /// <returns>
    /// The kept places in the order they came, each on the ground with its normal and its seed.
    /// </returns>
    /// <exception cref="ArgumentException">The density map is not of the field's size.</exception>
    /// <remarks>A place off the field is not kept.</remarks>
    public static IReadOnlyList<Planted> Keep(Heightfield field, IEnumerable<(Vector3 Place, uint Seed)> places, PlacementRule rule, DensityMap? density = null)
    {
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(places);
        ArgumentNullException.ThrowIfNull(rule);
        if (density is not null && (density.Columns != field.Columns || density.Rows != field.Rows))
            throw new ArgumentException($"The density map is of {density.Columns} by {density.Rows} points and the field of {field.Columns} by {field.Rows}.", nameof(density));

        var kept = new List<Planted>();
        foreach (var (place, seed) in places)
        {
            var across = new Vector2(place.X, place.Z);
            if (!field.Covers(across)) continue;
            var height = field.Height(across);
            var chance = rule.Chance(height, field.Steepness(across)) * (density?.At(field, across) ?? 1f);
            if (!(Draw(seed) < chance)) continue;
            kept.Add(new Planted(new Vector3(place.X, height, place.Z), field.Normal(across), seed));
        }

        return kept;
    }

    /// <summary>The number a seed draws against a place's chance, from nothing up to one.</summary>
    /// <param name="seed">The place's seed.</param>
    public static float Draw(uint seed) => new Pcg((ulong)seed * 0x9E3779B97F4A7C15ul + 0x51ED).Unit();
}

/// <summary>
/// A place kept on the ground, where it stands, the ground's way up there and its seed.
/// </summary>
public readonly record struct Planted
{
    /// <summary>A kept place, where it stands, the ground's way up there and its seed.</summary>
    /// <param name="place">
    /// Where it stands, on the ground, in meters about the field's middle.
    /// </param>
    /// <param name="normal">
    /// The way up out of the ground there, at unit length, which a plant may lean by.
    /// </param>
    /// <param name="seed">The seed it came with.</param>
    public Planted(Vector3 place, Vector3 normal, uint seed)
    {
        (Place, Normal, Seed) = (place, normal, seed);
    }

    /// <summary>Where it stands, on the ground, in meters about the field's middle.</summary>
    public Vector3 Place { get; init; }

    /// <summary>
    /// The way up out of the ground there, at unit length, which a plant may lean by.
    /// </summary>
    public Vector3 Normal { get; init; }

    /// <summary>The seed it came with.</summary>
    public uint Seed { get; init; }
}
