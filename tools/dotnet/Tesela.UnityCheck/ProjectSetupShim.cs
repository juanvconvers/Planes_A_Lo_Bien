// Sustituto de ProjectSetup solo para el chequeo local (el real usa APIs de UnityEditor 2021.2+).
namespace Tesela.EditorTools
{
    public static class ProjectSetup
    {
        public const string BootScene = "Assets/_Tesela/Scenes/Boot.unity";
        public static void Apply() { }
        public static void EnsureBootScene() { }
    }
}
