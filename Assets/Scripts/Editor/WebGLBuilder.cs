#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PrismaDash.EditorTools
{
    /// <summary>Menu "Prisma Dash" com build WebGL (também usado pelo GameCI via -executeMethod).</summary>
    public static class WebGLBuilder
    {
        private const string ScenePath = "Assets/Scenes/Main.unity";
        private const string OutputDir = "Build/WebGL";
        private const string Template = "PROJECT:PrismaPortrait";

        [MenuItem("Prisma Dash/Configurar Build Settings")]
        public static void Configure()
        {
            ApplyPlayerSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("[PrismaDash] Build Settings configurados (retrato, Gzip + decompression fallback).");
        }

        /// <summary>
        /// Gzip com decompression fallback: o GitHub Pages não manda Content-Encoding,
        /// e o loader descomprime os .unityweb no navegador.
        /// </summary>
        public static void ApplyPlayerSettings()
        {
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            PlayerSettings.productName = "Prisma Dash";
            PlayerSettings.companyName = "Gustavo Mazzotti";
            PlayerSettings.bundleVersion = "1.0";
            PlayerSettings.colorSpace = ColorSpace.Gamma;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.defaultScreenWidth = 1080;
            PlayerSettings.defaultScreenHeight = 1920;
            PlayerSettings.defaultWebScreenWidth = 540;
            PlayerSettings.defaultWebScreenHeight = 960;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.template = Template;
        }

        [MenuItem("Prisma Dash/Build WebGL")]
        public static void Build()
        {
            ApplyPlayerSettings();
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
                EditorUserBuildSettings.SwitchActiveBuildTarget(NamedBuildTarget.WebGL, BuildTarget.WebGL);

            string output = CustomBuildPathOrDefault();
            Directory.CreateDirectory(output);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = output,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            });

            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[PrismaDash] Build OK: {Path.GetFullPath(output)} ({report.summary.totalSize / 1024} KB)");
                if (!Application.isBatchMode) EditorUtility.RevealInFinder(output);
            }
            else
            {
                Debug.LogError($"[PrismaDash] Build falhou: {report.summary.result}");
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        private static string CustomBuildPathOrDefault()
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-customBuildPath" && !string.IsNullOrEmpty(args[i + 1]))
                    return args[i + 1];
            }
            return OutputDir;
        }
    }
}
#endif
