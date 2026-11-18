using System.Numerics;
using Xunit;

namespace Landscape.Tests;

public class GeometryTests
{
    [Fact]
    public void A_Field_Of_129_Points_Makes_Four_Tiles_Of_65_By_65_Corners()
    {
        var tiles = LandscapeGeometry.Tiles(Fields.ThreeStamps().Build());

        Assert.Equal([(0, 0), (1, 0), (0, 1), (1, 1)], tiles.Select(tile => (tile.Column, tile.Row)));
        Assert.All(tiles, tile =>
        {
            Assert.Equal(65 * 65, tile.Positions.Length);
            Assert.Equal(65 * 65, tile.Normals.Length);
            Assert.Equal(65 * 65, tile.Uvs.Length);
            Assert.Equal(64 * 64 * 6, tile.Indices.Length);
        });
    }

    [Fact]
    public void The_Last_Tile_Of_A_Row_And_A_Column_Is_Cut_To_What_Is_Left()
    {
        var tiles = LandscapeGeometry.Tiles(new Heightfield(10, 5, 1f), 4);

        Assert.Equal(3 * 1, tiles.Count);
        Assert.Equal(2 * 5, tiles[2].Positions.Length);
        Assert.Equal(1 * 4 * 6, tiles[2].Indices.Length);
    }

    [Fact]
    public void Two_Tiles_Give_Their_Shared_Edge_The_Same_Positions_And_Normals()
    {
        var tiles = LandscapeGeometry.Tiles(Fields.ThreeStamps().Build());

        for (var i = 0; i < 65; i++)
        {
            // Along X, the left tile's last column against the right tile's first.
            Assert.Equal(tiles[0].Positions[i * 65 + 64], tiles[1].Positions[i * 65]);
            Assert.Equal(tiles[0].Normals[i * 65 + 64], tiles[1].Normals[i * 65]);

            // Along Z, the near tile's last row against the far tile's first.
            Assert.Equal(tiles[1].Positions[64 * 65 + i], tiles[3].Positions[i]);
            Assert.Equal(tiles[1].Normals[64 * 65 + i], tiles[3].Normals[i]);
        }
    }

    [Fact]
    public void Every_Triangle_Runs_Counter_Clockwise_Seen_From_Above()
    {
        foreach (var tile in LandscapeGeometry.Tiles(Fields.ThreeStamps().Build(), 16))
        {
            for (var i = 0; i < tile.Indices.Length; i += 3)
            {
                var (a, b, c) = (tile.Positions[tile.Indices[i]], tile.Positions[tile.Indices[i + 1]], tile.Positions[tile.Indices[i + 2]]);
                Assert.True(Vector3.Cross(b - a, c - a).Y > 0f, $"Triangle {i / 3} of tile {tile.Column}, {tile.Row} turns clockwise.");
            }
        }
    }

    [Fact]
    public void The_Corners_Are_The_Fields_Points_With_Texture_Coordinates_Over_The_Whole_Field()
    {
        var field = Fields.ThreeStamps().Build();
        var tiles = LandscapeGeometry.Tiles(field);

        Assert.Equal(new Vector3(-64f, field[0, 0], -64f), tiles[0].Positions[0]);
        Assert.Equal(new Vector3(0f, field[64, 64], 0f), tiles[0].Positions[^1]);
        Assert.Equal(new Vector3(64f, field[128, 128], 64f), tiles[3].Positions[^1]);
        Assert.Equal(field.Normal(70, 3), tiles[1].Normals[3 * 65 + 6]);
        Assert.Equal(Vector2.Zero, tiles[0].Uvs[0]);
        Assert.Equal(new Vector2(0.5f, 0.5f), tiles[0].Uvs[^1]);
        Assert.Equal(Vector2.One, tiles[3].Uvs[^1]);
    }

    [Fact]
    public void A_Tile_Refuses_What_The_Field_Does_Not_Have()
    {
        var field = new Heightfield(129, 129, 1f);

        Assert.Throws<ArgumentOutOfRangeException>("column", () => LandscapeGeometry.Tile(field, 2, 0));
        Assert.Throws<ArgumentOutOfRangeException>("row", () => LandscapeGeometry.Tile(field, 0, -1));
        Assert.Throws<ArgumentOutOfRangeException>("tileCells", () => LandscapeGeometry.Tiles(field, 0));
    }
}
