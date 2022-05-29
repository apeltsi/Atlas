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
    }
}