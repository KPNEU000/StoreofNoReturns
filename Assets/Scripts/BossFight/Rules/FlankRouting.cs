using UnityEngine;

namespace BossFight.Rules
{
    public enum Flank
    {
        Direct,
        Left,
        Right,
        Wide,
    }

    // Picks where an enemy should walk to so a group surrounds the player instead of queueing behind one path.
    public class FlankRouting
    {
        private const float FlankOffsetDistance = 4f;
        private const float SideAngle = 70f;
        private const float WideAngle = 150f;

        private readonly BossFightSettings settings;

        public FlankRouting(BossFightSettings settings)
        {
            this.settings = settings;
        }

        // Cycles Left, Right, Wide, Direct so any group is spread evenly across the flanks.
        public static Flank Assign(int index)
        {
            switch (((index % 4) + 4) % 4)
            {
                case 0: return Flank.Left;
                case 1: return Flank.Right;
                case 2: return Flank.Wide;
                default: return Flank.Direct;
            }
        }

        public static float AngleOf(Flank flank)
        {
            switch (flank)
            {
                case Flank.Left: return -SideAngle;
                case Flank.Right: return SideAngle;
                case Flank.Wide: return WideAngle;
                default: return 0f;
            }
        }

        // Destination = player + (enemy->player offset reversed, rotated by the flank angle).
        // Inside flankDirectRadius, or for Direct, it collapses to the player's position.
        public Vector3 Destination(Vector3 playerPosition, Vector3 enemyPosition, Flank flank)
        {
            var away = enemyPosition - playerPosition;
            away.y = 0f;
            float distance = away.magnitude;
            if (flank == Flank.Direct || distance <= settings.flankDirectRadius)
            {
                return playerPosition;
            }
            var offset = Quaternion.Euler(0f, AngleOf(flank), 0f) * (away / distance) * FlankOffsetDistance;
            return playerPosition + offset;
        }
    }
}
