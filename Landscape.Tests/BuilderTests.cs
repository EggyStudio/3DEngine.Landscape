using System.Numerics;
using Xunit;

namespace Landscape.Tests;

public class BuilderTests
{
    private static Recipe Steps(Vector2 stamp, float talus = 0.9f) => new(129, 129, 1f,
    [
        new Noise(11, 48f, 6, 8f),
        new Stamp(StampShape.Dome, stamp, 20f, 15f),
        new Terrace(3f, 0.4f),
        new ThermalErosion(talus, 30),
    ]);

    [Fact]
    public void A_Rebuild_After_A_Change_Equals_A_Whole_Build_And_Applies_From_The_Change_On()
    {
        var builder = new LandscapeBuilder(Steps(new(-30f, 0f)));
        var change = builder.Rebuild(Steps(new(-30f, 0f), talus: 0.6f));

        Assert.Equal((3, 1), (change.FirstStep, change.StepsApplied));
        Assert.Equal(Steps(new(-30f, 0f), talus: 0.6f).Build().Heights, builder.Field.Heights);

        change = builder.Rebuild(Steps(new(25f, 10f), talus: 0.6f));
        Assert.Equal((1, 3), (change.FirstStep, change.StepsApplied));
        Assert.Equal(Steps(new(25f, 10f), talus: 0.6f).Build().Heights, builder.Field.Heights);
    }

    [Fact]
    public void A_Rebuild_Of_The_Same_Recipe_Applies_Nothing_And_Changes_Nothing()
    {
        var builder = new LandscapeBuilder(Steps(Vector2.Zero));
        var change = builder.Rebuild(Steps(Vector2.Zero));

        Assert.Equal(new LandscapeChange(4, 0, null), change);
    }

    [Fact]
    public void A_Moved_Stamp_Changes_The_Points_Under_Its_Old_And_New_Places_Alone()
    {
        static Recipe Moved(Vector2 place) => new(129, 129, 1f,
        [
            new Noise(2, 30f, 4, 3f),
            new Stamp(StampShape.Cone, place, 10f, 6f),
        ]);
        var builder = new LandscapeBuilder(Moved(new(-20f, -20f)));
        var before = builder.Field;
        var change = builder.Rebuild(Moved(new(15f, 5f)));

        // The old cone stood over points 34 to 54 each way and the new over 69 to 89 along X and 59
        // to 79 along Z, a point at the radius itself staying as it was.
        Assert.Equal(new LandscapeArea(35, 35, 88, 78), change.Changed);
        for (var row = 0; row < 129; row++)
        {
            for (var column = 0; column < 129; column++)
            {
                if (column is >= 35 and <= 88 && row is >= 35 and <= 78) continue;
                Assert.Equal(before[column, row], builder.Field[column, row]);
            }
        }
    }

    [Fact]
    public void A_Recipe_Of_Another_Size_Builds_Whole()
    {
        var builder = new LandscapeBuilder(Steps(Vector2.Zero));
        var change = builder.Rebuild(new Recipe(65, 33, 1f, Steps(Vector2.Zero).Steps));

        Assert.Equal(new LandscapeChange(0, 4, new LandscapeArea(0, 0, 64, 32)), change);
        Assert.Equal(new Recipe(65, 33, 1f, Steps(Vector2.Zero).Steps).Build().Heights, builder.Field.Heights);
    }

    [Fact]
    public void Writing_Into_The_Field_Changes_Nothing_A_Rebuild_Starts_From()
    {
        var builder = new LandscapeBuilder(Steps(Vector2.Zero));
        Array.Fill(builder.Field.Heights, 100f);
        builder.Rebuild(Steps(Vector2.Zero, talus: 0.5f));

        Assert.Equal(Steps(Vector2.Zero, talus: 0.5f).Build().Heights, builder.Field.Heights);
    }

    [Fact]
    public void A_Rebuild_Asked_To_Stop_Keeps_The_Field_And_Recipe_It_Had()
    {
        var builder = new LandscapeBuilder(Steps(Vector2.Zero));
        var (recipe, heights) = (builder.Recipe, builder.Field.Heights.ToArray());
        using var cancel = new CancellationTokenSource();
        cancel.Cancel();

        Assert.Throws<OperationCanceledException>(() => builder.Rebuild(Steps(new(9f, 9f)), cancel.Token));
        Assert.Same(recipe, builder.Recipe);
        Assert.Equal(heights, builder.Field.Heights);
    }

    [Theory]
    [InlineData(30, 30, 0, 0)]
    [InlineData(64, 64, 0, 1)]
    [InlineData(63, 63, 0, 1)]
    [InlineData(65, 65, 0, 1)]
    [InlineData(66, 66, 1, 1)]
    [InlineData(128, 128, 1, 1)]
    public void A_Change_Reaches_The_Tiles_Whose_Corners_Or_Normals_It_Changes(int first, int last, int fromTile, int toTile)
    {
        var tiles = new LandscapeArea(first, 10, last, 10).Tiles(new Heightfield(129, 129, 1f));

        Assert.Equal(Enumerable.Range(fromTile, toTile - fromTile + 1).Select(column => (column, 0)), tiles);
    }
}
