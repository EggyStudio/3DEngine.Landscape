using System.Numerics;
using Xunit;

namespace Landscape.Tests;

// A build across the machine's threads giving the same bits as one thread, and reporting its
// progress a step at a time.
public class BuildOptionsTests
{
    private static readonly HeightPicture Ridge = new(11, 5, [.. Enumerable.Range(0, 55).Select(i => i % 11 / 10f)]);

    // Every step the module has over a field large enough that each row's work is split.
    private static Recipe Every() => new(257, 193, 1f,
    [
        new Stamp(StampShape.Dome, new(-20f, 10f), 60f, 25f),
        new PictureStamp("ridge", new(30f, -20f), new(80f, 40f), 9f, StampBlend.Max, 6f),
        new Noise(7, 50f, 6, 5f),
        new Terrace(2f, 0.3f),
        new Smoothing(2),
        new ThermalErosion(0.8f, 15),
        new HydraulicErosion(2, 8_000),
    ]);

    [Fact]
    public void A_Build_At_Eight_Threads_Gives_The_Bits_Of_One()
    {
        var pictures = new Pictures();
        var one = Every().Build(pictures, new BuildOptions(threads: 1));
        var eight = Every().Build(pictures, new BuildOptions(threads: 8));

        Assert.Equal(one.Heights, eight.Heights);
        Assert.Equal(Every().Build(pictures).Heights, eight.Heights);
        Assert.Equal(Fields.ThreeStamps().Build().Heights, Fields.ThreeStamps().Build(null, new BuildOptions(threads: 8)).Heights);
    }

    [Fact]
    public void A_Splat_Map_And_Tiles_At_Eight_Threads_Are_Those_Of_One()
    {
        var field = Every().Build(new Pictures());
        SplatRule[] rules = [new("grass", mostSteep: 0.5f, steepBlend: 0.2f), new("rock", leastSteep: 0.7f, steepBlend: 0.2f), new("snow", lowest: 20f, heightBlend: 2f)];
        var (one, eight) = (SplatMap.Of(field, rules), SplatMap.Of(field, rules, new BuildOptions(threads: 8)));
        Assert.Equal(one.Layers[0], eight.Layers[0]);

        var (fine, fast) = (LandscapeGeometry.Tiles(field), LandscapeGeometry.Tiles(field, options: new BuildOptions(threads: 8)));
        Assert.Equal(fine.Count, fast.Count);
        for (var i = 0; i < fine.Count; i++)
        {
            Assert.Equal(fine[i].Positions, fast[i].Positions);
            Assert.Equal(fine[i].Normals, fast[i].Normals);
        }
    }

    [Fact]
    public void A_Build_Reports_Each_Step_In_Order_Up_To_The_Whole()
    {
        var reports = new Reports();
        Every().Build(new Pictures(), new BuildOptions(threads: 4, progress: reports));

        Assert.Equal(Enumerable.Range(1, 7).Select(done => new BuildProgress(done, 7)), reports.Seen);
    }

    [Fact]
    public void A_Rebuild_Reports_The_Steps_It_Applies()
    {
        var reports = new Reports();
        var builder = new LandscapeBuilder(Every(), new Pictures(), new BuildOptions(threads: 4, progress: reports));
        reports.Seen.Clear();
        builder.Rebuild(new Recipe(257, 193, 1f, [.. Every().Steps.Take(5), new ThermalErosion(0.6f, 15), Every().Steps[6]]));

        Assert.Equal([new BuildProgress(6, 7), new BuildProgress(7, 7)], reports.Seen);
    }

    [Fact]
    public void A_Count_Of_Threads_Below_One_Is_Refused()
    {
        Assert.Throws<ArgumentOutOfRangeException>("threads", () => new BuildOptions(threads: 0));
    }

    private sealed class Pictures : ILandscapePictures
    {
        public HeightPicture? Heights(string name) => name == "ridge" ? Ridge : null;
    }

    // A progress that keeps every report as it comes, on the thread that reports it.
    private sealed class Reports : IProgress<BuildProgress>
    {
        public List<BuildProgress> Seen { get; } = [];

        public void Report(BuildProgress value)
        {
            lock (Seen) Seen.Add(value);
        }
    }
}
