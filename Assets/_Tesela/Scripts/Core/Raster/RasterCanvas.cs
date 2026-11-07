using System;

namespace Tesela.Core.Raster
{
    /// <summary>
    /// Lienzo RGBA en memoria. Convención de Unity: índice = y * ancho + x con y = 0 abajo,
    /// así SetPixels32 lo usa sin voltear. Coordenadas de dibujo en píxeles con origen abajo a la izquierda.
    /// </summary>
    public sealed class RasterCanvas
    {
        public readonly int Width;
        public readonly int Height;
        public readonly Rgba[] Pixels;

        public RasterCanvas(int width, int height)
        {
            if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            Width = width;
            Height = height;
            Pixels = new Rgba[width * height];
        }

        public Rgba Get(int x, int y) => Pixels[y * Width + x];

        public void Set(int x, int y, Rgba c) => Pixels[y * Width + x] = c;

        /// <summary>Composición "source over" con una cobertura adicional (0..1).</summary>
        public void Blend(int x, int y, Rgba c, float coverage)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height) return;
            float a = c.A / 255f * Clamp01(coverage);
            if (a <= 0f) return;
            int i = y * Width + x;
            Rgba d = Pixels[i];
            float da = d.A / 255f;
            float oa = a + da * (1f - a);
            if (oa <= 0f) return;
            byte Mix(byte s, byte dst) => (byte)Math.Round((s * a + dst * da * (1f - a)) / oa);
            Pixels[i] = new Rgba(Mix(c.R, d.R), Mix(c.G, d.G), Mix(c.B, d.B), (byte)Math.Round(oa * 255f));
        }

        /// <summary>Rellena según una función de distancia firmada (negativa dentro), con antialias de 1 px.</summary>
        public void FillSdf(Func<float, float, float> sdf, Rgba color)
        {
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    float d = sdf(x + 0.5f, y + 0.5f);
                    if (d < 0.75f) Blend(x, y, color, Coverage(d));
                }
        }

        /// <summary>Traza el contorno (|sdf| &lt; ancho/2).</summary>
        public void StrokeSdf(Func<float, float, float> sdf, float width, Rgba color)
        {
            float h = width * 0.5f;
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    float d = Math.Abs(sdf(x + 0.5f, y + 0.5f)) - h;
                    if (d < 0.75f) Blend(x, y, color, Coverage(d));
                }
        }

        public static float Coverage(float signedDistance) => Clamp01(0.5f - signedDistance);

        public static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
    }

    /// <summary>Funciones de distancia firmada y ruido (todas puras y deterministas).</summary>
    public static class Sdf
    {
        public static float Circle(float x, float y, float cx, float cy, float r) => Length(x - cx, y - cy) - r;

        public static float RoundedBox(float x, float y, float cx, float cy, float hw, float hh, float r)
        {
            float qx = Math.Abs(x - cx) - hw + r;
            float qy = Math.Abs(y - cy) - hh + r;
            return Length(Math.Max(qx, 0f), Math.Max(qy, 0f)) + Math.Min(Math.Max(qx, qy), 0f) - r;
        }

        public static float Segment(float x, float y, float ax, float ay, float bx, float by)
        {
            float pax = x - ax, pay = y - ay, bax = bx - ax, bay = by - ay;
            float h = RasterCanvas.Clamp01((pax * bax + pay * bay) / (bax * bax + bay * bay + 1e-6f));
            return Length(pax - bax * h, pay - bay * h);
        }

        /// <summary>Arco de circunferencia (distancia sin signo) entre los ángulos a0..a1 (radianes, antihorario).</summary>
        public static float Arc(float x, float y, float cx, float cy, float r, float a0, float a1)
        {
            float ang = (float)Math.Atan2(y - cy, x - cx);
            if (InArc(ang, a0, a1)) return Math.Abs(Length(x - cx, y - cy) - r);
            float d0 = Length(x - (cx + r * (float)Math.Cos(a0)), y - (cy + r * (float)Math.Sin(a0)));
            float d1 = Length(x - (cx + r * (float)Math.Cos(a1)), y - (cy + r * (float)Math.Sin(a1)));
            return Math.Min(d0, d1);
        }

        private static bool InArc(float ang, float a0, float a1)
        {
            const float Tau = (float)(Math.PI * 2);
            float span = ((a1 - a0) % Tau + Tau) % Tau;
            float rel = ((ang - a0) % Tau + Tau) % Tau;
            return rel <= span;
        }

        /// <summary>Polígono convexo o no (relleno por paridad), distancia firmada.</summary>
        public static float Polygon(float x, float y, float[] pts)
        {
            int n = pts.Length / 2;
            float d = float.MaxValue;
            bool inside = false;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                float xi = pts[i * 2], yi = pts[i * 2 + 1], xj = pts[j * 2], yj = pts[j * 2 + 1];
                d = Math.Min(d, Segment(x, y, xi, yi, xj, yj));
                if ((yi > y) != (yj > y) && x < (xj - xi) * (y - yi) / (yj - yi + 1e-9f) + xi) inside = !inside;
            }
            return inside ? -d : d;
        }

        public static float Length(float x, float y) => (float)Math.Sqrt(x * x + y * y);

        public static float Smoothstep(float e0, float e1, float x)
        {
            float t = RasterCanvas.Clamp01((x - e0) / (e1 - e0));
            return t * t * (3f - 2f * t);
        }

        /// <summary>Ruido de valor 2D en [0,1], periódico con periodo <paramref name="period"/> (para texturas que se repiten).</summary>
        public static float ValueNoise(float x, float y, int seed, int period = 0)
        {
            int x0 = (int)Math.Floor(x), y0 = (int)Math.Floor(y);
            float fx = x - x0, fy = y - y0;
            float sx = fx * fx * (3 - 2 * fx), sy = fy * fy * (3 - 2 * fy);
            float a = Hash(x0, y0, seed, period), b = Hash(x0 + 1, y0, seed, period);
            float c = Hash(x0, y0 + 1, seed, period), d = Hash(x0 + 1, y0 + 1, seed, period);
            return a + (b - a) * sx + (c - a) * sy + (a - b - c + d) * sx * sy;
        }

        public static float Fbm(float x, float y, int seed, int octaves = 3, int period = 0)
        {
            float sum = 0f, amp = 0.5f, norm = 0f;
            int p = period;
            for (int i = 0; i < octaves; i++)
            {
                sum += ValueNoise(x, y, seed + i * 101, p) * amp;
                norm += amp;
                x *= 2f; y *= 2f; amp *= 0.5f;
                if (p > 0) p *= 2;
            }
            return sum / norm;
        }

        /// <summary>Distancia al borde de celda de Voronoi (F2 - F1): líneas finas para el craquelado del esmalte.</summary>
        public static float VoronoiEdge(float x, float y, int seed)
        {
            int cx = (int)Math.Floor(x), cy = (int)Math.Floor(y);
            float f1 = float.MaxValue, f2 = float.MaxValue;
            for (int j = -1; j <= 1; j++)
                for (int i = -1; i <= 1; i++)
                {
                    float px = cx + i + Hash(cx + i, cy + j, seed, 0);
                    float py = cy + j + Hash(cx + i, cy + j, seed + 7, 0);
                    float d = Length(x - px, y - py);
                    if (d < f1) { f2 = f1; f1 = d; }
                    else if (d < f2) f2 = d;
                }
            return f2 - f1;
        }

        public static float Hash(int x, int y, int seed, int period)
        {
            if (period > 0)
            {
                x = ((x % period) + period) % period;
                y = ((y % period) + period) % period;
            }
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 2246822519);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / 16777215f;
            }
        }
    }
}
