// Implements: design/game-brief.md "Core loop", "Điều khiển (Input)", "Cơ chế chính" (Kéo thả đặt đồ, Xoay đồ, Lấy đồ ra,
// Khuôn gợi ý), "Obstacles", "Booster" and "Điều kiện thắng / thua".
// Stories: production/epics/pack-it-perfect/story-001-core-packing-loop.md, story-002-level-data.md,
// story-003-obstacle-rules.md, story-004-outline-imprints.md, story-005-hint-and-stars.md
// Scene setup: one empty GameObject with this component, levelsJson = Assets/Data/Levels/levels.json.
// Everything else is built at runtime. Uses the scene's Camera.main.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PackItPerfect
{
    /// <summary>
    /// Entry point of the packing screen: loads a level, builds its views and turns pointer input
    /// into board actions (drag to place, tap in tray to rotate, tap placed piece to return it, drag placed piece to move it).
    /// The public members below are the contract for the screen-space UI (HUD, Hint button, timer, banners, win/lose
    /// screens, level select). Subscribe to the events in Awake/OnEnable to catch the first level load, or read
    /// <see cref="CurrentLevel"/> when you subscribe later.
    /// </summary>
    public class PackingGame : MonoBehaviour
    {
        /// <summary>Pointer interaction state. Transitions:
        /// Idle → Pressing (press on a piece); Pressing → Idle (release = tap); Pressing → Dragging (moved past threshold);
        /// Dragging → Idle (release = drop); Pressing/Dragging → Idle (input locked: press cancelled);
        /// Dragging → Won (last piece placed and every rule holds);
        /// Won → (tap after lid closed, only if autoAdvanceOnWin) next level. Loading a level always resets to Idle.</summary>
        enum InputState { Idle, Pressing, Dragging, Won }

        [Tooltip("Assets/Data/Levels/levels.json")]
        [SerializeField] TextAsset levelsJson;

        [Tooltip("Level loaded on Play, 1-based.")]
        [SerializeField] int startLevel = 1;

        [SerializeField] PackingTuning tuning = new PackingTuning();

        GameFeel feel;
        PatternSpriteCache patterns;
        bool reduceMotion;
        Camera cam;
        SpriteFactory factory;
        LevelsFile levelsFile;
        LevelInfo[] levelInfos;
        int levelIndex;

        LoadedLevel level;
        BoardModel board;
        PieceShape[] shapes;
        PieceView[] pieceViews;
        PieceView ghost;
        TMPro.TextMeshPro placementMark;
        GridView grid;
        TrayView tray;
        LidView lid;
        OutlineView outlines;
        HintGlowView hintGlow;
        HintService hints;
        Transform levelRoot;
        Vector2Int lastScreenSize;

        WorldFeedback worldFeedback;
        IFeedback feedback;

        InputState state;
        bool inputLocked;
        bool lidClosed;
        int activePiece;
        bool activeFromTray;
        Vector2 pressScreen;
        Vector2 pressWorld;
        Vector2 grabOffset;
        bool ghostShown;
        Vector2Int ghostOrigin;
        Pointer activePointer;

        // One piece's state inside an Undo snapshot. Charges, time and assist use are deliberately not part of it.
        struct PieceState
        {
            public bool placed;
            public Vector2Int origin;
            public PieceShape shape;
            public int artTurns;
        }

        readonly List<PieceState[]> history = new List<PieceState[]>();
        PieceState[] dragStart;
        bool assistUsed;
        bool busy;
        Coroutine autoPackCoroutine;
        PieceState[] autoPackStart;
        int autoPackOperation;
        float dragAge;

        const float MAX_VIEW_INSET = 0.45f;
        bool loadOnStart = true;
        float viewInsetTop;
        float viewInsetBottom;
        float viewInsetLeft;
        float viewInsetRight;

        // ---------------------------------------------------------------- public API (UI wave)

        /// <summary>Raised after a level is built and shown (also on restart). Carries the level's data, rules and <see cref="LevelInfo"/>.</summary>
        public event Action<LoadedLevel> LevelLoaded;

        /// <summary>Raised whenever the filled share of fillable cells changes, 0..1 (also 0 on level load).</summary>
        public event Action<float> FillChanged;

        /// <summary>Raised once per win, after the lid has closed: (level number, stars). Stars = 3, or 2 if a Hint, Auto Pack or Freeze was used.</summary>
        public event Action<int, int> LevelWon;

        /// <summary>Raised immediately at an accepted winning drop, before lid animation; safe persistence hook.</summary>
        public event Action<int, int> LevelCompleted;
        /// <summary>Raised when a piece starts being dragged (piece id).</summary>
        public event Action<int> PiecePickedUp;
        /// <summary>Raised on accepted placement (piece id, current view).</summary>
        public event Action<int, PieceView> PiecePlaced;
        /// <summary>Raised after a tray piece rotates.</summary>
        public event Action<int> PieceRotated;
        /// <summary>Raised on an invalid drop, forbidden rotation or failed completion rule.</summary>
        public event Action InvalidAction;
        /// <summary>Raised as the zipper begins, after the lid lands.</summary>
        public event Action ZipperStarted;
        /// <summary>Raised when <see cref="CanUndo"/> may have changed.</summary>
        public event Action HistoryChanged;
        /// <summary>Raised when an Auto Pack request ends: true if a piece was packed (spend the charge), false if nothing happened.</summary>
        public event Action<bool> AutoPackFinished;
        /// <summary>True if <see cref="RequestUndo"/> has a player action to take back.</summary>
        public bool CanUndo => history.Count > 0 && level != null && state == InputState.Idle && !busy && !inputLocked;
        /// <summary>True while Auto Pack is searching or flying a piece in; player input is ignored meanwhile.</summary>
        public bool IsBusy => busy;
        /// <summary>True once Hint, Auto Pack or Freeze was used in this play of the level (caps the result at 2 stars).</summary>
        public bool AssistUsed => assistUsed || (hints != null && hints.Used);
        /// <summary>Number of pieces still in the tray (0 when no level is loaded).</summary>
        public int UnplacedCount
        {
            get
            {
                if (level == null) return 0;
                int count = 0;
                for (int i = 0; i < board.PieceCount; i++)
                {
                    if (!board.IsPlaced(i)) count++;
                }
                return count;
            }
        }
        /// <summary>True while the player is pressing or dragging a piece.</summary>
        public bool IsInteracting => state == InputState.Pressing || state == InputState.Dragging;

        /// <summary>Applies motion reduction to current and future gameplay presentation.</summary>
        public bool ReduceMotion
        {
            get => reduceMotion;
            set
            {
                reduceMotion = value;
                if (grid != null) grid.ReduceMotion = value;
                if (lid != null) lid.ReduceMotion = value;
                if (worldFeedback != null) worldFeedback.ReduceMotion = value;
                if (pieceViews != null)
                    foreach (var view in pieceViews) view.ReduceMotion = value;
            }
        }

        /// <summary>Gets the tutorial's current tray anchor and first solution anchor without moving any item.</summary>
        public bool TryGetTutorialPath(out Vector3 from, out Vector3 to)
        {
            from = to = Vector3.zero;
            if (level == null || pieceViews == null || pieceViews.Length == 0 || board.IsPlaced(0)) return false;
            var shape = level.SolutionShapes[0];
            from = pieceViews[0].RestCenter;
            to = grid.CellTopLeft(level.SolutionOrigins[0]) + new Vector3(shape.Width / 2f, -shape.Height / 2f);
            return true;
        }

        /// <summary>1-based number of the current level (the last one requested if it failed to load).</summary>
        public int CurrentLevelNumber => levelIndex + 1;

        /// <summary>Number of levels in levels.json (0 before Awake or if the file failed to parse).</summary>
        public int LevelCount => levelsFile != null ? levelsFile.levels.Length : 0;

        /// <summary>The level being played, or null if none loaded.</summary>
        public LoadedLevel CurrentLevel => level;

        /// <summary>True once Hint was used in this play of the current level; reset on every (re)load.</summary>
        public bool HintUsed => AssistUsed;

        /// <summary>True while pointer input and Hint are ignored (see <see cref="SetInputLocked"/>).</summary>
        public bool InputLocked => inputLocked;

        /// <summary>True from the winning drop until the next level load.</summary>
        public bool IsWon => state == InputState.Won;

        /// <summary>
        /// When true (default), a tap after the lid closes loads the next level. The UI wave sets this false and
        /// shows its win screen on <see cref="LevelWon"/> instead.
        /// </summary>
        public bool AutoAdvanceOnWin
        {
            get => tuning.autoAdvanceOnWin;
            set => tuning.autoAdvanceOnWin = value;
        }

        /// <summary>
        /// Where rule toasts and words go. Defaults to the built-in world-space toast; assign a screen-space
        /// implementation to replace it, or null to restore the default.
        /// </summary>
        public IFeedback Feedback
        {
            get => feedback;
            set
            {
                feedback?.Clear();
                feedback = value ?? worldFeedback;
            }
        }

        /// <summary>
        /// When true (default), Start loads the serialized start level. The UI sets this false in its Awake so the app
        /// opens on the main menu. A <see cref="LoadLevel"/> call made before Start still loads that level in Start.
        /// </summary>
        public bool LoadOnStart
        {
            get => loadOnStart;
            set => loadOnStart = value;
        }

        /// <summary>
        /// Asked when a pointer press begins; return true to ignore that press (e.g. it landed on a screen-space UI button).
        /// Null = never blocked. Only the start of a press is checked: a drag that began on the board continues over the UI.
        /// </summary>
        public Func<bool> PointerBlocked { get; set; }

        /// <summary>
        /// Reserves screen space the board must stay out of (HUD bands), as fractions of the screen height measured from
        /// the top and bottom edges, each clamped to [0, 0.45]. Re-fits the camera at once when a level is showing.
        /// </summary>
        public void SetViewInsets(float topFraction, float bottomFraction, float leftFraction = 0f, float rightFraction = 0f)
        {
            viewInsetTop = Mathf.Clamp(topFraction, 0f, MAX_VIEW_INSET);
            viewInsetBottom = Mathf.Clamp(bottomFraction, 0f, MAX_VIEW_INSET);
            viewInsetLeft = Mathf.Clamp(leftFraction, 0f, MAX_VIEW_INSET);
            viewInsetRight = Mathf.Clamp(rightFraction, 0f, MAX_VIEW_INSET);
            lastScreenSize = Vector2Int.zero;
            if (level != null && cam != null)
            {
                FitCamera();
            }
        }

        /// <summary>Read-only meta of any level (name, timer, banner, rule chips) without loading it; null if out of range.</summary>
        /// <param name="levelNumber">1-based level number.</param>
        public LevelInfo GetLevelInfo(int levelNumber) =>
            levelInfos != null && levelNumber >= 1 && levelNumber <= levelInfos.Length ? levelInfos[levelNumber - 1] : null;

        /// <summary>
        /// Loads level <paramref name="levelNumber"/> (1-based): rebuilds the board and views, clears Hint use and unlocks input.
        /// Logs an error and does nothing if the number is out of range.
        /// </summary>
        public void LoadLevel(int levelNumber)
        {
            if (levelNumber < 1 || levelNumber > LevelCount)
            {
                Debug.LogError($"PackingGame: no level {levelNumber} (levels are 1..{LevelCount}).");
                return;
            }
            LoadLevelAt(levelNumber - 1);
        }

        /// <summary>Reloads the current level from scratch (Hint use cleared, input unlocked).</summary>
        public void RestartLevel() => LoadLevelAt(levelIndex);

        /// <summary>
        /// Hint booster: turns the first unplaced piece (data order) to its solution orientation, sends back to the tray
        /// every placed piece on its solution cells, marks <see cref="HintUsed"/>, lights the target cells and wobbles the
        /// piece in the tray. The player still drags it in. Does nothing while input is locked, while a piece is held,
        /// after the win, or when every piece is placed. The caller owns the charge: spend one when this returns true.
        /// A hint cannot be undone (it clears the Undo history).
        /// </summary>
        /// <returns>True if a hint was applied.</returns>
        public bool RequestHint()
        {
            if (level == null || inputLocked || busy || state != InputState.Idle) return false;
            if (!hints.TryApply(board, out var hint))
            {
                if (hints.LastFailure != PlacementFailure.None)
                    feedback?.Toast(GameTexts.HINT_BLOCKED);
                return false;
            }
            ClearHistory();
            foreach (int id in hint.Evicted)
            {
                pieceViews[id].PlaceAt(tray.TopLeftFor(id, shapes[id]), tuning.trayScale);
            }
            int piece = hint.PieceId;
            shapes[piece] = hint.Shape;
            var view = pieceViews[piece];
            view.SetShape(hint.Shape, 0);
            view.PlaceAt(tray.TopLeftFor(piece, hint.Shape), tuning.trayScale);
            view.StartWobble(tuning.hintWobbleDuration, tuning.hintWobbleCycles);
            OnBoardChanged();
            hintGlow.Show(hint.TargetCells);
            return true;
        }

        /// <summary>
        /// Marks this play of the level as assisted (the UI calls it when a Freeze is used), capping the result at 2 stars.
        /// </summary>
        public void MarkAssistUsed() => assistUsed = true;

        /// <summary>
        /// Undo: takes back the last player action (a placement, a move, a removal or a turn). Free and unlimited, but it
        /// never gives back time or booster charges, and it cannot step back past a Hint or an Auto Pack.
        /// </summary>
        /// <returns>True if an action was undone.</returns>
        public bool RequestUndo()
        {
            if (!CanUndo)
            {
                return false;
            }
            var snapshot = history[history.Count - 1];
            history.RemoveAt(history.Count - 1);
            Restore(snapshot);
            HistoryChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Auto Pack booster: packs one tray piece for the player. First it searches for a full packing that keeps every
        /// piece already on the board (see <see cref="AutoPackSolver"/>) and places the biggest tray piece of that packing.
        /// If the pieces on the board cannot be completed, it places the first tray piece at its reference spot instead,
        /// sending back to the tray whatever is in the way. Ends with <see cref="AutoPackFinished"/>; the caller spends
        /// the charge only on true. Cannot be undone.
        /// </summary>
        /// <returns>True if the request started.</returns>
        public bool RequestAutoPack()
        {
            if (level == null || inputLocked || busy || state != InputState.Idle || board.AllPlaced)
            {
                return false;
            }
            busy = true;
            autoPackStart = Capture();
            int operation = ++autoPackOperation;
            var coroutine = StartCoroutine(AutoPackRoutine(operation));
            if (busy && operation == autoPackOperation) autoPackCoroutine = coroutine;
            return true;
        }

        /// <summary>
        /// Locks or unlocks pointer input and Hint (the timer locks it on time-up). Locking drops any piece being dragged
        /// back to the tray. Loading a level unlocks.
        /// </summary>
        public void SetInputLocked(bool locked)
        {
            if (locked) CancelAutoPack();
            if (locked && (state == InputState.Pressing || state == InputState.Dragging))
            {
                CancelPress();
            }
            inputLocked = locked;
        }

        // ---------------------------------------------------------------- lifecycle

        void Awake()
        {
            feel = GetComponent<GameFeel>();
            if (feel == null) feel = gameObject.AddComponent<GameFeel>();
            if (levelsJson == null)
            {
                Debug.LogError("PackingGame: assign Assets/Data/Levels/levels.json to 'Levels Json'.");
                return;
            }
            levelsFile = LevelLoader.Parse(levelsJson.text, out string error);
            if (levelsFile == null)
            {
                Debug.LogError($"PackingGame: {error}");
                return;
            }
            levelInfos = new LevelInfo[levelsFile.levels.Length];
            for (int i = 0; i < levelInfos.Length; i++)
            {
                levelInfos[i] = new LevelInfo(levelsFile.levels[i]);
            }
            foreach (var problem in LevelLoader.ValidateAll(levelsFile, tuning.levelLimits))
            {
                Debug.LogError($"PackingGame: levels.json rejected — {problem}");
            }
        }

        void Start()
        {
            // Grid quads live in this object's local space while pieces and input use world space,
            // so pin the root to the world origin wherever the object was created in the scene.
            transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            transform.localScale = Vector3.one;

            cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError("PackingGame: the scene has no camera tagged MainCamera.");
                enabled = false;
                return;
            }
            if (levelsFile == null)
            {
                enabled = false;
                return;
            }

            cam.orthographic = true;
            cam.backgroundColor = tuning.backgroundColor;
            factory = new SpriteFactory();
            worldFeedback = new WorldFeedback(factory, transform, cam, tuning);
            feedback ??= worldFeedback;
            if (loadOnStart)
            {
                LoadLevelAt(Mathf.Clamp(startLevel, 1, LevelCount) - 1);
            }
        }

        void OnDestroy()
        {
            patterns?.Dispose();
            factory?.Dispose();
        }

        void LoadLevelAt(int index)
        {
            if (levelsFile == null)
            {
                return;
            }
            if (factory == null)
            {
                // Called before Start (e.g. by a UI script's Start): Start loads this level instead of startLevel,
                // even when LoadOnStart is false.
                startLevel = index + 1;
                loadOnStart = true;
                return;
            }
            CancelAutoPack();
            int count = levelsFile.levels.Length;
            levelIndex = ((index % count) + count) % count;
            StopAllCoroutines();
            if (levelRoot != null)
            {
                levelRoot.gameObject.SetActive(false);
                Destroy(levelRoot.gameObject);
            }
            patterns?.Dispose();
            patterns = new PatternSpriteCache(feel.Tuning);
            level = null;
            hints = null;
            assistUsed = false;
            busy = false;
            history.Clear();
            dragStart = null;
            state = InputState.Idle;
            inputLocked = false;
            lidClosed = false;
            ghostShown = false;
            feedback?.Clear();

            var data = levelsFile.levels[levelIndex];
            var loaded = LevelLoader.Build(levelsFile, data, tuning.levelLimits, out string error);
            if (loaded == null)
            {
                Debug.LogError($"PackingGame: {error}");
                return;
            }
            level = loaded;
            board = loaded.Board;
            hints = new HintService(loaded);
            shapes = (PieceShape[])loaded.StartShapes.Clone();
            Debug.Log($"PackingGame: loaded level {data.number} \"{data.name}\"");

            levelRoot = new GameObject($"Level {data.number}").transform;
            levelRoot.SetParent(transform, false);

            grid = new GridView(factory, levelRoot, loaded, tuning, feel.Tuning);
            outlines = new OutlineView(factory, grid, loaded, tuning);
            hintGlow = new HintGlowView(factory, grid, tuning);
            float trayWidth = Mathf.Max(grid.FrameRect.width, tuning.minTrayWidth);
            tray = new TrayView(shapes, grid.FrameRect.yMin - tuning.trayGap, trayWidth, tuning);
            lid = new LidView(factory, levelRoot, grid.FrameRect, data.theme, tuning);

            pieceViews = new PieceView[shapes.Length];
            for (int i = 0; i < shapes.Length; i++)
            {
                var piece = data.pieces[i];
                var decor = new PieceDecor
                {
                    art = factory.GetPieceArt(data.number, piece.letter),
                    patterns = patterns,
                    pattern = loaded.Items[i].pattern,
                    outlineColor = feel.Tuning.outlineColor,
                    icon = factory.GetIcon(piece.item),
                    zoneIcon = loaded.ZoneOf(i)?.icon,
                    heavy = piece.heavy,
                    upright = piece.upright,
                    paired = piece.pair >= 0,
                    fragile = piece.fragile,
                };
                var view = new PieceView(factory, levelRoot, $"Piece {piece.letter}",
                    LevelLoader.ParseColor(loaded.Items[i].color), SpriteFactory.ORDER_PIECE, tuning, decor);
                view.SetShape(shapes[i], piece.startTurns);
                view.PlaceAt(tray.TopLeftFor(i, shapes[i]), tuning.trayScale);
                pieceViews[i] = view;
            }
            ghost = new PieceView(factory, levelRoot, "Ghost", tuning.ghostValidColor, SpriteFactory.ORDER_GHOST, tuning);
            placementMark = TmpText.Create(ghost.Root, "Placement status", string.Empty, 0.5f,
                Color.black, SpriteFactory.ORDER_DRAGGED + 5);
            ghost.SetVisible(false);

            ReduceMotion = reduceMotion;
            lastScreenSize = Vector2Int.zero;
            FitCamera();
            OnBoardChanged();
            LevelLoaded?.Invoke(loaded);
            HistoryChanged?.Invoke();
        }

        void Update()
        {
#if UNITY_EDITOR
            if (AutoAdvanceOnWin) HandleKeyboard();
#endif
            if (level == null)
            {
                worldFeedback?.Tick(Time.deltaTime);
                return;
            }
            FitCamera();
            worldFeedback?.Tick(Time.deltaTime);
            if (!inputLocked && !busy)
            {
                HandlePointer();
            }
            float dt = Time.deltaTime;
            foreach (var view in pieceViews)
            {
                view.TickShake(dt);
            }
            grid.Tick(dt);
            hintGlow.Tick(dt);
        }

        // Dev shortcuts until level select exists (story 007): R restart, N next, P previous, H hint.
        void HandleKeyboard()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }
            if (keyboard.rKey.wasPressedThisFrame)
            {
                RestartLevel();
            }
            else if (keyboard.nKey.wasPressedThisFrame)
            {
                LoadLevelAt(levelIndex + 1);
            }
            else if (keyboard.pKey.wasPressedThisFrame)
            {
                LoadLevelAt(levelIndex - 1);
            }
            else if (keyboard.hKey.wasPressedThisFrame)
            {
                RequestHint();
            }
        }

        // Fits fill bar + frame + tray inside the view; recomputed only when the screen size changes.
        void FitCamera()
        {
            var screenSize = new Vector2Int(Screen.width, Screen.height);
            if (screenSize == lastScreenSize)
            {
                return;
            }
            lastScreenSize = screenSize;

            Rect content = grid.ContentRect;
            Rect trayBounds = tray.Bounds;
            float xMin = Mathf.Min(content.xMin, trayBounds.xMin);
            float xMax = Mathf.Max(content.xMax, trayBounds.xMax);
            float yMin = Mathf.Min(content.yMin, trayBounds.yMin);
            float yMax = Mathf.Max(content.yMax, trayBounds.yMax);
            // The content fits in the band left between the top and bottom view insets (fractions of the view height);
            // with no insets this is the same fit as before.
            float usable = 1f - viewInsetTop - viewInsetBottom;
            float halfContentHeight = (yMax - yMin) / 2f;
            float halfContentWidth = (xMax - xMin) / 2f;
            float usableWidth = 1f - viewInsetLeft - viewInsetRight;
            float size = Mathf.Max((halfContentHeight + tuning.screenMargin) / usable,
                (halfContentWidth / cam.aspect + tuning.screenMargin) / usableWidth);
            cam.orthographicSize = size;
            // Shift the camera so the content centre sits in the middle of the usable band, not of the screen.
            float bandCentre = viewInsetBottom + usable / 2f;
            float centreY = (yMin + yMax) / 2f + (0.5f - bandCentre) * 2f * size;
            float centreX = (xMin + xMax) / 2f + (0.5f - viewInsetLeft - usableWidth / 2f) * 2f * size * cam.aspect;
            cam.transform.position = new Vector3(centreX, centreY, cam.transform.position.z);
        }

        // ---------------------------------------------------------------- pointer

        void HandlePointer()
        {
            // While a press is in progress, keep reading the device that started it:
            // Pointer.current follows whichever pointer had input last.
            bool pressing = state == InputState.Pressing || state == InputState.Dragging;
            if (pressing && activePointer != null && !activePointer.added)
            {
                EndPress();
                return;
            }
            var pointer = pressing && activePointer != null ? activePointer : Pointer.current;
            if (pointer == null)
            {
                return;
            }
            Vector2 screen = pointer.position.ReadValue();

            if (state == InputState.Won)
            {
                if (lidClosed && tuning.autoAdvanceOnWin && pointer.press.wasPressedThisFrame)
                {
                    LoadLevelAt(levelIndex + 1);
                }
                return;
            }

            if (pointer.press.wasPressedThisFrame)
            {
                if (PointerBlocked != null && PointerBlocked())
                {
                    return; // the press landed on the UI
                }
                activePointer = pointer;
                BeginPress(screen);
            }
            if (state == InputState.Pressing || state == InputState.Dragging)
            {
                MovePress(screen);
                if (!pointer.press.isPressed)
                {
                    EndPress();
                }
            }
        }

        Vector2 ScreenToWorld(Vector2 screen) =>
            cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z));

        void BeginPress(Vector2 screen)
        {
            Vector2 world = ScreenToWorld(screen);
            int trayPiece = tray.HitTest(world, i => !board.IsPlaced(i));
            int gridPiece = board.PieceAt(grid.WorldToCell(world));
            if (trayPiece < 0 && gridPiece == BoardModel.NO_PIECE && activePointer is Touchscreen)
            {
                float padding = Mathf.Min(tuning.touchHitMaxCells,
                    tuning.touchHitPaddingPixels * cam.orthographicSize * 2f / Mathf.Max(1, Screen.height));
                trayPiece = tray.HitTestExpanded(world, i => !board.IsPlaced(i), padding);
            }
            if (trayPiece < 0 && gridPiece == BoardModel.NO_PIECE)
            {
                return;
            }
            activeFromTray = trayPiece >= 0;
            activePiece = activeFromTray ? trayPiece : gridPiece;
            pressScreen = screen;
            pressWorld = world;
            state = InputState.Pressing;
        }

        void MovePress(Vector2 screen)
        {
            float threshold = tuning.dragThresholdPixels;
            if (activePointer is Touchscreen)
            {
                float dpi = Screen.dpi;
                if (dpi < 72f || dpi > 1000f) dpi = Mathf.Min(Screen.width, Screen.height) / 2.5f;
                threshold = Mathf.Clamp(dpi * tuning.touchDragMillimetres / 25.4f,
                    tuning.touchDragPixelBounds.x, tuning.touchDragPixelBounds.y);
            }
            if (state == InputState.Pressing && (screen - pressScreen).magnitude > threshold)
            {
                BeginDrag();
            }
            if (state == InputState.Dragging)
            {
                dragAge += Time.unscaledDeltaTime;
                UpdateDrag(ScreenToWorld(screen));
            }
        }

        // How far the dragged piece floats above the pointer, in cells (eased in; touch only by default).
        float DragLift()
        {
            float target = activePointer is Touchscreen ? tuning.touchLiftCells : tuning.mouseLiftCells;
            float ease = tuning.liftEaseSeconds <= 0f ? 1f : Mathf.Clamp01(dragAge / tuning.liftEaseSeconds);
            return target * ease;
        }

        void BeginDrag()
        {
            var view = pieceViews[activePiece];
            var shape = shapes[activePiece];
            // Keep the grabbed point of the piece under the pointer, measured at full scale and clamped to the piece bounds.
            Vector2 local = (pressWorld - (Vector2)view.Root.position) / view.Scale;
            grabOffset = new Vector2(Mathf.Clamp(local.x, 0f, shape.Width), Mathf.Clamp(local.y, -shape.Height, 0f));

            dragStart = Capture(); // before the piece leaves the board: the whole gesture is one Undo step
            dragAge = 0f;
            if (!activeFromTray)
            {
                board.Remove(activePiece);
                OnBoardChanged();
            }
            view.PlaceAt(view.Root.position, 1f);
            view.SetSortingOrder(SpriteFactory.ORDER_DRAGGED);
            ghost.SetShape(shape);
            state = InputState.Dragging;
            PiecePickedUp?.Invoke(activePiece);
        }

        void UpdateDrag(Vector2 world)
        {
            // The lifted piece, its ghost and the drop all use the same shifted position, so what is shown is what lands.
            world += Vector2.up * DragLift();
            Vector2 topLeft = world - grabOffset;
            pieceViews[activePiece].PlaceAt(topLeft, 1f);

            ghostShown = grid.IsNear(world, tuning.ghostHideDistance);
            ghost.SetVisible(ghostShown);
            if (!ghostShown)
            {
                return;
            }
            ghostOrigin = grid.SnapOrigin(topLeft);
            ghost.PlaceAt(grid.CellTopLeft(ghostOrigin), 1f);
            // Full check (bounds, blocked, overlap, zone, heavy): red when any fails.
            bool valid = board.CanPlace(activePiece, shapes[activePiece], ghostOrigin);
            ghost.SetColor(valid ? tuning.ghostValidColor : tuning.ghostInvalidColor);
            TmpText.SetText(placementMark, valid ? GameTexts.PLACEMENT_VALID : GameTexts.PLACEMENT_INVALID);
            placementMark.transform.localPosition = new Vector3(shapes[activePiece].Width * 0.5f, 0.45f, 0f);
        }

        void EndPress()
        {
            int id = activePiece;
            var view = pieceViews[id];
            if (state == InputState.Pressing)
            {
                state = InputState.Idle;
                if (activeFromTray)
                {
                    RotateInTray(id);
                }
                else
                {
                    PushHistory(Capture());
                    board.Remove(id);
                    view.PlaceAt(tray.TopLeftFor(id, shapes[id]), tuning.trayScale);
                    OnBoardChanged();
                }
                return;
            }
            var before = dragStart;
            dragStart = null;

            ghost.SetVisible(false);
            view.SetSortingOrder(SpriteFactory.ORDER_PIECE);
            state = InputState.Idle;
            if (ghostShown)
            {
                var failure = board.CheckPlacement(id, shapes[id], ghostOrigin);
                if (failure == PlacementFailure.None && board.Place(id, shapes[id], ghostOrigin))
                {
                    view.PlaceAt(grid.CellTopLeft(ghostOrigin), 1f);
                    PushHistoryIfChanged(before);
                    OnBoardChanged();
                    PiecePlaced?.Invoke(id, view);
                    OnPiecePlaced(id);
                    return;
                }
                ToastPlacementFailure(id, failure);
            }
            view.PlaceAt(tray.TopLeftFor(id, shapes[id]), tuning.trayScale);
            PushHistoryIfChanged(before); // a board piece dropped badly ends in the tray: Undo puts it back
            view.StartShake();
            InvalidAction?.Invoke();
        }

        // Input lock during a press: a dragged piece goes back to the tray without a shake (it was already off the board).
        void CancelPress()
        {
            if (state == InputState.Dragging)
            {
                var view = pieceViews[activePiece];
                ghost.SetVisible(false);
                view.SetSortingOrder(SpriteFactory.ORDER_PIECE);
                view.PlaceAt(tray.TopLeftFor(activePiece, shapes[activePiece]), tuning.trayScale);
                PushHistoryIfChanged(dragStart);
                dragStart = null;
            }
            state = InputState.Idle;
        }

        void RotateInTray(int id)
        {
            var view = pieceViews[id];
            if (!board.CanRotate(id))
            {
                view.StartWobble(tuning.wobbleDuration, tuning.wobbleCycles);
                feedback?.Toast(GameTexts.UPRIGHT_VIOLATION);
                InvalidAction?.Invoke();
                return;
            }
            PushHistory(Capture());
            shapes[id] = shapes[id].Rotated();
            view.TurnArtClockwise();
            view.SetShape(shapes[id]);
            view.PlaceAt(tray.TopLeftFor(id, shapes[id]), tuning.trayScale);
            PieceRotated?.Invoke(id);
        }

        // Presentation names follow the illustration without mutating shared item definitions or rule data.
        string PresentationName(int id) =>
            factory.GetPieceArt(level.Data.number, level.Data.pieces[id].letter)?.DisplayName ?? level.Items[id].displayName;

        // Deterministic rule priority for combined flags; geometry-only failures keep the invalid-drop shake.
        void ToastPlacementFailure(int id, PlacementFailure failure)
        {
            if ((failure & PlacementFailure.Heavy) != 0)
            {
                feedback?.Toast(GameTexts.HEAVY_VIOLATION);
            }
            else if ((failure & PlacementFailure.Fragile) != 0)
            {
                feedback?.Toast(GameTexts.FRAGILE_VIOLATION);
            }
            else if ((failure & PlacementFailure.Zone) != 0)
            {
                var zone = level.ZoneOf(id);
                if (zone != null)
                {
                    feedback?.Toast(GameTexts.ZoneViolation(PresentationName(id), zone.name));
                }
            }
        }

        void OnPiecePlaced(int id)
        {
            var pairs = level.Rules.Pairs;
            if (pairs != null && pairs.TouchesPartner(board, id))
            {
                feedback?.Word(pieceViews[id].RestCenter, GameTexts.PAIR_PASS);
            }
            if (!board.AllPlaced)
            {
                int partner = pairs != null ? pairs.PartnerOf(id) : BoardModel.NO_PIECE;
                if (partner >= 0 && board.IsPlaced(partner) && !pairs.TouchesPartner(board, id))
                {
                    feedback?.Toast(GameTexts.PairViolation(PresentationName(id)));
                    pieceViews[partner].StartWobble(tuning.hintWobbleDuration, tuning.hintWobbleCycles);
                }
                return;
            }

            var violation = board.FindCompletionViolation();
            if (!violation.IsViolation)
            {
                state = InputState.Won;
                int stars = StarCalculator.StarsFor(AssistUsed);
                var completed = level;
                LevelCompleted?.Invoke(completed.Info.Number, stars);
                // A completion subscriber may navigate immediately; never close the replacement level's lid.
                if (ReferenceEquals(level, completed) && state == InputState.Won)
                    StartCoroutine(CloseLid(stars));
                return;
            }

            // Every piece is in but a rule fails at completion: only pairs are checked here.
            InvalidAction?.Invoke();
            grid.StartShake();
            for (int i = 0; i < pieceViews.Length; i++)
            {
                pieceViews[i].StartShake();
            }
            if (violation.rule is PairRule)
            {
                feedback?.Toast(GameTexts.PairViolation(PresentationName(violation.pieceId)));
            }
        }

        // ---------------------------------------------------------------- undo

        PieceState[] Capture()
        {
            var snapshot = new PieceState[shapes.Length];
            for (int i = 0; i < snapshot.Length; i++)
            {
                bool placed = board.TryGetPlacement(i, out var placedShape, out var origin);
                snapshot[i] = new PieceState
                {
                    placed = placed,
                    origin = origin,
                    shape = placed ? placedShape : shapes[i],
                    artTurns = pieceViews[i].ArtQuarterTurns,
                };
            }
            return snapshot;
        }

        bool SameAsNow(PieceState[] snapshot)
        {
            for (int i = 0; i < snapshot.Length; i++)
            {
                bool placed = board.TryGetPlacement(i, out _, out var origin);
                if (placed != snapshot[i].placed || (placed && origin != snapshot[i].origin) ||
                    !shapes[i].SameCells(snapshot[i].shape) || pieceViews[i].ArtQuarterTurns != snapshot[i].artTurns)
                {
                    return false;
                }
            }
            return true;
        }

        void PushHistory(PieceState[] snapshot)
        {
            history.Add(snapshot);
            if (history.Count > Mathf.Max(1, tuning.undoDepth))
            {
                history.RemoveAt(0);
            }
            HistoryChanged?.Invoke();
        }

        // A gesture that ends where it began (a tray piece dropped back in the tray) is not an Undo step.
        void PushHistoryIfChanged(PieceState[] snapshot)
        {
            if (snapshot != null && !SameAsNow(snapshot))
            {
                PushHistory(snapshot);
            }
        }

        void ClearHistory()
        {
            if (history.Count > 0)
            {
                history.Clear();
                HistoryChanged?.Invoke();
            }
        }

        // Puts every piece back where the snapshot has it. Rule checks cannot fail: the snapshot was a legal board,
        // and every rule is independent of placement order.
        void Restore(PieceState[] snapshot)
        {
            for (int i = 0; i < snapshot.Length; i++)
            {
                board.Remove(i);
            }
            for (int i = 0; i < snapshot.Length; i++)
            {
                var saved = snapshot[i];
                shapes[i] = saved.shape;
                var view = pieceViews[i];
                view.SetShape(saved.shape, saved.artTurns);
                if (saved.placed && board.Place(i, saved.shape, saved.origin))
                {
                    view.PlaceAt(grid.CellTopLeft(saved.origin), 1f);
                }
                else
                {
                    view.PlaceAt(tray.TopLeftFor(i, saved.shape), tuning.trayScale);
                }
            }
            hintGlow.Clear();
            OnBoardChanged();
        }

        // ---------------------------------------------------------------- auto pack

        IEnumerator AutoPackRoutine(int operation)
        {
            var playing = level;
            var solver = new AutoPackSolver(level, board, tuning.autoPackNodeLimit);
            while (solver.Step(Mathf.Max(1, tuning.autoPackNodesPerFrame)) == SolveStatus.Searching)
            {
                yield return null;
                if (!ReferenceEquals(level, playing) || inputLocked)
                {
                    FinishAutoPack(false);
                    yield break;
                }
            }

            if (solver.Status == SolveStatus.GaveUp)
            {
                feedback?.Toast(GameTexts.AUTO_PACK_SEARCH_LIMIT);
                FinishAutoPack(false);
                yield break;
            }
            var trial = board.Clone();
            var evicted = new List<int>();
            int piece = BoardModel.NO_PIECE;
            SolvedPlacement target = default;
            if (solver.Status == SolveStatus.Solved)
            {
                // Biggest tray piece of the found packing (ties: data order): the most helpful single placement.
                foreach (var entry in solver.Solution)
                {
                    if (piece == BoardModel.NO_PIECE || entry.Value.shape.Cells.Count > target.shape.Cells.Count ||
                        (entry.Value.shape.Cells.Count == target.shape.Cells.Count && entry.Key < piece))
                    {
                        piece = entry.Key;
                        target = entry.Value;
                    }
                }
            }
            if (piece == BoardModel.NO_PIECE)
            {
                // Proven impossible: plan the reference fallback on a clone, not the live board.
                for (int i = 0; i < board.PieceCount && piece == BoardModel.NO_PIECE; i++)
                {
                    if (!board.IsPlaced(i)) piece = i;
                }
                if (piece == BoardModel.NO_PIECE)
                {
                    FinishAutoPack(false);
                    yield break;
                }
                target = new SolvedPlacement(level.SolutionShapes[piece], level.SolutionOrigins[piece], 0);
                foreach (var cell in level.SolutionCellsOf(piece))
                {
                    int occupant = trial.PieceAt(cell);
                    if (occupant != BoardModel.NO_PIECE)
                    {
                        trial.Remove(occupant);
                        evicted.Add(occupant);
                    }
                }
                if (!trial.CanPlace(piece, target.shape, target.origin))
                {
                    // A neighbour rule still objects (fragile next to a misplaced heavy piece): send back every piece
                    // that is not at its own reference spot. What remains is part of the reference packing, which is legal.
                    for (int i = 0; i < board.PieceCount; i++)
                    {
                        if (trial.IsPlaced(i) && !level.IsAtSolution(trial, i))
                        {
                            trial.Remove(i);
                            evicted.Add(i);
                        }
                    }
                }
            }
            if (!trial.CanPlace(piece, target.shape, target.origin))
            {
                FinishAutoPack(false);
                yield break;
            }

            var view = pieceViews[piece];
            view.SetShape(target.shape, target.turnsFromSolution);
            Vector3 from = tray.TopLeftFor(piece, target.shape);
            Vector3 to = grid.CellTopLeft(target.origin);
            view.SetSortingOrder(SpriteFactory.ORDER_DRAGGED);
            PiecePickedUp?.Invoke(piece);
            float duration = reduceMotion ? 0f : tuning.autoPackFlySeconds;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                if (operation != autoPackOperation) yield break;
                float k = Mathf.SmoothStep(0f, 1f, t / duration);
                // A small arc: the piece rises out of the tray, then settles into its cells.
                var position = Vector3.Lerp(from, to, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.6f;
                view.PlaceAt(position, Mathf.Lerp(tuning.trayScale, 1f, k));
                yield return null;
            }
            view.SetSortingOrder(SpriteFactory.ORDER_PIECE);
            if (operation != autoPackOperation) yield break;
            if (!ReferenceEquals(level, playing) || inputLocked)
            {
                CancelAutoPack();
                yield break;
            }
            // No yields during commit: occupancy, orientation and assistance become visible together.
            foreach (int id in evicted) ReturnToTray(id);
            if (!board.Place(piece, target.shape, target.origin))
            {
                CancelAutoPack();
                yield break;
            }
            shapes[piece] = target.shape;
            view.PlaceAt(to, 1f);
            assistUsed = true;
            ClearHistory();
            OnBoardChanged();
            PiecePlaced?.Invoke(piece, view);
            FinishAutoPack(true);
            OnPiecePlaced(piece);
        }

        void ReturnToTray(int id)
        {
            if (id == BoardModel.NO_PIECE || !board.IsPlaced(id)) return;
            board.Remove(id);
            pieceViews[id].PlaceAt(tray.TopLeftFor(id, shapes[id]), tuning.trayScale);
        }

        void CancelAutoPack()
        {
            if (!busy) return;
            ++autoPackOperation;
            if (autoPackCoroutine != null) StopCoroutine(autoPackCoroutine);
            autoPackCoroutine = null;
            if (autoPackStart != null)
            {
                Restore(autoPackStart);
                foreach (var piece in pieceViews) piece.SetSortingOrder(SpriteFactory.ORDER_PIECE);
            }
            FinishAutoPack(false);
        }

        void FinishAutoPack(bool placed)
        {
            autoPackStart = null;
            autoPackCoroutine = null;
            busy = false;
            AutoPackFinished?.Invoke(placed);
            HistoryChanged?.Invoke();
        }

        // Fill bar, outlines and FillChanged follow every board change.
        void OnBoardChanged()
        {
            grid.SetFill(board.FillRatio);
            outlines.Refresh(board);
            FillChanged?.Invoke(board.FillRatio);
        }

        IEnumerator CloseLid(int stars)
        {
            int number = level.Info.Number;
            yield return lid.Close(() => ZipperStarted?.Invoke());
            lidClosed = true;
            LevelWon?.Invoke(number, stars);
        }
    }
}
