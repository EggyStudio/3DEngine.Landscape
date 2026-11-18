using System.Numerics;
using Xunit;

namespace Landscape.Tests;

// A splat map weighed again over a change alone, as a rebuild's changed area gives it.
public class SplatUpdateTests
{
    private static readonly SplatRule[] Rules =
    [
        new("sand", highest: 1f, heightBlend: 1f),
        new("grass", mostSteep: 0.5f, steepBlend: 0.2f),
        new("rock", leastSteep: 0.7f, steepBlend: 0.2f),
        new("snow", lowest: 9f, heightBlend: 2f),
        new("mud", highest: 0.3f, mostSteep: 0.1f),
    ];

    private static Recipe Ground(Vector2 cone) => new(129, 129, 1f, [new Noise(5, 30f, 4, 2f), new Stamp(StampShape.Cone, cone, 12f, 14f)]);

    [Fact]
    public void An_Update_Over_A_Change_Equals_A_Whole_Map_Of_The_New_Field()
    {
        var builder = new LandscapeBuilder(Ground(new(-30f, -20f)));
        var splat = SplatMap.Of(builder.Field, Rules);
        var change = builder.Rebuild(Ground(new(10f, 25f)));

        var updated = splat.Update(builder.Field, change.Changed!.Value);
        var whole = SplatMap.Of(builder.Field, Rules);

        Assert.NotNull(updated);
        for (var layer = 0; layer < whole.Layers.Count; layer++) Assert.Equal(whole.Layers[layer], splat.Layers[layer]);
        for (var row = 0; row < 129; row++)
        {
            for (var column = 0; column < 129; column++)
            {
                for (var m = 0; m < Rules.Length; m++) Assert.Equal(whole.Weight(m, column, row), splat.Weight(m, column, row));
            }
        }
    }

    [Fact]
    public void An_Update_Touches_The_Changed_Points_And_Those_Beside_Them_Alone()
    {
        var builder = new LandscapeBuilder(Ground(new(-30f, -20f)));
        var splat = SplatMap.Of(builder.Field, Rules);
        var before = splat.Layers.Select(layer => layer.ToArray()).ToArray();
        var area = builder.Rebuild(Ground(new(-28f, -20f))).Changed!.Value;

        var updated = splat.Update(builder.Field, area)!.Value;

        // A point's steepness reads the points beside it, so the area grows a point each way.
        Assert.Equal(new LandscapeArea(area.FirstColumn - 1, area.FirstRow - 1, area.LastColumn + 1, area.LastRow + 1), updated);
        for (var layer = 0; layer < before.Length; layer++)
        {
            for (var row = 0; row < 129; row++)
            {
                for (var column = 0; column < 129; column++)
                {
                    if (column >= updated.FirstColumn && column <= updated.LastColumn && row >= updated.FirstRow && row <= updated.LastRow) continue;
                    for (var channel = 0; channel < 4; channel++)
                    {
                        var at = (row * 129 + column) * 4 + channel;
                        Assert.Equal(before[layer][at], splat.Layers[layer][at]);
                    }
                }
            }
        }
    }

    [Fact]
    public void An_Update_At_The_Edge_Stays_In_The_Field_And_Another_Field_Is_Refused()
    {
        var field = Ground(Vector2.Zero).Build();
        var splat = SplatMap.Of(field, Rules);

        Assert.Equal(new LandscapeArea(0, 1, 3, 128), splat.Update(field, new LandscapeArea(0, 2, 2, 128)));
        Assert.Throws<ArgumentException>("field", () => splat.Update(new Heightfield(65, 65, 1f), new LandscapeArea(0, 0, 1, 1)));
        Assert.Throws<ArgumentOutOfRangeException>("area", () => splat.Update(field, new LandscapeArea(5, 5, 200, 6)));
    }
}
