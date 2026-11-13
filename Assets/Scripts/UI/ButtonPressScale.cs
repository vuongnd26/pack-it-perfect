// Press-in feedback for buttons built by UIFactory: the visual shrinks slightly and the face sinks into its base
// while the pointer is held, then springs back. Presentation only; respects UITuning.reduceMotion.

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PackItPerfect.UI
{
    /// <summary>Scales a button's visual down while it is pressed. Added by <see cref="UIFactory.Button"/>.</summary>
    [DisallowMultipleComponent]
    public sealed class ButtonPressScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        Button button;
        RectTransform visual;
        RectTransform face;
        UITuning tuning;
        float amount;
        bool pressed;

        /// <summary>Binds the parts to animate.</summary>
        /// <param name="button">Button whose interactable state gates the effect.</param>
        /// <param name="visual">Rect scaled about its centre.</param>
        /// <param name="face">Face rect that sinks towards the base.</param>
        /// <param name="tuning">Press scale, duration, sink and the motion preference.</param>
        public void Init(Button button, RectTransform visual, RectTransform face, UITuning tuning)
        {
            this.button = button;
            this.visual = visual;
            this.face = face;
            this.tuning = tuning;
            enabled = true;
        }

        /// <inheritdoc/>
        public void OnPointerDown(PointerEventData eventData)
        {
            if (button != null && button.IsInteractable())
            {
                pressed = true;
                enabled = true;
            }
        }

        /// <inheritdoc/>
        public void OnPointerUp(PointerEventData eventData) => Release();

        /// <inheritdoc/>
        public void OnPointerExit(PointerEventData eventData) => Release();

        void Release()
        {
            pressed = false;
            enabled = true;
        }

        void OnDisable()
        {
            // A hidden screen must not come back half-pressed.
            pressed = false;
            amount = 0f;
            if (visual != null && face != null && tuning != null) Apply();
        }

        void Update()
        {
            if (tuning == null) return;
            if (button == null || !button.IsInteractable()) pressed = false;
            float target = pressed ? 1f : 0f;
            if (Mathf.Approximately(amount, target)) return;
            if (tuning.reduceMotion || tuning.buttonPressDuration <= 0f)
            {
                amount = target;
            }
            else
            {
                amount = Mathf.MoveTowards(amount, target, Time.unscaledDeltaTime / tuning.buttonPressDuration);
            }
            Apply();
            // Keep the event receiver enabled at rest; disabled behaviours do not receive pointer callbacks.
        }

        void Apply()
        {
            float eased = amount * amount * (3f - 2f * amount);
            float scale = tuning.reduceMotion ? 1f : Mathf.Lerp(1f, tuning.buttonPressScale, eased);
            visual.localScale = new Vector3(scale, scale, 1f);
            float sink = tuning.buttonShadowDepth * tuning.buttonPressSink * eased;
            face.offsetMin = new Vector2(face.offsetMin.x, tuning.buttonShadowDepth - sink);
            face.offsetMax = new Vector2(face.offsetMax.x, -sink);
        }
    }
}
