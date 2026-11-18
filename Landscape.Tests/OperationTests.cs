using System.Numerics;
using Xunit;

namespace Landscape.Tests;

public class OperationTests
{
    [Fact]
    public void Noise_Gives_The_Same_Bits_For_A_Seed_On_Every_System_And_Others_For_Another()
    {
        var first = new Recipe(129, 129, 1f, [new Noise(7, 64f, 6, 10f)]).Build();
        var again = new Recipe(129, 129, 1f, [new Noise(7, 64f, 6, 10f)]).Build();
        var other = new Recipe(129, 129, 1f, [new Noise(8, 64f, 6, 10f)]).Build();

        Assert.Equal(first.Heights, again.Heights);
        Assert.NotEqual(first.Heights, other.Heights);
        Assert.Equal(PinnedNoise, Fields.Hash(first));
    }

    [Fact]
    public void Noise_Stays_Within_Its_Amplitude_And_Reaches_Toward_It()
    {
        var heights = new Recipe(257, 257, 1f, [new Noise(3, 32f, 8, 10f, 0.6f)]).Build().Heights;

        Assert.All(heights, height => Assert.InRange(height, -10f, 10f));
        Assert.True(heights.Max() > 4f && heights.Min() < -4f, $"The noise spans {heights.Min()} to {heights.Max()}.");
    }

    [Fact]
    public void Noise_Is_Of_The_Place_So_Two_Spacings_Agree_At_Their_Shared_Points()
    {
        var coarse = new Recipe(129, 129, 1f, [new Noise(5, 40f, 5, 6f)]).Build();
        var fine = new Recipe(257, 257, 0.5f, [new Noise(5, 40f, 5, 6f)]).Build();

        for (var row = 0; row < coarse.Rows; row += 7)
        {
            for (var column = 0; column < coarse.Columns; column += 5)
                Assert.Equal(coarse[column, row], fine[column * 2, row * 2]);
        }
    }

    [Fact]
    public void A_Terrace_Keeps_Its_Tread_Flat_And_Rises_Smoothly_To_The_Next()
    {
        var terrace = new Terrace(2f, 0.5f);

        Assert.Equal(2f, terrace.Of(2f));
        Assert.Equal(2f, terrace.Of(2.6f));
        Assert.Equal(3f, terrace.Of(3.5f), 1e-6f);
        Assert.Equal(4f, terrace.Of(3.9999f), 1e-3f);
        Assert.Equal(-2f, terrace.Of(-1.5f));
        var last = float.NegativeInfinity;
        for (var height = -6f; height < 6f; height += 0.01f)
        {
            Assert.True(terrace.Of(height) >= last, $"The terrace falls at {height}.");
            last = terrace.Of(height);
        }
    }

    [Fact]
    public void A_Smoothing_Pass_Spreads_A_Spike_By_One_Two_And_One_And_Keeps_A_Ramp()
    {
        var spike = new Heightfield(9, 9, 1f) { [4, 4] = 16f };
        new Smoothing(1).Apply(spike);

        Assert.Equal(4f, spike[4, 4]);
        Assert.Equal(2f, spike[3, 4]);
        Assert.Equal(2f, spike[4, 5]);
        Assert.Equal(1f, spike[5, 5]);
        Assert.Equal(0f, spike[6, 4]);
        Assert.Equal(16f, spike.Heights.Sum());

        var ramp = new Heightfield(9, 9, 1f);
        for (var row = 0; row < 9; row++)
        {
            for (var column = 0; column < 9; column++) ramp[column, row] = column;
        }

        new Smoothing(3).Apply(ramp);
        for (var column = 3; column < 6; column++) Assert.Equal(column, ramp[column, 4]);
    }

    [Fact]
    public void Thermal_Erosion_Keeps_The_Volume_And_Brings_The_Ground_Under_Its_Talus()
    {
        var before = new Recipe(65, 65, 1f, [new Stamp(StampShape.Cone, Vector2.Zero, 20f, 30f)]).Build();
        var after = before.Copy();
        new ThermalErosion(0.8f, 1600).Apply(after);

        Assert.Equal(before.Heights.Sum(height => (double)height), after.Heights.Sum(height => (double)height), VolumeTolerance);
        Assert.True(Steepest(before) > 1.4f);
        Assert.True(Steepest(after) <= 0.8f + TalusTolerance, $"The steepest rise is {Steepest(after)}.");
    }

    [Fact]
    public void Thermal_Erosion_Holds_The_Slope_To_Its_Talus_Whichever_Way_The_Ground_Faces()
    {
        var field = new Recipe(65, 65, 1f, [new Stamp(StampShape.Cone, Vector2.Zero, 20f, 30f), new ThermalErosion(0.8f, 1600)]).Build();

        // The slope's own steepness, its rise along X and Z taken together, at every point inside
        // the edge, which the four points beside a point along X and Z alone leave at the square
        // root of two times the talus where the ground faces along a diagonal.
        var steepest = 0f;
        for (var row = 1; row < 64; row++)
        {
            for (var column = 1; column < 64; column++)
            {
                var (x, z) = ((field[column + 1, row] - field[column - 1, row]) / 2f, (field[column, row + 1] - field[column, row - 1]) / 2f);
                steepest = MathF.Max(steepest, MathF.Sqrt(x * x + z * z));
            }
        }

        Assert.True(steepest <= 0.8f * SlopeShare, $"The steepest slope is {steepest}, {steepest / 0.8f} times the talus.");
    }

    [Fact]
    public void Thermal_Erosion_Leaves_Ground_Under_Its_Talus_As_It_Was()
    {
        var ramp = new Heightfield(17, 17, 2f);
        for (var row = 0; row < 17; row++)
        {
            for (var column = 0; column < 17; column++) ramp[column, row] = column + 0.5f * row;
        }

        var eroded = ramp.Copy();
        new ThermalErosion(0.7f, 50).Apply(eroded);

        Assert.Equal(ramp.Heights, eroded.Heights);
    }

    [Fact]
    public void Every_Operation_Together_Gives_The_Same_Bits_On_Every_System()
    {
        var field = new Recipe(129, 129, 1f,
        [
            new Noise(11, 48f, 6, 14f),
            new Stamp(StampShape.Cone, new(20f, -10f), 30f, 25f),
            new Terrace(3f, 0.4f),
            new ThermalErosion(0.9f, 60),
            new Smoothing(2),
        ]).Build();

        Assert.Equal(PinnedTogether, Fields.Hash(field));
    }

    [Fact]
    public void The_Operations_Refuse_What_They_Cannot_Do()
    {
        Assert.Throws<ArgumentOutOfRangeException>("Scale", () => new Noise(1, 0f, 1, 1f));
        Assert.Throws<ArgumentOutOfRangeException>("Octaves", () => new Noise(1, 1f, 0, 1f));
        Assert.Throws<ArgumentOutOfRangeException>("Octaves", () => new Noise(1, 1f, 17, 1f));
        Assert.Throws<ArgumentOutOfRangeException>("Amplitude", () => new Noise(1, 1f, 1, float.NaN));
        Assert.Throws<ArgumentOutOfRangeException>("Roughness", () => new Noise(1, 1f, 1, 1f, 0f));
        Assert.Throws<ArgumentOutOfRangeException>("Height", () => new Terrace(0f, 0.5f));
        Assert.Throws<ArgumentOutOfRangeException>("Tread", () => new Terrace(1f, 1f));
        Assert.Throws<ArgumentOutOfRangeException>("Passes", () => new Smoothing(0));
        Assert.Throws<ArgumentOutOfRangeException>("Talus", () => new ThermalErosion(0f, 1));
        Assert.Throws<ArgumentOutOfRangeException>("Passes", () => new ThermalErosion(1f, 0));
        Assert.Throws<ArgumentOutOfRangeException>("Share", () => new ThermalErosion(1f, 1, 1.5f));
    }

    // The steepest rise a meter between two points beside each other along X or Z.
    private static float Steepest(Heightfield field)
    {
        var steepest = 0f;
        for (var row = 0; row < field.Rows; row++)
        {
            for (var column = 0; column < field.Columns; column++)
            {
                if (column > 0) steepest = MathF.Max(steepest, MathF.Abs(field[column, row] - field[column - 1, row]) / field.Spacing);
                if (row > 0) steepest = MathF.Max(steepest, MathF.Abs(field[column, row] - field[column, row - 1]) / field.Spacing);
            }
        }

        return steepest;
    }

    // The cone's 12,565.242 cubic meters were 12,565.270 after 1,600 passes on Linux, the float
    // sums of a pass's moves rounding apart by 0.028, its steepest rise along an axis 0.80053 over
    // a talus of 0.8, 0.867 at 400 passes and 0.80002 at 6,400, and its steepest slope 1.0824 times
    // the talus, one over the cosine of 22.5 degrees, where it was 1.4142 with four neighbors. Each
    // tolerance is some twice what was read, and the slope's is the bound eight neighbors give.
    private const float SlopeShare = 1.1f;
    private const double VolumeTolerance = 0.05;
    private const float TalusTolerance = 0.001f;
    // The hashes of the two recipes' heights, read on Linux at the commit that wrote these tests
    // and held on Windows and macOS by the build workflow.
    private const ulong PinnedNoise = 11577238114687063638ul;
    private const ulong PinnedTogether = 8588781888222790370ul;
}
