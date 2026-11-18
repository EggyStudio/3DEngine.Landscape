using System.Numerics;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Landscape.Tests;

public class RecipeJsonTests
{
    // A recipe of every step the module has, with numbers no float reads back from a short text.
    private static Recipe Every() => new(65, 33, 0.75f,
    [
        new Stamp(StampShape.Crater, new(1.1f, -2.3f), 9.7f, 3.3f, StampBlend.Max),
        new PictureStamp("ridge", new(-4f, 2f), new(20f, 10f), 6.1f, StampBlend.Replace, 2.5f),
        new Noise(17, 33.3f, 5, 2.2f, 0.45f),
        new Terrace(1.7f, 0.3f),
        new Smoothing(2),
        new ThermalErosion(0.7f, 20, 0.4f),
        new HydraulicErosion(5, 500, lifetime: 20, inertia: 0.1f, radius: 2, scale: 12.5f),
    ]);

    private static string Text(Recipe recipe)
    {
        using var stream = new MemoryStream();
        recipe.Write(stream);
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static Recipe Read(string text) => Recipe.Read(new MemoryStream(Encoding.UTF8.GetBytes(text)));

    [Fact]
    public void A_Recipe_Read_Back_Equals_The_One_Written_And_Builds_The_Same_Bits()
    {
        var read = Read(Text(Every()));

        Assert.Equal(Every(), read);
        var pictures = new Pictures(("ridge", Ridge()));
        Assert.Equal(Every().Build(pictures).Heights, read.Build(pictures).Heights);
        Assert.Equal(PinnedEvery, Fields.Hash(read.Build(pictures)));
    }

    // The hash of the recipe of every step's heights, read on Linux at the commit that wrote these
    // tests and held on Windows and macOS by the build workflow.
    private const ulong PinnedEvery = 16712446001608249742ul;

    [Fact]
    public void A_Stamp_Is_Written_By_Its_Kind_And_Its_Numbers_In_Camel_Case()
    {
        var text = Text(new Recipe(3, 3, 1f, [new Stamp(StampShape.Dome, new(0.5f, -1f), 2f, 0.1f)]));

        Assert.Equal("""
            {
              "format": 1,
              "columns": 3,
              "rows": 3,
              "spacing": 1,
              "steps": [
                {
                  "kind": "stamp",
                  "shape": "Dome",
                  "place": [
                    0.5,
                    -1
                  ],
                  "radius": 2,
                  "height": 0.1,
                  "blend": "Add"
                }
              ]
            }
            """, text.ReplaceLineEndings("\n"));
    }

    [Theory]
    [InlineData("""{"format": 2, "columns": 3, "rows": 3, "spacing": 1, "steps": []}""", "format 2")]
    [InlineData("""{"format": 1, "columns": 3, "rows": 3, "spacing": 1, "steps": [{"kind": "volcano"}]}""", "volcano")]
    [InlineData("""{"format": 1, "columns": 3, "rows": 3, "spacing": 1, "colour": 2, "steps": []}""", "colour")]
    [InlineData("""{"format": 1, "columns": 3, "rows": 3, "spacing": 1, "steps": [{"kind": "smoothing", "passes": 0}]}""", "Passes")]
    [InlineData("""{"format": 1, "columns": 1, "rows": 3, "spacing": 1, "steps": []}""", "columns")]
    [InlineData("""{"format": 1, "columns": 3, "rows": 3, "spacing": 1, "steps": [{"kind": "stamp", "shape": "Dome", "place": [0], "radius": 1, "height": 1, "blend": "Add"}]}""", "vector")]
    public void A_File_Of_Another_Format_Or_An_Unknown_Or_Refused_Step_Is_Refused_By_Name(string text, string named)
    {
        var refused = Assert.ThrowsAny<JsonException>(() => Read(text));

        Assert.Contains(named, refused.Message + refused.InnerException?.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_Picture_Stamp_Draws_The_Ground_Toward_Its_Picture_Inside_Its_Rectangle_Alone()
    {
        var recipe = new Recipe(41, 21, 1f, [new PictureStamp("ridge", new(-5f, 0f), new(20f, 10f), 8f)]);
        var field = recipe.Build(new Pictures(("ridge", Ridge())));

        // The rectangle runs from -15 to 5 along X and -5 to 5 along Z, the ridge's picture rising
        // from nothing at its least X to one at its greatest and the same along every row.
        Assert.Equal(0f, field[5, 10]);
        Assert.Equal(4f, field[15, 10], 1e-5f);
        Assert.Equal(8f, field[25, 10], 1e-5f);
        Assert.Equal(8f, field[25, 5], 1e-5f);
        Assert.Equal(0f, field[26, 10]);
        Assert.Equal(0f, field[15, 4]);
        Assert.Equal(0f, field[4, 10]);
    }

    [Fact]
    public void A_Picture_Stamps_Edge_Fades_It_Into_The_Ground()
    {
        var recipe = new Recipe(41, 21, 1f, [new PictureStamp("flat", Vector2.Zero, new(20f, 10f), 4f, StampBlend.Replace, 4f)]);
        var field = recipe.Build(new Pictures(("flat", new HeightPicture(2, 2, [1f, 1f, 1f, 1f]))));

        Assert.Equal(4f, field[20, 10]);
        Assert.Equal(4f, field[26, 10]);
        Assert.Equal(2f, field[28, 10], 1e-5f);
        Assert.Equal(0f, field[30, 10]);
    }

    [Fact]
    public void A_Picture_The_Engine_Does_Not_Hold_Is_Refused_By_Name()
    {
        var recipe = new Recipe(9, 9, 1f, [new PictureStamp("missing", Vector2.Zero, new(4f, 4f), 1f)]);

        Assert.Contains("missing", Assert.Throws<InvalidOperationException>(() => recipe.Build(new Pictures())).Message, StringComparison.Ordinal);
        Assert.Throws<InvalidOperationException>(() => recipe.Build());
        Assert.Throws<ArgumentException>("shares", () => new HeightPicture(2, 2, [0f, 1f, 1f]));
        Assert.Throws<ArgumentOutOfRangeException>("Size", () => new PictureStamp("a", Vector2.Zero, new(0f, 1f), 1f));
    }

    [Fact]
    public void A_Builder_Reads_Its_Pictures_On_Every_Rebuild()
    {
        var pictures = new Pictures(("ridge", Ridge()));
        static Recipe At(float x) => new(41, 21, 1f, [new PictureStamp("ridge", new(x, 0f), new(20f, 10f), 8f)]);
        var builder = new LandscapeBuilder(At(-5f), pictures);
        builder.Rebuild(At(5f));

        Assert.Equal(At(5f).Build(pictures).Heights, builder.Field.Heights);
    }

    // A picture rising from nothing at its first column to one at its last, five rows alike.
    private static HeightPicture Ridge() =>
        new(11, 5, [.. Enumerable.Range(0, 55).Select(i => i % 11 / 10f)]);

    // An engine's pictures by name, as a host keeps them.
    private sealed class Pictures(params (string Name, HeightPicture Picture)[] pictures) : ILandscapePictures
    {
        public HeightPicture? Heights(string name) => pictures.FirstOrDefault(p => p.Name == name).Picture;
    }
}
