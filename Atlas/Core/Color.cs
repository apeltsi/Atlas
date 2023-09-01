// TODO: Standard color class
using System.Globalization;
using System.Numerics;
using SolidCode.Atlas.Rendering;
using SolidCode.Atlas.Telescope;
namespace SolidCode.Atlas
{

    public struct Color
    {
        public float R;
        public float G;
        public float B;
        public float A = 1f;
        
        // Default Colors
        public static Color White => new (1f, 1f, 1f, 1f);
        public static Color Black => new (0f, 0f, 0f, 1f);
        public static Color Red => new (1f, 0f, 0f, 1f);
        public static Color Green => new (0f, 1f, 0f, 1f);
        public static Color Blue => new (0f, 0f, 1f, 1f);
        public static Color TransparentWhite => new (1f, 1f, 1f, 0f);
        public static Color TransparentBlack => new (0f, 0f, 0f, 0f);
        public Color(float r, float g, float b)
        {
            R = r;
            G = g;
            B = b;
            A = 1f;
        }

        public Color(float r, float g, float b, float a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }
        
        public Color(string hex)
        {
            Color c = HexToColor(hex);
            R = c.R;
            G = c.G;
            B = c.B;
            A = c.A;
        }
        public static Color HexToColor(string hex)
        {
            string fullhex = hex;
            Color color = new Color(1f, 1f, 1f, 1f);
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
                color.R = (float)value / 255f;
                uint.TryParse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
                color.G = (float)value / 255f;
                uint.TryParse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
                color.B = (float)value / 255f;
            }
            if (hex.Length == 8)
            {
                // Alpha
                uint value = 255;
                uint.TryParse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
                color.A = (float)value / 255f;
            }
            return color;
        }

        public static implicit operator Vector4(Color c)
        {
            return new Vector4(c.R, c.G, c.B, c.A);
        }

        public static implicit operator Color(Vector4 v)
        {
            return new Color(v.X, v.Y, v.Z, v.W);
        }

        public static bool operator ==(Color a, Color b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Color a, Color b)
        {
            return !a.Equals(b);
        }

        public override bool Equals(object? obj)
        {
            if (!(obj is Color))
                return false;
            Color c = (Color)obj;
            return c.R == R && c.G == G && c.B == B && c.A == A;
        }
        
        public override int GetHashCode()
        {
            return R.GetHashCode() ^ G.GetHashCode() ^ B.GetHashCode() ^ A.GetHashCode();
        }
    }
}