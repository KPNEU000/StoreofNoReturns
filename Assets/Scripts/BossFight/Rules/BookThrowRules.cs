using System;
using System.Collections.Generic;
using UnityEngine;

namespace BossFight.Rules
{
    // When the Witch throws a book (Phase2 only), which book, and the flight/hit/glow math.
    public class BookThrowRules
    {
        // The pick is random among this many books closest to the player.
        public const int NearestCandidates = 3;

        private readonly float intervalSeconds;
        private readonly float flightSpeed;
        private readonly float hitRadius;
        private readonly float glowFadeSeconds;
        private readonly Func<int, int> randomBelow;
        private float timer;

        public float TelegraphSeconds { get; }
        public float ArmSeconds { get; }
        public float SweepSeconds { get; }

        // randomBelow(n) returns a value in [0, n); null uses System.Random.
        public BookThrowRules(BossFightSettings settings, Func<int, int> randomBelow = null)
        {
            intervalSeconds = settings.bookThrowIntervalSeconds;
            flightSpeed = settings.bookFlightSpeed;
            hitRadius = settings.bookHitRadius;
            glowFadeSeconds = Mathf.Max(settings.bookGlowFadeSeconds, 0.01f);
            TelegraphSeconds = settings.bookTelegraphSeconds;
            ArmSeconds = settings.bookArmSeconds;
            SweepSeconds = settings.bookSweepSeconds;
            var rng = new System.Random();
            this.randomBelow = randomBelow ?? (n => rng.Next(n));
            timer = intervalSeconds;
        }

        // True on the frame a throw should start. Outside Phase2 the interval restarts; while a throw is still
        // running (busy) a due throw waits for it to finish.
        public bool Tick(float deltaTime, bool phase2, bool busy)
        {
            if (!phase2)
            {
                timer = intervalSeconds;
                return false;
            }
            timer -= deltaTime;
            if (timer > 0f || busy) return false;
            timer = intervalSeconds;
            return true;
        }

        // Index into candidates of the book to throw, or -1 when there is none.
        public int PickBook(IReadOnlyList<Vector3> candidates, Vector3 playerPosition)
        {
            if (candidates == null || candidates.Count == 0) return -1;
            var order = new List<int>(candidates.Count);
            for (int i = 0; i < candidates.Count; i++) order.Add(i);
            order.Sort((a, b) => SqrDistance(candidates[a], playerPosition).CompareTo(SqrDistance(candidates[b], playerPosition)));
            int pool = Mathf.Min(NearestCandidates, order.Count);
            return order[Mathf.Clamp(randomBelow(pool), 0, pool - 1)];
        }

        // Straight line, no homing: the direction is fixed at release.
        public static Vector3 FlightDirection(Vector3 from, Vector3 target)
        {
            var d = target - from;
            return d.sqrMagnitude > 1e-6f ? d.normalized : Vector3.forward;
        }

        public Vector3 StepFlight(Vector3 position, Vector3 direction, float deltaTime)
        {
            return position + direction * (flightSpeed * deltaTime);
        }

        public static bool ReachedTarget(Vector3 position, Vector3 direction, Vector3 target)
        {
            return Vector3.Dot(position - target, direction) >= 0f;
        }

        public bool Hits(Vector3 bookPosition, Vector3 playerCenter)
        {
            return SqrDistance(bookPosition, playerCenter) <= hitRadius * hitRadius;
        }

        // 1 at landing, 0 once the glow has faded.
        public float GlowAt(float secondsSinceLanding)
        {
            return Mathf.Clamp01(1f - secondsSinceLanding / glowFadeSeconds);
        }

        private static float SqrDistance(Vector3 a, Vector3 b) => (a - b).sqrMagnitude;
    }
}
