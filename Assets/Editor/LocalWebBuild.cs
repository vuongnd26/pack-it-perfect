using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace PackItPerfect.EditorTools
{
    /// <summary>Local browser builds of the same gameplay scene; requests survive target-switch domain reloads.</summary>
    [InitializeOnLoad]
    public static class LocalWebBuild
    {
        const string Folder = "Library/LocalWebBuild";
        const string Output = "builds/web/local";
        const string LastRequest = "PackItPerfect.LocalWebBuild.LastRequest";
        static double nextPoll;

        [Serializable] sealed class Request { public int id; public string action; }
        [Serializable] sealed class Result { public int id; public bool ok; public string message; }

        static LocalWebBuild() => EditorApplication.update += Poll;

        [MenuItem("Pack It Perfect/Local Web/Switch to Web")]
        public static void SwitchToWeb()
        {
            EnsureIdle();
            EditorUserBuildSettings.SwitchActiveBuildTargetAsync(BuildTargetGroup.WebGL, BuildTarget.WebGL);
        }

        [MenuItem("Pack It Perfect/Local Web/Build Browser Test")]
        public static void Build()
        {
            EnsureIdle();
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
                throw new InvalidOperationException("Switch to Web and wait for imports before building.");
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0 || scenes.Any(s => !File.Exists(s)))
                throw new InvalidOperationException("The enabled build scenes are missing.");

            var compression = PlayerSettings.WebGL.compressionFormat;
            var template = PlayerSettings.WebGL.template;
            var memory = PlayerSettings.WebGL.initialMemorySize;
            bool defaultGraphics = PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.WebGL);
            var graphics = PlayerSettings.GetGraphicsAPIs(BuildTarget.WebGL);
            try
            {
                // Uncompressed local files work with a plain HTTP server; no gzip/Brotli response headers needed.
                PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
                PlayerSettings.WebGL.template = "PROJECT:PackLocal";
                PlayerSettings.WebGL.initialMemorySize = 256;
                PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.WebGL, false);
                PlayerSettings.SetGraphicsAPIs(BuildTarget.WebGL, new[] { GraphicsDeviceType.OpenGLES3 });
                Directory.CreateDirectory(Output);
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = Output,
                    target = BuildTarget.WebGL,
                    options = BuildOptions.None,
                });
                var summary = report.summary;
                File.WriteAllText(Path.Combine(Output, "build-report.txt"),
                    $"Unity {Application.unityVersion}\nVersion {PlayerSettings.bundleVersion}\n" +
                    $"Result: {summary.result}\nErrors: {summary.totalErrors}\nWarnings: {summary.totalWarnings}\n" +
                    $"Duration: {summary.totalTime.TotalSeconds:F1}s\nBuilt UTC: {DateTime.UtcNow:O}\n");
                if (summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException($"Web build {summary.result}: {summary.totalErrors} errors.");
            }
            finally
            {
                PlayerSettings.WebGL.compressionFormat = compression;
                PlayerSettings.WebGL.template = template;
                PlayerSettings.WebGL.initialMemorySize = memory;
                PlayerSettings.SetGraphicsAPIs(BuildTarget.WebGL, graphics);
                PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.WebGL, defaultGraphics);
                AssetDatabase.SaveAssets();
            }
        }

        static void EnsureIdle()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || BuildPipeline.isBuildingPlayer)
                throw new InvalidOperationException("Wait for Play, compilation and the current build to finish.");
        }

        static void Poll()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || BuildPipeline.isBuildingPlayer ||
                EditorApplication.timeSinceStartup < nextPoll) return;
            nextPoll = EditorApplication.timeSinceStartup + 0.5;
            var path = Path.Combine(Folder, "request.json");
            if (!File.Exists(path)) return;
            Request request;
            try { request = JsonUtility.FromJson<Request>(File.ReadAllText(path)); }
            catch (Exception) { return; }
            if (request == null || request.id == SessionState.GetInt(LastRequest, -1)) return;
            SessionState.SetInt(LastRequest, request.id);
            try
            {
                switch (request.action)
                {
                    case "switch-web": SwitchToWeb(); break;
                    case "switch-android":
                        EnsureIdle();
                        EditorUserBuildSettings.SwitchActiveBuildTargetAsync(BuildTargetGroup.Android, BuildTarget.Android);
                        break;
                    case "build": Save(request.id, false, "BUILDING"); Build(); break;
                    case "status": break;
                    default: throw new InvalidOperationException("Unknown local Web action.");
                }
                Save(request.id, true, $"Completed {request.action}; active target: {EditorUserBuildSettings.activeBuildTarget}.");
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                Save(request.id, false, error.GetBaseException().Message);
            }
        }

        static void Save(int id, bool ok, string message)
        {
            Directory.CreateDirectory(Folder);
            File.WriteAllText(Path.Combine(Folder, "result.json"), JsonUtility.ToJson(new Result
                { id = id, ok = ok, message = message }, true));
        }
    }
}
