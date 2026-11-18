namespace Landscape;

// The random numbers a seeded step draws, O'Neill's PCG of 2014 with 64 bits of state and 32 out,
// the generator Vegetation's Pcg is, written here since the module references no other.
// System.Random is no promise across versions of .NET, and a recipe kept in a level by its seeds
// has to build the same heights wherever the level is opened.
internal struct Pcg
{
    private const ulong Multiplier = 6364136223846793005ul;
    private const ulong Increment = 1442695040888963407ul;

    private ulong _state;

    public Pcg(ulong seed)
    {
        _state = 0;
        Next();
        _state += seed;
        Next();
    }

    public uint Next()
    {
        var old = _state;
        _state = old * Multiplier + Increment;
        var shifted = (uint)(((old >> 18) ^ old) >> 27);
        var rotation = (int)(old >> 59);
        return (shifted >> rotation) | (shifted << (-rotation & 31));
    }

    // From zero up to one, one never reached, from the top 24 bits so every value is a float's.
    public float Unit() => (Next() >> 8) * (1f / 16_777_216f);

    // From zero up to the count, never reaching it.
    public int Below(int count) => (int)((ulong)Next() * (uint)count >> 32);
}
