using SolidCode.Atlas.Rendering;

namespace SolidCode.Atlas.ECS;

/// <summary>
/// DO NOT USE AS A COMPONENT!
/// <para/>
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
}