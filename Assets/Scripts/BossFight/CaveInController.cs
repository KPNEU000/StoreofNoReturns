using System.Collections;
using System.Collections.Generic;
using BossFight.Rules;
using UnityEngine;
using UnityEngine.AI;

namespace BossFight
{
    // Once per fight, at low money: the Witch's hands tear chunks out of the ceiling. The ceiling cube is swapped for
    // tiles, each chunk is announced by a rising hand, falls, carves the NavMesh and lets moonlight through its hole.
    public class CaveInController : MonoBehaviour
    {
        public const string WitchLine = "The ceiling is too unstable to keep doing this.";
        private const int CandidateSamples = 80;
        private const float TileSize = 2f;
        private const float MessageSeconds = 5f;

        [SerializeField]
        private BossFightConfig config;
        [SerializeField]
        private BossDirector director;
        [SerializeField]
        private MoneyManager moneyManager;
        [SerializeField]
        private PlayerMovement player;
        [SerializeField]
        private Transform witch;
        [SerializeField]
        private Transform ceiling;
        [SerializeField]
        private Material chunkMaterial;
        [SerializeField]
        private Material handMaterial;
        [SerializeField]
        private Material dustMaterial;
        [SerializeField]
        private Color moonlightColor = new Color(0.65f, 0.78f, 1f);
        [SerializeField]
        private AudioClip crackSound;
        [SerializeField]
        private AudioClip rumbleSound;
        [SerializeField]
        private AudioClip impactSound;

        private BossFightSettings settings;
        private CaveInTrigger trigger;
        private ChunkPlacement placement;
        private readonly List<GameObject> tiles = new List<GameObject>();
        private Bounds ceilingBounds;
        private Camera shakeCamera;
        private Vector3 cameraRest;
        private int shakeToken;
        private int chunksPending;
        private int seedCounter;

        public bool Triggered => trigger != null && trigger.Triggered;

        private void Awake()
        {
            settings = BossFightConfig.SettingsOf(config);
            trigger = new CaveInTrigger(settings);
            placement = new ChunkPlacement(settings);
            if (director == null) director = FindFirstObjectByType<BossDirector>();
            if (moneyManager == null) moneyManager = FindFirstObjectByType<MoneyManager>();
            if (player == null) player = FindFirstObjectByType<PlayerMovement>();
            if (witch == null)
            {
                var boss = FindFirstObjectByType<WitchBoss>();
                if (boss != null) witch = boss.transform;
            }
            if (ceiling == null)
            {
                var found = GameObject.Find("Ceiling");
                if (found != null) ceiling = found.transform;
            }
        }

        private void OnEnable()
        {
            if (moneyManager != null) moneyManager.OnMoneyChanged += HandleMoneyChanged;
            if (director != null) director.PhaseChanged += HandlePhaseChanged;
        }

        private void OnDisable()
        {
            if (moneyManager != null) moneyManager.OnMoneyChanged -= HandleMoneyChanged;
            if (director != null) director.PhaseChanged -= HandlePhaseChanged;
        }

        private void HandleMoneyChanged(float money) => TryStart(money);

        private void HandlePhaseChanged(BossPhase phase)
        {
            if (moneyManager != null) TryStart(moneyManager.CurrentMoney);
        }

        private void TryStart(float money)
        {
            if (director == null || ceiling == null || player == null) return;
            if (trigger.Check(money, director.Phase)) StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            SwapCeiling();
            PlaySound(rumbleSound, player.transform.position);

            var spots = placement.Place(SampleCandidates(), player.transform.position);
            chunksPending = spots.Count;
            foreach (var spot in spots) StartCoroutine(RunChunk(spot));
            while (chunksPending > 0) yield return null;

            Say(WitchLine);
        }

        private void Say(string line)
        {
            Debug.Log($"Witch: {line}");
            var hud = FindFirstObjectByType<BossHud>();
            if (hud != null) hud.ShowMessage(line, MessageSeconds);
        }

        // Replaces the single Ceiling cube with a grid of identical tiles so single pieces can be removed.
        private void SwapCeiling()
        {
            var renderer = ceiling.GetComponent<Renderer>();
            ceilingBounds = renderer != null ? renderer.bounds : new Bounds(ceiling.position, ceiling.lossyScale);
            var material = renderer != null ? renderer.sharedMaterial : null;
            ceiling.gameObject.SetActive(false);

            int countX = Mathf.Max(1, Mathf.CeilToInt(ceilingBounds.size.x / TileSize));
            int countZ = Mathf.Max(1, Mathf.CeilToInt(ceilingBounds.size.z / TileSize));
            var size = new Vector3(ceilingBounds.size.x / countX, ceilingBounds.size.y, ceilingBounds.size.z / countZ);
            for (int x = 0; x < countX; x++)
            {
                for (int z = 0; z < countZ; z++)
                {
                    var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    tile.name = $"CeilingTile {x},{z}";
                    tile.transform.position = new Vector3(
                        ceilingBounds.min.x + size.x * (x + 0.5f), ceilingBounds.center.y, ceilingBounds.min.z + size.z * (z + 0.5f));
                    tile.transform.localScale = size;
                    if (material != null) tile.GetComponent<MeshRenderer>().sharedMaterial = material;
                    tiles.Add(tile);
                }
            }
        }

        private void RemoveTileAbove(Vector3 position)
        {
            GameObject best = null;
            float bestDistance = float.MaxValue;
            foreach (var tile in tiles)
            {
                if (tile == null) continue;
                var p = tile.transform.position;
                float d = (p.x - position.x) * (p.x - position.x) + (p.z - position.z) * (p.z - position.z);
                if (d < bestDistance) { bestDistance = d; best = tile; }
            }
            if (best != null) Destroy(best);
        }

        private List<Vector3> SampleCandidates()
        {
            var points = new List<Vector3>();
            var b = ceilingBounds;
            for (int i = 0; i < CandidateSamples; i++)
            {
                var random = new Vector3(Random.Range(b.min.x, b.max.x), b.min.y - 1f, Random.Range(b.min.z, b.max.z));
                if (NavMesh.SamplePosition(random, out var hit, b.size.y + 6f, NavMesh.AllAreas)
                    && Mathf.Abs(hit.position.x - random.x) < 0.5f && Mathf.Abs(hit.position.z - random.z) < 0.5f)
                {
                    points.Add(hit.position);
                }
            }
            return points;
        }

        private IEnumerator RunChunk(ChunkSpot spot)
        {
            yield return new WaitForSeconds(spot.Delay);

            float floorY = spot.Position.y;
            float ceilingY = ceilingBounds.min.y;
            float restHeight = settings.caveInChunkRestHeight;

            yield return RaiseHand(spot.Position, ceilingY);

            RemoveTileAbove(spot.Position);
            SpawnDust(new Vector3(spot.Position.x, ceilingY, spot.Position.z), 1.5f);
            PlaySound(crackSound, spot.Position);

            var chunk = CreateChunk(spot, restHeight);
            var body = chunk.GetComponent<Rigidbody>();
            var landing = chunk.GetComponent<ChunkLanding>();
            float dropFrom = ceilingY - restHeight - settings.caveInFallHeightPadding;
            chunk.transform.position = new Vector3(spot.Position.x, Mathf.Max(dropFrom, floorY), spot.Position.z);
            body.isKinematic = false;

            float waited = 0f;
            while (!landing.Landed && waited < 4f)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            body.isKinematic = true;
            chunk.transform.rotation = Quaternion.Euler(0f, spot.Yaw, 0f);
            var settled = chunk.transform.position;
            chunk.transform.position = new Vector3(settled.x, floorY, settled.z);
            SpawnDust(chunk.transform.position + Vector3.up * 0.2f, 1.2f);
            PlaySound(impactSound, chunk.transform.position);
            Shake(Vector3.Distance(player.transform.position, chunk.transform.position));

            var light = CreateMoonlight(spot.Position, ceilingY, floorY);
            AddObstacle(chunk);

            yield return new WaitForSeconds(settings.caveInReachabilityDelaySeconds);
            if (BlocksWitch())
            {
                SpawnDust(chunk.transform.position + Vector3.up * 0.6f, 2.5f);
                Destroy(chunk);
                Destroy(light);
            }
            chunksPending--;
        }

        private IEnumerator RaiseHand(Vector3 floorPoint, float ceilingY)
        {
            var hand = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            hand.name = "CaveInHand";
            Destroy(hand.GetComponent<Collider>());
            var renderer = hand.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = handMaterial != null ? handMaterial : renderer.sharedMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            float reach = Mathf.Max(ceilingY - floorPoint.y - 0.2f, 0.5f);
            float seconds = Mathf.Max(settings.caveInHandTelegraphSeconds, 0.01f);
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                float length = Mathf.Max(reach * (t / seconds), 0.05f);
                // A capsule is 2 units tall, so half the length is the Y scale; it grows up from the floor.
                hand.transform.localScale = new Vector3(0.16f, length * 0.5f, 0.16f);
                hand.transform.position = floorPoint + Vector3.up * (length * 0.5f);
                yield return null;
            }
            Destroy(hand);
        }

        private GameObject CreateChunk(ChunkSpot spot, float height)
        {
            var go = new GameObject("CaveInChunk");
            go.transform.rotation = Quaternion.Euler(0f, spot.Yaw, 0f);
            var mesh = ChunkMesh.Build(++seedCounter * 7919 + Mathf.RoundToInt(spot.Yaw), settings.caveInChunkRadius * 2f, height, 0.08f);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            if (chunkMaterial != null) renderer.sharedMaterial = chunkMaterial;
            var collider = go.AddComponent<BoxCollider>();
            collider.center = mesh.bounds.center;
            collider.size = mesh.bounds.size;
            var body = go.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.mass = 400f;
            go.AddComponent<ChunkLanding>();
            return go;
        }

        private void AddObstacle(GameObject chunk)
        {
            var mesh = chunk.GetComponent<MeshFilter>().sharedMesh;
            var obstacle = chunk.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.center = mesh.bounds.center;
            obstacle.size = mesh.bounds.size;
            obstacle.carving = true;
        }

        private GameObject CreateMoonlight(Vector3 spot, float ceilingY, float floorY)
        {
            var go = new GameObject("CaveInMoonlight");
            go.transform.position = new Vector3(spot.x, ceilingY + 0.5f, spot.z);
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var light = go.AddComponent<Light>();
            light.type = LightType.Spot;
            light.color = moonlightColor;
            light.intensity = settings.caveInMoonlightIntensity;
            light.range = ceilingY - floorY + 3f;
            light.spotAngle = 40f;
            light.innerSpotAngle = 25f;
            light.shadows = LightShadows.None;
            return go;
        }

        // After carving settles: is the Witch still reachable on foot from the player?
        private bool BlocksWitch()
        {
            if (witch == null || player == null) return false;
            if (!NavMesh.SamplePosition(player.transform.position, out var from, 3f, NavMesh.AllAreas)) return false;
            if (!NavMesh.SamplePosition(witch.position, out var to, 3f, NavMesh.AllAreas)) return false;
            var path = new NavMeshPath();
            if (!NavMesh.CalculatePath(from.position, to.position, NavMesh.AllAreas, path)) return true;
            return ChunkPlacement.Crumbles(path.status);
        }

        private void SpawnDust(Vector3 position, float size)
        {
            var go = new GameObject("CaveInDust");
            go.transform.position = position;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.duration = 0.5f;
            main.loop = false;
            main.startLifetime = 1.8f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f * size, 1.6f * size);
            main.startSize = new ParticleSystem.MinMaxCurve(0.25f * size, 0.7f * size);
            main.startColor = new Color(0.55f, 0.55f, 0.55f, 0.6f);
            main.gravityModifier = 0.05f;
            main.stopAction = ParticleSystemStopAction.Destroy;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.RoundToInt(30f * size)) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.4f * size;
            var fade = ps.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0.7f, 0f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            if (dustMaterial != null) renderer.sharedMaterial = dustMaterial;
            ps.Play();
        }

        private void PlaySound(AudioClip clip, Vector3 position)
        {
            if (clip != null) AudioSource.PlayClipAtPoint(clip, position);
        }

        private void Shake(float distance)
        {
            if (shakeCamera == null)
            {
                shakeCamera = Camera.main;
                if (shakeCamera == null) return;
                cameraRest = shakeCamera.transform.localPosition;
            }
            StartCoroutine(ShakeRoutine(++shakeToken, Mathf.Clamp01(1f - distance / 20f) * 0.12f + 0.02f));
        }

        private IEnumerator ShakeRoutine(int token, float amplitude)
        {
            for (float t = 0f; t < 0.4f && token == shakeToken; t += Time.deltaTime)
            {
                shakeCamera.transform.localPosition = cameraRest + Random.insideUnitSphere * (amplitude * (1f - t / 0.4f));
                yield return null;
            }
            // Only the newest shake puts the camera back, so overlapping shakes never strand it off-centre.
            if (token == shakeToken) shakeCamera.transform.localPosition = cameraRest;
        }
    }

    // Reports the first real collision of a falling chunk.
    public class ChunkLanding : MonoBehaviour
    {
        public bool Landed { get; private set; }

        private void OnCollisionEnter(Collision collision)
        {
            Landed = true;
        }
    }
}
