using UnityEngine;

namespace BossFight
{
    // The CharacterController pushes itself out of any collider it overlaps, so enemies walking or lunging into the
    // player shoved them around, and through the wall when standing against it. Enemy colliders now ignore the
    // player entirely; hits never depended on that contact (they are distance checks).
    public static class EnemyContact
    {
        private static CharacterController player;

        public static void IgnorePlayer(GameObject enemy)
        {
            if (enemy == null) return;
            if (player == null)
            {
                var movement = Object.FindFirstObjectByType<PlayerMovement>();
                if (movement != null) player = movement.GetComponent<CharacterController>();
            }
            if (player == null) return;
            foreach (var collider in enemy.GetComponentsInChildren<Collider>(true))
            {
                if (collider != player) Physics.IgnoreCollision(player, collider, true);
            }
        }
    }
}
