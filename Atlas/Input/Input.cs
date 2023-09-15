using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Veldrid;
using Veldrid.Sdl2;
using static Veldrid.Sdl2.Sdl2Native;
namespace SolidCode.Atlas.Input
{
    public static class Input
    {
        private static List<Key> _keys = new();
        private static List<Key> _downKeys = new();
        private static List<Key> _upKeys = new();
        private static List<MouseButton> _mouseButtons = new();
        private static List<MouseButton> _downMouseButtons = new();
        private static List<MouseButton> _upMouseButtons = new();
        public static string ControllerName { get; private set; } 
        public static float WheelDelta { get; internal set; }
        public static Vector2 MousePosition = Vector2.Zero;
        private static SDL_GameController _controller;
        private static int _controllerIndex;
        private static Dictionary<SDL_GameControllerAxis, float> _axisValues = new ();

        private static Dictionary<SDL_GameControllerButton, bool> _buttonValues =
            new ();
        internal static void Initialize()
        {
            SDL_Init(SDLInitFlags.GameController);
            Sdl2Events.Subscribe(HandleEvent);
            TryGetDefaultController();
        }

        internal static void Dispose()
        {
            Sdl2Events.Unsubscribe(HandleEvent);
            try
            {
                SDL_GameControllerClose(_controller);
            }
            finally
            {
                
            }
            _downKeys.Clear();
            _upKeys.Clear();
            _upMouseButtons.Clear();
            _keys.Clear();
            _axisValues.Clear();
            _buttonValues.Clear();
            ControllerName = "";
            
        }
  

        internal static void UpdateInputs(InputSnapshot snapshot)
        {
            Sdl2Events.ProcessEvents();
            _downKeys.Clear();
            _upKeys.Clear();
            _upMouseButtons.Clear();
            WheelDelta = snapshot.WheelDelta;
            for (int i = 0; i < snapshot.KeyEvents.Count; i++)
            {
                KeyEvent e = snapshot.KeyEvents[i];

                if (e.Down == true)
                {
                    if (!_keys.Contains(e.Key))
                    {
                        _keys.Add(e.Key);
                        _downKeys.Add(e.Key);
                    }
                }
                else
                {
                    _keys.Remove(e.Key);
                    _upKeys.Add(e.Key);
                }
            }
            MousePosition = snapshot.MousePosition;
            _downMouseButtons.Clear();
            foreach (var mevent in snapshot.MouseEvents)
            {
                if (mevent.Down)
                {
                    _mouseButtons.Add(mevent.MouseButton);
                    _downMouseButtons.Add(mevent.MouseButton);
                }
                else
                {
                    _mouseButtons.Remove(mevent.MouseButton);
                    _upMouseButtons.Add(mevent.MouseButton);
                }
            }

        }
        
        private static void HandleEvent(ref SDL_Event ev)
        {
            switch (ev.type)
            {
                case SDL_EventType.ControllerDeviceAdded:
                    Debug.Log(LogCategory.Framework, "Controller Connected");
                    TryGetDefaultController();
                    break;
                case SDL_EventType.ControllerDeviceRemoved:
                    Debug.Log(LogCategory.Framework, "Controller Disconnected");
                    break;
                case SDL_EventType.ControllerButtonUp:
                case SDL_EventType.ControllerButtonDown:
                    SDL_ControllerButtonEvent btnEvent = Unsafe.As<SDL_Event, SDL_ControllerButtonEvent>(ref ev);
                    if (btnEvent.which == _controllerIndex)
                    {
                        _buttonValues[btnEvent.button] = btnEvent.state == 1;
                    }
                    break;
                case SDL_EventType.ControllerAxisMotion:
                    SDL_ControllerAxisEvent axisEvent = Unsafe.As<SDL_Event, SDL_ControllerAxisEvent>(ref ev);
                    if (axisEvent.which == _controllerIndex)
                    {
                        _axisValues[axisEvent.axis] = Normalize(axisEvent.value);
                    }
                    break;
            }
            
        }

        private static float Normalize(short value)
        {
            return value < 0 ? -(value / (float)short.MinValue) : (value / (float)short.MaxValue);
        }

        private static unsafe void TryGetDefaultController()
        {
            int jsCount = SDL_NumJoysticks();
            Debug.Log(LogCategory.Framework, "Joystick Count: " + jsCount);
            for (int i = 0; i < jsCount; i++)
            {
                if (SDL_IsGameController(i))
                {
                    _controller = SDL_GameControllerOpen(i);
                    ControllerName = Marshal.PtrToStringUTF8((IntPtr)SDL_GameControllerName(_controller)) ?? "Unknown";
                    SDL_Joystick joystick = SDL_GameControllerGetJoystick(_controller);
                    _controllerIndex = SDL_JoystickInstanceID(joystick);
                    return;
                }
            }
        }


        
        public static bool GetKey(Key key)
        {
            return _keys.Contains(key);
        }

        public static bool GetKeyDown(Key key)
        {
            return _downKeys.Contains(key);
        }

        public static bool GetKeyUp(Key key)
        {
            return _upKeys.Contains(key);
        }
        
        public static bool GetMouseButton(MouseButton button)
        {
            return _mouseButtons.Contains(button);
        }
        
        public static bool GetMouseButtonDown(MouseButton button)
        {
            return _downMouseButtons.Contains(button);
        }

        public static bool GetMouseButtonUp(MouseButton button)
        {
            return _upMouseButtons.Contains(button);
        }

        public static float GetControllerAxis(SDL_GameControllerAxis axis)
        {
            _axisValues.TryGetValue(axis, out float ret);
            if(ret < 0.1f && ret > -0.1f)
                return 0;
            return ret;
        }
        
        public static bool GetControllerButton(SDL_GameControllerButton button)
        {
            _buttonValues.TryGetValue(button, out bool ret);
            return ret;
        }
    }

    public struct Keymap<T>
    {
        public Dictionary<Key, T> KeyboardContributors = new ();
        public Dictionary<SDL_GameControllerAxis, T> AxisContributors = new ();
        public Dictionary<SDL_GameControllerButton, T> ButtonContributors = new ();

        public Keymap(Dictionary<Key, T> keyboardContributors, Dictionary<SDL_GameControllerAxis, T> axisContributors, Dictionary<SDL_GameControllerButton, T> buttonContributors)
        {
            KeyboardContributors = keyboardContributors;
            AxisContributors = axisContributors;
            ButtonContributors = buttonContributors;
        }

    }
    
    public abstract class InputListener<T> {
        public Keymap<T> Keymap;

        public abstract T Evaluate();

        protected InputListener(Keymap<T> keymap)
        {
            this.Keymap = keymap;
            if (keymap.AxisContributors == null)
            {
                this.Keymap.AxisContributors = new();
            }
            if (keymap.ButtonContributors == null)
            {
                this.Keymap.ButtonContributors = new();
            }
            if (keymap.KeyboardContributors == null)
            {
                this.Keymap.KeyboardContributors = new();
            }
        }
        
        protected (Dictionary<Key, bool>, Dictionary<SDL_GameControllerAxis, float>, Dictionary<SDL_GameControllerButton, bool>) GetContributors() {
            Dictionary<Key, bool> keys = new();
            Dictionary<SDL_GameControllerAxis, float> axes = new();
            Dictionary<SDL_GameControllerButton, bool> buttons = new();
            foreach (var key in Keymap.KeyboardContributors.Keys) {
                keys.Add(key, Input.GetKey(key));
            }
            foreach (var axis in Keymap.AxisContributors.Keys) {
                axes.Add(axis, Input.GetControllerAxis(axis));
            }
            foreach (var button in Keymap.ButtonContributors.Keys) {
                buttons.Add(button, Input.GetControllerButton(button));
            }

            return (keys, axes, buttons);
        }
    }
    public class Action : InputListener<bool> {
        public Action(Keymap<bool> keymap) : base(keymap)
        {
        }

        public override bool Evaluate()
        {
            (var keys, var axes, var buttons) = GetContributors();
            foreach (var key in keys.Keys)
            {
                bool value = Keymap.KeyboardContributors[key];
                if (keys[key] && value)
                {
                    return true;
                }
            }
            foreach (var axis in axes.Keys)
            {
                if (axes[axis] > 0.75f && Keymap.AxisContributors[axis])
                {
                    return true;
                }
            }
            foreach (var button in buttons.Keys)
            {
                if (buttons[button] && Keymap.ButtonContributors[button])
                {
                    return true;
                }
            }
            return false;
        }
    }
    public class Axis1D : InputListener<float> {
        public Axis1D(Keymap<float> keymap) : base(keymap)
        {
        }

        public override float Evaluate() {
            (var keys, var axes, var buttons) = GetContributors();
            float value = 0;
            foreach (var key in keys.Keys)
            {
                float contribution = Keymap.KeyboardContributors[key];
                if (keys[key])
                {
                    value += contribution;
                }
            }
            foreach (var axis in axes.Keys)
            {
                float contribution = Keymap.AxisContributors[axis];
                value += axes[axis] * contribution;
            }
            foreach (var button in buttons.Keys)
            {
                float contribution = Keymap.ButtonContributors[button];
                if (buttons[button])
                {
                    value += contribution;
                }
            }
            
            return Math.Clamp(value, -1f, 1f);
        }
    }
    public class Axis2D : InputListener<Vector2> {
        public Axis2D(Keymap<Vector2> keymap) : base(keymap)
        {
        }

        public override Vector2 Evaluate() {
            (var keys, var axes, var buttons) = GetContributors();
            Vector2 value = Vector2.Zero;
            foreach (var key in keys.Keys)
            {
                Vector2 contribution = Keymap.KeyboardContributors[key];
                if (keys[key])
                {
                    value += contribution;
                }
            }
            foreach (var axis in axes.Keys)
            {
                Vector2 contribution = Keymap.AxisContributors[axis];
                value += axes[axis] * contribution;
            }
            foreach (var button in buttons.Keys)
            {
                Vector2 contribution = Keymap.ButtonContributors[button];
                if (buttons[button])
                {
                    value += contribution;
                }
            }

            Vector2 val = Vector2.Normalize(value);

            // Remove any NANs
            if (float.IsNaN(val.X))
            {
                val.X = 0;
            }

            if (float.IsNaN(val.Y))
            {
                val.Y = 0;
            }
            
            return val;
        }
    }
}