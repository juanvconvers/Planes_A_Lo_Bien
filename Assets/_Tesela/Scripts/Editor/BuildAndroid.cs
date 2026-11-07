using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Tesela.EditorTools
{
    /// <summary>
    /// Builds de Android. La keystore NUNCA se versiona: se lee de variables de entorno
    /// (TESELA_KEYSTORE_PATH, TESELA_KEYSTORE_PASS, TESELA_KEY_ALIAS, TESELA_KEY_PASS). Ver docs/RELEASE_SIGNING.md.
    /// </summary>
    public static class BuildAndroid
    {
        public const string OutputDir = "Builds";

        [MenuItem("Tesela/Build/AAB de release")]
        public static void BuildReleaseAab() => Build(release: true);

        [MenuItem("Tesela/Build/APK de desarrollo")]
        public static void BuildDevelopmentApk() => Build(release: false);

        private static void Build(bool release)
        {
            ProjectSetup.Apply();
            EditorUserBuildSettings.buildAppBundle = release;
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ProjectSetup.BootScene },
                target = BuildTarget.Android,
                locationPathName = Path.Combine(OutputDir, release ? "Tesela.aab" : "Tesela-dev.apk"),
                options = release ? BuildOptions.None : BuildOptions.Development
            };
            if (release) ConfigureSigning();
            Directory.CreateDirectory(OutputDir);
            BuildReport report = BuildPipeline.BuildPlayer(options);
            bool ok = report.summary.result == BuildResult.Succeeded;
            Debug.Log("[Tesela] Build " + (ok ? "OK" : "FALLÓ") + ": " + options.locationPathName + " (" + (report.summary.totalSize / (1024f * 1024f)).ToString("F1") + " MB)");
            if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
        }

        private static void ConfigureSigning()
        {
            string path = Environment.GetEnvironmentVariable("TESELA_KEYSTORE_PATH");
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                Debug.LogWarning("[Tesela] Sin TESELA_KEYSTORE_PATH: el AAB se firma con la clave de depuración y Play lo rechazará. Ver docs/RELEASE_SIGNING.md");
                PlayerSettings.Android.useCustomKeystore = false;
                return;
            }
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = path;
            PlayerSettings.Android.keystorePass = Environment.GetEnvironmentVariable("TESELA_KEYSTORE_PASS");
            PlayerSettings.Android.keyaliasName = Environment.GetEnvironmentVariable("TESELA_KEY_ALIAS");
            PlayerSettings.Android.keyaliasPass = Environment.GetEnvironmentVariable("TESELA_KEY_PASS");
        }
    }
}
