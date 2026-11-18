namespace Landscape;

/// <summary>How a build runs, the threads it may use and where it reports its progress.</summary>
/// <remarks>
/// The steps whose every point is reckoned alone, the stamps, the noise, the terraces and the
/// smoothing, and the splat map and the tiles, share their rows among the threads, and the
/// erosions, whose droplets and passes read what the ones before them left, run on one, so a build
/// gives the same bits at any count of threads.
/// </remarks>
public sealed record BuildOptions
{
    /// <summary>Options of a count of threads and a progress.</summary>
    /// <param name="threads">How many threads a build may use, one or more.</param>
    /// <param name="progress">Where a build reports each step it has applied, or none.</param>
    /// <exception cref="ArgumentOutOfRangeException">The threads are fewer than one.</exception>
    public BuildOptions(int threads = 1, IProgress<BuildProgress>? progress = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(threads, 1);
        (Threads, Progress) = (threads, progress);
    }

    /// <summary>How many threads a build may use.</summary>
    public int Threads { get; }

    /// <summary>Where a build reports each step it has applied, or none.</summary>
    /// <remarks>
    /// A build reports on the thread it runs on, as each step ends, so a progress that draws on an
    /// engine's own thread takes the report there.
    /// </remarks>
    public IProgress<BuildProgress>? Progress { get; }

    /// <summary>One thread and no progress, as a build runs where none are given.</summary>
    public static BuildOptions Default { get; } = new();

    // Runs a body for each row of a stretch, across the threads where there are more than one.
    internal void Rows(int first, int last, Action<int> row)
    {
        if (Threads == 1 || last - first < 8)
        {
            for (var r = first; r <= last; r++) row(r);
            return;
        }

        Parallel.For(first, last + 1, new ParallelOptions { MaxDegreeOfParallelism = Threads }, row);
    }
}

/// <summary>How far a build has come, the steps applied and the steps of its recipe.</summary>
/// <param name="Done">How many of the recipe's steps stand applied.</param>
/// <param name="Steps">How many steps the recipe has.</param>
public readonly record struct BuildProgress(int Done, int Steps);
