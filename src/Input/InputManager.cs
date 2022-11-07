using Veldrid;

namespace SolidCode.Atlas.Input
{
    public static class InputManager
    {
        private static List<Key> keys = new List<Key>();
        public static void ClearInputs()
        {
        }

        public static void KeyPress(Key key)
        {
            if (!keys.Contains(key))
                keys.Add(key);
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
    }
}