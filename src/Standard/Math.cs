using System.Numerics;

namespace SolidCode.Atlas.Mathematics
{
    public static class AMath
    {
        public static float Lerp(float a, float b, float t)
        {
            return a * (1 - t) + b * t;
        }

        public static double Lerp(double a, double b, float t)
        {
            return a * (1 - t) + b * t;
        }

        public static int Lerp(int a, int b, float t)
        {
            return RoundToInt(a * (1 - t) + b * t);
        }

        public static Vector2 Lerp(Vector2 a, Vector2 b, float t)
        {
            return Vector2.Lerp(a, b, t);
        }

        public static int RoundToInt(float value)
        {
            return (int)Math.Round(value);
        }

    }
}