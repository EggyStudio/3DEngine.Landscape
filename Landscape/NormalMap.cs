namespace Landscape;

/// <summary>
/// The field's normals as a texture of RGBA bytes, a texel a point, which lights a tile drawn far
/// off at a coarse step by the field's own normals between its corners.
/// </summary>
/// <remarks>
/// <para>
/// A texel holds the way up out of the ground at its point in the field's own axes, X in red, Y in
/// green and Z in blue, each from minus one at nothing to one at 255, rounded to the nearest byte,
/// and 255 in alpha, so an engine decodes a texel as its bytes over 127.5 less one and lights by
/// it in the field's axes. A texel a point lies as the splat map's layers lie, a row at a time
/// along X and the rows along Z, and an engine samples it with a tile's texture coordinates moved
/// by the same half texel.
/// </para>
/// <para>
/// A tile at a step of 16 carries one corner in 256 and their normals, so the ridges between its
/// corners are lit as one slope, where the map gives every point's.
/// </para>
/// </remarks>
public sealed class NormalMap
{
    private NormalMap(Heightfield field, BuildOptions options)
    {
        (Columns, Rows) = (field.Columns, field.Rows);
        Texels = new byte[field.Heights.Length * 4];
        Write(field, 0, 0, Columns - 1, Rows - 1, options);
    }

    /// <summary>How many points a row has along X, the texture's width in texels.</summary>
    public int Columns { get; }

    /// <summary>
    /// How many rows of points there are along Z, the texture's height in texels.
    /// </summary>
    public int Rows { get; }

    /// <summary>
    /// The texels, four bytes each, a row at a time along X and the rows along Z.
    /// </summary>
    public byte[] Texels { get; }

    /// <summary>Makes a field's normal map.</summary>
    /// <param name="field">The field.</param>
    public static NormalMap Of(Heightfield field) => Of(field, BuildOptions.Default);

    /// <summary>Makes a field's normal map across the threads the options allow.</summary>
    /// <param name="field">The field.</param>
    /// <param name="options">The threads the map's rows may be written on.</param>
    public static NormalMap Of(Heightfield field, BuildOptions options)
    {
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(options);
        return new NormalMap(field, options);
    }

    /// <summary>
    /// Writes again the texels of a changed area and those beside it, the map holding then what a
    /// whole map of the field holds.
    /// </summary>
    /// <param name="field">The field the map is of, as it stands after the change.</param>
    /// <param name="area">
    /// The points whose heights changed, as a rebuild's <see cref="LandscapeChange.Changed"/> gives
    /// them.
    /// </param>
    /// <returns>The rectangle of texels written again, which an engine uploads.</returns>
    /// <exception cref="ArgumentException">The field is not of the map's size.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The area reaches past the field.</exception>
    /// <remarks>
    /// A point's normal reads the points beside it, so the area is grown by a point each way, cut
    /// to the field, as <see cref="SplatMap.Update"/> grows its own.
    /// </remarks>
    public LandscapeArea? Update(Heightfield field, LandscapeArea area)
    {
        ArgumentNullException.ThrowIfNull(field);
        if (field.Columns != Columns || field.Rows != Rows)
            throw new ArgumentException($"The normal map is of {Columns} by {Rows} points and the field of {field.Columns} by {field.Rows}.", nameof(field));
        if (area.FirstColumn < 0 || area.FirstRow < 0 || area.LastColumn >= Columns || area.LastRow >= Rows || area.FirstColumn > area.LastColumn || area.FirstRow > area.LastRow)
            throw new ArgumentOutOfRangeException(nameof(area), area, "The area lies within the field, its first point at or before its last.");
        var grown = new LandscapeArea(
            Math.Max(area.FirstColumn - 1, 0), Math.Max(area.FirstRow - 1, 0),
            Math.Min(area.LastColumn + 1, Columns - 1), Math.Min(area.LastRow + 1, Rows - 1));
        Write(field, grown.FirstColumn, grown.FirstRow, grown.LastColumn, grown.LastRow, BuildOptions.Default);
        return grown;
    }

    // The texels of a rectangle, each point's normal alone, so its rows run on any thread.
    private void Write(Heightfield field, int firstColumn, int firstRow, int lastColumn, int lastRow, BuildOptions options)
    {
        options.Rows(firstRow, lastRow, row =>
        {
            for (var column = firstColumn; column <= lastColumn; column++)
            {
                var normal = field.Normal(column, row);
                var at = (row * Columns + column) * 4;
                (Texels[at], Texels[at + 1], Texels[at + 2], Texels[at + 3]) = (Byte(normal.X), Byte(normal.Y), Byte(normal.Z), 255);
            }
        });
    }

    // A component from minus one to one as the nearest of 256 steps, a half rounding up.
    private static byte Byte(float component) => (byte)Math.Clamp((int)MathF.Floor((component + 1f) * 127.5f + 0.5f), 0, 255);
}
