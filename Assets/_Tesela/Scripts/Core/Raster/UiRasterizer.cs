using System;
using System.Collections.Generic;

namespace Tesela.Core.Raster
{
    public enum FrameStyle
    {
        Panel = 0,
        ButtonPrimary = 1,
        ButtonSecondary = 2,
        ButtonGold = 3,
        Chip = 4,
        Board = 5
    }

    /// <summary>Marcos 9-slice de cerámica (borde algo irregular, sombra dura y corta abajo a la derecha) y grano de terracota.</summary>
    public static class UiRasterizer
    {
        public const int FrameSize = 128;

        /// <summary>Borde del 9-slice en píxeles para cada estilo (radios distintos a propósito: nada de "mismo radio en todo").</summary>
        public static int FrameBorder(FrameStyle style) => style == FrameStyle.Chip ? 40 : style == FrameStyle.Board ? 36 : 32;

        public static RasterCanvas RenderFrame(FrameStyle style, int seed)
        {
            int size = FrameSize;
            var c = new RasterCanvas(size, size);
            float s = size;
            Rgba fill, rim;
            float radius;
            switch (style)
            {
                case FrameStyle.ButtonPrimary: fill = Palette.Cobalt; rim = Rgba.Lerp(Palette.Cobalt, Palette.Indigo, 0.55f); radius = 22; break;
                case FrameStyle.ButtonSecondary: fill = Palette.SandLight; rim = Palette.Grout; radius = 18; break;
                case FrameStyle.ButtonGold: fill = Palette.Gold; rim = Palette.GoldDeep; radius = 26; break;
                case FrameStyle.Chip: fill = Palette.Sand; rim = Palette.Grout; radius = 30; break;
                case FrameStyle.Board: fill = Palette.Grout; rim = Rgba.Lerp(Palette.Grout, Palette.Indigo, 0.4f); radius = 20; break;
                default: fill = Palette.SandLight; rim = Rgba.Lerp(Palette.Grout, Palette.Sand, 0.2f); radius = 16; break;
            }
            float inset = 10f;
            float hw = s * 0.5f - inset, hh = s * 0.5f - inset;
            float shadowOff = 5f;

            // Sombra dura y corta (luz fija arriba a la izquierda): desplazada abajo-derecha, sin desenfoque.
            c.FillSdf((x, y) => Edge(x - shadowOff, y + shadowOff, s, hw, hh, radius, seed), Palette.Indigo.WithAlpha(60));
            c.FillSdf((x, y) => Edge(x, y, s, hw, hh, radius, seed), rim);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Edge(x + 0.5f, y + 0.5f, s, hw - 3.5f, hh - 3.5f, radius - 3f, seed);
                    if (d > 0.75f) continue;
                    float grain = 0.97f + 0.05f * Sdf.ValueNoise(x * 0.35f, y * 0.35f, seed + 9, 0);
                    float topLight = Sdf.Smoothstep(-8f, 0f, d) * (y > s * 0.5f ? 0.08f : -0.06f);
                    c.Blend(x, y, fill.Scaled(grain + topLight), RasterCanvas.Coverage(d));
                }
            return c;
        }

        private static float Edge(float x, float y, float s, float hw, float hh, float r, int seed)
        {
            // Irregularidad de cerámica: el contorno ondula ±1,2 px según ruido.
            float n = (Sdf.ValueNoise(x * 0.09f, y * 0.09f, seed) - 0.5f) * 2.4f;
            return Sdf.RoundedBox(x, y, s * 0.5f, s * 0.5f, hw, hh, r) + n;
        }

        /// <summary>Grano de terracota que se repite sin costuras, con alfa baja (3–5 %).</summary>
        public static RasterCanvas RenderGrain(int size, int seed, byte alpha = 11)
        {
            var c = new RasterCanvas(size, size);
            int period = 16;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float n = Sdf.Fbm(x / (float)size * period, y / (float)size * period, seed, 3, period);
                    float speck = Sdf.Hash(x, y, seed + 3, size) > 0.985f ? 0.6f : 0f;
                    var col = Rgba.Lerp(Palette.Terracotta, Palette.Indigo, speck);
                    c.Set(x, y, col.WithAlpha((byte)(alpha * (0.4f + n * 1.2f + speck))));
                }
            return c;
        }
    }

    /// <summary>
    /// Set único de iconos de trazo (mismo grosor en todos), blancos con alfa para teñirlos en la interfaz.
    /// Reemplazables en la Fase 10 por ids iguales.
    /// </summary>
    public static class IconRasterizer
    {
        public const int Size = 128;
        public const float Stroke = 10f;

        public static readonly string[] Ids =
        {
            "play", "back", "close", "settings", "shop", "coin", "tile", "star", "undo", "rotate", "hammer", "broom",
            "newtray", "reserve", "trophy", "calendar", "zen", "album", "pass", "sound", "music", "vibration", "check",
            "lock", "gift", "info", "crown", "chest", "home", "flame", "shield", "ad", "restore", "heart", "hand"
        };

        private delegate float Shape(float x, float y);

        public static RasterCanvas Render(string id)
        {
            var c = new RasterCanvas(Size, Size);
            var strokes = new List<Shape>();
            var fills = new List<Shape>();
            Build(id, strokes, fills);
            var white = new Rgba(255, 255, 255);
            foreach (var f in fills) c.FillSdf((x, y) => f(x, y), white);
            foreach (var s in strokes) c.StrokeSdf((x, y) => s(x, y), Stroke, white);
            return c;
        }

        private static float Seg(float x, float y, float ax, float ay, float bx, float by) => Sdf.Segment(x, y, ax, ay, bx, by);

        private const float Pi = (float)Math.PI;

        private static void Build(string id, List<Shape> st, List<Shape> fill)
        {
            switch (id)
            {
                case "play": fill.Add((x, y) => Sdf.Polygon(x, y, new[] { 44f, 30f, 44f, 98f, 98f, 64f }) - 2f); break;
                case "back": st.Add((x, y) => Seg(x, y, 76, 30, 42, 64)); st.Add((x, y) => Seg(x, y, 42, 64, 76, 98)); break;
                case "close": st.Add((x, y) => Seg(x, y, 38, 38, 90, 90)); st.Add((x, y) => Seg(x, y, 38, 90, 90, 38)); break;
                case "settings":
                    st.Add((x, y) => Math.Abs(Sdf.Circle(x, y, 64, 64, 18)));
                    for (int i = 0; i < 6; i++)
                    {
                        float a = i * Pi / 3f;
                        float ax = 64 + 30 * (float)Math.Cos(a), ay = 64 + 30 * (float)Math.Sin(a);
                        float bx = 64 + 44 * (float)Math.Cos(a), by = 64 + 44 * (float)Math.Sin(a);
                        st.Add((x, y) => Seg(x, y, ax, ay, bx, by));
                    }
                    break;
                case "shop":
                    st.Add((x, y) => Math.Abs(Sdf.RoundedBox(x, y, 64, 52, 34, 26, 8)));
                    st.Add((x, y) => Sdf.Arc(x, y, 64, 80, 16, 0, Pi));
                    break;
                case "coin":
                    st.Add((x, y) => Math.Abs(Sdf.Circle(x, y, 64, 64, 40)));
                    st.Add((x, y) => Math.Abs(Sdf.Circle(x, y, 64, 64, 20)));
                    break;
                case "tile":
                    st.Add((x, y) => Math.Abs(Sdf.RoundedBox(x, y, 64, 64, 36, 36, 10)));
                    st.Add((x, y) => Math.Abs(Math.Abs(x - 64) + Math.Abs(y - 64) - 18));
                    break;
                case "star": fill.Add((x, y) => StarSdf(x - 64, y - 62, 44, 20, 5)); break;
                case "undo":
                    st.Add((x, y) => Sdf.Arc(x, y, 66, 58, 30, -Pi * 0.75f, Pi * 0.9f));
                    st.Add((x, y) => Seg(x, y, 36, 88, 38, 64)); st.Add((x, y) => Seg(x, y, 36, 88, 60, 86));
                    break;
                case "rotate":
                    st.Add((x, y) => Sdf.Arc(x, y, 64, 64, 34, -Pi * 0.35f, Pi * 1.35f));
                    st.Add((x, y) => Seg(x, y, 92, 46, 92, 24)); st.Add((x, y) => Seg(x, y, 92, 46, 70, 46));
                    break;
                case "hammer":
                    st.Add((x, y) => Seg(x, y, 50, 30, 76, 84));
                    st.Add((x, y) => Math.Abs(Sdf.RoundedBox(x, y, 82, 88, 24, 12, 4)));
                    break;
                case "broom":
                    st.Add((x, y) => Seg(x, y, 84, 100, 64, 56));
                    st.Add((x, y) => Seg(x, y, 40, 28, 52, 56)); st.Add((x, y) => Seg(x, y, 58, 26, 64, 56)); st.Add((x, y) => Seg(x, y, 78, 30, 76, 56));
                    st.Add((x, y) => Seg(x, y, 48, 56, 80, 56));
                    break;
                case "newtray":
                    for (int i = 0; i < 3; i++) { float cx = 34 + i * 30; st.Add((x, y) => Math.Abs(Sdf.RoundedBox(x, y, cx, 56, 10, 10, 3))); }
                    st.Add((x, y) => Sdf.Arc(x, y, 64, 74, 26, Pi * 0.15f, Pi * 0.85f));
                    break;
                case "reserve":
                    st.Add((x, y) => Math.Abs(Sdf.RoundedBox(x, y, 64, 56, 36, 26, 6)));
                    st.Add((x, y) => Seg(x, y, 28, 82, 100, 82));
                    st.Add((x, y) => Seg(x, y, 52, 60, 76, 60));
                    break;
                case "trophy":
                    st.Add((x, y) => Sdf.Arc(x, y, 64, 82, 24, Pi, 0));
                    st.Add((x, y) => Seg(x, y, 40, 82, 88, 82));
                    st.Add((x, y) => Seg(x, y, 64, 58, 64, 40)); st.Add((x, y) => Seg(x, y, 46, 34, 82, 34));
                    break;
                case "calendar":
                    st.Add((x, y) => Math.Abs(Sdf.RoundedBox(x, y, 64, 58, 36, 32, 6)));
                    st.Add((x, y) => Seg(x, y, 28, 74, 100, 74));
                    st.Add((x, y) => Seg(x, y, 48, 84, 48, 98)); st.Add((x, y) => Seg(x, y, 80, 84, 80, 98));
                    break;
                case "zen":
                    st.Add((x, y) => Math.Abs(Sdf.Polygon(x, y, new[] { 30f, 30f, 46f, 82f, 98f, 98f, 82f, 46f })));
                    st.Add((x, y) => Seg(x, y, 30, 30, 84, 84));
                    break;
                case "album":
                    for (int i = 0; i < 4; i++) { float cx = i % 2 == 0 ? 46 : 82, cy = i < 2 ? 82 : 46; st.Add((x, y) => Math.Abs(Sdf.RoundedBox(x, y, cx, cy, 12, 12, 3))); }
                    break;
                case "pass":
                    st.Add((x, y) => Math.Abs(Sdf.Polygon(x, y, new[] { 40f, 100f, 88f, 100f, 88f, 26f, 64f, 42f, 40f, 26f })));
                    break;
                case "sound":
                    fill.Add((x, y) => Sdf.Polygon(x, y, new[] { 28f, 52f, 28f, 76f, 46f, 76f, 66f, 96f, 66f, 32f, 46f, 52f }));
                    st.Add((x, y) => Sdf.Arc(x, y, 70, 64, 22, -Pi / 3, Pi / 3)); st.Add((x, y) => Sdf.Arc(x, y, 70, 64, 38, -Pi / 3, Pi / 3));
                    break;
                case "music":
                    st.Add((x, y) => Seg(x, y, 52, 40, 52, 96)); st.Add((x, y) => Seg(x, y, 52, 96, 92, 104)); st.Add((x, y) => Seg(x, y, 92, 104, 92, 48));
                    fill.Add((x, y) => Sdf.Circle(x, y, 42, 40, 12)); fill.Add((x, y) => Sdf.Circle(x, y, 82, 48, 12));
                    break;
                case "vibration":
                    st.Add((x, y) => Math.Abs(Sdf.RoundedBox(x, y, 64, 64, 18, 32, 6)));
                    st.Add((x, y) => Seg(x, y, 30, 48, 30, 80)); st.Add((x, y) => Seg(x, y, 98, 48, 98, 80));
                    break;
                case "check": st.Add((x, y) => Seg(x, y, 32, 66, 54, 44)); st.Add((x, y) => Seg(x, y, 54, 44, 98, 88)); break;
                case "lock":
                    st.Add((x, y) => Math.Abs(Sdf.RoundedBox(x, y, 64, 50, 30, 22, 6)));
                    st.Add((x, y) => Sdf.Arc(x, y, 64, 74, 18, 0, Pi));
                    break;
                case "gift":
                    st.Add((x, y) => Math.Abs(Sdf.RoundedBox(x, y, 64, 52, 34, 26, 4)));
                    st.Add((x, y) => Seg(x, y, 64, 26, 64, 78)); st.Add((x, y) => Seg(x, y, 26, 78, 102, 78));
                    st.Add((x, y) => Sdf.Arc(x, y, 52, 88, 10, -Pi * 0.6f, Pi * 0.9f)); st.Add((x, y) => Sdf.Arc(x, y, 76, 88, 10, Pi * 0.1f, Pi * 1.6f));
                    break;
                case "info":
                    st.Add((x, y) => Math.Abs(Sdf.Circle(x, y, 64, 64, 40)));
                    st.Add((x, y) => Seg(x, y, 64, 44, 64, 66)); fill.Add((x, y) => Sdf.Circle(x, y, 64, 82, 6));
                    break;
                case "crown":
                    st.Add((x, y) => Math.Abs(Sdf.Polygon(x, y, new[] { 30f, 40f, 98f, 40f, 104f, 90f, 82f, 66f, 64f, 94f, 46f, 66f, 24f, 90f })));
                    break;
                case "chest":
                    st.Add((x, y) => Math.Abs(Sdf.RoundedBox(x, y, 64, 52, 38, 24, 4)));
                    st.Add((x, y) => Sdf.Arc(x, y, 64, 74, 38, 0, Pi));
                    fill.Add((x, y) => Sdf.RoundedBox(x, y, 64, 70, 7, 9, 2));
                    break;
                case "home":
                    st.Add((x, y) => Seg(x, y, 24, 64, 64, 100)); st.Add((x, y) => Seg(x, y, 64, 100, 104, 64));
                    st.Add((x, y) => Math.Abs(Sdf.RoundedBox(x, y, 64, 48, 28, 24, 4)));
                    break;
                case "flame":
                    st.Add((x, y) => Math.Abs(Sdf.Polygon(x, y, new[] { 64f, 106f, 88f, 70f, 90f, 46f, 64f, 24f, 38f, 46f, 40f, 70f, 54f, 64f })));
                    break;
                case "shield":
                    st.Add((x, y) => Math.Abs(Sdf.Polygon(x, y, new[] { 64f, 104f, 98f, 92f, 94f, 52f, 64f, 24f, 34f, 52f, 30f, 92f })));
                    break;
                case "ad":
                    st.Add((x, y) => Math.Abs(Sdf.RoundedBox(x, y, 64, 64, 40, 28, 8)));
                    fill.Add((x, y) => Sdf.Polygon(x, y, new[] { 56f, 52f, 56f, 76f, 76f, 64f }));
                    break;
                case "restore":
                    st.Add((x, y) => Sdf.Arc(x, y, 64, 64, 34, Pi * 0.2f, Pi * 1.9f));
                    st.Add((x, y) => Seg(x, y, 94, 76, 98, 54)); st.Add((x, y) => Seg(x, y, 94, 76, 74, 70));
                    break;
                case "heart":
                    fill.Add((x, y) => Math.Min(Math.Min(Sdf.Circle(x, y, 50, 74, 18), Sdf.Circle(x, y, 78, 74, 18)),
                                                Sdf.Polygon(x, y, new[] { 34f, 70f, 94f, 70f, 64f, 30f })));
                    break;
                case "hand":
                    st.Add((x, y) => Seg(x, y, 64, 36, 64, 96));
                    st.Add((x, y) => Seg(x, y, 50, 40, 50, 80)); st.Add((x, y) => Seg(x, y, 78, 40, 78, 84)); st.Add((x, y) => Seg(x, y, 36, 46, 36, 70));
                    st.Add((x, y) => Sdf.Arc(x, y, 58, 40, 22, Pi, Pi * 2f));
                    break;
                default: throw new ArgumentException("Icono desconocido: " + id);
            }
        }

        public static float StarSdf(float x, float y, float rOuter, float rInner, int points)
        {
            var pts = new float[points * 4];
            for (int i = 0; i < points * 2; i++)
            {
                float a = Pi / 2f + i * Pi / points;
                float r = i % 2 == 0 ? rOuter : rInner;
                pts[i * 2] = r * (float)Math.Cos(a);
                pts[i * 2 + 1] = r * (float)Math.Sin(a);
            }
            return Sdf.Polygon(x, y, pts);
        }
    }
}
