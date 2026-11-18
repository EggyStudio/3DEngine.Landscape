using System.Numerics;
using Xunit;

namespace Landscape.Tests;

// Tiles an engine culls by their bounds and draws far off at a coarser step, with skirts hung below
// their edges so two tiles at different steps meet with no crack.
public class DistanceTests
{
    private static Heightfield Ground() => new Recipe(257, 257, 1f,
    [
        new Noise(4, 40f, 6, 6f),
        new Stamp(StampShape.Cone, new(-20f, 10f), 50f, 30f),
    ]).Build();

    [Theory]
    [InlineData(1, 65)]
    [InlineData(2, 33)]
    [InlineData(8, 9)]
    [InlineData(64, 2)]
    public void A_Tile_At_A_Step_Has_A_Corner_Every_Step_Points(int step, int side)
    {
        var tile = LandscapeGeometry.Tile(Ground(), 1, 2, step: step);

        Assert.Equal(side * side, tile.Positions.Length);
        Assert.Equal((side - 1) * (side - 1) * 6, tile.Indices.Length);
        Assert.Equal(new Vector2(64f - 128f, 128f - 128f), new Vector2(tile.Positions[0].X, tile.Positions[0].Z));
        Assert.Equal(new Vector2(128f - 128f, 192f - 128f), new Vector2(tile.Positions[^1].X, tile.Positions[^1].Z));
    }

    [Fact]
    public void A_Coarse_Tiles_Corners_Are_The_Fields_Own_Points()
    {
        var field = Ground();
        var fine = LandscapeGeometry.Tile(field, 1, 1);
        var coarse = LandscapeGeometry.Tile(field, 1, 1, step: 8);

        for (var z = 0; z < 9; z++)
        {
            for (var x = 0; x < 9; x++)
            {
                Assert.Equal(fine.Positions[z * 8 * 65 + x * 8], coarse.Positions[z * 9 + x]);
                Assert.Equal(fine.Normals[z * 8 * 65 + x * 8], coarse.Normals[z * 9 + x]);
                Assert.Equal(fine.Uvs[z * 8 * 65 + x * 8], coarse.Uvs[z * 9 + x]);
            }
        }
    }

    [Fact]
    public void A_Tiles_Bounds_Hold_Every_Corner_And_Touch_Them()
    {
        var field = Ground();
        foreach (var tile in LandscapeGeometry.Tiles(field, step: 4, skirt: LandscapeGeometry.SkirtDepth(field, coarsest: 4)))
        {
            var (low, high) = tile.Bounds;
            Assert.All(tile.Positions, p => Assert.True(Vector3.Min(p, low) == low && Vector3.Max(p, high) == high));
            Assert.Equal(low, tile.Positions.Aggregate(Vector3.Min));
            Assert.Equal(high, tile.Positions.Aggregate(Vector3.Max));
        }
    }

    [Fact]
    public void Two_Tiles_At_Steps_1_And_8_Meet_With_No_Crack()
    {
        // Along the edge at point 128 between tiles 1 and 2 of the first row, the fine tile's edge
        // follows every point and the coarse tile's every eighth, and each skirt hangs far enough
        // below its own edge to reach the other's, so no sky shows between them.
        var field = Ground();
        var skirt = LandscapeGeometry.SkirtDepth(field, coarsest: 8);
        var fine = LandscapeGeometry.Tile(field, 1, 0, step: 1, skirt: skirt);
        var coarse = LandscapeGeometry.Tile(field, 2, 0, step: 8, skirt: skirt);

        Assert.True(skirt > 0f);
        for (var z = 0; z <= 64; z++)
        {
            var fineHeight = fine.Positions[z * 65 + 64].Y;
            var (below, above) = (coarse.Positions[z / 8 * 9].Y, coarse.Positions[Math.Min(z / 8 + 1, 8) * 9].Y);
            var coarseHeight = below + (above - below) * (z % 8 / 8f);
            Assert.True(fineHeight - skirt <= coarseHeight && coarseHeight - skirt <= fineHeight, $"At {z} the edges stand {fineHeight} and {coarseHeight} with a skirt of {skirt}.");
        }
    }

    [Fact]
    public void A_Skirts_Triangles_Face_Out_Of_The_Tile_And_Its_Surface_Up()
    {
        var field = Ground();
        var tile = LandscapeGeometry.Tile(field, 1, 1, step: 4, skirt: 2f);
        var middle = (tile.Bounds.Low + tile.Bounds.High) * 0.5f;
        var surface = 16 * 16 * 6;

        Assert.Equal(17 * 17 + 4 * 17, tile.Positions.Length);
        Assert.Equal(surface + 4 * 16 * 6, tile.Indices.Length);
        for (var i = 0; i < tile.Indices.Length; i += 3)
        {
            var (a, b, c) = (tile.Positions[tile.Indices[i]], tile.Positions[tile.Indices[i + 1]], tile.Positions[tile.Indices[i + 2]]);
            var facing = Vector3.Cross(b - a, c - a);
            if (i < surface) Assert.True(facing.Y > 0f, $"Surface triangle {i / 3} faces down.");
            else
            {
                // Out of the tile is the way across the ground from its middle to the triangle's.
                var centroid = (a + b + c) / 3f;
                var outward = new Vector3(centroid.X - middle.X, 0f, centroid.Z - middle.Z);
                Assert.True(Vector3.Dot(facing, outward) > 0f, $"Skirt triangle {(i - surface) / 3} faces in.");
            }
        }
    }

    [Fact]
    public void Tiles_Of_One_Size_Step_And_Skirt_Share_Their_Indices()
    {
        var tiles = LandscapeGeometry.Tiles(Ground(), step: 4, skirt: 1f);

        Assert.Same(tiles[0].Indices, tiles[^1].Indices);
        Assert.NotSame(tiles[0].Indices, LandscapeGeometry.Tile(Ground(), 0, 0, step: 4).Indices);
    }

    [Fact]
    public void A_Steps_Error_Is_The_Most_Its_Triangles_Stand_Off_The_Points()
    {
        var field = Ground();
        var coarse = LandscapeGeometry.Tile(field, 1, 1, step: 8);
        var error = LandscapeGeometry.StepError(field, 1, 1, 8);

        // Every point of the tile against the coarse triangles it falls under, read by a ray down.
        var most = 0f;
        for (var z = 64; z <= 128; z++)
        {
            for (var x = 64; x <= 128; x++)
            {
                var place = field.Place(x, z);
                most = MathF.Max(most, MathF.Abs(field[x, z] - Drawn(coarse, place)));
            }
        }

        Assert.Equal(most, error, 1e-4f);
        Assert.True(error > 0f);
        Assert.Equal(0f, LandscapeGeometry.StepError(field, 1, 1, 1));
        Assert.True(LandscapeGeometry.StepError(field, 1, 1, 16) >= LandscapeGeometry.StepError(field, 1, 1, 2));
    }

    // The height of a tile's drawn surface over a place, from the surface triangle beneath it.
    private static float Drawn(LandscapeMesh tile, Vector2 place)
    {
        for (var i = 0; i < tile.Indices.Length; i += 3)
        {
            var (a, b, c) = (tile.Positions[tile.Indices[i]], tile.Positions[tile.Indices[i + 1]], tile.Positions[tile.Indices[i + 2]]);
            float Edge(Vector3 p, Vector3 q) => (q.X - p.X) * (place.Y - p.Z) - (q.Z - p.Z) * (place.X - p.X);
            var area = Edge(a, b) is var ab && Edge(b, c) is var bc && Edge(c, a) is var ca ? (ab, bc, ca) : default;
            var whole = (b.X - a.X) * (c.Z - a.Z) - (b.Z - a.Z) * (c.X - a.X);
            if (whole == 0f) continue;
            var (wa, wb, wc) = (area.bc / whole, area.ca / whole, area.ab / whole);
            if (wa < -1e-5f || wb < -1e-5f || wc < -1e-5f) continue;
            return wa * a.Y + wb * b.Y + wc * c.Y;
        }

        throw new InvalidOperationException($"No triangle lies under {place}.");
    }

    [Fact]
    public void A_Step_That_Does_Not_Divide_The_Tile_Or_A_Skirt_Below_Nothing_Is_Refused()
    {
        var field = Ground();

        Assert.Throws<ArgumentOutOfRangeException>("step", () => LandscapeGeometry.Tile(field, 0, 0, step: 3));
        Assert.Throws<ArgumentOutOfRangeException>("step", () => LandscapeGeometry.Tile(field, 0, 0, step: 128));
        Assert.Throws<ArgumentOutOfRangeException>("skirt", () => LandscapeGeometry.Tile(field, 0, 0, skirt: -1f));
        Assert.Throws<ArgumentOutOfRangeException>("coarsest", () => LandscapeGeometry.SkirtDepth(field, coarsest: 6));
    }
}
