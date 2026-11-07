using System.Collections.Generic;
using System.IO;
using Tesela.Core;
using Tesela.Gameplay;
using UnityEditor;
using UnityEngine;

namespace Tesela.EditorTools
{
    /// <summary>Editor de niveles de Restauración: pinta el tablero inicial, edita objetivos y valida con el solver.</summary>
    public sealed class LevelEditorWindow : EditorWindow
    {
        private const string LevelsDir = "Assets/_Tesela/Data/Levels/Resources/Levels";
        private static readonly char[] Brushes = { '.', '1', '2', '3', '4', '5', '6', 'C', 'c', 'm', 'g' };

        private int _chapter = 1;
        private List<LevelDefinition> _levels = new List<LevelDefinition>();
        private int _selected;
        private char _brush = 'C';
        private string _report = "";
        private Vector2 _scroll;
        private PieceCatalog _catalog;
        private ScoringConfig _scoring;

        [MenuItem("Tesela/Editor de niveles")]
        public static void Open() => GetWindow<LevelEditorWindow>("Niveles");

        private void OnEnable()
        {
            _catalog = PieceCatalog.FromJson(File.ReadAllText("Assets/_Tesela/Config/Resources/Pieces.json"));
            _scoring = ScoringConfig.FromJson(File.ReadAllText("Assets/_Tesela/Config/Resources/ScoringConfig.json"));
            Load();
        }

        private string PathFor(int chapter) => LevelsDir + "/chapter" + chapter + ".json";

        private void Load()
        {
            _levels = File.Exists(PathFor(_chapter)) ? LevelDefinition.ListFromJson(File.ReadAllText(PathFor(_chapter))) : new List<LevelDefinition>();
            _selected = Mathf.Clamp(_selected, 0, Mathf.Max(0, _levels.Count - 1));
        }

        private void Save()
        {
            var list = new List<object>();
            foreach (var l in _levels) list.Add(l.ToJson());
            var doc = new Dictionary<string, object> { { "chapter", _chapter }, { "levels", list } };
            File.WriteAllText(PathFor(_chapter), Json.Serialize(doc, true));
            AssetDatabase.Refresh();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            int ch = EditorGUILayout.IntSlider("Capítulo", _chapter, 1, 9);
            if (ch != _chapter) { _chapter = ch; Load(); }
            if (GUILayout.Button("Recargar", GUILayout.Width(90))) Load();
            if (GUILayout.Button("Guardar", GUILayout.Width(90))) Save();
            EditorGUILayout.EndHorizontal();
            if (_levels.Count == 0)
            {
                if (GUILayout.Button("Generar nivel nuevo")) AddGenerated();
                return;
            }
            var names = new string[_levels.Count];
            for (int i = 0; i < names.Length; i++) names[i] = _levels[i].Id;
            _selected = EditorGUILayout.Popup("Nivel", _selected, names);
            var level = _levels[_selected];

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            level.MoveLimit = EditorGUILayout.IntField("Jugadas", level.MoveLimit);
            level.Seed = (ulong)EditorGUILayout.LongField("Semilla", (long)level.Seed);
            level.TwoStarMovesLeft = EditorGUILayout.IntField("★★ con jugadas sobrantes ≥", level.TwoStarMovesLeft);
            level.ThreeStarMovesLeft = EditorGUILayout.IntField("★★★ con jugadas sobrantes ≥", level.ThreeStarMovesLeft);

            EditorGUILayout.LabelField("Objetivos", EditorStyles.boldLabel);
            for (int i = 0; i < level.Objectives.Count; i++)
            {
                var o = level.Objectives[i];
                EditorGUILayout.BeginHorizontal();
                o.Type = (ObjectiveType)EditorGUILayout.EnumPopup(o.Type);
                if (o.Type == ObjectiveType.CollectGlaze) o.Glaze = (byte)EditorGUILayout.IntSlider(o.Glaze, 1, 6, GUILayout.Width(120));
                o.Target = EditorGUILayout.IntField(o.Target, GUILayout.Width(70));
                level.Objectives[i] = o;
                if (GUILayout.Button("×", GUILayout.Width(24))) { level.Objectives.RemoveAt(i); EditorGUILayout.EndHorizontal(); break; }
                EditorGUILayout.EndHorizontal();
            }
            if (GUILayout.Button("Añadir objetivo")) level.Objectives.Add(new ObjectiveDef { Type = ObjectiveType.ClearLines, Target = 5 });

            EditorGUILayout.LabelField("Tablero inicial (pincel: . vacío, 1-6 esmalte, C/c agrietada 2/1, m barro, g dorado)", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            foreach (char b in Brushes)
                if (GUILayout.Toggle(_brush == b, b.ToString(), "Button", GUILayout.Width(30))) _brush = b;
            EditorGUILayout.EndHorizontal();
            for (int y = 0; y < level.Height; y++)
            {
                EditorGUILayout.BeginHorizontal();
                var row = level.Rows[y].ToCharArray();
                for (int x = 0; x < level.Width; x++)
                    if (GUILayout.Button(row[x].ToString(), GUILayout.Width(30), GUILayout.Height(30))) row[x] = _brush;
                level.Rows[y] = new string(row);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Validar (60 partidas)")) ValidateSelected(level);
            if (GUILayout.Button("Validar capítulo")) ValidateChapter();
            if (GUILayout.Button("Generar nivel nuevo")) AddGenerated();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox(_report, MessageType.None);
            EditorGUILayout.EndScrollView();
        }

        private void ValidateSelected(LevelDefinition level)
        {
            var v = LevelValidator.Validate(level, _scoring, _catalog, 60);
            level.Solvability = v.Solvability;
            _report = level.Id + ": " + (v.Solvability * 100).ToString("F0") + " % resolubles (mínimo 70 %). Mediana de jugadas: " + v.MedianMovesUsed +
                      (v.Solvability < LevelValidator.MinSolvability ? "\nRECHAZADO: sube las jugadas o baja los objetivos." : "\nOK");
        }

        private void ValidateChapter()
        {
            var sb = new System.Text.StringBuilder();
            foreach (var l in _levels)
            {
                var v = LevelValidator.Validate(l, _scoring, _catalog, 60);
                l.Solvability = v.Solvability;
                sb.AppendLine(l.Id + ": " + (v.Solvability * 100).ToString("F0") + " %" + (v.Solvability < LevelValidator.MinSolvability ? "  ← RECHAZADO" : ""));
            }
            _report = sb.ToString();
        }

        private void AddGenerated()
        {
            var l = LevelGenerator.Generate(_chapter, _levels.Count + 1, _scoring, _catalog);
            _levels.Add(l);
            _selected = _levels.Count - 1;
            _report = "Generado " + l.Id + " (" + (l.Solvability * 100).ToString("F0") + " %)";
        }
    }
}
