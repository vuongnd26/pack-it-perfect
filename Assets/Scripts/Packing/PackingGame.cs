// Implements: design/game-brief.md "Core loop", "Điều khiển (Input)", "Cơ chế chính" (Kéo thả đặt đồ, Xoay đồ, Lấy đồ ra,
// Khuôn gợi ý), "Obstacles", "Booster" and "Điều kiện thắng / thua".
// Stories: production/epics/pack-it-perfect/story-001-core-packing-loop.md, story-002-level-data.md,
// story-003-obstacle-rules.md, story-004-outline-imprints.md, story-005-hint-and-stars.md
// Scene setup: one empty GameObject with this component, levelsJson = Assets/Data/Levels/levels.json.
// Everything else is built at runtime. Uses the scene's Camera.main.

using System;
using System.Collections;
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

        // ---------------------------------------------------------------- public API (UI wave)

        /// <summary>Raised after a level is built and shown (also on restart). Carries the level's data, rules and <see cref="LevelInfo"/>.</summary>
        public event Action<LoadedLevel> LevelLoaded;

        /// <summary>Raised whenever the filled share of fillable cells changes, 0..1 (also 0 on level load).</summary>
        public event Action<float> FillChanged;

        /// <summary>Raised once per win, after the lid has closed: (level number, stars). Stars = 3, or 2 if Hint was used.</summary>
        public event Action<int, int> LevelWon;

        /// <summary>1-based number of the current level (the last one requested if it failed to load).</summary>
        public int CurrentLevelNumber => levelIndex + 1;

        /// <summary>Number of levels in levels.json (0 before Awake or if the file failed to parse).</summary>
        public int LevelCount => levelsFile != null ? levelsFile.levels.Length : 0;

        /// <summary>The level being played, or null if none loaded.</summary>
        public LoadedLevel CurrentLevel => level;

        /// <summary>True once Hint was used in this play of the current level; reset on every (re)load.</summary>
        public bool HintUsed => hints != null && hints.Used;

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
        /// after the win, or when every piece is placed. Unlimited uses.
        /// </summary>
        public void RequestHint()
        {
            if (level == null || inputLocked || state != InputState.Idle || !hints.TryApply(board, out var hint))
            {
                return;
            }
            foreach (int id in hint.Evicted)
            {
                pieceViews[id].PlaceAt(tray.TopLeftFor(id, shapes[id]), tuning.trayScale);
            }
            int piece = hint.PieceId;
            shapes[piece] = hint.Shape;
            var view = pieceViews[piece];
            view.SetShape(hint.Shape);
            view.PlaceAt(tray.TopLeftFor(piece, hint.Shape), tuning.trayScale);
            view.StartWobble(tuning.hintWobbleDuration, tuning.hintWobbleCycles);
            OnBoardChanged();
            hintGlow.Show(hint.TargetCells);
        }

        /// <summary>
        /// Locks or unlocks pointer input and Hint (the timer locks it on time-up). Locking drops any piece being dragged
        /// back to the tray. Loading a level unlocks.
        /// </summary>
        public void SetInputLocked(bool locked)
        {
            if (locked && (state == InputState.Pressing || state == InputState.Dragging))
            {
                CancelPress();
            }
            inputLocked = locked;
        }

        // ---------------------------------------------------------------- lifecycle

        void Awake()
        {
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
            LoadLevelAt(Mathf.Clamp(startLevel, 1, LevelCount) - 1);
        }

        void OnDestroy()
        {
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
                // Called before Start (e.g. by a UI script's Start): Start loads this level instead of startLevel.
                startLevel = index + 1;
                return;
            }
            int count = levelsFile.levels.Length;
            levelIndex = ((index % count) + count) % count;
            StopAllCoroutines();
            if (levelRoot != null)
            {
                Destroy(levelRoot.gameObject);
            }
            level = null;
            hints = null;
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

            grid = new GridView(factory, levelRoot, loaded, tuning);
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
                    icon = factory.GetIcon(piece.item),
                    zoneIcon = loaded.ZoneOf(i)?.icon,
                    heavy = piece.heavy,
                    upright = piece.upright,
                    paired = piece.pair >= 0,
                };
                var view = new PieceView(factory, levelRoot, $"Piece {piece.letter}",
                    LevelLoader.ParseColor(loaded.Items[i].color), SpriteFactory.ORDER_PIECE, tuning, decor);
                view.SetShape(shapes[i]);
                view.PlaceAt(tray.TopLeftFor(i, shapes[i]), tuning.trayScale);
                pieceViews[i] = view;
            }
            ghost = new PieceView(factory, levelRoot, "Ghost", tuning.ghostValidColor, SpriteFactory.ORDER_GHOST, tuning);
            ghost.SetVisible(false);

            lastScreenSize = Vector2Int.zero;
            FitCamera();
            OnBoardChanged();
            LevelLoaded?.Invoke(loaded);
        }

        void Update()
        {
            HandleKeyboard();
            if (level == null)
            {
                worldFeedback?.Tick(Time.deltaTime);
                return;
            }
            FitCamera();
            worldFeedback?.Tick(Time.deltaTime);
            if (!inputLocked)
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
            float halfHeight = Mathf.Max((yMax - yMin) / 2f, (xMax - xMin) / 2f / cam.aspect);
            cam.orthographicSize = halfHeight + tuning.screenMargin;
            cam.transform.position = new Vector3((xMin + xMax) / 2f, (yMin + yMax) / 2f, cam.transform.position.z);
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
            if (state == InputState.Pressing && (screen - pressScreen).magnitude > tuning.dragThresholdPixels)
            {
                BeginDrag();
            }
            if (state == InputState.Dragging)
            {
                UpdateDrag(ScreenToWorld(screen));
            }
        }

        void BeginDrag()
        {
            var view = pieceViews[activePiece];
            var shape = shapes[activePiece];
            // Keep the grabbed point of the piece under the pointer, measured at full scale and clamped to the piece bounds.
            Vector2 local = (pressWorld - (Vector2)view.Root.position) / view.Scale;
            grabOffset = new Vector2(Mathf.Clamp(local.x, 0f, shape.Width), Mathf.Clamp(local.y, -shape.Height, 0f));

            if (!activeFromTray)
            {
                board.Remove(activePiece);
                OnBoardChanged();
            }
            view.PlaceAt(view.Root.position, 1f);
            view.SetSortingOrder(SpriteFactory.ORDER_DRAGGED);
            ghost.SetShape(shape);
            state = InputState.Dragging;
        }

        void UpdateDrag(Vector2 world)
        {
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
                    board.Remove(id);
                    view.PlaceAt(tray.TopLeftFor(id, shapes[id]), tuning.trayScale);
                    OnBoardChanged();
                }
                return;
            }

            ghost.SetVisible(false);
            view.SetSortingOrder(SpriteFactory.ORDER_PIECE);
            state = InputState.Idle;
            if (ghostShown)
            {
                var failure = board.CheckPlacement(id, shapes[id], ghostOrigin);
                if (failure == PlacementFailure.None && board.Place(id, shapes[id], ghostOrigin))
                {
                    view.PlaceAt(grid.CellTopLeft(ghostOrigin), 1f);
                    OnBoardChanged();
                    OnPiecePlaced(id);
                    return;
                }
                ToastPlacementFailure(id, failure);
            }
            view.PlaceAt(tray.TopLeftFor(id, shapes[id]), tuning.trayScale);
            view.StartShake();
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
                return;
            }
            shapes[id] = shapes[id].Rotated();
            view.SetShape(shapes[id]);
            view.PlaceAt(tray.TopLeftFor(id, shapes[id]), tuning.trayScale);
        }

        // Toast only when the drop failed for exactly one rule reason; geometry failures just shake.
        void ToastPlacementFailure(int id, PlacementFailure failure)
        {
            if (failure == PlacementFailure.Heavy)
            {
                feedback?.Toast(GameTexts.HEAVY_VIOLATION);
            }
            else if (failure == PlacementFailure.Zone)
            {
                var zone = level.ZoneOf(id);
                if (zone != null)
                {
                    feedback?.Toast(GameTexts.ZoneViolation(level.Items[id].displayName, zone.name));
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
                return;
            }

            var violation = board.FindCompletionViolation();
            if (!violation.IsViolation)
            {
                state = InputState.Won;
                StartCoroutine(CloseLid(StarCalculator.StarsFor(hints.Used)));
                return;
            }

            // Every piece is in but a rule fails at completion: only pairs are checked here.
            grid.StartShake();
            for (int i = 0; i < pieceViews.Length; i++)
            {
                pieceViews[i].StartShake();
            }
            if (violation.rule is PairRule)
            {
                feedback?.Toast(GameTexts.PairViolation(level.Items[violation.pieceId].displayName));
            }
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
            yield return lid.Close();
            lidClosed = true;
            LevelWon?.Invoke(number, stars);
        }
    }
}
