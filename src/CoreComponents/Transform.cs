using System.Numerics;
using SolidCode.Caerus.ECS;

namespace SolidCode.Caerus.Components
{
    public class Transform : Component
    {
        public Vector2 position;
        public Vector2 scale;

        public Transform(Vector2 position, Vector2 scale)
        {
            this.position = position;
            this.scale = scale;
        }
        public Transform()
        {
            this.position = Vector2.Zero;
            this.scale = Vector2.One;
        }

        public Matrix4x4 GetPositionMatrix()
        {
            return new Matrix4x4(
                1, 0, 0, this.position.X,
                0, 1, 0, this.position.Y,
                0, 0, 1, 0,
                0, 0, 0, 1);
        }
        public Matrix4x4 GetScaleMatrix()
        {
            return new Matrix4x4(
                0, 0, 0, 0,
                0, 0, 0, 0,
                0, 0, 0, 0,
                0, 0, 0, 1);
        }
    }
}