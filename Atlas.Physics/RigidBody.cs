using System.Numerics;
using SolidCode.Atlas.ECS;

namespace SolidCode.Atlas.Physics;

public abstract class RigidBody : Component
{
    protected PhysicsShape Shape = PhysicsShape.Box;
    private PhysicsObject? _physicsObject;
    public void OnEnable()
    {
        _physicsObject = new PhysicsObject(false, Shape, Entity.GetComponent<Transform>());
    }

    

    public void AddForce(Vector2 force)
    {
        _physicsObject.Body.ApplyForce(force.AsVec2(), _physicsObject.Body.GetWorldCenter());
    }

    public void OnDisable()
    {
        _physicsObject?.Dispose();
        _physicsObject = null;
    }
}

public class BoxRigidBody : RigidBody
{
    protected new PhysicsShape Shape = PhysicsShape.Box;
}

public class CircleRigidBody : RigidBody
{
    protected new PhysicsShape Shape = PhysicsShape.Circle;
    
}