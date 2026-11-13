// Screen-space UI entry point: builds the UGUI canvas at runtime and runs the screen flow
// (main → level select → playing → won / lost), the level timer and saved progress.
// Implements: design/game-brief.md "Điều kiện thắng / thua", "Cơ chế chính" (Hẹn giờ, Khuôn gợi ý banner), "Booster",
// "Progression & Meta", "Quyết định đã chốt".
// Stories: production/epics/pack-it-perfect/story-006-timer-mode.md, story-007-progression-meta.md
// Scene setup: add this component to the "PackingGame" object. Canvas, EventSystem and sprites are built at runtime.

using System;
using System.Collections.Generic;
using PackItPerfect.Meta;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace PackItPerfect.UI
{
    /// <summary>
    /// Owns the UI of the game: main screen, level select, HUD, banner, timer, win and lose screens.
    /// The UI only displays state and forwards player choices to <see cref="PackingGame"/> and <see cref="ProgressService"/>;
    /// it does not implement packing rules. Hooks for the audio wave: <see cref="Timer"/>.WarningTick (clock tick),
    /// <see cref="ButtonClicked"/> (UI clicks); for a store link: <see cref="InstallClicked"/>.
    /// </summary>
    [RequireComponent(typeof(PackingGame))]
    [DisallowMultipleComponent]
    public sealed class GameUI : MonoBehaviour
    {
        /// <summary>Screen flow (one screen visible at a time; HUD stays under Won/Lost overlays).
        /// Main ⇄ LevelSelect; Main/LevelSelect → Playing (a level loads; any level load enters Playing);
        /// Playing → Won (LevelWon) | Lost (timer expired) | Main (home);
        /// Won → Playing (next, replay) | LevelSelect | Main; Lost → Playing (try again) | LevelSelect.
        /// Back key: Playing → Main, LevelSelect → Main, Won/Lost → LevelSelect.</summary>
        enum UIState { Main, LevelSelect, Playing, Won, Lost }

        const float INSET_EPSILON = 0.001f;

        [SerializeField] UITuning tuning = new UITuning();

        PackingGame game;
        LevelTimer timer;
        UIAnimator animator;
        UIFactory factory;
        ProgressService progress;
        GameObject canvasObject;
        RectTransform canvasRect;

        MainScreen mainScreen;
        LevelSelectScreen levelSelect;
        HudView hud;
        BannerView banner;
        WinScreen winScreen;
        LoseScreen loseScreen;

        UIState state;
        bool built;
        Rect appliedSafeArea;
        Vector2Int appliedScreen;
        float appliedTopInset = -1f;
        float appliedBottomInset = -1f;

        PointerEventData pointerData;
        readonly List<RaycastResult> raycastHits = new List<RaycastResult>();

        /// <summary>"Install to keep packing" was clicked. This build opens no link (decision 2026-11-12); attach one here.</summary>
        public event Action InstallClicked;

        /// <summary>Any UI button was clicked (its id, e.g. "HUD Hint"). Audio hook for click sounds.</summary>
        public event Action<string> ButtonClicked;

        /// <summary>The level countdown. Subscribe to <see cref="LevelTimer.WarningTick"/> to play the clock tick.</summary>
        public LevelTimer Timer => timer;

        /// <summary>Saved progress (null until Start).</summary>
        public ProgressService Progress => progress;

        void Awake()
        {
            game = GetComponent<PackingGame>();
            game.AutoAdvanceOnWin = false;
            game.LoadOnStart = false;
            game.PointerBlocked = IsPointerOverUI;
            game.LevelLoaded += OnLevelLoaded;
            game.FillChanged += OnFillChanged;
            game.LevelWon += OnLevelWon;
            game.LevelCompleted += OnLevelCompleted;

            timer = new LevelTimer(tuning.timerWarningSeconds);
            timer.SecondTick += OnTimerSecond;
            timer.Expired += OnTimeUp;
            animator = new UIAnimator();
        }

        void Start()
        {
            // PackingGame.Awake has parsed levels.json by now (all Awakes run before any Start).
            progress = new ProgressService(new JsonSaveStore(JsonSaveStore.DefaultPath), game.LevelCount);
            EnsureEventSystem();
            BuildCanvas();
            built = true;
            ApplyPreferences();
            ShowState(UIState.Main);
        }

        void OnDestroy()
        {
            if (game != null)
            {
                game.LevelLoaded -= OnLevelLoaded;
                game.FillChanged -= OnFillChanged;
                game.LevelWon -= OnLevelWon;
                game.LevelCompleted -= OnLevelCompleted;
                game.PointerBlocked = null;
            }
            if (factory != null)
            {
                factory.ButtonClicked -= ForwardButtonClicked;
                factory.Dispose();
            }
            if (canvasObject != null)
            {
                Destroy(canvasObject);
            }
        }

        void Update()
        {
            if (!built)
            {
                return;
            }
            float deltaTime = Time.unscaledDeltaTime;
            if (state == UIState.Playing)
            {
                // IsWon turns true on the winning drop, before the lid animation and LevelWon: stop the clock there.
                if (game.IsWon)
                {
                    timer.Stop();
                }
                timer.Tick(deltaTime);
            }
            animator.Tick(deltaTime);
            UpdateSafeArea();
            UpdateViewInsets();
            HandleBack();
        }

        // ---------------------------------------------------------------- build

        void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            var module = go.AddComponent<InputSystemUIInputModule>();
            if (module.actionsAsset == null)
            {
                module.AssignDefaultActions();
            }
        }

        void BuildCanvas()
        {
            canvasObject = new GameObject("GameUI Canvas", typeof(RectTransform));
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = tuning.canvasSortingOrder;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = tuning.referenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = tuning.matchWidthOrHeight;
            canvasObject.AddComponent<GraphicRaycaster>();
            canvasRect = (RectTransform)canvasObject.transform;

            factory = new UIFactory(tuning);
            factory.ButtonClicked += ForwardButtonClicked;

            // Sibling order is draw order: HUD, banner, overlays, then the opaque menus on top.
            hud = new HudView(factory, canvasRect, tuning);
            hud.HomeClicked += () => ShowState(UIState.Main);
            hud.HintClicked += game.RequestHint;
            hud.RestartClicked += game.RestartLevel;

            banner = new BannerView(factory, canvasRect, tuning, animator);

            winScreen = new WinScreen(factory, canvasRect, tuning, animator, ProgressService.MAX_STARS);
            winScreen.NextClicked += OnNextClicked;
            winScreen.ReplayClicked += game.RestartLevel;
            winScreen.LevelsClicked += () => ShowState(UIState.LevelSelect);
            winScreen.HomeClicked += () => ShowState(UIState.Main);

            loseScreen = new LoseScreen(factory, canvasRect, tuning, animator);
            loseScreen.TryAgainClicked += game.RestartLevel;
            loseScreen.InstallClicked += () => InstallClicked?.Invoke();
            loseScreen.LevelsClicked += () => ShowState(UIState.LevelSelect);

            levelSelect = new LevelSelectScreen(factory, canvasRect, tuning, game.LevelCount);
            levelSelect.LevelChosen += OnLevelChosen;
            levelSelect.HomeClicked += () => ShowState(UIState.Main);

            mainScreen = new MainScreen(factory, canvasRect, tuning);
            mainScreen.PlayClicked += OnPlayClicked;
            mainScreen.LevelsClicked += () => ShowState(UIState.LevelSelect);
            mainScreen.SoundClicked += () => SetPreferences(!progress.Muted, progress.ReduceMotion);
            mainScreen.MotionClicked += () => SetPreferences(progress.Muted, !progress.ReduceMotion);

            UpdateSafeArea();
        }

        // ---------------------------------------------------------------- flow

        void ShowState(UIState next)
        {
            state = next;
            bool inLevel = next == UIState.Playing || next == UIState.Won || next == UIState.Lost;
            mainScreen.SetVisible(next == UIState.Main);
            levelSelect.SetVisible(next == UIState.LevelSelect);
            hud.SetVisible(inLevel);
            hud.SetInteractable(next == UIState.Playing && !game.IsWon);
            loseScreen.SetVisible(next == UIState.Lost);
            if (next != UIState.Won)
            {
                winScreen.Hide();
            }
            if (!inLevel)
            {
                timer.Stop();
                banner.Hide();
            }
            if (next != UIState.Playing)
            {
                game.SetInputLocked(true);
            }

            switch (next)
            {
                case UIState.Main:
                    mainScreen.Refresh(progress);
                    Focus(mainScreen.PlayButton);
                    break;
                case UIState.LevelSelect:
                    levelSelect.Refresh(progress);
                    Focus(levelSelect.Cell(progress.NextLevelToPlay) ?? levelSelect.HomeButton);
                    break;
                case UIState.Playing:
                    Focus(hud.HintButton);
                    break;
                case UIState.Lost:
                    Focus(loseScreen.TryAgainButton);
                    break;
            }
        }

        void OnPlayClicked()
        {
            int level = progress.NextLevelToPlay;
            if (level >= 1)
            {
                game.LoadLevel(level);
            }
        }

        void OnLevelChosen(int level)
        {
            if (progress.IsUnlocked(level))
            {
                game.LoadLevel(level);
            }
        }

        void OnNextClicked()
        {
            int next = game.CurrentLevelNumber + 1;
            if (next <= game.LevelCount)
            {
                game.LoadLevel(next);
            }
            else
            {
                ShowState(UIState.LevelSelect);
            }
        }

        void OnLevelLoaded(LoadedLevel level)
        {
            if (!built)
            {
                return;
            }
            var info = level.Info;
            hud.Bind(info, game.LevelCount);
            hud.SetFill(0f);
            ShowState(UIState.Playing);

            bool timed = info != null && info.HasTimer;
            hud.Timer.SetVisible(timed);
            if (timed)
            {
                // Timer levels show only the "Hard level!" banner, never their own bannerTitle (story 006).
                timer.WarningSeconds = tuning.timerWarningSeconds;
                timer.Start(info.TimerSeconds, tuning.timerStartDelay);
                banner.Show(UIStrings.HARD_TITLE, UIStrings.HARD_SUBTITLE);
            }
            else
            {
                timer.Stop();
                if (info != null && !string.IsNullOrEmpty(info.BannerTitle))
                {
                    banner.Show(info.BannerTitle, info.BannerSubtitle);
                }
                else
                {
                    banner.Hide();
                }
            }
        }

        void OnFillChanged(float fill)
        {
            if (built)
            {
                hud.SetFill(fill);
            }
        }

        void OnLevelCompleted(int level, int stars)
        {
            if (!built) return;
            timer.Stop();
            progress.RecordWin(level, stars);
            hud.SetInteractable(false);
        }

        void OnLevelWon(int level, int stars)
        {
            // The result was saved on the final valid drop. A stale presentation must not reopen a menu.
            if (!built || state != UIState.Playing || game.CurrentLevelNumber != level)
            {
                return;
            }
            timer.Stop();
            ShowState(UIState.Won);
            winScreen.Show(stars, level >= game.LevelCount);
            Focus(winScreen.PrimaryButton);
        }

        void OnTimerSecond(int secondsLeft)
        {
            if (built)
            {
                hud.Timer.SetSeconds(secondsLeft);
            }
        }

        void OnTimeUp()
        {
            if (state != UIState.Playing || game.IsWon)
            {
                return;
            }
            game.SetInputLocked(true);
            ShowState(UIState.Lost);
        }

        void HandleBack()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame)
            {
                return;
            }
            switch (state)
            {
                case UIState.LevelSelect:
                case UIState.Playing:
                    ShowState(UIState.Main);
                    break;
                case UIState.Won:
                case UIState.Lost:
                    ShowState(UIState.LevelSelect);
                    break;
            }
        }

        /// <summary>Saves and applies player audio and motion preferences.</summary>
        public void SetPreferences(bool muted, bool reduceMotion)
        {
            if (!built) return;
            progress.SetPreferences(muted, reduceMotion);
            ApplyPreferences();
            mainScreen.Refresh(progress);
        }

        void ApplyPreferences()
        {
            tuning.reduceMotion = progress.ReduceMotion;
            if (tuning.reduceMotion) animator.FinishAll();
            var feel = GetComponent<GameFeel>();
            if (feel != null)
            {
                feel.Muted = progress.Muted;
                feel.ReduceMotion = progress.ReduceMotion;
            }
        }

        void ForwardButtonClicked(string id) => ButtonClicked?.Invoke(id);

        // Keyboard and controller navigation both require an initial selected object.
        static void Focus(UIButton button)
        {
            if (button != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(button.Root.gameObject);
            }
        }

        // ---------------------------------------------------------------- layout

        void UpdateSafeArea()
        {
            var safe = Screen.safeArea;
            var screen = new Vector2Int(Screen.width, Screen.height);
            if (safe == appliedSafeArea && screen == appliedScreen || screen.x <= 0 || screen.y <= 0)
            {
                return;
            }
            appliedSafeArea = safe;
            appliedScreen = screen;
            var anchorMin = new Vector2(safe.xMin / screen.x, safe.yMin / screen.y);
            var anchorMax = new Vector2(safe.xMax / screen.x, safe.yMax / screen.y);
            foreach (var rect in factory.SafeRects)
            {
                rect.anchorMin = anchorMin;
                rect.anchorMax = anchorMax;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
        }

        // Tells the board how much of the screen height the HUD bands take, so it fits between them.
        void UpdateViewInsets()
        {
            float canvasHeight = canvasRect.rect.height;
            if (canvasHeight <= 0f || Screen.height <= 0)
            {
                return;
            }
            var safe = Screen.safeArea;
            float top = (Screen.height - safe.yMax) / Screen.height + hud.TopBandHeight / canvasHeight;
            float bottom = safe.yMin / Screen.height + hud.BottomBandHeight / canvasHeight;
            if (Mathf.Abs(top - appliedTopInset) < INSET_EPSILON && Mathf.Abs(bottom - appliedBottomInset) < INSET_EPSILON)
            {
                return;
            }
            appliedTopInset = top;
            appliedBottomInset = bottom;
            game.SetViewInsets(top, bottom);
        }

        // ---------------------------------------------------------------- input

        // PackingGame asks this at the start of each press: true when the pointer is over any raycast-target UI graphic.
        // Raycasting the pointer position works the same for mouse and touch with the Input System.
        bool IsPointerOverUI()
        {
            var eventSystem = EventSystem.current;
            var pointer = Pointer.current;
            if (eventSystem == null || pointer == null)
            {
                return false;
            }
            pointerData ??= new PointerEventData(eventSystem);
            pointerData.position = pointer.position.ReadValue();
            raycastHits.Clear();
            eventSystem.RaycastAll(pointerData, raycastHits);
            return raycastHits.Count > 0;
        }
    }
}
