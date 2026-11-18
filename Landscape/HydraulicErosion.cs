using System.Numerics;

namespace Landscape;

/// <summary>
/// Hydraulic erosion by droplets, each run downhill from a random place carrying the ground it
/// wears away and laying it down where it slows, so slopes are cut into channels and their feet
/// filled.
/// </summary>
/// <remarks>
/// <para>
/// The rule is Beyer's of 2015. A droplet moves a spacing a step along its way, which turns toward
/// the downhill gradient by all but its inertia. Its capacity is its fall over the step times its
/// speed and its water times <see cref="Capacity"/>, never below <see cref="LeastCapacity"/>. Over
/// capacity, or climbing, it lays down sediment on the four points about it by their bilinear
/// shares; under capacity it wears the ground away from the points within <see cref="Radius"/>
/// spacings, each by how near it lies, never by more than its fall, so it digs no pit. Its speed
/// grows with the square of its fall and its water evaporates a share a step.
/// </para>
/// <para>
/// The points on the field's edge are its base level, which no droplet wears or fills, since water
/// leaving the field there takes its sediment with it, and an edge point worn by every droplet that
/// left over it and filled by none was dug without end, deeper with each droplet its slope drew. A
/// droplet that ends inside the field lays down what it carries there, so the ground's volume is
/// kept apart from what droplets carry off the edge. The droplets run one after another in a fixed
/// order, their places and the ways a droplet on level ground takes drawn from the module's PCG,
/// and the step is reckoned with addition, multiplication, division and square root alone, so a
/// seed gives the same bits on every system. The numbers are a step's, and a step is a spacing.
/// </para>
/// </remarks>
public sealed record HydraulicErosion : LandscapeStep
{
    /// <summary>Hydraulic erosion of a count of droplets from a seed.</summary>
    /// <param name="seed">The seed the droplets' places are drawn from.</param>
    /// <param name="droplets">How many droplets run, from one to ten million.</param>
    /// <param name="lifetime">How many steps a droplet runs at most, from one to 1,000.</param>
    /// <param name="inertia">
    /// The share of a droplet's way it keeps each step, from nothing to under one.
    /// </param>
    /// <param name="capacity">
    /// How much sediment a droplet carries for its fall, speed and water.
    /// </param>
    /// <param name="leastCapacity">The least a droplet carries however little it falls.</param>
    /// <param name="deposition">
    /// The share of its sediment over capacity a droplet lays down a step.
    /// </param>
    /// <param name="erosion">
    /// The share of its room under capacity a droplet wears away a step.
    /// </param>
    /// <param name="evaporation">The share of its water a droplet loses a step.</param>
    /// <param name="gravity">How fast a droplet gains speed for its fall.</param>
    /// <param name="radius">
    /// How many spacings from a droplet it wears the ground away, one to eight.
    /// </param>
    /// <param name="scale">
    /// The height in meters a droplet's capacity and speed reckon its fall against, which the
    /// other numbers take as one.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// A number is out of the range its parameter gives.
    /// </exception>
    public HydraulicErosion(
        uint seed,
        int droplets,
        int lifetime = 30,
        float inertia = 0.05f,
        float capacity = 4f,
        float leastCapacity = 0.01f,
        float deposition = 0.3f,
        float erosion = 0.3f,
        float evaporation = 0.01f,
        float gravity = 4f,
        int radius = 3,
        float scale = 40f)
    {
        (Seed, Droplets, Lifetime, Inertia, Capacity, LeastCapacity) = (seed, droplets, lifetime, inertia, capacity, leastCapacity);
        (Deposition, Erosion, Evaporation, Gravity, Radius, Scale) = (deposition, erosion, evaporation, gravity, radius, scale);
    }

    /// <summary>The seed the droplets' places are drawn from.</summary>
    public uint Seed { get; init; }

    /// <summary>How many droplets run.</summary>
    public int Droplets { get; init => field = value is >= 1 and <= 10_000_000 ? value : throw Out(nameof(Droplets), value, "from one to ten million"); }

    /// <summary>How many steps a droplet runs at most.</summary>
    public int Lifetime { get; init => field = value is >= 1 and <= 1_000 ? value : throw Out(nameof(Lifetime), value, "from one to 1,000"); }

    /// <summary>The share of a droplet's way it keeps each step.</summary>
    public float Inertia { get; init => field = value is >= 0f and < 1f ? value : throw Out(nameof(Inertia), value, "from nothing to under one"); }

    /// <summary>How much sediment a droplet carries for its fall, speed and water.</summary>
    public float Capacity { get; init => field = value > 0f && float.IsFinite(value) ? value : throw Out(nameof(Capacity), value, "a finite number above nothing"); }

    /// <summary>The least a droplet carries however little it falls.</summary>
    public float LeastCapacity { get; init => field = value >= 0f && float.IsFinite(value) ? value : throw Out(nameof(LeastCapacity), value, "a finite number at or above nothing"); }

    /// <summary>The share of its sediment over capacity a droplet lays down a step.</summary>
    public float Deposition { get; init => field = value is >= 0f and <= 1f ? value : throw Out(nameof(Deposition), value, "from nothing to one"); }

    /// <summary>The share of its room under capacity a droplet wears away a step.</summary>
    public float Erosion { get; init => field = value is >= 0f and <= 1f ? value : throw Out(nameof(Erosion), value, "from nothing to one"); }

    /// <summary>The share of its water a droplet loses a step.</summary>
    public float Evaporation { get; init => field = value is >= 0f and < 1f ? value : throw Out(nameof(Evaporation), value, "from nothing to under one"); }

    /// <summary>How fast a droplet gains speed for its fall.</summary>
    public float Gravity { get; init => field = value >= 0f && float.IsFinite(value) ? value : throw Out(nameof(Gravity), value, "a finite number at or above nothing"); }

    /// <summary>How many spacings from a droplet it wears the ground away.</summary>
    public int Radius { get; init => field = value is >= 1 and <= 8 ? value : throw Out(nameof(Radius), value, "from one to eight"); }

    /// <summary>
    /// The height in meters a droplet's capacity and speed reckon its fall against.
    /// </summary>
    /// <remarks>
    /// Beyer's numbers were given for heights from nothing to one, and a field in meters falls tens
    /// of times further a step. Reckoned against the field's own meters, 66,000 droplets over a
    /// dome of 40 m on 129 by 129 points leave 80 hundredths of its peak and 749 points lower than
    /// all eight about them, the ground pitted and its gullies faint. Against 40 m they leave 86
    /// hundredths and 330 such pits and cut clean gullies, and a dome of 160 m against 160 m keeps
    /// the same share of its peak, so a scale near the hills' own height takes Beyer's numbers as
    /// he meant them.
    /// </remarks>
    public float Scale { get; init => field = value > 0f && float.IsFinite(value) ? value : throw Out(nameof(Scale), value, "a finite number of meters above nothing"); }

    internal override void Apply(Heightfield field)
    {
        var (columns, rows, heights) = (field.Columns, field.Rows, field.Heights);
        var unit = 1f / Scale;
        var brush = Brush();
        var random = new Pcg(Seed);
        for (var droplet = 0; droplet < Droplets; droplet++)
        {
            var place = new Vector2(random.Unit() * (columns - 1), random.Unit() * (rows - 1));
            var way = Vector2.Zero;
            var (speed, water, sediment) = (1f, 1f, 0f);
            for (var step = 0; step < Lifetime; step++)
            {
                var (column, row) = ((int)place.X, (int)place.Y);
                if (column >= columns - 1 || row >= rows - 1) break;
                var (height, gradient) = Sample(heights, columns, place);

                way = way * Inertia - gradient * (1f - Inertia);
                var length = MathF.Sqrt(way.X * way.X + way.Y * way.Y);
                if (length == 0f)
                {
                    // On level ground a droplet takes one of the eight ways at random.
                    way = Compass[random.Below(8)];
                }
                else
                {
                    way /= length;
                }

                var next = place + way;
                if (!(next.X >= 0f && next.Y >= 0f && next.X < columns - 1 && next.Y < rows - 1))
                {
                    // Off the edge, the droplet takes what it carries with it.
                    sediment = 0f;
                    break;
                }

                var fall = height - Sample(heights, columns, next).Height;
                var room = MathF.Max(fall * unit * speed * water * Capacity, LeastCapacity) * Scale;
                if (sediment > room || fall < 0f)
                {
                    var laid = fall < 0f ? MathF.Min(-fall, sediment) : (sediment - room) * Deposition;
                    sediment -= laid;
                    Lay(heights, columns, rows, place, laid);
                }
                else
                {
                    var worn = MathF.Min((room - sediment) * Erosion, fall);
                    sediment += Wear(heights, columns, rows, column, row, brush, worn);
                }

                speed = MathF.Sqrt(MathF.Max(speed * speed + fall * unit * Gravity, 0f));
                water *= 1f - Evaporation;
                place = next;
            }

            if (sediment > 0f) Lay(heights, columns, rows, place, sediment);
        }
    }

    private static readonly Vector2[] Compass =
    [
        new(1f, 0f), new(-1f, 0f), new(0f, 1f), new(0f, -1f),
        new(0.70710678f, 0.70710678f), new(0.70710678f, -0.70710678f), new(-0.70710678f, 0.70710678f), new(-0.70710678f, -0.70710678f),
    ];

    // The height a place in points stands at and the rise a spacing along X and Z there, both
    // bilinear over the cell it falls in.
    private static (float Height, Vector2 Gradient) Sample(float[] heights, int columns, Vector2 place)
    {
        var (column, row) = ((int)place.X, (int)place.Y);
        var (x, z) = (place.X - column, place.Y - row);
        var at = row * columns + column;
        var (nw, ne, sw, se) = (heights[at], heights[at + 1], heights[at + columns], heights[at + columns + 1]);
        var gradient = new Vector2((ne - nw) * (1f - z) + (se - sw) * z, (sw - nw) * (1f - x) + (se - ne) * x);
        var height = nw * (1f - x) * (1f - z) + ne * x * (1f - z) + sw * (1f - x) * z + se * x * z;
        return (height, gradient);
    }

    // Lays an amount on the four points about a place by their bilinear shares, the shares of
    // points on the field's edge carried off with the water.
    private static void Lay(float[] heights, int columns, int rows, Vector2 place, float amount)
    {
        var (column, row) = ((int)place.X, (int)place.Y);
        var (x, z) = (place.X - column, place.Y - row);
        Add(column, row, amount * (1f - x) * (1f - z));
        Add(column + 1, row, amount * x * (1f - z));
        Add(column, row + 1, amount * (1f - x) * z);
        Add(column + 1, row + 1, amount * x * z);

        void Add(int c, int r, float share)
        {
            if (!OnEdge(c, r, columns, rows)) heights[r * columns + c] += share;
        }
    }

    private static bool OnEdge(int column, int row, int columns, int rows) =>
        column == 0 || row == 0 || column == columns - 1 || row == rows - 1;

    // Wears an amount away from the points within the radius of a droplet's cell, each by its share
    // of the brush, and gives back what was worn, the shares of points on or past the edge left
    // out.
    private static float Wear(float[] heights, int columns, int rows, int column, int row, (int X, int Z, float Share)[] brush, float amount)
    {
        var worn = 0f;
        foreach (var (x, z, share) in brush)
        {
            var (c, r) = (column + x, row + z);
            if ((uint)c >= (uint)columns || (uint)r >= (uint)rows || OnEdge(c, r, columns, rows)) continue;
            var taken = amount * share;
            heights[r * columns + c] -= taken;
            worn += taken;
        }

        return worn;
    }

    // The points within the radius of a cell's corner and their shares, falling straight to
    // nothing at the radius and summing to one.
    private (int X, int Z, float Share)[] Brush()
    {
        var points = new List<(int, int, float)>();
        var total = 0f;
        for (var z = -Radius; z <= Radius; z++)
        {
            for (var x = -Radius; x <= Radius; x++)
            {
                var weight = 1f - MathF.Sqrt(x * x + z * z) / Radius;
                if (weight <= 0f) continue;
                points.Add((x, z, weight));
                total += weight;
            }
        }

        return [.. points.Select(point => (point.Item1, point.Item2, point.Item3 / total))];
    }

    private static ArgumentOutOfRangeException Out(string name, object value, string range) =>
        new(name, value, $"Hydraulic erosion's {char.ToLowerInvariant(name[0])}{name[1..]} is {range}.");
}
