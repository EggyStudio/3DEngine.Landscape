using System.Numerics;
using Xunit;

namespace Landscape.Tests;

// A ray met with the ground, as an editor places a stamp where the pointer falls and a game drops
// what falls, against the triangles the tiles draw.
public class RaycastTests
{
    [Fact]
    public void A_Ray_Straight_Down_Meets_A_Dome_At_Its_Height()
    {
        var field = new Recipe(65, 65, 1f, [new Stamp(StampShape.Dome, Vector2.Zero, 20f, 10f)]).Build();
        var hit = field.Raycast(new Vector3(3f, 100f, -2f), -Vector3.UnitY);

        Assert.NotNull(hit);
        // The place lies along the ray, its origin and the distance times its way, which rounds a
        // few units of the last place off the point's own height.
        Assert.Equal(3f, hit.Value.Place.X);
        Assert.Equal(-2f, hit.Value.Place.Z);
        Assert.Equal(field[35, 30], hit.Value.Place.Y, 1e-4f);
        Assert.Equal(100f - field[35, 30], hit.Value.Distance, 1e-4f);
        Assert.Equal(field.Normal(new Vector2(3f, -2f)), hit.Value.Normal);
    }

    [Fact]
    public void A_Slanted_Ray_Meets_A_Ramp_Where_The_Arithmetic_Puts_It()
    {
        // A ramp rising half a meter a meter along X, met by a ray falling at 45 degrees along X
        // from 20 m up at X -10, which meets it where 20 less t equals half of -10 plus t, at t of
        // 50 thirds.
        var ramp = new Heightfield(33, 33, 1f);
        for (var row = 0; row < 33; row++)
        {
            for (var column = 0; column < 33; column++) ramp[column, row] = 0.5f * ramp.Place(column, row).X;
        }

        var hit = ramp.Raycast(new Vector3(-10f, 20f, 3f), Vector3.Normalize(new Vector3(1f, -1f, 0f)));

        Assert.NotNull(hit);
        Assert.Equal(-10f + 50f / 3f, hit.Value.Place.X, 1e-4f);
        Assert.Equal(20f - 50f / 3f, hit.Value.Place.Y, 1e-4f);
        Assert.Equal(3f, hit.Value.Place.Z, 1e-4f);
        Assert.Equal(50f / 3f * MathF.Sqrt(2f), hit.Value.Distance, 1e-3f);
    }

    [Fact]
    public void A_Ray_Above_Everything_Or_Leaving_The_Field_Or_Out_Of_Reach_Misses()
    {
        var field = new Recipe(33, 33, 1f, [new Noise(1, 8f, 3, 2f)]).Build();

        Assert.Null(field.Raycast(new Vector3(-30f, 50f, 0f), Vector3.UnitX));
        Assert.Null(field.Raycast(new Vector3(-30f, 1f, 0f), -Vector3.UnitX));
        Assert.Null(field.Raycast(new Vector3(0f, 50f, 0f), -Vector3.UnitY, reach: 10f));
        Assert.NotNull(field.Raycast(new Vector3(-30f, 20f, 0f), Vector3.Normalize(new Vector3(1f, -0.5f, 0f))));
    }

    [Fact]
    public void A_Hit_Is_The_Nearest_Crossing_Of_Every_Drawn_Triangle()
    {
        var field = new Recipe(65, 65, 1f, [new Noise(8, 12f, 4, 5f), new Stamp(StampShape.Cone, new(5f, -3f), 15f, 12f)]).Build();
        var triangles = LandscapeGeometry.Tiles(field, 16).SelectMany(tile => Enumerable.Range(0, tile.Indices.Length / 3)
            .Select(t => (tile.Positions[tile.Indices[3 * t]], tile.Positions[tile.Indices[3 * t + 1]], tile.Positions[tile.Indices[3 * t + 2]]))).ToArray();
        var random = new Random(4);
        for (var ray = 0; ray < 60; ray++)
        {
            var origin = new Vector3(random.NextSingle() * 80f - 40f, 15f + random.NextSingle() * 20f, random.NextSingle() * 80f - 40f);
            var toward = new Vector3(random.NextSingle() * 50f - 25f, random.NextSingle() * 8f - 6f, random.NextSingle() * 50f - 25f);
            var way = Vector3.Normalize(toward - origin);

            var nearest = triangles.Select(t => Crossing(origin, way, t)).Where(d => d is not null).Min();
            var hit = field.Raycast(origin, way);
            if (nearest is null)
            {
                Assert.Null(hit);
                continue;
            }

            Assert.NotNull(hit);
            Assert.Equal(nearest.Value, hit.Value.Distance, 1e-3f);
            Assert.True(Vector3.Distance(origin + way * hit.Value.Distance, hit.Value.Place) < 1e-3f);
        }
    }

    [Fact]
    public void A_Ray_Of_No_Way_Is_Refused()
    {
        var field = new Heightfield(3, 3, 1f);

        Assert.Throws<ArgumentException>("direction", () => field.Raycast(Vector3.Zero, Vector3.Zero));
        Assert.Throws<ArgumentException>("origin", () => field.Raycast(new Vector3(float.NaN, 0f, 0f), Vector3.UnitY));
    }

    // Where a ray crosses a triangle, by Möller and Trumbore's rule from either side, or nothing.
    private static float? Crossing(Vector3 origin, Vector3 way, (Vector3 A, Vector3 B, Vector3 C) t)
    {
        var (e1, e2) = (t.B - t.A, t.C - t.A);
        var p = Vector3.Cross(way, e2);
        var det = Vector3.Dot(e1, p);
        if (MathF.Abs(det) < 1e-9f) return null;
        var s = origin - t.A;
        var u = Vector3.Dot(s, p) / det;
        if (u < 0f || u > 1f) return null;
        var q = Vector3.Cross(s, e1);
        var v = Vector3.Dot(way, q) / det;
        if (v < 0f || u + v > 1f) return null;
        var distance = Vector3.Dot(e2, q) / det;
        return distance >= 0f ? distance : null;
    }
}
