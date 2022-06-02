using Veldrid;

namespace SolidCode.Caerus.Input
{
    public static class InputManager
    {
        private static List<Key> keys = new List<Key>();
        public static void ClearInputs()
        {
            keys.Clear();
        }

        public static void KeyPress(Key key)
        {
            keys.Add(key);
        }

        public static bool GetKey(Key key)
        {
            return keys.Contains(key);
        }
    }
}