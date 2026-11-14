using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace PackItPerfect.Tests
{
    /// <summary>Editor-only, fixed-action bridge for local verification when MCP is disconnected.</summary>
    [InitializeOnLoad]
    public static class EditorVerificationBridge
    {
        const string Folder = "Library/MobilePuzzleVerification";
        const string LastRequestKey = "PackItPerfect.Verification.LastRequest";
        static double nextPoll;
        static bool busy;
        static bool testsRunning;
        static TestRunnerApi runner;
        static readonly Callbacks callbacks = new Callbacks();

        [Serializable]
        sealed class Request
        {
            public int id;
            public string action;
            public string[] testNames;
        }

        [Serializable]
        sealed class Result
        {
            public int id;
            public bool ok;
            public string message;
        }

        static EditorVerificationBridge()
        {
            EditorApplication.update -= Poll;
            EditorApplication.update += Poll;
        }

        static void Poll()
        {
            if (busy || EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.timeSinceStartup < nextPoll) return;
            nextPoll = EditorApplication.timeSinceStartup + 0.5;
            string path = Path.Combine(Folder, "request.json");
            if (!File.Exists(path)) return;
            Request request;
            try { request = JsonUtility.FromJson<Request>(File.ReadAllText(path)); }
            catch (Exception) { return; }
            if (request == null || request.id == SessionState.GetInt(LastRequestKey, -1)) return;
            SessionState.SetInt(LastRequestKey, request.id);
            _ = Execute(request);
        }

        static async Task Execute(Request request)
        {
            busy = true;
            try
            {
                switch (request.action)
                {
                    case "connect-mcp":
                        Type locator = null;
                        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                        {
                            locator = assembly.GetType("MCPForUnity.Editor.Services.MCPServiceLocator");
                            if (locator != null) break;
                        }
                        if (locator == null) throw new InvalidOperationException("Installed MCP Editor assembly is not loaded.");
                        object bridge = locator.GetProperty("Bridge", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                        if (bridge == null) throw new InvalidOperationException("MCP bridge service unavailable.");
                        var method = bridge.GetType().GetMethod("StartAsync", Type.EmptyTypes);
                        if (method == null) throw new InvalidOperationException("MCP StartAsync API unavailable.");
                        bool connected = await (Task<bool>)method.Invoke(bridge, null);
                        Save(request.id, connected, connected ? "MCP bridge connected." : "MCP bridge did not connect.");
                        break;
                    case "orientation":
                        var game = UnityEngine.Object.FindFirstObjectByType<PackingGame>();
                        if (game == null || game.GetComponent<PackItPerfect.UI.GameUI>() == null)
                            throw new InvalidOperationException("Saved scene runtime components missing.");
                        var serialized = new SerializedObject(game);
                        if (serialized.FindProperty("levelsJson").objectReferenceValue == null)
                            throw new InvalidOperationException("Scene catalog reference missing.");
                        Save(request.id, true, $"Scene component/catalog references intact. Default orientation: {PlayerSettings.defaultInterfaceOrientation}; " +
                            $"landscape permissions: {PlayerSettings.allowedAutorotateToLandscapeLeft}/{PlayerSettings.allowedAutorotateToLandscapeRight}. " +
                            "Permission flags are active only when orientation is AutoRotation.");
                        break;
                    case "import-art":
                        if (EditorApplication.isPlayingOrWillChangePlaymode)
                            throw new InvalidOperationException("Stop Play before importing art.");
                        AssetDatabase.Refresh();
                        foreach (string file in Directory.GetFiles("Assets/Resources/Art", "*.png", SearchOption.AllDirectories))
                        {
                            string assetPath = file.Replace('\\', '/');
                            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                            if (importer == null || importer.textureType == TextureImporterType.Sprite) continue;
                            importer.textureType = TextureImporterType.Sprite;
                            importer.spriteImportMode = SpriteImportMode.Single;
                            importer.spritePixelsPerUnit = assetPath.Contains("/Items/") ? 128 : 100;
                            importer.alphaIsTransparency = true;
                            importer.mipmapEnabled = false;
                            importer.isReadable = false;
                            importer.filterMode = FilterMode.Bilinear;
                            importer.wrapMode = TextureWrapMode.Clamp;
                            importer.textureCompression = TextureImporterCompression.Uncompressed;
                            importer.maxTextureSize = 2048;
                            var settings = new TextureImporterSettings();
                            importer.ReadTextureSettings(settings);
                            settings.spriteMeshType = SpriteMeshType.FullRect;
                            settings.spriteGenerateFallbackPhysicsShape = false;
                            importer.SetTextureSettings(settings);
                            importer.SaveAndReimport();
                        }
                        Save(request.id, true, "New art imported as sprites; existing sprite import settings preserved.");
                        break;
                    case "reimport-art":
                        if (EditorApplication.isPlayingOrWillChangePlaymode)
                            throw new InvalidOperationException("Stop Play before importing art.");
                        int reimported = 0;
                        foreach (string file in Directory.GetFiles("Assets/Resources", "*.png", SearchOption.AllDirectories))
                        {
                            AssetDatabase.ImportAsset(file.Replace('\\', '/'), ImportAssetOptions.ForceUpdate);
                            reimported++;
                        }
                        Save(request.id, true, $"Re-imported {reimported} textures.");
                        break;
                    case "editmode":
                    case "playmode":
                        if (EditorApplication.isPlayingOrWillChangePlaymode)
                            throw new InvalidOperationException("Stop Play before starting tests.");
                        Directory.CreateDirectory("test-results");
                        if (runner == null) runner = ScriptableObject.CreateInstance<TestRunnerApi>();
                        runner.UnregisterCallbacks(callbacks);
                        runner.RegisterCallbacks(callbacks);
                        callbacks.requestId = request.id;
                        bool playMode = request.action == "playmode";
                        callbacks.resultPath = playMode ? "test-results/playmode.xml" : "test-results/editmode.xml";
                        testsRunning = true;
                        Save(request.id, true, "Tests started; await completed result and XML.");
                        runner.Execute(new ExecutionSettings(new Filter
                        {
                            testMode = playMode ? TestMode.PlayMode : TestMode.EditMode,
                            testNames = request.testNames,
                            assemblyNames = new[] { playMode ? "PackItPerfect.PlayMode.Tests" : typeof(EditorVerificationBridge).Assembly.GetName().Name }
                        }));
                        return;
                    default:
                        throw new InvalidOperationException("Unknown verification action.");
                }
            }
            catch (Exception exception)
            {
                testsRunning = false;
                Save(request.id, false, exception.GetBaseException().Message);
            }
            finally { busy = testsRunning; }
        }

        static void Save(int id, bool ok, string message)
        {
            Directory.CreateDirectory(Folder);
            File.WriteAllText(Path.Combine(Folder, "result.json"), JsonUtility.ToJson(new Result
            { id = id, ok = ok, message = message }, true));
        }

        sealed class Callbacks : ICallbacks
        {
            public int requestId;
            public string resultPath;
            public void RunStarted(ITestAdaptor testsToRun) { busy = true; }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }
            public void RunFinished(ITestResultAdaptor result)
            {
                TestRunnerApi.SaveResultToFile(result, Path.GetFullPath(resultPath));
                Save(requestId, result.FailCount == 0 && result.PassCount > 0,
                    $"Completed: {result.PassCount} passed, {result.FailCount} failed, {result.SkipCount} skipped.");
                testsRunning = false;
                busy = false;
            }
        }
    }
}
