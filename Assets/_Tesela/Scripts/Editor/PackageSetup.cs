using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace Tesela.EditorTools
{
    /// <summary>
    /// Instala los paquetes de Unity por nombre (Unity elige la versión compatible con el editor) y las
    /// TMP Essential Resources. Se ejecuta sola la primera vez que se abre el proyecto; el código que depende de
    /// cada paquete está protegido por versionDefines, así que el proyecto compila antes y después de instalar.
    /// Se instalan uno a uno: si alguno falla, los demás siguen.
    /// </summary>
    [InitializeOnLoad]
    public static class PackageSetup
    {
        private const string MarkerPath = "ProjectSettings/TeselaPackages.done";

        public static readonly string[] Packages =
        {
            "com.unity.render-pipelines.universal",
            "com.unity.inputsystem",
            "com.unity.purchasing",
            "com.unity.mobile.notifications",
            "com.unity.addressables",
            "com.unity.services.levelplay"
        };

        private static readonly Queue<string> Pending = new Queue<string>();
        private static AddRequest _current;
        private static readonly List<string> Failed = new List<string>();

        static PackageSetup()
        {
            if (Application.isBatchMode || File.Exists(MarkerPath)) return;
            EditorApplication.delayCall += Install;
        }

        [MenuItem("Tesela/Setup/Instalar paquetes")]
        public static void Install()
        {
            if (_current != null && !_current.IsCompleted) return;
            Pending.Clear();
            Failed.Clear();
            foreach (string p in Packages) Pending.Enqueue(p);
            Debug.Log("[Tesela] Instalando paquetes…");
            EditorApplication.update += Poll;
            Next();
        }

        /// <summary>Para modo batch: -executeMethod Tesela.EditorTools.PackageSetup.InstallBlocking</summary>
        public static void InstallBlocking()
        {
            foreach (string p in Packages)
            {
                var request = Client.Add(p);
                while (!request.IsCompleted) System.Threading.Thread.Sleep(200);
                if (request.Status == StatusCode.Failure) Debug.LogWarning("[Tesela] " + p + ": " + request.Error?.message);
            }
            ImportTmpEssentials();
            File.WriteAllText(MarkerPath, "ok");
        }

        private static void Next()
        {
            if (Pending.Count == 0)
            {
                EditorApplication.update -= Poll;
                _current = null;
                ImportTmpEssentials();
                File.WriteAllText(MarkerPath, "ok");
                if (Failed.Count > 0)
                    Debug.LogWarning("[Tesela] No se instalaron: " + string.Join(", ", Failed) + ". El juego sigue funcionando con las implementaciones falsas.");
                return;
            }
            _current = Client.Add(Pending.Dequeue());
        }

        private static void Poll()
        {
            if (_current == null || !_current.IsCompleted) return;
            if (_current.Status == StatusCode.Failure) Failed.Add(_current.Error?.message ?? "?");
            Next();
        }

        [MenuItem("Tesela/Setup/Importar TMP Essential Resources")]
        public static void ImportTmpEssentials()
        {
            if (Resources.Load("TMP Settings") != null) return;
            TMPro.TMP_PackageResourceImporter.ImportResources(true, false, false);
        }
    }
}
