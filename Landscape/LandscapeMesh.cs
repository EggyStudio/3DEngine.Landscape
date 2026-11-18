using System.Numerics;

namespace Landscape;

/// <summary>
/// One tile of a field as triangles, in meters with Y up about the field's middle, which an engine
/// builds a mesh of.
/// </summary>
/// <param name="Column">The tile's column among the field's tiles, from zero along X.</param>
/// <param name="Row">The tile's row among the field's tiles, from zero along Z.</param>
/// <param name="Positions">
/// A corner a point of the field the tile covers, a row at a time along X and the rows along Z.
/// </param>
/// <param name="Normals">
/// The way up out of the ground at each corner, the field's own at its point.
/// </param>
/// <param name="Uvs">
/// Where each corner falls over the whole field, from nothing at its first point to one at its
/// last, X in X and Z in Y.
/// </param>
/// <param name="Indices">
/// Three corners a triangle, two triangles a cell, counter-clockwise seen from above.
/// </param>
/// <remarks>
/// Neighboring tiles share the corners of their common edge, each tile holding its own copy, and
/// give them the same positions and normals, since both are the field's at that point, so the tiles
/// meet with no crack and no seam of light. The texture coordinates run over the whole field rather
/// than a tile, so a splat map or a picture laid over the field lines up across the tiles. The
/// winding is counter-clockwise seen from above, the front faces of an engine with Y up and
/// right-handed axes, and an engine that culls the other side takes each triangle's corners in the
/// other order.
/// </remarks>
public sealed record LandscapeMesh(int Column, int Row, Vector3[] Positions, Vector3[] Normals, Vector2[] Uvs, uint[] Indices);
