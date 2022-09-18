using Veldrid;

namespace SolidCode.Caerus.Input
{
    public static class InputManager
    {
        private static List<Key> keys = new List<Key>();
        private static List<Key> keysDown = new List<Key>();
        public static void ClearInputs()
        {
            keys.Clear();
            keysDown.Clear();
        }

        public static void KeyPress(Key key)
        {
            keys.Add(key);
        }

        public static void KeyPressDown(Key key)
        {
            keysDown.Add(key);
        }

        public static bool GetKey(Key key)
        {
            return keys.Contains(key);
        }

        public static bool GetKeyDown(Key key)
        {
            return keysDown.Contains(key);
        }
    }
}