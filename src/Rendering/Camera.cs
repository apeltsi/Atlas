namespace SolidCode.Atlas.Components
{
    using System.Numerics;
    using SolidCode.Atlas.ECS;
    [SingleInstance]
    public class Camera : Component
    {
        /// <summary>The camera position in world space</summary>
        private static Vector2 Position = Vector2.Zero;
        /// <summary>The camera "size". Think of this like zoom where a smaller number means that the camera is more zoomed in.</summary>
        private static Vector2 Scale = Vector2.One;
        Transform t;

        public void Start()
        {
            t = entity.GetComponent<Transform>();
            Position = t.globalPosition;
            Scale = t.globalScale;
        }
        public void Update()
        {
            Position = t.globalPosition;
            Scale = t.globalScale;
        }

        public static Matrix4x4 GetTransformMatrix()
        {
            Matrix4x4 scale = new Matrix4x4(
                1f / Scale.X, 0, 0, 0,
                0, 1f / Scale.Y, 0, 0, // NOTE: These are inverted because we "move" the world, not the viewport
                0, 0, 1, 0,
                0, 0, 0, 1);
            Matrix4x4 translate = new Matrix4x4(
                1f, 0, 0, -Position.X,
                0, 1f, 0, -Position.Y, // NOTE: These are inverted because we "move" the world, not the viewport
                0, 0, 1f, 0,
                0, 0, 0, 1f);
            return scale * translate;
        }

    }
}