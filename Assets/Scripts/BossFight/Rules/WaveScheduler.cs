using System;

namespace BossFight.Rules
{
    // Decides when the Witch possesses the next rack and how many costumes it releases.
    public class WaveScheduler
    {
        public struct Wave
        {
            // True when an unpossessed rack is left; false means re-use an already emptied rack spot.
            public bool FreshRack;
            public int Count;
        }

        private readonly float intervalSeconds;
        private readonly int minCostumes;
        private readonly int maxCostumes;
        private readonly Func<int, int, int> randomInclusive;
        private float timer;

        public bool Stopped { get; private set; }

        // randomInclusive(min, max) returns a value in [min, max]; null uses System.Random.
        public WaveScheduler(BossFightSettings settings, Func<int, int, int> randomInclusive = null)
        {
            intervalSeconds = settings.waveIntervalSeconds;
            minCostumes = settings.waveMinCostumes;
            maxCostumes = Math.Max(settings.waveMinCostumes, settings.waveMaxCostumes);
            var rng = new Random();
            this.randomInclusive = randomInclusive ?? ((min, max) => rng.Next(min, max + 1));
            timer = intervalSeconds;
        }

        public void Stop()
        {
            Stopped = true;
        }

        // Returns a wave on the frame one triggers. When the cap leaves no room the wave waits, so it fires as
        // soon as something dies.
        public Wave? Tick(float deltaTime, int aliveCount, int cap, int remainingRacks)
        {
            if (Stopped) return null;
            timer -= deltaTime;
            if (timer > 0f) return null;
            int room = cap - aliveCount;
            if (room <= 0) return null;
            timer = intervalSeconds;
            int count = Math.Min(randomInclusive(minCostumes, maxCostumes), room);
            return new Wave { FreshRack = remainingRacks > 0, Count = count };
        }
    }
}
