using BossFight.Rules;
using UnityEngine;
using UnityEngine.AI;

namespace BossFight
{
    // Added to the player by BossFightRig. Enemies can no longer push the player (EnemyContact), but if anything
    // ever moves them through a wall or off the floor, they are put back at their last safe spot inside the store.
    public class PlayerBoundsGuard : MonoBehaviour
    {
        private const float RecordInterval = 0.2f;

        private CharacterController controller;
        private Rect footprint;
        private bool hasFootprint;
        private Vector3 lastSafe;
        private bool hasSafe;
        private float nextRecord;

        public void Init(Bounds? ceiling)
        {
            hasFootprint = ceiling.HasValue;
            if (hasFootprint) footprint = StoreBounds.Footprint(ceiling.Value);
        }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        private void LateUpdate()
        {
            var position = transform.position;
            if (hasSafe && StoreBounds.MustRestore(footprint, hasFootprint, position, lastSafe.y))
            {
                Restore();
                return;
            }
            if (Time.time < nextRecord) return;
            nextRecord = Time.time + RecordInterval;
            bool grounded = controller == null || controller.isGrounded;
            bool inside = !hasFootprint || StoreBounds.Inside(footprint, position, 0.25f);
            if (grounded && inside && NavMesh.SamplePosition(position, out _, 2.5f, NavMesh.AllAreas))
            {
                lastSafe = position;
                hasSafe = true;
            }
        }

        private void Restore()
        {
            // A CharacterController overrides transform moves while enabled.
            if (controller != null) controller.enabled = false;
            transform.position = lastSafe;
            if (controller != null) controller.enabled = true;
        }
    }
}
