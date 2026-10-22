using BossFight.Rules;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace BossFight
{
    // The Witch's arrival: cinematic, then a tutorial that teaches the sword and flashlight, then the door blast.
    // OpeningSteps decides when each step ends; this class does what each step looks like.
    public class OpeningSequence : MonoBehaviour
    {
        private const string WitchLine = "I'm turning everyone who hates you into monsters…";
        private const string SwordPrompt = "Left-click: grab the sword";
        private const string FlashlightPrompt = "Right-click: grab the flashlight";
        private const string MovePrompt = "Get to the middle of the store…";
        private const string LightPrompt = "What is that? Hold right-click to light it up.";
        private const string SlashPrompt = "Left-click to slash.";

        [SerializeField]
        private BossFightConfig config;
        [SerializeField]
        private BossDirector director;
        [SerializeField]
        private PlayerMovement player;
        [SerializeField]
        private Transform witch;
        [SerializeField]
        private Transform monitor;
        [SerializeField]
        private GlassShard monitorGlass;
        [SerializeField]
        private Transform door;
        [SerializeField]
        private Collider doorBarrier;
        [SerializeField]
        private Transform vaultTarget;
        [SerializeField]
        private ClothingSpawner spawner;
        [SerializeField]
        private BossHud hud;
        [SerializeField]
        private ScreenCrackOverlay crackOverlay;
        [SerializeField]
        private GameObject swordObject;
        [SerializeField]
        private PlayerFlashlight flashlight;
        [SerializeField]
        private Material magicMaterial;
        [SerializeField]
        private Material shardMaterial;
        [SerializeField]
        private Material wallCrackMaterial;
        [SerializeField]
        private AudioClip magicSound;
        [SerializeField]
        private AudioClip shatterSound;
        [SerializeField]
        private AudioClip doorBlastSound;

        private const float TutorialPassAhead = 3.5f;
        private const float TutorialPassHalfWidth = 3f;

        private BossFightSettings settings;
        private OpeningSteps steps;
        private StoreLightingController lighting;
        private BossFightRig rig;
        private Camera playerCamera;
        private CharacterController controller;

        private int monsterCount;
        private bool costumeSpawned;
        private bool vaultDone;
        private bool vaulting;
        private float vaultElapsed;
        private Vector3 vaultStart;
        private Vector3 vaultEnd;
        private LineRenderer beam;
        private Transform shard;
        private float shardElapsed;
        private Vector3 shardStart;

        public bool Running { get; private set; }
        public OpeningStep CurrentStep => steps.Current.Step;

        private void Awake()
        {
            settings = BossFightConfig.SettingsOf(config);
            steps = new OpeningSteps(settings);
            if (director == null) director = FindFirstObjectByType<BossDirector>();
            if (player == null) player = FindFirstObjectByType<PlayerMovement>();
            if (witch == null && FindFirstObjectByType<WitchBoss>() != null) witch = FindFirstObjectByType<WitchBoss>().transform;
            if (spawner == null) spawner = FindFirstObjectByType<ClothingSpawner>();
            if (hud == null) hud = FindFirstObjectByType<BossHud>();
            if (crackOverlay == null) crackOverlay = FindFirstObjectByType<ScreenCrackOverlay>();
            if (flashlight == null) flashlight = FindFirstObjectByType<PlayerFlashlight>(FindObjectsInactive.Include);
            if (swordObject == null && FindFirstObjectByType<PlayerSword>(FindObjectsInactive.Include) != null)
            {
                swordObject = FindFirstObjectByType<PlayerSword>(FindObjectsInactive.Include).gameObject;
            }
            if (player != null) controller = player.GetComponent<CharacterController>();
            // Both tools are handed over during the tutorial.
            if (swordObject != null) swordObject.SetActive(false);
            if (flashlight != null) flashlight.gameObject.SetActive(false);
            if (monitorGlass != null) monitorGlass.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            if (director != null) director.OpeningRequested += Begin;
        }

        private void OnDisable()
        {
            if (director != null) director.OpeningRequested -= Begin;
            RestoreController();
        }

        private void Begin(int count)
        {
            if (Running) return;
            monsterCount = count;
            Running = true;
            EnterStep(steps.Current.Step);
        }

        private void Update()
        {
            if (!Running) return;
            var mouse = Mouse.current;
            var signals = new OpeningSignals
            {
                LeftClick = mouse != null && mouse.leftButton.wasPressedThisFrame,
                RightClick = mouse != null && mouse.rightButton.wasPressedThisFrame,
                RightHeld = mouse != null && mouse.rightButton.isPressed,
                VaultDone = vaultDone,
                CostumeDead = costumeSpawned && (spawner == null || spawner.AliveCount == 0),
            };
            UpdateStep(steps.Current.Step);
            if (!steps.Advance(signals, Time.deltaTime)) return;
            if (steps.Finished)
            {
                Running = false;
                return;
            }
            EnterStep(steps.Current.Step);
        }

        private void EnterStep(OpeningStep step)
        {
            switch (step)
            {
                case OpeningStep.LockPlayer: LockPlayer(); break;
                case OpeningStep.MagicToMonitor: StartMagic(); break;
                case OpeningStep.ShatterMonitor: ShatterMonitor(); break;
                case OpeningStep.CrackScreen: CrackScreen(); break;
                case OpeningStep.WitchLine: BossTextBox.Show(WitchLine, settings.openingWitchLineSeconds); break;
                case OpeningStep.GrabSword: Prompt(SwordPrompt); break;
                case OpeningStep.GrabFlashlight:
                    if (swordObject != null) swordObject.SetActive(true);
                    Prompt(FlashlightPrompt);
                    break;
                case OpeningStep.MoveLine:
                    GiveFlashlight();
                    Prompt(MovePrompt);
                    break;
                case OpeningStep.Vault: StartVault(); break;
                case OpeningStep.TutorialLight: SummonCostume(); break;
                case OpeningStep.TutorialSlash: Prompt(SlashPrompt); break;
                case OpeningStep.DoorBlast: BlastDoor(); break;
            }
        }

        // Tutorial prompts live on the glass HUD; only the Witch speaks in the bottom text box.
        private void Prompt(string text)
        {
            BossTextBox.Hide();
            if (hud != null) hud.ShowPrompt(text);
            else BossTextBox.Show(text, 0f);
        }

        private void ClearPrompt()
        {
            BossTextBox.Hide();
            if (hud != null) hud.HidePrompt();
        }

        private void UpdateStep(OpeningStep step)
        {
            switch (step)
            {
                case OpeningStep.MagicToMonitor: UpdateBeam(); break;
                case OpeningStep.ShatterMonitor: UpdateShard(); break;
                case OpeningStep.Vault: UpdateVault(); break;
            }
        }

        private void LockPlayer()
        {
            if (player != null) player.EnableLookOnly();
            var cameraBehaviour = FindFirstObjectByType<CameraBehaviour>();
            if (cameraBehaviour != null) cameraBehaviour.DisableForBoss();
            if (hud != null) hud.SetVisible(false);
            lighting = FindFirstObjectByType<StoreLightingController>();
            if (lighting != null) lighting.ForceNormal = true;
            rig = FindFirstObjectByType<BossFightRig>();
        }

        private Vector3 WitchHand()
        {
            if (witch == null) return transform.position;
            var collider = witch.GetComponent<Collider>();
            return collider != null ? collider.bounds.center : witch.position + Vector3.up * 1.5f;
        }

        private Vector3 MonitorCenter()
        {
            if (monitor == null) return WitchHand() + Vector3.forward;
            var renderer = monitor.GetComponentInChildren<Renderer>();
            return renderer != null ? renderer.bounds.center : monitor.position;
        }

        private void StartMagic()
        {
            var go = new GameObject("WitchMagicBeam");
            beam = go.AddComponent<LineRenderer>();
            beam.positionCount = 14;
            beam.startWidth = 0.1f;
            beam.endWidth = 0.04f;
            beam.sharedMaterial = magicMaterial;
            beam.startColor = beam.endColor = new Color(0.7f, 0.3f, 1f, 1f);
            beam.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            MakeEmitter(go.transform, new Color(0.75f, 0.4f, 1f, 1f));
            if (magicSound != null) AudioSource.PlayClipAtPoint(magicSound, WitchHand());
            UpdateBeam();
        }

        private void UpdateBeam()
        {
            if (beam == null) return;
            var from = WitchHand();
            var to = MonitorCenter();
            for (int i = 0; i < beam.positionCount; i++)
            {
                float t = (float)i / (beam.positionCount - 1);
                var jitter = i == 0 || i == beam.positionCount - 1 ? Vector3.zero : Random.insideUnitSphere * 0.08f;
                beam.SetPosition(i, Vector3.Lerp(from, to, t) + jitter);
            }
            beam.transform.position = to;
        }

        private void ShatterMonitor()
        {
            if (beam != null) Destroy(beam.gameObject);
            var center = MonitorCenter();
            // The computer stays; its screen turns to cracked glass.
            if (monitorGlass != null) monitorGlass.gameObject.SetActive(true);
            Burst(center, new Color(0.8f, 0.85f, 1f, 1f), 80, 0.3f, 5f);
            Burst(center, new Color(0.75f, 0.35f, 1f, 1f), 40, 0.3f, 3f);
            if (shatterSound != null) AudioSource.PlayClipAtPoint(shatterSound, center);

            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "MonitorShard";
            Destroy(go.GetComponent<Collider>());
            go.transform.localScale = new Vector3(0.12f, 0.2f, 0.02f);
            go.transform.position = center;
            if (shardMaterial != null) go.GetComponent<Renderer>().sharedMaterial = shardMaterial;
            shard = go.transform;
            shardStart = center;
            shardElapsed = 0f;
        }

        private void UpdateShard()
        {
            if (shard == null) return;
            var camera = PlayerCamera();
            shardElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(shardElapsed / Mathf.Max(settings.openingShardFlightSeconds, 1e-3f));
            if (camera != null)
            {
                var target = camera.transform.position + camera.transform.forward * 0.3f;
                shard.position = Vector3.Lerp(shardStart, target, t * t);
                shard.Rotate(540f * Time.deltaTime, 360f * Time.deltaTime, 0f);
            }
            if (t >= 1f)
            {
                Destroy(shard.gameObject);
                shard = null;
                if (crackOverlay != null) crackOverlay.Show();
                if (hud != null) hud.SetVisible(true);
            }
        }

        // Wall cracks appear from the player's eye outward, so they only ever mark the inside of the shop.
        private void CrackScreen()
        {
            if (crackOverlay != null && !crackOverlay.Shown) crackOverlay.Show();
            if (hud != null) hud.SetVisible(true);
            if (wallCrackMaterial == null || player == null) return;
            var eye = player.transform.position + Vector3.up * 1.5f;
            int count = Mathf.Max(0, settings.openingWallCrackCount);
            for (int i = 0; i < count; i++)
            {
                var direction = Quaternion.Euler(0f, 360f * i / Mathf.Max(count, 1) + Random.Range(-10f, 10f), 0f) * Vector3.forward;
                var origin = eye + Vector3.up * Random.Range(-0.4f, 0.6f);
                if (!Physics.Raycast(origin, direction, out var hit, 40f, ~0, QueryTriggerInteraction.Ignore)) continue;
                if (Mathf.Abs(hit.normal.y) > 0.3f || hit.rigidbody != null || hit.collider.GetComponentInParent<IDamageable>() != null) continue;
                PlaceWallCrack(hit.point, hit.normal, i);
            }
        }

        private void PlaceWallCrack(Vector3 point, Vector3 normal, int seed)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "WallCrack";
            Destroy(quad.GetComponent<Collider>());
            quad.transform.position = point + normal * 0.012f;
            quad.transform.rotation = Quaternion.LookRotation(-normal) * Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            quad.transform.localScale = Vector3.one * Random.Range(1.3f, 2.2f);
            var renderer = quad.GetComponent<Renderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var material = new Material(wallCrackMaterial);
            var texture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            var segments = CrackPattern.Generate(100 + seed, new Vector2(0.5f, 0.5f), 6, 6, 0.9f);
            texture.SetPixels32(CrackPattern.Rasterize(128, segments, new Color(0.05f, 0.04f, 0.06f, 0.85f)));
            texture.Apply();
            material.SetTexture("_BaseMap", texture);
            renderer.sharedMaterial = material;
        }

        private void GiveFlashlight()
        {
            if (flashlight == null) return;
            flashlight.gameObject.SetActive(true);
            flashlight.BrightMultiplier = settings.openingFlashlightMultiplier;
        }

        private void StartVault()
        {
            ClearPrompt();
            if (player == null)
            {
                vaultDone = true;
                return;
            }
            vaultStart = player.transform.position;
            if (vaultTarget != null)
            {
                vaultEnd = vaultTarget.position;
            }
            else
            {
                var forward = Vector3.ProjectOnPlane(player.transform.forward, Vector3.up).normalized;
                vaultEnd = vaultStart + forward * settings.openingVaultDistance;
            }
            vaultElapsed = 0f;
            vaulting = true;
            player.ScriptedMotion = true;
            if (controller != null) controller.enabled = false;
        }

        private void UpdateVault()
        {
            if (!vaulting) return;
            vaultElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(vaultElapsed / Mathf.Max(settings.openingVaultSeconds, 1e-3f));
            float eased = Mathf.SmoothStep(0f, 1f, t);
            var position = Vector3.Lerp(vaultStart, vaultEnd, eased);
            position.y += settings.openingVaultArcHeight * 4f * t * (1f - t);
            player.transform.position = position;
            if (t >= 1f)
            {
                vaulting = false;
                RestoreController();
                vaultDone = true;
            }
        }

        private void RestoreController()
        {
            if (player != null) player.ScriptedMotion = false;
            if (controller != null) controller.enabled = true;
        }

        private void SummonCostume()
        {
            Prompt(LightPrompt);
            if (spawner == null || player == null) return;
            // Starts off to one side of the player and floats back and forth across their view; it cannot hurt them.
            TutorialRules.PassEndpoints(player.transform.position, player.transform.forward, TutorialPassAhead, TutorialPassHalfWidth, out var from, out var to);
            from = OnNavMesh(from);
            to = OnNavMesh(to);
            costumeSpawned = spawner.SpawnHarmless(from, from, to) != null;
        }

        private static Vector3 OnNavMesh(Vector3 point)
        {
            return NavMesh.SamplePosition(point, out var hit, 4f, NavMesh.AllAreas) ? hit.position : point;
        }

        private void BlastDoor()
        {
            ClearPrompt();
            var center = door != null ? door.position + Vector3.up * 1.2f : transform.position;
            if (door != null)
            {
                var renderers = door.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length > 0)
                {
                    var bounds = renderers[0].bounds;
                    foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                    if (bounds.size.sqrMagnitude > 1e-4f) center = bounds.center;
                }
                foreach (var r in renderers) r.enabled = false;
                foreach (var c in door.GetComponentsInChildren<Collider>(true)) c.enabled = false;
                // Out of the scene entirely, so the NavMesh rebuild below sees an open doorway.
                door.gameObject.SetActive(false);
            }
            Burst(center, new Color(0.8f, 0.9f, 1f, 1f), 160, 1f, 7f);
            Burst(center, new Color(0.75f, 0.35f, 1f, 1f), 60, 1f, 4f);
            if (doorBlastSound != null) AudioSource.PlayClipAtPoint(doorBlastSound, center);

            if (rig == null) rig = FindFirstObjectByType<BossFightRig>();
            if (rig != null && rig.NavMeshSurface != null) rig.NavMeshSurface.BuildNavMesh();
            WarnIfDoorwayClosed();
            if (doorBarrier != null) doorBarrier.enabled = true;
            if (lighting != null) lighting.ForceNormal = false;
            if (flashlight != null) flashlight.BrightMultiplier = 1f;
            if (director != null) director.BeginFight(monsterCount);
        }

        private void WarnIfDoorwayClosed()
        {
            var monsterSpawner = FindFirstObjectByType<MonsterSpawner>();
            if (monsterSpawner == null || monsterSpawner.SpawnPoint == null || player == null) return;
            var from = monsterSpawner.SpawnPoint.position;
            var to = player.transform.position;
            if (NavMesh.SamplePosition(from, out var start, 4f, NavMesh.AllAreas)) from = start.position;
            if (NavMesh.SamplePosition(to, out var end, 4f, NavMesh.AllAreas)) to = end.position;
            var path = new NavMeshPath();
            bool found = NavMesh.CalculatePath(from, to, NavMesh.AllAreas, path);
            var last = path.corners.Length > 0 ? path.corners[path.corners.Length - 1] : from;
            if (!found || !DoorwayRules.IsPathComplete(path.status, last, to, 0.5f))
            {
                Debug.LogWarning("Boss fight: no complete NavMesh path from the monster spawn point into the store after the door blast.");
            }
        }

        private Camera PlayerCamera()
        {
            if (playerCamera == null && player != null) playerCamera = player.GetComponentInChildren<Camera>();
            if (playerCamera == null) playerCamera = Camera.main;
            return playerCamera;
        }

        private void Burst(Vector3 position, Color color, int count, float radius, float speed)
        {
            var go = new GameObject("OpeningBurst");
            go.transform.position = position;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.4f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.16f);
            main.startColor = color;
            main.gravityModifier = 0.8f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, count) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = radius;
            if (shardMaterial != null) go.GetComponent<ParticleSystemRenderer>().sharedMaterial = shardMaterial;
            ps.Play();
            Destroy(go, 4f);
        }

        private void MakeEmitter(Transform parent, Color color)
        {
            var go = new GameObject("MagicSparks");
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.startLifetime = 0.6f;
            main.startSpeed = 1.5f;
            main.startSize = 0.07f;
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.rateOverTime = 40f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.25f;
            if (shardMaterial != null) go.GetComponent<ParticleSystemRenderer>().sharedMaterial = shardMaterial;
            ps.Play();
        }
    }
}
