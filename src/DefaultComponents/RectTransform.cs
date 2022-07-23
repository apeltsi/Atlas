
namespace SolidCode.Caerus.Components
{
    using System.Numerics;
    using SolidCode.Caerus.ECS;
    using SolidCode.Caerus.Rendering;

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

        public override void Start()
        {
            Debug.Log(LogCategories.General, "Hello, im '" + entity.name +"' and this is my scale: " + GetAdjustedScale().ToString());
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
            Vector2 win = GetParentBoundingBox();
            Vector2 pos = (this.globalPosition + new Vector2(win.X * anchor.X, win.Y * anchor.Y)) / win - new Vector2(0.5f, 0.5f);
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
            float z = this.globalZ;
            return new Vector4(pos.X - scale.X / 2, pos.Y - scale.Y / 2, pos.X + scale.X / 2, pos.Y + scale.Y / 2);
        }

        public override Matrix4x4 GetTransformationMatrix()
        {
            Vector2 win = GetParentBoundingBox();
            Vector2 pos = (this.globalPosition + new Vector2(win.X * anchor.X, win.Y * anchor.Y)) / win - new Vector2(0.5f, 0.5f);
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
                0, 0, 1, 1f - trueZ,
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