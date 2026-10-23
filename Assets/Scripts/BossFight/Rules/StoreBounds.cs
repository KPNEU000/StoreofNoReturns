using UnityEngine;

namespace BossFight.Rules
{
    // Where the player is allowed to be: inside the store's footprint (the ceiling's extent on the ground plane)
    // and not falling below the floor. Used to undo any shove through a wall.
    public static class StoreBounds
    {
        // How far below the last safe height counts as falling out of the map.
        public const float FallDistance = 2.5f;

        public static Rect Footprint(Bounds ceiling)
        {
            return Rect.MinMaxRect(ceiling.min.x, ceiling.min.z, ceiling.max.x, ceiling.max.z);
        }

        // Inside the footprint shrunk by `margin` (negative margin grows it).
        public static bool Inside(Rect footprint, Vector3 position, float margin)
        {
            return position.x >= footprint.xMin + margin && position.x <= footprint.xMax - margin
                && position.z >= footprint.yMin + margin && position.z <= footprint.yMax - margin;
        }

        public static bool Fallen(float y, float safeY) => y < safeY - FallDistance;

        // Points across the store the player must be able to see at least one of (over the racks, under the ceiling)
        // to count as inside. The store's front corners are indented, so pockets inside the footprint rectangle are
        // really outside; from there every sight line crosses a wall.
        public static Vector3[] SightAnchors(Rect footprint, float height)
        {
            var c = footprint.center;
            float dx = footprint.width * 0.3f;
            float dz = footprint.height * 0.3f;
            return new[]
            {
                new Vector3(c.x, height, c.y),
                new Vector3(c.x - dx, height, c.y - dz),
                new Vector3(c.x + dx, height, c.y - dz),
                new Vector3(c.x - dx, height, c.y + dz),
                new Vector3(c.x + dx, height, c.y + dz),
            };
        }

        // Consecutive failed sight checks before the player is put back (a single blocked check is not enough).
        public const int OutsideChecksBeforeRestore = 3;

        // True when the player must be put back at their last safe spot.
        public static bool MustRestore(Rect footprint, bool hasFootprint, Vector3 position, float safeY)
        {
            if (Fallen(position.y, safeY)) return true;
            return hasFootprint && !Inside(footprint, position, -0.2f);
        }
    }
}
