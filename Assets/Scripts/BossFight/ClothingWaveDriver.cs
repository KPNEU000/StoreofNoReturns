using System.Collections.Generic;
using BossFight.Rules;
using UnityEngine;

namespace BossFight
{
    // While the fight is in Phase1/Phase2: a rack shakes (telegraph), releases a ring of its costumes, and the next
    // rack starts shaking a few seconds after most of that wave is dead. Wave size scales with reputation.
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

        private BossFightSettings settings;
        private WaveScheduler scheduler;
        private float hpMultiplier = 1f;
        private readonly List<PossessableRack> racks = new List<PossessableRack>();
        private readonly List<PossessedClothing> lastWave = new List<PossessedClothing>();
        private PossessableRack shaking;
        private Vector3 reuseSpot;
        private ReputationManager reputation;
        private int reuseIndex;

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
            if (scheduler == null)
            {
                // Reputation is read once, when the fight starts.
                int rep = reputation != null ? reputation.CurrentReputation : 50;
                scheduler = new WaveScheduler(settings, rep);
                hpMultiplier = WaveScheduler.ClothingHpMultiplier(settings, rep);
            }
            if (phase == BossPhase.Won || phase == BossPhase.Lost)
            {
                scheduler.Stop();
                return;
            }

            var wave = scheduler.Tick(Time.deltaTime, LastWaveAlive(), spawner.AliveCount, spawner.Cap, CountFresh());
            if (wave == null) return;
            if (wave.Value.Event == WaveScheduler.WaveEvent.Telegraph) StartTelegraph(wave.Value.FreshRack);
            else if (wave.Value.Event == WaveScheduler.WaveEvent.Release) ReleaseWave(wave.Value.Count);
        }

        private void StartTelegraph(bool fresh)
        {
            shaking = null;
            if (fresh)
            {
                var candidates = racks.FindAll(r => r != null && !r.IsPossessed);
                if (candidates.Count > 0)
                {
                    shaking = candidates[Random.Range(0, candidates.Count)];
                    shaking.BeginTelegraph(settings.rackTelegraphSeconds);
                    return;
                }
            }
            // Every rack is gone: the Witch re-uses an emptied rack spot.
            if (racks.Count == 0) return;
            reuseIndex = (reuseIndex + 1) % racks.Count;
            var spot = racks[reuseIndex];
            reuseSpot = spot != null ? spot.ReleasePoint : transform.position;
        }

        private void ReleaseWave(int count)
        {
            lastWave.Clear();
            if (shaking != null)
            {
                lastWave.AddRange(shaking.Release(spawner, count, hpMultiplier, RebuildNavMesh));
                shaking = null;
                return;
            }
            var garments = racks.Count > 0 && racks[reuseIndex] != null ? racks[reuseIndex].Garments : null;
            lastWave.AddRange(spawner.SpawnRing(reuseSpot, count, garments, settings.waveRingMinRadius, settings.waveRingMaxRadius, hpMultiplier));
        }

        private int LastWaveAlive()
        {
            int n = 0;
            foreach (var c in lastWave) if (c != null && c.IsAlive) n++;
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
