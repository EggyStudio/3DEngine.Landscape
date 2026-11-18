using System.Numerics;
using Xunit;

namespace Landscape.Tests;

public class StampTests
{
    [Fact]
    public void A_Dome_Stands_At_Its_Height_At_Its_Middle_And_At_Nothing_From_Its_Radius_Out()
    {
        var field = new Recipe(101, 101, 1f, [new Stamp(StampShape.Dome, Vector2.Zero, 20f, 10f)]).Build();

        Assert.Equal(10f, field[50, 50]);
        for (var row = 0; row < field.Rows; row++)
        {
            for (var column = 0; column < field.Columns; column++)
            {
                var place = field.Place(column, row);
                if (place.Length() >= 20f) Assert.Equal(0f, field[column, row]);
                else Assert.True(field[column, row] > 0f, $"{column}, {row} is {field[column, row]}");
            }
        }
    }

    [Theory]
    [InlineData(StampShape.Cone, 1f, 0.5f, 0f)]
    [InlineData(StampShape.Dome, 1f, 0.5625f, 0f)]
    [InlineData(StampShape.Plateau, 1f, 1f, 0f)]
    [InlineData(StampShape.Crater, -0.4f, -0.4f + 1.4f * (0.5f / 0.7f) * (0.5f / 0.7f), 0f)]
    public void Each_Shape_Rises_By_Its_Profile(StampShape shape, float middle, float half, float edge)
    {
        var stamp = new Stamp(shape, Vector2.Zero, 8f, 1f);

        Assert.Equal(middle, stamp.Profile(0f), 1e-6f);
        Assert.Equal(half, stamp.Profile(4f), 1e-6f);
        Assert.Equal(edge, stamp.Profile(8f));
        Assert.Equal(0f, stamp.Profile(100f));
    }

    [Fact]
    public void A_Crater_Has_Its_Rim_Seven_Tenths_Out()
    {
        var crater = new Stamp(StampShape.Crater, Vector2.Zero, 10f, 1f);

        Assert.Equal(1f, crater.Profile(7f), 1e-6f);
        Assert.True(crater.Profile(6.5f) < 1f && crater.Profile(7.5f) < 1f);
    }

    [Fact]
    public void A_Stamp_Over_The_Edge_Is_Cut_There()
    {
        var field = new Recipe(21, 21, 1f, [new Stamp(StampShape.Cone, new(10f, 10f), 6f, 6f)]).Build();

        Assert.Equal(6f, field[20, 20]);
        Assert.Equal(1f, field[15, 20], 1e-6f);
        Assert.Equal(0f, field[14, 20]);
        Assert.Equal(0f, field[0, 0]);
    }

    [Fact]
    public void Two_Stamps_That_Overlap_Sum_By_Adding_And_Give_The_Larger_By_The_Larger()
    {
        Stamp Cone(float x, float height, StampBlend blend) => new(StampShape.Cone, new(x, 0f), 4f, height, blend);
        var added = new Recipe(21, 3, 1f, [Cone(-1f, 4f, StampBlend.Add), Cone(1f, 3f, StampBlend.Add)]).Build();
        var larger = new Recipe(21, 3, 1f, [Cone(-1f, 4f, StampBlend.Add), Cone(1f, 3f, StampBlend.Max)]).Build();
        var smaller = new Recipe(21, 3, 1f, [Cone(-1f, 4f, StampBlend.Add), Cone(1f, 3f, StampBlend.Min)]).Build();

        // The point at the second cone's middle, X 1, two meters from the first's and so at half
        // its height of four, two by the first and three by the second.
        Assert.Equal(2f + 3f, added[11, 1]);
        Assert.Equal(3f, larger[11, 1]);
        Assert.Equal(2f, smaller[11, 1]);

        // Past the second cone's radius on the far side of the first, the first alone.
        Assert.Equal(added[6, 1], larger[6, 1]);
        Assert.Equal(added[6, 1], smaller[6, 1]);
    }

    [Fact]
    public void A_Plateau_That_Replaces_Flattens_Its_Middle_And_Keeps_The_Ground_At_Its_Edge()
    {
        var field = new Recipe(41, 41, 1f,
        [
            new Stamp(StampShape.Cone, Vector2.Zero, 40f, 20f),
            new Stamp(StampShape.Plateau, Vector2.Zero, 10f, 5f, StampBlend.Replace),
        ]).Build();

        Assert.Equal(5f, field[20, 20]);
        Assert.Equal(5f, field[24, 20]);
        Assert.Equal(20f * (1f - 10f / 40f), field[30, 20], 1e-5f);
    }

    [Fact]
    public void Three_Stamps_Give_The_Same_Bits_Each_Build_And_On_Every_System()
    {
        var first = Fields.ThreeStamps().Build();
        var second = Fields.ThreeStamps().Build();

        Assert.Equal(first.Heights, second.Heights);
        Assert.Equal(PinnedHash, Fields.Hash(first));
    }

    [Fact]
    public void A_Stamp_Refuses_What_It_Cannot_Press()
    {
        Assert.Throws<ArgumentOutOfRangeException>("Radius", () => new Stamp(StampShape.Dome, Vector2.Zero, 0f, 1f));
        Assert.Throws<ArgumentOutOfRangeException>("Radius", () => new Stamp(StampShape.Dome, Vector2.Zero, float.NaN, 1f));
        Assert.Throws<ArgumentOutOfRangeException>("Height", () => new Stamp(StampShape.Dome, Vector2.Zero, 1f, float.NaN));
        Assert.Throws<ArgumentOutOfRangeException>("Place", () => new Stamp(StampShape.Dome, new(float.PositiveInfinity, 0f), 1f, 1f));
        Assert.Throws<ArgumentOutOfRangeException>("Shape", () => new Stamp((StampShape)9, Vector2.Zero, 1f, 1f));
        Assert.Throws<ArgumentOutOfRangeException>("Blend", () => new Stamp(StampShape.Dome, Vector2.Zero, 1f, 1f, (StampBlend)9));
        var stamp = new Stamp(StampShape.Dome, Vector2.Zero, 1f, 1f);
        Assert.Throws<ArgumentOutOfRangeException>("Radius", () => stamp with { Radius = -1f });
    }

    // The hash of Fields.ThreeStamps' heights, read on Linux at the commit that wrote these tests
    // and held on Windows and macOS by the build workflow.
    private const ulong PinnedHash = 13085094106724815367ul;
}
