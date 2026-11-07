using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Tesela.Gameplay;
using Tesela.Gameplay.Bots;

namespace Tesela.Tools
{
    /// <summary>
    /// dotnet run --project tools/dotnet/Tesela.Tools -- sim [partidas]   → docs/SIM_REPORT.md
    /// dotnet run --project tools/dotnet/Tesela.Tools -- levels            → genera y valida Data/Levels
    /// dotnet run --project tools/dotnet/Tesela.Tools -- economy           → docs/ECONOMY_REPORT.md
    /// </summary>
    public static class Program
    {
        public static int Main(string[] args)
        {
            string cmd = args.Length > 0 ? args[0] : "sim";
            switch (cmd)
            {
                case "sim": return RunSimulation(args.Length > 1 ? int.Parse(args[1]) : 10000);
                case "levels": return LevelTool.Run(Paths.Root);
                case "economy": return EconomyTool.Run(Paths.Root);
                default:
                    Console.Error.WriteLine("Comando desconocido: " + cmd);
                    return 2;
            }
        }

        private static int RunSimulation(int games)
        {
            var catalog = PieceCatalog.FromJson(File.ReadAllText(Paths.Config("Pieces.json")));
            var scoring = ScoringConfig.FromJson(File.ReadAllText(Paths.Config("ScoringConfig.json")));
            var all = new List<SimulationStats>();
            var sw = Stopwatch.StartNew();
            all.Add(Simulation.Run(s => new RandomBot(s), "Aleatorio · Clásico", games, ModeRules.Classic(), scoring, catalog, 1));
            all.Add(Simulation.Run(s => new GreedyBot(s), "Codicioso · Clásico", games, ModeRules.Classic(), scoring, catalog, 2, 3000));
            var relaxed = ModeRules.Classic();
            relaxed.Relaxed = true;
            all.Add(Simulation.Run(s => new RandomBot(s), "Aleatorio · Relajado", games / 10, relaxed, scoring, catalog, 3));
            var ev = ModeRules.EventFromJson(Tesela.Core.Json.ParseNode("{\"id\":\"regiones\",\"boardWidth\":9,\"boardHeight\":9,\"regions3x3\":true}"));
            all.Add(Simulation.Run(s => new RandomBot(s), "Aleatorio · Evento 9×9", games / 10, ev, scoring, catalog, 4));
            sw.Stop();

            var sb = new StringBuilder();
            sb.Append(Simulation.ToMarkdown(all, "Informe de simulación (Fase 1)"));
            sb.AppendLine();
            sb.AppendLine("Generado con `dotnet run --project tools/dotnet/Tesela.Tools -- sim " + games + "` en " + sw.Elapsed.TotalSeconds.ToString("F1") + " s.");
            sb.AppendLine();
            sb.AppendLine("**Qué se verifica en cada partida**");
            sb.AppendLine("- *Fines falsos*: el juego terminó pero una comprobación independiente celda a celda (sin máscaras de bits) encuentra una jugada, o el bot no encontró jugada sin que el juego hubiera terminado.");
            sb.AppendLine("- *Bandejas sin pieza jugable*: alguna bandeja nueva llegó sin ninguna pieza que quepa (el generador lo prohíbe).");
            sb.AppendLine("- *>2 iguales seguidas*: tres piezas consecutivas de la misma familia (el generador lo prohíbe salvo que la única pieza que cabe lo exija).");
            sb.AppendLine("- *Topadas*: partidas cortadas al llegar a 3.000 jugadas (el bot codicioso puede jugar casi indefinidamente).");
            sb.AppendLine();
            bool ok = all.TrueForAll(s => s.Passed);
            sb.AppendLine(ok ? "**Resultado: PASA** (cero excepciones, cero fines falsos, cero bandejas injugables)." : "**Resultado: FALLA**");
            foreach (var s in all)
                foreach (var e in s.ExceptionSamples) sb.AppendLine("- Excepción (" + s.BotName + "): " + e);
            File.WriteAllText(Path.Combine(Paths.Root, "docs", "SIM_REPORT.md"), sb.ToString());
            Console.WriteLine(sb.ToString());
            return ok ? 0 : 1;
        }
    }

    public static class Paths
    {
        private static string _root;

        public static string Root
        {
            get
            {
                if (_root != null) return _root;
                var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
                while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "Assets", "_Tesela"))) dir = dir.Parent;
                if (dir == null) dir = new DirectoryInfo(AppContext.BaseDirectory);
                while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "Assets", "_Tesela"))) dir = dir.Parent;
                return _root = dir?.FullName ?? throw new DirectoryNotFoundException("Assets/_Tesela");
            }
        }

        public static string Config(string name) => Path.Combine(Root, "Assets", "_Tesela", "Config", "Resources", name);
    }
}
