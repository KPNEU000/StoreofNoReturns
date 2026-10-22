using UnityEngine;

namespace BossFight.Rules
{
    // Where the opening vault lands and how the harmless tutorial costume crosses in front of the player.
    public static class TutorialRules
    {
        // Point on the far side of the desk along `forward`: clear of every desk corner by `clearance`, at the player's height.
        public static Vector3 VaultTarget(Vector3 playerPosition, Vector3 forward, Bounds desk, float clearance)
        {
            forward = Planar.Direction(forward);
            float reach = 0f;
            var min = desk.min;
            var max = desk.max;
            for (int i = 0; i < 4; i++)
            {
                var corner = new Vector3((i & 1) == 0 ? min.x : max.x, 0f, (i & 2) == 0 ? min.z : max.z);
                reach = Mathf.Max(reach, Vector3.Dot(corner - new Vector3(playerPosition.x, 0f, playerPosition.z), forward));
            }
            return playerPosition + forward * (reach + clearance);
        }

        // Endpoints of the pass: `ahead` m in front of the player, `halfWidth` m either side of their line of sight.
        public static void PassEndpoints(Vector3 playerPosition, Vector3 forward, float ahead, float halfWidth, out Vector3 from, out Vector3 to)
        {
            forward = Planar.Direction(forward);
            var right = Vector3.Cross(Vector3.up, forward);
            var centre = playerPosition + forward * ahead;
            from = centre - right * halfWidth;
            to = centre + right * halfWidth;
        }

        // A harmless tutorial costume never reaches the money system.
        public static bool CanHurtPlayer(bool harmless)
        {
            return !harmless;
        }
    }
}
