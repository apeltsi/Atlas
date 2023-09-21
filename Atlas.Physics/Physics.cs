using System.Numerics;
using Box2DX.Collision;
using Box2DX.Common;
using Box2DX.Dynamics;
using SolidCode.Atlas.ECS;

namespace SolidCode.Atlas.Physics;

public enum PhysicsShape
{
    Circle,
    Box
}

public static class Physics
{
    public static float TimeScale = 1f;
    private static Vector2 _gravity = new(0f, -9.82f);

    internal static World? World;

    private static bool _tickPhysics = true;
    internal static List<PhysicsObject> PhysicsObjects = new();

    public static Vector2 GravitationalAcceleration
    {
        get => _gravity;
        set
        {
            _gravity = value;
            if (World != null)
                World.Gravity = _gravity.AsVec2();
        }
    }

    /// <summary>
    /// Initializes the (and if one already exists, disposes the previous) physics world.
    /// </summary>
    /// <param name="allowSleep">Should physics items be allowed to sleep</param>
    /// <param name="tickPhysics">Should physics be calculated every tick (true) or at every frame (false)</param>
    public static void InitializePhysics(bool allowSleep = true, bool tickPhysics = true)
    {
        if (World != null)
        {
            if (_tickPhysics)
                EntityComponentSystem.UnregisterTickAction(PhysicsStep);
            else
                EntityComponentSystem.UnregisterUpdateAction(PhysicsStep);
            World.Dispose();
        }

        _tickPhysics = tickPhysics;

        if (_tickPhysics)
            EntityComponentSystem.RegisterTickAction(PhysicsStep);
        else
            EntityComponentSystem.RegisterUpdateAction(PhysicsStep);

        World = new World(new AABB
        {
            LowerBound = new Vec2(-10f, -10f),
            UpperBound = new Vec2(10f, 10f)
        }, GravitationalAcceleration.AsVec2(), allowSleep);
        World.SetContinuousPhysics(true);
    }

    internal static Vec2 AsVec2(this Vector2 @this)
    {
        return new Vec2(@this.X, @this.Y);
    }

    internal static Vector2 AsVector2(this Vec2 @this)
    {
        return new Vector2(@this.X, @this.Y);
    }

    public static void PhysicsStep()
    {
        var delta = (float)Time.tickDeltaTime;
        if (!_tickPhysics)
            delta = (float)Time.deltaTime;
        World?.Step(delta, 4, 4);
        for (var i = 0; i < PhysicsObjects.Count; i++)
        {
            var po = PhysicsObjects[i];
            if (po.Body != null && !po.Body.IsSleeping()) po.Update();
        }
    }
}