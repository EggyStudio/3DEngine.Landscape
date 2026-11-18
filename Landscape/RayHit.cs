using System.Numerics;

namespace Landscape;

/// <summary>Where a ray met the ground, how far along it and the ground's way up there.</summary>
public readonly record struct RayHit
{
    /// <summary>A hit of its place, its distance and the ground's normal.</summary>
    /// <param name="place">
    /// Where the ray met the ground, in meters about the field's middle.
    /// </param>
    /// <param name="distance">How far along the ray from its origin, in meters.</param>
    /// <param name="normal">
    /// The way up out of the ground there, at unit length, the field's own.
    /// </param>
    public RayHit(Vector3 place, float distance, Vector3 normal) => (Place, Distance, Normal) = (place, distance, normal);

    /// <summary>Where the ray met the ground, in meters about the field's middle.</summary>
    public Vector3 Place { get; init; }

    /// <summary>How far along the ray from its origin, in meters.</summary>
    public float Distance { get; init; }

    /// <summary>The way up out of the ground there, at unit length, the field's own.</summary>
    public Vector3 Normal { get; init; }
}
