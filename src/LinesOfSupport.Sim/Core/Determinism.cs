namespace LinesOfSupport.Sim.Core;

/// <summary>
/// SplitMix64 generator. Stable across platforms and .NET versions, unlike System.Random.
/// One instance per subsystem stream (ADR-0002): adding a draw in one stream never shifts another.
/// </summary>
public sealed class DeterministicRandom
{
    private ulong _state;

    public DeterministicRandom(ulong seed) => _state = seed;

    public static DeterministicRandom ForStream(long seed, string stream)
        => new(Mix((ulong)seed ^ Fnv1a(stream)));

    public ulong NextUInt64()
    {
        _state += 0x9E3779B97F4A7C15UL;
        return Mix(_state);
    }

    /// <summary>Uniform integer in [min, max] inclusive. Uses rejection to avoid modulo bias.</summary>
    public int NextInt(int min, int max)
    {
        if (max < min) (min, max) = (max, min);
        var span = (ulong)(max - min) + 1UL;
        var limit = ulong.MaxValue - (ulong.MaxValue % span);
        ulong r;
        do { r = NextUInt64(); } while (r >= limit);
        return min + (int)(r % span);
    }

    public int NextInt(Data.IntRange range) => NextInt(range.Min, range.Max);

    /// <summary>True with probability permille/1000.</summary>
    public bool Roll(int permille) => permille > 0 && (permille >= 1000 || NextInt(0, 999) < permille);

    private static ulong Mix(ulong z)
    {
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }

    public static ulong Fnv1a(string s)
    {
        var h = 0xCBF29CE484222325UL;
        foreach (var ch in s)
        {
            h ^= ch;
            h *= 0x100000001B3UL;
        }
        return h;
    }
}

/// <summary>Integer helpers. Outcome-bearing quantities never use floating point after load (ADR-0002).</summary>
public static class Fixed
{
    /// <summary>Converts a configured fraction or factor to permille, once, at load.</summary>
    public static int Permille(double value) => (int)Math.Round(value * 1000.0, MidpointRounding.AwayFromZero);

    /// <summary>Converts a configured quantity to milli-units, once, at load.</summary>
    public static long Milli(double value) => (long)Math.Round(value * 1000.0, MidpointRounding.AwayFromZero);

    public static long MulPermille(long value, int permille) => value * permille / 1000;

    public static int Ratio(long numerator, long denominator)
        => denominator <= 0 ? 1000 : (int)Math.Clamp(numerator * 1000 / denominator, 0, 1000);

    /// <summary>
    /// Front-loaded decay curve in permille, integer only:
    /// value(day) = floor + (start - floor) / 2^(day / halfLife), linearly interpolated inside each half-life.
    /// A deliberate approximation of exponential decay that is exact at every whole half-life.
    /// </summary>
    public static int Decay(int startPermille, int floorPermille, int halfLifeDays, int day)
    {
        if (halfLifeDays <= 0) return floorPermille;
        var d = Math.Max(0, day - 1);
        var halvings = d / halfLifeDays;
        if (halvings >= 20) return floorPermille;
        var span = startPermille - floorPermille;
        var hi = span >> halvings;
        var lo = span >> (halvings + 1);
        var into = d % halfLifeDays;
        var value = hi - (hi - lo) * into / halfLifeDays;
        return floorPermille + value;
    }
}
