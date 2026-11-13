using System;
using UnityEngine;

namespace PackItPerfect
{
    /// <summary>Serialized presentation-only values; lengths are grid-cell world units.</summary>
    [Serializable]
    public sealed class FeelTuning
    {
        [Header("Placement")]
        [Tooltip("Duration of the small centre-anchored placement bounce.")]
        [Min(0.01f)] public float bounceDuration = 0.24f;
        [Tooltip("Maximum fractional expansion of a placed item.")]
        [Range(0f, 0.2f)] public float bounceAmount = 0.07f;
        [Tooltip("Small rays per correct placement.")]
        [Range(1, 16)] public int sparkleCount = 7;
        [Tooltip("Seconds a placement ray remains visible.")]
        [Min(0.01f)] public float sparkleDuration = 0.38f;
        [Tooltip("Distance travelled by placement rays.")]
        public float sparkleTravel = 0.65f;
        [Tooltip("Length and width of a placement ray.")]
        public Vector2 sparkleSize = new Vector2(0.16f, 0.055f);
        [Tooltip("Placement ray colour.")]
        public Color sparkleColor = new Color(1f, 0.85f, 0.36f);
        [Tooltip("Offset of praise from the placed item centre.")]
        public Vector2 praiseOffset = new Vector2(0f, 0.55f);

        [Header("Tutorial")]
        [Tooltip("Only this introductory level displays the hand.")]
        public int tutorialLevel = 1;
        [Tooltip("Duration of one tray-to-solution demonstration, including pause.")]
        [Min(0.1f)] public float tutorialPeriod = 2.4f;
        [Tooltip("Fraction of the cycle spent travelling; remainder holds at the target.")]
        [Range(0.1f, 1f)] public float tutorialTravelFraction = 0.7f;
        [Tooltip("Scale of the procedural pointing hand.")]
        public float handSize = 0.48f;
        [Tooltip("Hand fill colour.")]
        public Color handColor = new Color(1f, 0.92f, 0.76f);

        [Header("Celebration")]
        [Tooltip("Maximum reusable sprites across confetti and placement rays.")]
        [Range(16, 96)] public int particleCapacity = 64;
        [Tooltip("Confetti pieces emitted per win (clamped to pool size).")]
        [Range(1, 80)] public int confettiCount = 48;
        [Tooltip("Seconds the confetti lasts.")]
        [Min(0.01f)] public float confettiDuration = 1.8f;
        [Tooltip("Initial horizontal/vertical confetti speed.")]
        public Vector2 confettiSpeed = new Vector2(3.5f, 5f);
        [Tooltip("Downward confetti acceleration.")]
        public float confettiGravity = 5f;
        [Tooltip("Confetti rectangle size.")]
        public Vector2 confettiSize = new Vector2(0.12f, 0.23f);
        [Tooltip("Maximum angular speed in degrees per second.")]
        public float confettiSpin = 220f;
        [Tooltip("Closed visual palette (not player-facing text).")]
        public Color[] confettiColors = { new Color(1f, 0.68f, 0.2f), new Color(0.94f, 0.35f, 0.56f),
            new Color(0.28f, 0.73f, 0.63f), new Color(0.49f, 0.55f, 0.9f) };

        [Header("Surface and exterior outlines")]
        [Tooltip("Texture resolution per cell; shared per shape and pattern for the current level.")]
        [Range(16, 48)] public int pixelsPerCell = 32;
        [Tooltip("Dark exterior border thickness in cell units.")]
        [Range(0.02f, 0.12f)] public float outlineWidth = 0.055f;
        [Tooltip("Exterior piece and container outline colour.")]
        public Color outlineColor = new Color(0.16f, 0.14f, 0.21f);
        [Tooltip("Fractional darkening of surface patterns.")]
        [Range(0f, 0.4f)] public float patternContrast = 0.14f;
        [Tooltip("Pattern repeats in one cell.")]
        [Range(1, 6)] public int patternRepeats = 3;
        [Tooltip("Container handle width and height.")]
        public Vector2 handleSize = new Vector2(1.3f, 0.3f);
        [Tooltip("Handle stroke thickness.")]
        public float handleStroke = 0.12f;
        [Tooltip("Wheel width and height.")]
        public Vector2 wheelSize = new Vector2(0.28f, 0.32f);
        [Tooltip("Wheel inset from each side of the container.")]
        public float wheelInset = 0.5f;

        [Header("Android haptics")]
        [Tooltip("Enable Android system light tactile feedback when supported and enabled by the user.")]
        public bool haptics = true;
    }
}
