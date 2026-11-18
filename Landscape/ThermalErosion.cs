namespace Landscape;

/// <summary>
/// Thermal erosion, ground steeper than its talus slumping to the lower points beside it until it
/// stands no steeper, as loose rock and soil settle.
/// </summary>
/// <remarks>
/// <para>
/// Each pass a point compares itself with the eight about it, along X, along Z and along the
/// diagonals. Where it stands higher than a neighbor by more than the talus allows over the
/// distance between them, a share of half the largest excess leaves it, split among the neighbors
/// it stands too high over by how far too high it stands over each, as Musgrave, Kolb and Mace gave
/// the rule in 1989. The moves of a pass are gathered into a second field before any is made, so
/// the order the points are visited in changes nothing, and what leaves one point reaches another,
/// so the ground's volume is kept.
/// </para>
/// <para>
/// The diagonals are compared over the square root of two spacings, since the four neighbors along
/// X and Z alone hold a slope facing a diagonal at the square root of two times the talus, and a
/// cone slumps into a square pyramid. With all eight the ground settles into eight faces, and a
/// slope facing between an axis and a diagonal stands at most some eight hundredths steeper than
/// the talus, one over the cosine of 22.5 degrees.
/// </para>
/// <para>
/// The talus is a rise over a run and not an angle, since a tangent is each C runtime's to round
/// and a rise is the field's own arithmetic. A talus of 0.7 stands at 35 degrees, and of 1 at 45.
/// </para>
/// </remarks>
public sealed record ThermalErosion : LandscapeStep
{
    /// <summary>Thermal erosion by a talus over a count of passes.</summary>
    /// <param name="talus">The steepest rise a meter the ground keeps, above nothing.</param>
    /// <param name="passes">How many passes, from one to 10,000.</param>
    /// <param name="share">
    /// The share of half the largest excess that moves each pass, above nothing and at most one.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// A number is out of the range its parameter gives.
    /// </exception>
    public ThermalErosion(float talus, int passes, float share = 0.5f)
    {
        Talus = talus;
        Passes = passes;
        Share = share;
    }

    /// <summary>The steepest rise a meter the ground keeps.</summary>
    public float Talus
    {
        get;
        init => field = value > 0f && float.IsFinite(value) ? value : throw new ArgumentOutOfRangeException(nameof(Talus), value, "A talus is a finite rise above nothing.");
    }

    /// <summary>How many passes, from one to 10,000.</summary>
    public int Passes
    {
        get;
        init => field = value is >= 1 and <= 10_000 ? value : throw new ArgumentOutOfRangeException(nameof(Passes), value, "Thermal erosion takes from one to 10,000 passes.");
    }

    /// <summary>The share of half the largest excess that moves each pass.</summary>
    public float Share
    {
        get;
        init => field = value is > 0f and <= 1f ? value : throw new ArgumentOutOfRangeException(nameof(Share), value, "A share is above nothing and at most one.");
    }

    internal override void Apply(Heightfield field)
    {
        var (columns, rows, heights) = (field.Columns, field.Rows, field.Heights);
        var straight = Talus * field.Spacing;
        var diagonal = straight * 1.41421356f;
        var moves = new float[heights.Length];
        Span<int> beside = stackalloc int[8];
        Span<float> excess = stackalloc float[8];
        for (var pass = 0; pass < Passes; pass++)
        {
            Array.Clear(moves);
            for (var row = 0; row < rows; row++)
            {
                for (var column = 0; column < columns; column++)
                {
                    var at = row * columns + column;
                    var (largest, total, count) = (0f, 0f, 0);
                    for (var z = -1; z <= 1; z++)
                    {
                        for (var x = -1; x <= 1; x++)
                        {
                            if ((x == 0 && z == 0) || (uint)(column + x) >= (uint)columns || (uint)(row + z) >= (uint)rows) continue;
                            var other = at + z * columns + x;
                            var over = heights[at] - heights[other] - (x == 0 || z == 0 ? straight : diagonal);
                            beside[count] = other;
                            excess[count] = over > 0f ? over : 0f;
                            total += excess[count];
                            largest = MathF.Max(largest, excess[count]);
                            count++;
                        }
                    }

                    if (total == 0f) continue;
                    var leaving = Share * largest * 0.5f;
                    moves[at] -= leaving;
                    for (var n = 0; n < count; n++)
                    {
                        if (excess[n] > 0f) moves[beside[n]] += leaving * (excess[n] / total);
                    }
                }
            }

            for (var i = 0; i < heights.Length; i++) heights[i] += moves[i];
        }
    }
}
