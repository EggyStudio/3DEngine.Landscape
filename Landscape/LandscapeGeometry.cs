using System.Numerics;

namespace Landscape;

/// <summary>
/// A field as the triangles an engine draws, cut into square tiles so an engine culls them apart
/// and builds again only those a change reaches.
/// </summary>
/// <remarks>
/// The module gives out arrays and not an engine's types, and the engine that binds it builds its
/// own meshes from them (N 2.1).
/// </remarks>
public static class LandscapeGeometry
{
    /// <summary>
    /// The number of cells along a tile's side an engine takes where it has no other.
    /// </summary>
    /// <remarks>
    /// A tile of 64 cells has 65 by 65 corners, 4,225, under the 65,536 a 16-bit index reaches, so
    /// an engine may draw it with short indices.
    /// </remarks>
    public const int TileCells = 64;

    /// <summary>
    /// Cuts a field into tiles, the last of a row and a column cut to what is left.
    /// </summary>
    /// <param name="field">The field.</param>
    /// <param name="tileCells">How many cells a tile's side spans.</param>
    /// <returns>The tiles a row at a time, each row along X and the rows along Z.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The tile's cells are fewer than one.
    /// </exception>
    public static IReadOnlyList<LandscapeMesh> Tiles(Heightfield field, int tileCells = TileCells)
    {
        ArgumentNullException.ThrowIfNull(field);
        ArgumentOutOfRangeException.ThrowIfLessThan(tileCells, 1);
        var across = (field.Columns - 2) / tileCells + 1;
        var along = (field.Rows - 2) / tileCells + 1;
        var tiles = new LandscapeMesh[across * along];
        for (var row = 0; row < along; row++)
        {
            for (var column = 0; column < across; column++)
                tiles[row * across + column] = Tile(field, column, row, tileCells);
        }

        return tiles;
    }

    /// <summary>
    /// One tile of a field, the cells from its column and row times the tile's side.
    /// </summary>
    /// <param name="field">The field.</param>
    /// <param name="column">The tile's column among the field's tiles, from zero along X.</param>
    /// <param name="row">The tile's row among the field's tiles, from zero along Z.</param>
    /// <param name="tileCells">How many cells a tile's side spans.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The tile is not one of the field's, or the tile's cells are fewer than one.
    /// </exception>
    public static LandscapeMesh Tile(Heightfield field, int column, int row, int tileCells = TileCells)
    {
        ArgumentNullException.ThrowIfNull(field);
        ArgumentOutOfRangeException.ThrowIfLessThan(tileCells, 1);
        var (firstColumn, firstRow) = (column * tileCells, row * tileCells);
        if (column < 0 || firstColumn >= field.Columns - 1) throw new ArgumentOutOfRangeException(nameof(column), column, "The tile's column is one of the field's.");
        if (row < 0 || firstRow >= field.Rows - 1) throw new ArgumentOutOfRangeException(nameof(row), row, "The tile's row is one of the field's.");

        var width = Math.Min(tileCells, field.Columns - 1 - firstColumn) + 1;
        var depth = Math.Min(tileCells, field.Rows - 1 - firstRow) + 1;
        var positions = new Vector3[width * depth];
        var normals = new Vector3[width * depth];
        var uvs = new Vector2[width * depth];
        for (var z = 0; z < depth; z++)
        {
            for (var x = 0; x < width; x++)
            {
                var (pointColumn, pointRow) = (firstColumn + x, firstRow + z);
                var place = field.Place(pointColumn, pointRow);
                var corner = z * width + x;
                positions[corner] = new Vector3(place.X, field[pointColumn, pointRow], place.Y);
                normals[corner] = field.Normal(pointColumn, pointRow);
                uvs[corner] = new Vector2((float)pointColumn / (field.Columns - 1), (float)pointRow / (field.Rows - 1));
            }
        }

        return new LandscapeMesh(column, row, positions, normals, uvs, Indices(width, depth));
    }

    // One array of indices for every tile of a width and a depth in corners, made once and shared,
    // since every such tile joins its corners in the same order and a field of 4,097 points a side
    // otherwise allocated 4,096 copies of it, 24 bytes a point of its tiles' 57.
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<(int Width, int Depth), uint[]> SharedIndices = new();

    private static uint[] Indices(int width, int depth) => SharedIndices.GetOrAdd((width, depth), static size =>
    {
        var (width, depth) = size;

        // Each cell's two triangles, its near corners first. Seen from above with X to the right, Z
        // runs down the picture, and near-left, far-left, near-right turns counter-clockwise.
        var indices = new uint[(width - 1) * (depth - 1) * 6];
        var next = 0;
        for (var z = 0; z < depth - 1; z++)
        {
            for (var x = 0; x < width - 1; x++)
            {
                var nearLeft = (uint)(z * width + x);
                var nearRight = nearLeft + 1;
                var farLeft = nearLeft + (uint)width;
                var farRight = farLeft + 1;
                indices[next++] = nearLeft;
                indices[next++] = farLeft;
                indices[next++] = nearRight;
                indices[next++] = nearRight;
                indices[next++] = farLeft;
                indices[next++] = farRight;
            }
        }

        return indices;
    });
}
