using System.Numerics;
using Xunit;

namespace Landscape.Tests;

public class SplatTests
{
    // Sand low, grass on gentle ground, rock on steep ground and snow high, each blending into the
    // next, over the three stamps and noise.
    private static readonly SplatRule[] Four =
    [
        new("sand", highest: 1f, heightBlend: 1f, mostSteep: 0.5f, steepBlend: 0.2f),
        new("grass", lowest: 1f, highest: 12f, heightBlend: 1f, mostSteep: 0.5f, steepBlend: 0.2f),
        new("rock", leastSteep: 0.7f, steepBlend: 0.2f),
        new("snow", lowest: 14f, heightBlend: 2f, mostSteep: 0.7f, steepBlend: 0.2f),
    ];

    private static Heightfield Ground() => new Recipe(129, 129, 1f,
        [.. Fields.ThreeStamps().Steps, new Noise(9, 24f, 4, 1.5f), new Stamp(StampShape.Cone, new(-40f, -40f), 15f, 20f)]).Build();

    [Fact]
    public void The_Weights_Sum_To_One_And_The_Bytes_To_255_At_Every_Point()
    {
        var map = SplatMap.Of(Ground(), Four);

        Assert.Equal(["sand", "grass", "rock", "snow"], map.Materials);
        Assert.Single(map.Layers);
        for (var row = 0; row < map.Rows; row++)
        {
            for (var column = 0; column < map.Columns; column++)
            {
                var weights = Enumerable.Range(0, 4).Sum(m => map.Weight(m, column, row));
                Assert.Equal(1f, weights, 1e-6f);
                var at = (row * map.Columns + column) * 4;
                Assert.Equal(255, map.Layers[0][at] + map.Layers[0][at + 1] + map.Layers[0][at + 2] + map.Layers[0][at + 3]);
            }
        }
    }

    [Fact]
    public void A_Material_Weighs_Nothing_Beyond_Its_Ranges_And_Their_Blends()
    {
        var field = Ground();
        var map = SplatMap.Of(field, Four);

        for (var row = 0; row < map.Rows; row++)
        {
            for (var column = 0; column < map.Columns; column++)
            {
                var (height, steepness) = (field[column, row], field.Steepness(column, row));
                if (height < 12f) Assert.Equal(0f, map.Weight(3, column, row));
                if (steepness < 0.5f) Assert.Equal(0f, map.Weight(2, column, row));
                if (height > 13f || steepness > 0.7f) Assert.Equal(0f, map.Weight(1, column, row));
            }
        }
    }

    [Fact]
    public void A_Steep_Face_Goes_To_Rock_And_Level_Ground_To_Grass()
    {
        var field = Ground();
        var map = SplatMap.Of(field, Four);

        // The cone at -40, -40 rises 20 m over 15 m, a steepness of 1.33 on its flank, and the
        // ground far from every stamp stands within a noise of 1.5 m at level.
        var flank = (24 + 8, 24);
        Assert.True(field.Steepness(flank.Item1, flank.Item2) > 1f);
        Assert.Equal(1f, map.Weight(2, flank.Item1, flank.Item2));
        Assert.Equal(255, map.Layers[0][(flank.Item2 * 129 + flank.Item1) * 4 + 2]);

        var level = Enumerable.Range(0, 129 * 129).Select(i => (Column: i % 129, Row: i / 129))
            .First(p => field[p.Column, p.Row] is > 2f and < 11f && field.Steepness(p.Column, p.Row) < 0.3f);
        Assert.Equal(1f, map.Weight(1, level.Column, level.Row));
    }

    [Fact]
    public void Where_No_Rule_Reaches_The_First_Material_Covers_The_Point()
    {
        var field = new Heightfield(3, 3, 1f);
        var map = SplatMap.Of(field, [new SplatRule("base", lowest: 5f), new SplatRule("high", lowest: 10f)]);

        Assert.Equal(1f, map.Weight(0, 1, 1));
        Assert.Equal(0f, map.Weight(1, 1, 1));
        Assert.Equal([255, 0, 0, 0], map.Layers[0].Take(4).Select(b => (int)b));
    }

    [Fact]
    public void Five_Materials_Take_Two_Layers_The_Second_Holding_One()
    {
        SplatRule[] five = [.. Four, new("mud", highest: 0.5f, mostSteep: 0.2f)];
        var map = SplatMap.Of(Ground(), five);

        Assert.Equal(2, map.Layers.Count);
        Assert.Equal(129 * 129 * 4, map.Layers[1].Length);
        for (var point = 0; point < 129 * 129; point++)
        {
            Assert.Equal(0, map.Layers[1][point * 4 + 1] + map.Layers[1][point * 4 + 2] + map.Layers[1][point * 4 + 3]);
            Assert.Equal(255, map.Layers[0].Skip(point * 4).Take(4).Sum(b => b) + map.Layers[1][point * 4]);
        }
    }

    [Fact]
    public void A_Ground_Gives_The_Same_Bytes_On_Every_System()
    {
        var map = SplatMap.Of(Ground(), Four);

        var hash = 14695981039346656037ul;
        foreach (var value in map.Layers[0]) hash = (hash ^ value) * 1099511628211ul;
        Assert.Equal(PinnedBytes, hash);
    }

    [Fact]
    public void A_Splat_Map_Refuses_Rules_It_Cannot_Keep()
    {
        var field = new Heightfield(2, 2, 1f);

        Assert.Throws<ArgumentException>("rules", () => SplatMap.Of(field, []));
        Assert.Throws<ArgumentException>("rules", () => SplatMap.Of(field, [.. Enumerable.Range(0, 9).Select(i => new SplatRule($"m{i}"))]));
        Assert.Throws<ArgumentException>("rules", () => SplatMap.Of(field, [new SplatRule("a"), new SplatRule("a")]));
        Assert.Throws<ArgumentOutOfRangeException>("highest", () => new SplatRule("a", lowest: 2f, highest: 1f));
        Assert.Throws<ArgumentOutOfRangeException>("mostSteep", () => new SplatRule("a", leastSteep: 1f, mostSteep: 0.5f));
        Assert.Throws<ArgumentOutOfRangeException>("heightBlend", () => new SplatRule("a", heightBlend: -1f));
        Assert.Throws<ArgumentOutOfRangeException>("steepBlend", () => new SplatRule("a", steepBlend: float.PositiveInfinity));
        Assert.Throws<ArgumentException>("material", () => new SplatRule(" "));
    }

    // The hash of the four materials' layer over the ground, read on Linux at the commit that
    // wrote these tests and held on Windows and macOS by the build workflow.
    private const ulong PinnedBytes = 8105046349425891616ul;
}
