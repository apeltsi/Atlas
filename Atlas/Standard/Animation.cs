using System.Numerics;
using SolidCode.Atlas.ECS;
using SolidCode.Atlas.Mathematics;
using SolidCode.Atlas.UI;

namespace SolidCode.Atlas.Animation;

public class ValueRef<T>
{
    private readonly Func<T> _get;
    private readonly Action<T> _set;

    public ValueRef(Func<T> get, Action<T> set)
    {
        _get = get;
        _set = set;
    }

    public T Value
    {
        get => _get();
        set => _set(value);
    }
}

public static class TimingFunction
{
    // https://easings.net/
    public static readonly Func<float, float> Linear = x => { return x; };

    public static readonly Func<float, float> EaseInOutSine = x =>
    {
        return (float)-(Math.Cos(Math.PI * x) - 1f) / 2f;
    };

    public static readonly Func<float, float> EaseInOutCubic = x =>
    {
        if (x < 0.5)
            return 4 * x * x * x;
        return (float)(1f - Math.Pow(-2 * x + 2, 3) / 2);
    };

    public static readonly Func<float, float> EaseInOutQuint = x =>
    {
        if (x < 0.5)
            return 16 * x * x * x * x * x;
        return (float)(1f - Math.Pow(-2 * x + 2, 5) / 2);
    };

    public static readonly Func<float, float> EaseInSine = x => { return 1f - (float)Math.Cos(x * Math.PI / 2); };

    public static readonly Func<float, float> EaseInCubic = x => { return x * x * x; };

    public static readonly Func<float, float> EaseInQuint = x => { return x * x * x * x * x; };

    public static readonly Func<float, float> EaseOutSine = x => { return 1f - (float)Math.Sin(x * Math.PI / 2); };

    public static readonly Func<float, float> EaseOutCubic = x => { return 1f - (float)Math.Pow(1 - x, 4); };

    public static readonly Func<float, float> EaseOutQuint = x => { return 1f - (float)Math.Pow(1 - x, 5); };
}

public static class Animation
{
    private static readonly List<ITween> tweens = new();
    private static bool isInitialized;

    public static TweenReference DoTween<T>(ValueRef<T> value, T end, float time, Action? onDone = null,
        Func<float, float>? timingFunction = null)
    {
        if (onDone == null) onDone = () => { };
        if (timingFunction == null) timingFunction = TimingFunction.Linear;

        if (!isInitialized)
        {
            EntityComponentSystem.RegisterUpdateAction(UpdateAnimations);
            isInitialized = true;
        }

        ITween t = new Tween<T>(value, end, time, onDone, timingFunction);
        tweens.Add(t);
        return new TweenReference(t);
    }

    private static void UpdateAnimations()
    {
        var curTweens = new List<ITween>(tweens);
        foreach (var tween in curTweens)
            try
            {
                if (!tween.Tick((float)Time.deltaTime)) tweens.Remove(tween);
            }
            catch (Exception e)
            {
                Debug.Error(LogCategory.Framework, "Failed to tween: " + e.Message);
                tweens.Remove(tween);
            }
    }

    public interface ITween
    {
        public float age { get; }
        public bool Tick(float diff);
    }

    public class TweenReference
    {
        private readonly ITween _tween;

        public TweenReference(ITween tween)
        {
            _tween = tween;
        }

        public float Time => _tween.age;
        public bool IsPlaying => tweens.Contains(_tween);

        public void Stop()
        {
            tweens.Remove(_tween);
        }
    }

    private class Tween<T> : ITween
    {
        private readonly float _duration;
        private readonly T _end;
        private readonly Action _onDone;
        private readonly T _start;
        private readonly Func<float, float> _timingFunction;
        private readonly ValueRef<T> _value;

        public Tween(ValueRef<T> value, T end, float duration, Action onDone, Func<float, float> timingFunction)
        {
            _start = value.Value;
            _value = value;
            _duration = duration;
            _end = end;
            _onDone = onDone;
            _timingFunction = timingFunction;
        }

        public float age { get; protected set; }

        public bool Tick(float diff)
        {
            try
            {
                age += diff;
                var t = _timingFunction(Math.Clamp(age / _duration, 0, 1));
                // Im really sorry for what im about to do...
                if (typeof(T) == typeof(float))
                    _value.Value = (T)(object)AMath.Lerp((float)(object)_start, (float)(object)_end, t);
                else if (typeof(T) == typeof(int))
                    _value.Value = (T)(object)AMath.Lerp((int)(object)_start, (int)(object)_end, t);
                else if (typeof(T) == typeof(double))
                    _value.Value = (T)(object)AMath.Lerp((double)(object)_start, (double)(object)_end, t);
                else if (typeof(T) == typeof(Vector2))
                    _value.Value = (T)(object)AMath.Lerp((Vector2)(object)_start, (Vector2)(object)_end, t);
                else if (typeof(T) == typeof(Vector3))
                    _value.Value = (T)(object)AMath.Lerp((Vector3)(object)_start, (Vector3)(object)_end, t);
                else if (typeof(T) == typeof(Vector4))
                    _value.Value = (T)(object)AMath.Lerp((Vector4)(object)_start, (Vector4)(object)_end, t);
                else if (typeof(T) == typeof(RelativeVector))
                    _value.Value = (T)(object)AMath.Lerp((RelativeVector)(object)_start,
                        (RelativeVector)(object)_end, t);
                if (age > _duration)
                {
                    _value.Value = _end;
                    _onDone.Invoke();
                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                if (age > _duration)
                {
                    _value.Value = _end;
                    _onDone.Invoke();
                    return false;
                }

                return true;
            }
        }
    }
}