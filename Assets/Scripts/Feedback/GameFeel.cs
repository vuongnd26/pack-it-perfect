using UnityEngine;

namespace PackItPerfect
{
    /// <summary>
    /// Presentation event adapter automatically attached by PackingGame.Awake. Owns no board rules or progression.
    /// Settings UI may set Muted and ReduceMotion at any time; no scene wiring is required.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameFeel : MonoBehaviour
    {
        [Header("Presentation")]
        [Tooltip("Placement, tutorial, particles and surface styling.")]
        [SerializeField] FeelTuning _tuning = new FeelTuning();
        [Tooltip("Original procedural sound synthesis and mix.")]
        [SerializeField] AudioTuning _audioTuning = new AudioTuning();
        [Tooltip("Initial audio preference; expose via settings UI using Muted.")]
        [SerializeField] bool _muted;
        [Tooltip("Initial motion preference; expose via settings UI using ReduceMotion.")]
        [SerializeField] bool _reduceMotion;

        const int BACKDROP_ORDER = -100;

        PackingGame _game;
        UI.GameUI _ui;
        LevelTimer _timer;
        GameAudio _audio;
        SpriteFactory _factory;
        Transform _root;
        TutorialHand _hand;
        ConfettiBurst _burst;
        SpriteRenderer _backdrop;
        Camera _camera;
        bool _tutorialFinished;
        readonly System.Random _random = new System.Random();
#if UNITY_ANDROID && !UNITY_EDITOR
        bool _hapticsUnavailable;
#endif

        /// <summary>Serialized feel configuration also used by the board's runtime sprite cache.</summary>
        public FeelTuning Tuning => _tuning;

        /// <summary>Mutes all game/UI/timer sounds immediately; persistence is owned by the caller.</summary>
        public bool Muted
        {
            get => _muted;
            set { _muted = value; if (_audio != null) _audio.Muted = value; }
        }

        /// <summary>Suppresses hand travel, particles, bounces, shakes, wobbles and lid travel; praise remains readable.</summary>
        public bool ReduceMotion
        {
            get => _reduceMotion;
            set
            {
                _reduceMotion = value;
                if (_game != null) _game.ReduceMotion = value;
                if (value) { _hand?.Hide(); _burst?.Clear(); }
            }
        }

        /// <summary>Routes any additional UI or gameplay presentation sound through the shared mute-aware sound bank.</summary>
        public void PlaySound(GameSound sound) => _audio?.Play(sound);

        void Awake()
        {
            _game = GetComponent<PackingGame>();
            _ui = GetComponent<UI.GameUI>();
            _factory = new SpriteFactory();
            _root = new GameObject("Game feel").transform;
            _root.SetParent(transform, false);
            _hand = new TutorialHand(_factory, _root, _tuning);
            _burst = new ConfettiBurst(_factory, _root, _tuning);
            var backdropArt = _factory.GetArt("bg_game");
            if (backdropArt != null)
            {
                _backdrop = _factory.CreateSprite(_root, "Backdrop", backdropArt, Vector2.zero, Vector2.one, Color.white, BACKDROP_ORDER);
            }
            _audio = new GameAudio(transform, _audioTuning);
            _audio.Muted = _muted;
            _game.ReduceMotion = _reduceMotion;
            _game.LevelLoaded += OnLoaded;
            _game.PiecePickedUp += OnPickup;
            _game.PiecePlaced += OnPlaced;
            _game.PieceRotated += OnRotate;
            _game.InvalidAction += OnInvalid;
            _game.ZipperStarted += OnZipper;
            _game.LevelCompleted += OnCompleted;
            _game.LevelWon += OnWon;
        }

        void Start()
        {
            // GameUI creates its timer in Awake; all Awakes have now completed regardless of component order.
            if (_ui != null)
            {
                _ui.ButtonClicked += OnButton;
                _timer = _ui.Timer;
                if (_timer != null) _timer.WarningTick += OnTick;
            }
        }

        void Update()
        {
            _burst.Tick(Time.unscaledDeltaTime);
            if (_reduceMotion || _tutorialFinished || _game.CurrentLevelNumber != _tuning.tutorialLevel ||
                _game.InputLocked || _game.IsWon || _game.IsInteracting || !_game.TryGetTutorialPath(out var from, out var to))
            {
                _hand.Hide();
                return;
            }
            _hand.Tick(Time.unscaledDeltaTime, from, to);
        }

        // The camera refits per level; keep the illustrated backdrop covering whatever it shows.
        void LateUpdate()
        {
            if (_backdrop == null) return;
            if (_camera == null) _camera = Camera.main;
            if (_camera == null || !_camera.orthographic) return;
            float viewHeight = _camera.orthographicSize * 2f;
            float viewWidth = viewHeight * _camera.aspect;
            var art = _backdrop.sprite.bounds.size;
            float scale = Mathf.Max(viewWidth / art.x, viewHeight / art.y);
            var position = _camera.transform.position;
            _backdrop.transform.position = new Vector3(position.x, position.y, 0f);
            _backdrop.transform.localScale = new Vector3(scale, scale, 1f);
        }

        void OnLoaded(LoadedLevel level)
        {
            _audio.Stop();
            _burst.Clear();
            _hand.Reset();
            _tutorialFinished = false;
            _game.ReduceMotion = _reduceMotion;
        }

        void OnPickup(int id) => PlaySound(GameSound.Pickup);
        void OnRotate(int id) => PlaySound(GameSound.Rotate);
        void OnButton(string id) => PlaySound(GameSound.UI);
        void OnTick() => PlaySound(GameSound.Tick);
        void OnZipper() => PlaySound(GameSound.Zipper);
        void OnWon(int level, int stars) => PlaySound(GameSound.Win);

        void OnPlaced(int id, PieceView view)
        {
            _tutorialFinished = true;
            _hand.Hide();
            PlaySound(GameSound.Place);
            if (!_reduceMotion)
            {
                view.StartBounce(_tuning.bounceDuration, _tuning.bounceAmount);
                _burst.Sparkle(view.RestCenter);
            }
            _game.Feedback?.Word(view.RestCenter + (Vector3)_tuning.praiseOffset,
                GameTexts.PlacementPraise(_random.Next(GameTexts.PRAISE_COUNT)));
            LightHaptic();
        }

        void OnInvalid()
        {
            PlaySound(GameSound.Invalid);
            LightHaptic();
        }

        void OnCompleted(int level, int stars)
        {
            // Before the delayed win screen appears; render above the lid and below praise/toasts.
            if (!_reduceMotion && _game.CurrentLevel != null)
                _burst.Celebrate(new Vector3(0f, -_game.CurrentLevel.Board.Height / 2f, 0f));
        }

        void LightHaptic()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!_tuning.haptics || _hapticsUnavailable) return;
            // Android's light system tactile feedback respects the OS setting and needs no VIBRATE permission.
            // Android View.performHapticFeedback returns false when feedback cannot be performed.
            try
            {
                using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                {
                    try
                    {
                        using var callbackPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                        using var callbackActivity = callbackPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                        using var window = callbackActivity.Call<AndroidJavaObject>("getWindow");
                        using var view = window.Call<AndroidJavaObject>("getDecorView");
                        const int VIRTUAL_KEY = 1;
                        view.Call<bool>("performHapticFeedback", VIRTUAL_KEY);
                    }
                    catch (AndroidJavaException) { _hapticsUnavailable = true; }
                }));
            }
            catch (AndroidJavaException) { _hapticsUnavailable = true; }
#endif
        }

        void OnDisable()
        {
            _audio?.Stop();
            _hand?.Hide();
            _burst?.Clear();
        }

        void OnDestroy()
        {
            if (_game != null)
            {
                _game.LevelLoaded -= OnLoaded;
                _game.PiecePickedUp -= OnPickup;
                _game.PiecePlaced -= OnPlaced;
                _game.PieceRotated -= OnRotate;
                _game.InvalidAction -= OnInvalid;
                _game.ZipperStarted -= OnZipper;
                _game.LevelCompleted -= OnCompleted;
                _game.LevelWon -= OnWon;
            }
            if (_ui != null) _ui.ButtonClicked -= OnButton;
            if (_timer != null) _timer.WarningTick -= OnTick;
            _audio?.Dispose();
            if (_root != null) Destroy(_root.gameObject);
            _factory?.Dispose();
        }
    }
}
