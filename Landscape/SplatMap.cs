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
    private readonly SplatRule[] _rules;

    private SplatMap(Heightfield field, IReadOnlyList<SplatRule> rules, BuildOptions options)
    {
        (Columns, Rows, Materials, _rules) = (field.Columns, field.Rows, [.. rules.Select(rule => rule.Material)], [.. rules]);
        _weights = new float[field.Heights.Length * rules.Count];
        Layers = [.. Enumerable.Range(0, (rules.Count + 3) / 4).Select(_ => new byte[field.Heights.Length * 4])];
        Weigh(field, 0, 0, Columns - 1, Rows - 1, options);
    }

    // The weights and the bytes of the points of a rectangle, each point weighed alone, so its rows
    // run on any thread.
    private void Weigh(Heightfield field, int firstColumn, int firstRow, int lastColumn, int lastRow, BuildOptions options)
    {
        var count = _rules.Length;
        options.Rows(firstRow, lastRow, row =>
        {
            Span<float> remainders = stackalloc float[count];
            Span<int> bytes = stackalloc int[count];
            for (var column = firstColumn; column <= lastColumn; column++)
            {
                var point = row * Columns + column;
                var (height, steepness) = (field[column, row], field.Steepness(column, row));
                var total = 0f;
                for (var m = 0; m < count; m++) total += _weights[point * count + m] = _rules[m].Weight(height, steepness);

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

                // A last layer of fewer than four holds nothing in the rest, which a point weighed
                // again keeps.
                for (var m = 0; m < count; m++) Layers[m / 4][point * 4 + m % 4] = (byte)bytes[m];
            }
        });
    }

    /// <summary>
    /// Weighs the points of a changed area again and those beside it, the map holding then what a
    /// whole map of the field holds.
    /// </summary>
    /// <param name="field">The field the map is of, as it stands after the change.</param>
    /// <param name="area">
    /// The points whose heights changed, as a rebuild's <see cref="LandscapeChange.Changed"/> gives
    /// them.
    /// </param>
    /// <returns>
    /// The rectangle of texels weighed again, which an engine uploads of each layer.
    /// </returns>
    /// <exception cref="ArgumentException">The field is not of the map's size.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The area reaches past the field.</exception>
    /// <remarks>
    /// A point's steepness reads the points beside it, so a changed height changes the weights of
    /// the points beside it as well, and the area is grown by a point each way, cut to the field.
    /// The layers are written in place, so an engine that keeps them uploads the rectangle the
    /// update gives.
    /// </remarks>
    public LandscapeArea? Update(Heightfield field, LandscapeArea area)
    {
        ArgumentNullException.ThrowIfNull(field);
        if (field.Columns != Columns || field.Rows != Rows)
            throw new ArgumentException($"The splat map is of {Columns} by {Rows} points and the field of {field.Columns} by {field.Rows}.", nameof(field));
        if (area.FirstColumn < 0 || area.FirstRow < 0 || area.LastColumn >= Columns || area.LastRow >= Rows || area.FirstColumn > area.LastColumn || area.FirstRow > area.LastRow)
            throw new ArgumentOutOfRangeException(nameof(area), area, "The area lies within the field, its first point at or before its last.");
        var grown = new LandscapeArea(
            Math.Max(area.FirstColumn - 1, 0), Math.Max(area.FirstRow - 1, 0),
            Math.Min(area.LastColumn + 1, Columns - 1), Math.Min(area.LastRow + 1, Rows - 1));
        Weigh(field, grown.FirstColumn, grown.FirstRow, grown.LastColumn, grown.LastRow, BuildOptions.Default);
        return grown;
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
    public static SplatMap Of(Heightfield field, IReadOnlyList<SplatRule> rules) => Of(field, rules, BuildOptions.Default);

    /// <summary>
    /// Makes a field's splat map by its materials' rules across the threads the options allow.
    /// </summary>
    /// <param name="field">The field.</param>
    /// <param name="rules">The materials' rules, from one to <see cref="MostMaterials"/>.</param>
    /// <param name="options">The threads the map's rows may be weighed on.</param>
    /// <exception cref="ArgumentException">
    /// There is no rule or more than eight, a rule is missing, or two rules name one material.
    /// </exception>
    public static SplatMap Of(Heightfield field, IReadOnlyList<SplatRule> rules, BuildOptions options)
    {
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(rules);
        if (rules.Count is 0 or > MostMaterials) throw new ArgumentException($"A splat map has from one to {MostMaterials} materials, not {rules.Count}.", nameof(rules));
        if (rules.Any(rule => rule is null)) throw new ArgumentException("A splat map's rules are all there.", nameof(rules));
        if (rules.Select(rule => rule.Material).Distinct(StringComparer.Ordinal).Count() != rules.Count)
            throw new ArgumentException("A splat map's rules name each material once.", nameof(rules));
        ArgumentNullException.ThrowIfNull(options);
        return new SplatMap(field, rules, options);
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
