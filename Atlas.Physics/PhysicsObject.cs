using Box2DX.Collision;
using Box2DX.Dynamics;
using SolidCode.Atlas.ECS;
using Math = System.Math;

namespace SolidCode.Atlas.Physics;

/// <summary>
/// Represents a object that is affected by physics
/// </summary>
internal class PhysicsObject
{
    internal Body? Body;
    private Fixture? _fixture;
    private PhysicsShape _shape;
    private bool _isStatic; 
    internal Transform Transform;
    private float _mass;

    public PhysicsObject(bool isStatic, PhysicsShape shape, Transform transform, float mass = 1f)
    {
        Transform = transform;
        _shape = shape;
        _isStatic = isStatic;
        _mass = isStatic ? 0 : mass;
        CreateResources();
    }

    internal void CreateResources()
    {
        if (Physics.World == null)
        {
            throw new NullReferenceException(
                "Physics have not been initialized yet. Cannot create PhysicsObject before Physics.InitializePhysics() is called");
        }
        
        Body = Physics.World.CreateBody(new BodyDef
        {
            MassData = new MassData
            {
                Mass = _mass,
            },
            Position = Transform.GlobalPosition.AsVec2(),
            Angle = Transform.GlobalRotation * (float)Math.PI / 180f,
            LinearVelocity = default,
            AllowSleep = _isStatic,
            IsSleeping = _isStatic,
            FixedRotation = _isStatic,
        });
        if(_isStatic)
            Body.SetStatic();
        FixtureDef fixtureDef;
        if (_shape == PhysicsShape.Box)
        {
            fixtureDef = new PolygonDef();
            ((PolygonDef)fixtureDef).SetAsBox(Transform.Scale.X, Transform.Scale.Y);
        }
        else
        {
            fixtureDef = new CircleDef
            {
                Radius = Transform.Scale.X / 2f
            };

        }
        
        
        _fixture = Body.CreateFixture(fixtureDef);
        lock(Physics.PhysicsObjects)
            Physics.PhysicsObjects.Add(this);
    }

    public void Update()
    {
        if (Body == null)
            return;
        Transform.GlobalPosition = Body.GetPosition().AsVector2();
        Transform.GlobalRotation = Body.GetAngle() * 180f / (float)Math.PI;
    }

    public void Dispose()
    {
        _fixture?.Dispose();
        Body?.Dispose();
        lock(Physics.PhysicsObjects)
            Physics.PhysicsObjects.Remove(this);
    }
}