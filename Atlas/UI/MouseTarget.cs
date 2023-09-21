using System.Numerics;
using SolidCode.Atlas.ECS;
using SolidCode.Atlas.Rendering;

namespace SolidCode.Atlas.UI;

public class MouseTarget : Component
{
    private static double _lastCalc;
    private static Entity? _currentTarget;
    private static readonly List<Entity> _currentTargets = new();
    private static readonly List<MouseTarget> _targets = new();
    private RectTransform _transform;

    public Action? OnMouseEnter;
    public Action? OnMouseExit;

    public static Entity? CurrentTarget
    {
        get
        {
            if (_lastCalc.Equals(Time.time))
            {
                CalculateTarget();
                _lastCalc = Time.time;
            }

            return _currentTarget;
        }
    }

    public bool IsHovered => CurrentTarget == Entity;
    public bool HoverInside => _currentTargets.Contains(Entity);

    public void Start()
    {
        _transform = Entity.GetComponent<RectTransform>();
        _targets.Add(this);
    }

    public void Update()
    {
        if (_lastCalc != Time.time)
        {
            CalculateTarget();
            _lastCalc = Time.time;
        }
    }

    public void OnRemove()
    {
        _targets.Remove(this);
    }

    protected Vector4 GetBounds()
    {
        var pos = _transform.Position.Evaluate() / 2f;
        var scale = _transform.Scale.Evaluate();
        var lower = pos - scale / 2f;
        var higher = pos + scale / 2f;
        return new Vector4(lower.X, lower.Y, higher.X, higher.Y);
    }

    protected bool IsInside(Vector2 point)
    {
        var bounds = GetBounds();
        return point.X >= bounds.X && point.X <= bounds.Z && point.Y >= bounds.Y && point.Y <= bounds.W;
    }

    private static void CalculateTarget()
    {
        Entity? currentTarget = null;
        var currentZ = float.MinValue;
        var currentLayer = uint.MinValue;

        var convertedPos = (Input.Input.MousePosition / Window.Size * new Vector2(1f, -1f) - new Vector2(0.5f, -0.5f)) *
                           Renderer.UnitScale;
        foreach (var target in _targets)
            if (target._transform.Layer > currentLayer ||
                (target._transform.GlobalZ > currentZ && target._transform.Layer == currentLayer))
                if (target.Enabled && target.IsInside(convertedPos))
                {
                    currentTarget = target.Entity;
                    currentZ = target._transform.GlobalZ;
                    currentLayer = target._transform.Layer;
                }

        if (currentTarget != _currentTarget)
        {
            currentTarget?.GetComponent<MouseTarget>()?.OnMouseEnter?.Invoke();
            _currentTarget?.GetComponent<MouseTarget>()?.OnMouseExit?.Invoke();
        }


        _currentTarget = currentTarget;

        _currentTargets.Clear();

        if (_currentTarget != null)
        {
            var curEntity = _currentTarget;
            _currentTargets.Add(curEntity);
            while (curEntity != EntityComponentSystem.RootEntity && curEntity.Parent != null)
            {
                curEntity = curEntity.Parent;
                _currentTargets.Add(curEntity);
            }
        }
    }
}