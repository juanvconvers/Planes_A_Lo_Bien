using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Tesela.Core;
using Tesela.Gameplay;

namespace Tesela.Tools
{
    /// <summary>Genera los 60 niveles de lanzamiento (3 capítulos × 20), los valida y escribe docs/LEVELS_REPORT.md.</summary>
    public static class LevelTool
    {
        public static int Run(string root)
        {
            var catalog = PieceCatalog.FromJson(File.ReadAllText(Paths.Config("Pieces.json")));
            var scoring = ScoringConfig.FromJson(File.ReadAllText(Paths.Config("ScoringConfig.json")));
            string dir = Path.Combine(root, "Assets", "_Tesela", "Data", "Levels", "Resources", "Levels");
            Directory.CreateDirectory(dir);
            var report = new StringBuilder("# Niveles de Restauración (lanzamiento)\n\n");
            report.AppendLine("Validador: 1 partida codiciosa + 59 Monte Carlo (codicioso con ruido) por nivel. Umbral: ≥ 70 % resolubles.\n");
            report.AppendLine("| Nivel | Objetivos | Jugadas | ★★ / ★★★ (sobrantes) | Resolubilidad |");
            report.AppendLine("|---|---|---|---|---|");
            bool allOk = true;
            for (int chapter = 1; chapter <= 3; chapter++)
            {
                var levels = new List<object>();
                for (int i = 1; i <= 20; i++)
                {
                    var l = LevelGenerator.Generate(chapter, i, scoring, catalog);
                    var check = LevelValidator.Validate(l, scoring, catalog, 60);
                    l.Solvability = check.Solvability;
                    allOk &= check.Solvability >= LevelValidator.MinSolvability;
                    levels.Add(l.ToJson());
                    var obj = new List<string>();
                    foreach (var o in l.Objectives) obj.Add(ObjectiveDef.TypeKey(o.Type) + (o.Type == ObjectiveType.CollectGlaze ? "(" + o.Glaze + ")" : "") + " " + o.Target);
                    report.AppendLine("| " + l.Id + " | " + string.Join(", ", obj) + " | " + l.MoveLimit + " | " + l.TwoStarMovesLeft + " / " + l.ThreeStarMovesLeft +
                                      " | " + (check.Solvability * 100).ToString("F0", CultureInfo.InvariantCulture) + " % |");
                    Console.Write(".");
                }
                var doc = new Dictionary<string, object>
                {
                    { "_comment", "Generado por tools/dotnet/Tesela.Tools (levels). Editable a mano o con Tesela > Editor de niveles; vuelve a validar." },
                    { "chapter", chapter },
                    { "levels", levels }
                };
                File.WriteAllText(Path.Combine(dir, "chapter" + chapter + ".json"), Json.Serialize(doc, true));
            }
            Console.WriteLine();
            report.AppendLine();
            report.AppendLine(allOk ? "**Resultado: los 60 niveles superan el validador.**" : "**Resultado: FALLA — hay niveles bajo el 70 %.**");
            File.WriteAllText(Path.Combine(root, "docs", "LEVELS_REPORT.md"), report.ToString());
            return allOk ? 0 : 1;
        }
    }
}
