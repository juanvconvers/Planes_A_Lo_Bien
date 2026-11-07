using System;
using System.Collections.Generic;
using Tesela.Core;

namespace Tesela.Gameplay
{
    public enum ObjectiveType
    {
        CollectGlaze = 0,
        BreakCracked = 1,
        FreeMud = 2,
        ReachScore = 3,
        ClearLines = 4
    }

    public struct ObjectiveDef
    {
        public ObjectiveType Type;
        public byte Glaze;
        public int Target;

        public static ObjectiveType ParseType(string s)
        {
            switch (s)
            {
                case "collect": return ObjectiveType.CollectGlaze;
                case "cracked": return ObjectiveType.BreakCracked;
                case "mud": return ObjectiveType.FreeMud;
                case "score": return ObjectiveType.ReachScore;
                case "lines": return ObjectiveType.ClearLines;
                default: throw new FormatException("Objetivo desconocido: " + s);
            }
        }

        public static string TypeKey(ObjectiveType t)
        {
            switch (t)
            {
                case ObjectiveType.CollectGlaze: return "collect";
                case ObjectiveType.BreakCracked: return "cracked";
                case ObjectiveType.FreeMud: return "mud";
                case ObjectiveType.ReachScore: return "score";
                default: return "lines";
            }
        }
    }

    /// <summary>Nivel de Restauración (JSON en Data/Levels). Contenido actualizable sin recompilar.</summary>
    public sealed class LevelDefinition
    {
        public string Id;
        public int Chapter;
        public int Index;
        public ulong Seed;
        public int Width = 8;
        public int Height = 8;
        public string[] Rows;
        public int MoveLimit;
        public readonly List<ObjectiveDef> Objectives = new List<ObjectiveDef>();
        /// <summary>Jugadas sobrantes necesarias para 2 y 3 estrellas.</summary>
        public int TwoStarMovesLeft;
        public int ThreeStarMovesLeft;
        /// <summary>Resolubilidad medida por el validador (0..1). Informativo; el validador la recalcula.</summary>
        public float Solvability;

        public static LevelDefinition FromJson(JsonNode n)
        {
            var l = new LevelDefinition
            {
                Id = n["id"].AsString(),
                Chapter = n["chapter"].AsInt(),
                Index = n["index"].AsInt(),
                Seed = n["seed"].AsULong(),
                Width = n["width"].AsInt(8),
                Height = n["height"].AsInt(8),
                Rows = n["rows"].AsStringArray(),
                MoveLimit = n["moves"].AsInt(30),
                TwoStarMovesLeft = n["stars"][0].AsInt(3),
                ThreeStarMovesLeft = n["stars"][1].AsInt(6),
                Solvability = n["solvability"].AsFloat(0f)
            };
            foreach (var o in n["objectives"].Items)
            {
                l.Objectives.Add(new ObjectiveDef
                {
                    Type = ObjectiveDef.ParseType(o["type"].AsString()),
                    Glaze = (byte)o["glaze"].AsInt(0),
                    Target = o["target"].AsInt(1)
                });
            }
            if (string.IsNullOrEmpty(l.Id)) throw new FormatException("Nivel sin id");
            if (l.Rows.Length == 0)
            {
                l.Rows = new string[l.Height];
                for (int i = 0; i < l.Height; i++) l.Rows[i] = new string('.', l.Width);
            }
            if (l.Objectives.Count == 0) throw new FormatException("Nivel sin objetivos: " + l.Id);
            if (l.MoveLimit <= 0) throw new FormatException("Nivel sin límite de jugadas: " + l.Id);
            return l;
        }

        public Dictionary<string, object> ToJson()
        {
            var objectives = new List<object>();
            foreach (var o in Objectives)
            {
                var d = new Dictionary<string, object> { { "type", ObjectiveDef.TypeKey(o.Type) }, { "target", o.Target } };
                if (o.Type == ObjectiveType.CollectGlaze) d["glaze"] = (int)o.Glaze;
                objectives.Add(d);
            }
            return new Dictionary<string, object>
            {
                { "id", Id }, { "chapter", Chapter }, { "index", Index }, { "seed", Seed.ToString() },
                { "width", Width }, { "height", Height }, { "moves", MoveLimit },
                { "stars", new List<object> { TwoStarMovesLeft, ThreeStarMovesLeft } },
                { "objectives", objectives }, { "rows", new List<object>(Rows) },
                { "solvability", Math.Round(Solvability, 3) }
            };
        }

        public static List<LevelDefinition> ListFromJson(string json)
        {
            var root = Json.ParseNode(json);
            var list = new List<LevelDefinition>();
            foreach (var n in root["levels"].Items) list.Add(FromJson(n));
            return list;
        }
    }

    /// <summary>Progreso de objetivos de un nivel.</summary>
    public sealed class ObjectiveTracker
    {
        private readonly List<ObjectiveDef> _defs;
        public readonly int[] Progress;

        public ObjectiveTracker(List<ObjectiveDef> defs)
        {
            _defs = defs;
            Progress = new int[defs.Count];
        }

        public IReadOnlyList<ObjectiveDef> Definitions => _defs;

        public void OnMove(LineClearInfo info, List<ClearedCell> cleared, int score)
        {
            for (int i = 0; i < _defs.Count; i++)
            {
                var d = _defs[i];
                switch (d.Type)
                {
                    case ObjectiveType.CollectGlaze:
                        for (int c = 0; c < cleared.Count; c++)
                            if (!cleared[c].Survived && cleared[c].Glaze == d.Glaze) Progress[i]++;
                        break;
                    case ObjectiveType.BreakCracked: Progress[i] += info.CrackedBroken; break;
                    case ObjectiveType.FreeMud: Progress[i] += info.MudFreed; break;
                    case ObjectiveType.ClearLines: Progress[i] += info.Lines; break;
                    case ObjectiveType.ReachScore: Progress[i] = score; break;
                }
                if (Progress[i] > d.Target) Progress[i] = d.Target;
            }
        }

        public bool IsComplete
        {
            get
            {
                for (int i = 0; i < _defs.Count; i++) if (Progress[i] < _defs[i].Target) return false;
                return true;
            }
        }

        public float Completion
        {
            get
            {
                float sum = 0f;
                for (int i = 0; i < _defs.Count; i++) sum += Math.Min(1f, Progress[i] / (float)Math.Max(1, _defs[i].Target));
                return _defs.Count == 0 ? 1f : sum / _defs.Count;
            }
        }

        public void CopyFrom(int[] progress)
        {
            if (progress == null) return;
            Array.Copy(progress, Progress, Math.Min(progress.Length, Progress.Length));
        }
    }
}
