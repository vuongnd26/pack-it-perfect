// Tutorial hand: an illustrated pointing hand (Resources/Art/hand) that demonstrates the first drag.
// Story: production/epics/pack-it-perfect/story-008-tutorial-and-juice.md

using UnityEngine;

namespace PackItPerfect
{
    /// <summary>Purely visual pointing hand; demonstrates a path without touching board state or consuming input.</summary>
    public sealed class TutorialHand
    {
        const int SORTING_ORDER = 56;
        const float ART_HEIGHT = 2.4f;                 // art height in root-local units (root is scaled by handSize)
        const float ART_PIXEL_HEIGHT = 520f;
        // Fingertip offset from the art centre, in art pixels (x right, y up).
        static readonly Vector2 FingertipPixels = new Vector2(-96f, 216f);
        const float PRESS_SCALE = 0.88f;               // the hand dips while it "grabs" and "drops"
        const float PRESS_SHARE = 0.14f;

        readonly Transform _root;
        readonly FeelTuning _tuning;
        float _age;

        /// <summary>Builds the hand. Local origin is the pointing fingertip.</summary>
        public TutorialHand(SpriteFactory factory, Transform parent, FeelTuning tuning)
        {
            _tuning = tuning;
            _root = new GameObject("Tutorial hand").transform;
            _root.SetParent(parent, false);
            var art = factory.GetArt("hand");
            if (art != null)
            {
                var offset = -FingertipPixels * (ART_HEIGHT / ART_PIXEL_HEIGHT);
                factory.CreateSprite(_root, "Hand", art, offset, new Vector2(ART_HEIGHT, ART_HEIGHT), Color.white, SORTING_ORDER);
            }
            else
            {
                // Fallback without the art: a plain outlined pointer.
                factory.CreateQuad(_root, "Finger outline", new Vector2(0f, -0.4f), new Vector2(0.34f, 0.9f), tuning.outlineColor, SORTING_ORDER - 1);
                factory.CreateQuad(_root, "Finger", new Vector2(0f, -0.4f), new Vector2(0.22f, 0.78f), tuning.handColor, SORTING_ORDER);
            }
            _root.localScale = Vector3.one * Mathf.Max(0.1f, tuning.handSize);
            Hide();
        }

        /// <summary>Restarts the demonstration cycle for a freshly loaded tutorial level.</summary>
        public void Reset() { _age = 0f; Hide(); }

        /// <summary>Hides the hand (first placement, another level, input lock, or reduced motion).</summary>
        public void Hide() => _root.gameObject.SetActive(false);

        /// <summary>Moves only the hand along the supplied tray-to-solution path, dipping as it grabs and drops.</summary>
        public void Tick(float deltaTime, Vector3 from, Vector3 to)
        {
            _age += deltaTime;
            float phase = Mathf.Repeat(_age / Mathf.Max(0.1f, _tuning.tutorialPeriod), 1f);
            float travel = Mathf.Clamp01(phase / Mathf.Max(0.1f, _tuning.tutorialTravelFraction));
            float k = travel * travel * (3f - 2f * travel);
            float press = Mathf.Max(1f - travel / PRESS_SHARE, (travel - (1f - PRESS_SHARE)) / PRESS_SHARE);
            float scale = Mathf.Lerp(1f, PRESS_SCALE, Mathf.Clamp01(press)) * Mathf.Max(0.1f, _tuning.handSize);
            _root.gameObject.SetActive(true);
            _root.position = Vector3.Lerp(from, to, k);
            _root.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
