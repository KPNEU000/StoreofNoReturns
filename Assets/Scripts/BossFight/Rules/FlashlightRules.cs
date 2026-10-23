using UnityEngine;

namespace BossFight.Rules
{
    // Flashlight brightness as a 0..1 fraction (faint glow .. full). Linear ramps between the two.
    public class FlashlightRules
    {
        private readonly BossFightSettings settings;

        // Tutorial makes the beam extra bright; 1 = normal.
        public float BrightMultiplier { get; set; } = 1f;

        public FlashlightRules(BossFightSettings settings)
        {
            this.settings = settings;
        }

        public float Faint => settings.flashlightFaintGlow;

        // Playtest round 5: no button. The beam comes on by itself once the store lights go out (money <= the
        // blackout threshold) and stays on; slashing still drops it to the faint glow.
        public bool ShouldBeOn(float money) => money <= settings.blackoutMoney;

        public float Step(float current, bool held, bool slashing, float dt)
        {
            if (slashing) return Faint;
            float span = 1f - Faint;
            if (held)
            {
                float up = settings.flashlightRampUpSeconds <= 0f ? span : span * dt / settings.flashlightRampUpSeconds;
                return Mathf.Min(1f, current + up);
            }
            float down = settings.flashlightRampDownSeconds <= 0f ? span : span * dt / settings.flashlightRampDownSeconds;
            return Mathf.Max(Faint, current - down);
        }

        public float Intensity(float fraction) => fraction * settings.flashlightFullIntensity * BrightMultiplier;
    }
}
