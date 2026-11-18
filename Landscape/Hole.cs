using System.Numerics;

namespace Landscape;

/// <summary>
/// A hole in the ground, the cells whose middles lie within its radius of its place, which no tile
/// draws and no ray meets, for a cave's or a tunnel's mouth an engine fills with a mesh of its own.
/// </summary>
/// <remarks>
/// A hole changes no height, so the ground about it stands as the steps left it and an engine's
/// mesh meets its edge, and the field's <see cref="Heightfield.Holes"/> carries the cells for an
/// engine's physics. A tile drawn at a coarse step leaves out each of its cells that holds a hole's
/// cell, so a hole drawn far off stands as wide as its coarse cells.
/// </remarks>
public sealed record Hole : LandscapeStep
{
    /// <summary>A hole of a radius at a place.</summary>
    /// <param name="place">
    /// Its middle across the ground, X in X and Z in Y, about the field's middle.
    /// </param>
    /// <param name="radius">
    /// How far from its middle a cell's middle lies to be a hole, in meters.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The place is not finite, or the radius is not a finite number above zero.
    /// </exception>
    public Hole(Vector2 place, float radius) => (Place, Radius) = (place, radius);

    /// <summary>
    /// Its middle across the ground, X in X and Z in Y, about the field's middle.
    /// </summary>
    public Vector2 Place
    {
        get;
        init => field = float.IsFinite(value.X) && float.IsFinite(value.Y) ? value : throw new ArgumentOutOfRangeException(nameof(Place), value, "A hole's place is finite.");
    }

    /// <summary>How far from its middle a cell's middle lies to be a hole, in meters.</summary>
    public float Radius
    {
        get;
        init => field = value > 0f && float.IsFinite(value) ? value : throw new ArgumentOutOfRangeException(nameof(Radius), value, "A hole's radius is a finite number of meters above zero.");
    }

    internal override void Apply(Heightfield field, BuildContext context)
    {
        var half = field.Spacing * 0.5f;
        for (var row = 0; row < field.Rows - 1; row++)
        {
            for (var column = 0; column < field.Columns - 1; column++)
            {
                var way = field.Place(column, row) + new Vector2(half, half) - Place;
                if (MathF.Sqrt(way.X * way.X + way.Y * way.Y) < Radius) field.MakeHole(column, row);
            }
        }
    }
}
