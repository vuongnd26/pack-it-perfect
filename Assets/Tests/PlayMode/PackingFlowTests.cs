using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using PackItPerfect.Meta;
using PackItPerfect.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace PackItPerfect.Tests
{
    /// <summary>Integration checks through the real controller, with an isolated in-memory save.</summary>
    public sealed class PackingFlowTests
    {
        const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
        GameObject root;
        GameObject cameraRoot;
        TextAsset catalog;
        PackingGame game;
        GameUI ui;
        Camera camera;
#if UNITY_EDITOR
        UnityEditor.EditorWindow gameView;
        int originalGameViewSize = -1;
#endif

        sealed class MemorySave : ISaveStore
        {
            ProgressData data;
            public ProgressData Load() => data == null ? null : JsonUtility.FromJson<ProgressData>(JsonUtility.ToJson(data));
            public void Save(ProgressData value) => data = JsonUtility.FromJson<ProgressData>(JsonUtility.ToJson(value));
        }

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // This suite must never drive a production scene or its save.
            Assert.That(UnityEngine.Object.FindAnyObjectByType<PackingGame>(), Is.Null,
                "Run PlayMode tests in the Test Runner's empty test scene, not a live game session.");
            cameraRoot = new GameObject("QA Camera");
            camera = cameraRoot.AddComponent<Camera>();
            cameraRoot.tag = "MainCamera";
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.orthographic = true;
            root = new GameObject("QA Packing Session");
            root.SetActive(false);
            catalog = new TextAsset(File.ReadAllText(Path.Combine(Application.dataPath, "Data/Levels/levels.json")));
            game = root.AddComponent<PackingGame>();
            SetField(game, "levelsJson", catalog);
            ui = root.AddComponent<GameUI>();
            var save = new MemorySave();
            save.Save(new ProgressData { muted = true, reduceMotion = true });
            ui.ConfigureSaveStore(save);
            root.AddComponent<GameFeel>();
            root.SetActive(true);
            yield return null;
            game.ReduceMotion = true;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
#if UNITY_EDITOR
            if (gameView != null && originalGameViewSize >= 0)
                gameView.GetType().GetProperty("selectedSizeIndex", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .SetValue(gameView, originalGameViewSize);
#endif
            if (root != null) UnityEngine.Object.Destroy(root);
            if (cameraRoot != null) UnityEngine.Object.Destroy(cameraRoot);
            if (catalog != null) UnityEngine.Object.Destroy(catalog);
            yield return null;
            var eventSystem = UnityEngine.Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem != null) UnityEngine.Object.Destroy(eventSystem.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AllTwentyLevels_ControllerDragReplay_WinsWithoutAssists()
        {
            Assert.That(game.LevelCount, Is.EqualTo(20));
            for (int number = 1; number <= game.LevelCount; number++)
            {
                LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex($"^PackingGame: loaded level {number} "));
                game.LoadLevel(number);
                yield return null;
                var level = game.CurrentLevel;
                Assert.That(level, Is.Not.Null, $"Level {number} failed to load.");
                yield return Capture($"level-{number:00}-initial");
                for (int piece = 0; piece < level.SolutionShapes.Length; piece++)
                {
                    RotateToReference(piece, level.SolutionShapes[piece]);
                    DragTo(piece, level.SolutionOrigins[piece]);
                    Assert.That(level.Board.IsPlaced(piece), Is.True, $"Level {number}, piece {piece}: drag failed.");
                }
                Assert.That(game.IsWon, Is.True, $"Level {number}: full controller replay did not win.");
                Assert.That(game.AssistUsed, Is.False);
                Assert.That(ui.Timer.IsRunning, Is.False, "Winning callback must stop the timer.");
                yield return new WaitForSecondsRealtime(1.5f);
                yield return Capture($"level-{number:00}-won");
                LogAssert.NoUnexpectedReceived();
            }
        }

        [UnityTest]
        public IEnumerator AutoPack_SearchBudgetExhausted_LeavesBoardAndAssistStateUnchanged()
        {
            game.LoadLevel(1);
            yield return null;
            var tuning = Field<PackingTuning>(game, "tuning");
            tuning.autoPackNodeLimit = 0;
            tuning.autoPackFlySeconds = 0f;
            int completions = 0;
            bool success = true;
            game.AutoPackFinished += packed => { completions++; success = packed; };

            Assert.That(game.RequestAutoPack(), Is.True);
            yield return null;

            Assert.That(completions, Is.EqualTo(1));
            Assert.That(success, Is.False, "Unknown search outcome must not use the impossible-board fallback.");
            Assert.That(game.AssistUsed, Is.False);
            Assert.That(game.IsBusy, Is.False);
            for (int piece = 0; piece < game.CurrentLevel.SolutionShapes.Length; piece++)
                Assert.That(game.CurrentLevel.Board.IsPlaced(piece), Is.False);
        }

        [UnityTest]
        public IEnumerator AutoPack_CancelledDuringFlight_PreservesShapeAndCompletesOnce()
        {
            game.LoadLevel(2);
            yield return null;
            game.ReduceMotion = false;
            Field<PackingTuning>(game, "tuning").autoPackFlySeconds = 10f;
            var before = (PieceShape[])Field<PieceShape[]>(game, "shapes").Clone();
            var views = Field<PieceView[]>(game, "pieceViews");
            var turns = new int[views.Length];
            for (int i = 0; i < turns.Length; i++) turns[i] = views[i].ArtQuarterTurns;
            int completions = 0;
            bool success = true;
            game.AutoPackFinished += packed => { completions++; success = packed; };

            Assert.That(game.RequestAutoPack(), Is.True);
            game.SetInputLocked(true);
            yield return null;
            yield return null;

            Assert.That(game.IsBusy, Is.False, "Input lock must cancel the flight immediately, not after its animation duration.");
            Assert.That(completions, Is.EqualTo(1));
            Assert.That(success, Is.False);
            Assert.That(game.AssistUsed, Is.False);
            for (int i = 0; i < before.Length; i++)
            {
                Assert.That(Field<PieceShape[]>(game, "shapes")[i].SameCells(before[i]), Is.True);
                Assert.That(views[i].ArtQuarterTurns, Is.EqualTo(turns[i]));
                Assert.That(game.CurrentLevel.Board.IsPlaced(i), Is.False);
            }
        }

        sealed class FeedbackSpy : IFeedback
        {
            public string Message;
            public void Toast(string message) => Message = message;
            public void Word(Vector3 position, string word) { }
            public void Clear() => Message = null;
        }

        [UnityTest]
        public IEnumerator CombinedPlacementFailure_ShowsHighestPriorityRule()
        {
            game.LoadLevel(16);
            yield return null;
            var spy = new FeedbackSpy();
            game.Feedback = spy;
            Invoke("ToastPlacementFailure", 0, PlacementFailure.Heavy | PlacementFailure.Fragile);
            Assert.That(spy.Message, Is.EqualTo(GameTexts.HEAVY_VIOLATION));
        }

        [UnityTest]
        public IEnumerator ApplicationPause_LocksInputAndPreservesClockUntilExplicitResume()
        {
            game.LoadLevel(5);
            yield return null;
            root.SendMessage("OnApplicationPause", true, SendMessageOptions.DontRequireReceiver);
            float remaining = ui.Timer.Remaining;
            yield return null;
            yield return null;
            Assert.That(game.InputLocked, Is.True);
            Assert.That(ui.Timer.Remaining, Is.EqualTo(remaining));
            root.SendMessage("OnApplicationPause", false, SendMessageOptions.DontRequireReceiver);
            Assert.That(game.InputLocked, Is.True, "Returning must leave the pause overlay open.");
            typeof(GameUI).GetMethod("ClosePause", PrivateInstance).Invoke(ui, null);
            Assert.That(game.InputLocked, Is.False);
        }

        [UnityTest]
        public IEnumerator AutoPack_CancelWithPlacedPiece_PreservesPlacementAndUndo()
        {
            game.LoadLevel(2);
            yield return null;
            var level = game.CurrentLevel;
            RotateToReference(0, level.SolutionShapes[0]);
            DragTo(0, level.SolutionOrigins[0]);
            Assert.That(game.CanUndo, Is.True);
            game.ReduceMotion = false;
            Field<PackingTuning>(game, "tuning").autoPackFlySeconds = 10f;
            Assert.That(game.RequestAutoPack(), Is.True);
            game.SetInputLocked(true);
            yield return null;
            Assert.That(level.Board.TryGetPlacement(0, out var shape, out var origin), Is.True);
            Assert.That(shape.SameCells(level.SolutionShapes[0]), Is.True);
            Assert.That(origin, Is.EqualTo(level.SolutionOrigins[0]));
            game.SetInputLocked(false);
            Assert.That(game.CanUndo, Is.True);
            Assert.That(game.RequestUndo(), Is.True);
            Assert.That(level.Board.IsPlaced(0), Is.False);
        }

        [UnityTest]
        public IEnumerator AutoPack_NavigationDuringFlight_DoesNotCommitIntoReplacementLevel()
        {
            game.LoadLevel(2);
            yield return null;
            game.ReduceMotion = false;
            Field<PackingTuning>(game, "tuning").autoPackFlySeconds = 10f;
            int callbacks = 0;
            game.AutoPackFinished += success => { Assert.That(success, Is.False); callbacks++; };
            Assert.That(game.RequestAutoPack(), Is.True);
            game.LoadLevel(3);
            yield return null;
            Assert.That(callbacks, Is.EqualTo(1));
            Assert.That(game.IsBusy, Is.False);
            Assert.That(game.AssistUsed, Is.False);
            Assert.That(game.CurrentLevel.Board.CoveredCellCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator AutoPack_ImmediateUICompletion_ConsumesExactlyOneCharge()
        {
            for (int number = 1; number <= 8; number++) ui.Progress.CompleteLevel(number, 3);
            game.LoadLevel(1);
            yield return null;
            var tuning = Field<PackingTuning>(game, "tuning");
            tuning.autoPackFlySeconds = 0f;
            tuning.autoPackNodesPerFrame = tuning.autoPackNodeLimit;
            int before = ui.Progress.BoosterCount(BoosterType.AutoPack);
            Assert.That(before, Is.GreaterThan(0));
            int completions = 0;
            game.AutoPackFinished += success => { if (success) completions++; };

            typeof(GameUI).GetMethod("OnBoosterClicked", PrivateInstance).Invoke(ui, new object[] { BoosterType.AutoPack });
            yield return null;

            Assert.That(completions, Is.EqualTo(1));
            Assert.That(ui.Progress.BoosterCount(BoosterType.AutoPack), Is.EqualTo(before - 1));
            Assert.That(Field<bool>(ui, "pendingAutoPack"), Is.False);
        }

        [UnityTest]
        public IEnumerator ConfigureSaveStore_AfterInitialization_RejectsReplacement()
        {
            Assert.Throws<InvalidOperationException>(() => ui.ConfigureSaveStore(new MemorySave()));
            yield return null;
        }

        [UnityTest]
        public IEnumerator MobileScreens_ResolutionMatrix_RetainsIsolatedRuntimeCaptures()
        {
            int[,] sizes = { {480,800}, {720,1280}, {1080,2400}, {1440,2960}, {1536,2048}, {800,480} };
            for (int index = 0; index < sizes.GetLength(0); index++)
            {
                int width = sizes[index,0], height = sizes[index,1];
                SetGameViewSize(width, height);
                for (int frame = 0; frame < 8; frame++) yield return null;
                Assert.That(Screen.width, Is.EqualTo(width));
                Assert.That(Screen.height, Is.EqualTo(height));
                string prefix = $"{width}x{height}";
                UIState("Main");
                yield return Capture(prefix + "-main");
                UIState("LevelSelect");
                yield return Capture(prefix + "-levels");
                game.LoadLevel(20);
                yield return Capture(prefix + "-level20");
                typeof(GameUI).GetMethod("OpenPause", PrivateInstance).Invoke(ui, null);
                yield return Capture(prefix + "-pause");
                ui.Progress.SetUIScalePercent(150);
                typeof(GameUI).GetMethod("ApplyPreferences", PrivateInstance).Invoke(ui, null);
                typeof(GameUI).GetMethod("ClosePause", PrivateInstance).Invoke(ui, null);
                yield return Capture(prefix + "-level20-large");
                typeof(GameUI).GetMethod("OnTimeUp", PrivateInstance).Invoke(ui, null);
                yield return Capture(prefix + "-lose-large");
                game.LoadLevel(1);
                yield return null;
                for (int piece = 0; piece < game.CurrentLevel.SolutionShapes.Length; piece++)
                {
                    RotateToReference(piece, game.CurrentLevel.SolutionShapes[piece]);
                    DragTo(piece, game.CurrentLevel.SolutionOrigins[piece]);
                }
                Assert.That(game.IsWon, Is.True);
                yield return new WaitForSecondsRealtime(1.5f);
                yield return Capture(prefix + "-win-large");
                ui.Progress.SetUIScalePercent(100);
                typeof(GameUI).GetMethod("ApplyPreferences", PrivateInstance).Invoke(ui, null);
            }
        }

        [UnityTest]
        public IEnumerator FirstWin_LargeUI_KeepsContinueButtonInsideViewport()
        {
            SetGameViewSize(480, 800);
            for (int frame = 0; frame < 8; frame++) yield return null;
            ui.Progress.SetUIScalePercent(150);
            typeof(GameUI).GetMethod("ApplyPreferences", PrivateInstance).Invoke(ui, null);
            game.LoadLevel(1);
            yield return null;
            for (int piece = 0; piece < game.CurrentLevel.SolutionShapes.Length; piece++)
            {
                RotateToReference(piece, game.CurrentLevel.SolutionShapes[piece]);
                DragTo(piece, game.CurrentLevel.SolutionOrigins[piece]);
            }
            yield return new WaitForSecondsRealtime(1.5f);
            yield return Capture("first-win-large-continue");
            var button = Field<WinScreen>(ui, "winScreen").PrimaryButton.Root;
            var corners = new Vector3[4];
            button.GetWorldCorners(corners);
            foreach (var point in corners)
            {
                Assert.That(point.y, Is.InRange(0f, (float)Screen.height), "Continue must fit on screen.");
                foreach (var mask in button.GetComponentsInParent<UnityEngine.UI.RectMask2D>())
                    Assert.That(mask.rectTransform.rect.Contains(mask.rectTransform.InverseTransformPoint(point)),
                        Is.True, "Continue must not be clipped behind reward overflow.");
            }
        }

        [UnityTest]
        public IEnumerator SafeArea_ChangesWithoutResolution_RefitsBoardAndHUD()
        {
            SetGameViewSize(480, 800);
            for (int frame = 0; frame < 8; frame++) yield return null;
            ui.Progress.SetUIScalePercent(150);
            typeof(GameUI).GetMethod("ApplyPreferences", PrivateInstance).Invoke(ui, null);
            game.LoadLevel(20);
            Rect safe = new Rect(0, 32, 480, 720);
            ui.SafeAreaProvider = () => safe;
            yield return Capture("safearea-top-bottom-large");
            var firstPosition = camera.transform.position;
            safe = new Rect(110, 32, 370, 720);
            yield return Capture("safearea-changed-left-large");
            Assert.That(camera.transform.position.x, Is.Not.EqualTo(firstPosition.x),
                "A changed horizontal safe area must refit even at unchanged resolution.");
            var bounds = Field<GridView>(game, "grid").ContentRect;
            foreach (var corner in new[] { bounds.min, bounds.max })
            {
                Vector2 screen = camera.WorldToScreenPoint(corner);
                Assert.That(safe.Contains(screen), Is.True, "Board frame must stay inside simulated safe area.");
            }
            var dock = Field<RectTransform>(Field<HudView>(ui, "hud"), "dock");
            var dockCorners = new Vector3[4];
            foreach (RectTransform slot in dock)
            {
                if (!slot.gameObject.activeSelf) continue;
                slot.GetWorldCorners(dockCorners);
                foreach (var corner in dockCorners)
                    Assert.That(safe.Contains(corner), Is.True, "Every dock slot must remain inside safe area.");
            }
            var factory = Field<UIFactory>(ui, "factory");
            foreach (var rect in factory.SafeRects)
            {
                Assert.That(rect.anchorMin.x, Is.EqualTo(safe.xMin / Screen.width).Within(0.001f));
                Assert.That(rect.anchorMax.y, Is.EqualTo(safe.yMax / Screen.height).Within(0.001f));
            }
        }

        void UIState(string state)
        {
            var type = typeof(GameUI).GetNestedType("UIState", BindingFlags.NonPublic);
            typeof(GameUI).GetMethod("ShowState", PrivateInstance).Invoke(ui, new[] { Enum.Parse(type, state) });
        }

        void SetGameViewSize(int width, int height)
        {
#if UNITY_EDITOR
            var assembly = typeof(UnityEditor.Editor).Assembly;
            var type = assembly.GetType("UnityEditor.GameView");
            gameView = UnityEditor.EditorWindow.GetWindow(type);
            var selection = type.GetProperty("selectedSizeIndex", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (originalGameViewSize < 0) originalGameViewSize = (int)selection.GetValue(gameView);
            var sizesType = assembly.GetType("UnityEditor.GameViewSizes");
            var singleton = typeof(UnityEditor.ScriptableSingleton<>).MakeGenericType(sizesType);
            var sizes = singleton.GetProperty("instance").GetValue(null);
            var groupType = assembly.GetType("UnityEditor.GameViewSizeGroupType");
            var currentGroup = sizesType.GetProperty("currentGroupType", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(sizes);
            var group = sizesType.GetMethod("GetGroup").Invoke(sizes, new[] { currentGroup });
            var sizeType = assembly.GetType("UnityEditor.GameViewSize");
            var modeType = assembly.GetType("UnityEditor.GameViewSizeType");
            int count = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null);
            int chosen = -1;
            for (int i = 0; i < count; i++)
            {
                var size = group.GetType().GetMethod("GetGameViewSize").Invoke(group,new object[]{i});
                if ((int)sizeType.GetProperty("width").GetValue(size)==width &&
                    (int)sizeType.GetProperty("height").GetValue(size)==height) { chosen=i; break; }
            }
            if (chosen < 0)
            {
                var size = Activator.CreateInstance(sizeType, new[] { Enum.Parse(modeType,"FixedResolution"), (object)width, height, "QA mobile" });
                group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{size});
                chosen=count;
            }
            selection.SetValue(gameView,chosen);
            gameView.Repaint();
#else
            Screen.SetResolution(width,height,false);
#endif
        }

        static IEnumerator Capture(string name)
        {
            for (int frame = 0; frame < 4; frame++) yield return null;
            yield return new WaitForEndOfFrame();
            const string directory = "production/qa/evidence/mobile-puzzle";
            Directory.CreateDirectory(directory);
            var image = ScreenCapture.CaptureScreenshotAsTexture();
            Assert.That(image, Is.Not.Null);
            File.WriteAllBytes(Path.Combine(directory, name + ".png"), image.EncodeToPNG());
            UnityEngine.Object.Destroy(image);
        }

        void RotateToReference(int piece, PieceShape reference)
        {
            for (int turn = 0; turn < 4; turn++)
            {
                var shapes = Field<PieceShape[]>(game, "shapes");
                if (shapes[piece].SameCells(reference)) return;
                Vector2 screen = TrayGrabScreen(piece);
                Invoke("BeginPress", screen);
                Invoke("EndPress");
            }
            Assert.That(Field<PieceShape[]>(game, "shapes")[piece].SameCells(reference), Is.True);
        }

        Vector2 TrayGrabScreen(int piece)
        {
            var shape = Field<PieceShape[]>(game, "shapes")[piece];
            var view = Field<PieceView[]>(game, "pieceViews")[piece];
            var first = shape.FirstCell;
            Vector3 world = view.Root.position + new Vector3(first.x + 0.5f, -first.y - 0.5f, 0f) * view.Scale;
            return camera.WorldToScreenPoint(world);
        }

        void DragTo(int piece, Vector2Int origin)
        {
            var shape = Field<PieceShape[]>(game, "shapes")[piece];
            var grid = Field<GridView>(game, "grid");
            Vector2 start = TrayGrabScreen(piece);
            Vector2 grabOffset = new Vector2(shape.FirstCell.x + 0.5f, -shape.FirstCell.y - 0.5f);
            Vector2 destination = camera.WorldToScreenPoint((Vector2)grid.CellTopLeft(origin) + grabOffset);
            Invoke("BeginPress", start);
            Invoke("MovePress", destination);
            Invoke("EndPress");
        }

        void Invoke(string method, params object[] arguments)
        {
            var info = typeof(PackingGame).GetMethod(method, PrivateInstance);
            Assert.That(info, Is.Not.Null, $"Controller test adapter missing {method}.");
            info.Invoke(game, arguments);
        }

        static T Field<T>(object target, string name) => (T)target.GetType().GetField(name, PrivateInstance).GetValue(target);
        static void SetField(object target, string name, object value) => target.GetType().GetField(name, PrivateInstance).SetValue(target, value);
    }
}
