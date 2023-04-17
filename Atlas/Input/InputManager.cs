using System.Numerics;
using Veldrid;

namespace SolidCode.Atlas.Input
{
    public static class InputManager
    {
        private static List<Key> keys = new();
        private static List<Key> downKeys = new();
        public static float WheelDelta { get; internal set; }
        public static void ClearInputs()
        {
            downKeys = new List<Key>();
        }

        public static void KeyPress(Key key)
        {
            if (!keys.Contains(key))
            {
                keys.Add(key);
                downKeys.Add(key);
            }
        }


        public static void RemoveKeyPress(Key key)
        {
            if (keys.Contains(key))
                keys.Remove(key);
        }

        public static bool GetKey(Key key)
        {
            return keys.Contains(key);
        }

        public static bool GetKeyDown(Key key)
        {
            return downKeys.Contains(key);
        }
    }
    public abstract class Input<T> {
        public Dictionary<Key, T> keyboardContributors = new();
        public Dictionary<Veldrid.Sdl2.SDL_GameControllerAxis, T> axisContributors = new();
        public abstract T Evaluate();
    }
    public class Action : Input<bool> {
        public override bool Evaluate() {
            return true;
        }
    }
    public class Axis1D : Input<float> {
        public override float Evaluate() {
            return 1f;
        }
    }
    public class Axis2D : Input<Vector2> {
        public override Vector2 Evaluate() {
            return Vector2.One;
        }
    }
}