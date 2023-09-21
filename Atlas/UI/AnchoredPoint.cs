using System.Numerics;
using SolidCode.Atlas.Rendering;

namespace SolidCode.Atlas.UI;

/// <summary>
/// A UI anchor point
/// </summary>
public class AnchoredPoint
{
    private Vector2 _cachedPosition;
    private double _cacheTime;

    /// <summary>
    /// The anchor point
    /// </summary>
    public Vector2 AnchorPoint;

    /// <summary>
    /// The parent of the anchor point
    /// </summary>
    public RectTransform? Parent;

    /// <summary>
    /// The position of the anchor point
    /// </summary>
    public Vector2 Position;

    /// <summary>
    /// Creates a new anchor point
    /// </summary>
    /// <param name="anchorPoint"> The anchor point value </param>
    /// <param name="position"> The position relative to the anchor point </param>
    public AnchoredPoint(Vector2 anchorPoint, Vector2 position)
    {
        AnchorPoint = anchorPoint;
        Position = position;
    }

    /// <summary>
    /// Creates a new anchor point at the center
    /// </summary>
    /// <param name="position"> The position relative to the anchor point </param>
    public AnchoredPoint(Vector2 position)
    {
        Position = position;
        AnchorPoint = new Vector2(0.5f);
    }

    /// <summary>
    /// Evaluates the anchor point
    /// </summary>
    /// <returns> The evaluated position </returns>
    public Vector2 Evaluate()
    {
        if (Time.time == _cacheTime)
            return _cachedPosition;
        var ret = Position + (Parent?.Position.Evaluate() ?? Vector2.Zero);
        if (Parent == null)
            ret += (AnchorPoint * 2f - new Vector2(1f)) * Renderer.UnitScale;
        else
            ret += (AnchorPoint * 2f - new Vector2(1f)) * Parent.Scale.Evaluate();

        _cacheTime = Time.time;
        _cachedPosition = ret;

        return ret;
    }
}