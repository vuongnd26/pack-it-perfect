using UnityEngine;

namespace PackItPerfect
{
    /// <summary>Purely visual pointing hand; demonstrates a path without touching board state or consuming input.</summary>
    public sealed class TutorialHand
    {
        readonly Transform _root;
        readonly FeelTuning _tuning;
        float _age;

        /// <summary>Builds a small outlined hand from a bounded number of sprites.</summary>
        public TutorialHand(SpriteFactory factory, Transform parent, FeelTuning tuning)
        {
            _tuning = tuning;
            _root = new GameObject("Tutorial hand").transform;
            _root.SetParent(parent, false);
            // Local y=0 is the pointing fingertip, with the palm below it.
            Part(factory, "Palm outline", new Vector2(0.14f, -0.56f), new Vector2(0.65f, 0.7f), tuning.outlineColor, 55);
            Part(factory, "Finger outline", new Vector2(0f, -0.18f), new Vector2(0.24f, 0.75f), tuning.outlineColor, 55);
            Part(factory, "Thumb outline", new Vector2(0.43f, -0.47f), new Vector2(0.34f, 0.27f), tuning.outlineColor, 55);
            Part(factory, "Palm", new Vector2(0.14f, -0.56f), new Vector2(0.53f, 0.58f), tuning.handColor, 56);
            Part(factory, "Finger", new Vector2(0f, -0.18f), new Vector2(0.13f, 0.64f), tuning.handColor, 56);
            Part(factory, "Thumb", new Vector2(0.4f, -0.47f), new Vector2(0.27f, 0.16f), tuning.handColor, 56);
            _root.localScale = Vector3.one * Mathf.Max(0.1f, tuning.handSize);
            Hide();
        }

        void Part(SpriteFactory factory, string name, Vector2 center, Vector2 size, Color color, int order) =>
            factory.CreateQuad(_root, name, center, size, color, order);

        /// <summary>Restarts the demonstration cycle for a freshly loaded tutorial level.</summary>
        public void Reset() { _age = 0f; Hide(); }

        /// <summary>Hides the hand (first placement, another level, input lock, or reduced motion).</summary>
        public void Hide() => _root.gameObject.SetActive(false);

        /// <summary>Moves only the hand along the supplied tray-to-solution path.</summary>
        public void Tick(float deltaTime, Vector3 from, Vector3 to)
        {
            _age += deltaTime;
            float phase = Mathf.Repeat(_age / Mathf.Max(0.1f, _tuning.tutorialPeriod), 1f);
            float k = Mathf.Clamp01(phase / Mathf.Max(0.1f, _tuning.tutorialTravelFraction));
            k = k * k * (3f - 2f * k);
            _root.gameObject.SetActive(true);
            _root.position = Vector3.Lerp(from, to, k);
        }
    }
}
