using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PackItPerfect.EditorTools
{
    /// <summary>Builds versioned, explicitly debug-signed internal Android APKs from release.json.</summary>
    [InitializeOnLoad]
    public static class ReleaseBuild
    {
        const string ConfigPath = "tools/build/release.json";
        const string BridgeFolder = "Library/ReleaseBuild";
        const string LastRequestKey = "PackItPerfect.ReleaseBuild.LastRequest";
        static double nextPoll;
        static bool busy;

        [Serializable]
        sealed class Config
        {
            public string version;
            public int androidVersionCode;
            public string applicationId;
            public string channel;
            public string outputRoot;
        }

        [Serializable]
        sealed class Request
        {
            public int id;
            public string action;
        }

        [Serializable]
        sealed class Result
        {
            public int id;
            public bool ok;
            public string message;
        }

        [Serializable]
        sealed class Manifest
        {
            public string product;
            public string version;
            public int androidVersionCode;
            public string applicationId;
            public string channel;
            public string signing;
            public string unityVersion;
            public string sourceCommit;
            public string trackedWorktreeChanges;
            public string builtAtUtc;
            public string buildGuid;
            public string architecture;
            public string scriptingBackend;
            public string minimumSdk;
            public string targetSdkSetting;
            public string buildOptions;
            public string[] scenes;
            public string apk;
            public long apkBytes;
            public string sha256;
            public string buildResult;
            public int warnings;
            public int errors;
            public double durationSeconds;
            public string deviceVerification;
        }

        static ReleaseBuild()
        {
            EditorApplication.update -= Poll;
            EditorApplication.update += Poll;
        }

        /// <summary>Applies the approved version and non-development ARM64/IL2CPP build settings.</summary>
        [MenuItem("Pack It Perfect/Release/Configure Internal Android")]
        public static void ConfigureInternalAndroid()
        {
            EnsureIdle();
            Configure(ReadConfig());
            Debug.Log("[ReleaseBuild] Internal Android release settings saved.");
        }

        /// <summary>Builds an internal APK; also usable through Unity's -executeMethod option.</summary>
        [MenuItem("Pack It Perfect/Release/Build Internal Android APK")]
        public static void BuildInternalAndroid()
        {
            EnsureIdle();
            Config config = ReadConfig();
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                throw new InvalidOperationException("Select Android as the active build target before building.");
            Configure(config);
            string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            if (scenes.Length == 0 || scenes.Any(scene => !File.Exists(scene)))
                throw new InvalidOperationException("The enabled build scene list is empty or contains a missing scene.");
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save or discard unsaved scene changes before building. No scene is auto-saved.");

            string output = Path.Combine(config.outputRoot, config.version);
            Directory.CreateDirectory(output);
            string name = $"pack-it-perfect-{config.version}-b{config.androidVersionCode}-android-arm64-internal.apk";
            string apk = Path.Combine(output, name);
            string manifestPath = Path.Combine(output, "build-manifest.json");
            if (File.Exists(apk) || File.Exists(manifestPath))
                throw new InvalidOperationException("This release already exists. Use a new build number/output folder; existing releases are never overwritten.");

            var manifest = new Manifest
            {
                product = PlayerSettings.productName,
                version = config.version,
                androidVersionCode = config.androidVersionCode,
                applicationId = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android),
                channel = config.channel,
                signing = "Android debug keystore; INTERNAL ONLY, NOT FOR GOOGLE PLAY",
                unityVersion = Application.unityVersion,
                sourceCommit = Git("rev-parse HEAD"),
                trackedWorktreeChanges = Git("diff --name-only HEAD"),
                builtAtUtc = DateTime.UtcNow.ToString("O"),
                architecture = PlayerSettings.Android.targetArchitectures.ToString(),
                scriptingBackend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android).ToString(),
                minimumSdk = PlayerSettings.Android.minSdkVersion.ToString(),
                targetSdkSetting = PlayerSettings.Android.targetSdkVersion.ToString(),
                buildOptions = BuildOptions.None.ToString(),
                scenes = scenes,
                apk = name,
                deviceVerification = "NOT ASSESSED: no Android device connected; Editor tests do not verify device behavior."
            };
            var messages = new List<string>();
            Application.LogCallback capture = (message, stack, type) =>
            {
                if (type != LogType.Log)
                    messages.Add($"[{type}] {message}\n{stack}");
            };
            BuildReport report;
            Application.logMessageReceived += capture;
            try
            {
                report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = apk,
                    target = BuildTarget.Android,
                    options = BuildOptions.None
                });
            }
            finally
            {
                Application.logMessageReceived -= capture;
                File.WriteAllLines(Path.Combine(output, "build-diagnostics.log"), messages);
            }
            var summary = report.summary;
            manifest.buildResult = summary.result.ToString();
            manifest.buildGuid = summary.guid.ToString();
            manifest.warnings = (int)summary.totalWarnings;
            manifest.errors = (int)summary.totalErrors;
            manifest.durationSeconds = summary.totalTime.TotalSeconds;
            if (summary.result == BuildResult.Succeeded && File.Exists(apk))
            {
                manifest.apkBytes = new FileInfo(apk).Length;
                using (var input = File.OpenRead(apk))
                using (var sha = SHA256.Create())
                    manifest.sha256 = BitConverter.ToString(sha.ComputeHash(input)).Replace("-", "").ToLowerInvariant();
                File.WriteAllText(Path.Combine(output, "SHA256SUMS"), $"{manifest.sha256}  {name}\n");
            }
            File.WriteAllText(manifestPath, JsonUtility.ToJson(manifest, true) + "\n");
            if (summary.result != BuildResult.Succeeded || manifest.apkBytes == 0)
                throw new InvalidOperationException($"Android build {summary.result}: {summary.totalErrors} errors. See {output} and Editor.log.");
            Debug.Log($"[ReleaseBuild] Built {apk}: {manifest.apkBytes} bytes; {manifest.warnings} warnings, {manifest.errors} errors.");
        }

        static Config ReadConfig()
        {
            var config = JsonUtility.FromJson<Config>(File.ReadAllText(ConfigPath));
            if (config == null || !Version.TryParse(config.version, out var version) || version.Build < 0 || version.Revision >= 0 ||
                config.androidVersionCode <= 0 || config.androidVersionCode > 2100000000 || config.channel != "internal" ||
                config.applicationId != PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android) ||
                config.outputRoot != "builds/android")
                throw new InvalidOperationException("Invalid internal release config; expected major.minor.patch, positive Android code, unchanged application ID and builds/android output.");
            return config;
        }

        static void Configure(Config config)
        {
            if (config.androidVersionCode < PlayerSettings.Android.bundleVersionCode)
                throw new InvalidOperationException("Android versionCode cannot go backwards.");
            if (PlayerSettings.Android.useCustomKeystore)
                throw new InvalidOperationException("A custom signing key is active. Internal builds will not silently replace it.");
            PlayerSettings.bundleVersion = config.version;
            PlayerSettings.Android.bundleVersionCode = config.androidVersionCode;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.allowDebugging = false;
            EditorUserBuildSettings.connectProfiler = false;
            EditorUserBuildSettings.buildWithDeepProfilingSupport = false;
            AssetDatabase.SaveAssets();
        }

        static void EnsureIdle()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || BuildPipeline.isBuildingPlayer)
                throw new InvalidOperationException("Stop Play and wait for compilation/build completion first.");
        }

        static string Git(string arguments)
        {
            using (var process = Process.Start(new ProcessStartInfo("git", arguments)
            {
                WorkingDirectory = Directory.GetCurrentDirectory(),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }))
            {
                string text = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();
                if (process.ExitCode != 0) throw new InvalidOperationException("Cannot record Git source provenance.");
                return text;
            }
        }

        static void Poll()
        {
            if (busy || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.timeSinceStartup < nextPoll)
                return;
            nextPoll = EditorApplication.timeSinceStartup + 0.5;
            string path = Path.Combine(BridgeFolder, "request.json");
            if (!File.Exists(path)) return;
            Request request;
            try { request = JsonUtility.FromJson<Request>(File.ReadAllText(path)); }
            catch (Exception) { return; }
            if (request == null || request.id == SessionState.GetInt(LastRequestKey, -1)) return;
            SessionState.SetInt(LastRequestKey, request.id);
            busy = true;
            try
            {
                switch (request.action)
                {
                    case "configure": ConfigureInternalAndroid(); break;
                    case "build":
                        Save(request.id, false, "BUILDING: wait for terminal result.");
                        BuildInternalAndroid();
                        break;
                    case "status": break;
                    default: throw new InvalidOperationException("Unknown release action.");
                }
                Save(request.id, true, $"Completed {request.action}: version {PlayerSettings.bundleVersion}, code {PlayerSettings.Android.bundleVersionCode}, target {EditorUserBuildSettings.activeBuildTarget}.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Save(request.id, false, exception.GetBaseException().Message);
            }
            finally { busy = false; }
        }

        static void Save(int id, bool ok, string message)
        {
            Directory.CreateDirectory(BridgeFolder);
            File.WriteAllText(Path.Combine(BridgeFolder, "result.json"), JsonUtility.ToJson(new Result
            { id = id, ok = ok, message = message }, true));
        }
    }
}
