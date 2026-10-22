using UnityEngine;
using UnityEngine.AI;

namespace BossFight.Rules
{
    // Where the Witch stands relative to the entrance, and whether a monster path through it is usable.
    public static class DoorwayRules
    {
        // Floor point beside the doorway: `depth` m inside from the door, shifted sideways (towards the player's side)
        // far enough that the shield dome clears the doorway lane. `inward` points from the door into the store.
        public static Vector3 WitchPosition(Vector3 doorCenter, Vector3 inward, Vector3 playerPosition, float doorHalfWidth, float domeRadius, float depth, float margin)
        {
            inward = Planar.Direction(inward);
            var lateral = Vector3.Cross(Vector3.up, inward);
            var toPlayer = playerPosition - doorCenter;
            float side = Vector3.Dot(toPlayer, lateral) >= 0f ? 1f : -1f;
            float offset = doorHalfWidth + domeRadius + margin;
            var position = doorCenter + inward * depth + lateral * (side * offset);
            position.y = doorCenter.y;
            return position;
        }

        // Horizontal distance from `point` to the lane running from the door straight into the store.
        public static float DistanceFromLane(Vector3 point, Vector3 doorCenter, Vector3 inward)
        {
            inward = Planar.Direction(inward);
            var lateral = Vector3.Cross(Vector3.up, inward);
            return Mathf.Abs(Vector3.Dot(point - doorCenter, lateral));
        }

        // True when a circle of `radius` at `point` does not reach into the lane of half-width `laneHalfWidth`.
        public static bool IsClearOfLane(Vector3 point, float radius, Vector3 doorCenter, Vector3 inward, float laneHalfWidth)
        {
            return DistanceFromLane(point, doorCenter, inward) - radius >= laneHalfWidth;
        }

        // A NavMesh path counts as open only when it is complete and ends at the target, not at a dead end.
        public static bool IsPathComplete(NavMeshPathStatus status, Vector3 pathEnd, Vector3 target, float tolerance)
        {
            return status == NavMeshPathStatus.PathComplete && (pathEnd - target).sqrMagnitude <= tolerance * tolerance;
        }
    }
}
