
namespace SolidCode.Caerus.Rendering
{
    using System.Numerics;
    public static class Camera
    {
        /// <summary>The camera position in world space</summary>
        public static Vector2 Position = Vector2.Zero;
        /// <summary>The camera "size". Think of this like zoom where a smaller number means that the camera is more zoomed in.</summary>
        public static float Scale = 1f;

        public static Matrix4x4 GetTransformMatrix()
        {
            return new Matrix4x4(
                Scale, 0, 0, -Position.X,
                0, Scale, 0, -Position.Y, // NOTE: These are inverted because we "move" the world, not the viewport
                0, 0, 1, 0,
                0, 0, 0, 1);
        }
    }
}