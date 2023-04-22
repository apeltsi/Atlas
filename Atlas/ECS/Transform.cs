namespace SolidCode.Atlas.ECS
{
    using System.Collections.Concurrent;
    using System.Numerics;
    using SolidCode.Atlas.ECS;
    using SolidCode.Atlas.Rendering;
    using SolidCode.Atlas.Standard;

    public class Transform : Component
    {
        /// <summary>
        /// Local position of entity
        /// </summary>
        public Vector2 position;
        /// <summary>
        /// Local scale of entity
        /// </summary>
        public Vector2 scale;
        /// <summary>
        /// Local rotation of entity, measured in degrees
        /// </summary>

        public float rotation = 0f;
        private float _z = 1f;
        /// <summary>
        /// Local z of entity
        /// </summary>

        private ManualConcurrentList<Drawable> _drawables = new ManualConcurrentList<Drawable>();
        public float z
        {
            get
            {
                return _z;
            }
            set
            {
                _z = value;
                _drawables.Update();
                foreach (Drawable d in _drawables)
                {
                    Window.ResortDrawable(d);
                }
            }
        }
        /// <summary>
        /// Global position of entity
        /// </summary>

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
                        return t.globalPosition + position * t.globalScale;
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
        /// <summary>
        /// Global scale of entity
        /// </summary>

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
        /// <summary>
        /// Global rotation of entity, measured in degrees
        /// </summary>

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
        /// <summary>
        /// Global z of entity
        /// </summary>

        public float globalZ
        {
            get
            {
                if (entity == null)
                {
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

        public void RegisterDrawable(Drawable d)
        {
            _drawables.Add(d);
        }
        public void UnregisterDrawable(Drawable d)
        {
            if (_drawables.Contains(d))
                _drawables.Remove(d);
        }


        public virtual Matrix4x4 GetTransformationMatrix()
        {
            Vector2 pos = this.globalPosition;
            Vector2 scale = this.globalScale;
            float rot = this.globalRotation * ((float)Math.PI / 180f);
            Matrix4x4 scaleMat = new Matrix4x4(
                scale.X, 0, 0, 0,
                0, scale.Y, 0, 0,
                0, 0, 1, 0,
                0, 0, 0, 1
            );
            Matrix4x4 rotation = new Matrix4x4(
                (float)Math.Cos(rot), (float)-Math.Sin(rot), 0, 0,
                (float)Math.Sin(rot), (float)Math.Cos(rot), 0, 0,
                0, 0, 1, 0,
                0, 0, 0, 1);
            Matrix4x4 translation = new Matrix4x4(
                1, 0, 0, pos.X,
                0, 1, 0, pos.Y,
                0, 0, 1, 0,
                0, 0, 0, 1);

            return translation * rotation * scaleMat;
        }

    }
}