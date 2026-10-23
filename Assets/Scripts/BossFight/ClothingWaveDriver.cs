using System.Collections.Generic;
using BossFight.Rules;
using UnityEngine;

namespace BossFight
{
    // While the fight is in Phase1/Phase2, runs waveLanes independent rack cycles (round 10: two racks always in
    // play). In each lane a rack shakes (telegraph), releases a ring of its costumes, and the lane's next rack
    // starts once most of that wave is dead. Wave size scales with reputation.
    public class ClothingWaveDriver : MonoBehaviour
    {
        [SerializeField]
        private BossFightConfig config;
        [SerializeField]
        private BossDirector director;
        [SerializeField]
        private ClothingSpawner spawner;
        [SerializeField]
        private BossFightRig rig;

        private class Lane
        {
            public WaveScheduler scheduler;
            public PossessableRack shaking;
            public Vector3 reuseSpot;
            public GameObject[] reuseGarments;
            public readonly List<PossessedClothing> wave = new List<PossessedClothing>();
        }

        private BossFightSettings settings;
        private readonly List<Lane> lanes = new List<Lane>();
        private float hpMultiplier = 1f;
        private readonly List<PossessableRack> racks = new List<PossessableRack>();
        private int reuseIndex;
        private ReputationManager reputation;

        private void Awake()
        {
            settings = BossFightConfig.SettingsOf(config);
            if (director == null) director = FindFirstObjectByType<BossDirector>();
            if (spawner == null) spawner = GetComponent<ClothingSpawner>();
            if (rig == null) rig = FindFirstObjectByType<BossFightRig>();
            racks.AddRange(FindObjectsByType<PossessableRack>(FindObjectsSortMode.None));
            reputation = FindFirstObjectByType<ReputationManager>();
        }

        private void Update()
        {
            if (director == null || spawner == null) return;
            var phase = director.Phase;
            if (phase == BossPhase.Intro) return;
            if (lanes.Count == 0) StartLanes();
            if (phase == BossPhase.Won || phase == BossPhase.Lost)
            {
                foreach (var lane in lanes) lane.scheduler.Stop();
                return;
            }

            foreach (var lane in lanes)
            {
                var wave = lane.scheduler.Tick(Time.deltaTime, Alive(lane.wave), spawner.AliveCount, spawner.Cap, CountFresh());
                if (wave == null) continue;
                if (wave.Value.Event == WaveScheduler.WaveEvent.Telegraph) StartTelegraph(lane, wave.Value.FreshRack);
                else if (wave.Value.Event == WaveScheduler.WaveEvent.Release) ReleaseWave(lane, wave.Value.Count);
            }
        }

        // Reputation is read once, when the fight starts; each lane starts waveLaneStaggerSeconds after the last.
        private void StartLanes()
        {
            int rep = reputation != null ? reputation.CurrentReputation : 50;
            hpMultiplier = WaveScheduler.ClothingHpMultiplier(settings, rep);
            int count = Mathf.Max(1, settings.waveLanes);
            for (int i = 0; i < count; i++)
            {
                float delay = settings.waveFirstDelaySeconds + i * settings.waveLaneStaggerSeconds;
                lanes.Add(new Lane { scheduler = new WaveScheduler(settings, rep, delay) });
            }
        }

        private void StartTelegraph(Lane lane, bool fresh)
        {
            lane.shaking = null;
            if (fresh)
            {
                var candidates = racks.FindAll(r => r != null && !r.IsPossessed);
                if (candidates.Count > 0)
                {
                    lane.shaking = candidates[Random.Range(0, candidates.Count)];
                    lane.shaking.BeginTelegraph(settings.rackTelegraphSeconds);
                    return;
                }
            }
            // Every rack is gone: the Witch re-uses an emptied rack spot.
            if (racks.Count == 0) return;
            reuseIndex = (reuseIndex + 1) % racks.Count;
            var spot = racks[reuseIndex];
            lane.reuseSpot = spot != null ? spot.ReleasePoint : transform.position;
            lane.reuseGarments = spot != null ? spot.Garments : null;
        }

        private void ReleaseWave(Lane lane, int count)
        {
            lane.wave.Clear();
            if (lane.shaking != null)
            {
                lane.wave.AddRange(lane.shaking.Release(spawner, count, hpMultiplier, RebuildNavMesh));
                lane.shaking = null;
                return;
            }
            lane.wave.AddRange(spawner.SpawnRing(lane.reuseSpot, count, lane.reuseGarments, settings.waveRingMinRadius, settings.waveRingMaxRadius, hpMultiplier));
        }

        private static int Alive(List<PossessedClothing> wave)
        {
            int n = 0;
            foreach (var c in wave) if (c != null && c.IsAlive) n++;
            return n;
        }

        private int CountFresh()
        {
            int n = 0;
            foreach (var rack in racks) if (rack != null && !rack.IsPossessed) n++;
            return n;
        }

        private void RebuildNavMesh()
        {
            if (rig == null || rig.NavMeshSurface == null) return;
            var surface = rig.NavMeshSurface;
            // Incremental async update of the existing data; a first-time build is the only synchronous case.
            if (surface.navMeshData == null) surface.BuildNavMesh();
            else surface.UpdateNavMesh(surface.navMeshData);
        }
    }
}
