using System;
using UnityEngine;

namespace BossFight.Rules
{
    // Paces the swarm (playtest round 6): a rack shakes as a telegraph, then releases a wave; once
    // waveClearFraction of that wave is dead, a waveNextDelaySeconds countdown starts the next telegraph.
    // Wave size scales with reputation; the costumes' HP only doubles at rock-bottom reputation.
    public class WaveScheduler
    {
        public enum WaveEvent
        {
            None,
            // A rack (or, once every rack is gone, an emptied rack spot) starts its shake.
            Telegraph,
            // The telegraph is over: release Count costumes.
            Release,
        }

        public enum Stage
        {
            Countdown,
            Telegraph,
            WaitingForClear,
        }

        public struct Wave
        {
            public WaveEvent Event;
            // True when an unpossessed rack is left; false means re-use an already emptied rack spot.
            public bool FreshRack;
            public int Count;
        }

        private readonly BossFightSettings settings;
        private readonly int plannedCount;
        private float timer;
        private int lastWaveSize;

        public Stage Current { get; private set; } = Stage.Countdown;
        public bool Stopped { get; private set; }

        public WaveScheduler(BossFightSettings settings, int reputation)
        {
            this.settings = settings;
            plannedCount = SwarmSize(settings, reputation);
            timer = settings.waveFirstDelaySeconds;
        }

        public void Stop()
        {
            Stopped = true;
        }

        // `lastWaveAlive`: how many of the previous wave are still alive. Returns a Telegraph or Release event on
        // the frame one happens. A Release waits while the alive cap leaves no room.
        public Wave? Tick(float deltaTime, int lastWaveAlive, int aliveCount, int cap, int remainingRacks)
        {
            if (Stopped) return null;
            switch (Current)
            {
                case Stage.WaitingForClear:
                    if (!Cleared(lastWaveSize, lastWaveAlive, settings.waveClearFraction)) return null;
                    Current = Stage.Countdown;
                    timer = settings.waveNextDelaySeconds;
                    return null;
                case Stage.Countdown:
                    timer -= deltaTime;
                    if (timer > 0f) return null;
                    Current = Stage.Telegraph;
                    timer = settings.rackTelegraphSeconds;
                    return new Wave { Event = WaveEvent.Telegraph, FreshRack = remainingRacks > 0 };
                case Stage.Telegraph:
                    timer -= deltaTime;
                    if (timer > 0f) return null;
                    int room = cap - aliveCount;
                    if (room <= 0) return null;
                    lastWaveSize = Math.Min(plannedCount, room);
                    Current = Stage.WaitingForClear;
                    return new Wave { Event = WaveEvent.Release, FreshRack = remainingRacks > 0, Count = lastWaveSize };
            }
            return null;
        }

        // Enough of the wave is dead to start the next countdown.
        public static bool Cleared(int waveSize, int alive, float clearFraction)
        {
            if (waveSize <= 0) return true;
            return waveSize - alive >= Mathf.CeilToInt(waveSize * clearFraction - 1e-4f);
        }

        // 1x at 100 reputation, swarmMultiplierAtRep50 at 50, swarmMultiplierAtRep0 at 0, linear between.
        public static float SwarmMultiplier(BossFightSettings settings, int reputation)
        {
            float r = Mathf.Clamp(reputation, 0, 100);
            return r >= 50f
                ? Mathf.Lerp(settings.swarmMultiplierAtRep50, 1f, (r - 50f) / 50f)
                : Mathf.Lerp(settings.swarmMultiplierAtRep0, settings.swarmMultiplierAtRep50, r / 50f);
        }

        public static int SwarmSize(BossFightSettings settings, int reputation)
        {
            return Mathf.Max(1, Mathf.RoundToInt(settings.waveBaseCostumes * SwarmMultiplier(settings, reputation)));
        }

        public static float ClothingHpMultiplier(BossFightSettings settings, int reputation)
        {
            return reputation <= settings.clothingDoubleHpReputation ? 2f : 1f;
        }
    }
}
