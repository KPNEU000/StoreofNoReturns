using BossFight.Rules;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace BossFight
{
    // The Witch's arrival: cinematic, then a tutorial (grab the sword, slash a costume, walk up and hit the Witch),
    // then she shields, leaps back to the door and blasts it open. The flashlight is automatic (PlayerFlashlight).
    // OpeningSteps decides when each step ends; this class does what each step looks like.
    public class OpeningSequence : MonoBehaviour
    {
        private const string WitchLine = "I'm turning everyone who hates you into monsters…";
        private const string SwordPrompt = "Left-click: grab the sword";
        private const string SlashPrompt = "What is that? Left-click to slash it.";
        private const string HitWitchPrompt = "WASD to move. Hit the Witch!";

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
        private Material screenCrackMaterial;
        [SerializeField]
        private AudioClip magicSound;
        [SerializeField]
        private AudioClip shatterSound;
        [SerializeField]
        private AudioClip doorBlastSound;

        public const float TutorialPassAhead = 1.6f; // inside the 2.2 m slash reach
        public const float TutorialPassHalfWidth = 2f;

        private BossFightSettings settings;
        private OpeningSteps steps;
        private StoreLightingController lighting;
        private BossFightRig rig;
        private Camera playerCamera;
        private CharacterController controller;

        private int monsterCount;
        private bool costumeSpawned;
        private bool vaultDone;
        // The Witch casts from the customer spot in front of the counter (in view), then glides back to her post.
        private Vector3 witchPost;
        private Quaternion witchPostRotation;
        private Vector3 witchMoveFrom;
        private Vector3 witchMoveTo;
        private Quaternion witchMoveRotation;
        private float witchMoveArc;
        private float witchMoveElapsed = -1f;
        private WitchBoss witchBoss;
        private bool witchHit;
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
            // The sword is handed over during the tutorial; the flashlight is always held.
            if (swordObject != null) swordObject.SetActive(false);
            if (monitorGlass != null) monitorGlass.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            if (director != null)
            {
                director.OpeningRequested += Begin;
                director.IntroWitchHit += OnIntroWitchHit;
            }
        }

        private void OnIntroWitchHit()
        {
            witchHit = true;
        }

        private void OnDisable()
        {
            if (director != null)
            {
                director.OpeningRequested -= Begin;
                director.IntroWitchHit -= OnIntroWitchHit;
            }
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
                WitchHit = witchHit,
                VaultDone = vaultDone,
                CostumeDead = costumeSpawned && (spawner == null || spawner.AliveCount == 0),
            };
            UpdateStep(steps.Current.Step);
            UpdateWitchMove();
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
                case OpeningStep.GrabSword:
                    MoveWitch(TutorialWitchSpot(), FacingPlayer());
                    Prompt(SwordPrompt);
                    break;
                case OpeningStep.Vault:
                    if (swordObject != null) swordObject.SetActive(true);
                    StartVault();
                    break;
                case OpeningStep.TutorialSlash: SummonCostume(); break;
                case OpeningStep.HitWitch: StartWitchHunt(); break;
                case OpeningStep.WitchRetreat: WitchRetreats(); break;
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
            witchBoss = witch != null ? witch.GetComponent<WitchBoss>() : null;
            // Unshielded through the tutorial so the player can land the first hit on her.
            if (witchBoss != null) witchBoss.SetShield(false);
            BringWitchToCounter();
        }

        // Free movement and one open hit on the Witch, who waits down the aisle behind the tutorial costume.
        private void StartWitchHunt()
        {
            if (player != null) player.enableMovement();
            if (director != null) director.OpenIntroHit();
            Prompt(HitWitchPrompt);
        }

        // The hit landed: she throws up her shield and leaps back to her post by the door.
        private void WitchRetreats()
        {
            ClearPrompt();
            if (witchBoss != null) witchBoss.SetShield(true);
            Burst(WitchHand(), new Color(0.75f, 0.35f, 1f, 1f), 120, 0.6f, 4f);
            MoveWitch(witchPost, witchPostRotation, 1.5f);
        }

        private Vector3 TutorialWitchSpot()
        {
            if (player == null) return witch != null ? witch.position : Vector3.zero;
            var forward = StoreForward();
            var start = vaultTarget != null ? vaultTarget.position : player.transform.position + forward * settings.openingVaultDistance;
            var spot = OnNavMesh(start + forward * settings.openingWitchTutorialDistance);
            return new Vector3(spot.x, witchPost.y, spot.z);
        }

        // Playtest round 8: the Witch, the tutorial costume and the vault used to follow wherever the player was
        // looking when they clicked (facing the back wall put them behind the counter). Everything now uses the
        // fixed direction from the player's start spot to the vault landing point: straight at the store front.
        private Vector3 storeForward;

        private Vector3 StoreForward()
        {
            if (storeForward == Vector3.zero && player != null)
            {
                storeForward = vaultTarget != null
                    ? Planar.Direction(vaultTarget.position - player.transform.position)
                    : Planar.Direction(player.transform.forward);
            }
            return storeForward;
        }

        private Quaternion FacingPlayer()
        {
            return player != null ? Quaternion.LookRotation(-StoreForward()) : witch.rotation;
        }

        // Round-2 playtest: from her post by the door she was lost in the fog, so she casts from right in front of the player.
        private void BringWitchToCounter()
        {
            if (witch == null || player == null) return;
            witchPost = witch.position;
            witchPostRotation = witch.rotation;
            var forward = StoreForward();
            var spot = player.transform.position + forward * settings.openingWitchCounterDistance;
            spot = new Vector3(spot.x, witchPost.y, spot.z);
            witch.position = spot;
            witch.rotation = Quaternion.LookRotation(-forward);
            Burst(WitchHand(), new Color(0.75f, 0.35f, 1f, 1f), 50, 0.6f, 3f);
        }

        // Drifts (or leaps, with a higher arc) to `to` over openingWitchReturnSeconds.
        private void MoveWitch(Vector3 to, Quaternion rotation, float arcHeight = 0.6f)
        {
            if (witch == null || witchPost == Vector3.zero) return;
            witchMoveFrom = witch.position;
            witchMoveTo = to;
            witchMoveRotation = rotation;
            witchMoveArc = arcHeight;
            witchMoveElapsed = 0f;
        }

        private void UpdateWitchMove()
        {
            if (witch == null || witchMoveElapsed < 0f) return;
            witchMoveElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(witchMoveElapsed / Mathf.Max(settings.openingWitchReturnSeconds, 1e-3f));
            float eased = t * t * (3f - 2f * t);
            witch.position = Vector3.Lerp(witchMoveFrom, witchMoveTo, eased) + Vector3.up * Mathf.Sin(t * Mathf.PI) * witchMoveArc;
            if (t >= 1f)
            {
                witch.rotation = witchMoveRotation;
                witchMoveElapsed = -1f;
            }
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
            // Playtest: more purple — a second swirl at the Witch's hand while she casts.
            if (witch != null)
            {
                var hand = new GameObject("WitchHandSparks").transform;
                hand.SetParent(go.transform, false);
                hand.position = WitchHand();
                MakeEmitter(hand, new Color(0.65f, 0.25f, 1f, 1f));
            }
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
            // The computer stays; its screen goes dark and cracked.
            BreakScreen();
            Burst(center, new Color(0.8f, 0.85f, 1f, 1f), 80, 0.3f, 5f);
            Burst(center, new Color(0.75f, 0.35f, 1f, 1f), 160, 0.45f, 3.5f);
            if (shatterSound != null) AudioSource.PlayClipAtPoint(shatterSound, center);

            // A jagged piece of the same cracked, glossy glass as the HUD shard it becomes.
            var go = new GameObject("MonitorShard", typeof(MeshFilter), typeof(MeshRenderer));
            go.GetComponent<MeshFilter>().sharedMesh = GlassShard.BuildMesh(settings, true);
            var shardRenderer = go.GetComponent<MeshRenderer>();
            shardRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var glass = HudGlassMaterial();
            if (glass != null) shardRenderer.sharedMaterial = glass;
            else if (shardMaterial != null) shardRenderer.sharedMaterial = shardMaterial;
            go.transform.localScale = Vector3.one * 0.2f;
            go.transform.position = center;
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
                // Lands where the glass HUD shard sits (top right), not the middle of the view.
                var hudRect = HudGlassViewport();
                var target = camera.ViewportToWorldPoint(new Vector3(hudRect.center.x, hudRect.center.y, 0.3f));
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

        // Swaps the monitor's UI canvas for a dark cracked screen covering the same rectangle.
        private void BreakScreen()
        {
            var canvas = monitor != null ? monitor.GetComponentInChildren<Canvas>() : null;
            if (canvas == null || screenCrackMaterial == null) return;
            var corners = new Vector3[4];
            ((RectTransform)canvas.transform).GetWorldCorners(corners); // bottom-left, top-left, top-right, bottom-right
            canvas.enabled = false;
            if (monitorGlass != null) monitorGlass.gameObject.SetActive(false);

            var go = new GameObject("BrokenScreen", typeof(MeshFilter), typeof(MeshRenderer));
            var mesh = new Mesh { name = "BrokenScreen", vertices = corners, uv = new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right } };
            // Face the player: a triangle's front is the side its (b - a) x (c - a) normal points to.
            var camera = PlayerCamera();
            var viewer = camera != null ? camera.transform.position : corners[0] - canvas.transform.forward;
            var normal = Vector3.Cross(corners[1] - corners[0], corners[2] - corners[0]);
            bool facesViewer = Vector3.Dot(normal, viewer - corners[0]) > 0f;
            mesh.triangles = facesViewer ? new[] { 0, 1, 2, 0, 2, 3 } : new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateBounds();
            go.GetComponent<MeshFilter>().sharedMesh = mesh;

            var texture = new Texture2D(256, 256, TextureFormat.RGBA32, false);
            var pixels = CrackPattern.Rasterize(256, CrackPattern.Generate(31, new Vector2(0.62f, 0.55f), 9, 7, 0.95f), new Color(0.85f, 0.9f, 1f, 1f));
            var dark = new Color32(6, 7, 10, 245);
            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].a < 40) pixels[i] = dark;
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            var material = new Material(screenCrackMaterial);
            material.SetTexture("_MainTex", texture);
            var meshRenderer = go.GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private static Material HudGlassMaterial()
        {
            foreach (var glass in FindObjectsByType<GlassShard>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (glass.FollowsCamera) return glass.GetComponent<MeshRenderer>().sharedMaterial;
            }
            return null;
        }

        private Rect HudGlassViewport()
        {
            foreach (var glass in FindObjectsByType<GlassShard>(FindObjectsSortMode.None))
            {
                if (glass.FollowsCamera) return glass.Viewport;
            }
            return GlassShardRules.ScreenRect(settings.glassHudWidth, settings.glassHudHeight, settings.glassHudMargin);
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
                var forward = StoreForward();
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
            Prompt(SlashPrompt);
            if (spawner == null || player == null) return;
            // Starts off to one side of the player and floats back and forth across their view; it cannot hurt them.
            TutorialRules.PassEndpoints(player.transform.position, StoreForward(), TutorialPassAhead, TutorialPassHalfWidth, out var from, out var to);
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
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
            main.startColor = color;
            main.maxParticles = 400;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.rateOverTime = 140f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.35f;
            if (shardMaterial != null) go.GetComponent<ParticleSystemRenderer>().sharedMaterial = shardMaterial;
            ps.Play();
        }
    }
}
