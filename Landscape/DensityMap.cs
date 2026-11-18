using System.Numerics;

namespace Landscape;

/// <summary>
/// How densely a kind of plant grows over a field, a share from nothing to one at each of the
/// field's points, painted by an engine's editor or taken from a splat map's material.
/// </summary>
/// <remarks>
/// The shares lie a row at a time, each row along X and the rows along Z, as the field's heights
/// do, and between the points the density is bilinear, so a map painted a point a texel lines up
/// with the field it was painted over.
/// </remarks>
public sealed class DensityMap
{
    /// <summary>A map of shares an engine gives, a point each.</summary>
    /// <param name="columns">How many points a row has along X, the field's.</param>
    /// <param name="rows">How many rows of points there are along Z, the field's.</param>
    /// <param name="shares">The shares, a row at a time, each from nothing to one.</param>
    /// <exception cref="ArgumentOutOfRangeException">A count is below two.</exception>
    /// <exception cref="ArgumentException">
    /// The shares are not a point each, or one is not a number from nothing to one.
    /// </exception>
    public DensityMap(int columns, int rows, float[] shares)
    {
        ArgumentNullException.ThrowIfNull(shares);
        ArgumentOutOfRangeException.ThrowIfLessThan(columns, 2);
        ArgumentOutOfRangeException.ThrowIfLessThan(rows, 2);
        if (shares.Length != columns * rows) throw new ArgumentException($"A density map of {columns} by {rows} points holds {columns * rows} shares, not {shares.Length}.", nameof(shares));
        if (shares.Any(share => !(share is >= 0f and <= 1f))) throw new ArgumentException("A density map's shares are each from nothing to one.", nameof(shares));
        (Columns, Rows, Shares) = (columns, rows, [.. shares]);
    }

    /// <summary>How many points a row has along X.</summary>
    public int Columns { get; }

    /// <summary>How many rows of points there are along Z.</summary>
    public int Rows { get; }

    /// <summary>The shares, a row at a time, each row along X and the rows along Z.</summary>
    public IReadOnlyList<float> Shares { get; }

    /// <summary>
    /// A map of one material's weights in a splat map, the plant growing where the material lies.
    /// </summary>
    /// <param name="splat">The splat map.</param>
    /// <param name="material">The material's place among the map's materials.</param>
    /// <exception cref="ArgumentOutOfRangeException">The map has no such material.</exception>
    public static DensityMap Of(SplatMap splat, int material)
    {
        ArgumentNullException.ThrowIfNull(splat);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)material, (uint)splat.Materials.Count, nameof(material));
        var shares = new float[splat.Columns * splat.Rows];
        for (var row = 0; row < splat.Rows; row++)
        {
            for (var column = 0; column < splat.Columns; column++)
                shares[row * splat.Columns + column] = Math.Clamp(splat.Weight(material, column, row), 0f, 1f);
        }

        return new DensityMap(splat.Columns, splat.Rows, shares);
    }

    /// <summary>
    /// The density at a place over a field of the map's size, bilinear between its points.
    /// </summary>
    /// <param name="field">The field the map is of, which gives where its points lie.</param>
    /// <param name="place">
    /// The place across the ground, X in X and Z in Y, about the field's middle.
    /// </param>
    /// <exception cref="ArgumentException">The field is not of the map's size.</exception>
    public float At(Heightfield field, Vector2 place)
    {
        ArgumentNullException.ThrowIfNull(field);
        if (field.Columns != Columns || field.Rows != Rows) throw new ArgumentException($"The density map is of {Columns} by {Rows} points and the field of {field.Columns} by {field.Rows}.", nameof(field));
        var x = Math.Clamp(place.X / field.Spacing + (Columns - 1) * 0.5f, 0f, Columns - 1);
        var z = Math.Clamp(place.Y / field.Spacing + (Rows - 1) * 0.5f, 0f, Rows - 1);
        var (column, row) = (Math.Min((int)x, Columns - 2), Math.Min((int)z, Rows - 2));
        var (across, along) = (x - column, z - row);
        float At(int c, int r) => Shares[r * Columns + c];
        var near = At(column, row) + (At(column + 1, row) - At(column, row)) * across;
        var far = At(column, row + 1) + (At(column + 1, row + 1) - At(column, row + 1)) * across;
        return near + (far - near) * along;
    }
}
