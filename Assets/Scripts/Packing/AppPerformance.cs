// Device-level runtime settings for a smooth 2D game on phones: frame pacing, no unused physics steps, no 2D lights.
// Applied once by PackingGame.Awake. Presentation and platform only: no game rules live here.

using System;
using UnityEngine;

namespace PackItPerfect
{
    /// <summary>One-time frame-rate, physics and lighting setup. Safe to call more than once.</summary>
    public static class AppPerformance
    {
        /// <summary>Frames per second the game asks for. Android and iOS default to 30 when nothing is set.</summary>
        public const int TARGET_FRAME_RATE = 60;

        const string LIGHT_2D_TYPE = "UnityEngine.Rendering.Universal.Light2D, Unity.RenderPipelines.Universal.2D.Runtime";
        const string LIGHT_2D_TYPE_LEGACY = "UnityEngine.Rendering.Universal.Light2D, Unity.RenderPipelines.Universal.Runtime";

        static bool applied;

        /// <summary>Sets the frame-rate target and stops the physics engines this game never uses from stepping.</summary>
        public static void Apply()
        {
            if (applied) return;
            applied = true;
            // vSync off + an explicit target lets Android frame pacing (Swappy) hold a steady 60 instead of the 30 default.
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = TARGET_FRAME_RATE;
            // Nothing in the game uses rigid bodies or colliders: skip both fixed-step simulations.
            Physics.simulationMode = SimulationMode.Script;
            Physics2D.simulationMode = SimulationMode2D.Script;
            if (Application.isMobilePlatform) Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        /// <summary>
        /// Turns off every 2D light in the loaded scenes. All world sprites use an unlit material, so a light would only
        /// make the 2D renderer draw light textures nobody samples.
        /// </summary>
        public static void DisableSceneLights()
        {
            var type = Type.GetType(LIGHT_2D_TYPE) ?? Type.GetType(LIGHT_2D_TYPE_LEGACY);
            if (type == null) return;
            foreach (var found in UnityEngine.Object.FindObjectsByType(type))
            {
                if (found is Behaviour light) light.enabled = false;
            }
        }
    }
}
