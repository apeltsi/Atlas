
using System.Numerics;

namespace SolidCode.Atlas
{
    public static class Extensions
    {
        public static Vector3 AsVector3(this Vector4 vec)
        {
            return new Vector3(vec.X, vec.Y, vec.Z);
        }
        public static Vector3 AsVector3(this Vector2 vec)
        {
            return new Vector3(vec.X, vec.Y, 0f);
        }
        public static Vector4 AsVector4(this Vector3 vec)
        {
            return new Vector4(vec.X, vec.Y, vec.Z, 0f);
        }
    }
}