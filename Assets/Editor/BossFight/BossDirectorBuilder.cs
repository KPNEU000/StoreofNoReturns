using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BossFight.Rules;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace BossFight.Editor
{
    // Places the Witch (shield + shatter burst) by the door, the BossDirector with DebugCheats, and the fight HUD in the Boss Scene.
    public static class BossDirectorBuilder
    {
        public const string DoorName = "FrontDoor";
        public const string WitchName = "Witch";
        public const string ShieldName = "WitchShield";
        public const string ShatterName = "ShieldShatter";
        public const string DirectorName = "BossDirector";
        public const string HudName = "BossHudCanvas";
        public const string HudMoneyName = "BossMoneyText";
        public const string HudObjectiveName = "BossObjectiveText";
        public const string WitchPrefab = "Assets/Prefabs/CC Attribution Characters/Witch.prefab";
        public const string ShieldMaterial = BossFightPaths.SettingsFolder + "/WitchShield.mat";
        public const string ShatterMaterial = BossFightPaths.SettingsFolder + "/WitchShatter.mat";
        private const float WitchDepthInside = 1.5f;
        private const float WitchLaneMargin = 0.75f;
        // The team's shield (2026-10-23, strongpizzaboy): a slab mesh posed in the Witch's local space.
        public const string ShieldMesh = "Assets/Models/Cube.mesh";
        internal static readonly Vector3 ShieldLocalPosition = new Vector3(0f, 1.31f, 0f);
        internal static readonly Quaternion ShieldLocalRotation = Quaternion.Euler(90f, 0f, 0f);
        internal static readonly Vector3 ShieldLocalScale = new Vector3(80f, 1f, 150f);
        public const float LaneHalfWidth = 0.5f;
        private static readonly Color ShieldColor = new Color(0.6f, 0.2f, 1f, 0.3f);
        private static readonly Color ShatterColor = new Color(0.75f, 0.35f, 1f, 0.9f);

        public static void Build()
        {
            var config = ConfigBuilder.Load();
            EditorFolders.Ensure(BossFightPaths.SettingsFolder);
            var shieldMat = BuilderUtil.EnsureTransparentMaterial(ShieldMaterial, "Universal Render Pipeline/Unlit", ShieldColor);
            var shatterMat = BuilderUtil.EnsureTransparentMaterial(ShatterMaterial, "Universal Render Pipeline/Particles/Unlit", ShatterColor);
            var witchPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WitchPrefab);
            if (witchPrefab == null)
            {
                throw new System.InvalidOperationException($"Witch prefab missing at {WitchPrefab}");
            }

            using (var boss = new BossSceneScope())
            {
                var player = BuilderUtil.FindPlayer(boss.Scene);
                if (player == null)
                {
                    throw new System.InvalidOperationException("Boss Scene has no PlayerMovement object");
                }
                var moneyManager = BuilderUtil.FindInScene<MoneyManager>(boss.Scene);
                if (moneyManager == null)
                {
                    throw new System.InvalidOperationException("Boss Scene has no MoneyManager");
                }
                var reputationManager = BuilderUtil.FindInScene<ReputationManager>(boss.Scene);
                var door = BuilderUtil.FindByName(boss.Scene, DoorName);
                if (door == null)
                {
                    throw new System.InvalidOperationException($"Boss Scene has no object named '{DoorName}' to place the Witch by");
                }

                bool changed = false;
                var director = EnsureRootComponent<BossDirector>(boss.Scene, DirectorName, ref changed);
                var cheats = director.GetComponent<DebugCheats>();
                if (cheats == null)
                {
                    cheats = director.gameObject.AddComponent<DebugCheats>();
                    changed = true;
                }

                var witch = EnsureWitch(boss.Scene, witchPrefab, door, player, ref changed);
                var shield = EnsureShield(witch, shieldMat, ref changed);
                PlaceWitch(witch, shield, door, player, ref changed);
                changed |= FaceShieldToPlayer(shield, player);
                var burst = EnsureShatter(witch, shield.transform, shatterMat, ref changed);
                changed |= PlaceShatter(witch, shield.transform, burst);
                var hud = EnsureHud(boss.Scene, ref changed, out var moneyText, out var objectiveText);

                var so = new SerializedObject(director);
                changed |= BuilderUtil.SetReference(so, "config", config);
                changed |= BuilderUtil.SetReference(so, "moneyManager", moneyManager);
                changed |= BuilderUtil.SetReference(so, "player", player);
                changed |= so.ApplyModifiedPropertiesWithoutUndo();

                so = new SerializedObject(cheats);
                changed |= BuilderUtil.SetReference(so, "config", config);
                changed |= BuilderUtil.SetReference(so, "director", director);
                changed |= BuilderUtil.SetReference(so, "moneyManager", moneyManager);
                changed |= BuilderUtil.SetReference(so, "reputationManager", reputationManager);
                changed |= so.ApplyModifiedPropertiesWithoutUndo();

                so = new SerializedObject(witch);
                changed |= BuilderUtil.SetReference(so, "config", config);
                changed |= BuilderUtil.SetReference(so, "director", director);
                changed |= BuilderUtil.SetReference(so, "shield", shield);
                changed |= BuilderUtil.SetReference(so, "shatterBurst", burst);
                changed |= so.ApplyModifiedPropertiesWithoutUndo();

                so = new SerializedObject(hud);
                changed |= BuilderUtil.SetReference(so, "director", director);
                changed |= BuilderUtil.SetReference(so, "moneyManager", moneyManager);
                changed |= BuilderUtil.SetReference(so, "moneyText", moneyText);
                changed |= BuilderUtil.SetReference(so, "objectiveText", objectiveText);
                changed |= so.ApplyModifiedPropertiesWithoutUndo();

                if (changed)
                {
                    EditorSceneManager.MarkSceneDirty(boss.Scene);
                }
                boss.SaveIfDirty();
            }
        }

        // Called by BossRigValidator with the Boss Scene open.
        public static void Validate(UnityEngine.SceneManagement.Scene scene, List<string> errors)
        {
            foreach (var path in new[] { ShieldMaterial, ShatterMaterial })
            {
                if (AssetDatabase.LoadAssetAtPath<Material>(path) == null)
                {
                    errors.Add($"Material missing at {path}");
                }
            }

            var director = BuilderUtil.FindInScene<BossDirector>(scene);
            if (director == null)
            {
                errors.Add($"Boss Scene has no {DirectorName} with a BossDirector component");
            }
            else
            {
                BuilderUtil.RequireFields(director, new[] { "config", "moneyManager", "player" }, errors);
                var cheats = director.GetComponent<DebugCheats>();
                if (cheats == null)
                {
                    errors.Add("Boss Scene BossDirector has no DebugCheats");
                }
                else
                {
                    BuilderUtil.RequireFields(cheats, new[] { "config", "director", "moneyManager" }, errors);
                }
            }

            var witch = BuilderUtil.FindInScene<WitchBoss>(scene);
            if (witch == null)
            {
                errors.Add("Boss Scene has no WitchBoss");
            }
            else
            {
                BuilderUtil.RequireFields(witch, new[] { "config", "director", "shield", "shatterBurst" }, errors);
                if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(witch.gameObject) != WitchPrefab)
                {
                    errors.Add($"Boss Scene WitchBoss is not an instance of {WitchPrefab}");
                }
                var shield = witch.transform.Find(ShieldName);
                var renderer = shield != null ? shield.GetComponent<Renderer>() : null;
                if (renderer == null || renderer.sharedMaterial == null)
                {
                    errors.Add($"Boss Scene Witch has no {ShieldName} with a material");
                }
                if (shield != null && shield.GetComponent<Collider>() != null)
                {
                    errors.Add($"Boss Scene {ShieldName} must not have a collider");
                }
                var facing = shield != null ? shield.GetComponent<ShieldFacesPlayer>() : null;
                if (shield != null && facing == null)
                {
                    errors.Add($"Boss Scene {ShieldName} has no ShieldFacesPlayer");
                }
                else if (facing != null)
                {
                    BuilderUtil.RequireFields(facing, new[] { "player" }, errors);
                }
                var shieldFilter = shield != null ? shield.GetComponent<MeshFilter>() : null;
                if (shieldFilter != null && shieldFilter.sharedMesh != AssetDatabase.LoadAssetAtPath<Mesh>(ShieldMesh))
                {
                    errors.Add($"Boss Scene {ShieldName} must use {ShieldMesh}");
                }
                var door = BuilderUtil.FindByName(scene, DoorName);
                if (door == null)
                {
                    errors.Add($"Boss Scene has no '{DoorName}' prefab instance for the door blast");
                }
                else
                {
                    var player = BuilderUtil.FindPlayer(scene);
                    var bounds = OpeningBuilder.DoorBounds(door);
                    if (player != null)
                    {
                        var inward = player.transform.position - bounds.center;
                        inward.y = 0f;
                        Footprint(witch, out var radius, out _);
                        if (!DoorwayRules.IsClearOfLane(witch.transform.position, radius, bounds.center, inward, LaneHalfWidth))
                        {
                            errors.Add("Boss Scene Witch stands in the doorway lane");
                        }
                    }
                }
            }

            var hud = BuilderUtil.FindInScene<BossHud>(scene);
            if (hud == null)
            {
                errors.Add($"Boss Scene has no {HudName} with a BossHud component");
            }
            else
            {
                BuilderUtil.RequireFields(hud, new[] { "director", "moneyManager", "moneyText", "objectiveText" }, errors);
                foreach (var label in hud.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (label.font == null)
                    {
                        errors.Add($"Boss Scene HUD text {label.name} has no font");
                    }
                }
            }
        }

        private static T EnsureRootComponent<T>(UnityEngine.SceneManagement.Scene scene, string name, ref bool changed) where T : Component
        {
            var existing = BuilderUtil.FindInScene<T>(scene);
            if (existing != null)
            {
                return existing;
            }
            var go = new GameObject(name);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
            changed = true;
            return go.AddComponent<T>();
        }

        // The Witch.prefab instance; PlaceWitch decides where it stands. WitchBoss applies the height scale at runtime.
        private static WitchBoss EnsureWitch(UnityEngine.SceneManagement.Scene scene, GameObject prefab, Transform door, PlayerMovement player, ref bool changed)
        {
            var existing = BuilderUtil.FindInScene<WitchBoss>(scene);
            if (existing != null)
            {
                return existing;
            }
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.name = WitchName;
            changed = true;
            return instance.AddComponent<WitchBoss>();
        }

        // World radius and height of the Witch's capsule as she stands in the scene (before WitchBoss's runtime height scale).
        private static void Footprint(WitchBoss witch, out float radius, out float height)
        {
            var capsule = witch.GetComponent<CapsuleCollider>();
            var scale = witch.transform.lossyScale;
            height = capsule != null ? capsule.height * scale.y : 2.6f;
            radius = capsule != null ? capsule.radius * Mathf.Max(scale.x, scale.z) : 0.5f;
        }

        // World horizontal radius and top height of the shield, from its mesh bounds and its pose on the Witch.
        private static void ShieldSize(WitchBoss witch, out float radius, out float height)
        {
            var bounds = LoadShieldMesh().bounds;
            var scale = witch.transform.lossyScale;
            radius = 0f;
            height = 0f;
            for (int i = 0; i < 8; i++)
            {
                var corner = bounds.center + Vector3.Scale(bounds.extents, new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                var p = Vector3.Scale(scale, ShieldLocalPosition + ShieldLocalRotation * Vector3.Scale(ShieldLocalScale, corner));
                radius = Mathf.Max(radius, Mathf.Abs(p.x), Mathf.Abs(p.z));
                height = Mathf.Max(height, p.y);
            }
        }

        private static Mesh LoadShieldMesh()
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(ShieldMesh);
            if (mesh == null) throw new System.InvalidOperationException($"Witch shield mesh missing: {ShieldMesh}");
            return mesh;
        }

        // On the floor beside the entrance, clear of the doorway lane, facing the player. Re-applied every build.
        private static void PlaceWitch(WitchBoss witch, GameObject shield, Transform door, PlayerMovement player, ref bool changed)
        {
            ShieldSize(witch, out var domeRadius, out _);
            var bounds = OpeningBuilder.DoorBounds(door);
            var doorCenter = bounds.center;
            doorCenter.y = BuilderUtil.PlayerFloorY(player);
            var inward = BuilderUtil.TowardsPlayer(doorCenter, player);
            var lateral = Vector3.Cross(Vector3.up, inward);
            float halfWidth = Mathf.Abs(lateral.x) * bounds.extents.x + Mathf.Abs(lateral.z) * bounds.extents.z;
            halfWidth = Mathf.Max(halfWidth, LaneHalfWidth);

            var position = DoorwayRules.WitchPosition(doorCenter, inward, player.transform.position, halfWidth, domeRadius, WitchDepthInside, WitchLaneMargin);
            var toPlayer = player.transform.position - position;
            toPlayer.y = 0f;
            var facing = toPlayer.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(toPlayer.normalized) : Quaternion.identity;
            changed |= BuilderUtil.SetPose(witch.transform, position, facing);
            // The shield is sized and anchored from the Witch's final transform.
            changed |= FitShield(witch, shield.transform);
        }

        // Created from a primitive for its renderer; FitShield swaps in the team's mesh and pose.
        private static GameObject EnsureShield(WitchBoss witch, Material material, ref bool changed)
        {
            var existing = witch.transform.Find(ShieldName);
            if (existing != null)
            {
                return existing.gameObject;
            }
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = ShieldName;
            Object.DestroyImmediate(sphere.GetComponent<Collider>());
            sphere.transform.SetParent(witch.transform, false);
            FitShield(witch, sphere.transform);

            var renderer = sphere.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            changed = true;
            return sphere;
        }

        private static bool FitShield(WitchBoss witch, Transform shield)
        {
            var mesh = LoadShieldMesh();
            var filter = shield.GetComponent<MeshFilter>();
            bool changed = false;
            if (filter.sharedMesh != mesh)
            {
                filter.sharedMesh = mesh;
                changed = true;
            }
            if ((shield.localPosition - ShieldLocalPosition).sqrMagnitude > 1e-8f
                || Quaternion.Angle(shield.localRotation, ShieldLocalRotation) > 0.01f
                || (shield.localScale - ShieldLocalScale).sqrMagnitude > 1e-8f)
            {
                shield.localPosition = ShieldLocalPosition;
                shield.localRotation = ShieldLocalRotation;
                shield.localScale = ShieldLocalScale;
                changed = true;
            }
            return changed;
        }

        private static bool FaceShieldToPlayer(GameObject shield, PlayerMovement player)
        {
            bool changed = false;
            var facing = shield.GetComponent<ShieldFacesPlayer>();
            if (facing == null)
            {
                facing = shield.AddComponent<ShieldFacesPlayer>();
                changed = true;
            }
            var so = new SerializedObject(facing);
            changed |= BuilderUtil.SetReference(so, "player", player.transform);
            var rest = so.FindProperty("restRotation");
            if (Quaternion.Angle(rest.quaternionValue, ShieldLocalRotation) > 0.01f)
            {
                rest.quaternionValue = ShieldLocalRotation;
                changed = true;
            }
            changed |= so.ApplyModifiedPropertiesWithoutUndo();
            return changed;
        }

        private static ParticleSystem EnsureShatter(WitchBoss witch, Transform shield, Material material, ref bool changed)
        {
            var existing = witch.transform.Find(ShatterName);
            if (existing != null && existing.GetComponent<ParticleSystem>() != null)
            {
                return existing.GetComponent<ParticleSystem>();
            }
            var go = new GameObject(ShatterName);
            go.transform.SetParent(witch.transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.25f);
            main.startColor = ShatterColor;
            main.gravityModifier = 0.6f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 120) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            changed = true;
            return ps;
        }

        // Burst sits half the shield's height up and spans the shield's radius.
        private static bool PlaceShatter(WitchBoss witch, Transform shield, ParticleSystem burst)
        {
            ShieldSize(witch, out var radius, out var height);
            var scale = witch.transform.lossyScale;
            var local = new Vector3(0f, height * 0.5f / scale.y, 0f);
            var shape = burst.shape;
            float shapeRadius = radius / Mathf.Max(scale.x, scale.z);
            bool same = (burst.transform.localPosition - local).sqrMagnitude < 1e-8f && Mathf.Abs(shape.radius - shapeRadius) < 1e-4f;
            if (same)
            {
                return false;
            }
            burst.transform.localPosition = local;
            shape.radius = shapeRadius;
            return true;
        }

        private static BossHud EnsureHud(UnityEngine.SceneManagement.Scene scene, ref bool changed, out TMP_Text moneyText, out TMP_Text objectiveText)
        {
            var hud = BuilderUtil.FindInScene<BossHud>(scene);
            if (hud == null)
            {
                var canvasGo = new GameObject(HudName, typeof(Canvas), typeof(CanvasScaler));
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(canvasGo, scene);
                var canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 90;
                var scaler = canvasGo.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                hud = canvasGo.AddComponent<BossHud>();
                changed = true;
            }
            moneyText = EnsureLabel(hud.transform, HudMoneyName, new Vector2(-40f, -30f), 44f, ref changed);
            objectiveText = EnsureLabel(hud.transform, HudObjectiveName, new Vector2(-40f, -90f), 30f, ref changed);
            return hud;
        }

        private static TMP_Text EnsureLabel(Transform parent, string name, Vector2 offset, float size, ref bool changed)
        {
            // Anywhere under the HUD: OpeningBuilder moves the labels into the glass frame.
            var existing = BuilderUtil.FindLabel(parent, name);
            if (existing != null) return existing;
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.one;
            rect.anchorMax = Vector2.one;
            rect.pivot = Vector2.one;
            rect.anchoredPosition = offset;
            rect.sizeDelta = new Vector2(900f, size * 1.4f);
            var label = go.AddComponent<TextMeshProUGUI>();
            label.font = FontBuilder.Load();
            label.fontSize = size;
            label.alignment = TextAlignmentOptions.TopRight;
            label.color = Color.white;
            label.raycastTarget = false;
            label.text = "";
            changed = true;
            return label;
        }
    }
}
