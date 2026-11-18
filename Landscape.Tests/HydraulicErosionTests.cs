using System.Numerics;
using Xunit;

namespace Landscape.Tests;

public class HydraulicErosionTests
{
    // A cone of 30 m over 40 m on noise, 129 by 129 points a meter apart.
    private static Recipe Cone(int droplets) => new(129, 129, 1f,
    [
        new Noise(6, 32f, 4, 1f),
        new Stamp(StampShape.Cone, Vector2.Zero, 40f, 30f),
        .. droplets > 0 ? new LandscapeStep[] { new HydraulicErosion(3, droplets) } : [],
    ]);

    [Fact]
    public void Droplets_Give_The_Same_Bits_For_A_Seed_On_Every_System()
    {
        var first = Cone(20_000).Build();

        Assert.Equal(first.Heights, Cone(20_000).Build().Heights);
        Assert.Equal(PinnedDroplets, Fields.Hash(first));
    }

    [Fact]
    public void Droplets_Wear_The_Flanks_Down_And_Fill_The_Foot()
    {
        var (before, after) = (Cone(0).Build(), Cone(20_000).Build());

        // The flank between 10 and 30 m from the middle and the foot between 36 and 50 m, summed.
        double Ring(Heightfield field, float near, float far)
        {
            var sum = 0.0;
            for (var row = 0; row < field.Rows; row++)
            {
                for (var column = 0; column < field.Columns; column++)
                {
                    var distance = field.Place(column, row).Length();
                    if (distance >= near && distance < far) sum += field[column, row];
                }
            }

            return sum;
        }

        Assert.True(Ring(after, 10f, 30f) < Ring(before, 10f, 30f) - Worn, "The flank was not worn down.");
        Assert.True(Ring(after, 36f, 50f) > Ring(before, 36f, 50f) + Worn, "The foot was not filled.");
        Assert.True(after.Heights.Max() <= before.Heights.Max() + 0.01f, "A droplet raised the peak.");
    }

    [Fact]
    public void A_Closed_Bowl_Keeps_Its_Volume()
    {
        // A hollow dome as wide as the field with a cone in it, whose droplets all run inward and
        // none off the edge.
        var recipe = new Recipe(65, 65, 1f,
        [
            new Stamp(StampShape.Dome, Vector2.Zero, 46f, -20f),
            new Stamp(StampShape.Cone, new(6f, -4f), 14f, 10f),
        ]);
        var before = recipe.Build();
        var after = before.Copy();
        new HydraulicErosion(5, 5_000).Apply(after);

        Assert.NotEqual(before.Heights, after.Heights);
        Assert.Equal(before.Heights.Sum(h => (double)h), after.Heights.Sum(h => (double)h), VolumeTolerance);
    }

    [Fact]
    public void Ground_Draining_Off_The_Edge_Is_Not_Dug_Below_It()
    {
        // A ramp falling a meter in ten toward the first column, with noise, which every droplet
        // runs off, a valley's mouth at the field's edge.
        var ramp = new Heightfield(129, 33, 1f);
        for (var row = 0; row < ramp.Rows; row++)
        {
            for (var column = 0; column < ramp.Columns; column++) ramp[column, row] = 0.1f * column;
        }

        new Noise(2, 16f, 3, 0.5f).Apply(ramp);
        var edge = Enumerable.Range(0, ramp.Rows).Min(row => ramp[0, row]);
        new HydraulicErosion(4, 20_000).Apply(ramp);

        Assert.True(ramp.Heights.Min() >= edge - BelowEdge, $"The ground was dug to {ramp.Heights.Min()}, {edge - ramp.Heights.Min()} m below the edge's lowest point.");
    }

    [Theory]
    [InlineData(40f)]
    [InlineData(160f)]
    public void A_Hill_Reckoned_Against_Its_Own_Height_Keeps_Its_Peak_And_Cuts_Gullies_Without_Pits(float height)
    {
        var recipe = new Recipe(129, 129, 1f, [new Noise(6, 32f, 4, 1f), new Stamp(StampShape.Dome, Vector2.Zero, 50f, height)]);
        var before = recipe.Build();
        var after = before.Copy();
        new HydraulicErosion(3, 66_000, scale: height).Apply(after);

        var kept = after.Heights.Max() / before.Heights.Max();
        Assert.InRange(kept, PeakKept, PeakKept + 0.03f);
        Assert.True(Pits(after) <= MostPits, $"The ground holds {Pits(after)} pits.");
    }

    // The points inside the edge lower than all eight about them.
    private static int Pits(Heightfield field)
    {
        var pits = 0;
        for (var row = 1; row < field.Rows - 1; row++)
        {
            for (var column = 1; column < field.Columns - 1; column++)
            {
                var lowest = true;
                for (var z = -1; z <= 1; z++)
                {
                    for (var x = -1; x <= 1; x++)
                    {
                        if ((x != 0 || z != 0) && field[column + x, row + z] <= field[column, row]) lowest = false;
                    }
                }

                if (lowest) pits++;
            }
        }

        return pits;
    }

    [Fact]
    public void Droplets_Start_Near_The_One_Before_So_The_Field_Stays_In_The_Caches()
    {
        // A proxy for the time a point, which a test cannot read steadily. A droplet of 30 steps
        // stays within 30 points of where it starts, so droplets started near each other read
        // and write the same few rows of the field while they are in the processor's caches.
        var starts = new HydraulicErosion(3, 4097 * 4097 / 4).Starts(4097, 4097).ToArray();
        var apart = 0.0;
        for (var i = 1; i < starts.Length; i++) apart += Vector2.Distance(starts[i], starts[i - 1]);
        apart /= starts.Length - 1;

        Assert.Equal(4097 * 4097 / 4, starts.Length);
        Assert.True(apart <= MostApart, $"A droplet starts {apart:F1} points from the one before.");
        Assert.All(starts, start => Assert.True(start.X >= 0f && start.X < 4096f && start.Y >= 0f && start.Y < 4096f));
    }

    [Fact]
    public void Level_Ground_Stays_Level()
    {
        var field = new Heightfield(33, 33, 1f);
        new HydraulicErosion(1, 2_000).Apply(field);

        Assert.All(field.Heights, height => Assert.Equal(0f, height));
    }

    [Fact]
    public void Hydraulic_Erosion_Refuses_What_It_Cannot_Do()
    {
        Assert.Throws<ArgumentOutOfRangeException>("Droplets", () => new HydraulicErosion(1, 0));
        Assert.Throws<ArgumentOutOfRangeException>("Lifetime", () => new HydraulicErosion(1, 1, lifetime: 0));
        Assert.Throws<ArgumentOutOfRangeException>("Inertia", () => new HydraulicErosion(1, 1, inertia: 1f));
        Assert.Throws<ArgumentOutOfRangeException>("Capacity", () => new HydraulicErosion(1, 1, capacity: 0f));
        Assert.Throws<ArgumentOutOfRangeException>("Deposition", () => new HydraulicErosion(1, 1, deposition: 1.5f));
        Assert.Throws<ArgumentOutOfRangeException>("Evaporation", () => new HydraulicErosion(1, 1, evaporation: 1f));
        Assert.Throws<ArgumentOutOfRangeException>("Radius", () => new HydraulicErosion(1, 1, radius: 9));
    }

    // 20,000 droplets on Linux wore the flank from 34,529 cubic meters to 28,594 and filled the
    // foot from 1,552 to 7,094, so a sixth of either is asked, and the closed bowl's 39,313.020
    // cubic meters were 39,313.018 after 5,000 droplets, the float sums of their moves rounding
    // apart by 0.002, so the tolerance is some twice that.
    // On Linux, 20,000 droplets wore the cone's flank from 34,529 cubic meters to 27,618 and filled
    // its foot from 1,552 to 7,497, so a sixth of either is asked. The closed bowl's 39,313.0203
    // cubic meters were 39,313.0197 after 5,000 droplets, the float sums of their moves rounding
    // apart by 0.0007. The ramp was dug 0.009 m below its edge's lowest point, where it was 16.4 m
    // with the edge worn and never filled. Each tolerance is some twice what was read.
    // A dome of 40 m against 40 m and one of 160 m against 160 m kept 85.55 and 85.56 hundredths of
    // their peaks after 66,000 droplets and held 330 and 266 pits, where against 1 m and 4 m they
    // kept 80.3 and 81.1 and held 749 and 666, so the test asks 84 hundredths and 500 pits at most.
    // Droplets drawn anywhere over a field of 4,097 points a side started 2,135.7 points from the
    // one before, and drawn a block of 64 points at a time 35.0, so a bound of 64 holds them
    // within a block.
    private const double MostApart = 64;

    // A dome of 40 m against 40 m and one of 160 m against 160 m kept 86.91 and 86.92 hundredths
    // of their peaks after 66,000 droplets and held 335 and 298 pits, where against 1 m and 4 m
    // they kept 80.8 and 81.7 and held 725 and 670, so the test asks 84 hundredths and 500 pits at
    // most.
    private const float PeakKept = 0.84f;
    private const int MostPits = 500;

    // On Linux, 20,000 droplets wore the cone's flank from 34,529 cubic meters to 27,684 and filled
    // its foot from 1,552 to 7,460, so a sixth of either is asked. The closed bowl's 39,313.0203
    // cubic meters were 39,313.0197 after 5,000 droplets, the float sums of their moves rounding
    // apart by 0.0007. The ramp was dug no point below its edge's lowest, where it was 16.4 m with
    // the edge worn and never filled. Each tolerance is some twice what was read.
    private const float BelowEdge = 0.02f;
    private const double Worn = 1_000;
    private const double VolumeTolerance = 0.002;

    // The hash of the cone's heights after 20,000 droplets, read on Linux at the commit that wrote
    // these tests and held on Windows and macOS by the build workflow.
    private const ulong PinnedDroplets = 17289324050643138123ul;
}
