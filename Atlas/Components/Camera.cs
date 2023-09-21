using System.Numerics;
using SolidCode.Atlas.ECS;

namespace SolidCode.Atlas.Components;

[SingleInstance]
public class Camera : Component
{
    /// <summary> The camera position in world space </summary>
    private static Vector2 _position = Vector2.Zero;

    /// <summary> The camera "size". Think of this like zoom where a smaller number means that the camera is more zoomed in. </summary>
    private static Vector2 _scale = Vector2.One;

    private Transform? _t;

    public void Start()
    {
        _t = Entity?.GetComponent<Transform>();
        if (_t == null) return;
        _position = _t.GlobalPosition;
        _scale = _t.GlobalScale;
    }

    public void Update()
    {
        if (_t == null) return;
        _position = _t.GlobalPosition;
        _scale = _t.GlobalScale;
    }

    public void OnDisable()
    {
        _position = Vector2.Zero;
        _scale = Vector2.One;
    }

    public static Matrix4x4 GetTransformMatrix()
    {
        var scale = new Matrix4x4(
            1f / _scale.X, 0, 0, 0,
            0, 1f / _scale.Y, 0, 0,
            0, 0, 1, 0,
            0, 0, 0, 1);
        var translate = new Matrix4x4(
            1f, 0, 0, -_position.X, // NOTE: These are inverted because we "move" the world, not the viewport
            0, 1f, 0, -_position.Y,
            0, 0, 1f, 0,
            0, 0, 0, 1f);
        return scale * translate;
    }

    public static Vector2 GetScaling()
    {
        return new Vector2(1f / _scale.X, 1f / _scale.Y);
    }
}