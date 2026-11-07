using Tesela.Core;

namespace Tesela.Gameplay
{
    public enum GameMode
    {
        Classic = 0,
        Restoration = 1,
        Daily = 2,
        Zen = 3,
        Event = 4
    }

    /// <summary>Reglas de un modo. Los eventos semanales las reciben desde JSON remoto sin recompilar.</summary>
    public sealed class ModeRules
    {
        public GameMode Mode = GameMode.Classic;
        public int BoardWidth = 8;
        public int BoardHeight = 8;
        public bool ClearRegions3x3;
        /// <summary>Giro libre: se puede rotar cualquier pieza de la bandeja sin gastar potenciador.</summary>
        public bool FreeRotation;
        /// <summary>Modo relajado: las tres piezas de cada bandeja siempre caben (opción transparente).</summary>
        public bool Relaxed;
        /// <summary>Zen: si nada cabe, un barrido limpia la fila más llena en lugar de terminar.</summary>
        public bool ZenSweep;
        public bool Competitive = true;
        /// <summary>Evento de bombas: cada N jugadas aparece una bomba con esta cuenta. 0 = sin bombas.</summary>
        public int BombEveryMoves;
        public int BombCountdown = 9;
        /// <summary>Evento de piezas doradas: probabilidad de que una pieza nueva sea dorada.</summary>
        public float GoldenPieceChance;
        public string EventId;

        public static ModeRules Classic() => new ModeRules { Mode = GameMode.Classic };
        public static ModeRules Daily() => new ModeRules { Mode = GameMode.Daily };
        public static ModeRules Zen() => new ModeRules { Mode = GameMode.Zen, ZenSweep = true, Competitive = false, Relaxed = false };
        public static ModeRules Restoration() => new ModeRules { Mode = GameMode.Restoration };

        public static ModeRules EventFromJson(JsonNode n)
        {
            var r = new ModeRules
            {
                Mode = GameMode.Event,
                EventId = n["id"].AsString("event"),
                BoardWidth = n["boardWidth"].AsInt(8),
                BoardHeight = n["boardHeight"].AsInt(8),
                ClearRegions3x3 = n["regions3x3"].AsBool(),
                FreeRotation = n["freeRotation"].AsBool(),
                BombEveryMoves = n["bombEveryMoves"].AsInt(0),
                BombCountdown = n["bombCountdown"].AsInt(9),
                GoldenPieceChance = n["goldenPieceChance"].AsFloat(0f),
                Competitive = n["competitive"].AsBool(true)
            };
            return r;
        }

        public ModeRules Copy() => (ModeRules)MemberwiseClone();
    }
}
