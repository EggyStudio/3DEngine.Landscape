namespace Landscape;

/// <summary>
/// Fractal gradient noise added to the field, octaves of Perlin's noise each half the size of the
/// one before and weighted by the roughness, drawn from a seed.
/// </summary>
/// <remarks>
/// <para>
/// The noise is a function of the place in meters and not of the field's points, so two fields of
/// different sizes or spacings with the same noise agree where they overlap. Its gradients are the
/// eight ways of the compass at unit length and its fade is Perlin's quintic of 2002, so each
/// octave lies within plus and minus the square root of a half, which the module scales to one, and
/// the octaves summed over their weights keep the noise within its amplitude.
/// </para>
/// <para>
/// A seed's permutation and the octaves' offsets are drawn from the module's PCG in a fixed order,
/// and the noise is reckoned with addition, multiplication and floor alone, so a seed gives the
/// same bits on every system.
/// </para>
/// </remarks>
public sealed record Noise : LandscapeStep
{
    /// <summary>Noise of a seed, a size, a count of octaves and an amplitude.</summary>
    /// <param name="seed">The seed its permutation and its octaves' offsets are drawn from.</param>
    /// <param name="scale">
    /// The first octave's wavelength in meters, the size of its largest features.
    /// </param>
    /// <param name="octaves">How many octaves are summed, from one to sixteen.</param>
    /// <param name="amplitude">
    /// How far in meters the noise reaches above and below the ground.
    /// </param>
    /// <param name="roughness">
    /// Each octave's weight over the one before, above nothing and at most one.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// A number is out of the range its parameter gives.
    /// </exception>
    public Noise(uint seed, float scale, int octaves, float amplitude, float roughness = 0.5f)
    {
        Seed = seed;
        Scale = scale;
        Octaves = octaves;
        Amplitude = amplitude;
        Roughness = roughness;
    }

    /// <summary>The seed its permutation and its octaves' offsets are drawn from.</summary>
    public uint Seed { get; init; }

    /// <summary>
    /// The first octave's wavelength in meters, the size of its largest features.
    /// </summary>
    public float Scale
    {
        get;
        init => field = value > 0f && float.IsFinite(value) ? value : throw new ArgumentOutOfRangeException(nameof(Scale), value, "A noise's scale is a finite number of meters above zero.");
    }

    /// <summary>How many octaves are summed, from one to sixteen.</summary>
    public int Octaves
    {
        get;
        init => field = value is >= 1 and <= 16 ? value : throw new ArgumentOutOfRangeException(nameof(Octaves), value, "A noise has from one to sixteen octaves.");
    }

    /// <summary>How far in meters the noise reaches above and below the ground.</summary>
    public float Amplitude
    {
        get;
        init => field = float.IsFinite(value) ? value : throw new ArgumentOutOfRangeException(nameof(Amplitude), value, "A noise's amplitude is finite.");
    }

    /// <summary>Each octave's weight over the one before, above nothing and at most one.</summary>
    public float Roughness
    {
        get;
        init => field = value is > 0f and <= 1f ? value : throw new ArgumentOutOfRangeException(nameof(Roughness), value, "A noise's roughness is above nothing and at most one.");
    }

    /// <summary>
    /// The noise in meters at a place, from minus the amplitude to the amplitude.
    /// </summary>
    /// <param name="place">The place across the ground, X in X and Z in Y, in meters.</param>
    public float At(System.Numerics.Vector2 place) => Sampler().At(place.X, place.Y);

    internal override void Apply(Heightfield field, ILandscapePictures? pictures)
    {
        var sampler = Sampler();
        for (var row = 0; row < field.Rows; row++)
        {
            for (var column = 0; column < field.Columns; column++)
            {
                var place = field.Place(column, row);
                field[column, row] += sampler.At(place.X, place.Y);
            }
        }
    }

    private Lattice Sampler() => new(this);

    // A seed's permutation and offsets, made once a build and read at every point.
    private sealed class Lattice
    {
        // Each octave's lattice is moved by up to this many cells so the octaves' zeros at the
        // lattice points do not fall together at the origin.
        private const float Reach = 256f;

        // One over the square root of a half, which brings an octave of unit gradients to one.
        private const float Gain = 1.41421356f;

        private readonly int[] _permutation = new int[512];
        private readonly (float X, float Z)[] _offsets;
        private readonly float[] _weights;
        private readonly float _frequency;

        public Lattice(Noise noise)
        {
            var random = new Pcg(noise.Seed);
            for (var i = 0; i < 256; i++) _permutation[i] = i;
            for (var i = 255; i > 0; i--)
            {
                var j = random.Below(i + 1);
                (_permutation[i], _permutation[j]) = (_permutation[j], _permutation[i]);
            }

            for (var i = 0; i < 256; i++) _permutation[i + 256] = _permutation[i];
            _offsets = new (float, float)[noise.Octaves];
            _weights = new float[noise.Octaves];
            var (weight, total) = (1f, 0f);
            for (var octave = 0; octave < noise.Octaves; octave++)
            {
                _offsets[octave] = (random.Unit() * Reach, random.Unit() * Reach);
                _weights[octave] = weight;
                total += weight;
                weight *= noise.Roughness;
            }

            for (var octave = 0; octave < noise.Octaves; octave++) _weights[octave] *= noise.Amplitude / total;
            _frequency = 1f / noise.Scale;
        }

        public float At(float x, float z)
        {
            var (sum, frequency) = (0f, _frequency);
            for (var octave = 0; octave < _weights.Length; octave++)
            {
                sum += _weights[octave] * Gain * Perlin(x * frequency + _offsets[octave].X, z * frequency + _offsets[octave].Z);
                frequency *= 2f;
            }

            return sum;
        }

        private float Perlin(float x, float z)
        {
            var (cellX, cellZ) = (MathF.Floor(x), MathF.Floor(z));
            var (fx, fz) = (x - cellX, z - cellZ);
            var (i, j) = ((int)cellX & 255, (int)cellZ & 255);
            var near = Lerp(Corner(i, j, fx, fz), Corner(i + 1, j, fx - 1f, fz), Fade(fx));
            var far = Lerp(Corner(i, j + 1, fx, fz - 1f), Corner(i + 1, j + 1, fx - 1f, fz - 1f), Fade(fx));
            return Lerp(near, far, Fade(fz));
        }

        // A lattice point's gradient, one of the compass's eight ways at unit length, against the
        // way from it to the place.
        private float Corner(int i, int j, float x, float z)
        {
            const float diagonal = 0.70710678f;
            return (_permutation[_permutation[i & 255] + (j & 255)] & 7) switch
            {
                0 => x,
                1 => -x,
                2 => z,
                3 => -z,
                4 => (x + z) * diagonal,
                5 => (x - z) * diagonal,
                6 => (-x + z) * diagonal,
                _ => (-x - z) * diagonal,
            };
        }

        private static float Fade(float t) => t * t * t * (t * (t * 6f - 15f) + 10f);

        private static float Lerp(float from, float to, float amount) => from + (to - from) * amount;
    }
}
