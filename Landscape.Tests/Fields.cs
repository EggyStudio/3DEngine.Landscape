namespace Landscape.Tests;

// What the tests share about fields.
internal static class Fields
{
    // FNV-1a over each height's bits, so two fields hash alike only where every bit agrees.
    public static ulong Hash(Heightfield field)
    {
        var hash = 14695981039346656037ul;
        foreach (var height in field.Heights)
        {
            var bits = (uint)BitConverter.SingleToInt32Bits(height);
            for (var shift = 0; shift < 32; shift += 8)
            {
                hash ^= (bits >> shift) & 0xFF;
                hash *= 1099511628211ul;
            }
        }

        return hash;
    }

    // Three stamps that overlap and reach past the edge, a dome, a plateau flattened into it and a
    // crater added at the corner, on a field of 129 by 129 points a meter apart.
    public static Recipe ThreeStamps() => new(129, 129, 1f,
    [
        new Stamp(StampShape.Dome, new(-10f, 5f), 45f, 18f),
        new Stamp(StampShape.Plateau, new(20f, -15f), 25f, 8f, StampBlend.Replace),
        new Stamp(StampShape.Crater, new(55f, 50f), 30f, 6f),
    ]);
}
