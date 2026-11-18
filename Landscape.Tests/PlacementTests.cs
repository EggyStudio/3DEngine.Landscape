using System.Numerics;
using Xunit;

namespace Landscape.Tests;

public class PlacementTests
{
    // The splat tests' ground, three stamps, noise and a steep cone, and a place every meter and a
    // half over it with a seed each, as a planting would give them, at the height zero.
    private static Heightfield Ground() => new Recipe(129, 129, 1f,
        [.. Fields.ThreeStamps().Steps, new Noise(9, 24f, 4, 1.5f), new Stamp(StampShape.Cone, new(-40f, -40f), 15f, 20f)]).Build();

    private static (Vector3 Place, uint Seed)[] Places() =>
        [.. Enumerable.Range(0, 85 * 85).Select(i => (new Vector3(-63f + 1.5f * (i % 85), 0f, -63f + 1.5f * (i / 85)), (uint)i * 2654435761u))];

    [Fact]
    public void A_Kept_Place_Stands_On_The_Ground_With_Its_Normal_And_Its_Seed()
    {
        var field = Ground();
        var kept = Placement.Keep(field, Places(), new PlacementRule());

        Assert.Equal(Places().Length, kept.Count);
        Assert.All(kept, planted =>
        {
            var across = new Vector2(planted.Place.X, planted.Place.Z);
            Assert.Equal(field.Height(across), planted.Place.Y);
            Assert.Equal(field.Normal(across), planted.Normal);
        });
        Assert.Equal(Places().Select(place => place.Seed), kept.Select(planted => planted.Seed));
    }

    [Fact]
    public void A_Place_Is_Kept_Only_Within_The_Rules_Ranges_And_Their_Blends()
    {
        var field = Ground();
        var rule = new PlacementRule(lowest: 1f, highest: 12f, heightBlend: 1f, mostSteep: 0.4f, steepBlend: 0.1f);
        var kept = Placement.Keep(field, Places(), rule);

        Assert.NotEmpty(kept);
        Assert.All(kept, planted =>
        {
            var across = new Vector2(planted.Place.X, planted.Place.Z);
            Assert.InRange(planted.Place.Y, 0f, 13f);
            Assert.True(field.Steepness(across) < 0.5f, $"{across} stands at a steepness of {field.Steepness(across)}.");
        });

        // Every place well inside both ranges is kept, its chance being one.
        var inside = Places().Where(place =>
        {
            var across = new Vector2(place.Place.X, place.Place.Z);
            return field.Height(across) is > 1f and < 12f && field.Steepness(across) < 0.4f;
        }).Select(place => place.Seed).ToHashSet();
        Assert.Subset(kept.Select(planted => planted.Seed).ToHashSet(), inside);
    }

    [Fact]
    public void A_Density_Of_A_Half_Keeps_About_Half_And_None_Keeps_None()
    {
        var field = Ground();
        var half = new DensityMap(129, 129, Enumerable.Repeat(0.5f, 129 * 129).ToArray());
        var none = new DensityMap(129, 129, new float[129 * 129]);

        var share = (float)Placement.Keep(field, Places(), new PlacementRule(), half).Count / Places().Length;
        Assert.InRange(share, 0.48f, 0.52f);
        Assert.Empty(Placement.Keep(field, Places(), new PlacementRule(), none));
    }

    [Fact]
    public void A_Seed_Decides_Its_Own_Place_Whatever_Comes_Before_It()
    {
        var field = Ground();
        var half = new DensityMap(129, 129, Enumerable.Repeat(0.5f, 129 * 129).ToArray());
        var all = Placement.Keep(field, Places(), new PlacementRule(), half);
        var later = Placement.Keep(field, Places()[3000..], new PlacementRule(), half);

        Assert.Equal(all.Where(planted => later.Any(other => other.Seed == planted.Seed)), later);
        Assert.Equal(all.SkipWhile(planted => !later.Contains(planted)), later);
    }

    [Fact]
    public void A_Splat_Materials_Weight_Is_A_Density_Map()
    {
        var field = Ground();
        var splat = SplatMap.Of(field, [new SplatRule("grass", mostSteep: 0.5f, steepBlend: 0.2f), new SplatRule("rock", leastSteep: 0.7f, steepBlend: 0.2f)]);
        var grass = DensityMap.Of(splat, 0);

        Assert.Equal(splat.Weight(0, 30, 40), grass.Shares[40 * 129 + 30]);
        Assert.Equal(splat.Weight(0, 30, 40), grass.At(field, field.Place(30, 40)));
        // The density is bilinear between the points, so a kept place has a corner of its cell
        // where grass lies, a point no steeper than grass grows on with its blend.
        var kept = Placement.Keep(field, Places(), new PlacementRule(), grass);
        Assert.NotEmpty(kept);
        Assert.All(kept, planted =>
        {
            var (column, row) = ((int)(planted.Place.X + 64f), (int)(planted.Place.Z + 64f));
            var corners = new[] { (column, row), (column + 1, row), (column, row + 1), (column + 1, row + 1) };
            Assert.Contains(corners, corner => field.Steepness(Math.Min(corner.Item1, 128), Math.Min(corner.Item2, 128)) < 0.7f);
        });
    }

    [Fact]
    public void A_Place_Off_The_Field_Is_Not_Kept()
    {
        var field = new Heightfield(5, 5, 1f);

        Assert.Empty(Placement.Keep(field, [(new Vector3(2.5f, 0f, 0f), 1u), (new Vector3(0f, 0f, -2.01f), 2u)], new PlacementRule()));
        Assert.Single(Placement.Keep(field, [(new Vector3(2f, 0f, -2f), 3u)], new PlacementRule()));
    }

    [Fact]
    public void Placement_Gives_The_Same_Places_On_Every_System()
    {
        var field = Ground();
        var kept = Placement.Keep(field, Places(), new PlacementRule(highest: 10f, heightBlend: 2f, mostSteep: 0.5f, steepBlend: 0.2f), DensityMap.Of(SplatMap.Of(field, [new SplatRule("a", highest: 6f, heightBlend: 4f)]), 0));

        var hash = 14695981039346656037ul;
        foreach (var planted in kept) hash = (hash ^ planted.Seed ^ (ulong)BitConverter.SingleToInt32Bits(planted.Place.Y) << 32) * 1099511628211ul;
        Assert.Equal(PinnedPlaces, hash);
    }

    [Fact]
    public void Placement_Refuses_A_Map_Or_A_Rule_It_Cannot_Read()
    {
        var field = new Heightfield(5, 5, 1f);

        Assert.Throws<ArgumentException>("density", () => Placement.Keep(field, [], new PlacementRule(), new DensityMap(4, 5, new float[20])));
        Assert.Throws<ArgumentException>("shares", () => new DensityMap(5, 5, new float[24]));
        Assert.Throws<ArgumentException>("shares", () => new DensityMap(2, 2, [0f, 1f, 1.5f, 0f]));
        Assert.Throws<ArgumentException>("shares", () => new DensityMap(2, 2, [0f, 1f, float.NaN, 0f]));
        Assert.Throws<ArgumentOutOfRangeException>("highest", () => new PlacementRule(lowest: 2f, highest: 1f));
        Assert.Throws<ArgumentOutOfRangeException>("material", () => DensityMap.Of(SplatMap.Of(field, [new SplatRule("a")]), 1));
    }

    // The hash of the kept places' seeds and heights, read on Linux at the commit that wrote these
    // tests and held on Windows and macOS by the build workflow.
    private const ulong PinnedPlaces = 15254396740630659621ul;
}
