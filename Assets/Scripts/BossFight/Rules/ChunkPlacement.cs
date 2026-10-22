using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace BossFight.Rules
{
    // One ceiling chunk to drop: where it lands, its yaw and when (seconds after the cave-in starts) its hand appears.
    public struct ChunkSpot
    {
        public Vector3 Position;
        public float Yaw;
        public float Delay;
    }

    // Picks the chunk landing spots from candidate NavMesh points. Distances are measured on the floor plane.
    public class ChunkPlacement
    {
        public const int MinCount = 3;
        public const int MaxCount = 10;

        private readonly float minPlayerDistance;
        private readonly float minChunkSeparation;
        private readonly float durationSeconds;
        private readonly Func<float> random01;

        public int Count { get; }

        // random01() returns a value in [0, 1); null uses System.Random.
        public ChunkPlacement(BossFightSettings settings, Func<float> random01 = null)
        {
            minPlayerDistance = settings.caveInPlayerClearance;
            minChunkSeparation = settings.caveInChunkRadius * 2f;
            durationSeconds = Mathf.Max(settings.caveInDurationSeconds, 0f);
            Count = ClampCount(settings.caveInChunkCount);
            var rng = new System.Random();
            this.random01 = random01 ?? (() => (float)rng.NextDouble());
        }

        public static int ClampCount(int count) => Mathf.Clamp(count, MinCount, MaxCount);

        // Chunks start evenly spread over the duration: the first at 0, the last one step before the end.
        public float DelayOf(int index) => durationSeconds * index / Count;

        // Accepts candidates in order; may return fewer than Count when the candidates run out.
        public List<ChunkSpot> Place(IList<Vector3> candidates, Vector3 player)
        {
            var spots = new List<ChunkSpot>();
            foreach (var candidate in candidates)
            {
                if (spots.Count >= Count) break;
                if (Planar(candidate, player) < minPlayerDistance) continue;
                bool overlaps = false;
                foreach (var spot in spots)
                {
                    if (Planar(candidate, spot.Position) < minChunkSeparation) { overlaps = true; break; }
                }
                if (overlaps) continue;
                spots.Add(new ChunkSpot { Position = candidate, Yaw = random01() * 360f, Delay = DelayOf(spots.Count) });
            }
            return spots;
        }

        // A landed chunk that cuts the player off from the Witch crumbles away.
        public static bool Crumbles(NavMeshPathStatus status) => status != NavMeshPathStatus.PathComplete;

        private static float Planar(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }
    }

    // The cave-in fires once per fight, the first time money is at or under the threshold while a phase is running.
    public class CaveInTrigger
    {
        private readonly float threshold;

        public bool Triggered { get; private set; }

        public CaveInTrigger(BossFightSettings settings)
        {
            threshold = settings.caveInMoney;
        }

        public bool Check(float money, BossPhase phase)
        {
            if (Triggered) return false;
            if (phase != BossPhase.Phase1 && phase != BossPhase.Phase2) return false;
            if (money > threshold) return false;
            Triggered = true;
            return true;
        }
    }
}
