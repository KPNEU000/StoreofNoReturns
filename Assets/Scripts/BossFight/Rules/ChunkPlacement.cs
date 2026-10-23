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

        // Candidates this far above the floor are furniture tops or the roof, never a landing spot.
        public const float FloorTolerance = 0.5f;

        public static bool OnFloor(Vector3 point, float floorY) => point.y <= floorY + FloorTolerance;

        // Accepts candidates in order; may return fewer than Count when the candidates run out.
        public List<ChunkSpot> Place(IList<Vector3> candidates, Vector3 player, float floorY)
        {
            var spots = new List<ChunkSpot>();
            foreach (var candidate in candidates)
            {
                if (spots.Count >= Count) break;
                if (!OnFloor(candidate, floorY)) continue;
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

        public List<ChunkSpot> Place(IList<Vector3> candidates, Vector3 player) => Place(candidates, player, float.PositiveInfinity);

        // A landed chunk that cuts the player off from the Witch crumbles away.
        public static bool Crumbles(NavMeshPathStatus status) => status != NavMeshPathStatus.PathComplete;

        // A landing chunk hits the player (once) when they stand within this fraction of its radius of its centre.
        public const float CrushRadiusFraction = 0.9f;

        public static bool LandsOn(Vector3 chunkCentre, Vector3 player, float radius)
        {
            float dx = chunkCentre.x - player.x;
            float dz = chunkCentre.z - player.z;
            float r = radius * CrushRadiusFraction;
            return dx * dx + dz * dz <= r * r;
        }

        // A spot must be at least this fraction of the chunk radius from any NavMesh edge (racks, counter, walls).
        public const float OpenFloorRadiusFraction = 0.6f;

        // Seconds to free-fall `height` metres under normal gravity (minimum one frame's worth).
        public static float FallSeconds(float height) => Mathf.Max(Mathf.Sqrt(2f * Mathf.Max(height, 0f) / 9.81f), 0.05f);

        // Height during a free fall from `from` to `to`, t = 0..1 of the fall time (accelerating, lands exactly at t = 1).
        public static float FallHeight(float from, float to, float t)
        {
            t = Mathf.Clamp01(t);
            return Mathf.Lerp(from, to, t * t);
        }

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
