namespace Landscape;

/// <summary>
/// Cuts the field into terraces, steps of a height each with a flat tread and a riser that rises
/// smoothly to the next.
/// </summary>
/// <remarks>
/// A height is split into its step and how far up the step it lies, from nothing to one. The
/// tread's share of the step keeps the lower height, and the rest rises by Hermite's smooth step to
/// the next, so the ground stays continuous across every riser and a tread of nothing gives gentle
/// steps with no flat at all.
/// </remarks>
public sealed record Terrace : LandscapeStep
{
    /// <summary>Terraces of a height with a tread of a share of each step.</summary>
    /// <param name="height">Each step's height in meters.</param>
    /// <param name="tread">The share of each step that is flat, from nothing up to one.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The height is not a finite number above zero, or the tread is not from nothing up to one.
    /// </exception>
    public Terrace(float height, float tread)
    {
        Height = height;
        Tread = tread;
    }

    /// <summary>Each step's height in meters.</summary>
    public float Height
    {
        get;
        init => field = value > 0f && float.IsFinite(value) ? value : throw new ArgumentOutOfRangeException(nameof(Height), value, "A terrace's height is a finite number of meters above zero.");
    }

    /// <summary>The share of each step that is flat, from nothing up to one.</summary>
    public float Tread
    {
        get;
        init => field = value is >= 0f and < 1f ? value : throw new ArgumentOutOfRangeException(nameof(Tread), value, "A terrace's tread is from nothing up to one.");
    }

    /// <summary>The height a terraced ground takes in place of a height.</summary>
    /// <param name="height">The ground's height in meters before the terraces.</param>
    public float Of(float height)
    {
        var steps = height / Height;
        var step = MathF.Floor(steps);
        var up = steps - step;
        var rise = up <= Tread ? 0f : Smooth((up - Tread) / (1f - Tread));
        return (step + rise) * Height;
    }

    internal override void Apply(Heightfield field, BuildContext context)
    {
        context.Options.Rows(0, field.Rows - 1, row =>
        {
            for (var i = row * field.Columns; i < (row + 1) * field.Columns; i++) field.Heights[i] = Of(field.Heights[i]);
        });
    }

    private static float Smooth(float t) => t * t * (3f - 2f * t);
}
