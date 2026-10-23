using UnityEngine;

namespace BossFight.Rules
{
    // Environment lighting that blackout drives (the ceiling lights are only part of the darkness).
    public struct EnvironmentLight
    {
        public float ambientIntensity;
        public Color ambientLight;
        public Color ambientSky;
        public Color ambientEquator;
        public Color ambientGround;
        public float reflectionIntensity;
        public Color fogColor;
    }

    // Target ambient/reflection/fog values for a store light fade (1 = lit, 0 = Off tier), from the originals.
    public class BlackoutRules
    {
        private readonly BossFightSettings settings;

        public BlackoutRules(BossFightSettings settings)
        {
            this.settings = settings;
        }

        // Brightness factor applied to the originals: 1 while lit, blackoutEnvironmentFactor at full blackout.
        public float Factor(float fade) => Mathf.Lerp(settings.blackoutEnvironmentFactor, 1f, Mathf.Clamp01(fade));

        public EnvironmentLight Target(EnvironmentLight original, float fade)
        {
            float f = Factor(fade);
            return new EnvironmentLight
            {
                ambientIntensity = original.ambientIntensity * f,
                ambientLight = Scale(original.ambientLight, f),
                ambientSky = Scale(original.ambientSky, f),
                ambientEquator = Scale(original.ambientEquator, f),
                ambientGround = Scale(original.ambientGround, f),
                reflectionIntensity = original.reflectionIntensity * f,
                fogColor = Scale(original.fogColor, f),
            };
        }

        private static Color Scale(Color c, float f) => new Color(c.r * f, c.g * f, c.b * f, c.a);
    }
}
