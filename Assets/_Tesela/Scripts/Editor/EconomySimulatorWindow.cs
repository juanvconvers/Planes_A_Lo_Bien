using System.IO;
using Tesela.Meta;
using UnityEditor;
using UnityEngine;

namespace Tesela.EditorTools
{
    /// <summary>Simulador de economía: ingresos y gastos por día de varios arquetipos con las reglas reales.</summary>
    public sealed class EconomySimulatorWindow : EditorWindow
    {
        private int _days = 30;
        private string _output = "";
        private Vector2 _scroll;

        [MenuItem("Tesela/Simulador de economía")]
        public static void Open() => GetWindow<EconomySimulatorWindow>("Economía");

        private void OnGUI()
        {
            _days = EditorGUILayout.IntSlider("Días", _days, 7, 90);
            if (GUILayout.Button("Simular con Economy.json")) Run();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.TextArea(_output, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        private void Run()
        {
            var config = EconomyConfig.FromJson(File.ReadAllText("Assets/_Tesela/Config/Resources/Economy.json"));
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Rango sano al final: " + config.HealthySavingsMin + "–" + config.HealthySavingsMax);
            foreach (var a in EconomySimulator.DefaultArchetypes())
            {
                var r = EconomySimulator.Run(config, a, _days);
                sb.AppendLine(r.ToMarkdown(config));
            }
            _output = sb.ToString();
        }
    }
}
