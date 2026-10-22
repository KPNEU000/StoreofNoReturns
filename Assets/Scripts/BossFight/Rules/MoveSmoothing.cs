using UnityEngine;

namespace BossFight.Rules
{
    // Velocity easing and mouse-look math for first-person movement.
    public class MoveSmoothing
    {
        public const float DefaultAccelTime = 0.1f;
        public const float DefaultSensitivity = 0.1f; // degrees per pixel
        // Scenes serialized the old Time.deltaTime-scaled value (100); anything above this is legacy.
        public const float MaxSensitivity = 5f;

        readonly float accelTime;

        public MoveSmoothing(float accelTime = DefaultAccelTime)
        {
            this.accelTime = accelTime;
        }

        // Moves current toward target by at most (maxSpeed / accelTime) * dt, so a full-speed
        // velocity reaches zero within accelTime after release.
        public Vector2 Approach(Vector2 current, Vector2 target, float maxSpeed, float dt)
        {
            if (accelTime <= 0f) return target;
            return Vector2.MoveTowards(current, target, maxSpeed * dt / accelTime);
        }

        // Raw mouse delta (pixels) -> degrees. Deliberately no deltaTime.
        public static Vector2 LookDelta(Vector2 rawDelta, float sensitivity)
        {
            return rawDelta * sensitivity;
        }

        public static float NormalizeSensitivity(float sensitivity)
        {
            return sensitivity > MaxSensitivity || sensitivity <= 0f ? DefaultSensitivity : sensitivity;
        }
    }
}
