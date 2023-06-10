using System.Numerics;
using SolidCode.Atlas.ECS;
using SolidCode.Atlas.Mathematics;
using SolidCode.Atlas.Rendering;
using SolidCode.Atlas.Telescope;
using SolidCode.Atlas.UI;
using static SolidCode.Atlas.Animation.Animation;

namespace SolidCode.Atlas.Animation
{
    public class ValueRef<T>
    {
        private Func<T> _get;
        private Action<T> _set;

        public ValueRef(Func<T> @get, Action<T> @set)
        {
            _get = @get;
            _set = @set;
        }

        public T Value
        {
            get { return _get(); }
            set { _set(value); }
        }
    }
    public static class TimingFunction
    {
        // https://easings.net/
        public static readonly Func<float, float> Linear = (x) => { return x; };
        public static readonly Func<float, float> EaseInOutSine = (x) => { return (float)-(Math.Cos(Math.PI * x) - 1f) / 2f; };
        public static readonly Func<float, float> EaseInOutCubic = (x) =>
        {
            if (x < 0.5)
            {
                return 4 * x * x * x;
            }
            else
            {
                return (float)(1f - Math.Pow(-2 * x + 2, 3) / 2);
            }
        };
        public static readonly Func<float, float> EaseInOutQuint = (x) =>
        {
            if (x < 0.5)
            {
                return 16 * x * x * x * x * x;
            }
            else
            {
                return (float)(1f - Math.Pow(-2 * x + 2, 5) / 2);
            }
        };

        public static readonly Func<float, float> EaseInSine = (x) =>
        {
            return 1f - (float)Math.Cos((x * Math.PI) / 2);
        };

        public static readonly Func<float, float> EaseInCubic = (x) =>
        {
            return x * x * x;
        };

        public static readonly Func<float, float> EaseInQuint = (x) =>
        {
            return x * x * x * x * x;
        };

        public static readonly Func<float, float> EaseOutSine = (x) =>
        {
            return 1f - (float)Math.Sin((x * Math.PI) / 2);
        };

        public static readonly Func<float, float> EaseOutCubic = (x) =>
        {
            return 1f - (float)Math.Pow(1 - x, 4);
        };

        public static readonly Func<float, float> EaseOutQuint = (x) =>
        {
            return 1f - (float)Math.Pow(1 - x, 5);
        };

    }
    public static class Animation
    {
        static List<ITween> tweens = new List<ITween>();
        private static bool isInitialized = false;
        
        public static TweenReference DoTween<T>(ValueRef<T> value, T end, float time, Action? onDone = null, Func<float, float>? timingFunction = null)
        {
            if (onDone == null)
            {
                onDone = () => { };
            }
            if (timingFunction == null)
            {
                timingFunction = TimingFunction.Linear;
            }

            if (!isInitialized)
            {
                EntityComponentSystem.RegisterUpdateAction(UpdateAnimations);
                isInitialized = true;
            }
            ITween t = new Tween<T>(value, end, time, onDone, timingFunction);
            tweens.Add(t);
            return new TweenReference(t);
        }
        public interface ITween
        {
            public bool Tick(float diff);
            public float age { get; }

        }

        public class TweenReference
        {
            private ITween _tween;
            public float Time => _tween.age;
            public bool IsPlaying => Animation.tweens.Contains(_tween);

            public TweenReference(ITween tween)
            {
                this._tween = tween;
            }

            public void Stop()
            {
                Animation.tweens.Remove(_tween);
            }
        }

        class Tween<T> : ITween
        {
            ValueRef<T> _value;
            T _end;
            T _start;
            float _duration;
            public float age { get; protected set; }
            Action _onDone;
            Func<float, float> _timingFunction;

            public Tween(ValueRef<T> value, T end, float duration, Action onDone, Func<float, float> timingFunction)
            {
                this._start = value.Value;
                this._value = value;
                this._duration = duration;
                this._end = end;
                this._onDone = onDone;
                this._timingFunction = timingFunction;
            }

            public bool Tick(float diff)
            {
                try
                {
                    age += diff;
                    float t = _timingFunction(Math.Clamp(age / _duration, 0, 1));
                    // Im really sorry for what im about to do...
                    if (typeof(T) == typeof(float))
                    {
                        this._value.Value = (T)(object)AMath.Lerp((float)(object)_start, (float)(object)_end, t);
                    }
                    else if (typeof(T) == typeof(int))
                    {
                        this._value.Value = (T)(object)AMath.Lerp((int)(object)_start, (int)(object)_end, t);
                    }
                    else if (typeof(T) == typeof(double))
                    {
                        this._value.Value = (T)(object)AMath.Lerp((double)(object)_start, (double)(object)_end, t);
                    }
                    else if (typeof(T) == typeof(Vector2))
                    {
                        this._value.Value = (T)(object)AMath.Lerp((Vector2)(object)_start, (Vector2)(object)_end, t);
                    }
                    else if (typeof(T) == typeof(Vector3))
                    {
                        this._value.Value = (T)(object)AMath.Lerp((Vector3)(object)_start, (Vector3)(object)_end, t);
                    }
                    else if (typeof(T) == typeof(Vector4))
                    {
                        this._value.Value = (T)(object)AMath.Lerp((Vector4)(object)_start, (Vector4)(object)_end, t);
                    }
                    else if (typeof(T) == typeof(RelativeVector))
                    {
                        this._value.Value = (T)(object)AMath.Lerp((RelativeVector)(object)_start,
                            (RelativeVector)(object)_end, t);
                    }
                    if (age > _duration)
                    {
                        this._value.Value = _end;
                        _onDone.Invoke();
                        return false;
                    }
                    return true;
                }
                catch (Exception e)
                {
                    if (age > _duration)
                    {
                        this._value.Value = _end;
                        _onDone.Invoke();
                        return false;
                    }
                    return true;
                }
            }
            

        }
        private static void UpdateAnimations()
        {
            List<ITween> curTweens = new List<ITween>(Animation.tweens);
            foreach (ITween tween in curTweens)
            {
                try
                {
                    if (!tween.Tick((float)Time.deltaTime))
                    {
                        tweens.Remove(tween);
                    }
                }
                catch (Exception e)
                {
                    Debug.Error(LogCategory.Framework, "Failed to tween: " + e.Message);
                    tweens.Remove(tween);
                }
            }
        }

    }
}