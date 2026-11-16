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
    /// <summary>Rendered-size regression and isolated before/after evidence for the mobile readability pass.</summary>
    public sealed class MobileReadabilityTests
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        const string Evidence = "production/qa/evidence/sourced-polish";
        GameObject root;
        GameObject cameraRoot;
        TextAsset catalog;
        PackingGame game;
        GameUI ui;
        Camera camera;
#if UNITY_EDITOR
        UnityEditor.EditorWindow gameView;
        int originalSize = -1;
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
            cameraRoot = new GameObject("Readability Camera");
            camera = cameraRoot.AddComponent<Camera>();
            cameraRoot.tag = "MainCamera";
            camera.orthographic = true;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            root = new GameObject("Readability Isolated Session");
            root.SetActive(false);
            catalog = new TextAsset(File.ReadAllText(Path.Combine(Application.dataPath, "Data/Levels/levels.json")));
            game = root.AddComponent<PackingGame>();
            Field(game, "levelsJson").SetValue(game, catalog);
            ui = root.AddComponent<GameUI>();
            var save = new MemorySave();
            save.Save(new ProgressData { muted = true, reduceMotion = true });
            ui.ConfigureSaveStore(save);
            root.AddComponent<GameFeel>();
            root.SetActive(true);
            yield return null;
            SetSize(480, 800);
            for (int i = 0; i < 8; i++) yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
#if UNITY_EDITOR
            if (gameView != null && originalSize >= 0)
                gameView.GetType().GetProperty("selectedSizeIndex", Private | BindingFlags.Public).SetValue(gameView, originalSize);
#endif
            UnityEngine.Object.Destroy(root);
            UnityEngine.Object.Destroy(cameraRoot);
            UnityEngine.Object.Destroy(catalog);
            yield return null;
            var events = UnityEngine.Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (events != null) UnityEngine.Object.Destroy(events.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CrowdedLevels_ReadabilityBaseline_RetainsRenderedMetrics()
        {
            // The retained baseline is immutable; every current run writes into after/.
            const string stage = "after";
            foreach (var size in new[] { new Vector2Int(480, 800), new Vector2Int(1080, 2400) })
            {
                SetSize(size.x, size.y);
                for (int frame = 0; frame < 8; frame++) yield return null;
                foreach (int number in new[] { 1, 15, 20 })
                {
                    game.LoadLevel(number);
                    for (int frame = 0; frame < 8; frame++) yield return null;
                    yield return Capture($"{stage}/{size.x}x{size.y}-level{number:00}");
                    string metrics = $"level={number}; screen={Screen.width}x{Screen.height}; " +
                        $"cellPixels={Screen.height / (2f * camera.orthographicSize):F3}; " +
                        $"cameraSize={camera.orthographicSize:F4}\n";
                    Directory.CreateDirectory(Path.Combine(Evidence, stage));
                    File.WriteAllText(Path.Combine(Evidence, stage, $"{size.x}x{size.y}-level{number:00}.txt"), metrics);
                }
            }
        }

        [UnityTest]
        public IEnumerator UnlockedDock_SmallPortrait_NameAndCountMeetRenderedFontFloor()
        {
            // Arrange: activate boosters through legitimate isolated progression, never through production saves.
            for (int n = 1; n <= 7; n++) ui.Progress.CompleteLevel(n, 3);
            game.LoadLevel(RichestLevel());
            for (int frame = 0; frame < 8; frame++) yield return null;
            yield return Capture("after/unlocked-dock");
            var dockCanvas = (GameObject)Field(ui, "canvasObject").GetValue(ui);
            Assert.That(Find(dockCanvas.transform, "HUD Freeze").gameObject.activeInHierarchy, Is.False,
                "No level has a timer, so Freeze stays out of the dock.");
            foreach (var name in new[] { "HUD Hint", "HUD AutoPack" })
            {
                var uiCanvas = dockCanvas;
                var slot = Find(uiCanvas.transform, name);
                Assert.That(slot, Is.Not.Null, name);
                int checkedLabels = 0;
                foreach (var label in slot.GetComponentsInChildren<Component>(false))
                {
                    var type = label.GetType();
                    if (type.FullName != "TMPro.TextMeshProUGUI" || (label.name != "Label" && label.name != "Count")) continue;
                    type.GetMethod("ForceMeshUpdate").Invoke(label, new object[] { false, false });
                    float fontSize = (float)type.GetProperty("fontSize").GetValue(label);
                    var canvas = label.GetComponentInParent<Canvas>().rootCanvas;
                    float floor = label.name == "Count" ? 20f : 18f;
                    Assert.That(fontSize * canvas.scaleFactor, Is.GreaterThanOrEqualTo(floor),
                        $"{name}/{label.name}: readable rendered font must not shrink below {floor}px at 480x800.");
                    checkedLabels++;
                }
                Assert.That(checkedLabels, Is.EqualTo(2), "Check both the action label and count, not an empty search.");
            }
        }

        [UnityTest]
        public IEnumerator LevelIntro_BannerStaysInsideReservedHud()
        {
            // Arrange/act: loading a level that introduces a rule triggers the actual banner.
            game.LoadLevel(FirstLevelWithBanner());
            for (int i = 0; i < 8; i++) yield return null;
            var hud = Field(ui, "hud").GetValue(ui);
            var banner = Field(ui, "banner").GetValue(ui);
            var card = (RectTransform)Field(banner, "card").GetValue(banner);
            float band = (float)hud.GetType().GetProperty("TopBandHeight").GetValue(hud);
            var host = (RectTransform)hud.GetType().GetProperty("NoticeHost").GetValue(hud);
            var corners = new Vector3[4];
            card.GetWorldCorners(corners);
            foreach (var point in corners)
            {
                var local = host.InverseTransformPoint(point);
                Assert.That(local.x, Is.InRange(host.rect.xMin - 0.1f, host.rect.xMax + 0.1f));
                Assert.That(local.y, Is.InRange(host.rect.yMin - 0.1f, host.rect.yMax + 0.1f));
            }
            Assert.That(band, Is.GreaterThan(0f));
            yield return new WaitForSecondsRealtime(1.8f);
            Assert.That(card.gameObject.activeSelf, Is.False);
            Assert.That(Find(host, "Rule Chips").gameObject.activeInHierarchy, Is.True,
                "The ordinary info row returns after the notice.");
        }

        [UnityTest]
        public IEnumerator RulesButton_PausesWithFullCurrentLevelExplanations()
        {
            int number = RichestLevel();
            var level = Levels().levels[number - 1];
            game.LoadLevel(number);
            yield return null;
            var canvas = (GameObject)Field(ui, "canvasObject").GetValue(ui);
            Find(canvas.transform, "HUD Rules").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;
            var rules = Find(canvas.transform, "Level Rules");
            Assert.That(rules, Is.Not.Null, "Rules must explain the current level, not just open settings.");
            Assert.That(rules.gameObject.activeInHierarchy, Is.True);
            var label = rules.GetComponent("TextMeshProUGUI");
            string text = (string)label.GetType().GetProperty("text").GetValue(label);
            int explained = 0;
            foreach (var expected in RuleLines(level))
            {
                StringAssert.Contains(expected, text);
                explained++;
            }
            Assert.That(explained, Is.GreaterThanOrEqualTo(2), "Use a level with several rules, not an empty check.");
            Assert.That(game.InputLocked, Is.True);
            yield return Capture("after/rules-level20");
        }

        [UnityTest]
        public IEnumerator SourcedAudio_PlaysImportedClipsAndSurvivesSessionDisposal()
        {
            var host = new GameObject("Audio lifetime check");
            var bank = new GameAudio(host.transform, new AudioTuning());
            var clips = new System.Collections.Generic.List<AudioClip>();
            try
            {
                foreach (var sound in new[] { GameSound.Pickup, GameSound.Place, GameSound.Rotate,
                    GameSound.Return, GameSound.Undo, GameSound.UI, GameSound.Locked })
                {
                    bank.Play(sound);
                    var expected = Resources.Load<AudioClip>("Audio/Sourced/" + sound);
                    Assert.That(expected, Is.Not.Null);
                    var voices = host.GetComponentsInChildren<AudioSource>();
                    Assert.That(Array.Exists(voices, v => v.clip == expected), Is.True, sound.ToString());
                    clips.Add(expected);
                }
                bank.Muted = true;
                foreach (var voice in host.GetComponentsInChildren<AudioSource>()) Assert.That(voice.isPlaying, Is.False);
            }
            finally { bank.Dispose(); UnityEngine.Object.Destroy(host); }
            yield return null;
            foreach (var clip in clips) Assert.That(clip != null, Is.True, "Imported clips are shared assets.");
        }

        [UnityTest]
        public IEnumerator ParticleFeedback_UsesSourcedTexturesAndClearsForReducedMotion()
        {
            var feel = root.GetComponent<GameFeel>();
            var burst = (ConfettiBurst)Field(feel, "_burst").GetValue(feel);
            foreach (var entry in new[] { ("_confetti", "star"), ("_sparkle", "spark"), ("_puff", "puff") })
            {
                var system = (ParticleSystem)Field(burst, entry.Item1).GetValue(burst);
                var sprite = Resources.Load<Sprite>("Art/Sourced/VFX/" + entry.Item2);
                Assert.That(sprite, Is.Not.Null);
                Assert.That(system.GetComponent<ParticleSystemRenderer>().sharedMaterial.mainTexture,
                    Is.SameAs(sprite.texture), entry.Item2);
            }
            burst.Sparkle(Vector3.zero);
            burst.Dust(Vector3.zero);
            burst.Celebrate(Vector3.zero);
            feel.ReduceMotion = true;
            yield return null;
            foreach (var system in root.GetComponentsInChildren<ParticleSystem>())
                Assert.That(system.particleCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator SourcedPieces_RuleMarkersDoNotCoverIllustrations()
        {
            foreach (int level in new[] { 15, 20 })
            {
                game.LoadLevel(level);
                for (int frame = 0; frame < 8; frame++) yield return null;
                int checkedMarkers = 0;
                foreach (var illustration in root.GetComponentsInChildren<SpriteRenderer>())
                {
                    if (illustration.name != "Sourced object") continue;
                    foreach (var marker in illustration.transform.parent.GetComponentsInChildren<SpriteRenderer>())
                    {
                        if (!marker.name.EndsWith("Badge") || !marker.gameObject.activeInHierarchy) continue;
                        Assert.That(illustration.bounds.Intersects(marker.bounds), Is.False,
                            $"Level {level}: {illustration.transform.parent.name}/{marker.name} covers the item.");
                        checkedMarkers++;
                    }
                }
                Assert.That(checkedMarkers, Is.GreaterThan(0));
            }
        }

        [UnityTest]
        public IEnumerator MissingSourcedSprite_RetainsLegacyIllustrationAndOrientation()
        {
            var factory = new SpriteFactory();
            try
            {
                // Simulate a failed optional resource lookup without modifying any imported asset.
                var cache = (System.Collections.Generic.Dictionary<string, Sprite>)Field(factory, "art").GetValue(factory);
                cache["Sourced/Items/socks"] = null;
                var art = factory.GetPieceArt(1, "A");
                Assert.That(art, Is.Not.Null);
                Assert.That(art.Sprite, Is.SameAs(Resources.Load<Sprite>("Art/Items/sock_long")));
                Assert.That(art.OnTile, Is.False);
                Assert.That(art.SolutionTurns, Is.EqualTo(2));
                Assert.That(art.Mirrored, Is.True);
            }
            finally { factory.Dispose(); }
            yield return null;
        }

        [UnityTest]
        public IEnumerator RulesReopenedAfterSettingsScroll_StartsAtExplanations()
        {
            SetSize(800, 480);
            ui.Progress.SetUIScalePercent(150);
            typeof(GameUI).GetMethod("ApplyPreferences", Private).Invoke(ui, null);
            game.LoadLevel(RichestLevel());
            for (int frame = 0; frame < 8; frame++) yield return null;
            var canvas = (GameObject)Field(ui, "canvasObject").GetValue(ui);
            var rules = Find(canvas.transform, "HUD Rules").GetComponent<UnityEngine.UI.Button>();
            rules.onClick.Invoke();
            for (int frame = 0; frame < 8; frame++) yield return null;
            var text = (RectTransform)Find(canvas.transform, "Level Rules");
            var scroll = text.GetComponentInParent<UnityEngine.UI.ScrollRect>();
            Assert.That(scroll, Is.Not.Null);
            scroll.verticalNormalizedPosition = 0f;
            Assert.That(scroll.content.anchoredPosition.y, Is.GreaterThan(100f),
                "The test must actually scroll away from the explanations before reopening.");
            typeof(GameUI).GetMethod("ClosePause", Private).Invoke(ui, null);
            rules.onClick.Invoke();
            for (int frame = 0; frame < 8; frame++) yield return null;
            var corners = new Vector3[4];
            text.GetWorldCorners(corners);
            var top = scroll.viewport.InverseTransformPoint(corners[1]);
            Assert.That(top.y, Is.InRange(scroll.viewport.rect.yMin, scroll.viewport.rect.yMax),
                "Opening Rules must reveal its first line even after settings were scrolled.");
            yield return Capture("after/rules-landscape-reopened");
        }

        LevelsFile Levels() => JsonUtility.FromJson<LevelsFile>(catalog.text);

        /// <summary>The level with the most distinct rules (first one on ties), found in the data, not hard-coded.</summary>
        int RichestLevel()
        {
            int best = 1, most = -1;
            foreach (var level in Levels().levels)
            {
                int count = RuleLines(level).Count;
                if (count > most)
                {
                    best = level.number;
                    most = count;
                }
            }
            return best;
        }

        int FirstLevelWithBanner()
        {
            foreach (var level in Levels().levels)
                if (!string.IsNullOrEmpty(level.bannerTitle)) return level.number;
            Assert.Fail("No level introduces a rule with a banner.");
            return 1;
        }

        /// <summary>Words the pause screen must show for each rule the level uses.</summary>
        static System.Collections.Generic.List<string> RuleLines(LevelData level)
        {
            bool heavy = false, upright = false, paired = false, fragile = false, zoned = false;
            foreach (var piece in level.pieces)
            {
                heavy |= piece.heavy; upright |= piece.upright; paired |= piece.pair >= 0;
                fragile |= piece.fragile; zoned |= !string.IsNullOrEmpty(piece.zone);
            }
            var lines = new System.Collections.Generic.List<string>();
            if (zoned) lines.Add("zone");
            if (heavy) lines.Add("bottom");
            if (upright) lines.Add("cannot be turned");
            if (paired) lines.Add("touch");
            if (fragile) lines.Add("Fragile");
            return lines;
        }

        static FieldInfo Field(object target, string name) => target.GetType().GetField(name, Private);
        static Transform Find(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                var found = Find(parent.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        static IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            string file = Path.Combine(Evidence, name + ".png");
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            var image = ScreenCapture.CaptureScreenshotAsTexture();
            Assert.That(image, Is.Not.Null);
            File.WriteAllBytes(file, image.EncodeToPNG());
            UnityEngine.Object.Destroy(image);
        }

        void SetSize(int width, int height)
        {
#if UNITY_EDITOR
            var assembly = typeof(UnityEditor.Editor).Assembly;
            var type = assembly.GetType("UnityEditor.GameView");
            gameView = UnityEditor.EditorWindow.GetWindow(type);
            var selection = type.GetProperty("selectedSizeIndex", Private | BindingFlags.Public);
            if (originalSize < 0) originalSize = (int)selection.GetValue(gameView);
            var sizesType = assembly.GetType("UnityEditor.GameViewSizes");
            var singleton = typeof(UnityEditor.ScriptableSingleton<>).MakeGenericType(sizesType);
            var sizes = singleton.GetProperty("instance").GetValue(null);
            var groupType = sizesType.GetProperty("currentGroupType", Private | BindingFlags.Public).GetValue(sizes);
            var group = sizesType.GetMethod("GetGroup").Invoke(sizes, new[] { groupType });
            var sizeType = assembly.GetType("UnityEditor.GameViewSize");
            int count = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null);
            int chosen = -1;
            for (int i = 0; i < count; i++)
            {
                var size = group.GetType().GetMethod("GetGameViewSize").Invoke(group, new object[] { i });
                if ((int)sizeType.GetProperty("width").GetValue(size) == width && (int)sizeType.GetProperty("height").GetValue(size) == height)
                { chosen = i; break; }
            }
            if (chosen < 0)
            {
                var modeType = assembly.GetType("UnityEditor.GameViewSizeType");
                var size = Activator.CreateInstance(sizeType, new[] { Enum.Parse(modeType, "FixedResolution"), (object)width, height, "Readability QA" });
                group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size });
                chosen = count;
            }
            selection.SetValue(gameView, chosen);
            gameView.Repaint();
#else
            Screen.SetResolution(width, height, false);
#endif
        }
    }
}
