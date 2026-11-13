using UnityEngine;

namespace PackItPerfect
{
    /// <summary>Reusable fixed-capacity sprite bank for small placement rays and a one-shot win shower.</summary>
    public sealed class ConfettiBurst
    {
        sealed class Particle
        {
            public SpriteRenderer renderer;
            public Vector3 origin;
            public Vector2 velocity;
            public Color color;
            public float age, lifetime, gravity, spin, angle;
            public bool active;
        }

        readonly Particle[] _particles;
        readonly FeelTuning _tuning;
        readonly System.Random _random = new System.Random();
        int _cursor;

        /// <summary>Allocates one bounded pool under the feedback root, never in an Update or placement callback.</summary>
        public ConfettiBurst(SpriteFactory factory, Transform parent, FeelTuning tuning)
        {
            _tuning = tuning;
            _particles = new Particle[Mathf.Clamp(tuning.particleCapacity, 16, 96)];
            for (int i = 0; i < _particles.Length; i++)
            {
                var renderer = factory.CreateQuad(parent, "Celebration particle", Vector2.zero, Vector2.one, Color.white, 49);
                renderer.enabled = false;
                _particles[i] = new Particle { renderer = renderer };
            }
        }

        float Range(float low, float high) => Mathf.Lerp(low, high, (float)_random.NextDouble());

        Particle Emit(Vector3 position, Vector2 size, Color color, float lifetime)
        {
            var p = _particles[_cursor];
            _cursor = (_cursor + 1) % _particles.Length;
            p.origin = position;
            p.color = color;
            p.age = 0f;
            p.lifetime = Mathf.Max(0.01f, lifetime);
            p.gravity = p.spin = p.angle = 0f;
            p.active = true;
            p.renderer.enabled = true;
            p.renderer.color = color;
            p.renderer.transform.SetPositionAndRotation(position, Quaternion.identity);
            p.renderer.transform.localScale = new Vector3(size.x, size.y, 1f);
            return p;
        }

        /// <summary>Small radial glints on a correct drop.</summary>
        public void Sparkle(Vector3 position)
        {
            int count = Mathf.Clamp(_tuning.sparkleCount, 1, 16);
            for (int i = 0; i < count; i++)
            {
                float angle = (i + Range(0f, 0.25f)) * Mathf.PI * 2f / count;
                var p = Emit(position, _tuning.sparkleSize, _tuning.sparkleColor, _tuning.sparkleDuration);
                p.velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) *
                    (_tuning.sparkleTravel / Mathf.Max(0.01f, _tuning.sparkleDuration));
                p.angle = angle * Mathf.Rad2Deg;
            }
        }

        /// <summary>Emits a colourful, bounded win shower around the supplied centre.</summary>
        public void Celebrate(Vector3 position)
        {
            int count = Mathf.Clamp(_tuning.confettiCount, 1, _particles.Length);
            for (int i = 0; i < count; i++)
            {
                var colors = _tuning.confettiColors;
                var color = colors != null && colors.Length > 0 ? colors[i % colors.Length] : _tuning.sparkleColor;
                var p = Emit(position, _tuning.confettiSize, color, _tuning.confettiDuration);
                p.velocity = new Vector2(Range(-_tuning.confettiSpeed.x, _tuning.confettiSpeed.x),
                    Range(_tuning.confettiSpeed.y * 0.5f, _tuning.confettiSpeed.y));
                p.gravity = _tuning.confettiGravity;
                p.spin = Range(-_tuning.confettiSpin, _tuning.confettiSpin);
                p.angle = Range(0f, 360f);
            }
        }

        /// <summary>Immediately removes effects on a new level or when motion reduction is enabled.</summary>
        public void Clear()
        {
            foreach (var p in _particles) { p.active = false; p.renderer.enabled = false; }
        }

        /// <summary>Advances active particles without allocating.</summary>
        public void Tick(float deltaTime)
        {
            foreach (var p in _particles)
            {
                if (!p.active) continue;
                p.age += deltaTime;
                if (p.age >= p.lifetime) { p.active = false; p.renderer.enabled = false; continue; }
                p.renderer.transform.position = p.origin + (Vector3)(p.velocity * p.age) -
                    Vector3.up * (p.gravity * p.age * p.age / 2f);
                p.renderer.transform.localRotation = Quaternion.Euler(0f, 0f, p.angle + p.spin * p.age);
                var color = p.color;
                color.a *= 1f - p.age / p.lifetime;
                p.renderer.color = color;
            }
        }
    }
}
