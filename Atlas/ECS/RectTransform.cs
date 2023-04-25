
namespace SolidCode.Atlas.ECS
{
    using System.Numerics;
    using SolidCode.Atlas.Rendering;

    public enum PositionMode
    {
        Absolute,
        Relative,
        Auto
    }
    public class RectTransform : Transform
    {
        public Vector2 anchor = new Vector2(0.5f, 0.5f);
        /// <summary> top | right | bottom | left </summary>
        //public Vector4 margin = new Vector4(5f, 5f, 5f, 5f);
        public bool widthRelative = false;
        public bool heightRelative = false;
        public PositionMode positionMode = PositionMode.Relative;
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
                        // parents position + (parents size * anchor) + (position / window size)
                        //                                                  converted size (pixels to screenspace) 
                        Vector2 win = Window.Size;
                        return (t.GlobalPosition - t.GlobalScale) + (t.GlobalScale * anchor) + (Position / new Vector2(win.X, win.Y));
                    }
                }
                return Position;
            }
            set
            {
                throw new NotImplementedException("Yaah! This isnt implemented yet!");
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
                        return t.GlobalScale * Scale;
                    }
                }
                return Scale;
            }
        }


        Vector2 GetParentBoundingBox()
        {
            Vector2 win = Window.Size;
            if (positionMode == PositionMode.Absolute || entity.parent == null || entity.parent.GetComponent<RectTransform>() == null)
            {
                return new Vector2(win.X, win.Y);
            }
            return new Vector2(win.X, win.Y) * entity.parent.GetComponent<RectTransform>().GetAdjustedScale();
        }
        public Vector2 GetAdjustedScale()
        {
            Vector2 win = GetParentBoundingBox();

            Vector2 scale = this.Scale;
            if (!widthRelative)
            {
                scale.X = scale.X / win.X;
            }
            if (!heightRelative)
            {
                scale.Y = scale.Y / win.Y;
            }

            return scale;
        }

        public Vector2 GetParentPosition()
        {
            if (positionMode == PositionMode.Absolute || entity.parent == null || entity.parent.GetComponent<RectTransform> == null)
            {
                return this.Position;
            }
            return entity.parent.GetComponent<RectTransform>().GetAdjustedScale();
        }

        public Vector4 GetBounds()
        {
            Vector2 parent = GetParentBoundingBox();
            Vector2 pos = this.globalPosition;
            if (!widthRelative)
            {
                Scale.X = Scale.X / parent.X;
            }
            if (!heightRelative)
            {
                Scale.Y = Scale.Y / parent.Y;
            }

            float rot = this.GlobalRotation;
            float z = this.GlobalZ;
            return new Vector4(pos.X - Scale.X / 2, pos.Y - Scale.Y / 2, pos.X + Scale.X / 2, pos.Y + Scale.Y / 2);
        }

        public override Matrix4x4 GetTransformationMatrix()
        {
            Vector2 win = GetParentBoundingBox();
            Vector2 pos = this.globalPosition;
            Vector2 scale = this.Scale;
            if (!widthRelative)
            {
                scale.X = scale.X / win.X;
            }
            if (!heightRelative)
            {
                scale.Y = scale.Y / win.Y;
            }

            float rot = this.GlobalRotation;
            float trueZ = this.GlobalZ;
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