using System;

namespace Tesela.Core.Raster
{
    public enum TileOverlay
    {
        None = 0,
        Cracked = 1,
        CrackedHeavy = 2,
        Mud = 3,
        Gold = 4,
        Bomb = 5
    }

    /// <summary>
    /// Azulejo de cerámica esmaltada pintado a mano, generado por código: rectángulo redondeado con bisel (luz fija
    /// arriba a la izquierda), brillo de esmalte, moteado, craquelado fino y el patrón propio de cada esmalte.
    /// La variación por baldosa (tono ±3 %, giro del patrón) se aplica al dibujar cada instancia.
    /// </summary>
    public static class TileRasterizer
    {
        private static readonly Rgba Paint = Rgba.FromHex("#F7EFE2");
        private static readonly float LightX = -0.7071f, LightY = 0.7071f;

        /// <param name="rotation">Giro del patrón en cuartos de vuelta (0–3); la luz del bisel no gira.</param>
        public static RasterCanvas Render(int size, Rgba glaze, GlazePattern pattern, int seed, int rotation = 0)
        {
            var c = new RasterCanvas(size, size);
            float s = size;
            float half = s * 0.5f;
            float radius = s * 0.16f;
            float hw = s * 0.47f;
            float bevel = s * 0.11f;

            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f, py = y + 0.5f;
                    float d = Sdf.RoundedBox(px, py, half, half, hw, hw, radius);
                    if (d > 0.75f) continue;

                    // Normal aproximada por el gradiente de la distancia: el bisel se ilumina según la luz.
                    float gx = Sdf.RoundedBox(px + 1, py, half, half, hw, hw, radius) - Sdf.RoundedBox(px - 1, py, half, half, hw, hw, radius);
                    float gy = Sdf.RoundedBox(px, py + 1, half, half, hw, hw, radius) - Sdf.RoundedBox(px, py - 1, half, half, hw, hw, radius);
                    float gl = Sdf.Length(gx, gy) + 1e-5f;
                    float edge = Sdf.Smoothstep(-bevel, 0f, d);
                    float lambert = (gx / gl * LightX + gy / gl * LightY);
                    float shade = 1f + edge * lambert * 0.32f;

                    float u = px / s, v = py / s;
                    float mottling = 0.94f + 0.08f * Sdf.Fbm(u * 6f, v * 6f, seed, 3);
                    float shine = Sdf.Smoothstep(0.42f, 0.0f, Sdf.Length((u - 0.3f) * 1.4f, (v - 0.74f) * 2.2f)) * 0.22f;
                    float crackle = Sdf.Smoothstep(0.035f, 0.0f, Sdf.VoronoiEdge(u * 5.5f, v * 5.5f, seed + 3)) * 0.09f;

                    float k = shade * mottling - crackle;
                    var col = glaze.Scaled(k);
                    col = Rgba.Lerp(col, new Rgba(255, 252, 244), shine * (1f - edge));
                    c.Blend(x, y, col, RasterCanvas.Coverage(d));
                }

            DrawPattern(c, pattern, seed, rotation);
            return c;
        }

        private static void DrawPattern(RasterCanvas c, GlazePattern pattern, int seed, int rotation)
        {
            if (pattern == GlazePattern.None) return;
            float s = c.Width;
            float cx = s * 0.5f, cy = s * 0.5f;
            float r = s * 0.24f;
            for (int y = 0; y < c.Height; y++)
                for (int x = 0; x < c.Width; x++)
                {
                    float px = x + 0.5f, py = y + 0.5f;
                    // Borde de pincel: el contorno se desplaza unos píxeles con ruido para que no parezca vectorial.
                    float wobble = (Sdf.ValueNoise(px / s * 9f, py / s * 9f, seed + 11) - 0.5f) * s * 0.025f;
                    float rx = px - cx, ry = py - cy;
                    for (int q = 0; q < (rotation & 3); q++) { float t = rx; rx = -ry; ry = t; }
                    float d = PatternSdf(pattern, rx, ry, r, s) + wobble;
                    if (d > 0.75f) continue;
                    float pigment = 0.82f + 0.18f * Sdf.ValueNoise(px / s * 14f, py / s * 14f, seed + 21);
                    c.Blend(x, y, Paint.WithAlpha((byte)(235 * pigment)), RasterCanvas.Coverage(d));
                }
        }

        /// <summary>Distancia firmada al patrón centrado en el origen.</summary>
        public static float PatternSdf(GlazePattern p, float x, float y, float r, float size)
        {
            float ang = (float)Math.Atan2(y, x);
            float len = Sdf.Length(x, y);
            switch (p)
            {
                case GlazePattern.Star:
                {
                    // Estrella de ocho puntas: radio que oscila entre punta y valle.
                    float t = (float)Math.Abs(Math.Cos(ang * 4f));
                    float rr = r * (0.55f + 0.45f * (float)Math.Pow(t, 3));
                    return (len - rr) * 0.8f;
                }
                case GlazePattern.Flower:
                {
                    float petals = r * (0.62f + 0.38f * (float)Math.Abs(Math.Cos(ang * 3f)));
                    float flower = (len - petals) * 0.85f;
                    float hole = r * 0.22f - len;
                    return Math.Max(flower, hole);
                }
                case GlazePattern.Rhombus:
                {
                    float d = (Math.Abs(x) * 1.0f + Math.Abs(y) * 0.72f) - r * 0.95f;
                    float inner = r * 0.36f - (Math.Abs(x) + Math.Abs(y) * 0.72f);
                    return Math.Max(d * 0.75f, inner * 0.75f);
                }
                case GlazePattern.Wave:
                {
                    float w = size * 0.06f;
                    float a = Math.Abs(y - (float)Math.Sin(x / r * 2.4f) * r * 0.32f + r * 0.32f) - w;
                    float b = Math.Abs(y - (float)Math.Sin(x / r * 2.4f) * r * 0.32f - r * 0.42f) - w;
                    float bounds = Math.Abs(x) - r * 1.25f;
                    return Math.Max(Math.Min(a, b), bounds);
                }
                case GlazePattern.Dot:
                {
                    float dot = len - r * 0.38f;
                    float ring = Math.Abs(len - r * 0.86f) - size * 0.035f;
                    return Math.Min(dot, ring);
                }
                case GlazePattern.Cross:
                {
                    float arm = size * 0.075f;
                    float h = Sdf.RoundedBox(x, y, 0, 0, r, arm, arm * 0.9f);
                    float v = Sdf.RoundedBox(x, y, 0, 0, arm, r, arm * 0.9f);
                    return Math.Min(h, v);
                }
                default: return 1e6f;
            }
        }

        /// <summary>Capa superpuesta para celdas especiales (se dibuja encima del azulejo).</summary>
        public static RasterCanvas RenderOverlay(int size, TileOverlay overlay, int seed)
        {
            var c = new RasterCanvas(size, size);
            float s = size;
            switch (overlay)
            {
                case TileOverlay.Cracked:
                case TileOverlay.CrackedHeavy:
                {
                    var ink = Palette.Indigo.WithAlpha(200);
                    var rng = new Pcg32((ulong)(seed + 1));
                    int cracks = overlay == TileOverlay.CrackedHeavy ? 5 : 3;
                    for (int i = 0; i < cracks; i++)
                    {
                        float x0 = s * 0.5f, y0 = s * 0.5f;
                        float a = (float)(rng.NextDouble() * Math.PI * 2);
                        for (int seg = 0; seg < 4; seg++)
                        {
                            float len = s * (0.08f + 0.06f * rng.NextFloat());
                            float x1 = x0 + (float)Math.Cos(a) * len, y1 = y0 + (float)Math.Sin(a) * len;
                            float ax = x0, ay = y0, bx = x1, by = y1;
                            c.StrokeSdf((px, py) => Sdf.Segment(px, py, ax, ay, bx, by), s * 0.022f, ink);
                            x0 = x1; y0 = y1;
                            a += (rng.NextFloat() - 0.5f) * 1.1f;
                        }
                    }
                    break;
                }
                case TileOverlay.Mud:
                {
                    var mud = Rgba.Lerp(Palette.Grout, Palette.Ochre, 0.25f).WithAlpha(225);
                    c.FillSdf((px, py) =>
                    {
                        float n = Sdf.Fbm(px / s * 4f, py / s * 4f, seed + 5, 3);
                        return Sdf.RoundedBox(px, py, s * 0.5f, s * 0.5f, s * 0.44f, s * 0.44f, s * 0.2f) + (n - 0.55f) * s * 0.35f;
                    }, mud);
                    break;
                }
                case TileOverlay.Gold:
                    c.StrokeSdf((px, py) => Sdf.RoundedBox(px, py, s * 0.5f, s * 0.5f, s * 0.4f, s * 0.4f, s * 0.12f), s * 0.05f, Palette.Gold.WithAlpha(240));
                    break;
                case TileOverlay.Bomb:
                    c.FillSdf((px, py) => Sdf.Circle(px, py, s * 0.5f, s * 0.47f, s * 0.27f), Palette.Indigo.WithAlpha(235));
                    c.StrokeSdf((px, py) => Sdf.Segment(px, py, s * 0.62f, s * 0.68f, s * 0.72f, s * 0.8f), s * 0.04f, Palette.Gold);
                    break;
            }
            return c;
        }

        /// <summary>Casilla vacía del patio: baldosa de arena hundida con junta (fondo del tablero).</summary>
        public static RasterCanvas RenderSocket(int size, int seed)
        {
            var c = new RasterCanvas(size, size);
            float s = size, half = s * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f, py = y + 0.5f;
                    float d = Sdf.RoundedBox(px, py, half, half, s * 0.47f, s * 0.47f, s * 0.12f);
                    if (d > 0.75f) continue;
                    float inner = Sdf.Smoothstep(-s * 0.08f, 0f, d);
                    float top = py / s;
                    float k = 0.95f + 0.05f * Sdf.Fbm(px / s * 5f, py / s * 5f, seed, 2) - inner * 0.07f * top;
                    c.Blend(x, y, Palette.SandDeep.Scaled(k), RasterCanvas.Coverage(d));
                }
            return c;
        }
    }
}
