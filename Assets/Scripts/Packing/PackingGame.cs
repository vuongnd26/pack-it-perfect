// Implements: design/game-brief.md "Core loop", "Điều khiển (Input)", "Cơ chế chính" (Kéo thả đặt đồ, Xoay đồ, Lấy đồ ra).
// Story: production/epics/pack-it-perfect/story-001-core-packing-loop.md
// Scene setup: one empty GameObject with this component, levelsJson = Assets/Data/Levels/levels.json.
// Everything else is built at runtime. Uses the scene's Camera.main.

using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PackItPerfect
{
    /// <summary>
    /// Entry point of the packing screen: loads a level, builds its views and turns pointer input
    /// into board actions (drag to place, tap in tray to rotate, tap placed piece to return it, drag placed piece to move it).
    /// </summary>
    public class PackingGame : MonoBehaviour
    {
        /// <summary>Pointer interaction state. Transitions:
        /// Idle → Pressing (press on a piece); Pressing → Idle (release = tap); Pressing → Dragging (moved past threshold);
        /// Dragging → Idle (release = drop); Idle/Pressing/Dragging → Won (last piece placed, level won);
        /// Won → (tap after lid closed) next level, Idle. Loading a level always resets to Idle.</summary>
        enum InputState { Idle, Pressing, Dragging, Won }

        [Tooltip("Assets/Data/Levels/levels.json")]
        [SerializeField] TextAsset levelsJson;

        [Tooltip("Level loaded on Play, 1-based.")]
        [SerializeField] int startLevel = 1;

        [SerializeField] PackingTuning tuning = new PackingTuning();

        Camera cam;
        SpriteFactory factory;
        LevelsFile levelsFile;
        int levelIndex;

        LoadedLevel level;
        BoardModel board;
        PieceShape[] shapes;
        PieceView[] pieceViews;
        PieceView ghost;
        GridView grid;
        TrayView tray;
        LidView lid;
        Transform levelRoot;
        Vector2Int lastScreenSize;

        InputState state;
        bool lidClosed;
        int activePiece;
        bool activeFromTray;
        Vector2 pressScreen;
        Vector2 pressWorld;
        Vector2 grabOffset;
        bool ghostShown;
        Vector2Int ghostOrigin;
        Pointer activePointer;

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
            if (levelsJson == null)
            {
                Debug.LogError("PackingGame: assign Assets/Data/Levels/levels.json to 'Levels Json'.");
                enabled = false;
                return;
            }
            levelsFile = LevelLoader.Parse(levelsJson.text, out string error);
            if (levelsFile == null)
            {
                Debug.LogError($"PackingGame: {error}");
                enabled = false;
                return;
            }

            cam.orthographic = true;
            cam.backgroundColor = tuning.backgroundColor;
            factory = new SpriteFactory();
            LoadLevel(startLevel - 1);
        }

        void OnDestroy()
        {
            factory?.Dispose();
        }

        void LoadLevel(int index)
        {
            int count = levelsFile.levels.Length;
            levelIndex = ((index % count) + count) % count;
            StopAllCoroutines();
            if (levelRoot != null)
            {
                Destroy(levelRoot.gameObject);
            }
            level = null;
            state = InputState.Idle;
            lidClosed = false;
            ghostShown = false;

            var data = levelsFile.levels[levelIndex];
            var loaded = LevelLoader.Build(levelsFile, data, out string error);
            if (loaded == null)
            {
                Debug.LogError($"PackingGame: {error}");
                return;
            }
            level = loaded;
            board = loaded.Board;
            shapes = (PieceShape[])loaded.StartShapes.Clone();
            Debug.Log($"PackingGame: loaded level {data.number} \"{data.name}\"");

            levelRoot = new GameObject($"Level {data.number}").transform;
            levelRoot.SetParent(transform, false);

            grid = new GridView(factory, levelRoot, board, data.theme, tuning);
            float trayWidth = Mathf.Max(grid.FrameRect.width, tuning.minTrayWidth);
            tray = new TrayView(shapes, grid.FrameRect.yMin - tuning.trayGap, trayWidth, tuning);
            lid = new LidView(factory, levelRoot, grid.FrameRect, data.theme, tuning);

            pieceViews = new PieceView[shapes.Length];
            for (int i = 0; i < shapes.Length; i++)
            {
                var view = new PieceView(factory, levelRoot, $"Piece {data.pieces[i].letter}",
                    LevelLoader.ParseColor(loaded.Items[i].color), SpriteFactory.ORDER_PIECE, tuning);
                view.SetShape(shapes[i]);
                view.PlaceAt(tray.TopLeftFor(i, shapes[i]), tuning.trayScale);
                pieceViews[i] = view;
            }
            ghost = new PieceView(factory, levelRoot, "Ghost", tuning.ghostValidColor, SpriteFactory.ORDER_GHOST, tuning);
            ghost.SetVisible(false);

            grid.SetFill(board.FillRatio);
            lastScreenSize = Vector2Int.zero;
            FitCamera();
        }

        void Update()
        {
            HandleKeyboard();
            if (level == null)
            {
                return;
            }
            FitCamera();
            HandlePointer();
            foreach (var view in pieceViews)
            {
                view.TickShake(Time.deltaTime);
            }
        }

        // Dev shortcuts until level select exists (story 007): R restart, N next, P previous.
        void HandleKeyboard()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }
            if (keyboard.rKey.wasPressedThisFrame)
            {
                LoadLevel(levelIndex);
            }
            else if (keyboard.nKey.wasPressedThisFrame)
            {
                LoadLevel(levelIndex + 1);
            }
            else if (keyboard.pKey.wasPressedThisFrame)
            {
                LoadLevel(levelIndex - 1);
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
                if (lidClosed && pointer.press.wasPressedThisFrame)
                {
                    LoadLevel(levelIndex + 1);
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
                grid.SetFill(board.FillRatio);
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
            ghost.SetColor(board.CanPlace(shapes[activePiece], ghostOrigin) ? tuning.ghostValidColor : tuning.ghostInvalidColor);
        }

        void EndPress()
        {
            var view = pieceViews[activePiece];
            if (state == InputState.Pressing)
            {
                if (activeFromTray)
                {
                    shapes[activePiece] = shapes[activePiece].Rotated();
                    view.SetShape(shapes[activePiece]);
                    view.PlaceAt(tray.TopLeftFor(activePiece, shapes[activePiece]), tuning.trayScale);
                }
                else
                {
                    board.Remove(activePiece);
                    grid.SetFill(board.FillRatio);
                    view.PlaceAt(tray.TopLeftFor(activePiece, shapes[activePiece]), tuning.trayScale);
                }
                state = InputState.Idle;
                return;
            }

            ghost.SetVisible(false);
            view.SetSortingOrder(SpriteFactory.ORDER_PIECE);
            state = InputState.Idle;
            if (ghostShown && board.Place(activePiece, shapes[activePiece], ghostOrigin))
            {
                view.PlaceAt(grid.CellTopLeft(ghostOrigin), 1f);
                grid.SetFill(board.FillRatio);
                if (board.IsWon())
                {
                    state = InputState.Won;
                    StartCoroutine(CloseLid());
                }
                return;
            }
            view.PlaceAt(tray.TopLeftFor(activePiece, shapes[activePiece]), tuning.trayScale);
            view.StartShake();
        }

        IEnumerator CloseLid()
        {
            yield return lid.Close();
            lidClosed = true;
        }
    }
}
