using System.Collections.Generic;
using UnityEngine;

namespace BossFight.Rules
{
    public enum SlashPhase
    {
        Windup,
        Recovery,
        Done,
    }

    // Targeting and timing for the sword slash, on the horizontal plane.
    public class SlashRules
    {
        private readonly BossFightSettings settings;

        public SlashRules(BossFightSettings settings)
        {
            this.settings = settings;
        }

        public float HitTime => settings.slashWindupSeconds;
        public float Duration => settings.slashWindupSeconds + settings.slashRecoverySeconds;

        public SlashPhase PhaseAt(float elapsed)
        {
            if (elapsed < settings.slashWindupSeconds) return SlashPhase.Windup;
            if (elapsed < Duration) return SlashPhase.Recovery;
            return SlashPhase.Done;
        }

        // Slash move speed is derived, never stored: 0.95 x clothing speed.
        public float MoveSpeed(float playerWalkSpeed) => playerWalkSpeed * settings.SlashMoveSpeedFactor;

        // Indices into targets of the closest (at most slashMaxTargets) inside reach and +-half the arc, closest first.
        public List<int> SelectTargets(Vector3 origin, Vector3 forward, IList<Vector3> targets)
        {
            var result = new List<int>();
            forward.y = 0f;
            if (forward.sqrMagnitude < 1e-8f) return result;
            forward.Normalize();
            float halfArc = settings.slashArcDegrees * 0.5f;

            var hits = new List<KeyValuePair<float, int>>();
            for (int i = 0; i < targets.Count; i++)
            {
                Vector3 to = targets[i] - origin;
                to.y = 0f;
                float dist = to.magnitude;
                if (dist > settings.slashReach) continue;
                if (dist > 1e-4f && Vector3.Angle(forward, to) > halfArc) continue;
                hits.Add(new KeyValuePair<float, int>(dist, i));
            }
            hits.Sort((a, b) => a.Key.CompareTo(b.Key));
            for (int i = 0; i < hits.Count && i < settings.slashMaxTargets; i++)
            {
                result.Add(hits[i].Value);
            }
            return result;
        }
    }
}
