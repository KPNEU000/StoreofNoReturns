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

        // True when the player must be put back at their last safe spot.
        public static bool MustRestore(Rect footprint, bool hasFootprint, Vector3 position, float safeY)
        {
            if (Fallen(position.y, safeY)) return true;
            return hasFootprint && !Inside(footprint, position, -0.2f);
        }
    }
}
