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
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t)
        {
            return Vector3.Lerp(a, b, t);
        }
        public static Vector4 Lerp(Vector4 a, Vector4 b, float t)
        {
            return Vector4.Lerp(a, b, t);
        }

        public static int RoundToInt(float value)
        {
            return (int)Math.Round(value);
        }
        public static int RoundToInt(double value)
        {
            return (int)Math.Round(value);
        }

        public static int FloorToInt(float value)
        {
            return (int)Math.Floor(value);
        }
        public static int CeilToInt(float value)
        {
            return (int)Math.Ceiling(value);
        }

    }
}