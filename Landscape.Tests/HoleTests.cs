using System.Numerics;
using System.Text;
using Xunit;

namespace Landscape.Tests;

// Holes in the ground, cells no tile draws and no ray meets, for a cave's or a tunnel's mouth an
// engine fills with a mesh of its own.
public class HoleTests
{
    private static Recipe Ground(params LandscapeStep[] more) => new(129, 129, 1f, [new Noise(3, 20f, 4, 3f), .. more]);

    [Fact]
    public void A_Hole_Marks_The_Cells_Whose_Middles_Lie_Within_It_And_Keeps_The_Heights()
    {
        var plain = Ground().Build();
        var holed = Ground(new Hole(new(10.5f, -4.5f), 3f)).Build();

        Assert.Equal(plain.Heights, holed.Heights);
        Assert.Null(plain.Holes);
        Assert.NotNull(holed.Holes);
        Assert.Equal(128 * 128, holed.Holes!.Length);
        for (var row = 0; row < 128; row++)
        {
            for (var column = 0; column < 128; column++)
            {
                var middle = (holed.Place(column, row) + holed.Place(column + 1, row + 1)) * 0.5f;
                Assert.Equal(Vector2.Distance(middle, new(10.5f, -4.5f)) < 3f, holed.IsHole(column, row));
            }
        }

        // The cells about cell 74, 59, whose middle is the hole's, with offsets whose squares sum
        // under nine, 25 of them.
        Assert.Equal(25, holed.Holes.Count(hole => hole));
    }

    [Fact]
    public void A_Tile_Draws_No_Triangle_Over_A_Hole_And_Tiles_Without_One_Share_Their_Indices()
    {
        var field = Ground(new Hole(new(10.5f, -4.5f), 3f)).Build();
        var tiles = LandscapeGeometry.Tiles(field);
        var holed = tiles.Single(tile => tile.Indices.Length < 64 * 64 * 6);

        Assert.Equal(64 * 64 * 6 - 25 * 6, holed.Indices.Length);
        for (var i = 0; i < holed.Indices.Length; i += 3)
        {
            var middle = (holed.Positions[holed.Indices[i]] + holed.Positions[holed.Indices[i + 1]] + holed.Positions[holed.Indices[i + 2]]) / 3f;
            var (column, row) = ((int)MathF.Floor(middle.X + 64f), (int)MathF.Floor(middle.Z + 64f));
            Assert.False(field.IsHole(column, row), $"A triangle covers the hole's cell {column}, {row}.");
        }

        var whole = tiles.Where(tile => tile != holed).ToArray();
        Assert.All(whole, tile => Assert.Same(whole[0].Indices, tile.Indices));
        Assert.NotSame(whole[0].Indices, holed.Indices);
    }

    [Fact]
    public void A_Coarse_Tile_Leaves_Out_Every_Cell_Over_Any_Hole()
    {
        var field = Ground(new Hole(new(10.5f, -4.5f), 1f)).Build();
        var coarse = LandscapeGeometry.Tile(field, 1, 0, step: 8);

        // The hole's cells lie in one coarse cell of 8 by 8, which the coarse tile leaves out.
        Assert.Equal(8 * 8 * 6 - 6, coarse.Indices.Length);
    }

    [Fact]
    public void A_Ray_Passes_Through_A_Hole_To_What_Lies_Beyond()
    {
        var field = Ground(new Hole(new(10.5f, -4.5f), 3f)).Build();

        Assert.Null(field.Raycast(new Vector3(10.5f, 50f, -4.5f), -Vector3.UnitY));
        Assert.NotNull(field.Raycast(new Vector3(20.5f, 50f, -4.5f), -Vector3.UnitY));
    }

    [Fact]
    public void A_Rebuild_That_Opens_A_Hole_Reaches_Its_Cells()
    {
        var builder = new LandscapeBuilder(Ground());
        var change = builder.Rebuild(Ground(new Hole(new(10.5f, -4.5f), 3f)));

        // The hole's cells run from 72 to 76 along X and 57 to 61 along Z, their corners one more.
        Assert.Equal(new LandscapeArea(72, 57, 77, 62), change.Changed);
        Assert.True(builder.Field.IsHole(74, 59));
    }

    [Fact]
    public void A_Hole_Is_Written_And_Read_By_Its_Kind()
    {
        var recipe = Ground(new Hole(new(10.5f, -4.5f), 3f));
        using var stream = new MemoryStream();
        recipe.Write(stream);

        Assert.Contains("\"kind\": \"hole\"", Encoding.UTF8.GetString(stream.ToArray()), StringComparison.Ordinal);
        Assert.Equal(recipe, Recipe.Read(new MemoryStream(stream.ToArray())));
    }

    [Fact]
    public void A_Hole_Of_No_Radius_Is_Refused()
    {
        Assert.Throws<ArgumentOutOfRangeException>("Radius", () => new Hole(Vector2.Zero, 0f));
        Assert.Throws<ArgumentOutOfRangeException>("Place", () => new Hole(new(float.NaN, 0f), 1f));
    }
}
