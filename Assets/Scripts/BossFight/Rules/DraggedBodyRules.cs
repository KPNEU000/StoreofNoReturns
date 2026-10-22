using System.Collections.Generic;
using UnityEngine;

namespace BossFight.Rules
{
    // Plain rules for the body a monster drags: what it collides with, how damage is forwarded, the fixed-pose sway.
    public static class DraggedBodyRules
    {
        public const string LayerName = "DraggedBody";
        private const int LayerCount = 32;

        // Every layer the body layer must ignore: all of them except the ground (the body's own layer included,
        // so its limbs do not fight each other).
        public static List<int> LayersToIgnore(int bodyLayer, int groundLayer, int layerCount = LayerCount)
        {
            var ignored = new List<int>();
            for (int layer = 0; layer < layerCount; layer++)
            {
                if (layer != groundLayer) ignored.Add(layer);
            }
            return ignored;
        }

        // The layer named DraggedBody, or the highest unnamed layer when the project has not defined it.
        public static int ResolveLayer()
        {
            int named = LayerMask.NameToLayer(LayerName);
            if (named >= 0) return named;
            for (int layer = LayerCount - 1; layer >= 8; layer--)
            {
                if (string.IsNullOrEmpty(LayerMask.LayerToName(layer))) return layer;
            }
            return 0;
        }

        public static void ConfigureCollisionMatrix(int bodyLayer, int groundLayer = 0)
        {
            foreach (var layer in LayersToIgnore(bodyLayer, groundLayer))
            {
                Physics.IgnoreLayerCollision(bodyLayer, layer, true);
            }
            Physics.IgnoreLayerCollision(bodyLayer, groundLayer, false);
        }

        // Damage dealt to the body is dealt, unchanged, to its monster. Returns false when there is nobody to hurt.
        public static bool Forward(IDamageable monster, float amount, Vector3 from)
        {
            if (monster == null || !monster.IsAlive) return false;
            monster.TakeDamage(amount, from);
            return true;
        }

        public static Vector3 JoltDirection(Vector3 from, Vector3 bodyPoint)
        {
            var direction = bodyPoint - from;
            return direction.sqrMagnitude > 1e-6f ? direction.normalized : Vector3.up;
        }

        public static float SwayAngle(float time, float degrees, float hertz)
        {
            return Mathf.Sin(time * hertz * 2f * Mathf.PI) * degrees;
        }
    }
}
