using UnityEngine;

namespace BossFight.Rules
{
    // How high a possessed rack rises (playtest round 8): far enough that its bottom clears the top of the tallest
    // neighbouring rack, but never so far that its top comes within `clearance` of the ceiling.
    public static class RackLift
    {
        public const float Gap = 0.15f;
        public const float Clearance = 0.3f;
        public const float Minimum = 0.5f;

        public static float Meters(float rackBottom, float rackHeight, float neighbourTop, float ceilingBottom)
        {
            float wanted = Mathf.Max(neighbourTop + Gap - rackBottom, Minimum);
            float room = ceilingBottom - Clearance - (rackBottom + rackHeight);
            return Mathf.Max(0f, Mathf.Min(wanted, room));
        }
    }
}
