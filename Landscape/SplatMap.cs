namespace Landscape;

/// <summary>
/// How much of each of a few materials covers each point of a field, as weights that sum to one
/// and as layers of RGBA bytes, four materials a layer, which an engine uploads as textures and
/// blends its materials by.
/// </summary>
/// <remarks>
/// <para>
/// A layer has a texel a point, a row at a time along X and the rows along Z as the field's heights
/// lie, the first material of the layer in red, the second in green, the third in blue and the
/// fourth in alpha, a last layer of fewer than four holding nothing in the rest. A point's bytes
/// across every layer sum to 255, each weight taken to the byte below and the bytes left over given
/// to the largest remainders, the first material winning a tie, so no point is lighter or heavier
/// than its whole.
/// </para>
/// <para>
/// A tile's texture coordinates run from nothing at the field's first point to one at its last,
/// where a texel's middle stands half a texel in from the layer's edge, so an engine sampling a
/// layer with them scales them by the points less one over the points and moves them by half a
/// texel, as u times (columns less one) over columns plus a half over columns.
/// </para>
/// </remarks>
public sealed class SplatMap
{
    /// <summary>The most materials a map holds, two layers.</summary>
    public const int MostMaterials = 8;

    private readonly float[] _weights;

    private SplatMap(Heightfield field, IReadOnlyList<SplatRule> rules)
    {
        (Columns, Rows, Materials) = (field.Columns, field.Rows, [.. rules.Select(rule => rule.Material)]);
        var count = rules.Count;
        _weights = new float[field.Heights.Length * count];
        var layers = (count + 3) / 4;
        Layers = [.. Enumerable.Range(0, layers).Select(_ => new byte[field.Heights.Length * 4])];
        Span<float> remainders = stackalloc float[count];
        Span<int> bytes = stackalloc int[count];
        for (var row = 0; row < Rows; row++)
        {
            for (var column = 0; column < Columns; column++)
            {
                var point = row * Columns + column;
                var (height, steepness) = (field[column, row], field.Steepness(column, row));
                var total = 0f;
                for (var m = 0; m < count; m++) total += _weights[point * count + m] = rules[m].Weight(height, steepness);

                // Where no rule reaches, the first material, the ground's own, covers the point.
                if (total == 0f) (_weights[point * count], total) = (1f, 1f);
                var given = 0;
                for (var m = 0; m < count; m++)
                {
                    var weight = _weights[point * count + m] /= total;
                    var scaled = weight * 255f;
                    bytes[m] = (int)MathF.Floor(scaled);
                    remainders[m] = scaled - bytes[m];
                    given += bytes[m];
                }

                for (; given < 255; given++)
                {
                    var largest = 0;
                    for (var m = 1; m < count; m++)
                    {
                        if (remainders[m] > remainders[largest]) largest = m;
                    }

                    bytes[largest]++;
                    remainders[largest] = -1f;
                }

                for (var m = 0; m < count; m++) Layers[m / 4][point * 4 + m % 4] = (byte)bytes[m];
            }
        }
    }

    /// <summary>How many points a row has along X, the layers' width in texels.</summary>
    public int Columns { get; }

    /// <summary>How many rows of points there are along Z, the layers' height in texels.</summary>
    public int Rows { get; }

    /// <summary>The materials' names in the order of the rules, four to a layer.</summary>
    public IReadOnlyList<string> Materials { get; }

    /// <summary>
    /// The layers, four bytes a texel in red, green, blue and alpha, four materials each.
    /// </summary>
    public IReadOnlyList<byte[]> Layers { get; }

    /// <summary>Makes a field's splat map by its materials' rules.</summary>
    /// <param name="field">The field.</param>
    /// <param name="rules">
    /// The materials' rules, from one to <see cref="MostMaterials"/>, the first covering any point
    /// no rule reaches.
    /// </param>
    /// <exception cref="ArgumentException">
    /// There is no rule or more than eight, a rule is missing, or two rules name one material.
    /// </exception>
    public static SplatMap Of(Heightfield field, IReadOnlyList<SplatRule> rules)
    {
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(rules);
        if (rules.Count is 0 or > MostMaterials) throw new ArgumentException($"A splat map has from one to {MostMaterials} materials, not {rules.Count}.", nameof(rules));
        if (rules.Any(rule => rule is null)) throw new ArgumentException("A splat map's rules are all there.", nameof(rules));
        if (rules.Select(rule => rule.Material).Distinct(StringComparer.Ordinal).Count() != rules.Count)
            throw new ArgumentException("A splat map's rules name each material once.", nameof(rules));
        return new SplatMap(field, rules);
    }

    /// <summary>
    /// A material's weight at a point, from nothing to one, the materials' summing to one.
    /// </summary>
    /// <param name="material">The material's place among <see cref="Materials"/>.</param>
    /// <param name="column">The point's column, from zero along X.</param>
    /// <param name="row">The point's row, from zero along Z.</param>
    public float Weight(int material, int column, int row)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)material, (uint)Materials.Count, nameof(material));
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)column, (uint)Columns, nameof(column));
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)row, (uint)Rows, nameof(row));
        return _weights[(row * Columns + column) * Materials.Count + material];
    }
}
