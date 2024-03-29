using System.Numerics;
using SolidCode.Atlas.Rendering;

namespace SolidCode.Atlas.ECS;

public class Transform : Component
{
    private Drawable[] _drawables = new Drawable[0];

    private uint _layer;

    private float _z;

    /// <summary>
    /// Local position of entity
    /// </summary>
    public Vector2 Position;

    /// <summary>
    /// Local rotation of entity, measured in degrees
    /// </summary>
    public float Rotation;

    /// <summary>
    /// Local scale of entity
    /// </summary>
    public Vector2 Scale;

    public Transform(Vector2 position, Vector2 scale)
    {
        Position = position;
        Scale = scale;
    }

    public Transform()
    {
        Position = Vector2.Zero;
        Scale = Vector2.One;
    }

    public uint Layer
    {
        get => _layer;
        set
        {
            var prevValue = _layer;
            _layer = value;
            lock (_drawables)
            {
                foreach (var d in _drawables)
                    Renderer.ResortDrawable(d, prevValue);
            }
        }
    }

    /// <summary>
    /// Local z of entity
    /// </summary>
    public float Z
    {
        get => _z;
        set
        {
            _z = value;
            lock (_drawables)
            {
                foreach (var d in _drawables)
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
            if (Entity == null) return Position; // We might be a "dummy" transform
            if (Entity.Parent != null)
            {
                // We have a parent! Lets check if it has a transform
                var t = Entity.Parent.GetComponent<Transform>(true);
                if (t != null)
                    // We have a parent with a transform. Lets get its global position and add it to ours
                    return t.GlobalPosition + Position * t.GlobalScale;
            }

            return Position;
        }
        set
        {
            if (Entity == null)
            {
                Position = value;
                return;
            }

            if (Entity.Parent != null)
            {
                // We have a parent! Lets check if it has a transform
                var t = Entity.Parent.GetComponent<Transform>(true);
                if (t != null)
                    // We have a parent with a transform. Lets get its global position and add it to ours
                    Position = value - t.GlobalPosition;
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
            if (Entity == null) return Scale; // We might be a "dummy" transform
            if (Entity.Parent != null)
            {
                // We have a parent! Lets check if it has a transform
                var t = Entity.Parent.GetComponent<Transform>(true);
                if (t != null)
                    // We have a parent with a transform. Lets get its global scale and multiply it by ours
                    return t.GlobalScale * Scale;
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
            if (Entity == null) return Rotation; // We might be a "dummy" transform
            if (Entity.Parent != null)
            {
                // We have a parent! Lets check if it has a transform
                var t = Entity.Parent.GetComponent<Transform>(true);
                if (t != null)
                    // We have a parent with a transform. Lets get its global rotation and add it to ours
                    return t.GlobalRotation + Rotation;
            }

            return Rotation;
        }
        set
        {
            if (Entity == null)
            {
                Rotation = value;
                return;
            }

            if (Entity.Parent != null)
            {
                // We have a parent! Lets check if it has a transform
                var t = Entity.Parent.GetComponent<Transform>(true);
                if (t != null)
                    // We have a parent with a transform. Lets get its global rotation and add it to ours
                    Rotation = value - t.GlobalRotation;
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
            if (Entity == null) return Z; // We might be a "dummy" transform
            if (Entity.Parent != null)
            {
                // We have a parent! Lets check if it has a transform
                var t = Entity.Parent.GetComponent<Transform>(true);
                if (t != null)
                {
                    if (Z == 0)
                        // Lets get the next z level after our parent
                        // so that we aren't displayed under our parent
                        return Increment(t.GlobalZ);
                    // We have a parent with a transform. Lets get its global z and add it to ours
                    return t.GlobalZ + Z;
                }
            }

            return Z;
        }
        set
        {
            if (Entity == null)
            {
                Z = value;
                return;
            }

            if (Entity.Parent != null)
            {
                // We have a parent! Lets check if it has a transform
                var t = Entity.Parent.GetComponent<Transform>(true);
                if (t != null)
                    // We have a parent with a transform. Lets get its global z and add it to ours
                    Z = value - t.GlobalZ;
            }

            Z = value;
        }
    }

    private static unsafe float Increment(float f)
    {
        var val = *(int*)&f;
        if (f > 0)
            val++;
        else if (f < 0)
            val--;
        else if (f == 0)
            return float.Epsilon;
        return *(float*)&val;
    }

    public virtual void RegisterDrawable(Drawable d)
    {
        lock (_drawables)
        {
            var drawables = new Drawable[_drawables.Length + 1];
            _drawables.CopyTo(drawables, 0);
            drawables[^1] = d;
            _drawables = drawables;
        }
    }

    public virtual void UnregisterDrawable(Drawable d)
    {
        lock (_drawables)
        {
            if (_drawables.Contains(d))
            {
                var newArray = new Drawable[_drawables.Length - 1];
                var index = 0;
                foreach (var drawable in _drawables)
                {
                    if (drawable == d) continue;

                    newArray[index] = drawable;
                    index++;
                }

                _drawables = newArray;
            }
        }
    }


    public virtual Matrix4x4 GetTransformationMatrix()
    {
        var pos = GlobalPosition;
        var scale = GlobalScale;
        var rot = GlobalRotation * ((float)Math.PI / 180f);
        var scaleMat = new Matrix4x4(
            scale.X, 0, 0, 0,
            0, scale.Y, 0, 0,
            0, 0, 1, 0,
            0, 0, 0, 1
        );
        var rotation = new Matrix4x4(
            (float)Math.Cos(rot), (float)-Math.Sin(rot), 0, 0,
            (float)Math.Sin(rot), (float)Math.Cos(rot), 0, 0,
            0, 0, 1, 0,
            0, 0, 0, 1);
        var translation = new Matrix4x4(
            1, 0, 0, pos.X,
            0, 1, 0, pos.Y,
            0, 0, 1, 0,
            0, 0, 0, 1);

        return translation * rotation * scaleMat;
    }
}