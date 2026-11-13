// Win confetti and placement glints as real Unity ParticleSystems (burst emission, gravity, spin, fade).
// Story: production/epics/pack-it-perfect/story-008-tutorial-and-juice.md

using UnityEngine;

namespace PackItPerfect
{
    /// <summary>Owns two particle systems under the feedback root: a one-shot win fountain and small placement rays.</summary>
    public sealed class ConfettiBurst
    {
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

        /// <summary>Small radial glints on a correct drop.</summary>
        public void Sparkle(Vector3 position)
        {
            _sparkle.transform.position = position;
            _sparkle.Emit(Mathf.Clamp(_tuning.sparkleCount, 1, 16));
        }

        /// <summary>Fires the win burst: a colourful fountain from <paramref name="position"/> that falls under gravity.</summary>
        public void Celebrate(Vector3 position)
        {
            _confetti.transform.position = position;
            _confetti.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _confetti.Play(true);
        }

        /// <summary>Immediately removes effects on a new level or when motion reduction is enabled.</summary>
        public void Clear()
        {
            if (_confetti != null) _confetti.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (_sparkle != null) _sparkle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        /// <summary>Kept for the owner's update loop; the particle systems simulate themselves.</summary>
        public void Tick(float deltaTime)
        {
        }
    }
}
