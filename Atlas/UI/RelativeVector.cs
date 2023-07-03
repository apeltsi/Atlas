using System.Numerics;
using SolidCode.Atlas.Rendering;

namespace SolidCode.Atlas.UI;

public class RelativeVector
{
    public float X;
    public float Y;
    public bool XRelative;
    public bool YRelative;
    public RelativeVector? Parent;
    private double _cacheTime;
    private Vector2 _cachedValue;
    /// <param name="x">The X value</param>
    /// <param name="y">The Y value</param>
    public RelativeVector(float x, float y)
    {
        X = x;
        Y = y;
    }
    /// <param name="x">The X value</param>
    /// <param name="xr">If the X axis should be relative</param>
    /// <param name="y">The Y value</param>
    /// <param name="yr">If the Y axis should be relative</param>
    public RelativeVector(float x, bool xr, float y, bool yr)
    {
        X = x;
        Y = y;
        XRelative = xr;
        YRelative = yr;
    }

    public Vector2 Evaluate()
    {
        if (Time.time.Equals(_cacheTime))
            return _cachedValue;
        Vector2 parentEval = Renderer.UnitScale;
        if (Parent != null)
        {
            parentEval = Parent.Evaluate();
        }
        Vector2 ret = new Vector2(XRelative ? parentEval.X * X : X, YRelative ? parentEval.Y * Y : Y);
        if (!Time.time.Equals(_cacheTime))
        {
            _cacheTime = Time.time;
            _cachedValue = ret;
        }
        return ret;
    }
}