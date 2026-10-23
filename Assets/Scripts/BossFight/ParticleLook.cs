using UnityEngine;
using UnityEngine.Rendering;

namespace BossFight
{
    // One soft, round, alpha-blended particle material shared by every boss effect. Without a texture the URP
    // particle shader draws each particle as a hard square (playtest round 11: purple squares in the air).
    // Particle start colours tint it.
    public static class ParticleLook
    {
        private static Material soft;

        public static Material Soft()
        {
            if (soft != null) return soft;
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) return null;
            soft = new Material(shader) { name = "BossSoftParticle" };
            soft.SetTexture("_BaseMap", SoftDot(64));
            soft.SetColor("_BaseColor", Color.white);
            soft.SetFloat("_Surface", 1f);
            soft.SetFloat("_Blend", 0f);
            soft.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            soft.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            soft.SetFloat("_ZWrite", 0f);
            soft.SetOverrideTag("RenderType", "Transparent");
            soft.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            soft.renderQueue = (int)RenderQueue.Transparent;
            return soft;
        }

        public static void Apply(ParticleSystem system)
        {
            var material = Soft();
            if (system == null || material == null) return;
            system.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
        }

        // White dot whose alpha falls smoothly from the centre to nothing at the edge.
        private static Texture2D SoftDot(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            float half = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - half) * (x - half) + (y - half) * (y - half)) / half;
                    float a = Mathf.Clamp01(1f - d);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(255 * a * a));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }
    }
}
