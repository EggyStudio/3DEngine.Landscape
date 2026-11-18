using System.Numerics;

namespace Landscape;

/// <summary>
/// Heights in meters on a grid of points a spacing apart, columns along X and rows along Z, with Y
/// up and the grid about its own middle, which a recipe builds and an engine draws, collides with
/// and plants on.
/// </summary>
/// <remarks>
/// <para>
/// The heights lie a row at a time, each row along X and the rows along Z, the order a physics
/// heightfield takes them in, so an engine hands <see cref="Heights"/> to its physics as it is.
/// </para>
/// <para>
/// Between its points the field is bilinear, and a point's normal is the field's slope across the
/// points beside it, so a mesh made of two tiles gives the corners of their shared edge the same
/// normal and no seam shows between them.
/// </para>
/// </remarks>
public sealed class Heightfield
{
    /// <summary>A flat field at height zero.</summary>
    /// <param name="columns">How many points a row has along X, two or more.</param>
    /// <param name="rows">How many rows of points the field has along Z, two or more.</param>
    /// <param name="spacing">The distance in meters between two points beside each other.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// A count is below two, or the spacing is not a finite number above zero.
    /// </exception>
    public Heightfield(int columns, int rows, float spacing)
    {
        Check(columns, rows, spacing);
        Columns = columns;
        Rows = rows;
        Spacing = spacing;
        Heights = new float[columns * rows];
    }

    /// <summary>How many points a row has along X.</summary>
    public int Columns { get; }

    /// <summary>How many rows of points the field has along Z.</summary>
    public int Rows { get; }

    /// <summary>The distance in meters between two points beside each other.</summary>
    public float Spacing { get; }

    /// <summary>
    /// The heights in meters, a row at a time, each row along X and the rows along Z.
    /// </summary>
    public float[] Heights { get; }

    /// <summary>
    /// The field's width along X in X and its depth along Z in Y, in meters, from its first point
    /// to its last.
    /// </summary>
    public Vector2 Size => new((Columns - 1) * Spacing, (Rows - 1) * Spacing);

    /// <summary>The height of a point in meters.</summary>
    /// <param name="column">The point's column, from zero along X.</param>
    /// <param name="row">The point's row, from zero along Z.</param>
    public float this[int column, int row]
    {
        get => Heights[Index(column, row)];
        set => Heights[Index(column, row)] = value;
    }

    /// <summary>
    /// Where a point lies across the ground, its X in X and its Z in Y, about the field's middle.
    /// </summary>
    /// <param name="column">The point's column, from zero along X.</param>
    /// <param name="row">The point's row, from zero along Z.</param>
    public Vector2 Place(int column, int row) =>
        new((column - (Columns - 1) * 0.5f) * Spacing, (row - (Rows - 1) * 0.5f) * Spacing);

    /// <summary>
    /// The height in meters anywhere over the field, between its points bilinear and past its edge
    /// the edge's.
    /// </summary>
    /// <param name="place">
    /// The place across the ground, its X in X and its Z in Y, about the field's middle.
    /// </param>
    public float Height(Vector2 place)
    {
        var (column, across, row, along) = Cell(place);
        var near = Lerp(this[column, row], this[column + 1, row], across);
        var far = Lerp(this[column, row + 1], this[column + 1, row + 1], across);
        return Lerp(near, far, along);
    }

    /// <summary>The way up out of the ground at a point, at unit length.</summary>
    /// <param name="column">The point's column, from zero along X.</param>
    /// <param name="row">The point's row, from zero along Z.</param>
    /// <remarks>
    /// The rise along X and along Z are taken across the points either side, or between the point
    /// and the one inside at an edge, so a plane's points all give the plane's own normal.
    /// </remarks>
    public Vector3 Normal(int column, int row)
    {
        var (riseX, riseZ) = Rise(column, row);
        return Unit(new Vector3(-riseX, 1f, -riseZ));
    }

    /// <summary>
    /// The way up out of the ground anywhere over the field, at unit length, between its points
    /// blended from theirs.
    /// </summary>
    /// <param name="place">
    /// The place across the ground, its X in X and its Z in Y, about the field's middle.
    /// </param>
    public Vector3 Normal(Vector2 place)
    {
        var (column, across, row, along) = Cell(place);
        var (x00, z00) = Rise(column, row);
        var (x10, z10) = Rise(column + 1, row);
        var (x01, z01) = Rise(column, row + 1);
        var (x11, z11) = Rise(column + 1, row + 1);
        var riseX = Lerp(Lerp(x00, x10, across), Lerp(x01, x11, across), along);
        var riseZ = Lerp(Lerp(z00, z10, across), Lerp(z01, z11, across), along);
        return Unit(new Vector3(-riseX, 1f, -riseZ));
    }

    /// <summary>
    /// How steep the ground is anywhere over the field, the angle in radians between its normal and
    /// up.
    /// </summary>
    /// <param name="place">
    /// The place across the ground, its X in X and its Z in Y, about the field's middle.
    /// </param>
    public float Slope(Vector2 place) => MathF.Acos(Math.Clamp(Normal(place).Y, -1f, 1f));

    /// <summary>Refuses a field's counts and spacing as the arguments they came in.</summary>
    internal static void Check(int columns, int rows, float spacing)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(columns, 2);
        ArgumentOutOfRangeException.ThrowIfLessThan(rows, 2);
        if (!(spacing > 0f) || !float.IsFinite(spacing))
            throw new ArgumentOutOfRangeException(nameof(spacing), spacing, "A field's spacing is a finite number of meters above zero.");
    }

    private int Index(int column, int row)
    {
        if ((uint)column >= (uint)Columns) throw new ArgumentOutOfRangeException(nameof(column), column, $"The field's columns are from zero up to {Columns}.");
        if ((uint)row >= (uint)Rows) throw new ArgumentOutOfRangeException(nameof(row), row, $"The field's rows are from zero up to {Rows}.");
        return row * Columns + column;
    }

    // The cell a place falls in, its first column and row, and how far across and along it the
    // place lies from nothing to one, a place past the edge taken to the edge.
    private (int Column, float Across, int Row, float Along) Cell(Vector2 place)
    {
        var x = Math.Clamp(place.X / Spacing + (Columns - 1) * 0.5f, 0f, Columns - 1);
        var z = Math.Clamp(place.Y / Spacing + (Rows - 1) * 0.5f, 0f, Rows - 1);
        var column = Math.Min((int)x, Columns - 2);
        var row = Math.Min((int)z, Rows - 2);
        return (column, x - column, row, z - row);
    }

    // The rise in meters a meter along X and along Z at a point.
    private (float X, float Z) Rise(int column, int row)
    {
        var (left, right) = (Math.Max(column - 1, 0), Math.Min(column + 1, Columns - 1));
        var (back, front) = (Math.Max(row - 1, 0), Math.Min(row + 1, Rows - 1));
        return ((this[right, row] - this[left, row]) / ((right - left) * Spacing),
                (this[column, front] - this[column, back]) / ((front - back) * Spacing));
    }

    private static float Lerp(float from, float to, float amount) => from + (to - from) * amount;

    // A way at unit length by its own sum of squares, summed in one order.
    private static Vector3 Unit(Vector3 way) => way / MathF.Sqrt(way.X * way.X + way.Y * way.Y + way.Z * way.Z);
}
