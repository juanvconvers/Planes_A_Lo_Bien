using System;
using Tesela.Core;
using Tesela.Services;
using UnityEngine;

namespace Tesela.UI
{
    /// <summary>
    /// Tokens visuales (Theme.asset). Mientras <see cref="useTextures"/> es falso se usa el arte procedural;
    /// el importador de ArtDrop (Fase 10) rellena las texturas y activa el interruptor sin tocar código de juego.
    /// </summary>
    [CreateAssetMenu(menuName = "Tesela/Theme", fileName = "Theme")]
    public sealed class Theme : ScriptableObject
    {
        [Header("Paleta")]
        public Color sand = Palette.Sand.ToColor();
        public Color grout = Palette.Grout.ToColor();
        public Color indigo = Palette.Indigo.ToColor();
        public Color gold = Palette.Gold.ToColor();
        public Color[] glazes = { Palette.Cobalt.ToColor(), Palette.Ochre.ToColor(), Palette.Olive.ToColor(), Palette.Coral.ToColor(), Palette.Turquoise.ToColor(), Palette.Plum.ToColor() };

        [Header("Tipografía (OFL)")]
        public Font displayFont;
        public Font uiFont;

        [Header("Arte final (Fase 10)")]
        public bool useTextures;
        public Texture2D[] tileTextures = new Texture2D[6];
        public Texture2D socketTexture;
        public Texture2D panelFrame;
        public Texture2D buttonPrimaryFrame;
        public Texture2D buttonSecondaryFrame;
        public Texture2D background;
        public Texture2D[] mosaics = new Texture2D[0];
        public IconOverride[] icons = new IconOverride[0];

        [Serializable]
        public struct IconOverride
        {
            public string id;
            public Texture2D texture;
        }

        public Color Glaze(int glaze) => glaze >= 1 && glaze <= glazes.Length ? glazes[glaze - 1] : grout;

        public Font Display => displayFont != null ? displayFont : Resources.Load<Font>("Fonts/Fraunces");
        public Font Interface => uiFont != null ? uiFont : Resources.Load<Font>("Fonts/BricolageGrotesque");

        private static Theme _instance;

        public static Theme Instance
        {
            get
            {
                if (_instance != null) return _instance;
                _instance = Resources.Load<Theme>("Theme");
                if (_instance == null) _instance = CreateInstance<Theme>();
                return _instance;
            }
        }
    }
}
