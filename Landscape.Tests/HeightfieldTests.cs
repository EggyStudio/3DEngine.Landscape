using System.Numerics;
using Xunit;

namespace Landscape.Tests;

public class HeightfieldTests
{
    [Fact]
    public void An_Empty_Recipe_Builds_Flat_Ground_Of_Its_Size()
    {
        var field = new Recipe(33, 17, 2f, []).Build();

        Assert.Equal((33, 17, 2f), (field.Columns, field.Rows, field.Spacing));
        Assert.Equal(33 * 17, field.Heights.Length);
        Assert.All(field.Heights, height => Assert.Equal(0f, height));
        Assert.Equal(new Vector2(64f, 32f), field.Size);
        Assert.Equal(new Vector2(-32f, -16f), field.Place(0, 0));
        Assert.Equal(new Vector2(32f, 16f), field.Place(32, 16));
    }

    [Fact]
    public void A_Height_Between_Points_Is_Bilinear_And_Past_The_Edge_The_Edges()
    {
        var field = new Heightfield(2, 2, 4f) { [0, 0] = 1f, [1, 0] = 3f, [0, 1] = 5f, [1, 1] = 11f };

        Assert.Equal(5f, field.Height(Vector2.Zero));
        Assert.Equal(2f, field.Height(new(0f, -2f)));
        Assert.Equal(8f, field.Height(new(0f, 2f)));
        Assert.Equal(11f, field.Height(new(50f, 50f)));
        Assert.Equal(1f, field.Height(new(-50f, -50f)));
    }

    [Fact]
    public void A_Ramp_Of_45_Degrees_Reads_45()
    {
        var field = new Heightfield(9, 9, 0.5f);
        for (var row = 0; row < field.Rows; row++)
        {
            for (var column = 0; column < field.Columns; column++)
                field[column, row] = field.Place(column, row).X;
        }

        Assert.Equal(MathF.PI / 4f, field.Slope(new(0.3f, -1.1f)), 1e-6f);
        Assert.Equal(MathF.PI / 4f, field.Slope(new(-2f, 2f)), 1e-6f);
        var expected = new Vector3(-1f, 1f, 0f) / MathF.Sqrt(2f);
        Assert.Equal(expected, field.Normal(0, 4));
        Assert.Equal(expected, field.Normal(4, 4));
        Assert.Equal(expected, field.Normal(8, 8));
    }

    [Fact]
    public void A_Field_Refuses_A_Size_It_Cannot_Hold()
    {
        Assert.Throws<ArgumentOutOfRangeException>("columns", () => new Heightfield(1, 2, 1f));
        Assert.Throws<ArgumentOutOfRangeException>("rows", () => new Heightfield(2, 0, 1f));
        Assert.Throws<ArgumentOutOfRangeException>("spacing", () => new Heightfield(2, 2, 0f));
        Assert.Throws<ArgumentOutOfRangeException>("spacing", () => new Heightfield(2, 2, float.NaN));
        Assert.Throws<ArgumentOutOfRangeException>("spacing", () => new Recipe(2, 2, float.PositiveInfinity, []));
        Assert.Throws<ArgumentNullException>("steps", () => new Recipe(2, 2, 1f, null!));
        Assert.Throws<ArgumentException>("steps", () => new Recipe(2, 2, 1f, [null!]));
        Assert.Throws<ArgumentOutOfRangeException>("column", () => new Heightfield(2, 2, 1f)[2, 0]);
    }

    [Fact]
    public void A_Build_Asked_To_Stop_Stops()
    {
        using var cancel = new CancellationTokenSource();
        cancel.Cancel();

        Assert.Throws<OperationCanceledException>(() => Fields.ThreeStamps().Build(cancel.Token));
    }

    [Fact]
    public void Recipes_Of_The_Same_Steps_Are_Equal()
    {
        Assert.Equal(Fields.ThreeStamps(), Fields.ThreeStamps());
        Assert.Equal(Fields.ThreeStamps().GetHashCode(), Fields.ThreeStamps().GetHashCode());
        Assert.NotEqual(Fields.ThreeStamps(), new Recipe(129, 129, 1f, [.. Fields.ThreeStamps().Steps.Take(2)]));
    }
}
