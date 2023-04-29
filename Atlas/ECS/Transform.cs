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
        public Vector2 Position;
        /// <summary>
        /// Local scale of entity
        /// </summary>
        public Vector2 Scale;
        /// <summary>
        /// Local rotation of entity, measured in degrees
        /// </summary>
        public float Rotation = 0f;

        private uint _layer = 0;

        public uint Layer
        {
            get => _layer;
            set
            {
                _layer = value;
                _drawables.Update();
                foreach (Drawable d in _drawables)
                {
                    Renderer.ResortDrawable(d);
                }
            }
        }
        
        private float _z = 1f;

        private ManualConcurrentList<Drawable> _drawables = new ManualConcurrentList<Drawable>();
        /// <summary>
        /// Local z of entity
        /// </summary>
        public float Z
        {
            get => _z;
            set
            {
                _z = value;
                _drawables.Update();
                foreach (Drawable d in _drawables)
                {
                    Renderer.ResortDrawable(d);
                }
            }
        }
        /// <summary>
        /// Global position of entity
        /// </summary>

        public Vector2 GlobalPosition
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
                        return t.GlobalPosition + Position * t.GlobalScale;
                    }
                }
                return Position;
            }
            set
            {
                if (entity == null)
                {
                    Position = value;
                    return;
                }
                if (entity.parent != null)
                {
                    // We have a parent! Lets check if it has a transform
                    Transform? t = entity.parent.GetComponent<Transform>(true);
                    if (t != null)
                    {
                        // We have a parent with a transform. Lets get its global position and add it to ours
                        Position = value - t.GlobalPosition;
                    }
                }

                Position = value;
            }
        }
        /// <summary>
        /// Global scale of entity
        /// </summary>

        public Vector2 GlobalScale
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
                        return t.GlobalScale * Scale;
                    }
                }
                return Scale;
            }
        }
        /// <summary>
        /// Global rotation of entity, measured in degrees
        /// </summary>

        public float GlobalRotation
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
                        return t.GlobalRotation + Rotation;
                    }
                }
                return Rotation;
            }
            set
            {
                if (entity == null)
                {
                    Rotation = value;
                    return;
                }
                if (entity.parent != null)
                {
                    // We have a parent! Lets check if it has a transform
                    Transform? t = entity.parent.GetComponent<Transform>(true);
                    if (t != null)
                    {
                        // We have a parent with a transform. Lets get its global rotation and add it to ours
                        Rotation = value - t.GlobalRotation;
                    }
                }

                Rotation = value;
            }

        }
        /// <summary>
        /// Global z of entity
        /// </summary>

        public float GlobalZ
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
                        return t.GlobalZ + Z;
                    }
                }
                return Z;
            }
            set
            {
                if (entity == null)
                {
                    Z = value;
                    return;
                }
                if (entity.parent != null)
                {
                    // We have a parent! Lets check if it has a transform
                    Transform? t = entity.parent.GetComponent<Transform>(true);
                    if (t != null)
                    {
                        // We have a parent with a transform. Lets get its global z and add it to ours
                        Z = value - t.GlobalZ;
                    }
                }

                Z = value;
            }
        }


        public Transform(Vector2 position, Vector2 scale)
        {
            this.Position = position;
            this.Scale = scale;
        }
        public Transform()
        {
            this.Position = Vector2.Zero;
            this.Scale = Vector2.One;
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
            Vector2 pos = this.GlobalPosition;
            Vector2 scale = this.GlobalScale;
            float rot = this.GlobalRotation * ((float)Math.PI / 180f);
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