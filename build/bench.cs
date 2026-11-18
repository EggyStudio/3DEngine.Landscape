#:project ../Landscape/Landscape.csproj
#:property PublishAot=false
// Times a recipe of every step the module has at fields of 513, 1,025, 2,049 and 4,097 points a
// side, a step at a time, with the bytes each allocates, and then the tiles, the splat map and a
// rebuild after the last step changes, the work an engine asks for after a build. It writes a
// Markdown table to the standard output and a JSON file of the same numbers for the sheet. It uses
// no GPU, and it is a timing, so it runs under the machine's exclusive lock (NORM.md, N 7.9).
//
//   dotnet run -c Release --no-cache build/bench.cs -- <numbers.json> [sides...]
//
// Each step's numbers scale with the points, the droplets a quarter a point and the passes fixed,
// so a step's time and bytes a point stay level where its cost is in proportion to the field. A
// step is timed as a builder's rebuild that adds it, less the rebuild's own overhead. A first pass
// at 513 points a side runs every stage and is not reported, so the fields measured run the code
// the runtime has optimized, as an engine's would after its first build.
using System.Diagnostics;
using System.Numerics;
using System.Text.Json;
using Landscape;

if (args.Length < 1)
{
    Console.Error.WriteLine("Usage: dotnet run -c Release --no-cache build/bench.cs -- <numbers.json> [sides...]");
    return 2;
}

int[] sides = args.Length > 1 ? [.. args[1..].Select(int.Parse)] : [513, 1025, 2049, 4097];
var warming = true;
var rows = new List<Row>();
Console.WriteLine("| Side | Stage | ms | MB allocated | ns a point | bytes a point |");
Console.WriteLine("|---:|---|---:|---:|---:|---:|");
foreach (var side in (int[])[513, .. sides])
{
    var half = (side - 1) * 0.5f;
    LandscapeStep[] steps =
    [
        new Stamp(StampShape.Dome, new(-0.2f * half, 0.1f * half), 0.6f * half, 0.15f * half),
        new PictureStamp("ridge", new(0.3f * half, -0.3f * half), new(0.8f * half, 0.4f * half), 0.1f * half, StampBlend.Max, 0.05f * half),
        new Noise(7, 0.25f * half, 8, 0.02f * half),
        new Terrace(0.01f * half, 0.4f),
        new Smoothing(2),
        new ThermalErosion(0.8f, 20),
        new HydraulicErosion(3, side * side / 4),
    ];
    var pictures = new Ridge();

    // Each step through a builder that holds the steps before it, so a rebuild with the step added
    // applies that step alone. A rebuild of the same recipe copies the last field and compares it
    // with the one before, the overhead every such rebuild carries, which the table takes off.
    var builder = new LandscapeBuilder(new Recipe(side, side, 1f, []), pictures);
    var overhead = Measure(side, "rebuild overhead", () => { }, () => builder.Rebuild(new Recipe(side, side, 1f, [])), null);
    for (var k = 0; k < steps.Length; k++)
    {
        var (before, upTo) = (new Recipe(side, side, 1f, steps[..k]), new Recipe(side, side, 1f, steps[..(k + 1)]));
        Measure(side, steps[k].GetType().Name, () => builder.Rebuild(before), () => builder.Rebuild(upTo), overhead);
    }

    var field = builder.Field;
    Measure(side, "tiles", () => { }, () => LandscapeGeometry.Tiles(field), null);
    SplatRule[] rules =
    [
        new("sand", highest: 0.01f * half, heightBlend: 1f),
        new("grass", mostSteep: 0.5f, steepBlend: 0.2f),
        new("rock", leastSteep: 0.7f, steepBlend: 0.2f),
        new("snow", lowest: 0.12f * half, heightBlend: 2f),
    ];
    Measure(side, "splat map", () => { }, () => SplatMap.Of(field, rules), null);
    Measure(side, "whole build", () => { }, () => new Recipe(side, side, 1f, steps).Build(pictures), null);
    Measure(side, "whole build, 8 threads", () => { }, () => new Recipe(side, side, 1f, steps).Build(pictures, new BuildOptions(threads: 8)), null);
    warming = false;
}

File.WriteAllText(args[0], JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = true }));
return 0;

// A stage's fastest time of three runs, each after its setup and a collection so one run's garbage
// is not counted in the next, and the bytes the thread allocated in a run, less a rebuild's
// overhead where one is given.
Row Measure(int side, string stage, Action setup, Action run, Row? less)
{
    var (fastest, bytes) = (double.MaxValue, 0L);
    for (var time = 0; time < 3; time++)
    {
        setup();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        var clock = Stopwatch.StartNew();
        run();
        clock.Stop();
        bytes = GC.GetAllocatedBytesForCurrentThread() - before;
        fastest = Math.Min(fastest, clock.Elapsed.TotalMilliseconds);
    }

    bytes -= less?.Bytes ?? 0;
    var milliseconds = fastest - (less?.Milliseconds ?? 0);
    var points = (double)side * side;
    var row = new Row(side, stage, milliseconds, bytes, milliseconds * 1e6 / points, bytes / points);
    if (warming) return row;
    rows.Add(row);
    Console.WriteLine($"| {side} | {stage} | {row.Milliseconds:F0} | {bytes / 1e6:F1} | {row.NanosecondsAPoint:F1} | {row.BytesAPoint:F2} |");
    return row;
}

record Row(int Side, string Stage, double Milliseconds, long Bytes, double NanosecondsAPoint, double BytesAPoint);

sealed class Ridge : ILandscapePictures
{
    private readonly HeightPicture _picture = new(64, 32, [.. Enumerable.Range(0, 64 * 32).Select(i => i % 64 / 63f)]);

    public HeightPicture? Heights(string name) => _picture;
}
