using System.Numerics;

namespace SolidCode.Atlas.Standard;

public static class ARandom
{
    private static readonly Random _random = new();

    /// <summary>
    /// Returns a random value between min and max
    /// </summary>
    /// <param name="min"> The minimum value </param>
    /// <param name="max"> The maximum value </param>
    /// <returns> A random value between min and max </returns>
    public static float Range(float min, float max)
    {
        return _random.NextSingle() * (max - min) + min;
    }

    /// <summary>
    /// Returns a random value between min and max
    /// </summary>
    /// <param name="min"> The minimum value </param>
    /// <param name="max"> The maximum value </param>
    /// <returns> A random value between min and max </returns>
    public static double Range(double min, double max)
    {
        return _random.NextDouble() * (max - min) + min;
    }

    /// <summary>
    /// Returns a random value between min (inclusive) and max (exclusive)
    /// </summary>
    /// <param name="min"> The inclusive lower bound of the random number returned </param>
    /// <param name="max">
    /// The exclusive upper bound of the random number returned. maxValue must be greater than or equal to
    /// minValue
    /// </param>
    /// <returns> A random integer </returns>
    public static int Range(int min, int max)
    {
        return _random.Next(min, max);
    }

    /// <summary>
    /// Returns a random value between min (inclusive) and max (exclusive)
    /// </summary>
    /// <param name="min"> The inclusive lower bound of the random number returned </param>
    /// <param name="max">
    /// The exclusive upper bound of the random number returned. maxValue must be greater than or equal to
    /// minValue
    /// </param>
    /// <returns> A random unsigned-integer </returns>
    public static uint Range(uint min, uint max)
    {
        return (uint)_random.Next((int)min, (int)max);
    }

    /// <summary>
    /// Returns a random value between min (inclusive) and max (exclusive)
    /// </summary>
    /// <param name="min"> The inclusive lower bound of the random number returned </param>
    /// <param name="max">
    /// The exclusive upper bound of the random number returned. maxValue must be greater than or equal to
    /// minValue
    /// </param>
    /// <returns> A random long </returns>
    public static long Range(long min, long max)
    {
        return _random.NextInt64((int)min, (int)max);
    }

    /// <summary>
    /// A random floating-point value between 0 and 1
    /// </summary>
    /// <returns> </returns>
    public static float Value()
    {
        return _random.NextSingle();
    }

    /// <summary>
    /// A random double precision floating-point value between 0 and 1
    /// </summary>
    /// <returns> </returns>
    public static double ValueDouble()
    {
        return _random.NextDouble();
    }

    /// <summary>
    /// A random Vector2 where each component is between -1 and 1
    /// </summary>
    /// <returns> </returns>
    public static Vector2 Vector2()
    {
        return new Vector2(Range(-1f, 1f), Range(-1f, 1f));
    }
}