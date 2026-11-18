using System.Numerics;

namespace Landscape;

/// <summary>
/// One tile of a field as triangles, in meters with Y up about the field's middle, which an engine
/// builds a mesh of.
/// </summary>
/// <remarks>
/// <para>
/// Neighboring tiles share the corners of their common edge, each tile holding its own copy, and
/// give them the same positions and normals, since both are the field's at that point, so the tiles
/// meet with no crack and no seam of light. The texture coordinates run over the whole field rather
/// than a tile, so a splat map or a picture laid over the field lines up across the tiles. The
/// winding is counter-clockwise seen from above, the front faces of an engine with Y up and
/// right-handed axes, and an engine that culls the other side takes each triangle's corners in the
/// other order.
/// </para>
/// <para>
/// The indices of every tile of one width and depth are one array, shared among the tiles of every
/// field the module cuts, so an engine reads them and never writes into them.
/// </para>
/// </remarks>
public sealed record LandscapeMesh
{
    /// <summary>
    /// A tile of its column and row among the field's tiles and its corners' arrays.
    /// </summary>
    /// <param name="column">The tile's column among the field's tiles, from zero along X.</param>
    /// <param name="row">The tile's row among the field's tiles, from zero along Z.</param>
    /// <param name="positions">
    /// A corner a point of the field the tile covers, a row at a time along X and the rows along Z.
    /// </param>
    /// <param name="normals">
    /// The way up out of the ground at each corner, the field's own at its point.
    /// </param>
    /// <param name="uvs">
    /// Where each corner falls over the whole field, from nothing at its first point to one at its
    /// last, X in X and Z in Y.
    /// </param>
    /// <param name="indices">
    /// Three corners a triangle, two triangles a cell, counter-clockwise seen from above.
    /// </param>
    public LandscapeMesh(int column, int row, Vector3[] positions, Vector3[] normals, Vector2[] uvs, uint[] indices)
    {
        (Column, Row, Positions, Normals, Uvs, Indices) = (column, row, positions, normals, uvs, indices);
        ArgumentNullException.ThrowIfNull(positions);
        var (low, high) = positions.Length > 0 ? (positions[0], positions[0]) : default;
        foreach (var corner in positions) (low, high) = (Vector3.Min(low, corner), Vector3.Max(high, corner));
        Bounds = (low, high);
    }

    /// <summary>
    /// The least and the greatest corner of the box about every corner of the tile, its skirt's
    /// among them, which an engine culls the tile by.
    /// </summary>
    public (Vector3 Low, Vector3 High) Bounds { get; }

    /// <summary>
    /// The indices as 16-bit numbers, which an engine draws a tile of up to 65,536 corners with at
    /// half the memory, or nothing where the tile has more.
    /// </summary>
    /// <remarks>
    /// The short indices are made once for each array of indices and kept beside it, so tiles that
    /// share their indices share their short ones, which an engine reads and never writes.
    /// </remarks>
    public ushort[]? ShortIndices => Positions.Length > 65_536 ? null : Short.GetValue(Indices, static indices => [.. indices.Select(index => (ushort)index)]);

    // The short indices of each array of indices, dropped with the array.
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<uint[], ushort[]> Short = new();

    /// <summary>The tile's column among the field's tiles, from zero along X.</summary>
    public int Column { get; init; }

    /// <summary>The tile's row among the field's tiles, from zero along Z.</summary>
    public int Row { get; init; }

    /// <summary>
    /// A corner a point of the field the tile covers, a row at a time along X and the rows along Z.
    /// </summary>
    public Vector3[] Positions { get; init; }

    /// <summary>
    /// The way up out of the ground at each corner, the field's own at its point.
    /// </summary>
    public Vector3[] Normals { get; init; }

    /// <summary>
    /// Where each corner falls over the whole field, from nothing at its first point to one at its
    /// last, X in X and Z in Y.
    /// </summary>
    public Vector2[] Uvs { get; init; }

    /// <summary>
    /// Three corners a triangle, two triangles a cell, counter-clockwise seen from above.
    /// </summary>
    public uint[] Indices { get; init; }
}
