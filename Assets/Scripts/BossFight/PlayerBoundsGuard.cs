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
        private int outsideChecks;
        private float floorY = float.NaN;
        // Sight lines run this high above the floor: over racks, counter and chunks, under the ceiling.
        private const float SightHeight = 3.2f;

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
            bool inFootprint = !hasFootprint || StoreBounds.Inside(footprint, position, 0.25f);
            bool seesStore = SeesIntoStore(position);
            outsideChecks = seesStore ? 0 : outsideChecks + 1;
            if (hasSafe && outsideChecks >= StoreBounds.OutsideChecksBeforeRestore)
            {
                outsideChecks = 0;
                Restore();
                return;
            }
            if (grounded && inFootprint && seesStore && NavMesh.SamplePosition(position, out var hit, 2.5f, NavMesh.AllAreas))
            {
                lastSafe = position;
                hasSafe = true;
                if (float.IsNaN(floorY)) floorY = hit.position.y;
            }
        }

        private bool SeesIntoStore(Vector3 position)
        {
            if (!hasFootprint) return true;
            float height = (float.IsNaN(floorY) ? position.y - 1f : floorY) + SightHeight;
            var eye = new Vector3(position.x, height, position.z);
            foreach (var anchor in StoreBounds.SightAnchors(footprint, height))
            {
                if (!BlockedByWall(eye, anchor)) return true;
            }
            return false;
        }

        // Only static scenery counts as a wall: enemies, bodies, books and the player never block a sight line.
        private static bool BlockedByWall(Vector3 from, Vector3 to)
        {
            var delta = to - from;
            float length = delta.magnitude;
            if (length < 0.01f) return false;
            foreach (var hit in Physics.RaycastAll(from, delta / length, length, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.rigidbody != null) continue;
                if (hit.collider.GetComponentInParent<NavMeshAgent>() != null) continue;
                if (hit.collider.GetComponentInParent<CharacterController>() != null) continue;
                return true;
            }
            return false;
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
