using System.Numerics;
using Xunit;

namespace Landscape.Tests;

// A picture turned by quarter turns within its rectangle, with no sine or cosine, so a turned stamp
// is the stamp of the picture turned by hand.
public class TurnTests
{
    // A picture of 3 by 2 texels, no two alike, and the same turned by hand, a quarter turn taking
    // its first row, along X, to run along Z at the rectangle's greatest X.
    private static readonly HeightPicture Flat = new(3, 2, [0.0f, 0.1f, 0.2f, 0.5f, 0.7f, 1.0f]);
    private static readonly HeightPicture Quarter = new(2, 3, [0.5f, 0.0f, 0.7f, 0.1f, 1.0f, 0.2f]);
    private static readonly HeightPicture Half = new(3, 2, [1.0f, 0.7f, 0.5f, 0.2f, 0.1f, 0.0f]);

    private sealed class Pictures : ILandscapePictures
    {
        public HeightPicture? Heights(string name) => name switch { "flat" => Flat, "quarter" => Quarter, "half" => Half, _ => null };
    }

    private static Heightfield Stamped(string picture, int turns) =>
        new Recipe(41, 41, 1f, [new PictureStamp(picture, Vector2.Zero, new(20f, 20f), 5f, StampBlend.Add, 0f, turns)]).Build(new Pictures());

    [Theory]
    [InlineData(1, "quarter")]
    [InlineData(2, "half")]
    public void A_Turned_Stamp_Is_The_Stamp_Of_The_Picture_Turned_By_Hand(int turns, string turned)
    {
        Assert.Equal(Stamped(turned, 0).Heights, Stamped("flat", turns).Heights, (a, b) => MathF.Abs(a - b) < 1e-5f);
    }

    [Fact]
    public void Three_Turns_Are_A_Half_Turn_Of_A_Quarter()
    {
        var three = new Recipe(41, 41, 1f, [new PictureStamp("quarter", Vector2.Zero, new(20f, 20f), 5f, StampBlend.Add, 0f, 2)]).Build(new Pictures());

        Assert.Equal(three.Heights, Stamped("flat", 3).Heights, (a, b) => MathF.Abs(a - b) < 1e-5f);
    }

    [Fact]
    public void A_Masked_Step_Turns_Its_Mask_As_A_Stamp_Turns_Its_Picture()
    {
        var noise = new Noise(5, 8f, 3, 2f);
        var masked = new Recipe(41, 41, 1f, [new Masked(noise, "flat", Vector2.Zero, new(20f, 20f), 1)]).Build(new Pictures());
        var byHand = new Recipe(41, 41, 1f, [new Masked(noise, "quarter", Vector2.Zero, new(20f, 20f))]).Build(new Pictures());

        Assert.Equal(byHand.Heights, masked.Heights, (a, b) => MathF.Abs(a - b) < 1e-5f);
    }

    [Fact]
    public void A_Turn_Outside_Nothing_To_Three_Is_Refused_And_A_Turn_Is_Kept_In_Json()
    {
        Assert.Throws<ArgumentOutOfRangeException>("Turns", () => new PictureStamp("flat", Vector2.Zero, Vector2.One, 1f, StampBlend.Add, 0f, 4));
        Assert.Throws<ArgumentOutOfRangeException>("Turns", () => new Masked(new Smoothing(1), "flat", Vector2.Zero, Vector2.One, -1));
        var recipe = new Recipe(9, 9, 1f, [new PictureStamp("flat", Vector2.Zero, new(4f, 4f), 1f, StampBlend.Add, 0f, 3)]);
        using var stream = new MemoryStream();
        recipe.Write(stream);
        Assert.Equal(recipe, Recipe.Read(new MemoryStream(stream.ToArray())));
    }
}
