using System;
using System.IO;
using System.Text;
using Tesela.Meta;

namespace Tesela.Tools
{
    public static class EconomyTool
    {
        public static int Run(string root)
        {
            var config = EconomyConfig.FromJson(File.ReadAllText(Paths.Config("Economy.json")));
            var sb = new StringBuilder();
            sb.AppendLine("# Simulador de economía (30 días)").AppendLine();
            sb.AppendLine("Rango sano del saldo al día 30 (de `Economy.json`, decidido en D-15): **" + config.HealthySavingsMin + "–" + config.HealthySavingsMax + " monedas**.");
            sb.AppendLine("Se usan las reglas reales de `MetaService`. Generado con `dotnet run --project tools/dotnet/Tesela.Tools -- economy`.").AppendLine();
            bool allHealthy = true;
            foreach (var a in EconomySimulator.DefaultArchetypes())
            {
                var report = EconomySimulator.Run(config, a);
                if (a.Name == "Típico") allHealthy &= report.Healthy(config);
                sb.AppendLine(report.ToMarkdown(config));
                Console.WriteLine(a.Name + ": " + report.FinalBalance + " (nivel " + report.Days[^1].Level + ", pase " + report.Days[^1].PassTier + ")");
            }
            sb.AppendLine(allHealthy ? "**Resultado: el jugador típico queda dentro del rango sano (sin inflación descontrolada).**" : "**Resultado: FUERA DE RANGO — ajustar Economy.json.**");
            File.WriteAllText(Path.Combine(root, "docs", "ECONOMY_REPORT.md"), sb.ToString());
            return allHealthy ? 0 : 1;
        }
    }
}
