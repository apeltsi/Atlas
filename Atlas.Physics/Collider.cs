using SolidCode.Atlas.ECS;

namespace SolidCode.Atlas.Physics;

public abstract class Collider : Component
{
    private PhysicsObject? _physicsObject;
    protected PhysicsShape Shape = PhysicsShape.Box;

    public void OnEnable()
    {
        _physicsObject = new PhysicsObject(true, Shape, Entity.GetComponent<Transform>()!);
    }

    public void OnDisable()
    {
        _physicsObject?.Dispose();
        _physicsObject = null;
    }
}

public class BoxCollider : Collider
{
    protected new PhysicsShape Shape = PhysicsShape.Box;
}

public class CircleCollider : Collider
{
    protected new PhysicsShape Shape = PhysicsShape.Circle;
}