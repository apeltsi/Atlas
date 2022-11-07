namespace SolidCode.Atlas.Components
{
    using System.Numerics;
    using SolidCode.Atlas.ECS;
    public class Transform : Component
    {
        public Vector2 position;
        public Vector2 scale;
        public float rotation = 0f;
        public float z = 0.1f;
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
                    Transform? t = entity.parent.GetComponent<Transform>(true);
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
                    Transform? t = entity.parent.GetComponent<Transform>(true);
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
                    Transform? t = entity.parent.GetComponent<Transform>(true);
                    if (t != null)
                    {
                        // We have a parent with a transform. Lets get its global scale and multiply it by ours
                        return t.globalScale * scale;
                    }
                }
                return scale;
            }
        }
        public float globalRotation
        {
            get
            {
                if (entity == null)
                {
                    return 0;
                }
                if (entity.parent != null)
                {
                    // We have a parent! Lets check if it has a transform
                    Transform? t = entity.parent.GetComponent<Transform>(true);
                    if (t != null)
                    {
                        // We have a parent with a transform. Lets get its global rotation and add it to ours
                        return t.globalRotation + rotation;
                    }
                }
                return rotation;
            }
            set
            {
                if (entity == null)
                {
                    rotation = value;
                    return;
                }
                if (entity.parent != null)
                {
                    // We have a parent! Lets check if it has a transform
                    Transform? t = entity.parent.GetComponent<Transform>(true);
                    if (t != null)
                    {
                        // We have a parent with a transform. Lets get its global rotation and add it to ours
                        rotation = value - t.globalRotation;
                    }
                }

                rotation = value;
            }

        }
        public float globalZ
        {
            get
            {
                if (entity == null)
                {
                    Debug.Error("Entity is null, cant return global z");
                    return 0f;
                }
                if (entity.parent != null)
                {
                    // We have a parent! Lets check if it has a transform
                    Transform? t = entity.parent.GetComponent<Transform>(true);
                    if (t != null)
                    {
                        // We have a parent with a transform. Lets get its global z and add it to ours
                        return t.globalZ + z;
                    }
                }
                return z;
            }
            set
            {
                if (entity == null)
                {
                    z = value;
                    return;
                }
                if (entity.parent != null)
                {
                    // We have a parent! Lets check if it has a transform
                    Transform? t = entity.parent.GetComponent<Transform>(true);
                    if (t != null)
                    {
                        // We have a parent with a transform. Lets get its global z and add it to ours
                        z = value - t.globalZ;
                    }
                }

                z = value;
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

        public virtual Matrix4x4 GetTransformationMatrix()
        {
            Vector2 pos = this.globalPosition;
            Vector2 scale = this.globalScale;
            float rot = this.globalRotation;
            float trueZ = this.globalZ;
            Matrix4x4 translationAndPosition = new Matrix4x4(
                scale.X, 0, 0, pos.X,
                0, scale.Y, 0, pos.Y,
                0, 0, 1, 0,
                0, 0, 0, 1);
            Matrix4x4 rotation = new Matrix4x4(
                (float)Math.Cos(rot), (float)-Math.Sin(rot), 0, 0,
                (float)Math.Sin(rot), (float)Math.Cos(rot), 0, 0,
                0, 0, 1, 0,
                0, 0, 0, 1);
            return translationAndPosition * rotation;
        }

    }
}