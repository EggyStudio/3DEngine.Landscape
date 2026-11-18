namespace Landscape;

/// <summary>
/// The profile a <see cref="Stamp"/> rises by from its radius to its middle, as a share of its
/// height.
/// </summary>
public enum StampShape
{
    /// <summary>
    /// A straight rise from nothing at the radius to the whole height at the middle.
    /// </summary>
    Cone,

    /// <summary>
    /// A round top that meets the ground with no edge, one less the square of the distance over the
    /// radius, squared.
    /// </summary>
    Dome,

    /// <summary>
    /// A flat top across the inner half of the radius and a smooth fall from there to nothing at
    /// the radius.
    /// </summary>
    Plateau,

    /// <summary>
    /// A rim at the whole height seven tenths of the way out, falling smoothly to nothing at the
    /// radius outside and to a bowl four tenths of the height below the ground at the middle.
    /// </summary>
    Crater,
}
