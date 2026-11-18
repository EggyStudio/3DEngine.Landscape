namespace Landscape;

/// <summary>How a <see cref="Stamp"/> meets the heights the steps before it left.</summary>
/// <remarks>Each blend changes the points inside the stamp's radius alone.</remarks>
public enum StampBlend
{
    /// <summary>The stamp's height is added to the ground's.</summary>
    Add,

    /// <summary>The ground takes the higher of its own height and the stamp's.</summary>
    Max,

    /// <summary>The ground takes the lower of its own height and the stamp's.</summary>
    Min,

    /// <summary>
    /// The ground is drawn toward the stamp's whole height by the share its profile gives, so the
    /// middle of a plateau stands at the height and its edge keeps the ground's, as a flattened
    /// place for a building is made.
    /// </summary>
    Replace,
}
