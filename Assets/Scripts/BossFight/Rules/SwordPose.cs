using UnityEngine;

namespace BossFight.Rules
{
    // Blade pose over a slash: upright -> point forward (inside the windup) -> horizontal right-to-left
    // sweep around the hit moment -> back upright. The blade runs along local +Y when upright.
    public static class SwordPose
    {
        public const float SweepStartYaw = 60f;
        public const float SweepEndYaw = -60f;
        public const float ForwardPitch = 90f;

        // Pivot slides this far toward the screen centre (local X) at the sweep, so the arc is in front of the player.
        public const float CenterPull = -0.35f;

        public struct Pose
        {
            public Quaternion rotation;
            public Vector3 positionOffset;
        }

        // t is normalized slash time 0..1, hitFraction is HitTime / Duration.
        public static Pose Evaluate(float t, float hitFraction)
        {
            t = Mathf.Clamp01(t);
            hitFraction = Mathf.Clamp(hitFraction, 0.05f, 0.95f);
            float sweepStart = hitFraction * 0.7f;
            float sweepEnd = hitFraction + (1f - hitFraction) * 0.2f;

            float point;  // 0 upright, 1 pointing forward
            float sweep;  // 0 at the right, 1 at the left
            float back;   // 0 held, 1 upright again
            point = Smooth(Mathf.InverseLerp(0f, sweepStart, t));
            sweep = Smooth(Mathf.InverseLerp(sweepStart, sweepEnd, t));
            back = Smooth(Mathf.InverseLerp(sweepEnd, 1f, t));

            float yaw = Mathf.Lerp(SweepStartYaw, SweepEndYaw, sweep);
            float pitch = ForwardPitch * point * (1f - back);
            float yawWeight = point * (1f - back);

            Pose pose;
            pose.rotation = Quaternion.Euler(0f, yaw * yawWeight, 0f) * Quaternion.Euler(pitch, 0f, 0f);
            pose.positionOffset = new Vector3(CenterPull * yawWeight, 0f, 0f);
            return pose;
        }

        private static float Smooth(float x) => x * x * (3f - 2f * x);
    }
}
