using System.Collections.Generic;
using Tesela.Core;
using Tesela.Core.Raster;
using Tesela.Services;
using UnityEngine;

namespace Tesela.UI
{
    /// <summary>
    /// Todos los sprites del juego. Por defecto se generan por código y se empaquetan en un único atlas
    /// (un draw call por capa); con Theme.useTextures se usan las texturas finales importadas de ArtDrop.
    /// </summary>
    public sealed class ArtLibrary
    {
        public const int TileRotations = 4;
        private const int TileSize = 128;

        private readonly Sprite[,] _tiles = new Sprite[Palette.GlazeCount + 1, TileRotations];
        private readonly Dictionary<TileOverlay, Sprite> _overlays = new Dictionary<TileOverlay, Sprite>();
        private readonly Dictionary<FrameStyle, Sprite> _frames = new Dictionary<FrameStyle, Sprite>();
        private readonly Dictionary<string, Sprite> _icons = new Dictionary<string, Sprite>();
        public Sprite Socket { get; private set; }
        public Sprite White { get; private set; }
        public Texture2D Grain { get; private set; }
        public Texture2D Atlas { get; private set; }

        public Sprite Tile(int glaze, int rotation = 0) => _tiles[Mathf.Clamp(glaze, 0, Palette.GlazeCount), rotation & 3];
        public Sprite Overlay(TileOverlay o) => _overlays.TryGetValue(o, out var s) ? s : null;
        public Sprite Frame(FrameStyle f) => _frames[f];
        public Sprite Icon(string id) => _icons.TryGetValue(id, out var s) ? s : _icons["info"];

        /// <summary>Arte procedural ya rasterizado (se calcula en un hilo aparte para no frenar el arranque).</summary>
        public sealed class Rendered
        {
            public readonly List<string> Keys = new List<string>();
            public readonly List<Tesela.Core.Raster.RasterCanvas> Canvases = new List<Tesela.Core.Raster.RasterCanvas>();
            public Tesela.Core.Raster.RasterCanvas Grain;
        }

        /// <summary>Código puro: se puede llamar desde Task.Run.</summary>
        public static Rendered RenderProcedural()
        {
            var r = new Rendered();
            void Add(string key, Tesela.Core.Raster.RasterCanvas c) { r.Keys.Add(key); r.Canvases.Add(c); }

            for (int g = 0; g <= Palette.GlazeCount; g++)
                for (int rot = 0; rot < TileRotations; rot++)
                    Add("tile" + g + "_" + rot, TileRasterizer.Render(TileSize, Palette.Glaze(g), Palette.Pattern(g), g * 97 + rot * 13, rot));
            foreach (TileOverlay o in new[] { TileOverlay.Cracked, TileOverlay.CrackedHeavy, TileOverlay.Mud, TileOverlay.Gold, TileOverlay.Bomb })
                Add("ov" + (int)o, TileRasterizer.RenderOverlay(TileSize, o, (int)o * 7));
            Add("socket", TileRasterizer.RenderSocket(TileSize, 5));
            foreach (FrameStyle f in System.Enum.GetValues(typeof(FrameStyle))) Add("frame" + (int)f, UiRasterizer.RenderFrame(f, 11 + (int)f));
            foreach (string id in IconRasterizer.Ids) Add("icon_" + id, IconRasterizer.Render(id));
            var white = new Tesela.Core.Raster.RasterCanvas(8, 8);
            for (int i = 0; i < white.Pixels.Length; i++) white.Pixels[i] = new Rgba(255, 255, 255);
            Add("white", white);
            r.Grain = UiRasterizer.RenderGrain(256, 3);
            return r;
        }

        /// <summary>Hilo principal: crea texturas, empaqueta el atlas y los sprites.</summary>
        public static ArtLibrary Build(Theme theme, Rendered rendered)
        {
            var lib = new ArtLibrary();
            var canvases = rendered.Canvases;
            var keys = rendered.Keys;

            var textures = new Texture2D[canvases.Count];
            for (int i = 0; i < canvases.Count; i++) textures[i] = canvases[i].ToTexture(keys[i]);
            var atlas = new Texture2D(2, 2, TextureFormat.RGBA32, false) { name = "TeselaAtlas", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            Rect[] rects = atlas.PackTextures(textures, 2, 4096, true);
            foreach (var t in textures) Object.Destroy(t);
            lib.Atlas = atlas;

            for (int i = 0; i < keys.Count; i++)
            {
                Rect uv = rects[i];
                var px = new Rect(uv.x * atlas.width, uv.y * atlas.height, uv.width * atlas.width, uv.height * atlas.height);
                string k = keys[i];
                Vector4 border = Vector4.zero;
                if (k.StartsWith("frame"))
                {
                    float b = UiRasterizer.FrameBorder((FrameStyle)int.Parse(k.Substring(5)));
                    border = new Vector4(b, b, b, b);
                }
                var sprite = Sprite.Create(atlas, px, new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
                sprite.name = k;
                if (k.StartsWith("tile"))
                {
                    int g = k[4] - '0';
                    int r = k[6] - '0';
                    lib._tiles[g, r] = sprite;
                }
                else if (k.StartsWith("ov")) lib._overlays[(TileOverlay)int.Parse(k.Substring(2))] = sprite;
                else if (k == "socket") lib.Socket = sprite;
                else if (k.StartsWith("frame")) lib._frames[(FrameStyle)int.Parse(k.Substring(5))] = sprite;
                else if (k.StartsWith("icon_")) lib._icons[k.Substring(5)] = sprite;
                else if (k == "white") lib.White = sprite;
            }

            lib.Grain = rendered.Grain.ToTexture("Grain");
            lib.Grain.wrapMode = TextureWrapMode.Repeat;
            if (theme != null && theme.useTextures) lib.ApplyFinalArt(theme);
            return lib;
        }

        /// <summary>Fase 10: reemplaza lo procedural por las texturas finales que existan; lo que falte sigue procedural.</summary>
        private void ApplyFinalArt(Theme theme)
        {
            for (int g = 1; g <= Palette.GlazeCount && g - 1 < theme.tileTextures.Length; g++)
            {
                var tex = theme.tileTextures[g - 1];
                if (tex == null) continue;
                var s = FromTexture(tex, Vector4.zero);
                for (int r = 0; r < TileRotations; r++) _tiles[g, r] = s;
            }
            if (theme.socketTexture != null) Socket = FromTexture(theme.socketTexture, Vector4.zero);
            Vector4 nine = new Vector4(64, 64, 64, 64);
            if (theme.panelFrame != null) _frames[FrameStyle.Panel] = FromTexture(theme.panelFrame, nine);
            if (theme.buttonPrimaryFrame != null) _frames[FrameStyle.ButtonPrimary] = FromTexture(theme.buttonPrimaryFrame, nine);
            if (theme.buttonSecondaryFrame != null) _frames[FrameStyle.ButtonSecondary] = FromTexture(theme.buttonSecondaryFrame, nine);
            foreach (var icon in theme.icons)
                if (icon.texture != null && !string.IsNullOrEmpty(icon.id)) _icons[icon.id] = FromTexture(icon.texture, Vector4.zero);
        }

        private static Sprite FromTexture(Texture2D t, Vector4 border) =>
            Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
    }
}
