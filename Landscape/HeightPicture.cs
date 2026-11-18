namespace Landscape;

/// <summary>
/// A picture's heights as shares of a stamp's height, a row at a time, which an engine reads from
/// a file for <see cref="ILandscapePictures"/>.
/// </summary>
/// <remarks>
/// The first row lies at the stamp's least Z and each row runs along X, the order an image file is
/// read in from its top, so a picture seen from above with Z down the screen stands as it is drawn.
/// A share is usually an image's gray from nothing to one, and any finite number is taken.
/// </remarks>
public sealed record HeightPicture
{
    /// <summary>A picture of a width and a height in texels and its shares.</summary>
    /// <param name="width">How many texels a row has, two or more.</param>
    /// <param name="height">How many rows it has, two or more.</param>
    /// <param name="shares">The shares, a row at a time, a texel each.</param>
    /// <exception cref="ArgumentOutOfRangeException">A count is below two.</exception>
    /// <exception cref="ArgumentException">
    /// The shares are not a texel each, or one is not finite.
    /// </exception>
    public HeightPicture(int width, int height, float[] shares)
    {
        ArgumentNullException.ThrowIfNull(shares);
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 2);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 2);
        if (shares.Length != width * height) throw new ArgumentException($"A picture of {width} by {height} texels holds {width * height} shares, not {shares.Length}.", nameof(shares));
        if (shares.Any(share => !float.IsFinite(share))) throw new ArgumentException("A picture's shares are finite.", nameof(shares));
        (Width, Height, Shares) = (width, height, [.. shares]);
    }

    /// <summary>How many texels a row has.</summary>
    public int Width { get; }

    /// <summary>How many rows it has.</summary>
    public int Height { get; }

    /// <summary>The shares, a row at a time from the stamp's least Z.</summary>
    public IReadOnlyList<float> Shares { get; }

    /// <summary>
    /// The share at a place across the picture, from nothing at its first texel's middle to one at
    /// its last's along each way, bilinear between the texels.
    /// </summary>
    /// <param name="u">How far along X, from nothing to one.</param>
    /// <param name="v">How far along Z, from nothing to one.</param>
    public float At(float u, float v)
    {
        return Sample(u, v);
    }

    // The share at a place across a rectangle the picture is turned within by quarter turns, a
    // quarter taking its first row, along X, to run along Z at the rectangle's greatest X.
    internal float At(float u, float v, int turns) => turns switch
    {
        0 => Sample(u, v),
        1 => Sample(v, 1f - u),
        2 => Sample(1f - u, 1f - v),
        _ => Sample(1f - v, u),
    };

    private float Sample(float u, float v)
    {
        var x = Math.Clamp(u, 0f, 1f) * (Width - 1);
        var z = Math.Clamp(v, 0f, 1f) * (Height - 1);
        var (column, row) = (Math.Min((int)x, Width - 2), Math.Min((int)z, Height - 2));
        var (across, along) = (x - column, z - row);
        float At(int c, int r) => Shares[r * Width + c];
        var near = At(column, row) + (At(column + 1, row) - At(column, row)) * across;
        var far = At(column, row + 1) + (At(column + 1, row + 1) - At(column, row + 1)) * across;
        return near + (far - near) * along;
    }
}
