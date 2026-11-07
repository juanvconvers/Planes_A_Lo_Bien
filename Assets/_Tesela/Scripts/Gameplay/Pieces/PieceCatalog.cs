using System;
using System.Collections.Generic;
using Tesela.Core;

namespace Tesela.Gameplay
{
    /// <summary>Catálogo de piezas y tabla pública de dificultad (Pieces.json). Igual para todos los jugadores.</summary>
    public sealed class PieceCatalog
    {
        private readonly List<PieceShape> _shapes = new List<PieceShape>();
        private readonly Dictionary<string, PieceShape> _byId = new Dictionary<string, PieceShape>();
        private readonly List<DifficultyStep> _difficulty = new List<DifficultyStep>();

        public IReadOnlyList<PieceShape> Shapes => _shapes;
        public IReadOnlyList<DifficultyStep> Difficulty => _difficulty;

        /// <summary>Piezas con al menos este número de celdas cuentan como "grandes" para la tabla de dificultad.</summary>
        public int LargePieceMinCells { get; private set; } = 5;

        public PieceShape Mono { get; private set; }

        public static PieceCatalog FromJson(string json)
        {
            var root = Json.ParseNode(json);
            var catalog = new PieceCatalog();
            catalog.LargePieceMinCells = root["largePieceMinCells"].AsInt(5);
            foreach (var p in root["pieces"].Items)
            {
                var shape = PieceShape.FromRows(p["id"].AsString(), p["family"].AsString(), p["cells"].AsStringArray(), p["weight"].AsFloat(1f));
                catalog.Add(shape);
            }
            foreach (var d in root["difficulty"].Items)
            {
                catalog._difficulty.Add(new DifficultyStep
                {
                    MinScore = d["minScore"].AsInt(),
                    LargeWeightMultiplier = d["largeWeightMultiplier"].AsFloat(1f),
                    SmallWeightMultiplier = d["smallWeightMultiplier"].AsFloat(1f)
                });
            }
            catalog._difficulty.Sort((a, b) => a.MinScore.CompareTo(b.MinScore));
            if (catalog._difficulty.Count == 0) catalog._difficulty.Add(new DifficultyStep { MinScore = 0, LargeWeightMultiplier = 1f, SmallWeightMultiplier = 1f });
            catalog.Validate();
            return catalog;
        }

        private void Add(PieceShape shape)
        {
            if (_byId.ContainsKey(shape.Id)) throw new FormatException("Pieza duplicada: " + shape.Id);
            if (shape.Id.IndexOf('@') >= 0) throw new FormatException("'@' está reservado para rotaciones: " + shape.Id);
            _shapes.Add(shape);
            _byId[shape.Id] = shape;
            if (shape.CellCount == 1 && Mono == null) Mono = shape;
        }

        private void Validate()
        {
            if (_shapes.Count == 0) throw new FormatException("Catálogo vacío");
            if (Mono == null) throw new FormatException("El catálogo necesita una pieza de 1 celda (garantiza que siempre haya algo que quepa)");
            foreach (var s in _shapes)
            {
                if (s.CellCount > 9) throw new FormatException("Máximo 9 celdas: " + s.Id);
                if (s.Weight <= 0) throw new FormatException("Peso debe ser > 0: " + s.Id);
                if (s.Width > 5 || s.Height > 5) throw new FormatException("Máximo 5×5: " + s.Id);
            }
        }

        /// <summary>Resuelve ids con sufijo de rotación ("L3a@2").</summary>
        public PieceShape Get(string id)
        {
            if (_byId.TryGetValue(id, out var s)) return s;
            string baseId = PieceShape.BaseId(id);
            if (!_byId.TryGetValue(baseId, out var shape)) throw new KeyNotFoundException("Pieza desconocida: " + id);
            int rot = PieceShape.RotationOf(id);
            for (int i = 0; i < rot; i++) shape = shape.RotatedClockwise();
            _byId[id] = shape;
            return shape;
        }

        public bool TryGet(string id, out PieceShape shape)
        {
            try { shape = Get(id); return true; }
            catch (KeyNotFoundException) { shape = null; return false; }
        }

        public DifficultyStep DifficultyFor(int score)
        {
            DifficultyStep current = _difficulty[0];
            foreach (var step in _difficulty)
                if (score >= step.MinScore) current = step;
            return current;
        }

        public float EffectiveWeight(PieceShape shape, int score)
        {
            var step = DifficultyFor(score);
            return shape.Weight * (shape.CellCount >= LargePieceMinCells ? step.LargeWeightMultiplier : step.SmallWeightMultiplier);
        }
    }

    public struct DifficultyStep
    {
        public int MinScore;
        public float LargeWeightMultiplier;
        public float SmallWeightMultiplier;
    }
}
