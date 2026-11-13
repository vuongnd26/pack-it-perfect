// Win confetti and placement glints as real Unity ParticleSystems (burst emission, gravity, spin, fade).
// Story: production/epics/pack-it-perfect/story-008-tutorial-and-juice.md

using UnityEngine;

namespace PackItPerfect
{
    /// <summary>
    /// Owns the particle systems under the feedback root: a win fountain with side cannons, small placement rays and a
    /// soft puff used for landing glows and dust. Everything is built once; firing an effect allocates nothing.
    /// </summary>
    public sealed class ConfettiBurst
    {
        const float PUFF_GROWTH = 2.4f;
        const float GLOW_SECONDS = 0.32f;
        const float GLOW_ALPHA = 0.5f;
        const float DUST_SECONDS = 0.4f;
        const float DUST_ALPHA = 0.55f;
        const float SIDE_UP_SPEED = 0.85f;
        const float SIDE_IN_SPEED = 0.45f;
        static readonly Color DustColor = new Color(1f, 0.97f, 0.9f, 1f);

        readonly ParticleSystem _puff;
        readonly Material _softMaterial;
        readonly System.Random _random = new System.Random(4099);

        const int SORTING_ORDER = 49;
        const int MAX_GRADIENT_KEYS = 8;
        const float STANDARD_GRAVITY = 9.81f;
        const float FADE_START = 0.7f;
        const float SIZE_VARIATION = 0.3f;
        const float EMITTER_RADIUS = 0.35f;
        const float SPARKLE_RADIUS = 0.05f;
        const float MIN_LIFETIME_SHARE = 0.65f;

        readonly FeelTuning _tuning;
        readonly ParticleSystem _confetti;
        readonly ParticleSystem _sparkle;

        /// <summary>Builds both systems once; nothing is allocated when they fire.</summary>
        public ConfettiBurst(SpriteFactory factory, Transform parent, FeelTuning tuning)
        {
            _tuning = tuning;
            // Reuse the project's sprite material so particles draw (and light) exactly like the rest of the board.
            var probe = factory.CreateQuad(parent, "Material probe", Vector2.zero, Vector2.one, Color.white, 0);
            var material = probe.sharedMaterial;
            Object.Destroy(probe.gameObject);

            _confetti = Create(parent, "Confetti", material);
            ConfigureConfetti();
            _sparkle = Create(parent, "Placement sparkle", material);
            ConfigureSparkle();

            // Same shader as every sprite, but sampling the blurred disc so puffs have soft edges.
            if (material != null)
            {
                _softMaterial = new Material(material) { name = "PackSoftParticle", mainTexture = factory.SoftTexture };
            }
            _puff = Create(parent, "Soft puff", _softMaterial != null ? _softMaterial : material);
            _puff.GetComponent<ParticleSystemRenderer>().sortingOrder = SORTING_ORDER - 1;
            ConfigurePuff();
        }

        void ConfigurePuff()
        {
            var main = _puff.main;
            main.playOnAwake = false;
            main.loop = false;
            main.useUnscaledTime = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 48;
            main.startLifetime = DUST_SECONDS;
            main.startSpeed = 0f;
            main.startSize = 0.4f;
            main.gravityModifier = 0f;

            var emission = _puff.emission;
            emission.enabled = false; // fired by Emit()
            var shape = _puff.shape;
            shape.enabled = false;

            var grow = _puff.sizeOverLifetime;
            grow.enabled = true;
            grow.size = new ParticleSystem.MinMaxCurve(PUFF_GROWTH, AnimationCurve.EaseInOut(0f, 1f / PUFF_GROWTH, 1f, 1f));

            var fade = _puff.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            fade.color = new ParticleSystem.MinMaxGradient(gradient);
        }

        static ParticleSystem Create(Transform parent, string name, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingOrder = SORTING_ORDER;
            return system;
        }

        void ConfigureConfetti()
        {
            float duration = Mathf.Max(0.01f, _tuning.confettiDuration);
            var main = _confetti.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = duration;
            main.useUnscaledTime = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = _tuning.particleCapacity;
            main.startLifetime = new ParticleSystem.MinMaxCurve(duration * MIN_LIFETIME_SHARE, duration);
            main.startSpeed = new ParticleSystem.MinMaxCurve(_tuning.confettiSpeed.x, _tuning.confettiSpeed.y);
            main.startSize3D = true;
            main.startSizeX = Vary(_tuning.confettiSize.x);
            main.startSizeY = Vary(_tuning.confettiSize.y);
            main.startSizeZ = 1f;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = _tuning.confettiGravity / STANDARD_GRAVITY;
            main.startColor = Palette();

            var emission = _confetti.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, (short)Mathf.Clamp(_tuning.confettiCount, 1, _tuning.particleCapacity)),
            });

            // Cone turned to open upwards (+Y): a fountain that gravity pulls back down.
            var shape = _confetti.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = _tuning.confettiSpread;
            shape.radius = EMITTER_RADIUS;
            shape.rotation = new Vector3(-90f, 0f, 0f);

            float spin = _tuning.confettiSpin * Mathf.Deg2Rad;
            var rotation = _confetti.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-spin, spin);

            var fade = _confetti.colorOverLifetime;
            fade.enabled = true;
            fade.color = FadeOut();
        }

        void ConfigureSparkle()
        {
            float duration = Mathf.Max(0.01f, _tuning.sparkleDuration);
            var main = _sparkle.main;
            main.playOnAwake = false;
            main.loop = false;
            main.useUnscaledTime = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = _tuning.particleCapacity;
            main.startLifetime = duration;
            main.startSpeed = _tuning.sparkleTravel / duration;
            main.startSize = _tuning.sparkleSize.y;
            main.startColor = _tuning.sparkleColor;
            main.gravityModifier = 0f;

            var emission = _sparkle.emission;
            emission.enabled = false; // fired by Emit()

            // Circle emits radially in the board plane.
            var shape = _sparkle.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = SPARKLE_RADIUS;

            var fade = _sparkle.colorOverLifetime;
            fade.enabled = true;
            fade.color = FadeOut();

            // Stretch each particle along its travel so it reads as a short ray.
            var renderer = _sparkle.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0f;
            renderer.lengthScale = _tuning.sparkleSize.x / Mathf.Max(0.001f, _tuning.sparkleSize.y);
        }

        static ParticleSystem.MinMaxCurve Vary(float size) =>
            new ParticleSystem.MinMaxCurve(size * (1f - SIZE_VARIATION), size * (1f + SIZE_VARIATION));

        // One random palette colour per particle: a fixed (stepped) gradient sampled at random.
        ParticleSystem.MinMaxGradient Palette()
        {
            var colors = _tuning.confettiColors;
            if (colors == null || colors.Length == 0)
            {
                return _tuning.sparkleColor;
            }
            int count = Mathf.Min(colors.Length, MAX_GRADIENT_KEYS);
            var keys = new GradientColorKey[count];
            for (int i = 0; i < count; i++)
            {
                keys[i] = new GradientColorKey(colors[i], (i + 1f) / count);
            }
            var gradient = new Gradient { mode = GradientMode.Fixed };
            gradient.SetKeys(keys, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return new ParticleSystem.MinMaxGradient(gradient) { mode = ParticleSystemGradientMode.RandomColor };
        }

        static ParticleSystem.MinMaxGradient FadeOut()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, FADE_START), new GradientAlphaKey(0f, 1f) });
            return new ParticleSystem.MinMaxGradient(gradient);
        }

        /// <summary>Small radial glints on a correct drop, over a soft glow the size of the piece.</summary>
        /// <param name="position">Centre of the placed piece.</param>
        /// <param name="size">Longest side of the piece in cells; scales the glow.</param>
        public void Sparkle(Vector3 position, float size = 1f)
        {
            _sparkle.transform.position = position;
            _sparkle.Emit(Mathf.Clamp(_tuning.sparkleCount, 1, 16));
            var glow = new ParticleSystem.EmitParams
            {
                position = position,
                velocity = Vector3.zero,
                startLifetime = GLOW_SECONDS,
                startSize = Mathf.Clamp(size, 1f, 4f) * _tuning.glowSize,
                startColor = WithAlpha(_tuning.sparkleColor, GLOW_ALPHA),
            };
            _puff.Emit(glow, 1);
        }

        /// <summary>A few soft dust puffs drifting outwards (piece back in the tray, wrong drop).</summary>
        public void Dust(Vector3 position, float spread = 0.5f)
        {
            int count = Mathf.Clamp(_tuning.dustCount, 1, 12);
            for (int i = 0; i < count; i++)
            {
                float angle = (i + (float)_random.NextDouble()) / count * Mathf.PI * 2f;
                var direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle) * 0.55f, 0f);
                var dust = new ParticleSystem.EmitParams
                {
                    position = position + direction * spread * 0.4f,
                    velocity = direction * (0.5f + (float)_random.NextDouble() * 0.7f),
                    startLifetime = DUST_SECONDS * (0.7f + (float)_random.NextDouble() * 0.5f),
                    startSize = _tuning.dustSize * (0.7f + (float)_random.NextDouble() * 0.6f),
                    startColor = WithAlpha(DustColor, DUST_ALPHA),
                };
                _puff.Emit(dust, 1);
            }
        }

        /// <summary>
        /// Fires the win burst: a colourful fountain from <paramref name="position"/> that falls under gravity, plus two
        /// side cannons <paramref name="halfWidth"/> to the left and right shooting inwards.
        /// </summary>
        public void Celebrate(Vector3 position, float halfWidth = 0f)
        {
            _confetti.transform.position = position;
            _confetti.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _confetti.Play(true);
            int perSide = Mathf.Clamp(_tuning.confettiSideCount, 0, _tuning.particleCapacity / 4);
            if (halfWidth <= 0f || perSide <= 0) return;
            var colors = _tuning.confettiColors;
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < perSide; i++)
                {
                    float speed = Mathf.Lerp(_tuning.confettiSpeed.x, _tuning.confettiSpeed.y, (float)_random.NextDouble());
                    float lean = SIDE_IN_SPEED * (0.4f + (float)_random.NextDouble());
                    var shot = new ParticleSystem.EmitParams
                    {
                        position = position + new Vector3(side * halfWidth, 0f, 0f),
                        velocity = new Vector3(-side * speed * lean, speed * SIDE_UP_SPEED, 0f),
                    };
                    if (colors != null && colors.Length > 0) shot.startColor = colors[_random.Next(colors.Length)];
                    _confetti.Emit(shot, 1);
                }
            }
        }

        static Color32 WithAlpha(Color color, float alpha) => new Color(color.r, color.g, color.b, color.a * alpha);

        /// <summary>Immediately removes effects on a new level or when motion reduction is enabled.</summary>
        public void Clear()
        {
            if (_confetti != null) _confetti.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (_sparkle != null) _sparkle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (_puff != null) _puff.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        /// <summary>Destroys the material this instance created (the particle objects die with their parent).</summary>
        public void Dispose()
        {
            if (_softMaterial != null) Object.Destroy(_softMaterial);
        }
    }
}
