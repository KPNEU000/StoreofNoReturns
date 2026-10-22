using System.Collections.Generic;
using TMPro;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace BossFight.Editor
{
    // The OpeningSequence object on the Boss Scene plus its text box, screen-crack frame and invisible door barrier.
    // Scene references are found by exact name or tag and a missing monitor/door fails loudly.
    public static class OpeningBuilder
    {
        public const string ObjectName = "OpeningSequence";
        public const string TextCanvasName = "BossTextCanvas";
        public const string TextPanelName = "BossTextPanel";
        public const string TextLabelName = "BossTextLabel";
        public const string CrackFrameName = "ScreenCrackFrame";
        public const string CrackImageName = "CrackImage";
        public const string BarrierName = "DoorBarrier";
        public const string MonitorTag = "Computer";
        public const string MagicMaterial = BossFightPaths.SettingsFolder + "/OpeningMagic.mat";
        public const string WallCrackMaterial = BossFightPaths.SettingsFolder + "/OpeningWallCrack.mat";
        private const float BarrierPadding = 0.5f;

        public static void Build()
        {
            var config = ConfigBuilder.Load();
            EditorFolders.Ensure(BossFightPaths.SettingsFolder);
            var magic = BuilderUtil.EnsureTransparentMaterial(MagicMaterial, "Universal Render Pipeline/Unlit", new Color(0.7f, 0.3f, 1f, 0.9f));
            var wallCrack = BuilderUtil.EnsureTransparentMaterial(WallCrackMaterial, "Universal Render Pipeline/Unlit", Color.white);
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

                bool changed = false;
                var textBox = EnsureTextBox(scene, ref changed);
                var crack = EnsureCrackFrame(hud, ref changed);
                var barrier = EnsureBarrier(scene, door, ref changed);

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
                changed |= BuilderUtil.SetReference(so, "door", door);
                changed |= BuilderUtil.SetReference(so, "doorBarrier", barrier);
                changed |= BuilderUtil.SetReference(so, "spawner", spawner);
                changed |= BuilderUtil.SetReference(so, "hud", hud);
                changed |= BuilderUtil.SetReference(so, "crackOverlay", crack);
                changed |= BuilderUtil.SetReference(so, "swordObject", sword.gameObject);
                changed |= BuilderUtil.SetReference(so, "flashlight", flashlight);
                changed |= BuilderUtil.SetReference(so, "magicMaterial", magic);
                changed |= BuilderUtil.SetReference(so, "shardMaterial", shard);
                changed |= BuilderUtil.SetReference(so, "wallCrackMaterial", wallCrack);
                changed |= so.ApplyModifiedPropertiesWithoutUndo();

                if (changed) EditorSceneManager.MarkSceneDirty(scene);
                boss.SaveIfDirty();
            }
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
                text.font = TMP_Settings.defaultFontAsset;
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

        // A top-right frame under the HUD canvas; the crack image is its first child so moved HUD labels draw on top.
        private static ScreenCrackOverlay EnsureCrackFrame(BossHud hud, ref bool changed)
        {
            var existing = hud.GetComponentInChildren<ScreenCrackOverlay>(true);
            ScreenCrackOverlay overlay = existing;
            if (overlay == null)
            {
                var go = new GameObject(CrackFrameName, typeof(RectTransform));
                go.transform.SetParent(hud.transform, false);
                go.transform.SetAsFirstSibling();
                var rect = (RectTransform)go.transform;
                rect.anchorMin = Vector2.one;
                rect.anchorMax = Vector2.one;
                rect.pivot = Vector2.one;
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(820f, 420f);
                overlay = go.AddComponent<ScreenCrackOverlay>();
                changed = true;
            }
            var imageTransform = overlay.transform.Find(CrackImageName);
            RawImage image = imageTransform != null ? imageTransform.GetComponent<RawImage>() : null;
            if (image == null)
            {
                var go = new GameObject(CrackImageName, typeof(RectTransform), typeof(RawImage));
                go.transform.SetParent(overlay.transform, false);
                go.transform.SetAsFirstSibling();
                var rect = (RectTransform)go.transform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                image = go.GetComponent<RawImage>();
                image.raycastTarget = false;
                changed = true;
            }
            // Untextured until ScreenCrackOverlay.Show, so saved active it draws as a white box in edit mode.
            if (image.gameObject.activeSelf)
            {
                image.gameObject.SetActive(false);
                changed = true;
            }
            var so = new SerializedObject(overlay);
            changed |= BuilderUtil.SetReference(so, "crackImage", image);
            changed |= so.ApplyModifiedPropertiesWithoutUndo();
            return overlay;
        }

        // Invisible blocker across the doorway, off until the blast. NavMesh builds ignore it so monsters can still path in.
        private static Collider EnsureBarrier(UnityEngine.SceneManagement.Scene scene, Transform door, ref bool changed)
        {
            var existing = BuilderUtil.FindByName(scene, BarrierName);
            if (existing != null && existing.GetComponent<BoxCollider>() != null) return existing.GetComponent<BoxCollider>();

            var bounds = DoorBounds(door);
            bounds.Expand(BarrierPadding * 2f);
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
        private static Bounds DoorBounds(Transform door)
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

        // Called by BossRigValidator with the Boss Scene open.
        public static void Validate(UnityEngine.SceneManagement.Scene scene, List<string> errors)
        {
            foreach (var path in new[] { MagicMaterial, WallCrackMaterial })
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
                    "config", "director", "player", "witch", "monitor", "door", "doorBarrier", "spawner", "hud", "crackOverlay",
                    "swordObject", "flashlight", "magicMaterial", "shardMaterial", "wallCrackMaterial",
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

            var overlay = BuilderUtil.FindInScene<ScreenCrackOverlay>(scene);
            if (overlay == null)
            {
                errors.Add($"Boss Scene has no {CrackFrameName} with a ScreenCrackOverlay component");
            }
            else
            {
                var image = new SerializedObject(overlay).FindProperty("crackImage").objectReferenceValue as RawImage;
                if (image == null) errors.Add("Boss Scene ScreenCrackOverlay: crackImage reference is missing");
                else if (image.gameObject.activeSelf) errors.Add("Boss Scene ScreenCrackOverlay: crackImage must be saved inactive (it is a white box until the crack appears)");
            }
        }
    }
}
