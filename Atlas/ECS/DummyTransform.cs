using System.Numerics;
using SolidCode.Atlas.Rendering;

namespace SolidCode.Atlas.ECS;

/// <summary>
/// DO NOT USE AS A COMPONENT!
/// <para />
/// Can be used as a way to give a position, scale, rotation and z to a drawable without it being attached to a
/// </summary>
public class DummyTransform : Transform
{
    public override void RegisterDrawable(Drawable d)
    {
        // Lets do nothing because we're a dummy
    }

    public override void UnregisterDrawable(Drawable d)
    {
        // Same
    }

    public override Matrix4x4 GetTransformationMatrix()
    {
        var pos = Position;
        var scale = Scale;
        var rot = Rotation * ((float)Math.PI / 180f);
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