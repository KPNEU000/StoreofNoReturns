using System.Collections.Generic;
using UnityEngine;

namespace BossFight.Rules
{
    // Plain rules for the body a monster drags: what it collides with, how damage is forwarded, the fixed-pose sway.
    public static class DraggedBodyRules
    {
        public const string LayerName = "DraggedBody";
        private const int LayerCount = 32;

        // Every layer the body layer must ignore: all of them except its own. The ground the body rests on is a
        // floor proxy placed on the body layer itself, so walls, shelves, monsters and the player (all on Default,
        // like the real floor) cannot touch it.
        public static List<int> LayersToIgnore(int bodyLayer, int layerCount = LayerCount)
        {
            var ignored = new List<int>();
            for (int layer = 0; layer < layerCount; layer++)
            {
                if (layer != bodyLayer) ignored.Add(layer);
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

        public static void ConfigureCollisionMatrix(int bodyLayer)
        {
            foreach (var layer in LayersToIgnore(bodyLayer)) Physics.IgnoreLayerCollision(bodyLayer, layer, true);
            Physics.IgnoreLayerCollision(bodyLayer, bodyLayer, false);
        }

        // Height of the floor proxy: where a downward ray from the monster lands, else the monster's own feet.
        public static float FloorHeight(bool hitGround, float groundY, float fallbackY)
        {
            return hitGround ? groundY : fallbackY;
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

        // The box behind a monster a dragged body may occupy, in the monster's local space (x right, z forward):
        // never in front of it, at most `halfWidth` to either side, between `nearBack` and `farBack` behind it.
        public const float BoxHalfWidth = 0.9f;
        public const float BoxNearBack = 0.25f;
        public const float BoxFarBack = 3.2f;

        public static Vector3 ClampBehind(Vector3 local, float halfWidth = BoxHalfWidth, float nearBack = BoxNearBack, float farBack = BoxFarBack)
        {
            local.x = Mathf.Clamp(local.x, -halfWidth, halfWidth);
            local.z = Mathf.Clamp(local.z, -farBack, -nearBack);
            return local;
        }

        public static float SwayAngle(float time, float degrees, float hertz)
        {
            return Mathf.Sin(time * hertz * 2f * Mathf.PI) * degrees;
        }
    }
}
