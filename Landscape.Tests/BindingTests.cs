using System.Numerics;
using Xunit;

namespace Landscape.Tests;

// The README's smallest binding run against a host with no engine, whose three calls keep what they
// are given, so the build proves the guide a stranger binds the module by, ModuleTests holding the
// README's block to the lines between the markers.
public class BindingTests
{
    [Fact]
    public void The_Smallest_Binding_Leaves_The_Engine_Holding_The_Edited_Field()
    {
        var (recipe, edited) = (Ground(new(-60f, -60f)), Ground(new(-50f, -60f)));
        var engine = new Host();
        Bind(engine, recipe, Rules, edited);

        // A host bound to the edited recipe from nothing holds the same tiles, layers and heights,
        // so the tiles the change reached were all the tiles it changed.
        var fresh = new Host();
        Bind(fresh, edited, Rules, edited);
        Assert.Equal(fresh.Tiles.Keys.Order(), engine.Tiles.Keys.Order());
        foreach (var (place, tile) in fresh.Tiles)
        {
            Assert.Equal(tile.Positions, engine.Tiles[place].Positions);
            Assert.Equal(tile.Normals, engine.Tiles[place].Normals);
            Assert.Equal(tile.Uvs, engine.Tiles[place].Uvs);
            Assert.Equal(tile.Indices, engine.Tiles[place].Indices);
        }

        Assert.Equal(fresh.Layers[0], engine.Layers[0]);
        Assert.Equal(fresh.Heights, engine.Heights);

        // Sixteen tiles made at first, and the cone's edit reaching the four that meet at point 64
        // each way, its old and new places spanning points 60 to 86 along X and 60 to 76 along Z.
        Assert.Equal(16, fresh.TilesMade);
        Assert.Equal(16 + 4, engine.TilesMade);
    }

    private static readonly SplatRule[] Rules =
    [
        new("grass", mostSteep: 0.5f, steepBlend: 0.2f),
        new("rock", leastSteep: 0.7f, steepBlend: 0.2f),
    ];

    private static Recipe Ground(Vector2 cone) => new(257, 257, 1f,
    [
        new Noise(21, 64f, 5, 4f),
        new Stamp(StampShape.Cone, cone, 8f, 6f),
    ]);

    // The README's code, word for word between the markers, which ModuleTests reads back.
    private static void Bind(Host engine, Recipe recipe, SplatRule[] rules, Recipe edited)
    {
        // The README's block from here
        var builder = new LandscapeBuilder(recipe);
        var field = builder.Field;
        foreach (var tile in LandscapeGeometry.Tiles(field))
            engine.MeshTile(tile.Column, tile.Row, tile.Positions, tile.Normals, tile.Uvs, tile.Indices);
        var splat = SplatMap.Of(field, rules);
        for (var layer = 0; layer < splat.Layers.Count; layer++)
            engine.SplatLayer(layer, splat.Columns, splat.Rows, splat.Layers[layer]);
        engine.CollisionHeights(field.Columns, field.Rows, field.Spacing, field.Heights);

        // After an edit, the tiles the change reached are made again and the rest kept, and
        // the splat layers and the heights, which are the whole field's, are given again.
        var change = builder.Rebuild(edited);
        field = builder.Field;
        if (change.Changed is { } area)
        {
            foreach (var (column, row) in area.Tiles(field))
            {
                var tile = LandscapeGeometry.Tile(field, column, row);
                engine.MeshTile(column, row, tile.Positions, tile.Normals, tile.Uvs, tile.Indices);
            }

            splat = SplatMap.Of(field, rules);
            for (var layer = 0; layer < splat.Layers.Count; layer++)
                engine.SplatLayer(layer, splat.Columns, splat.Rows, splat.Layers[layer]);
            engine.CollisionHeights(field.Columns, field.Rows, field.Spacing, field.Heights);
        }
        // The end marker.
    }

    // An engine's three calls, each keeping a copy of what it was given as a renderer and a physics
    // would keep their own.
    private sealed class Host
    {
        public Dictionary<(int Column, int Row), (Vector3[] Positions, Vector3[] Normals, Vector2[] Uvs, uint[] Indices)> Tiles { get; } = [];

        public Dictionary<int, byte[]> Layers { get; } = [];

        public float[] Heights { get; private set; } = [];

        public int TilesMade { get; private set; }

        public void MeshTile(int column, int row, Vector3[] positions, Vector3[] normals, Vector2[] uvs, uint[] indices)
        {
            Tiles[(column, row)] = ([.. positions], [.. normals], [.. uvs], [.. indices]);
            TilesMade++;
        }

        public void SplatLayer(int layer, int width, int height, byte[] rgba)
        {
            Assert.Equal(width * height * 4, rgba.Length);
            Layers[layer] = [.. rgba];
        }

        public void CollisionHeights(int columns, int rows, float spacing, float[] heights)
        {
            Assert.Equal(columns * rows, heights.Length);
            Assert.True(spacing > 0f);
            Heights = [.. heights];
        }
    }
}
