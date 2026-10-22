using UnityEngine;

namespace BossFight
{
    // Damage-flash tint shared by the enemies and the Witch: base/legacy colour, plus emission when given.
    internal static class RendererTint
    {
        public static void Apply(Renderer[] renderers, MaterialPropertyBlock block, Color color, Color? emission)
        {
            foreach (var r in renderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(block);
                block.SetColor("_BaseColor", color);
                block.SetColor("_Color", color);
                if (emission.HasValue) block.SetColor("_EmissionColor", emission.Value);
                r.SetPropertyBlock(block);
            }
        }
    }
}
