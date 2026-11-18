namespace Landscape;

/// <summary>
/// Smooths the field by passes of a blur over each point and the eight about it, the binomial
/// weights of one, two and one each way.
/// </summary>
/// <remarks>
/// A pass blurs along X and then along Z into a copy, so a point reads its neighbors as the pass
/// found them and the order the points are visited in changes nothing. A point at the edge takes
/// itself in place of the neighbor outside, so a flat field stays flat to its edge.
/// </remarks>
public sealed record Smoothing : LandscapeStep
{
    /// <summary>Smoothing of a count of passes.</summary>
    /// <param name="passes">How many passes, from one to 256.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The passes are not from one to 256.
    /// </exception>
    public Smoothing(int passes) => Passes = passes;

    /// <summary>How many passes, from one to 256.</summary>
    public int Passes
    {
        get;
        init => field = value is >= 1 and <= 256 ? value : throw new ArgumentOutOfRangeException(nameof(Passes), value, "Smoothing takes from one to 256 passes.");
    }

    internal override void Apply(Heightfield field, ILandscapePictures? pictures)
    {
        var (columns, rows, heights) = (field.Columns, field.Rows, field.Heights);
        var across = new float[heights.Length];
        for (var pass = 0; pass < Passes; pass++)
        {
            for (var row = 0; row < rows; row++)
            {
                for (var column = 0; column < columns; column++)
                {
                    var at = row * columns + column;
                    var left = heights[row * columns + Math.Max(column - 1, 0)];
                    var right = heights[row * columns + Math.Min(column + 1, columns - 1)];
                    across[at] = (left + 2f * heights[at] + right) * 0.25f;
                }
            }

            for (var row = 0; row < rows; row++)
            {
                for (var column = 0; column < columns; column++)
                {
                    var back = across[Math.Max(row - 1, 0) * columns + column];
                    var front = across[Math.Min(row + 1, rows - 1) * columns + column];
                    heights[row * columns + column] = (back + 2f * across[row * columns + column] + front) * 0.25f;
                }
            }
        }
    }
}
