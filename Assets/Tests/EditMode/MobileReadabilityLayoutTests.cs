using NUnit.Framework;
using PackItPerfect.UI;
using UnityEngine;

namespace PackItPerfect.Tests
{
    /// <summary>Readability contracts in reference units and rendered pixels, not unverified physical dp.</summary>
    public sealed class MobileReadabilityLayoutTests
    {
        const float MinimumNamePixels = 18f;
        const float MinimumCountPixels = 20f;
        static readonly Vector2 MinimumPortrait = new Vector2(480f, 800f);

        [Test]
        public void DockTypography_MinimumPortrait_DoesNotShrinkBelowReadableFloor()
        {
            var tuning = new UITuning();
            float widthScale = MinimumPortrait.x / tuning.referenceResolution.x;
            float heightScale = MinimumPortrait.y / tuning.referenceResolution.y;
            float scale = Mathf.Pow(2f, Mathf.Lerp(Mathf.Log(widthScale, 2f), Mathf.Log(heightScale, 2f), tuning.matchWidthOrHeight));

            Assert.That(tuning.dockLabelFontSize * scale, Is.GreaterThanOrEqualTo(MinimumNamePixels), "Booster action names must be readable at the minimum portrait render size.");
            Assert.That(tuning.badgeFontSize * scale, Is.GreaterThanOrEqualTo(MinimumCountPixels), "Booster quantities must have a separate readable floor.");
        }
    }
}
