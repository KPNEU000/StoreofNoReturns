using UnityEngine;

namespace BossFight
{
    // The Witch's shield is a flat slab, not a dome, so it turns about the vertical axis to keep its face towards the
    // player wherever the Witch stands. restRotation is the slab's local pose on the Witch (face along her forward).
    public class ShieldFacesPlayer : MonoBehaviour
    {
        [SerializeField]
        private Transform player;
        [SerializeField]
        private Quaternion restRotation = Quaternion.identity;

        private void Awake()
        {
            if (player == null)
            {
                var movement = FindFirstObjectByType<PlayerMovement>();
                if (movement != null) player = movement.transform;
            }
        }

        private void LateUpdate()
        {
            if (player == null) return;
            var toPlayer = player.position - transform.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude < 1e-4f) return;
            transform.rotation = Quaternion.LookRotation(toPlayer.normalized) * restRotation;
        }
    }
}
