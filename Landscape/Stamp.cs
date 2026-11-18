using System.Numerics;

namespace Landscape;

/// <summary>
/// A shape pressed into the field at a place, raising or lowering the points within its radius by
/// its profile and its blend.
/// </summary>
/// <remarks>
/// The profile is reckoned from the square of the distance with multiplication, division and a
/// square root alone, each of which IEEE 754 rounds to the nearest float on every system, so a
/// stamp gives the same heights to the bit on Linux, Windows and macOS. A stamp reaching past the
/// field's edge changes the points inside the field and no others.
/// </remarks>
public sealed record Stamp : LandscapeStep
{
    /// <summary>A stamp of a shape at a place.</summary>
    /// <param name="shape">The profile it rises by.</param>
    /// <param name="place">
    /// Its middle across the ground, X in X and Z in Y, about the field's middle.
    /// </param>
    /// <param name="radius">How far from its middle it reaches, in meters.</param>
    /// <param name="height">
    /// Its height at the profile's top in meters, below zero for a hollow.
    /// </param>
    /// <param name="blend">How it meets the heights before it.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The shape or the blend is not one the module has, the place or the height is not finite, or
    /// the radius is not a finite number above zero.
    /// </exception>
    public Stamp(StampShape shape, Vector2 place, float radius, float height, StampBlend blend = StampBlend.Add)
    {
        Shape = shape;
        Place = place;
        Radius = radius;
        Height = height;
        Blend = blend;
    }

    /// <summary>The profile it rises by.</summary>
    public StampShape Shape
    {
        get;
        init => field = Enum.IsDefined(value) ? value : throw new ArgumentOutOfRangeException(nameof(Shape), value, "A stamp's shape is one the module has.");
    }

    /// <summary>
    /// Its middle across the ground, X in X and Z in Y, about the field's middle.
    /// </summary>
    public Vector2 Place
    {
        get;
        init => field = float.IsFinite(value.X) && float.IsFinite(value.Y) ? value : throw new ArgumentOutOfRangeException(nameof(Place), value, "A stamp's place is finite.");
    }

    /// <summary>How far from its middle it reaches, in meters.</summary>
    public float Radius
    {
        get;
        init => field = value > 0f && float.IsFinite(value) ? value : throw new ArgumentOutOfRangeException(nameof(Radius), value, "A stamp's radius is a finite number of meters above zero.");
    }

    /// <summary>Its height at the profile's top in meters, below zero for a hollow.</summary>
    public float Height
    {
        get;
        init => field = float.IsFinite(value) ? value : throw new ArgumentOutOfRangeException(nameof(Height), value, "A stamp's height is finite.");
    }

    /// <summary>How it meets the heights before it.</summary>
    public StampBlend Blend
    {
        get;
        init => field = Enum.IsDefined(value) ? value : throw new ArgumentOutOfRangeException(nameof(Blend), value, "A stamp's blend is one the module has.");
    }

    /// <summary>The share of its height the profile gives at a distance from its middle.</summary>
    /// <param name="distance">
    /// The distance in meters, at or beyond the radius giving nothing.
    /// </param>
    public float Profile(float distance)
    {
        var t = distance / Radius;
        if (!(t < 1f)) return 0f;
        return Shape switch
        {
            StampShape.Cone => 1f - t,
            StampShape.Dome => Square(1f - t * t),
            StampShape.Plateau => t <= 0.5f ? 1f : 1f - Smooth((t - 0.5f) / 0.5f),
            _ => t <= 0.7f ? -0.4f + 1.4f * Square(t / 0.7f) : 1f - Smooth((t - 0.7f) / 0.3f),
        };
    }

    internal override void Apply(Heightfield field, BuildContext context)
    {
        // The points whose cell can reach the radius, cut to the field's own.
        var reach = Radius / field.Spacing;
        var middleX = Place.X / field.Spacing + (field.Columns - 1) * 0.5f;
        var middleZ = Place.Y / field.Spacing + (field.Rows - 1) * 0.5f;
        var (first, last) = (Math.Max((int)MathF.Floor(middleX - reach), 0), Math.Min((int)MathF.Ceiling(middleX + reach), field.Columns - 1));
        var (front, back) = (Math.Max((int)MathF.Floor(middleZ - reach), 0), Math.Min((int)MathF.Ceiling(middleZ + reach), field.Rows - 1));

        context.Options.Rows(front, back, row =>
        {
            for (var column = first; column <= last; column++)
            {
                var way = field.Place(column, row) - Place;
                var distance = MathF.Sqrt(way.X * way.X + way.Y * way.Y);
                if (!(distance < Radius)) continue;
                var share = Profile(distance);
                var ground = field[column, row];
                field[column, row] = Blend switch
                {
                    StampBlend.Add => ground + Height * share,
                    StampBlend.Max => MathF.Max(ground, Height * share),
                    StampBlend.Min => MathF.Min(ground, Height * share),
                    _ => ground + (Height - ground) * share,
                };
            }
        });
    }

    private static float Square(float value) => value * value;

    // Hermite's smooth step from nothing at nothing to one at one, flat at both ends.
    private static float Smooth(float t) => t * t * (3f - 2f * t);
}
