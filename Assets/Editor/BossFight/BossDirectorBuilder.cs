using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace BossFight.Editor
{
    // Places the Witch (shield + shatter burst) by the door, the BossDirector with DebugCheats, and the fight HUD in the Boss Scene.
    public static class BossDirectorBuilder
    {
        public const string DoorName = "GlassDoor";
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
        private const float WitchDistanceFromDoor = 2.5f;
        private static readonly Color ShieldColor = new Color(0.6f, 0.2f, 1f, 0.3f);
        private static readonly Color ShatterColor = new Color(0.75f, 0.35f, 1f, 0.9f);

        public static void Build()
        {
            var config = ConfigBuilder.Load();
            EditorFolders.Ensure(BossFightPaths.SettingsFolder);
            var shieldMat = EnsureTransparentMaterial(ShieldMaterial, "Universal Render Pipeline/Unlit", ShieldColor);
            var shatterMat = EnsureTransparentMaterial(ShatterMaterial, "Universal Render Pipeline/Particles/Unlit", ShatterColor);
            var witchPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WitchPrefab);
            if (witchPrefab == null)
            {
                throw new System.InvalidOperationException($"Witch prefab missing at {WitchPrefab}");
            }

            using (var boss = new BossSceneScope())
            {
                var player = PlayerHitReceiverBuilder.FindPlayer(boss.Scene);
                if (player == null)
                {
                    throw new System.InvalidOperationException("Boss Scene has no PlayerMovement object");
                }
                var moneyManager = PlayerHitReceiverBuilder.FindInScene<MoneyManager>(boss.Scene);
                if (moneyManager == null)
                {
                    throw new System.InvalidOperationException("Boss Scene has no MoneyManager");
                }
                var reputationManager = PlayerHitReceiverBuilder.FindInScene<ReputationManager>(boss.Scene);
                var door = FindByName(boss.Scene, DoorName);
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
                var burst = EnsureShatter(witch, shield.transform, shatterMat, ref changed);
                var hud = EnsureHud(boss.Scene, ref changed, out var moneyText, out var objectiveText);

                var so = new SerializedObject(director);
                changed |= RigPrefabBuilder.SetReference(so, "config", config);
                changed |= RigPrefabBuilder.SetReference(so, "moneyManager", moneyManager);
                changed |= RigPrefabBuilder.SetReference(so, "player", player);
                changed |= so.ApplyModifiedPropertiesWithoutUndo();

                so = new SerializedObject(cheats);
                changed |= RigPrefabBuilder.SetReference(so, "config", config);
                changed |= RigPrefabBuilder.SetReference(so, "director", director);
                changed |= RigPrefabBuilder.SetReference(so, "moneyManager", moneyManager);
                changed |= RigPrefabBuilder.SetReference(so, "reputationManager", reputationManager);
                changed |= so.ApplyModifiedPropertiesWithoutUndo();

                so = new SerializedObject(witch);
                changed |= RigPrefabBuilder.SetReference(so, "config", config);
                changed |= RigPrefabBuilder.SetReference(so, "director", director);
                changed |= RigPrefabBuilder.SetReference(so, "shield", shield);
                changed |= RigPrefabBuilder.SetReference(so, "shatterBurst", burst);
                changed |= so.ApplyModifiedPropertiesWithoutUndo();

                so = new SerializedObject(hud);
                changed |= RigPrefabBuilder.SetReference(so, "director", director);
                changed |= RigPrefabBuilder.SetReference(so, "moneyManager", moneyManager);
                changed |= RigPrefabBuilder.SetReference(so, "moneyText", moneyText);
                changed |= RigPrefabBuilder.SetReference(so, "objectiveText", objectiveText);
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

            var director = PlayerHitReceiverBuilder.FindInScene<BossDirector>(scene);
            if (director == null)
            {
                errors.Add($"Boss Scene has no {DirectorName} with a BossDirector component");
            }
            else
            {
                RequireFields(director, new[] { "config", "moneyManager", "player" }, errors);
                var cheats = director.GetComponent<DebugCheats>();
                if (cheats == null)
                {
                    errors.Add("Boss Scene BossDirector has no DebugCheats");
                }
                else
                {
                    RequireFields(cheats, new[] { "config", "director", "moneyManager" }, errors);
                }
            }

            var witch = PlayerHitReceiverBuilder.FindInScene<WitchBoss>(scene);
            if (witch == null)
            {
                errors.Add("Boss Scene has no WitchBoss");
            }
            else
            {
                RequireFields(witch, new[] { "config", "director", "shield", "shatterBurst" }, errors);
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
            }

            var hud = PlayerHitReceiverBuilder.FindInScene<BossHud>(scene);
            if (hud == null)
            {
                errors.Add($"Boss Scene has no {HudName} with a BossHud component");
            }
            else
            {
                RequireFields(hud, new[] { "director", "moneyManager", "moneyText", "objectiveText" }, errors);
                foreach (var label in hud.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (label.font == null)
                    {
                        errors.Add($"Boss Scene HUD text {label.name} has no font");
                    }
                }
            }
        }

        private static void RequireFields(Component component, string[] fields, List<string> errors)
        {
            var so = new SerializedObject(component);
            foreach (var field in fields)
            {
                var property = so.FindProperty(field);
                if (property == null || property.objectReferenceValue == null)
                {
                    errors.Add($"Boss Scene {component.GetType().Name}: {field} reference is missing");
                }
            }
        }

        private static Transform FindByName(UnityEngine.SceneManagement.Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == name)
                    {
                        return t;
                    }
                }
            }
            return null;
        }

        private static T EnsureRootComponent<T>(UnityEngine.SceneManagement.Scene scene, string name, ref bool changed) where T : Component
        {
            var existing = PlayerHitReceiverBuilder.FindInScene<T>(scene);
            if (existing != null)
            {
                return existing;
            }
            var go = new GameObject(name);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
            changed = true;
            return go.AddComponent<T>();
        }

        // A new Witch.prefab instance, standing on the floor a few metres in from the door, facing the player.
        // WitchBoss lifts it to hover height and applies the height scale at runtime.
        private static WitchBoss EnsureWitch(UnityEngine.SceneManagement.Scene scene, GameObject prefab, Transform door, PlayerMovement player, ref bool changed)
        {
            var existing = PlayerHitReceiverBuilder.FindInScene<WitchBoss>(scene);
            if (existing != null)
            {
                return existing;
            }
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.name = WitchName;

            var controller = player.GetComponent<CharacterController>();
            float floorY = controller != null
                ? player.transform.position.y + controller.center.y - controller.height * 0.5f
                : player.transform.position.y - 1f;
            var toPlayer = player.transform.position - door.position;
            toPlayer.y = 0f;
            var inward = toPlayer.sqrMagnitude > 1e-4f ? toPlayer.normalized : Vector3.forward;
            var position = door.position + inward * WitchDistanceFromDoor;
            position.y = floorY;
            instance.transform.SetPositionAndRotation(position, Quaternion.LookRotation(inward));

            changed = true;
            return instance.AddComponent<WitchBoss>();
        }

        // Purple bubble sized to the Witch's capsule.
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

            var capsule = witch.GetComponent<CapsuleCollider>();
            var scale = witch.transform.lossyScale;
            float worldHeight = capsule != null ? capsule.height * scale.y : 2.6f;
            sphere.transform.position = capsule != null
                ? witch.transform.TransformPoint(capsule.center)
                : witch.transform.position + Vector3.up * worldHeight * 0.5f;
            float diameter = worldHeight * 1.15f;
            sphere.transform.localScale = new Vector3(diameter / scale.x, diameter / scale.y, diameter / scale.z);

            var renderer = sphere.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            changed = true;
            return sphere;
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
            go.transform.position = shield.position;
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
            shape.radius = shield.lossyScale.y * 0.5f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            changed = true;
            return ps;
        }

        private static BossHud EnsureHud(UnityEngine.SceneManagement.Scene scene, ref bool changed, out TMP_Text moneyText, out TMP_Text objectiveText)
        {
            var hud = PlayerHitReceiverBuilder.FindInScene<BossHud>(scene);
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
            var existing = parent.Find(name);
            if (existing != null && existing.GetComponent<TMP_Text>() != null)
            {
                return existing.GetComponent<TMP_Text>();
            }
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.one;
            rect.anchorMax = Vector2.one;
            rect.pivot = Vector2.one;
            rect.anchoredPosition = offset;
            rect.sizeDelta = new Vector2(900f, size * 1.4f);
            var label = go.AddComponent<TextMeshProUGUI>();
            label.font = TMP_Settings.defaultFontAsset;
            label.fontSize = size;
            label.alignment = TextAlignmentOptions.TopRight;
            label.color = Color.white;
            label.raycastTarget = false;
            label.text = "";
            changed = true;
            return label;
        }

        private static Material EnsureTransparentMaterial(string path, string shaderName, Color color)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                return existing;
            }
            var shader = Shader.Find(shaderName);
            if (shader == null)
            {
                throw new System.InvalidOperationException($"Shader '{shaderName}' not found for {path}");
            }
            var material = new Material(shader);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
            material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, path);
            AssetDatabase.SaveAssetIfDirty(material);
            return material;
        }
    }
}
