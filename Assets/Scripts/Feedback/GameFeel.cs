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
        [Tooltip("Sourced sound effects, procedural fallback tones and mix.")]
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
        int _placements;
        bool _audioWarming = true;
        float _backdropSize = -1f;
        float _backdropAspect = -1f;
        Vector3 _backdropCentre;
        readonly System.Random _random = new System.Random();
#if UNITY_ANDROID && !UNITY_EDITOR
        bool _hapticsUnavailable;
        AndroidJavaObject _activity;
        AndroidJavaObject _decorView;
        AndroidJavaRunnable _hapticRunnable;
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

        /// <summary>Plays a sound at a chosen pitch and volume; <paramref name="vary"/> adds the natural pitch spread.</summary>
        public void PlaySound(GameSound sound, float pitch, float volume = 1f, bool vary = false) =>
            _audio?.Play(sound, pitch, volume, vary);

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
            _game.PieceReturned += OnReturned;
            _game.Undone += OnUndone;
            _game.HintShown += OnHint;
        }

        void Start()
        {
            // GameUI creates its timer in Awake; all Awakes have now completed regardless of component order.
            if (_ui != null)
            {
                _ui.ButtonClicked += OnButton;
                _ui.BoosterUsed += OnBooster;
                _ui.Cue += OnCue;
                _timer = _ui.Timer;
                if (_timer != null) _timer.WarningTick += OnTick;
            }
        }

        void Update()
        {
            if (_audioWarming) _audioWarming = _audio.WarmNext();
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
            var position = _camera.transform.position;
            if (Mathf.Approximately(_camera.orthographicSize, _backdropSize) &&
                Mathf.Approximately(_camera.aspect, _backdropAspect) && position == _backdropCentre)
            {
                return; // the view did not change: leave the backdrop's transform alone
            }
            _backdropSize = _camera.orthographicSize;
            _backdropAspect = _camera.aspect;
            _backdropCentre = position;
            float viewHeight = _camera.orthographicSize * 2f;
            float viewWidth = viewHeight * _camera.aspect;
            var art = _backdrop.sprite.bounds.size;
            float scale = Mathf.Max(viewWidth / art.x, viewHeight / art.y);
            _backdrop.transform.position = new Vector3(position.x, position.y, 0f);
            _backdrop.transform.localScale = new Vector3(scale, scale, 1f);
        }

        void OnLoaded(LoadedLevel level)
        {
            _audio.Stop();
            _burst.Clear();
            _hand.Reset();
            _tutorialFinished = false;
            _placements = 0;
            _game.ReduceMotion = _reduceMotion;
            PlaySound(GameSound.LevelStart);
        }

        void OnPickup(int id) => PlaySound(GameSound.Pickup, 1f, 1f, true);
        void OnRotate(int id) => PlaySound(GameSound.Rotate, 1f, 1f, true);
        void OnButton(string id) => PlaySound(GameSound.UI);
        void OnBooster() => PlaySound(GameSound.Booster);
        void OnTick() => PlaySound(GameSound.Tick);
        void OnZipper() => PlaySound(GameSound.Zipper);
        void OnWon(int level, int stars) => PlaySound(GameSound.Win);
        void OnUndone() => PlaySound(GameSound.Undo);
        void OnHint(int id) => PlaySound(GameSound.Hint);

        void OnReturned(int id, PieceView view)
        {
            PlaySound(GameSound.Return, 1f, 1f, true);
            if (!_reduceMotion && view != null) _burst.Dust(view.RestCenter, view.Shape.MaxDimension * view.Scale);
        }

        // Interface moments that deserve a sound; the UI names the moment and never picks a clip.
        void OnCue(UI.UICue cue, int step)
        {
            switch (cue)
            {
                case UI.UICue.Star:
                    PlaySound(GameSound.Star, Mathf.Pow(Mathf.Max(1f, _audioTuning.starPitchStep), Mathf.Max(0, step)));
                    break;
                case UI.UICue.Reward:
                case UI.UICue.Purchase: PlaySound(GameSound.Reward); break;
                case UI.UICue.Refused: PlaySound(GameSound.Locked); break;
                case UI.UICue.Banner: PlaySound(GameSound.Banner); break;
                case UI.UICue.Freeze: PlaySound(GameSound.Freeze); break;
                case UI.UICue.Lost: PlaySound(GameSound.Lose); break;
            }
        }

        void OnPlaced(int id, PieceView view)
        {
            _tutorialFinished = true;
            _hand.Hide();
            // The knock climbs in pitch as the container fills: progress you can hear.
            PlaySound(GameSound.Place, 1f + _game.FillRatio * _audioTuning.placePitchRise, 1f, true);
            if (!_reduceMotion)
            {
                view.StartBounce(_tuning.bounceDuration, _tuning.bounceAmount);
                _burst.Sparkle(view.RestCenter, view.Shape.MaxDimension);
            }
            _placements++;
            if (_placements % Mathf.Max(1, _tuning.praiseEvery) == 0)
            {
                _game.Feedback?.Word(view.RestCenter + (Vector3)_tuning.praiseOffset,
                    GameTexts.PlacementPraise(_random.Next(GameTexts.PRAISE_COUNT)));
            }
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
                _burst.Celebrate(new Vector3(0f, -_game.CurrentLevel.Board.Height / 2f, 0f),
                    _game.CurrentLevel.Board.Width / 2f + 0.5f);
        }

        void LightHaptic()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!_tuning.haptics || _hapticsUnavailable) return;
            // Android's light system tactile feedback respects the OS setting and needs no VIBRATE permission.
            // Android View.performHapticFeedback returns false when feedback cannot be performed.
            // The activity, its view and the callback are looked up once: doing all of it on every placement
            // cost several JNI round trips and garbage each time.
            try
            {
                if (_activity == null)
                {
                    using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                    _activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                    _hapticRunnable = RunHaptic;
                }
                _activity.Call("runOnUiThread", _hapticRunnable);
            }
            catch (AndroidJavaException) { _hapticsUnavailable = true; }
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        void RunHaptic()
        {
            try
            {
                if (_decorView == null)
                {
                    using var window = _activity.Call<AndroidJavaObject>("getWindow");
                    _decorView = window.Call<AndroidJavaObject>("getDecorView");
                }
                const int VIRTUAL_KEY = 1;
                _decorView.Call<bool>("performHapticFeedback", VIRTUAL_KEY);
            }
            catch (AndroidJavaException) { _hapticsUnavailable = true; }
        }
#endif

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
                _game.PieceReturned -= OnReturned;
                _game.Undone -= OnUndone;
                _game.HintShown -= OnHint;
            }
            if (_ui != null)
            {
                _ui.ButtonClicked -= OnButton;
                _ui.BoosterUsed -= OnBooster;
                _ui.Cue -= OnCue;
            }
            if (_timer != null) _timer.WarningTick -= OnTick;
#if UNITY_ANDROID && !UNITY_EDITOR
            _decorView?.Dispose();
            _activity?.Dispose();
#endif
            _burst?.Dispose();
            _audio?.Dispose();
            if (_root != null) Destroy(_root.gameObject);
            _factory?.Dispose();
        }
    }
}
