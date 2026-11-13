// Editor-only verification helper for the Cozy Travel upgrade. Not part of the game build (Assets/Editor).
// It lets a tool outside Unity drive the ALREADY OPEN Editor through two local files, with a fixed list of actions:
//   Library/CozyWorkbench/request.json  {"id": 7, "action": "shot", "arg": "01-main"}
//   Library/CozyWorkbench/result.json   {"id": 7, "ok": true, "message": "...", "playing": true, ...}
// There is no network listener and no way to run arbitrary code or shell commands: unknown actions are refused.
// Actions: status | clear | refresh | play | stop | level <n> | restart | hint | autopack | undo | shot <name>

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace PackItPerfect.EditorTools
{
    /// <summary>Polls a local request file and performs one whitelisted Editor action per request.</summary>
    [InitializeOnLoad]
    public static class CozyWorkbench
    {
        const string FOLDER = "Library/CozyWorkbench";
        const string EVIDENCE_FOLDER = "production/qa/evidence/cozy-travel";
        const double POLL_SECONDS = 0.25;
        const int MAX_LOG_LINES = 40;

        [Serializable]
        sealed class Request
        {
            public int id;
            public string action;
            public string arg;
        }

        [Serializable]
        sealed class Result
        {
            public int id;
            public bool ok;
            public string message;
            public bool playing;
            public bool compiling;
            public int level;
            public string screen;
            public string[] errors;
        }

        static readonly List<string> problems = new List<string>();
        static double nextPoll;
        static int lastId = -1;

        static CozyWorkbench()
        {
            Directory.CreateDirectory(FOLDER);
            EditorApplication.update -= Poll;
            EditorApplication.update += Poll;
            Application.logMessageReceived -= OnLog;
            Application.logMessageReceived += OnLog;
            CompilationPipeline.assemblyCompilationFinished -= OnCompiled;
            CompilationPipeline.assemblyCompilationFinished += OnCompiled;
        }

        static void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                Remember($"{type}: {condition}");
            }
        }

        static void OnCompiled(string assembly, CompilerMessage[] messages)
        {
            foreach (var message in messages)
            {
                if (message.type == CompilerMessageType.Error)
                {
                    Remember($"Compile: {message.message}");
                }
            }
        }

        static void Remember(string line)
        {
            if (problems.Count < MAX_LOG_LINES)
            {
                problems.Add(line.Length > 300 ? line.Substring(0, 300) : line);
            }
        }

        static void Poll()
        {
            if (EditorApplication.timeSinceStartup < nextPoll)
            {
                return;
            }
            nextPoll = EditorApplication.timeSinceStartup + POLL_SECONDS;
            string path = Path.Combine(FOLDER, "request.json");
            if (!File.Exists(path))
            {
                return;
            }
            Request request;
            try
            {
                request = JsonUtility.FromJson<Request>(File.ReadAllText(path));
            }
            catch (Exception)
            {
                return; // half-written file: try again on the next poll
            }
            if (request == null || request.id == lastId)
            {
                return;
            }
            lastId = request.id;
            var result = new Result { id = request.id };
            try
            {
                result.message = Run(request.action ?? "", request.arg ?? "");
                result.ok = true;
            }
            catch (Exception e)
            {
                result.ok = false;
                result.message = e.Message;
            }
            result.playing = EditorApplication.isPlaying;
            result.compiling = EditorApplication.isCompiling;
            var game = UnityEngine.Object.FindFirstObjectByType<PackingGame>();
            result.level = game != null && game.CurrentLevel != null ? game.CurrentLevelNumber : 0;
            result.screen = $"{Screen.width}x{Screen.height}";
            result.errors = problems.ToArray();
            File.WriteAllText(Path.Combine(FOLDER, "result.json"), JsonUtility.ToJson(result, true));
        }

        static string Run(string action, string arg)
        {
            switch (action)
            {
                case "status":
                    return "ok";
                case "clear":
                    problems.Clear();
                    return "problem list cleared";
                case "refresh":
                    if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play first.");
                    problems.Clear();
                    AssetDatabase.Refresh();
                    return "refresh requested";
                case "play":
                    if (EditorApplication.isCompiling) throw new InvalidOperationException("Still compiling.");
                    EditorApplication.isPlaying = true;
                    return "entering Play";
                case "stop":
                    EditorApplication.isPlaying = false;
                    return "leaving Play";
                case "level":
                    Playing().LoadLevel(int.Parse(arg));
                    return "level requested";
                case "restart":
                    Playing().RestartLevel();
                    return "restarted";
                case "hint":
                    return Playing().RequestHint() ? "hint applied" : "hint refused";
                case "autopack":
                    return Playing().RequestAutoPack() ? "auto pack started" : "auto pack refused";
                case "undo":
                    return Playing().RequestUndo() ? "undone" : "nothing to undo";
                case "shot":
                    return Shot(arg);
                default:
                    throw new InvalidOperationException($"Unknown action '{action}'.");
            }
        }

        static PackingGame Playing()
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Not in Play mode.");
            var game = UnityEngine.Object.FindFirstObjectByType<PackingGame>();
            if (game == null) throw new InvalidOperationException("No PackingGame in the scene.");
            return game;
        }

        static string Shot(string name)
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Not in Play mode.");
            foreach (char c in name)
            {
                if (!char.IsLetterOrDigit(c) && c != '-' && c != '_') throw new InvalidOperationException("Bad shot name.");
            }
            if (name.Length == 0) throw new InvalidOperationException("Shot needs a name.");
            Directory.CreateDirectory(EVIDENCE_FOLDER);
            string file = Path.Combine(EVIDENCE_FOLDER, name + ".png");
            ScreenCapture.CaptureScreenshot(file);
            return file;
        }
    }
}
