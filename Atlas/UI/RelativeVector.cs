using System.Numerics;
using SolidCode.Atlas.Rendering;

namespace SolidCode.Atlas.UI;

/// <summary>
/// A relative vector, used for UI positioning
/// </summary>
public class RelativeVector
{
    private Vector2 _cachedValue;
    private double _cacheTime;

    /// <summary>
    /// The parent of this relative vector
    /// </summary>
    public RelativeVector? Parent;

    /// <summary>
    /// The X value
    /// </summary>
    public float X;

    /// <summary>
    /// If the X axis should be relative
    /// </summary>
    public bool XRelative;

    /// <summary>
    /// The Y value
    /// </summary>
    public float Y;

    /// <summary>
    /// If the Y axis should be relative
    /// </summary>
    public bool YRelative;

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

    /// <summary>
    /// Evaluates the relative vector
    /// </summary>
    /// <returns>The evaluated vector</returns>
    public Vector2 Evaluate()
    {
        if (Time.time.Equals(_cacheTime))
            return _cachedValue;
        var parentEval = Renderer.UnitScale;
        if (Parent != null) parentEval = Parent.Evaluate();
        var ret = new Vector2(XRelative ? parentEval.X * X : X, YRelative ? parentEval.Y * Y : Y);
        if (!Time.time.Equals(_cacheTime))
        {
            _cacheTime = Time.time;
            _cachedValue = ret;
        }

        return ret;
    }
}