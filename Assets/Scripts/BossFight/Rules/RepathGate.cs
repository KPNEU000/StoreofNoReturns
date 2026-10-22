using UnityEngine;

namespace BossFight.Rules
{
    // Limits how often an agent asks for a new path. Asking every frame (toward a target that moves every frame)
    // queued path requests, so agents stood still until a path arrived and then lurched forward.
    public class RepathGate
    {
        public const float DefaultInterval = 0.25f;
        public const float DefaultJump = 2f;

        private readonly float interval;
        private readonly float jump;
        private float lastTime = float.NegativeInfinity;
        private Vector3 lastTarget;

        public RepathGate(float interval = DefaultInterval, float jump = DefaultJump)
        {
            this.interval = Mathf.Max(0f, interval);
            this.jump = Mathf.Max(0f, jump);
        }

        // True when a new path should be requested: the first time, every `interval` seconds, or at once when the
        // target jumps more than `jump` metres (a new goal, not drift).
        public bool ShouldRepath(float time, Vector3 target)
        {
            bool due = time - lastTime >= interval || (target - lastTarget).sqrMagnitude > jump * jump;
            if (!due) return false;
            lastTime = time;
            lastTarget = target;
            return true;
        }

        public void Reset()
        {
            lastTime = float.NegativeInfinity;
        }
    }
}
