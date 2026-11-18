using System.Numerics;
using System.Text;
using Xunit;

namespace Landscape.Tests;

// A step scaled by a picture of weights over a rectangle, so it acts on part of the field.
public class MaskTests
{
    // Weights rising from nothing at the rectangle's least X to one at its greatest, rows alike.
    private sealed class Pictures : ILandscapePictures
    {
        public HeightPicture? Heights(string name) => name == "ramp" ? new HeightPicture(3, 2, [0f, 0.5f, 1f, 0f, 0.5f, 1f]) : null;
    }

    private static readonly Noise Rough = new(5, 10f, 3, 4f);

    [Fact]
    public void A_Masked_Step_Changes_Each_Point_By_Its_Weight_And_Nothing_Outside()
    {
        var plain = new Recipe(65, 33, 1f, [new Stamp(StampShape.Dome, Vector2.Zero, 30f, 10f)]);
        var whole = new Recipe(65, 33, 1f, [.. plain.Steps, Rough]).Build();
        var masked = new Recipe(65, 33, 1f, [.. plain.Steps, new Masked(Rough, "ramp", new(0f, 0f), new(20f, 10f))]).Build(new Pictures());
        var before = plain.Build();

        // The rectangle runs from -10 to 10 along X, its weight a half at X 0 and one at X 10.
        Assert.Equal(before[22, 16], masked[22, 16]);
        Assert.Equal(before[32, 16] + (whole[32, 16] - before[32, 16]) * 0.5f, masked[32, 16]);
        Assert.Equal(whole[42, 16], masked[42, 16]);
        Assert.Equal(before[50, 16], masked[50, 16]);
        Assert.Equal(before[32, 2], masked[32, 2]);
    }

    [Fact]
    public void A_Masked_Step_Is_Written_And_Read_By_Its_Kind_With_Its_Step()
    {
        var recipe = new Recipe(9, 9, 1f, [new Masked(Rough, "ramp", new(1f, 2f), new(4f, 4f))]);
        using var stream = new MemoryStream();
        recipe.Write(stream);
        var text = Encoding.UTF8.GetString(stream.ToArray());

        Assert.Contains("\"kind\": \"masked\"", text, StringComparison.Ordinal);
        Assert.Contains("\"kind\": \"noise\"", text, StringComparison.Ordinal);
        Assert.Equal(recipe, Recipe.Read(new MemoryStream(stream.ToArray())));
    }

    [Fact]
    public void A_Mask_The_Engine_Does_Not_Hold_Or_Of_No_Size_Is_Refused()
    {
        var recipe = new Recipe(9, 9, 1f, [new Masked(Rough, "missing", Vector2.Zero, new(4f, 4f))]);

        Assert.Contains("missing", Assert.Throws<InvalidOperationException>(() => recipe.Build(new Pictures())).Message, StringComparison.Ordinal);
        Assert.Throws<ArgumentOutOfRangeException>("Size", () => new Masked(Rough, "ramp", Vector2.Zero, new(0f, 1f)));
        Assert.Throws<ArgumentNullException>("step", () => new Masked(null!, "ramp", Vector2.Zero, Vector2.One));
    }
}
