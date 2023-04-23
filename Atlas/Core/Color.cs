// TODO: Standard color class
using System.Globalization;
using System.Numerics;
using SolidCode.Atlas.Telescope;
namespace SolidCode.Atlas
{

    public static class Color
    {
        public static Vector4 HexToColor(string hex)
        {
            string fullhex = hex;
            Vector4 color = new Vector4(1f, 1f, 1f, 1f);
            if (hex.StartsWith("#"))
            {
                hex = hex.Substring(1);
            }
            if (hex.Length != 6 && hex.Length != 8)
            {
                Debug.Error("Invalid hex color: \"" + fullhex + "\". Returning default color");
                return color;
            }
            if (hex.Length >= 6)
            {
                // RGB
                uint value = 255;
                uint.TryParse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
                color.X = (float)value / 255f;
                uint.TryParse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
                color.Y = (float)value / 255f;
                uint.TryParse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
                color.Z = (float)value / 255f;
            }
            if (hex.Length == 8)
            {
                // Alpha
                uint value = 255;
                uint.TryParse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
                color.Z = (float)value / 255f;
            }
            return color;
        }
    }
}