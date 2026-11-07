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

        private static AddAndRemoveRequest _request;

        static PackageSetup()
        {
            if (Application.isBatchMode || File.Exists(MarkerPath)) return;
            EditorApplication.delayCall += () => Install(false);
        }

        [MenuItem("Tesela/Setup/Instalar paquetes")]
        public static void InstallFromMenu() => Install(true);

        /// <summary>Para modo batch: -executeMethod Tesela.EditorTools.PackageSetup.InstallBlocking</summary>
        public static void InstallBlocking()
        {
            var request = Client.AddAndRemove(Packages, null);
            while (!request.IsCompleted) System.Threading.Thread.Sleep(200);
            if (request.Status == StatusCode.Failure) Debug.LogError("[Tesela] Paquetes: " + request.Error?.message);
            ImportTmpEssentials();
            File.WriteAllText(MarkerPath, "ok");
        }

        private static void Install(bool interactive)
        {
            if (_request != null && !_request.IsCompleted) return;
            _request = Client.AddAndRemove(Packages, null);
            EditorApplication.update += Poll;
            if (interactive) Debug.Log("[Tesela] Instalando paquetes…");
        }

        private static void Poll()
        {
            if (_request == null || !_request.IsCompleted) return;
            EditorApplication.update -= Poll;
            if (_request.Status == StatusCode.Failure)
            {
                Debug.LogWarning("[Tesela] No se pudieron instalar todos los paquetes: " + _request.Error?.message +
                                 ". El juego sigue funcionando con las implementaciones falsas.");
            }
            ImportTmpEssentials();
            File.WriteAllText(MarkerPath, "ok");
            _request = null;
        }

        [MenuItem("Tesela/Setup/Importar TMP Essential Resources")]
        public static void ImportTmpEssentials()
        {
            if (Resources.Load("TMP Settings") != null) return;
            TMPro.TMP_PackageResourceImporter.ImportResources(true, false, false);
        }
    }
}
