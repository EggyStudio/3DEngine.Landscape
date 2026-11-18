#:project ../Landscape/Landscape.csproj
#:property PublishAot=false
// Draws a recipe's field on the CPU as the pictures a reading is made of, each a PNG of the
// module's own tiles lit from one side on a white ground, which Tools' sheet.py lays out with a
// title and labels. It opens no window and uses no GPU.
//
//   dotnet run --no-cache build/reading.cs -- <folder> <recipe>
//
// The cached build of a file-based program does not follow a change to the library, so --no-cache
// builds it again.
//
// The recipe is one of the names below. It writes <recipe>-oblique.png, the field seen from its
// corner of least X and greatest Z at 35 degrees down, and <recipe>-above.png, seen from straight
// above with X to the right and Z down the picture, each lit by a sun 33 degrees high from the
// corner of least X and Z, the top left seen from above, and <recipe>-contours.png, a line each
// meter of height from above on white, every fifth darker, which shows the shape a single light
// hides where it falls along a slope.
using System.IO.Compression;
using System.Numerics;
using Landscape;

var recipes = new Dictionary<string, Recipe>
{
    // The tests' three stamps (Landscape.Tests/Fields.cs), a dome, a plateau flattened into it and
    // a crater added over the corner.
    ["stamps"] = new(129, 129, 1f,
    [
        new Stamp(StampShape.Dome, new(-10f, 5f), 45f, 18f),
        new Stamp(StampShape.Plateau, new(20f, -15f), 25f, 8f, StampBlend.Replace),
        new Stamp(StampShape.Crater, new(55f, 50f), 30f, 6f),
    ]),
    // Noise with a steep cone over it, before and after thermal erosion at 31 degrees.
    ["erosion-before"] = Erosion(eroded: false),
    ["erosion-after"] = Erosion(eroded: true),
    // Noise and two hills, before and after 150,000 droplets of hydraulic erosion.
    ["droplets-before"] = Droplets(eroded: false),
    ["droplets-after"] = Droplets(eroded: true),
    // The tests' ground of three stamps, noise and a steep cone (Landscape.Tests/SplatTests.cs),
    // drawn in the colors of the splat map's four materials.
    ["placement"] = Splat(),
    // Noise and a cone on 257 by 257 points, its tiles drawn at a step by their distance from the
    // near corner, every point near, every fourth beyond and every sixteenth far, with skirts.
    ["distance"] = Distance(),
    ["distance-bare"] = Distance(),
    // The distance recipe's field drawn whole at every point, at every 16th lit by its corners'
    // normals, and at every 16th lit by the normal map, the far-off tile's two lightings.
    ["far-fine"] = Distance(),
    ["far-corners"] = Distance(),
    ["far-mapped"] = Distance(),
    // A drawn picture of a ridge with a valley across it, 64 by 32 texels, stamped over 80 by 40 m
    // on noise and faded in over 8 m, and the same added over a dome.
    ["picture"] = new(129, 129, 1f,
    [
        new Noise(3, 40f, 5, 2f),
        new Stamp(StampShape.Dome, new(20f, 30f), 35f, 12f),
        new PictureStamp("ridge", new(0f, -20f), new(80f, 40f), 18f, StampBlend.Max, 8f),
        new PictureStamp("ridge", new(20f, 30f), new(40f, 20f), 6f, StampBlend.Add, 4f),
    ]),
    ["splat"] = Splat(),
};

// The splat recipe's materials as the tests give them, sand, grass, rock and snow, and a color
// each, linear, which the picture blends by the map's weights.
SplatRule[] rules =
[
    new("sand", highest: 1f, heightBlend: 1f, mostSteep: 0.5f, steepBlend: 0.2f),
    new("grass", lowest: 1f, highest: 12f, heightBlend: 1f, mostSteep: 0.5f, steepBlend: 0.2f),
    new("rock", leastSteep: 0.7f, steepBlend: 0.2f),
    new("snow", lowest: 14f, heightBlend: 2f, mostSteep: 0.7f, steepBlend: 0.2f),
];
Vector3[] colors = [new(0.75f, 0.62f, 0.38f), new(0.18f, 0.36f, 0.10f), new(0.30f, 0.28f, 0.27f), new(0.92f, 0.93f, 0.95f)];

if (args.Length != 2 || !recipes.TryGetValue(args[1], out var recipe))
{
    Console.Error.WriteLine($"Usage: dotnet run --no-cache build/reading.cs -- <folder> <{string.Join('|', recipes.Keys)}>");
    return 2;
}

Directory.CreateDirectory(args[0]);
var field = recipe.Build(new DrawnPictures());
var tiles = args[1] switch
{
    "distance" or "distance-bare" => Distant(field, skirted: args[1] == "distance"),
    "far-corners" or "far-mapped" => LandscapeGeometry.Tiles(field, step: 16, skirt: LandscapeGeometry.SkirtDepth(field, coarsest: 16)),
    _ => LandscapeGeometry.Tiles(field),
};

// The far-mapped recipe lights each pixel by the normal map at its place, bilinear between the
// texels, where every other recipe lights it by its triangle's corners' normals.
var normals = args[1] == "far-mapped" ? NormalMap.Of(field) : null;
Vector3? NormalAt(Vector3 place)
{
    if (normals is null) return null;
    var x = Math.Clamp(place.X / field.Spacing + (field.Columns - 1) * 0.5f, 0f, field.Columns - 1.001f);
    var z = Math.Clamp(place.Z / field.Spacing + (field.Rows - 1) * 0.5f, 0f, field.Rows - 1.001f);
    var (c, r) = ((int)x, (int)z);
    var (u, v) = (x - c, z - r);
    Vector3 Texel(int column, int row)
    {
        var at = (row * field.Columns + column) * 4;
        return new Vector3(normals.Texels[at], normals.Texels[at + 1], normals.Texels[at + 2]) / 127.5f - Vector3.One;
    }

    var near = Vector3.Lerp(Texel(c, r), Texel(c + 1, r), u);
    var far = Vector3.Lerp(Texel(c, r + 1), Texel(c + 1, r + 1), u);
    return Vector3.Normalize(Vector3.Lerp(near, far, v));
}
var splat = args[1] is "splat" or "placement" ? SplatMap.Of(field, rules) : null;

// The placement recipe's trees, a place every 1.5 m jittered from a hash of its number, kept on
// grass below 12 m and no steeper than 0.4 by the grass's weight as their density, each marking
// its nearest point.
var trees = new HashSet<(int, int)>();
if (args[1] == "placement")
{
    var places = Enumerable.Range(0, 85 * 85).Select(i =>
    {
        // A hash of the number by Murmur3's finalizer, whose bits do not follow the number's.
        var hash = (uint)i * 2654435761u;
        hash ^= hash >> 16;
        hash *= 0x85EBCA6Bu;
        hash ^= hash >> 13;
        hash *= 0xC2B2AE35u;
        hash ^= hash >> 16;
        var (x, z) = ((hash & 0xFFFF) / 65536f - 0.5f, (hash >> 16) / 65536f - 0.5f);
        return (new Vector3(-63f + 1.5f * (i % 85 + x), 0f, -63f + 1.5f * (i / 85 + z)), hash);
    });
    var rule = new PlacementRule(lowest: 1f, highest: 12f, heightBlend: 1f, mostSteep: 0.4f, steepBlend: 0.1f);
    foreach (var planted in Placement.Keep(field, places, rule, DensityMap.Of(splat!, 1)))
        trees.Add(((int)MathF.Round(planted.Place.X / field.Spacing + (field.Columns - 1) * 0.5f), (int)MathF.Round(planted.Place.Z / field.Spacing + (field.Rows - 1) * 0.5f)));
}

// A corner's color, the splat map's materials blended by their weights at its point, the step's
// tint of the distance recipe's tiles, or a dry ground's.
Vector3 Tint(Vector3 corner)
{
    if (args[1].StartsWith("distance", StringComparison.Ordinal))
    {
        var step = StepAt(field, corner);
        return step == 1 ? new Vector3(0.62f, 0.55f, 0.42f) : step == 4 ? new Vector3(0.42f, 0.52f, 0.62f) : new Vector3(0.62f, 0.42f, 0.42f);
    }

    if (splat is null) return new Vector3(0.62f, 0.55f, 0.42f);
    var column = (int)MathF.Round(corner.X / field.Spacing + (field.Columns - 1) * 0.5f);
    var row = (int)MathF.Round(corner.Z / field.Spacing + (field.Rows - 1) * 0.5f);
    if (trees.Contains((column, row))) return new Vector3(0.03f, 0.10f, 0.03f);
    var color = Vector3.Zero;
    for (var m = 0; m < colors.Length; m++) color += splat.Weight(m, column, row) * colors[m];
    return color;
}
var sun = Vector3.Normalize(new Vector3(-0.6f, 0.55f, -0.6f));
foreach (var (view, yaw, pitch, wide) in new[] { ("oblique", 45f, 35f, 720), ("above", 0f, 90f, 440) })
{
    var path = Path.Combine(args[0], $"{args[1]}-{view}.png");
    WritePng(path, Draw(tiles, Tint, NormalAt, yaw * MathF.PI / 180f, pitch * MathF.PI / 180f, sun, wide));
    Console.WriteLine(path);
}

var contours = Path.Combine(args[0], $"{args[1]}-contours.png");
WritePng(contours, Contours(field, 440, 1f));
Console.WriteLine(contours);
return 0;

// The distance recipe's tiles, each at the step its distance from the near corner gives, every
// skirt hung for the coarsest of them, or none for the bare recipe that shows the cracks.
static IReadOnlyList<LandscapeMesh> Distant(Heightfield field, bool skirted)
{
    var skirt = skirted ? LandscapeGeometry.SkirtDepth(field, coarsest: 16) : 0f;
    var tiles = new List<LandscapeMesh>();
    for (var row = 0; row < 4; row++)
    {
        for (var column = 0; column < 4; column++)
            tiles.Add(LandscapeGeometry.Tile(field, column, row, step: StepOf(column, row), skirt: skirt));
    }

    return tiles;
}

// A tile's step by its distance from the near corner, the tile of least X and greatest Z.
static int StepOf(int column, int row) => Math.Max(column, 3 - row) switch { 0 or 1 => 1, 2 => 4, _ => 16 };

// The step of the tile a corner lies in, a corner on an edge taken into the nearer tile.
static int StepAt(Heightfield field, Vector3 corner)
{
    var (x, z) = (corner.X / field.Spacing + 128f, corner.Z / field.Spacing + 128f);
    var (column, row) = (Math.Min((int)(x / 64f), 3), Math.Min((int)(z / 64f), 3));
    return StepOf(column, row);
}

static Recipe Distance() => new(257, 257, 1f, [new Noise(4, 40f, 6, 6f), new Stamp(StampShape.Cone, new(-20f, 10f), 50f, 30f)]);

static Recipe Splat() => new(129, 129, 1f,
[
    new Stamp(StampShape.Dome, new(-10f, 5f), 45f, 18f),
    new Stamp(StampShape.Plateau, new(20f, -15f), 25f, 8f, StampBlend.Replace),
    new Stamp(StampShape.Crater, new(55f, 50f), 30f, 6f),
    new Noise(9, 24f, 4, 1.5f),
    new Stamp(StampShape.Cone, new(-40f, -40f), 15f, 20f),
]);

static Recipe Droplets(bool eroded) => new(193, 193, 1f,
[
    new Noise(12, 80f, 7, 10f, 0.55f),
    new Stamp(StampShape.Dome, new(-30f, -20f), 60f, 40f),
    new Stamp(StampShape.Cone, new(45f, 40f), 40f, 30f),
    .. eroded ? new LandscapeStep[] { new HydraulicErosion(8, 150_000) } : [],
]);

static Recipe Erosion(bool eroded) => new(129, 129, 1f,
[
    new Noise(4, 64f, 6, 12f),
    new Stamp(StampShape.Cone, new(15f, 10f), 30f, 36f),
    .. eroded ? new LandscapeStep[] { new ThermalErosion(0.6f, 600) } : [],
]);

// The field from above, X to the right and Z down, a dark line where a pixel and the one to its
// right or below it fall between different meters of height, and darker every fifth.
static (int Width, int Height, byte[] Rgb) Contours(Heightfield field, int width, float interval)
{
    var size = field.Size;
    var height = (int)MathF.Round(width * size.Y / size.X);
    var levels = new int[width * height];
    for (var y = 0; y < height; y++)
    {
        for (var x = 0; x < width; x++)
        {
            var place = new Vector2((x + 0.5f) / width * size.X - size.X / 2f, (y + 0.5f) / height * size.Y - size.Y / 2f);
            levels[y * width + x] = (int)MathF.Floor(field.Height(place) / interval);
        }
    }

    var rgb = new byte[width * height * 3];
    Array.Fill(rgb, (byte)255);
    for (var y = 0; y < height; y++)
    {
        for (var x = 0; x < width; x++)
        {
            var at = y * width + x;
            var crossed = int.MinValue;
            if (x + 1 < width && levels[at + 1] != levels[at]) crossed = Math.Max(levels[at + 1], levels[at]);
            if (y + 1 < height && levels[at + width] != levels[at]) crossed = Math.Max(crossed, Math.Max(levels[at + width], levels[at]));
            if (crossed == int.MinValue) continue;
            var shade = (byte)(crossed % 5 == 0 ? 40 : 150);
            (rgb[at * 3], rgb[at * 3 + 1], rgb[at * 3 + 2]) = (shade, shade, shade);
        }
    }

    return (width, height, rgb);
}

// The tiles' triangles drawn by a depth buffer in an orthographic view turned about up by the yaw
// and tipped down by the pitch, each pixel lit by its corners' normals, on white.
static (int Width, int Height, byte[] Rgb) Draw(IReadOnlyList<LandscapeMesh> tiles, Func<Vector3, Vector3> tint, Func<Vector3, Vector3?> normalAt, float yaw, float pitch, Vector3 sun, int width)
{
    var turn = Matrix4x4.CreateRotationY(yaw) * Matrix4x4.CreateRotationX(pitch);
    var corners = tiles.Select(tile => tile.Positions.Select(p => Vector3.Transform(p, turn)).ToArray()).ToArray();
    var all = corners.SelectMany(c => c).ToArray();
    var (left, right) = (all.Min(p => p.X), all.Max(p => p.X));
    var (bottom, top) = (all.Min(p => p.Y), all.Max(p => p.Y));
    const int margin = 16;
    var scale = (width - 2 * margin) / (right - left);
    var height = (int)MathF.Ceiling((top - bottom) * scale) + 2 * margin;
    var depth = new float[width * height];
    Array.Fill(depth, float.NegativeInfinity);
    var rgb = new byte[width * height * 3];
    Array.Fill(rgb, (byte)255);

    for (var t = 0; t < tiles.Count; t++)
    {
        var (tile, turned) = (tiles[t], corners[t]);
        var screen = turned.Select(p => new Vector3(margin + (p.X - left) * scale, margin + (top - p.Y) * scale, p.Z)).ToArray();
        var tints = tile.Positions.Select(tint).ToArray();
        for (var i = 0; i < tile.Indices.Length; i += 3)
        {
            var (a, b, c) = (tile.Indices[i], tile.Indices[i + 1], tile.Indices[i + 2]);
            var (pa, pb, pc) = (screen[a], screen[b], screen[c]);
            var area = Edge(pa, pb, pc);
            if (area == 0f) continue;
            var (x0, x1) = ((int)MathF.Max(0, MathF.Floor(MathF.Min(pa.X, MathF.Min(pb.X, pc.X)))), (int)MathF.Min(width - 1, MathF.Ceiling(MathF.Max(pa.X, MathF.Max(pb.X, pc.X)))));
            var (y0, y1) = ((int)MathF.Max(0, MathF.Floor(MathF.Min(pa.Y, MathF.Min(pb.Y, pc.Y)))), (int)MathF.Min(height - 1, MathF.Ceiling(MathF.Max(pa.Y, MathF.Max(pb.Y, pc.Y)))));
            for (var y = y0; y <= y1; y++)
            {
                for (var x = x0; x <= x1; x++)
                {
                    var p = new Vector3(x + 0.5f, y + 0.5f, 0f);
                    var (wa, wb, wc) = (Edge(pb, pc, p) / area, Edge(pc, pa, p) / area, Edge(pa, pb, p) / area);
                    if (wa < 0f || wb < 0f || wc < 0f) continue;
                    var z = wa * pa.Z + wb * pb.Z + wc * pc.Z;
                    // The view looks down the turned frame's negative Z, so the nearest is the
                    // largest.
                    if (z <= depth[y * width + x]) continue;
                    depth[y * width + x] = z;
                    var normal = normalAt(wa * tile.Positions[a] + wb * tile.Positions[b] + wc * tile.Positions[c])
                        ?? Vector3.Normalize(wa * tile.Normals[a] + wb * tile.Normals[b] + wc * tile.Normals[c]);
                    var ground = wa * tints[a] + wb * tints[b] + wc * tints[c];
                    var lit = ground * (0.3f + 0.7f * MathF.Max(0f, Vector3.Dot(normal, sun)));
                    var at = (y * width + x) * 3;
                    (rgb[at], rgb[at + 1], rgb[at + 2]) = (Byte(lit.X), Byte(lit.Y), Byte(lit.Z));
                }
            }
        }
    }

    return (width, height, rgb);
}

static float Edge(Vector3 a, Vector3 b, Vector3 p) => (b.X - a.X) * (p.Y - a.Y) - (b.Y - a.Y) * (p.X - a.X);

// A linear share as an sRGB byte, by sRGB's own curve.
static byte Byte(float linear)
{
    var v = Math.Clamp(linear, 0f, 1f);
    var encoded = v <= 0.0031308f ? 12.92f * v : 1.055f * MathF.Pow(v, 1f / 2.4f) - 0.055f;
    return (byte)MathF.Round(255f * encoded);
}

// A PNG of 8-bit RGB, each row with no filter, deflated whole.
static void WritePng(string path, (int Width, int Height, byte[] Rgb) picture)
{
    using var file = File.Create(path);
    file.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
    var header = new byte[13];
    System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(header, picture.Width);
    System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), picture.Height);
    (header[8], header[9]) = (8, 2);
    Chunk(file, "IHDR", header);
    using var packed = new MemoryStream();
    using (var deflate = new ZLibStream(packed, CompressionLevel.SmallestSize, leaveOpen: true))
    {
        for (var y = 0; y < picture.Height; y++)
        {
            deflate.WriteByte(0);
            deflate.Write(picture.Rgb, y * picture.Width * 3, picture.Width * 3);
        }
    }

    Chunk(file, "IDAT", packed.ToArray());
    Chunk(file, "IEND", []);
}

static void Chunk(Stream file, string kind, byte[] data)
{
    Span<byte> word = stackalloc byte[4];
    System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(word, data.Length);
    file.Write(word);
    var named = System.Text.Encoding.ASCII.GetBytes(kind).Concat(data).ToArray();
    file.Write(named);
    System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(word, Crc(named));
    file.Write(word);
}

// The CRC-32 a PNG chunk ends with, over its kind and its data.
static uint Crc(byte[] bytes)
{
    var crc = 0xFFFFFFFFu;
    foreach (var b in bytes)
    {
        crc ^= b;
        for (var k = 0; k < 8; k++) crc = (crc & 1) != 0 ? 0xEDB88320u ^ (crc >> 1) : crc >> 1;
    }

    return ~crc;
}

// The picture recipe's one picture, a ridge along X with a valley cut across it, drawn as a
// function of the texel, which an engine would read from a file.
sealed class DrawnPictures : ILandscapePictures
{
    public HeightPicture? Heights(string name)
    {
        if (name != "ridge") return null;
        var shares = new float[64 * 32];
        for (var z = 0; z < 32; z++)
        {
            for (var x = 0; x < 64; x++)
            {
                var across = (z - 15.5f) / 15.5f;
                var ridge = MathF.Max(0f, 1f - across * across * 1.6f);
                var valley = MathF.Min(1f, MathF.Abs(x - 40f) / 6f);
                shares[z * 64 + x] = ridge * (0.35f + 0.65f * valley);
            }
        }

        return new HeightPicture(64, 32, shares);
    }
}
