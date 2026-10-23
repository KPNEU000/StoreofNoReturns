using UnityEngine;
using UnityEngine.Rendering;

namespace BossFight
{
    // One particle material shared by every boss effect: solid, hard-edged square particles. Round 11 tried soft
    // round dots; the playtest preferred the blocky look, which suits the PSX style. Particle start colours tint it.
    public static class ParticleLook
    {
        private static Material blocky;

        public static Material Blocky()
        {
            if (blocky != null) return blocky;
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) return null;
            blocky = new Material(shader) { name = "BossBlockyParticle" };
            blocky.SetTexture("_BaseMap", Texture2D.whiteTexture);
            blocky.SetColor("_BaseColor", Color.white);
            blocky.SetFloat("_Surface", 1f);
            blocky.SetFloat("_Blend", 0f);
            blocky.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            blocky.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            blocky.SetFloat("_ZWrite", 0f);
            blocky.SetOverrideTag("RenderType", "Transparent");
            blocky.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            blocky.renderQueue = (int)RenderQueue.Transparent;
            return blocky;
        }

        public static void Apply(ParticleSystem system)
        {
            var material = Blocky();
            if (system == null || material == null) return;
            system.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
        }
    }
}
