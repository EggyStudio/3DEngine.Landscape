using System.Numerics;

namespace Landscape;

/// <summary>
/// A step scaled by a picture of weights over a rectangle, so it acts on part of the field, the
/// weights read through the engine's <see cref="ILandscapePictures"/> by the picture's name.
/// </summary>
/// <remarks>
/// <para>
/// The step is applied to a copy of the field, and each point takes the height before it and the
/// step's change times the weight at its place, the weight the picture's share there, bilinear
/// between its texels as a <see cref="PictureStamp"/> reads them, and nothing outside the
/// rectangle. A weight of one gives the step's height and one of nothing the ground's, each to the
/// bit. A hole the step opens is kept where the weight at its cell's middle is a half or more.
/// </para>
/// <para>
/// A step whose change reaches past its own place, an erosion among them, reckons that change
/// over the whole field before the weights scale it, so a masked erosion's edge is as soft as its
/// mask's.
/// </para>
/// </remarks>
public sealed record Masked : LandscapeStep
{
    /// <summary>A step masked by a picture over a rectangle.</summary>
    /// <param name="step">The step the weights scale.</param>
    /// <param name="mask">
    /// The picture's name, its shares the weights, which the engine answers for.
    /// </param>
    /// <param name="place">The rectangle's middle across the ground, X in X and Z in Y.</param>
    /// <param name="size">
    /// The rectangle's width along X in X and its depth along Z in Y, in meters.
    /// </param>
    /// <param name="turns">
    /// How many quarter turns the picture is turned by within the rectangle, from nothing to three,
    /// a quarter taking its first row, along X, to run along Z at the rectangle's greatest X.
    /// </param>
    /// <exception cref="ArgumentNullException">The step is missing.</exception>
    /// <exception cref="ArgumentException">The mask has no name.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The place is not finite or the size not above nothing.
    /// </exception>
    public Masked(LandscapeStep step, string mask, Vector2 place, Vector2 size, int turns = 0)
    {
        ArgumentNullException.ThrowIfNull(step);
        ArgumentException.ThrowIfNullOrWhiteSpace(mask);
        (Step, Mask, Place, Size, Turns) = (step, mask, place, size, turns);
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

    /// <summary>The step the weights scale.</summary>
    public LandscapeStep Step { get; init; }

    /// <summary>The picture's name, its shares the weights, which the engine answers for.</summary>
    public string Mask { get; init; }

    /// <summary>The rectangle's middle across the ground, X in X and Z in Y.</summary>
    public Vector2 Place
    {
        get;
        init => field = float.IsFinite(value.X) && float.IsFinite(value.Y) ? value : throw new ArgumentOutOfRangeException(nameof(Place), value, "A mask's place is finite.");
    }

    /// <summary>The rectangle's width along X in X and its depth along Z in Y, in meters.</summary>
    public Vector2 Size
    {
        get;
        init => field = value.X > 0f && value.Y > 0f && float.IsFinite(value.X) && float.IsFinite(value.Y) ? value : throw new ArgumentOutOfRangeException(nameof(Size), value, "A mask's size is finite and above nothing each way.");
    }

    internal override void Apply(Heightfield field, BuildContext context)
    {
        var mask = context.Pictures?.Heights(Mask)
            ?? throw new InvalidOperationException($"The recipe masks a step by the picture {Mask}, which the engine's pictures do not hold.");
        var changed = field.Copy();
        Step.Apply(changed, context);
        context.Options.Rows(0, field.Rows - 1, row =>
        {
            for (var column = 0; column < field.Columns; column++)
            {
                var weight = Weight(mask, field.Place(column, row));
                var (before, after) = (field[column, row], changed[column, row]);
                field[column, row] = weight >= 1f ? after : weight <= 0f ? before : before + (after - before) * weight;
            }
        });

        if (changed.Holes is null) return;
        var half = field.Spacing * 0.5f;
        for (var row = 0; row < field.Rows - 1; row++)
        {
            for (var column = 0; column < field.Columns - 1; column++)
            {
                if (changed.IsHole(column, row) && Weight(mask, field.Place(column, row) + new Vector2(half, half)) >= 0.5f)
                    field.MakeHole(column, row);
            }
        }
    }

    // The weight at a place, the mask's share there and nothing outside the rectangle.
    private float Weight(HeightPicture mask, Vector2 place)
    {
        var from = place - Place;
        var half = Size * 0.5f;
        if (!(MathF.Abs(from.X) <= half.X && MathF.Abs(from.Y) <= half.Y)) return 0f;
        return mask.At((from.X + half.X) / Size.X, (from.Y + half.Y) / Size.Y, Turns);
    }
}
