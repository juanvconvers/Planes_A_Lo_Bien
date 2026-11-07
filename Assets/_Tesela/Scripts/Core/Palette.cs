namespace Tesela.Core
{
    /// <summary>Paleta propia de Tesela (Parte 2). Theme.asset la copia; esta clase es la fuente para la lógica y los tests.</summary>
    public static class Palette
    {
        public static readonly Rgba Sand = Rgba.FromHex("#F2E6D8");
        public static readonly Rgba Grout = Rgba.FromHex("#8C7B6B");
        public static readonly Rgba Indigo = Rgba.FromHex("#23305C");
        public static readonly Rgba Gold = Rgba.FromHex("#E8B64C");

        public static readonly Rgba Cobalt = Rgba.FromHex("#2F5DA8");
        public static readonly Rgba Ochre = Rgba.FromHex("#D99A2B");
        public static readonly Rgba Olive = Rgba.FromHex("#6E8B3D");
        public static readonly Rgba Coral = Rgba.FromHex("#E0654F");
        public static readonly Rgba Turquoise = Rgba.FromHex("#2A9D8F");
        public static readonly Rgba Plum = Rgba.FromHex("#7B3F61");

        /// <summary>Tonos derivados (no son colores nuevos: mezclas de la paleta) para superficies y estados.</summary>
        public static readonly Rgba SandDeep = Rgba.Lerp(Sand, Grout, 0.18f);
        public static readonly Rgba SandLight = Rgba.Lerp(Sand, new Rgba(255, 255, 255), 0.45f);
        public static readonly Rgba Terracotta = Rgba.Lerp(Coral, Grout, 0.45f);
        public static readonly Rgba IndigoSoft = Rgba.Lerp(Indigo, Grout, 0.35f);
        public static readonly Rgba GoldDeep = Rgba.Lerp(Gold, Indigo, 0.55f);
        public static readonly Rgba Shadow = Indigo.WithAlpha(70);

        public const int GlazeCount = 6;

        /// <summary>Índice 1..6 → esmalte. El 0 es "vacío".</summary>
        public static Rgba Glaze(int glaze)
        {
            switch (glaze)
            {
                case 1: return Cobalt;
                case 2: return Ochre;
                case 3: return Olive;
                case 4: return Coral;
                case 5: return Turquoise;
                case 6: return Plum;
                default: return Grout;
            }
        }

        /// <summary>Patrón de doble código por esmalte (para daltonismo).</summary>
        public static GlazePattern Pattern(int glaze)
        {
            switch (glaze)
            {
                case 1: return GlazePattern.Star;
                case 2: return GlazePattern.Flower;
                case 3: return GlazePattern.Rhombus;
                case 4: return GlazePattern.Wave;
                case 5: return GlazePattern.Dot;
                case 6: return GlazePattern.Cross;
                default: return GlazePattern.None;
            }
        }
    }

    public enum GlazePattern
    {
        None = 0,
        Star = 1,
        Flower = 2,
        Rhombus = 3,
        Wave = 4,
        Dot = 5,
        Cross = 6
    }
}
