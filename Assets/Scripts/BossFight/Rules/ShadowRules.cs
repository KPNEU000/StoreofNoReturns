using System.Collections.Generic;
using UnityEngine;

namespace BossFight.Rules
{
    // During the fight only the flashlight beam casts a realtime shadow; every other light loses its shadow map.
    public static class ShadowRules
    {
        public static bool IsFlashlight(Light light)
        {
            return light != null && light.GetComponentInParent<PlayerFlashlight>() != null;
        }

        // Returns how many lights had their shadows switched off.
        public static int RestrictToFlashlight(IEnumerable<Light> lights)
        {
            int changed = 0;
            foreach (var light in lights)
            {
                if (light == null || IsFlashlight(light) || light.shadows == LightShadows.None) continue;
                light.shadows = LightShadows.None;
                changed++;
            }
            return changed;
        }
    }
}
