using Xunit;

namespace Landscape.Tests;

// N 4.11, a comment line holds to 100 columns, read from the sources the repository keeps.
public class CommentWidthTests
{
    private const int Width = 100;

    [Fact]
    public void Every_Comment_Line_Holds_To_100_Columns()
    {
        var root = Root();
        var found = new List<string>();
        foreach (var path in Sources(root))
        {
            var lines = File.ReadAllLines(path);
            var inXmlComment = false;
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var text = line.TrimStart();
                var isComment = Path.GetExtension(path) switch
                {
                    ".cs" => text.StartsWith("//", StringComparison.Ordinal),
                    ".sh" or ".yml" or ".py" => text.StartsWith('#'),
                    _ => inXmlComment || text.Contains("<!--", StringComparison.Ordinal),
                };
                if (text.Contains("<!--", StringComparison.Ordinal)) inXmlComment = true;
                if (text.Contains("-->", StringComparison.Ordinal)) inXmlComment = false;
                if (isComment && line.Length > Width && !IsAddressAlone(text))
                    found.Add($"{Path.GetRelativePath(root, path)}:{i + 1} has {line.Length} columns");
            }
        }

        Assert.True(found.Count == 0, string.Join(Environment.NewLine, found));
    }

    // A comment whose words are an address alone, which no break can shorten.
    private static bool IsAddressAlone(string text)
    {
        var words = text.TrimStart('/', '#', '<', '!', '-', ' ').TrimEnd('-', '>', ' ');
        return !words.Contains(' ') && words.Contains("://", StringComparison.Ordinal);
    }

    private static IEnumerable<string> Sources(string root)
    {
        string[] kinds = [".cs", ".csproj", ".props", ".targets", ".slnx", ".yml", ".sh", ".py"];
        string[] skipped = ["bin", "obj", ".git", ".ref", "dump", "TestResults"];
        var pending = new Stack<string>([root]);
        while (pending.TryPop(out var folder))
        {
            foreach (var inner in Directory.EnumerateDirectories(folder))
            {
                if (!skipped.Contains(Path.GetFileName(inner))) pending.Push(inner);
            }

            foreach (var file in Directory.EnumerateFiles(folder))
            {
                if (kinds.Contains(Path.GetExtension(file))) yield return file;
            }
        }
    }

    // The repository's root, the folder above the tests' output that holds the solution.
    private static string Root()
    {
        for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
        {
            if (File.Exists(Path.Combine(folder.FullName, "Landscape.slnx"))) return folder.FullName;
        }

        throw new InvalidOperationException($"No Landscape.slnx above {AppContext.BaseDirectory}.");
    }
}
