using System.IO;
using Tesela.Services;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Tesela.EditorTools
{
    /// <summary>Aplica la configuración Android de la Parte 4 y crea la escena Boot. Idempotente.</summary>
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        public const string BootScene = "Assets/_Tesela/Scenes/Boot.unity";
        public const int TargetSdk = 36;
        public const int MinSdk = 24;
        private const string MarkerPath = "ProjectSettings/TeselaProject.done";

        static ProjectSetup()
        {
            if (Application.isBatchMode || File.Exists(MarkerPath)) return;
            EditorApplication.delayCall += () =>
            {
                Apply();
                File.WriteAllText(MarkerPath, "ok");
            };
        }

        [MenuItem("Tesela/Setup/Configurar proyecto Android")]
        public static void Apply()
        {
            var ids = ExternalIds.Instance;
            PlayerSettings.productName = "Tesela";
            PlayerSettings.companyName = ExternalIds.IsSet(ids.companyName) ? ids.companyName : "Tesela";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, ids.androidPackageName);
            PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)MinSdk;
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)TargetSdk;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Medium);
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            PlayerSettings.runInBackground = false;
            EditorUserBuildSettings.buildAppBundle = true;
            SetActiveInputHandlingBoth();
            EnsureBootScene();
            EnsureRenderPipeline();
            AssetDatabase.SaveAssets();
        }

        /// <summary>"Both": la UI usa el Input System si está instalado y el botón Atrás funciona aunque no lo esté.</summary>
        private static void SetActiveInputHandlingBoth()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (assets == null || assets.Length == 0) return;
            var so = new SerializedObject(assets[0]);
            var prop = so.FindProperty("activeInputHandler");
            if (prop == null) return;
            prop.intValue = 2;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void EnsureBootScene()
        {
            if (!File.Exists(BootScene))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(BootScene));
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, BootScene);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(BootScene, true) };
        }

        private static void EnsureRenderPipeline()
        {
#if TESELA_URP
            const string rendererPath = "Assets/_Tesela/Settings/Tesela2DRenderer.asset";
            const string pipelinePath = "Assets/_Tesela/Settings/TeselaURP.asset";
            if (File.Exists(pipelinePath)) return;
            Directory.CreateDirectory("Assets/_Tesela/Settings");
            var renderer = ScriptableObject.CreateInstance<UnityEngine.Rendering.Universal.Renderer2DData>();
            AssetDatabase.CreateAsset(renderer, rendererPath);
            var pipeline = UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset.Create(renderer);
            pipeline.renderScale = 1f;
            pipeline.msaaSampleCount = 1;
            AssetDatabase.CreateAsset(pipeline, pipelinePath);
            UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
#endif
        }
    }
}
