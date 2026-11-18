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
    /// Which cells are holes, a cell between four points a row at a time, each row along X and
    /// the rows along Z, or nothing where the field has none.
    /// </summary>
    /// <remarks>
    /// A hole's cell is drawn by no tile and met by no ray, and its heights stand as the steps left
    /// them, so a physics heightfield that takes holes takes this array beside the heights.
    /// </remarks>
    public bool[]? Holes { get; private set; }

    /// <summary>Whether a cell is a hole.</summary>
    /// <param name="column">The cell's column, the column of its least corner.</param>
    /// <param name="row">The cell's row, the row of its least corner.</param>
    public bool IsHole(int column, int row)
    {
        if ((uint)column >= (uint)(Columns - 1)) throw new ArgumentOutOfRangeException(nameof(column), column, $"The field's cells are from zero up to {Columns - 1}.");
        if ((uint)row >= (uint)(Rows - 1)) throw new ArgumentOutOfRangeException(nameof(row), row, $"The field's cells are from zero up to {Rows - 1}.");
        return Holes is { } holes && holes[row * (Columns - 1) + column];
    }

    // Marks a cell a hole, making the field's holes where it had none.
    internal void MakeHole(int column, int row) => (Holes ??= new bool[(Columns - 1) * (Rows - 1)])[row * (Columns - 1) + column] = true;

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

    /// <summary>
    /// How steep the ground is at a point as a rise over a run, the rise along X and along Z taken
    /// together, nothing on flat ground and one at 45 degrees.
    /// </summary>
    /// <param name="column">The point's column, from zero along X.</param>
    /// <param name="row">The point's row, from zero along Z.</param>
    /// <remarks>
    /// A rise is the field's own arithmetic, a square root of a sum of squares, where an angle is a
    /// tangent's or an arc cosine's that each C runtime rounds its own way, so what is reckoned
    /// from it, a splat map among them, is the same on every system.
    /// </remarks>
    public float Steepness(int column, int row)
    {
        var (x, z) = Rise(column, row);
        return MathF.Sqrt(x * x + z * z);
    }

    /// <summary>
    /// How steep the ground is anywhere over the field as a rise over a run, between its points
    /// blended from theirs.
    /// </summary>
    /// <param name="place">
    /// The place across the ground, its X in X and its Z in Y, about the field's middle.
    /// </param>
    public float Steepness(Vector2 place)
    {
        var (column, across, row, along) = Cell(place);
        var (x00, z00) = Rise(column, row);
        var (x10, z10) = Rise(column + 1, row);
        var (x01, z01) = Rise(column, row + 1);
        var (x11, z11) = Rise(column + 1, row + 1);
        var riseX = Lerp(Lerp(x00, x10, across), Lerp(x01, x11, across), along);
        var riseZ = Lerp(Lerp(z00, z10, across), Lerp(z01, z11, across), along);
        return MathF.Sqrt(riseX * riseX + riseZ * riseZ);
    }

    /// <summary>Whether a place lies over the field, its edge counted in.</summary>
    /// <param name="place">
    /// The place across the ground, its X in X and its Z in Y, about the field's middle.
    /// </param>
    public bool Covers(Vector2 place)
    {
        var half = Size * 0.5f;
        return place.X >= -half.X && place.X <= half.X && place.Y >= -half.Y && place.Y <= half.Y;
    }

    /// <summary>Where a ray first meets the ground the field's tiles draw, or nothing.</summary>
    /// <param name="origin">Where the ray starts, in meters about the field's middle.</param>
    /// <param name="direction">The way it runs, of any length but none.</param>
    /// <param name="reach">How far along it a hit counts, in meters.</param>
    /// <exception cref="ArgumentException">
    /// The origin is not finite, or the direction has no length.
    /// </exception>
    /// <remarks>
    /// The ray is cut to the field's rectangle and walks it a cell at a time in the order it
    /// crosses them, meeting each cell's two triangles as <see cref="LandscapeGeometry.Tile"/>
    /// draws them at a step of one, from either side, so the first cell it meets the ground in
    /// holds the nearest hit and the walk crosses a field of 4,097 points a side in some 8,000
    /// cells at most rather than its 33 million triangles.
    /// </remarks>
    public RayHit? Raycast(Vector3 origin, Vector3 direction, float reach = float.PositiveInfinity)
    {
        if (!float.IsFinite(origin.X) || !float.IsFinite(origin.Y) || !float.IsFinite(origin.Z))
            throw new ArgumentException("A ray's origin is finite.", nameof(origin));
        var length = MathF.Sqrt(direction.X * direction.X + direction.Y * direction.Y + direction.Z * direction.Z);
        if (!(length > 0f) || !float.IsFinite(length)) throw new ArgumentException("A ray runs some way.", nameof(direction));
        var way = direction / length;

        // The ray in the grid's units, a point's column and row, and the stretch of it over the
        // field's rectangle.
        var (gx, gz) = (origin.X / Spacing + (Columns - 1) * 0.5f, origin.Z / Spacing + (Rows - 1) * 0.5f);
        var (wx, wz) = (way.X / Spacing, way.Z / Spacing);
        var (enter, leave) = (0f, reach);
        if (!Slab(gx, wx, Columns - 1, ref enter, ref leave) || !Slab(gz, wz, Rows - 1, ref enter, ref leave)) return null;

        var (x, z) = (gx + wx * enter, gz + wz * enter);
        var (column, row) = (Math.Clamp((int)MathF.Floor(x), 0, Columns - 2), Math.Clamp((int)MathF.Floor(z), 0, Rows - 2));
        var (stepX, stepZ) = (wx > 0f ? 1 : -1, wz > 0f ? 1 : -1);
        var nextX = wx == 0f ? float.PositiveInfinity : ((wx > 0f ? column + 1 : column) - gx) / wx;
        var nextZ = wz == 0f ? float.PositiveInfinity : ((wz > 0f ? row + 1 : row) - gz) / wz;
        var (acrossX, acrossZ) = (wx == 0f ? float.PositiveInfinity : MathF.Abs(1f / wx), wz == 0f ? float.PositiveInfinity : MathF.Abs(1f / wz));
        while (true)
        {
            if (Crossing(origin, way, column, row) is { } distance && distance <= reach)
            {
                var place = origin + way * distance;
                return new RayHit(place, distance, Normal(new Vector2(place.X, place.Z)));
            }

            if (MathF.Min(nextX, nextZ) > leave) return null;
            if (nextX < nextZ)
            {
                column += stepX;
                nextX += acrossX;
            }
            else
            {
                row += stepZ;
                nextZ += acrossZ;
            }

            if ((uint)column > (uint)(Columns - 2) || (uint)row > (uint)(Rows - 2)) return null;
        }
    }

    // Cuts the stretch of a ray to where its one coordinate lies from nothing to the far side, or
    // says the ray never lies there.
    private static bool Slab(float from, float way, float far, ref float enter, ref float leave)
    {
        if (way == 0f) return from >= 0f && from <= far;
        var (a, b) = ((0f - from) / way, (far - from) / way);
        if (a > b) (a, b) = (b, a);
        (enter, leave) = (MathF.Max(enter, a), MathF.Min(leave, b));
        return enter <= leave;
    }

    // The nearest crossing of a ray with a cell's two triangles, the near-left, far-left and
    // near-right corners and the near-right, far-left and far-right, by Möller and Trumbore's rule
    // from either side, each edge taken in by a millionth so a ray down an edge is met.
    private float? Crossing(Vector3 origin, Vector3 way, int column, int row)
    {
        if (IsHole(column, row)) return null;
        Vector3 Corner(int c, int r) => new(Place(c, r).X, this[c, r], Place(c, r).Y);
        var (nearLeft, nearRight, farLeft, farRight) = (Corner(column, row), Corner(column + 1, row), Corner(column, row + 1), Corner(column + 1, row + 1));
        var (first, second) = (Triangle(origin, way, nearLeft, farLeft, nearRight), Triangle(origin, way, nearRight, farLeft, farRight));
        return first is null ? second : second is null ? first : MathF.Min(first.Value, second.Value);
    }

    private static float? Triangle(Vector3 origin, Vector3 way, Vector3 a, Vector3 b, Vector3 c)
    {
        const float edge = 1e-6f;
        var (e1, e2) = (b - a, c - a);
        var p = Vector3.Cross(way, e2);
        var det = Vector3.Dot(e1, p);
        if (det == 0f) return null;
        var s = origin - a;
        var u = Vector3.Dot(s, p) / det;
        if (u < -edge || u > 1f + edge) return null;
        var q = Vector3.Cross(s, e1);
        var v = Vector3.Dot(way, q) / det;
        if (v < -edge || u + v > 1f + edge) return null;
        var distance = Vector3.Dot(e2, q) / det;
        return distance >= 0f ? distance : null;
    }

    /// <summary>A field of the same size holding the same heights, apart from this one.</summary>
    public Heightfield Copy()
    {
        var copy = new Heightfield(Columns, Rows, Spacing);
        Heights.CopyTo(copy.Heights, 0);
        copy.Holes = Holes is null ? null : [.. Holes];
        return copy;
    }

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
