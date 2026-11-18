namespace Landscape;

/// <summary>
/// A rectangle of a field's points, from its first column and row to its last, both kept.
/// </summary>
/// <param name="FirstColumn">The first column, from zero along X.</param>
/// <param name="FirstRow">The first row, from zero along Z.</param>
/// <param name="LastColumn">The last column, at or past the first.</param>
/// <param name="LastRow">The last row, at or past the first.</param>
public readonly record struct LandscapeArea(int FirstColumn, int FirstRow, int LastColumn, int LastRow)
{
    /// <summary>
    /// The tiles of a field cut by <see cref="LandscapeGeometry.Tiles"/> whose corners a change of
    /// the points in the rectangle reaches, a row of tiles at a time.
    /// </summary>
    /// <param name="field">The field the rectangle is of.</param>
    /// <param name="tileCells">How many cells a tile's side spans.</param>
    /// <remarks>
    /// A point's normal is read from the points either side of it, so a changed height changes the
    /// normals of the points beside it as well, and the rectangle is grown by a point each way
    /// before the tiles it touches are found. Two tiles share the corners of their common edge, so
    /// a point on it reaches both.
    /// </remarks>
    public IReadOnlyList<(int Column, int Row)> Tiles(Heightfield field, int tileCells = LandscapeGeometry.TileCells)
    {
        ArgumentNullException.ThrowIfNull(field);
        ArgumentOutOfRangeException.ThrowIfLessThan(tileCells, 1);
        var (lastTileColumn, lastTileRow) = ((field.Columns - 2) / tileCells, (field.Rows - 2) / tileCells);
        var (first, last) = (Math.Max(FirstColumn - 1, 0), Math.Min(LastColumn + 1, field.Columns - 1));
        var (front, back) = (Math.Max(FirstRow - 1, 0), Math.Min(LastRow + 1, field.Rows - 1));

        // A point on a tile's first edge is the last of the tile before it as well.
        static (int From, int To) Span(int from, int to, int cells, int lastTile) =>
            (Math.Max(0, Math.Min((from - 1) / cells, lastTile)), Math.Min(to / cells, lastTile));
        var (fromColumn, toColumn) = Span(first, last, tileCells, lastTileColumn);
        var (fromRow, toRow) = Span(front, back, tileCells, lastTileRow);

        var tiles = new List<(int, int)>();
        for (var row = fromRow; row <= toRow; row++)
        {
            for (var column = fromColumn; column <= toColumn; column++) tiles.Add((column, row));
        }

        return tiles;
    }
}
