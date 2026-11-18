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
    /// <param name="step">
    /// How many points apart a tile's corners stand, a power of two dividing the tile's cells, one
    /// for every point and more for a tile drawn far off.
    /// </param>
    /// <param name="skirt">
    /// How far in meters a ring of corners hangs below each tile's edge, nothing for none, as
    /// <see cref="SkirtDepth"/> reckons it for tiles drawn at different steps.
    /// </param>
    /// <param name="options">
    /// The threads the tiles may be made on, one where none are given.
    /// </param>
    /// <returns>The tiles a row at a time, each row along X and the rows along Z.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The tile's cells are fewer than one, the step is not a power of two dividing them, or the
    /// skirt is below nothing or not finite.
    /// </exception>
    public static IReadOnlyList<LandscapeMesh> Tiles(Heightfield field, int tileCells = TileCells, int step = 1, float skirt = 0f, BuildOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(field);
        Check(tileCells, step, skirt);
        var across = (field.Columns - 2) / tileCells + 1;
        var along = (field.Rows - 2) / tileCells + 1;
        var tiles = new LandscapeMesh[across * along];
        (options ?? BuildOptions.Default).Rows(0, across * along - 1, i => tiles[i] = Tile(field, i % across, i / across, tileCells, step, skirt));

        return tiles;
    }

    /// <summary>
    /// One tile of a field, the cells from its column and row times the tile's side.
    /// </summary>
    /// <param name="field">The field.</param>
    /// <param name="column">The tile's column among the field's tiles, from zero along X.</param>
    /// <param name="row">The tile's row among the field's tiles, from zero along Z.</param>
    /// <param name="tileCells">How many cells a tile's side spans.</param>
    /// <param name="step">
    /// How many points apart its corners stand, a power of two dividing the tile's cells.
    /// </param>
    /// <param name="skirt">
    /// How far in meters a ring of corners hangs below its edge, nothing for none.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The tile is not one of the field's, the tile's cells are fewer than one, the step is not a
    /// power of two dividing them, or the skirt is below nothing or not finite.
    /// </exception>
    /// <remarks>
    /// <para>
    /// A tile at a step has a corner at every step-th point from its first, and at its last point
    /// where the field's last tile is cut short, so its corners are the field's own points with
    /// their own normals and texture coordinates, and two tiles at different steps share the
    /// corners of the coarser along their common edge to the bit.
    /// </para>
    /// <para>
    /// The skirt is a corner below each edge corner, lowered by the skirt and keeping the corner's
    /// normal and texture coordinates, and two triangles a segment of the edge, facing out of the
    /// tile. Its corners follow the surface's in the order of the edges at the least Z, the
    /// greatest Z, the least X and the greatest X, each along its own axis, and its triangles
    /// follow the surface's.
    /// </para>
    /// </remarks>
    public static LandscapeMesh Tile(Heightfield field, int column, int row, int tileCells = TileCells, int step = 1, float skirt = 0f)
    {
        ArgumentNullException.ThrowIfNull(field);
        Check(tileCells, step, skirt);
        var (firstColumn, firstRow) = (column * tileCells, row * tileCells);
        if (column < 0 || firstColumn >= field.Columns - 1) throw new ArgumentOutOfRangeException(nameof(column), column, "The tile's column is one of the field's.");
        if (row < 0 || firstRow >= field.Rows - 1) throw new ArgumentOutOfRangeException(nameof(row), row, "The tile's row is one of the field's.");

        var xs = Offsets(Math.Min(tileCells, field.Columns - 1 - firstColumn), step);
        var zs = Offsets(Math.Min(tileCells, field.Rows - 1 - firstRow), step);
        var (width, depth) = (xs.Length, zs.Length);
        var skirted = skirt > 0f;
        var count = width * depth + (skirted ? 2 * width + 2 * depth : 0);
        var positions = new Vector3[count];
        var normals = new Vector3[count];
        var uvs = new Vector2[count];
        var next = 0;
        void Corner(int x, int z, float lowered)
        {
            var (pointColumn, pointRow) = (firstColumn + x, firstRow + z);
            var place = field.Place(pointColumn, pointRow);
            positions[next] = new Vector3(place.X, field[pointColumn, pointRow] - lowered, place.Y);
            normals[next] = field.Normal(pointColumn, pointRow);
            uvs[next] = new Vector2((float)pointColumn / (field.Columns - 1), (float)pointRow / (field.Rows - 1));
            next++;
        }

        foreach (var z in zs)
        {
            foreach (var x in xs) Corner(x, z, 0f);
        }

        if (skirted)
        {
            foreach (var x in xs) Corner(x, zs[0], skirt);
            foreach (var x in xs) Corner(x, zs[^1], skirt);
            foreach (var z in zs) Corner(xs[0], z, skirt);
            foreach (var z in zs) Corner(xs[^1], z, skirt);
        }

        return new LandscapeMesh(column, row, positions, normals, uvs, Holed(field, firstColumn, firstRow, xs, zs, Indices(width, depth, skirted)));
    }

    // A tile's indices with the triangles of each of its cells that holds a hole's cell left out,
    // or the shared array where it holds none. The surface's triangles come first, six a cell a row
    // of cells at a time, and the skirt's after them.
    private static uint[] Holed(Heightfield field, int firstColumn, int firstRow, int[] xs, int[] zs, uint[] shared)
    {
        if (field.Holes is null) return shared;
        var (across, along) = (xs.Length - 1, zs.Length - 1);
        var left = new bool[across * along];
        var any = false;
        for (var j = 0; j < along; j++)
        {
            for (var i = 0; i < across; i++)
            {
                for (var z = firstRow + zs[j]; z < firstRow + zs[j + 1] && !left[j * across + i]; z++)
                {
                    for (var x = firstColumn + xs[i]; x < firstColumn + xs[i + 1]; x++)
                    {
                        if (!field.IsHole(x, z)) continue;
                        any = left[j * across + i] = true;
                        break;
                    }
                }
            }
        }

        if (!any) return shared;
        var kept = new List<uint>(shared.Length);
        for (var cell = 0; cell < left.Length; cell++)
        {
            if (!left[cell]) kept.AddRange(shared.AsSpan(cell * 6, 6));
        }

        kept.AddRange(shared.AsSpan(left.Length * 6));
        return [.. kept];
    }

    /// <summary>
    /// How far a skirt hangs so tiles drawn at any steps up to the coarsest meet with no crack.
    /// </summary>
    /// <param name="field">The field.</param>
    /// <param name="tileCells">How many cells a tile's side spans.</param>
    /// <param name="coarsest">
    /// The coarsest step an engine draws a tile at, a power of two dividing the tile's cells.
    /// </param>
    /// <returns>
    /// The skirt's depth in meters, nothing where every step lies on the field's points.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The tile's cells are fewer than one, or the coarsest step is not a power of two dividing
    /// them.
    /// </exception>
    /// <remarks>
    /// Along every tile's edge a tile at a step is a straight line between every step-th point,
    /// and a point between stands off that line by some height. Two tiles at different steps stand
    /// apart along their common edge by at most the sum of their two lines' heights off the points,
    /// so the depth is twice the most any step up to the coarsest stands off any edge, and a
    /// thousandth of a meter more for the rounding of the lines.
    /// </remarks>
    public static float SkirtDepth(Heightfield field, int tileCells = TileCells, int coarsest = 1)
    {
        ArgumentNullException.ThrowIfNull(field);
        Check(tileCells, coarsest, 0f, nameof(coarsest));
        var most = 0f;
        for (var step = 2; step <= coarsest; step *= 2)
        {
            // The edges along Z, at every tile's first column and the field's last, then along X.
            for (var column = 0; column < field.Columns; column = column + tileCells < field.Columns - 1 ? column + tileCells : column == field.Columns - 1 ? field.Columns : field.Columns - 1)
                most = MathF.Max(most, OffTheLine(field.Rows, tileCells, step, z => field[column, z]));
            for (var row = 0; row < field.Rows; row = row + tileCells < field.Rows - 1 ? row + tileCells : row == field.Rows - 1 ? field.Rows : field.Rows - 1)
                most = MathF.Max(most, OffTheLine(field.Columns, tileCells, step, x => field[x, row]));
        }

        return most == 0f ? 0f : 2f * most + 0.001f;
    }

    /// <summary>
    /// The most in meters a tile drawn at a step stands off the field's points it covers, which a
    /// binding weighs against the distance or the error on screen it accepts to pick the step.
    /// </summary>
    /// <param name="field">The field.</param>
    /// <param name="column">The tile's column among the field's tiles, from zero along X.</param>
    /// <param name="row">The tile's row among the field's tiles, from zero along Z.</param>
    /// <param name="step">The step, a power of two dividing the tile's cells.</param>
    /// <param name="tileCells">How many cells a tile's side spans.</param>
    /// <returns>The error in meters, nothing at a step of one.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The tile is not one of the field's, or the step is not a power of two dividing its cells.
    /// </exception>
    /// <remarks>
    /// Each point is read against the triangle of the coarse cell it falls in, the cell's
    /// near-left, far-left and near-right corners or its near-right, far-left and far-right, as the
    /// tile draws it, so the error is the height an engine sees the ground move by when the tile
    /// changes to the step.
    /// </remarks>
    public static float StepError(Heightfield field, int column, int row, int step, int tileCells = TileCells)
    {
        ArgumentNullException.ThrowIfNull(field);
        Check(tileCells, step, 0f);
        var (firstColumn, firstRow) = (column * tileCells, row * tileCells);
        if (column < 0 || firstColumn >= field.Columns - 1) throw new ArgumentOutOfRangeException(nameof(column), column, "The tile's column is one of the field's.");
        if (row < 0 || firstRow >= field.Rows - 1) throw new ArgumentOutOfRangeException(nameof(row), row, "The tile's row is one of the field's.");
        // A tile at a step of one draws every point, which the arithmetic below would read back
        // off its own corners by a rounding.
        if (step == 1) return 0f;
        var xs = Offsets(Math.Min(tileCells, field.Columns - 1 - firstColumn), step);
        var zs = Offsets(Math.Min(tileCells, field.Rows - 1 - firstRow), step);
        var most = 0f;
        for (var j = 0; j + 1 < zs.Length; j++)
        {
            for (var i = 0; i + 1 < xs.Length; i++)
            {
                var (x0, x1, z0, z1) = (firstColumn + xs[i], firstColumn + xs[i + 1], firstRow + zs[j], firstRow + zs[j + 1]);
                var (nearLeft, nearRight, farLeft, farRight) = (field[x0, z0], field[x1, z0], field[x0, z1], field[x1, z1]);
                for (var z = z0; z <= z1; z++)
                {
                    for (var x = x0; x <= x1; x++)
                    {
                        var (u, v) = ((float)(x - x0) / (x1 - x0), (float)(z - z0) / (z1 - z0));
                        var drawn = u + v <= 1f
                            ? nearLeft + (nearRight - nearLeft) * u + (farLeft - nearLeft) * v
                            : farRight + (farLeft - farRight) * (1f - u) + (nearRight - farRight) * (1f - v);
                        most = MathF.Max(most, MathF.Abs(field[x, z] - drawn));
                    }
                }
            }
        }

        return most;
    }

    // The most an edge's points stand off the straight lines between every step-th point of each
    // tile along it, the last of a tile cut short at the field's end.
    private static float OffTheLine(int points, int tileCells, int step, Func<int, float> height)
    {
        var most = 0f;
        for (var first = 0; first < points - 1; first += tileCells)
        {
            var offsets = Offsets(Math.Min(tileCells, points - 1 - first), step);
            for (var k = 0; k + 1 < offsets.Length; k++)
            {
                var (from, to) = (first + offsets[k], first + offsets[k + 1]);
                var (low, high) = (height(from), height(to));
                for (var p = from + 1; p < to; p++)
                {
                    var line = low + (high - low) * ((float)(p - from) / (to - from));
                    most = MathF.Max(most, MathF.Abs(height(p) - line));
                }
            }
        }

        return most;
    }

    // The offsets of a tile's corners from its first point, every step-th and its last.
    private static int[] Offsets(int cells, int step)
    {
        var offsets = new List<int>();
        for (var offset = 0; offset < cells; offset += step) offsets.Add(offset);
        offsets.Add(cells);
        return [.. offsets];
    }

    private static void Check(int tileCells, int step, float skirt, string stepName = "step")
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(tileCells, 1);
        if (step < 1 || (step & (step - 1)) != 0 || step > tileCells || tileCells % step != 0)
            throw new ArgumentOutOfRangeException(stepName, step, $"A step is a power of two dividing a tile's {tileCells} cells.");
        if (!(skirt >= 0f) || !float.IsFinite(skirt))
            throw new ArgumentOutOfRangeException(nameof(skirt), skirt, "A skirt is a finite number of meters at or above nothing.");
    }

    // One array of indices for every tile of a width and a depth in corners, with or without a
    // skirt, made once and shared, since every such tile joins its corners in the same order and a
    // field of 4,097 points a side otherwise allocated 4,096 copies of it, 24 bytes a point of its
    // tiles' 57.
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<(int Width, int Depth, bool Skirted), uint[]> SharedIndices = new();

    private static uint[] Indices(int width, int depth, bool skirted) => SharedIndices.GetOrAdd((width, depth, skirted), static size =>
    {
        var (width, depth, skirted) = size;
        var indices = new List<uint>((width - 1) * (depth - 1) * 6 + (skirted ? 12 * (width + depth) : 0));

        // Each cell's two triangles, its near corners first. Seen from above with X to the right, Z
        // runs down the picture, and near-left, far-left, near-right turns counter-clockwise.
        for (var z = 0; z < depth - 1; z++)
        {
            for (var x = 0; x < width - 1; x++)
            {
                var nearLeft = (uint)(z * width + x);
                var nearRight = nearLeft + 1;
                var farLeft = nearLeft + (uint)width;
                var farRight = farLeft + 1;
                indices.AddRange([nearLeft, farLeft, nearRight, nearRight, farLeft, farRight]);
            }
        }

        if (!skirted) return [.. indices];

        // Each edge's corners and the skirt's below them, the edge along X at the least Z and
        // the greatest, then along Z at the least X and the greatest. Taken from one edge corner to
        // the next and down, a segment's triangles face the way the edge runs turned a quarter
        // clockwise seen from above, out of the tile at the least Z and the greatest X and into it
        // at the other two, which take their corners the other way round.
        var skirtStart = (uint)(width * depth);
        void Edge(Func<int, uint> surface, uint below, int corners, bool outward)
        {
            for (var k = 0; k + 1 < corners; k++)
            {
                var (e0, e1, s0, s1) = (surface(k), surface(k + 1), below + (uint)k, below + (uint)k + 1);
                if (outward) indices.AddRange([e0, e1, s0, e1, s1, s0]);
                else indices.AddRange([e0, s0, e1, e1, s0, s1]);
            }
        }

        Edge(k => (uint)k, skirtStart, width, true);
        Edge(k => (uint)((depth - 1) * width + k), skirtStart + (uint)width, width, false);
        Edge(k => (uint)(k * width), skirtStart + 2u * (uint)width, depth, false);
        Edge(k => (uint)(k * width + width - 1), skirtStart + 2u * (uint)width + (uint)depth, depth, true);
        return [.. indices];
    });
}
