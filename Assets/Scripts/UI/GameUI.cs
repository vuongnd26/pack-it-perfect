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
        /// Back key: Playing → opens / closes the pause overlay (a flag inside Playing), LevelSelect → Main,
        /// Won/Lost → LevelSelect.</summary>
        enum UIState { Main, LevelSelect, Playing, Won, Lost }

        const float INSET_EPSILON = 0.001f;

        [SerializeField] UITuning tuning = new UITuning();

        PackingGame game;
        LevelTimer timer;
        UIAnimator animator;
        UIFactory factory;
        ProgressService progress;
        ISaveStore saveStoreOverride;
        ProgressionRules rules;
        GameObject canvasObject;
        RectTransform canvasRect;

        MainScreen mainScreen;
        LevelSelectScreen levelSelect;
        HudView hud;
        BannerView banner;
        WinScreen winScreen;
        LoseScreen loseScreen;
        PauseScreen pauseScreen;

        UIState state;
        bool built;
        bool paused;
        bool pendingAutoPack;
        LevelReward lastReward;
        int lastRewardLevel;
        /// <summary>Optional platform safe-area source in screen pixels; null uses the device safe area.</summary>
        public Func<Rect> SafeAreaProvider { get; set; }

        Rect CurrentSafeArea => SafeAreaProvider != null ? SafeAreaProvider() : Screen.safeArea;

        Rect appliedSafeArea;
        Vector2Int appliedScreen;
        float appliedTopInset = -1f;
        float appliedBottomInset = -1f;
        float appliedLeftInset = -1f;
        float appliedRightInset = -1f;

        PointerEventData pointerData;
        readonly List<RaycastResult> raycastHits = new List<RaycastResult>();

        /// <summary>"Install to keep packing" was clicked. This build opens no link (decision 2026-11-12); attach one here.</summary>
        public event Action InstallClicked;

        /// <summary>Any UI button was clicked (its id, e.g. "HUD Hint"). Audio hook for click sounds.</summary>
        public event Action<string> ButtonClicked;

        /// <summary>A Hint, Auto Pack or Freeze was actually used. Audio hook for the booster sound.</summary>
        public event Action BoosterUsed;

        /// <summary>An interface moment worth a sound (star, reward, refusal, banner, freeze, loss) and its step.</summary>
        public event Action<UICue, int> Cue;

        readonly object mainFadeKey = new object();
        readonly object selectFadeKey = new object();
        readonly object overlayFadeKey = new object();
        bool lastInLevel;
        bool viewInsetsDirty = true;

        /// <summary>The level countdown. Subscribe to <see cref="LevelTimer.WarningTick"/> to play the clock tick.</summary>
        public LevelTimer Timer => timer;

        /// <summary>Saved progress (null until Start).</summary>
        public ProgressService Progress => progress;

        /// <summary>
        /// Supplies an isolated save store before Start. Used by tests and alternate hosts so initialization
        /// never reads, migrates or writes the production save. Cannot replace an initialized session's store.
        /// </summary>
        public void ConfigureSaveStore(ISaveStore store)
        {
            if (progress != null) throw new InvalidOperationException("Configure the save store before Start.");
            saveStoreOverride = store ?? throw new ArgumentNullException(nameof(store));
        }

        void Awake()
        {
            game = GetComponent<PackingGame>();
            game.AutoAdvanceOnWin = false;
            game.LoadOnStart = false;
            game.PointerBlocked = IsPointerOverUI;
            // Rule messages use the HUD line: it wraps and always fits the safe area on every screen size.
            game.ScreenToast = OnGameToast;
            game.LevelLoaded += OnLevelLoaded;
            game.FillChanged += OnFillChanged;
            game.HistoryChanged += OnHistoryChanged;
            game.AutoPackFinished += OnAutoPackFinished;
            game.LevelWon += OnLevelWon;
            game.LevelCompleted += OnLevelCompleted;

            timer = new LevelTimer(tuning.timerWarningSeconds);
            timer.SecondTick += OnTimerSecond;
            timer.Expired += OnTimeUp;
            timer.FreezeChanged += OnFreezeChanged;
            animator = new UIAnimator();
        }

        void Start()
        {
            // PackingGame.Awake has parsed levels.json by now (all Awakes run before any Start).
            rules = LoadRules();
            // The Cozy Travel build keeps its own save; the 15-level build's progress file is never written here.
            progress = new ProgressService(saveStoreOverride ?? new JsonSaveStore(JsonSaveStore.CozyPath), game.LevelCount, rules);
            progress.BoostersChanged += OnBoostersChanged;
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
                game.HistoryChanged -= OnHistoryChanged;
                game.AutoPackFinished -= OnAutoPackFinished;
                game.LevelWon -= OnLevelWon;
                game.LevelCompleted -= OnLevelCompleted;
                game.PointerBlocked = null;
                game.ScreenToast = null;
            }
            if (progress != null)
            {
                progress.BoostersChanged -= OnBoostersChanged;
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
                // The pause overlay stops the clock; it must not drain behind the card.
                if (!paused)
                {
                    timer.Tick(deltaTime);
                    hud.SetUndoUsable(game.CanUndo);
                    if (timer.IsFrozen)
                    {
                        hud.Timer.SetFrozen(true, Mathf.CeilToInt(timer.FrozenRemaining));
                    }
                }
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
            hud = new HudView(factory, canvasRect, tuning, animator);
            hud.PauseClicked += OpenPause;
            hud.UndoClicked += OnUndoClicked;
            hud.BoosterClicked += OnBoosterClicked;

            banner = new BannerView(factory, canvasRect, tuning, animator);

            pauseScreen = new PauseScreen(factory, canvasRect, tuning, animator);
            pauseScreen.ResumeClicked += ClosePause;
            pauseScreen.UISizeClicked += CycleUISize;
            pauseScreen.RestartClicked += OnPauseRestart;
            pauseScreen.HomeClicked += () => ShowState(UIState.Main);
            pauseScreen.SoundClicked += () => SetPreferences(!progress.Muted, progress.ReduceMotion);
            pauseScreen.MotionClicked += () => SetPreferences(progress.Muted, !progress.ReduceMotion);

            winScreen = new WinScreen(factory, canvasRect, tuning, animator, ProgressService.MAX_STARS);
            winScreen.NextClicked += OnNextClicked;
            winScreen.ReplayClicked += game.RestartLevel;
            winScreen.LevelsClicked += () => ShowState(UIState.LevelSelect);
            winScreen.HomeClicked += () => ShowState(UIState.Main);
            winScreen.StarShown += index => Cue?.Invoke(UICue.Star, index);
            winScreen.RewardShown += () => Cue?.Invoke(UICue.Reward, 0);

            loseScreen = new LoseScreen(factory, canvasRect, tuning, animator);
            loseScreen.TryAgainClicked += game.RestartLevel;
            loseScreen.InstallClicked += () => InstallClicked?.Invoke();
            loseScreen.LevelsClicked += () => ShowState(UIState.LevelSelect);

            levelSelect = new LevelSelectScreen(factory, canvasRect, tuning, game.LevelCount);
            levelSelect.LevelChosen += OnLevelChosen;
            levelSelect.HomeClicked += () => ShowState(UIState.Main);

            mainScreen = new MainScreen(factory, canvasRect, tuning);
            mainScreen.PlayClicked += OnPlayClicked;
            mainScreen.UISizeClicked += CycleUISize;
            mainScreen.LevelsClicked += () => ShowState(UIState.LevelSelect);
            mainScreen.SoundClicked += () => SetPreferences(!progress.Muted, progress.ReduceMotion);
            mainScreen.MotionClicked += () => SetPreferences(progress.Muted, !progress.ReduceMotion);

            UpdateSafeArea();
        }

        // ---------------------------------------------------------------- flow

        void ShowState(UIState next)
        {
            var previous = state;
            state = next;
            paused = false;
            pauseScreen.Hide();
            bool inLevel = next == UIState.Playing || next == UIState.Won || next == UIState.Lost;
            // The opaque menus cover the board completely: do not make the device draw it underneath.
            game.SetWorldVisible(inLevel);
            lastInLevel = inLevel;
            viewInsetsDirty = true;
            mainScreen.SetVisible(next == UIState.Main);
            levelSelect.SetVisible(next == UIState.LevelSelect);
            if (next != previous)
            {
                if (next == UIState.Main)
                    UIMotion.FadeIn(animator, mainFadeKey, mainScreen.Group, tuning.screenFade, tuning.reduceMotion);
                else if (next == UIState.LevelSelect)
                    UIMotion.FadeIn(animator, selectFadeKey, levelSelect.Group, tuning.screenFade, tuning.reduceMotion);
                else if (next == UIState.Lost)
                    UIMotion.FadeIn(animator, overlayFadeKey, loseScreen.Group, tuning.screenFade, tuning.reduceMotion);
            }
            hud.SetVisible(inLevel);
            hud.SetInteractable(next == UIState.Playing && !game.IsWon);
            hud.SetDockVisible(next == UIState.Playing);
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
                    Focus(hud.FirstFocus);
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
            hud.Bind(info);
            pendingAutoPack = false;
            ShowState(UIState.Playing);

            bool timed = info != null && info.HasTimer;
            hud.SetTimed(timed);
            hud.Timer.SetFrozen(false);
            hud.SetUndoUsable(game.CanUndo);
            hud.RefreshBoosters(progress, false);
            if (timed)
            {
                // Timer levels show only the "Hard level!" banner, never their own bannerTitle (story 006).
                timer.WarningSeconds = tuning.timerWarningSeconds;
                timer.Start(info.TimerSeconds, tuning.timerStartDelay);
                banner.Show(UIStrings.HARD_TITLE, UIStrings.HARD_SUBTITLE);
                Cue?.Invoke(UICue.Banner, 0);
            }
            else
            {
                timer.Stop();
                if (info != null && !string.IsNullOrEmpty(info.BannerTitle))
                {
                    banner.Show(info.BannerTitle, info.BannerSubtitle, isNew: true);
                    Cue?.Invoke(UICue.Banner, 0);
                }
                else
                {
                    banner.Hide();
                }
            }
        }

        void OnLevelCompleted(int level, int stars)
        {
            if (!built) return;
            timer.Stop();
            lastReward = progress.CompleteLevel(level, stars);
            lastRewardLevel = level;
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
            UIMotion.FadeIn(animator, overlayFadeKey, winScreen.Group, tuning.screenFade, tuning.reduceMotion);
            var reward = lastRewardLevel == level ? lastReward : null;
            winScreen.Show(stars, level >= game.LevelCount, game.CurrentLevel?.Info?.Name ?? string.Empty, reward,
                stars < ProgressService.MAX_STARS && game.AssistUsed, rules.NextMilestoneAfter(HighestPassed()),
                rules.FreezeSeconds);
            Focus(winScreen.PrimaryButton);
        }

        void OnTimerSecond(int secondsLeft)
        {
            if (built)
            {
                hud.Timer.SetSeconds(secondsLeft);
                if (secondsLeft > 0 && secondsLeft <= tuning.timerDangerSeconds && !timer.IsFrozen)
                {
                    hud.Timer.Pulse(animator);
                }
            }
        }

        void OnGameToast(string message)
        {
            if (built && lastInLevel)
            {
                hud.ShowToast(message);
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
            Cue?.Invoke(UICue.Lost, 0);
        }

        void OnApplicationPause(bool suspended)
        {
            if (suspended && built) OpenPause();
        }

        void OnApplicationFocus(bool focused)
        {
            if (!focused && built) OpenPause();
        }

        void HandleBack()
        {
            var keyboard = Keyboard.current;
            var gamepad = Gamepad.current;
            bool back = keyboard != null && keyboard.escapeKey.wasPressedThisFrame
                || gamepad != null && (gamepad.buttonEast.wasPressedThisFrame || gamepad.startButton.wasPressedThisFrame);
            if (!back)
            {
                return;
            }
            switch (state)
            {
                case UIState.LevelSelect:
                    ShowState(UIState.Main);
                    break;
                case UIState.Playing:
                    if (paused)
                    {
                        ClosePause();
                    }
                    else
                    {
                        OpenPause();
                    }
                    break;
                case UIState.Won:
                case UIState.Lost:
                    ShowState(UIState.LevelSelect);
                    break;
            }
        }

        // ---------------------------------------------------------------- pause and boosters

        bool CanAct() => built && state == UIState.Playing && !paused && !game.IsWon;

        void OpenPause()
        {
            if (state != UIState.Playing || paused || game.IsWon)
            {
                return;
            }
            paused = true;
            game.SetInputLocked(true);
            hud.SetInteractable(false);
            pauseScreen.Refresh(progress.Muted, progress.ReduceMotion, progress.UIScalePercent);
            pauseScreen.Show();
            UIMotion.FadeIn(animator, overlayFadeKey, pauseScreen.Group, tuning.screenFade, tuning.reduceMotion);
            Focus(pauseScreen.ResumeButton);
        }

        void ClosePause()
        {
            if (!paused)
            {
                return;
            }
            paused = false;
            pauseScreen.Hide();
            // Only a level still in play gets its input back (a lost or won level stays locked).
            if (state == UIState.Playing && !game.IsWon)
            {
                game.SetInputLocked(false);
                hud.SetInteractable(true);
                Focus(hud.FirstFocus);
            }
        }

        void OnPauseRestart()
        {
            paused = false;
            pauseScreen.Hide();
            game.RestartLevel();
        }

        void OnUndoClicked()
        {
            if (!CanAct())
            {
                return;
            }
            if (game.RequestUndo())
            {
                hud.BounceUndo();
            }
            else
            {
                hud.ShakeUndo();
                Cue?.Invoke(UICue.Refused, 0);
            }
        }

        void OnBoosterClicked(BoosterType type)
        {
            if (!CanAct())
            {
                return;
            }
            if (!progress.IsBoosterUnlocked(type))
            {
                Refuse(type, string.Format(UIStrings.BOOSTER_LOCKED_FORMAT, rules.UnlockAfterLevel(type)));
                return;
            }
            if (progress.BoosterCount(type) <= 0)
            {
                Refuse(type, string.Format(UIStrings.BOOSTER_EMPTY_FORMAT, UIStrings.BoosterPlural(type)));
                return;
            }
            switch (type)
            {
                case BoosterType.Hint:
                    if (game.RequestHint())
                    {
                        progress.TryConsume(type);
                        Used(type);
                    }
                    else
                    {
                        hud.ShakeBooster(type);
                    }
                    break;
                case BoosterType.AutoPack:
                    if (pendingAutoPack || game.IsBusy)
                    {
                        hud.ShakeBooster(type);
                        break;
                    }
                    // Reduced motion can complete the coroutine before RequestAutoPack returns.
                    pendingAutoPack = true;
                    if (!game.RequestAutoPack())
                    {
                        pendingAutoPack = false;
                        hud.ShakeBooster(type);
                    }
                    break;
                case BoosterType.Freeze:
                    if (!timer.Freeze(rules.FreezeSeconds))
                    {
                        Refuse(type, UIStrings.FREEZE_UNAVAILABLE);
                        break;
                    }
                    progress.TryConsume(type);
                    game.MarkAssistUsed();
                    Used(type);
                    break;
            }
        }

        void Refuse(BoosterType type, string message)
        {
            hud.ShakeBooster(type);
            hud.ShowToast(message);
            Cue?.Invoke(UICue.Refused, 0);
        }

        void Used(BoosterType type)
        {
            hud.BounceBooster(type);
            BoosterUsed?.Invoke();
            if (type == BoosterType.Freeze) Cue?.Invoke(UICue.Freeze, 0);
        }

        void OnAutoPackFinished(bool packed)
        {
            if (!built || !pendingAutoPack)
            {
                return;
            }
            pendingAutoPack = false;
            if (packed && progress.TryConsume(BoosterType.AutoPack))
            {
                Used(BoosterType.AutoPack);
            }
        }

        void OnFillChanged(float fraction)
        {
            if (built) hud.SetFill(fraction);
        }

        void OnHistoryChanged()
        {
            if (built)
            {
                hud.SetUndoUsable(game.CanUndo);
            }
        }

        void OnBoostersChanged()
        {
            if (built)
            {
                hud.RefreshBoosters(progress, timer.IsFrozen);
            }
        }

        void OnFreezeChanged(bool frozen)
        {
            if (!built)
            {
                return;
            }
            hud.Timer.SetFrozen(frozen, frozen ? Mathf.CeilToInt(timer.FrozenRemaining) : 0);
            hud.RefreshBoosters(progress, frozen);
        }

        int HighestPassed()
        {
            for (int level = progress.LevelCount; level >= 1; level--)
            {
                if (progress.IsPassed(level))
                {
                    return level;
                }
            }
            return 0;
        }

        ProgressionRules LoadRules()
        {
            var asset = Resources.Load<TextAsset>(ProgressionRules.RESOURCE);
            if (asset == null)
            {
                Debug.LogError($"GameUI: Resources/{ProgressionRules.RESOURCE}.json is missing; boosters are disabled.");
                return ProgressionRules.Empty();
            }
            var loaded = ProgressionRules.FromJson(asset.text, out string error);
            if (loaded == null)
            {
                Debug.LogError($"GameUI: {error} Boosters are disabled.");
                return ProgressionRules.Empty();
            }
            return loaded;
        }

        /// <summary>Saves and applies player audio and motion preferences.</summary>
        public void SetPreferences(bool muted, bool reduceMotion)
        {
            if (!built) return;
            progress.SetPreferences(muted, reduceMotion);
            ApplyPreferences();
            mainScreen.Refresh(progress);
            pauseScreen.Refresh(progress.Muted, progress.ReduceMotion, progress.UIScalePercent);
        }

        void CycleUISize()
        {
            progress.SetUIScalePercent(progress.UIScalePercent >= 150 ? 100 : progress.UIScalePercent + 25);
            ApplyPreferences();
            mainScreen.Refresh(progress);
            pauseScreen.Refresh(progress.Muted, progress.ReduceMotion, progress.UIScalePercent);
        }

        void ApplyPreferences()
        {
            // Scale the entire interface, not just glyphs: controls and scroll layouts grow with the text.
            // The smaller logical viewport triggers chip wrapping and adaptive grid columns.
            canvasObject.GetComponent<CanvasScaler>().referenceResolution =
                tuning.referenceResolution / (progress.UIScalePercent / 100f);
            viewInsetsDirty = true;
            tuning.reduceMotion = progress.ReduceMotion;
            game.ReduceMotion = progress.ReduceMotion;
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
            var safe = CurrentSafeArea;
            var screen = new Vector2Int(Screen.width, Screen.height);
            if (safe == appliedSafeArea && screen == appliedScreen || screen.x <= 0 || screen.y <= 0)
            {
                return;
            }
            appliedSafeArea = safe;
            appliedScreen = screen;
            viewInsetsDirty = true;
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
            // Menus hide the board and the HUD: nothing to fit until a level is on screen again.
            if (!lastInLevel && !viewInsetsDirty)
            {
                return;
            }
            viewInsetsDirty = false;
            banner.FitBelowHUD(hud.TopBandHeight);
            float canvasHeight = canvasRect.rect.height;
            if (canvasHeight <= 0f || Screen.height <= 0)
            {
                return;
            }
            var safe = CurrentSafeArea;
            float top = (Screen.height - safe.yMax) / Screen.height + hud.TopBandHeight / canvasHeight;
            float bottom = safe.yMin / Screen.height + hud.BottomBandHeight / canvasHeight;
            float left = safe.xMin / Screen.width;
            float right = (Screen.width - safe.xMax) / Screen.width;
            if (Mathf.Abs(top - appliedTopInset) < INSET_EPSILON && Mathf.Abs(bottom - appliedBottomInset) < INSET_EPSILON &&
                Mathf.Abs(left - appliedLeftInset) < INSET_EPSILON && Mathf.Abs(right - appliedRightInset) < INSET_EPSILON)
            {
                return;
            }
            appliedTopInset = top;
            appliedBottomInset = bottom;
            appliedLeftInset = left;
            appliedRightInset = right;
            game.SetViewInsets(top, bottom, left, right);
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
