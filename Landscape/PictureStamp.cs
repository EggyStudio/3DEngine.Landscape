using System.Numerics;

namespace Landscape;

/// <summary>
/// A picture's heights pressed into the field over a rectangle, a mountain or a riverbed an artist
/// drew, read through the engine's <see cref="ILandscapePictures"/> by its name.
/// </summary>
/// <remarks>
/// <para>
/// The picture spans the rectangle of its size about its place, its first texel's middle at the
/// rectangle's least X and Z and its last's at the greatest, along the field's own axes. Its share
/// times the height is the stamp's height at a point, and the blend gives the height the ground
/// is drawn toward, the two added, the higher, the lower or the stamp's alone.
/// </para>
/// <para>
/// The ground is drawn toward that height by a weight of one inside the rectangle, falling by
/// Hermite's smooth step to nothing over the edge's width inward from each side, so a picture meets
/// the ground with no cliff. An edge of nothing draws the whole rectangle to it.
/// </para>
/// </remarks>
public sealed record PictureStamp : LandscapeStep
{
    /// <summary>A stamp of a picture over a rectangle.</summary>
    /// <param name="picture">The picture's name, which the engine answers for.</param>
    /// <param name="place">The rectangle's middle across the ground, X in X and Z in Y.</param>
    /// <param name="size">
    /// The rectangle's width along X in X and its depth along Z in Y, in meters.
    /// </param>
    /// <param name="height">The height in meters a share of one stands at.</param>
    /// <param name="blend">How the picture meets the heights before it.</param>
    /// <param name="edge">How many meters in from each side the stamp fades over.</param>
    /// <param name="turns">
    /// How many quarter turns the picture is turned by within the rectangle, from nothing to three,
    /// a quarter taking its first row, along X, to run along Z at the rectangle's greatest X.
    /// </param>
    /// <exception cref="ArgumentException">The picture has no name.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// A number is out of the range its parameter gives.
    /// </exception>
    public PictureStamp(string picture, Vector2 place, Vector2 size, float height, StampBlend blend = StampBlend.Add, float edge = 0f, int turns = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(picture);
        (Picture, Place, Size, Height, Blend, Edge, Turns) = (picture, place, size, height, blend, edge, turns);
    }

    /// <summary>
    /// How many quarter turns the picture is turned by within the rectangle, from nothing to three.
    /// </summary>
    /// <remarks>
    /// A turn reorders where the picture is read and reckons no sine or cosine, so a turned picture
    /// gives the same bits on every system, and the rectangle's size is its size on the ground
    /// after the turn.
    /// </remarks>
    public int Turns
    {
        get;
        init => field = value is >= 0 and <= 3 ? value : throw new ArgumentOutOfRangeException(nameof(Turns), value, "A picture is turned by from nothing to three quarter turns.");
    }

    /// <summary>The picture's name, which the engine answers for.</summary>
    public string Picture { get; init; }

    /// <summary>The rectangle's middle across the ground, X in X and Z in Y.</summary>
    public Vector2 Place
    {
        get;
        init => field = float.IsFinite(value.X) && float.IsFinite(value.Y) ? value : throw new ArgumentOutOfRangeException(nameof(Place), value, "A picture stamp's place is finite.");
    }

    /// <summary>The rectangle's width along X in X and its depth along Z in Y, in meters.</summary>
    public Vector2 Size
    {
        get;
        init => field = value.X > 0f && value.Y > 0f && float.IsFinite(value.X) && float.IsFinite(value.Y) ? value : throw new ArgumentOutOfRangeException(nameof(Size), value, "A picture stamp's size is finite and above nothing each way.");
    }

    /// <summary>The height in meters a share of one stands at.</summary>
    public float Height
    {
        get;
        init => field = float.IsFinite(value) ? value : throw new ArgumentOutOfRangeException(nameof(Height), value, "A picture stamp's height is finite.");
    }

    /// <summary>How the picture meets the heights before it.</summary>
    public StampBlend Blend
    {
        get;
        init => field = Enum.IsDefined(value) ? value : throw new ArgumentOutOfRangeException(nameof(Blend), value, "A picture stamp's blend is one the module has.");
    }

    /// <summary>How many meters in from each side the stamp fades over.</summary>
    public float Edge
    {
        get;
        init => field = value >= 0f && float.IsFinite(value) ? value : throw new ArgumentOutOfRangeException(nameof(Edge), value, "A picture stamp's edge is a finite number of meters at or above nothing.");
    }

    internal override void Apply(Heightfield field, BuildContext context)
    {
        var picture = context.Pictures?.Heights(Picture)
            ?? throw new InvalidOperationException($"The recipe stamps the picture {Picture}, which the engine's pictures do not hold.");
        var half = Size * 0.5f;
        context.Options.Rows(0, field.Rows - 1, row =>
        {
            for (var column = 0; column < field.Columns; column++)
            {
                var from = field.Place(column, row) - Place;
                if (!(MathF.Abs(from.X) <= half.X && MathF.Abs(from.Y) <= half.Y)) continue;
                var weight = Fade(half.X - MathF.Abs(from.X)) * Fade(half.Y - MathF.Abs(from.Y));
                if (weight == 0f) continue;
                var stamped = Height * picture.At((from.X + half.X) / Size.X, (from.Y + half.Y) / Size.Y, Turns);
                var ground = field[column, row];
                var toward = Blend switch
                {
                    StampBlend.Add => ground + stamped,
                    StampBlend.Max => MathF.Max(ground, stamped),
                    StampBlend.Min => MathF.Min(ground, stamped),
                    _ => stamped,
                };
                field[column, row] = ground + (toward - ground) * weight;
            }
        });
    }

    // The weight a distance in from a side gives, one at the edge's width and beyond.
    private float Fade(float inward)
    {
        if (Edge == 0f || inward >= Edge) return 1f;
        var t = inward / Edge;
        return t * t * (3f - 2f * t);
    }
}
