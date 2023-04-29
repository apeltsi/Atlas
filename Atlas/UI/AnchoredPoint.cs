using System.Numerics;
using SolidCode.Atlas.ECS;
using SolidCode.Atlas.Rendering;

namespace SolidCode.Atlas.UI;

public class AnchoredPoint
{
    public Vector2 AnchorPoint;
    public RelativeVector Position;
    public RectTransform? Parent;
    private Vector2 cachedPositon;
    private double cacheTime;
    public AnchoredPoint(Vector2 anchorPoint, RelativeVector position)
    {
        AnchorPoint = anchorPoint;
        Position = position;
    }

    public AnchoredPoint(RelativeVector position)
    {
        this.Position = position;
        AnchorPoint = new Vector2(0.5f);
    }

    public Vector2 Evaluate()
    {
        if (Time.time == cacheTime)
            return cachedPositon;
        Vector2 ret = Position.Evaluate();
        if (Parent == null)
        {
            ret += AnchorPoint * Renderer.UnitScale;
        }
        else
        {
            ret += (AnchorPoint * 2f - new Vector2(1f)) * Parent.Scale.Evaluate();
        }
        
        if (Time.time != cacheTime)
        {
            cacheTime = Time.time;
            cachedPositon = ret;
        }

        return ret;
    }
    
}