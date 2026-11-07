using System;
using System.IO;
using Tesela.Core;
using Tesela.Gameplay;

namespace Tesela.Tests
{
    /// <summary>Localiza los JSON reales del proyecto tanto en Unity (cwd = raíz del proyecto) como en .NET (bin/).</summary>
    public static class TestData
    {
        private static string _root;

        public static string ProjectRoot
        {
            get
            {
                if (_root != null) return _root;
                foreach (string start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
                {
                    var dir = new DirectoryInfo(start);
                    while (dir != null)
                    {
                        if (Directory.Exists(System.IO.Path.Combine(dir.FullName, "Assets", "_Tesela"))) return _root = dir.FullName;
                        dir = dir.Parent;
                    }
                }
                throw new DirectoryNotFoundException("No se encontró Assets/_Tesela");
            }
        }

        public static string Path(string relative) => System.IO.Path.Combine(ProjectRoot, relative.Replace('/', System.IO.Path.DirectorySeparatorChar));

        public static string Read(string relative) => File.ReadAllText(Path(relative));

        public static string Config(string name) => Read("Assets/_Tesela/Config/Resources/" + name);

        private static PieceCatalog _catalog;
        private static ScoringConfig _scoring;

        public static PieceCatalog Catalog => _catalog ??= PieceCatalog.FromJson(Config("Pieces.json"));
        public static ScoringConfig Scoring => _scoring ??= ScoringConfig.FromJson(Config("ScoringConfig.json"));

        public static GameSession NewClassic(ulong seed = 42) => new GameSession(ModeRules.Classic(), Scoring, Catalog, seed);

        public static PieceShape Shape(params string[] rows) => PieceShape.FromRows("t", "t", rows);
    }
}
