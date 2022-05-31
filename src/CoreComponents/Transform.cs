namespace SolidCode.Caerus.Components
{
    using System.Numerics;
    using SolidCode.Caerus.ECS;
    public class Transform : Component
    {
        public Vector2 position;
        public Vector2 scale;
        public Vector2 globalPosition
        {
            get
            {
                if (entity == null)
                {
                    return Vector2.Zero;
                }
                if (entity.parent != null)
                {
                    // We have a parent! Lets check if it has a transform
                    Transform? t = entity.parent.GetComponent<Transform>();
                    if (t != null)
                    {
                        // We have a parent with a transform. Lets get its global position and add it to ours
                        return t.globalPosition + position;
                    }
                }
                return position;
            }
            set
            {
                if (entity == null)
                {
                    position = value;
                    return;
                }
                if (entity.parent != null)
                {
                    // We have a parent! Lets check if it has a transform
                    Transform? t = entity.parent.GetComponent<Transform>();
                    if (t != null)
                    {
                        // We have a parent with a transform. Lets get its global position and add it to ours
                        position = value - t.globalPosition;
                    }
                }

                position = value;
            }
        }
        public Vector2 globalScale
        {

            get
            {
                if (entity == null)
                {
                    return Vector2.One;
                }
                if (entity.parent != null)
                {
                    // We have a parent! Lets check if it has a transform
                    Transform? t = entity.parent.GetComponent<Transform>();
                    if (t != null)
                    {
                        // We have a parent with a transform. Lets get its global scale and multiply it by ours
                        return t.globalScale * scale;
                    }
                }
                return scale;
            }
        }
        public Vector2 inheritedScale = Vector2.One;

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

        public Matrix4x4 GetTransformationMatrix()
        {
            Vector2 pos = this.globalPosition;
            Vector2 scale = this.globalScale;

            return new Matrix4x4(
                scale.X, 0, 0, pos.X,
                0, scale.Y, 0, pos.Y,
                0, 0, 1, 0,
                0, 0, 0, 1);
        }

    }
}