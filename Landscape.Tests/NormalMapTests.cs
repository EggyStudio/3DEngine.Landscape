using System.Numerics;
using Xunit;

namespace Landscape.Tests;

// The field's normals as a texture, a texel a point, which lights a tile drawn far off at the
// field's own detail.
public class NormalMapTests
{
    private static Recipe Ground(Vector2 cone) => new(129, 97, 1f, [new Noise(3, 20f, 5, 4f), new Stamp(StampShape.Cone, cone, 15f, 12f)]);

    // A texel's bytes back to a way, each from nothing to 255 over minus one to one.
    private static Vector3 Decode(byte[] texels, int at) =>
        new(texels[at] / 127.5f - 1f, texels[at + 1] / 127.5f - 1f, texels[at + 2] / 127.5f - 1f);

    [Fact]
    public void Every_Texel_Is_The_Fields_Normal_At_Its_Point_To_A_Bytes_Rounding()
    {
        var field = Ground(Vector2.Zero).Build();
        var map = NormalMap.Of(field);

        Assert.Equal((129, 97), (map.Columns, map.Rows));
        Assert.Equal(129 * 97 * 4, map.Texels.Length);
        for (var row = 0; row < 97; row++)
        {
            for (var column = 0; column < 129; column++)
            {
                var at = (row * 129 + column) * 4;
                var (decoded, normal) = (Decode(map.Texels, at), field.Normal(column, row));
                Assert.True(MathF.Abs(decoded.X - normal.X) <= Rounding && MathF.Abs(decoded.Y - normal.Y) <= Rounding && MathF.Abs(decoded.Z - normal.Z) <= Rounding, $"{column}, {row} decodes to {decoded} where the normal is {normal}.");
                Assert.Equal(255, map.Texels[at + 3]);
            }
        }
    }

    [Fact]
    public void An_Update_Over_A_Change_Equals_A_Whole_Map_And_Touches_Nothing_Else()
    {
        var builder = new LandscapeBuilder(Ground(new(-30f, -20f)));
        var map = NormalMap.Of(builder.Field);
        var before = map.Texels.ToArray();
        var area = builder.Rebuild(Ground(new(-27f, -20f))).Changed!.Value;

        var updated = map.Update(builder.Field, area)!.Value;

        Assert.Equal(NormalMap.Of(builder.Field).Texels, map.Texels);
        Assert.Equal(new LandscapeArea(area.FirstColumn - 1, area.FirstRow - 1, area.LastColumn + 1, area.LastRow + 1), updated);
        for (var row = 0; row < 97; row++)
        {
            for (var column = 0; column < 129; column++)
            {
                if (column >= updated.FirstColumn && column <= updated.LastColumn && row >= updated.FirstRow && row <= updated.LastRow) continue;
                for (var channel = 0; channel < 4; channel++) Assert.Equal(before[(row * 129 + column) * 4 + channel], map.Texels[(row * 129 + column) * 4 + channel]);
            }
        }
    }

    [Fact]
    public void A_Map_At_Eight_Threads_Is_The_Map_Of_One()
    {
        var field = Ground(Vector2.Zero).Build();

        Assert.Equal(NormalMap.Of(field).Texels, NormalMap.Of(field, new BuildOptions(threads: 8)).Texels);
    }

    [Fact]
    public void An_Update_Of_Another_Field_Or_Past_The_Field_Is_Refused()
    {
        var map = NormalMap.Of(Ground(Vector2.Zero).Build());

        Assert.Throws<ArgumentException>("field", () => map.Update(new Heightfield(65, 65, 1f), new LandscapeArea(0, 0, 1, 1)));
        Assert.Throws<ArgumentOutOfRangeException>("area", () => map.Update(Ground(Vector2.Zero).Build(), new LandscapeArea(0, 0, 129, 1)));
    }

    // A byte spans two 255ths of a component, so a texel decodes within one 255th of the normal.
    private const float Rounding = 1f / 255f + 1e-6f;
}
