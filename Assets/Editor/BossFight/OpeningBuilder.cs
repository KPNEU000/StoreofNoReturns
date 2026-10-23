using BossFight.Rules;
using System.Collections.Generic;
using TMPro;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace BossFight.Editor
{
    // The OpeningSequence object on the Boss Scene plus its text box, glass HUD shard, cracked monitor glass and
    // invisible door barrier.
    // Scene references are found by exact name or tag and a missing monitor/door fails loudly.
    public static class OpeningBuilder
    {
        public const string ObjectName = "OpeningSequence";
        public const string TextCanvasName = "BossTextCanvas";
        public const string TextPanelName = "BossTextPanel";
        public const string TextLabelName = "BossTextLabel";
        public const string CrackFrameName = "ScreenCrackFrame";
        public const string CrackImageName = "CrackImage";
        public const string GlassHudName = "GlassHudShard";
        public const string MonitorGlassName = "MonitorGlass";
        public const string HudPromptName = "BossPromptText";
        public const string GlassShader = "BossFight/GlassShard";
        public const string GlassHudMaterial = BossFightPaths.SettingsFolder + "/GlassHud.mat";
        public const string MonitorGlassMaterial = BossFightPaths.SettingsFolder + "/MonitorGlass.mat";
        // In front of the monitor's world-space canvas, in metres.
        internal const float MonitorGlassLift = 0.03f; // clears the curved CRT surface in front of the canvas
        // Text sits in the middle of the shard, clear of its jagged edge (fractions of the frame).
        internal static readonly Vector4 MoneyArea = new Vector4(0.16f, 0.6f, 0.84f, 0.82f);
        internal static readonly Vector4 ObjectiveArea = new Vector4(0.16f, 0.42f, 0.84f, 0.6f);
        internal static readonly Vector4 PromptArea = new Vector4(0.16f, 0.18f, 0.84f, 0.42f);
        public const string BarrierName = "DoorBarrier";
        public const string VaultTargetName = "VaultTarget";
        public const string DeskName = "Front Desk";
        internal const float VaultClearance = 1.2f;
        public const string MonitorTag = "Computer";
        public const string MagicMaterial = BossFightPaths.SettingsFolder + "/OpeningMagic.mat";
        public const string WallCrackMaterial = BossFightPaths.SettingsFolder + "/OpeningWallCrack.mat";
        public const string ScreenCrackMaterial = BossFightPaths.SettingsFolder + "/MonitorScreenCrack.mat";
        public const string ScreenCrackShader = "BossFight/ScreenCrack";
        private const float BarrierPadding = 0.5f;

        public static void Build()
        {
            var config = ConfigBuilder.Load();
            EditorFolders.Ensure(BossFightPaths.SettingsFolder);
            var magic = BuilderUtil.EnsureTransparentMaterial(MagicMaterial, "Universal Render Pipeline/Unlit", new Color(0.7f, 0.3f, 1f, 0.9f));
            var wallCrack = BuilderUtil.EnsureTransparentMaterial(WallCrackMaterial, "Universal Render Pipeline/Unlit", Color.white);
            var screenCrack = EnsureScreenCrackMaterial();
            var glassHud = EnsureGlassMaterial(GlassHudMaterial, CompareFunction.Always, (int)RenderQueue.Transparent + 100, 1f, 0f);
            var monitorGlassMaterial = EnsureGlassMaterial(MonitorGlassMaterial, CompareFunction.LessEqual, (int)RenderQueue.Transparent + 1, 0.65f, 1f);
            var shard = AssetDatabase.LoadAssetAtPath<Material>(BossDirectorBuilder.ShatterMaterial);
            if (shard == null) throw new System.InvalidOperationException($"Shatter material missing at {BossDirectorBuilder.ShatterMaterial}");

            using (var boss = new BossSceneScope())
            {
                var scene = boss.Scene;
                var player = BuilderUtil.FindPlayer(scene);
                var director = BuilderUtil.FindInScene<BossDirector>(scene);
                var witch = BuilderUtil.FindInScene<WitchBoss>(scene);
                var spawner = BuilderUtil.FindInScene<ClothingSpawner>(scene);
                var hud = BuilderUtil.FindInScene<BossHud>(scene);
                if (player == null || director == null || witch == null || spawner == null || hud == null)
                {
                    throw new System.InvalidOperationException("Opening builder needs the player, BossDirector, Witch, ClothingSpawner and BossHud in Boss Scene");
                }
                var door = BuilderUtil.FindByName(scene, BossDirectorBuilder.DoorName);
                if (door == null) throw new System.InvalidOperationException($"Boss Scene has no '{BossDirectorBuilder.DoorName}' object to blast");
                var monitor = FindMonitor(scene);
                if (monitor == null) throw new System.InvalidOperationException($"Boss Scene has no object tagged '{MonitorTag}' with a renderer for the monitor");
                var camera = new SerializedObject(player).FindProperty("cameraTrans").objectReferenceValue as Transform;
                var sword = camera != null ? camera.Find(PlayerToolsBuilder.SwordName) : null;
                var flashlight = player.GetComponentInChildren<PlayerFlashlight>(true);
                if (sword == null || flashlight == null) throw new System.InvalidOperationException("Opening builder needs the sword and flashlight on the player camera");
                var viewCamera = camera.GetComponent<Camera>();
                if (viewCamera == null) throw new System.InvalidOperationException("Boss Scene player cameraTrans has no Camera for the glass HUD");

                bool changed = false;
                var textBox = EnsureTextBox(scene, ref changed);
                var crack = EnsureCrackFrame(hud, viewCamera, config, glassHud, ref changed);
                var monitorGlass = EnsureMonitorGlass(monitor, viewCamera, config, monitorGlassMaterial, ref changed);
                var barrier = EnsureBarrier(scene, door, ref changed);
                var vaultTarget = EnsureVaultTarget(scene, player, ref changed);

                var sequence = BuilderUtil.FindInScene<OpeningSequence>(scene);
                if (sequence == null)
                {
                    var go = new GameObject(ObjectName);
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
                    sequence = go.AddComponent<OpeningSequence>();
                    changed = true;
                }
                var so = new SerializedObject(sequence);
                changed |= BuilderUtil.SetReference(so, "config", config);
                changed |= BuilderUtil.SetReference(so, "director", director);
                changed |= BuilderUtil.SetReference(so, "player", player);
                changed |= BuilderUtil.SetReference(so, "witch", witch.transform);
                changed |= BuilderUtil.SetReference(so, "monitor", monitor);
                changed |= BuilderUtil.SetReference(so, "monitorGlass", monitorGlass);
                changed |= BuilderUtil.SetReference(so, "door", door);
                changed |= BuilderUtil.SetReference(so, "doorBarrier", barrier);
                changed |= BuilderUtil.SetReference(so, "vaultTarget", vaultTarget);
                changed |= BuilderUtil.SetReference(so, "spawner", spawner);
                changed |= BuilderUtil.SetReference(so, "hud", hud);
                changed |= BuilderUtil.SetReference(so, "crackOverlay", crack);
                changed |= BuilderUtil.SetReference(so, "swordObject", sword.gameObject);
                changed |= BuilderUtil.SetReference(so, "flashlight", flashlight);
                changed |= BuilderUtil.SetReference(so, "magicMaterial", magic);
                changed |= BuilderUtil.SetReference(so, "shardMaterial", shard);
                changed |= BuilderUtil.SetReference(so, "wallCrackMaterial", wallCrack);
                changed |= BuilderUtil.SetReference(so, "screenCrackMaterial", screenCrack);
                changed |= so.ApplyModifiedPropertiesWithoutUndo();

                if (changed) EditorSceneManager.MarkSceneDirty(scene);
                boss.SaveIfDirty();
            }
        }

        // A marker on the store floor just past the far edge of the front desk, straight ahead of the player.
        private static Transform EnsureVaultTarget(UnityEngine.SceneManagement.Scene scene, PlayerMovement player, ref bool changed)
        {
            var desk = BuilderUtil.FindByName(scene, DeskName);
            if (desk == null) throw new System.InvalidOperationException($"Boss Scene has no '{DeskName}' object to vault over");
            var renderers = desk.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new System.InvalidOperationException($"'{DeskName}' has no renderer to measure");
            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);

            var position = TutorialRules.VaultTarget(player.transform.position, player.transform.forward, bounds, VaultClearance);
            var target = BuilderUtil.FindByName(scene, VaultTargetName);
            if (target == null)
            {
                var go = new GameObject(VaultTargetName);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
                target = go.transform;
                changed = true;
            }
            if ((target.position - position).sqrMagnitude > 1e-6f)
            {
                target.position = position;
                changed = true;
            }
            return target;
        }

        // The first object tagged Computer that actually has a mesh (the tagged root of the computer model has none).
        private static Transform FindMonitor(UnityEngine.SceneManagement.Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.CompareTag(MonitorTag) && t.GetComponentInChildren<Renderer>(true) != null) return t;
                }
            }
            return null;
        }

        private static BossTextBox EnsureTextBox(UnityEngine.SceneManagement.Scene scene, ref bool changed)
        {
            var textBox = BuilderUtil.FindInScene<BossTextBox>(scene);
            if (textBox == null)
            {
                var canvasGo = new GameObject(TextCanvasName, typeof(Canvas), typeof(CanvasScaler));
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(canvasGo, scene);
                var canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 95;
                var scaler = canvasGo.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                textBox = canvasGo.AddComponent<BossTextBox>();
                changed = true;
            }
            var root = textBox.transform;
            var panel = root.Find(TextPanelName);
            if (panel == null)
            {
                var go = new GameObject(TextPanelName, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(root, false);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = new Vector2(0.5f, 0f);
                rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = new Vector2(0f, 60f);
                rect.sizeDelta = new Vector2(1300f, 150f);
                var image = go.GetComponent<Image>();
                image.color = new Color(0.08f, 0.03f, 0.14f, 0.8f);
                image.raycastTarget = false;
                panel = go.transform;
                changed = true;
            }
            var labelTransform = panel.Find(TextLabelName);
            TMP_Text label = labelTransform != null ? labelTransform.GetComponent<TMP_Text>() : null;
            if (label == null)
            {
                var go = new GameObject(TextLabelName, typeof(RectTransform));
                go.transform.SetParent(panel, false);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = new Vector2(30f, 15f);
                rect.offsetMax = new Vector2(-30f, -15f);
                var text = go.AddComponent<TextMeshProUGUI>();
                text.font = FontBuilder.Load();
                text.fontSize = 44f;
                text.alignment = TextAlignmentOptions.Center;
                text.color = Color.white;
                text.raycastTarget = false;
                text.text = "";
                label = text;
                changed = true;
            }
            var so = new SerializedObject(textBox);
            changed |= BuilderUtil.SetReference(so, "panel", panel.gameObject);
            changed |= BuilderUtil.SetReference(so, "label", label);
            changed |= so.ApplyModifiedPropertiesWithoutUndo();
            return textBox;
        }

        // The broken-monitor material (texture is generated at runtime by OpeningSequence). Created once.
        private static Material EnsureScreenCrackMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(ScreenCrackMaterial);
            if (existing != null) return existing;
            var shader = Shader.Find(ScreenCrackShader);
            if (shader == null) throw new System.InvalidOperationException($"Shader '{ScreenCrackShader}' not found for {ScreenCrackMaterial}");
            var material = new Material(shader);
            AssetDatabase.CreateAsset(material, ScreenCrackMaterial);
            AssetDatabase.SaveAssetIfDirty(material);
            return material;
        }

        // Created once, like every other builder material; tune the look on the asset.
        private static Material EnsureGlassMaterial(string path, CompareFunction zTest, int queue, float opacity, float crackCoverage)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            var shader = Shader.Find(GlassShader);
            if (shader == null) throw new System.InvalidOperationException($"Shader '{GlassShader}' not found for {path}");
            var material = new Material(shader);
            material.SetFloat("_ZTest", (float)zTest);
            material.SetFloat("_Opacity", opacity);
            material.SetFloat("_CrackCoverage", crackCoverage);
            material.renderQueue = queue;
            AssetDatabase.CreateAsset(material, path);
            AssetDatabase.SaveAssetIfDirty(material);
            return material;
        }

        // The top-right frame under the HUD canvas covers the same viewport rect as the camera-parented glass shard;
        // the money, objective and prompt labels live inside it, drawn over the shard by the overlay canvas.
        private static ScreenCrackOverlay EnsureCrackFrame(BossHud hud, Camera viewCamera, BossFightConfig config, Material material, ref bool changed)
        {
            var settings = BossFightConfig.SettingsOf(config);
            var overlay = hud.GetComponentInChildren<ScreenCrackOverlay>(true);
            if (overlay == null)
            {
                var go = new GameObject(CrackFrameName, typeof(RectTransform));
                go.transform.SetParent(hud.transform, false);
                go.transform.SetAsFirstSibling();
                overlay = go.AddComponent<ScreenCrackOverlay>();
                changed = true;
            }
            var viewport = GlassShardRules.ScreenRect(settings.glassHudWidth, settings.glassHudHeight, settings.glassHudMargin);
            changed |= SetArea(overlay.Frame, new Vector4(viewport.xMin, viewport.yMin, viewport.xMax, viewport.yMax));

            // The old white line-drawn crack.
            var oldImage = overlay.transform.Find(CrackImageName);
            if (oldImage != null)
            {
                Object.DestroyImmediate(oldImage.gameObject);
                changed = true;
            }

            var hudSo = new SerializedObject(hud);
            var money = hudSo.FindProperty("moneyText").objectReferenceValue as TMP_Text;
            var objective = hudSo.FindProperty("objectiveText").objectReferenceValue as TMP_Text;
            if (money == null || objective == null) throw new System.InvalidOperationException("Boss Scene BossHud has no money/objective labels to put on the glass");
            var prompt = BuilderUtil.FindLabel(overlay.transform, HudPromptName);
            if (prompt == null)
            {
                var go = new GameObject(HudPromptName, typeof(RectTransform));
                go.transform.SetParent(overlay.transform, false);
                var text = go.AddComponent<TextMeshProUGUI>();
                text.font = FontBuilder.Load();
                text.color = new Color(1f, 0.93f, 0.7f, 1f);
                text.raycastTarget = false;
                text.textWrappingMode = TextWrappingModes.Normal;
                text.text = "";
                prompt = text;
                changed = true;
            }
            changed |= PlaceLabel(money, overlay.transform, MoneyArea, 64f);
            changed |= PlaceLabel(objective, overlay.transform, ObjectiveArea, 32f);
            changed |= PlaceLabel(prompt, overlay.transform, PromptArea, 30f);
            hudSo.Update();
            changed |= BuilderUtil.SetReference(hudSo, "promptText", prompt);
            changed |= hudSo.ApplyModifiedPropertiesWithoutUndo();

            var glass = EnsureGlass(viewCamera.transform, GlassHudName, config, viewCamera, true, material, ref changed);
            if (glass.transform.localPosition == Vector3.zero)
            {
                // A sensible edit-mode spot; GlassShard.Place fits it to the real camera at runtime.
                glass.transform.localPosition = new Vector3(0.1f, 0.06f, viewCamera.nearClipPlane + 0.02f);
                glass.transform.localScale = new Vector3(0.12f, 0.07f, 1f);
                changed = true;
            }
            var so = new SerializedObject(overlay);
            changed |= BuilderUtil.SetReference(so, "glass", glass);
            changed |= so.ApplyModifiedPropertiesWithoutUndo();
            return overlay;
        }

        // A rectangle of the same cracked glass over the monitor's world-space canvas, off until the shatter.
        private static GlassShard EnsureMonitorGlass(Transform monitor, Camera viewCamera, BossFightConfig config, Material material, ref bool changed)
        {
            var canvas = monitor.GetComponentInChildren<Canvas>(true);
            if (canvas == null || canvas.renderMode != RenderMode.WorldSpace)
            {
                throw new System.InvalidOperationException($"Monitor '{monitor.name}' has no world-space screen Canvas to crack");
            }
            var glass = EnsureGlass(canvas.transform, MonitorGlassName, config, viewCamera, false, material, ref changed);
            var rect = ((RectTransform)canvas.transform).rect;
            float scale = Mathf.Max(Mathf.Abs(canvas.transform.lossyScale.z), 1e-6f);
            // UI faces the viewer along the canvas's -z, so the glass sits on that side.
            var position = new Vector3(rect.center.x, rect.center.y, -MonitorGlassLift / scale);
            var size = new Vector3(rect.width, rect.height, 1f);
            var t = glass.transform;
            if ((t.localPosition - position).sqrMagnitude > 1e-8f || (t.localScale - size).sqrMagnitude > 1e-8f || t.localRotation != Quaternion.identity)
            {
                t.localPosition = position;
                t.localRotation = Quaternion.identity;
                t.localScale = size;
                changed = true;
            }
            return glass;
        }

        private static GlassShard EnsureGlass(Transform parent, string name, BossFightConfig config, Camera viewCamera, bool followCamera, Material material, ref bool changed)
        {
            var existing = parent.Find(name);
            GlassShard glass = existing != null ? existing.GetComponent<GlassShard>() : null;
            if (glass == null)
            {
                if (existing != null) Object.DestroyImmediate(existing.gameObject);
                var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(parent, false);
                glass = go.AddComponent<GlassShard>();
                var meshRenderer = go.GetComponent<MeshRenderer>();
                meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
                meshRenderer.receiveShadows = false;
                changed = true;
            }
            var renderer = glass.GetComponent<MeshRenderer>();
            if (renderer.sharedMaterial != material)
            {
                renderer.sharedMaterial = material;
                changed = true;
            }
            // Shown by the opening; saved off so it never draws in edit mode or before the shatter.
            if (glass.gameObject.activeSelf)
            {
                glass.gameObject.SetActive(false);
                changed = true;
            }
            var so = new SerializedObject(glass);
            changed |= BuilderUtil.SetReference(so, "config", config);
            changed |= BuilderUtil.SetReference(so, "viewCamera", viewCamera);
            var follow = so.FindProperty("followCamera");
            if (follow.boolValue != followCamera)
            {
                follow.boolValue = followCamera;
                changed = true;
            }
            changed |= so.ApplyModifiedPropertiesWithoutUndo();
            return glass;
        }

        // Parents the label under the frame, stretched over `area` (xMin, yMin, xMax, yMax as frame fractions), centred.
        private static bool PlaceLabel(TMP_Text label, Transform frame, Vector4 area, float size)
        {
            bool changed = false;
            if (label.transform.parent != frame)
            {
                label.transform.SetParent(frame, false);
                changed = true;
            }
            changed |= SetArea(label.rectTransform, area);
            if (!Mathf.Approximately(label.fontSize, size))
            {
                label.fontSize = size;
                changed = true;
            }
            if (label.alignment != TextAlignmentOptions.Center)
            {
                label.alignment = TextAlignmentOptions.Center;
                changed = true;
            }
            return changed;
        }

        private static bool SetArea(RectTransform rect, Vector4 area)
        {
            var min = new Vector2(area.x, area.y);
            var max = new Vector2(area.z, area.w);
            var pivot = new Vector2(0.5f, 0.5f);
            if (rect.anchorMin == min && rect.anchorMax == max && rect.offsetMin == Vector2.zero && rect.offsetMax == Vector2.zero && rect.pivot == pivot)
            {
                return false;
            }
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = pivot;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return true;
        }

        // Invisible blocker across the doorway, off until the blast. NavMesh builds ignore it so monsters can still path in.
        private static Collider EnsureBarrier(UnityEngine.SceneManagement.Scene scene, Transform door, ref bool changed)
        {
            var bounds = DoorBounds(door);
            bounds.Expand(BarrierPadding * 2f);
            var existing = BuilderUtil.FindByName(scene, BarrierName);
            if (existing != null && existing.GetComponent<BoxCollider>() != null)
            {
                // Follows the door, so a barrier built around a previously targeted door moves to the real one.
                var existingBox = existing.GetComponent<BoxCollider>();
                if ((existing.position - bounds.center).sqrMagnitude > 1e-6f || (existingBox.size - bounds.size).sqrMagnitude > 1e-6f)
                {
                    existing.position = bounds.center;
                    existingBox.size = bounds.size;
                    changed = true;
                }
                return existingBox;
            }
            var go = new GameObject(BarrierName);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
            go.transform.position = bounds.center;
            var box = go.AddComponent<BoxCollider>();
            box.size = bounds.size;
            box.enabled = false;
            var modifier = go.AddComponent<NavMeshModifier>();
            modifier.ignoreFromBuild = true;
            changed = true;
            return box;
        }

        // World AABB of the door's mesh/box bounds, computed from local data so it works while the door is inactive.
        internal static Bounds DoorBounds(Transform door)
        {
            bool any = false;
            var bounds = new Bounds(door.position, Vector3.zero);
            foreach (var filter in door.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null) continue;
                Encapsulate(ref bounds, ref any, filter.transform, filter.sharedMesh.bounds);
            }
            foreach (var box in door.GetComponentsInChildren<BoxCollider>(true))
            {
                Encapsulate(ref bounds, ref any, box.transform, new Bounds(box.center, box.size));
            }
            if (!any) bounds = new Bounds(door.position, new Vector3(3f, 3f, 1f));
            return bounds;
        }

        private static void Encapsulate(ref Bounds bounds, ref bool any, Transform t, Bounds local)
        {
            for (int i = 0; i < 8; i++)
            {
                var corner = local.center + Vector3.Scale(local.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                var world = t.TransformPoint(corner);
                if (!any)
                {
                    bounds = new Bounds(world, Vector3.zero);
                    any = true;
                }
                else
                {
                    bounds.Encapsulate(world);
                }
            }
        }

        private static void ValidateGlass(UnityEngine.SceneManagement.Scene scene, List<string> errors)
        {
            foreach (var path in new[] { GlassHudMaterial, MonitorGlassMaterial })
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null) errors.Add($"Material missing at {path}");
                else if (material.shader == null || material.shader.name != GlassShader) errors.Add($"{path} does not use the {GlassShader} shader");
            }
            var overlay = BuilderUtil.FindInScene<ScreenCrackOverlay>(scene);
            var hud = BuilderUtil.FindInScene<BossHud>(scene);
            if (overlay == null || hud == null)
            {
                errors.Add($"Boss Scene has no {CrackFrameName} with a ScreenCrackOverlay component under the BossHud");
                return;
            }
            if (overlay.transform.Find(CrackImageName) != null) errors.Add("Boss Scene ScreenCrackOverlay still holds the old white CrackImage");
            var glass = new SerializedObject(overlay).FindProperty("glass").objectReferenceValue as GlassShard;
            var player = BuilderUtil.FindPlayer(scene);
            var camera = player != null ? new SerializedObject(player).FindProperty("cameraTrans").objectReferenceValue as Transform : null;
            CheckGlass(glass, "HUD glass shard", GlassHudMaterial, camera, errors);
            var sequence = BuilderUtil.FindInScene<OpeningSequence>(scene);
            var monitorGlass = sequence != null ? new SerializedObject(sequence).FindProperty("monitorGlass").objectReferenceValue as GlassShard : null;
            CheckGlass(monitorGlass, "monitor glass", MonitorGlassMaterial, null, errors);

            var hudSo = new SerializedObject(hud);
            foreach (var field in new[] { "moneyText", "objectiveText", "promptText" })
            {
                var label = hudSo.FindProperty(field).objectReferenceValue as TMP_Text;
                if (label == null) errors.Add($"Boss Scene BossHud: {field} reference is missing");
                else if (label.transform.parent != overlay.transform) errors.Add($"Boss Scene BossHud: {field} must sit inside the glass frame {CrackFrameName}");
                else if (label.font == null) errors.Add($"Boss Scene HUD label {label.name} has no font");
            }
        }

        private static void CheckGlass(GlassShard glass, string label, string materialPath, Transform parent, List<string> errors)
        {
            if (glass == null)
            {
                errors.Add($"Boss Scene {label} reference is missing");
                return;
            }
            if (glass.gameObject.activeSelf) errors.Add($"Boss Scene {label} must be saved inactive (it appears when the monitor shatters)");
            var renderer = glass.GetComponent<MeshRenderer>();
            if (renderer == null || renderer.sharedMaterial != AssetDatabase.LoadAssetAtPath<Material>(materialPath)) errors.Add($"Boss Scene {label} does not use {materialPath}");
            if (parent != null && glass.transform.parent != parent) errors.Add($"Boss Scene {label} must be a child of the player camera");
            BuilderUtil.RequireFields(glass, $"Boss Scene {label}", new[] { "config", "viewCamera" }, errors);
        }

        // Called by BossRigValidator with the Boss Scene open.
        public static void Validate(UnityEngine.SceneManagement.Scene scene, List<string> errors)
        {
            foreach (var path in new[] { MagicMaterial, WallCrackMaterial, ScreenCrackMaterial })
            {
                if (AssetDatabase.LoadAssetAtPath<Material>(path) == null) errors.Add($"Material missing at {path}");
            }
            var sequence = BuilderUtil.FindInScene<OpeningSequence>(scene);
            if (sequence == null)
            {
                errors.Add($"Boss Scene has no {ObjectName} with an OpeningSequence component");
            }
            else
            {
                var so = new SerializedObject(sequence);
                foreach (var field in new[]
                {
                    "config", "director", "player", "witch", "monitor", "monitorGlass", "door", "doorBarrier", "vaultTarget", "spawner", "hud", "crackOverlay",
                    "swordObject", "flashlight", "magicMaterial", "shardMaterial", "wallCrackMaterial", "screenCrackMaterial",
                })
                {
                    if (so.FindProperty(field).objectReferenceValue == null) errors.Add($"Boss Scene OpeningSequence: {field} reference is missing");
                }
                var barrier = so.FindProperty("doorBarrier").objectReferenceValue as Collider;
                if (barrier != null && (barrier.isTrigger || barrier.enabled)) errors.Add("Boss Scene DoorBarrier must be a solid collider that starts disabled");
            }

            var textBox = BuilderUtil.FindInScene<BossTextBox>(scene);
            if (textBox == null)
            {
                errors.Add($"Boss Scene has no {TextCanvasName} with a BossTextBox component");
            }
            else
            {
                var so = new SerializedObject(textBox);
                foreach (var field in new[] { "panel", "label" })
                {
                    if (so.FindProperty(field).objectReferenceValue == null) errors.Add($"Boss Scene BossTextBox: {field} reference is missing");
                }
                foreach (var label in textBox.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (label.font == null) errors.Add($"Boss Scene text box label {label.name} has no font");
                }
            }

            ValidateGlass(scene, errors);
        }
    }
}
