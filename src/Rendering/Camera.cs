namespace SolidCode.Atlas.Components
{
    using System.Numerics;
    using SolidCode.Atlas.ECS;
    [SingleInstance]
    public class Camera : Component
    {
        /// <summary>The camera position in world space</summary>
        public static Vector2 Position = Vector2.Zero;
        /// <summary>The camera "size". Think of this like zoom where a smaller number means that the camera is more zoomed in.</summary>
        public static Vector2 Scale = Vector2.One;
        Transform t;

        public override void Start()
        {
            t = entity.GetComponent<Transform>();
            Position = t.globalPosition;
            Scale = t.globalScale;
        }
        public override void FixedUpdate()
        {
            Position = t.globalPosition;
            Scale = t.globalScale;

        }
        public override void Update()
        {
            Position = t.globalPosition;
            Scale = t.globalScale;
        }

        public static Matrix4x4 GetTransformMatrix()
        {
            return new Matrix4x4(
                1f / Scale.X, 0, 0, -Position.X,
                0, 1f / Scale.Y, 0, -Position.Y, // NOTE: These are inverted because we "move" the world, not the viewport
                0, 0, 1, 0,
                0, 0, 0, 1);
        }

    }
}