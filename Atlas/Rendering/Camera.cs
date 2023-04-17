namespace SolidCode.Atlas.Components
{
    using System.Numerics;
    using ECS;
    [SingleInstance]
    public class Camera : Component
    {
        /// <summary>The camera position in world space</summary>
        private static Vector2 _position = Vector2.Zero;
        /// <summary>The camera "size". Think of this like zoom where a smaller number means that the camera is more zoomed in.</summary>
        private static Vector2 _scale = Vector2.One;
        private Transform? _t;

        public void Start()
        {
            _t = entity?.GetComponent<Transform>();
            if (_t == null) return;
            _position = _t.globalPosition;
            _scale = _t.globalScale;
            
        }
        public void Update()
        {
            if (_t == null) return;
            _position = _t.globalPosition;
            _scale = _t.globalScale;
        }

        public static Matrix4x4 GetTransformMatrix()
        {
            Matrix4x4 scale = new Matrix4x4(
                1f / _scale.X, 0, 0, 0,
                0, 1f / _scale.Y, 0, 0, // NOTE: These are inverted because we "move" the world, not the viewport
                0, 0, 1, 0,
                0, 0, 0, 1);
            Matrix4x4 translate = new Matrix4x4(
                1f, 0, 0, -_position.X,
                0, 1f, 0, -_position.Y, // NOTE: These are inverted because we "move" the world, not the viewport
                0, 0, 1f, 0,
                0, 0, 0, 1f);
            return scale * translate;
        }

    }
}