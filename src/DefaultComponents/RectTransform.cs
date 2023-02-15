
namespace SolidCode.Atlas.Components
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
                        return (t.globalPosition - t.globalScale) + (t.globalScale * anchor) + (position / new Vector2(Window.window.Width, Window.window.Height));
                    }
                }
                return position;
            }
            set
            {
                throw new NotImplementedException("Yaah! This isnt implemented yet!");
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

        public override void Start()
        {
            Debug.Log(LogCategory.General, "Hello, im '" + entity.name + "' and this is my scale: " + GetAdjustedScale().ToString());
        }

        Vector2 GetParentBoundingBox()
        {
            if (positionMode == PositionMode.Absolute || entity.parent == null || entity.parent.GetComponent<RectTransform>() == null)
            {
                return new Vector2(Window.window.Width, Window.window.Height);
            }
            return new Vector2(Window.window.Width, Window.window.Height) * entity.parent.GetComponent<RectTransform>().GetAdjustedScale();
        }
        public Vector2 GetAdjustedScale()
        {
            Vector2 win = GetParentBoundingBox();

            Vector2 scale = this.scale;
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
                return this.position;
            }
            return entity.parent.GetComponent<RectTransform>().GetAdjustedScale();
        }

        public Vector4 GetBounds()
        {
            Vector2 parent = GetParentBoundingBox();
            Vector2 pos = this.globalPosition;
            if (!widthRelative)
            {
                scale.X = scale.X / parent.X;
            }
            if (!heightRelative)
            {
                scale.Y = scale.Y / parent.Y;
            }

            float rot = this.globalRotation;
            float z = this.globalZ;
            return new Vector4(pos.X - scale.X / 2, pos.Y - scale.Y / 2, pos.X + scale.X / 2, pos.Y + scale.Y / 2);
        }

        public override Matrix4x4 GetTransformationMatrix()
        {
            Vector2 win = GetParentBoundingBox();
            Vector2 pos = this.globalPosition;
            Vector2 scale = this.scale;
            if (!widthRelative)
            {
                scale.X = scale.X / win.X;
            }
            if (!heightRelative)
            {
                scale.Y = scale.Y / win.Y;
            }

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