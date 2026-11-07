using System;
using System.Globalization;

namespace Tesela.Core
{
    /// <summary>Color RGBA de 8 bits independiente del motor; la capa Unity lo convierte a Color32.</summary>
    [Serializable]
    public struct Rgba : IEquatable<Rgba>
    {
        public byte R, G, B, A;

        public Rgba(byte r, byte g, byte b, byte a = 255) { R = r; G = g; B = b; A = a; }

        public static Rgba FromHex(string hex)
        {
            if (string.IsNullOrEmpty(hex)) throw new ArgumentException("hex vacío");
            string h = hex[0] == '#' ? hex.Substring(1) : hex;
            if (h.Length != 6 && h.Length != 8) throw new FormatException("Color inválido: " + hex);
            byte P(int i) => byte.Parse(h.Substring(i, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return new Rgba(P(0), P(2), P(4), h.Length == 8 ? P(6) : (byte)255);
        }

        public string ToHex() => "#" + R.ToString("X2") + G.ToString("X2") + B.ToString("X2");

        public static Rgba Lerp(Rgba a, Rgba b, float t)
        {
            t = t < 0 ? 0 : t > 1 ? 1 : t;
            return new Rgba(
                (byte)Math.Round(a.R + (b.R - a.R) * t),
                (byte)Math.Round(a.G + (b.G - a.G) * t),
                (byte)Math.Round(a.B + (b.B - a.B) * t),
                (byte)Math.Round(a.A + (b.A - a.A) * t));
        }

        public Rgba WithAlpha(byte a) => new Rgba(R, G, B, a);

        /// <summary>Multiplica el brillo (1 = igual). Se usa para la variación de tono ±3 % por baldosa.</summary>
        public Rgba Scaled(float k)
        {
            byte S(byte c) => (byte)Math.Max(0, Math.Min(255, (int)Math.Round(c * k)));
            return new Rgba(S(R), S(G), S(B), A);
        }

        /// <summary>Luminancia relativa WCAG 2.x.</summary>
        public double RelativeLuminance()
        {
            double L(byte c)
            {
                double s = c / 255.0;
                return s <= 0.04045 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
            }
            return 0.2126 * L(R) + 0.7152 * L(G) + 0.0722 * L(B);
        }

        public static double ContrastRatio(Rgba a, Rgba b)
        {
            double la = a.RelativeLuminance(), lb = b.RelativeLuminance();
            double hi = Math.Max(la, lb), lo = Math.Min(la, lb);
            return (hi + 0.05) / (lo + 0.05);
        }

        public bool Equals(Rgba o) => R == o.R && G == o.G && B == o.B && A == o.A;
        public override bool Equals(object obj) => obj is Rgba o && Equals(o);
        public override int GetHashCode() => (R << 24) | (G << 16) | (B << 8) | A;
        public override string ToString() => ToHex();
    }
}
