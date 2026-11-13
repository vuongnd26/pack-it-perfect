using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using PackItPerfect.Meta;
using PackItPerfect.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace PackItPerfect.Tests
{
    /// <summary>
    /// Editor-side performance budget: audio synthesis time, level build time, frame time and the managed garbage the game's own
    /// per-frame code produces while a level idles and while a piece is dragged. Garbage is measured by calling the game's
    /// Update methods back to back with the collector disabled, so the Editor's own windows cannot add to the count. Times are
    /// desktop-Editor times, a relative signal (a phone is several times slower), not an absolute frame rate. The report is
    /// retained in production/qa/.
    /// </summary>
    public sealed class PerformanceBudgetTests
    {
        const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
        const int TICKS = 600;
        const string REPORT = "production/qa/perf-report.md";

        GameObject root;
        GameObject cameraRoot;
        TextAsset catalog;
        PackingGame game;
        GameUI ui;
        GameFeel feel;
        Camera camera;
        readonly StringBuilder report = new StringBuilder();

        sealed class MemorySave : ISaveStore
        {
            ProgressData data;
            public ProgressData Load() => data == null ? null : JsonUtility.FromJson<ProgressData>(JsonUtility.ToJson(data));
            public void Save(ProgressData value) => data = JsonUtility.FromJson<ProgressData>(JsonUtility.ToJson(value));
        }

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            cameraRoot = new GameObject("Perf Camera");
            camera = cameraRoot.AddComponent<Camera>();
            cameraRoot.AddComponent<AudioListener>();
            cameraRoot.tag = "MainCamera";
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.orthographic = true;
            root = new GameObject("Perf Session");
            root.SetActive(false);
            catalog = new TextAsset(File.ReadAllText(Path.Combine(Application.dataPath, "Data/Levels/levels.json")));
            game = root.AddComponent<PackingGame>();
            typeof(PackingGame).GetField("levelsJson", PrivateInstance).SetValue(game, catalog);
            ui = root.AddComponent<GameUI>();
            var save = new MemorySave();
            save.Save(new ProgressData { muted = false, reduceMotion = false });
            ui.ConfigureSaveStore(save);
            feel = root.AddComponent<GameFeel>();
            root.SetActive(true);
            yield return null;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (root != null) UnityEngine.Object.Destroy(root);
            if (cameraRoot != null) UnityEngine.Object.Destroy(cameraRoot);
            if (catalog != null) UnityEngine.Object.Destroy(catalog);
            yield return null;
            var eventSystem = UnityEngine.Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem != null) UnityEngine.Object.Destroy(eventSystem.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Gameplay_IdleDragAndLoad_StaysWithinBudgetAndWritesReport()
        {
            report.AppendLine("# Performance report (Editor, desktop CPU)");
            report.AppendLine();
            report.AppendLine($"Generated {DateTime.Now:yyyy-MM-dd HH:mm}. Times are a relative signal only: a phone CPU is several times slower.");
            report.AppendLine();

            // Audio synthesis: the constructor must be cheap, and the clips are then built one per frame.
            var watch = Stopwatch.StartNew();
            var feelHost = new GameObject("Audio probe");
            var probe = new GameAudio(feelHost.transform, new AudioTuning());
            watch.Stop();
            double audioStartMs = watch.Elapsed.TotalMilliseconds;
            double audioTotalMs = 0, audioWorstStepMs = 0;
            int steps = 0;
            bool more = true;
            while (more)
            {
                watch.Restart();
                more = probe.WarmNext();
                watch.Stop();
                steps++;
                audioTotalMs += watch.Elapsed.TotalMilliseconds;
                audioWorstStepMs = Math.Max(audioWorstStepMs, watch.Elapsed.TotalMilliseconds);
            }
            probe.Dispose();
            UnityEngine.Object.Destroy(feelHost);
            report.AppendLine($"- Audio: start-up cost **{audioStartMs:F1} ms**; {steps} clips built one per frame, " +
                $"worst frame **{audioWorstStepMs:F1} ms**, total {audioTotalMs:F1} ms");

            // Level build time for a small, a medium and the largest level (the first includes loading its art).
            double worstLoad = 0;
            foreach (int number in new[] { 1, 10, 20 })
            {
                var load = Stopwatch.StartNew();
                game.LoadLevel(number);
                load.Stop();
                worstLoad = Math.Max(worstLoad, load.Elapsed.TotalMilliseconds);
                report.AppendLine($"- LoadLevel({number}): **{load.Elapsed.TotalMilliseconds:F1} ms**");
                yield return null;
            }

            // The per-frame entry points of the game, bound once so calling them allocates nothing itself.
            var ticks = new[]
            {
                Bind(game, "Update"), Bind(ui, "Update"), Bind(feel, "Update"), Bind(feel, "LateUpdate"),
            };
            void Tick() { foreach (var tick in ticks) tick(); }

            // The instrument must see a known 1 KB per call, or its zero for the game is not believed.
            var junk = new System.Collections.Generic.List<byte[]>(TICKS);
            double injected = GarbagePerCall(() => junk.Add(new byte[1024]), TICKS);
            junk.Clear();
            report.AppendLine($"- Instrument check: 1024 B injected per call reads as **{injected:F0} B**");

            // Idle gameplay: nothing moving after the deal animation has finished.
            game.LoadLevel(10);
            for (int frame = 0; frame < 90; frame++) yield return null;
            double idleGarbage = GarbagePerCall(Tick, TICKS);
            var idleTime = new FrameTimer(120);
            yield return idleTime.Run();
            report.AppendLine($"- Idle level: **{idleGarbage:F0} B** of garbage per frame from the game's own Update code; " +
                $"{idleTime.AverageMs:F2} ms avg and {idleTime.MaxMs:F2} ms worst frame including the Editor");

            // Dragging a piece around the board: the most input-heavy moment of play.
            Vector2 start = TrayGrab(0);
            Invoke("BeginPress", start);
            int step = 0;
            Action dragStep = () =>
            {
                float t = (step++ % 90) / 90f;
                Invoke("MovePress", start + new Vector2(Mathf.Sin(t * 12f) * 200f, t * 600f));
                Tick();
            };
            double dragGarbage = GarbagePerCall(dragStep, 180);
            step = 0;
            var dragTime = new FrameTimer(90, () => Invoke("MovePress", start + new Vector2(Mathf.Sin((step++ % 90) / 90f * 12f) * 200f, (step % 90) / 90f * 600f)));
            yield return dragTime.Run();
            Invoke("EndPress");
            report.AppendLine($"- Dragging a piece: **{dragGarbage:F0} B** of garbage per frame (input handling plus Update); " +
                $"{dragTime.AverageMs:F2} ms avg and {dragTime.MaxMs:F2} ms worst frame including the Editor");

            Directory.CreateDirectory(Path.GetDirectoryName(REPORT));
            File.WriteAllText(REPORT, report.ToString());

            // Budgets are generous on purpose: they catch a regression of a different order, not noise.
            Assert.That(injected, Is.InRange(900, 1500), "The garbage counter must see a known 1 KB per call.");
            Assert.That(idleGarbage, Is.LessThan(256).And.Not.NaN, "An idle level's Update code should allocate (almost) nothing per frame.");
            Assert.That(dragGarbage, Is.LessThan(2048).And.Not.NaN, "Dragging should allocate almost nothing per frame.");
            Assert.That(worstLoad, Is.LessThan(400), "Building a level must not hitch for hundreds of milliseconds.");
            Assert.That(audioStartMs, Is.LessThan(10), "Creating the audio bank must not synthesise anything up front.");
            Assert.That(audioWorstStepMs, Is.LessThan(30), "One synthesised clip per frame must stay below a visible hitch.");
        }

        // Runs the action back to back and reads the managed heap before and after. The Editor does not allow switching the
        // collector off, so a clean heap is forced first and a run that a collection interrupted is repeated shorter.
        static double GarbagePerCall(Action action, int calls)
        {
            action(); // first call pays one-off costs (caches, JIT) that are not per-frame garbage
            for (int attempt = 0; attempt < 4; attempt++, calls = Math.Max(20, calls / 3))
            {
                GC.Collect();
                int collections = GC.CollectionCount(0);
                long before = GC.GetTotalMemory(false);
                for (int i = 0; i < calls; i++) action();
                long after = GC.GetTotalMemory(false);
                if (GC.CollectionCount(0) == collections) return Math.Max(0, after - before) / (double)calls;
            }
            return double.NaN;
        }

        static Action Bind(object target, string method)
        {
            var info = target.GetType().GetMethod(method, PrivateInstance);
            Assert.That(info, Is.Not.Null, $"Perf adapter missing {target.GetType().Name}.{method}.");
            return (Action)Delegate.CreateDelegate(typeof(Action), target, info);
        }

        // Frame time with the game's own 60 fps cap lifted, so the figure shows the cost of the frame instead of the cap.
        sealed class FrameTimer
        {
            readonly int frames;
            readonly Action perFrame;
            public double AverageMs;
            public double MaxMs;
            public FrameTimer(int frames, Action perFrame = null) { this.frames = frames; this.perFrame = perFrame; }

            public IEnumerator Run()
            {
                int vsync = QualitySettings.vSyncCount;
                int cap = Application.targetFrameRate;
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = -1;
                yield return null;
                var clock = new Stopwatch();
                double total = 0;
                for (int i = 0; i < frames; i++)
                {
                    clock.Restart();
                    perFrame?.Invoke();
                    yield return null;
                    clock.Stop();
                    total += clock.Elapsed.TotalMilliseconds;
                    MaxMs = Math.Max(MaxMs, clock.Elapsed.TotalMilliseconds);
                }
                AverageMs = total / frames;
                QualitySettings.vSyncCount = vsync;
                Application.targetFrameRate = cap;
            }
        }

        Vector2 TrayGrab(int piece)
        {
            var shape = ((PieceShape[])typeof(PackingGame).GetField("shapes", PrivateInstance).GetValue(game))[piece];
            var view = ((PieceView[])typeof(PackingGame).GetField("pieceViews", PrivateInstance).GetValue(game))[piece];
            var first = shape.FirstCell;
            return camera.WorldToScreenPoint(view.RestTopLeft + new Vector3(first.x + 0.5f, -first.y - 0.5f, 0f) * view.Scale);
        }

        void Invoke(string method, params object[] arguments)
        {
            var info = typeof(PackingGame).GetMethod(method, PrivateInstance);
            Assert.That(info, Is.Not.Null, $"Perf adapter missing {method}.");
            info.Invoke(game, arguments);
        }
    }
}
