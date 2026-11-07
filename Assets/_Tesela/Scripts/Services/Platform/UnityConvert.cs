using Tesela.Core;
using Tesela.Core.Raster;
using UnityEngine;

namespace Tesela.Services
{
    public static class UnityConvert
    {
        public static Color32 ToColor32(this Rgba c) => new Color32(c.R, c.G, c.B, c.A);

        public static Color ToColor(this Rgba c) => new Color32(c.R, c.G, c.B, c.A);

        public static Color ToColor(this Rgba c, float alpha)
        {
            Color k = c.ToColor();
            k.a = alpha;
            return k;
        }

        public static Texture2D ToTexture(this Tesela.Core.Raster.RasterCanvas canvas, string name, bool mipmaps = false)
        {
            var tex = new Texture2D(canvas.Width, canvas.Height, TextureFormat.RGBA32, mipmaps)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            var pixels = new Color32[canvas.Pixels.Length];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = canvas.Pixels[i].ToColor32();
            tex.SetPixels32(pixels);
            tex.Apply(mipmaps, false);
            return tex;
        }

        public static AudioClip ToClip(float[] samples, int sampleRate, string name)
        {
            var clip = AudioClip.Create(name, samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
