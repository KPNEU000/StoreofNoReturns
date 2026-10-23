using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BossFight.Editor
{
    // Adds the sword and flashlight to the Boss Scene player camera. The old TrainingDummy sat in the camera's
    // face on the front desk; it is removed (the tutorial costume replaces it).
    public static class PlayerToolsBuilder
    {
        public const string SwordName = "Sword";
        public const string SwordPivotName = "SwordPivot";
        public const string FlashlightName = "Flashlight";
        public const string BeamName = "FlashlightBeam";
        public const string DummyName = "TrainingDummy";
        public const string DummyPrefab = BossFightPaths.PrefabsFolder + "/TrainingDummy.prefab";
        private const string FlashlightModel = "Assets/Models/flashlight.fbx";

        public static void Build()
        {
            var config = ConfigBuilder.Load();

            using (var boss = new BossSceneScope())
            {
                var player = BuilderUtil.FindPlayer(boss.Scene);
                if (player == null)
                {
                    throw new System.InvalidOperationException("Boss Scene has no PlayerMovement object for the sword/flashlight");
                }
                var camera = new SerializedObject(player).FindProperty("cameraTrans").objectReferenceValue as Transform;
                if (camera == null)
                {
                    throw new System.InvalidOperationException("Boss Scene PlayerMovement has no cameraTrans");
                }

                bool changed = false;
                BuildSword(camera, player, config, ref changed);
                BuildFlashlight(camera, config, ref changed);
                RemoveDummyInstances(boss.Scene, ref changed);

                if (changed)
                {
                    EditorSceneManager.MarkSceneDirty(boss.Scene);
                }
                boss.SaveIfDirty();
            }
            // After the instance is gone: deleting the prefab first leaves a missing-prefab placeholder in the scene.
            RemoveDummyPrefab();
        }

        // Called by BossRigValidator with the Boss Scene open; appends one message per missing object/reference.
        public static void Validate(UnityEngine.SceneManagement.Scene scene, System.Collections.Generic.List<string> errors)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(DummyPrefab) != null)
            {
                errors.Add($"TrainingDummy prefab should be removed from {DummyPrefab}");
            }
            foreach (var root in scene.GetRootGameObjects())
            {
                if (IsDummy(root))
                {
                    errors.Add($"Boss Scene still contains a {DummyName} ({root.name})");
                }
            }

            var player = BuilderUtil.FindPlayer(scene);
            var sword = player != null ? player.GetComponentInChildren<PlayerSword>(true) : null;
            var flashlight = player != null ? player.GetComponentInChildren<PlayerFlashlight>(true) : null;
            if (sword == null)
            {
                errors.Add("Boss Scene player camera has no PlayerSword");
            }
            else
            {
                BuilderUtil.RequireFields(sword, new[] { "config", "movement", "pivot" }, errors);
                var pivot = sword.transform.Find(SwordPivotName);
                if (pivot == null || pivot.Find(SwordModelName) == null) errors.Add($"Boss Scene sword has no {SwordModelName}");
                if (pivot != null && pivot.Find("Blade") != null) errors.Add("Boss Scene sword still has the placeholder box Blade");
            }
            if (flashlight == null)
            {
                errors.Add("Boss Scene player camera has no PlayerFlashlight");
            }
            else
            {
                BuilderUtil.RequireFields(flashlight, new[] { "config", "beam", "visual" }, errors);
            }
        }

        private static void RemoveDummyPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(DummyPrefab) != null)
            {
                AssetDatabase.DeleteAsset(DummyPrefab);
            }
        }

        // Also matches a "TrainingDummy (Missing Prefab ...)" placeholder.
        private static bool IsDummy(GameObject root)
        {
            return root.name.StartsWith(DummyName) || root.GetComponentInChildren<TrainingDummy>(true) != null;
        }

        private static void RemoveDummyInstances(UnityEngine.SceneManagement.Scene scene, ref bool changed)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (IsDummy(root))
                {
                    Object.DestroyImmediate(root);
                    changed = true;
                }
            }
        }

        // Round-2 playtest: the 0.9 m blade read as a dagger.
        internal const float SwordBladeLength = 2f; // playtest: trying a long blade to see how it reads

        // 2026-10-23: the team's sword model replaces the box blade. It is Y-up with its tip at +Y and the grip
        // around its origin; the slanted side of the tip (model +Z) is turned to local -X, the leading side of the
        // right-to-left slash (SwordPose). Flip SwordModelYaw to 90 if the edge reads as backwards.
        internal const string SwordModelPath = BossFightPaths.ModelsFolder + "/Sword.fbx";
        internal const string SwordModelName = "SwordModel";
        internal const float SwordModelYaw = -90f;
        internal const float SwordModelTipHeight = 1.972f; // model units, measured from the FBX
        private static readonly (string part, string material, Color color)[] SwordMaterials =
        {
            ("Cube", "SwordHandle", new Color(0.367f, 0.212f, 0.040f)),
            ("Cube.001", "SwordBlade", new Color(0.728f, 0.728f, 0.728f)),
            ("Plane", "SwordGuard", new Color(0.120f, 0.120f, 0.120f)),
        };

        // Existing swords are kept, so the length is enforced on the model and the trail tip every build.
        private static void EnsureBladeLength(Transform pivot, float length, ref bool changed)
        {
            var model = pivot.Find(SwordModelName);
            if (model != null)
            {
                var scale = Vector3.one * (length / SwordModelTipHeight);
                var rotation = Quaternion.Euler(0f, SwordModelYaw, 0f);
                if ((model.localScale - scale).sqrMagnitude > 1e-8f || model.localPosition.sqrMagnitude > 1e-8f
                    || Quaternion.Angle(model.localRotation, rotation) > 0.01f)
                {
                    model.localScale = scale;
                    model.localPosition = Vector3.zero;
                    model.localRotation = rotation;
                    changed = true;
                }
            }
            var tip = pivot.Find("BladeTip");
            if (tip != null && (tip.localPosition - new Vector3(0f, length, 0f)).sqrMagnitude > 1e-8f)
            {
                tip.localPosition = new Vector3(0f, length, 0f);
                changed = true;
            }
        }

        internal static void BuildSword(Transform camera, PlayerMovement player, BossFightConfig config, ref bool changed)
        {
            var sword = EnsureChild(camera, SwordName, ref changed);
            var pivot = EnsureChild(sword, SwordPivotName, ref changed);
            if (pivot.childCount == 0)
            {
                pivot.localPosition = new Vector3(0.35f, -0.4f, 0.6f);
                var tip = new GameObject("BladeTip");
                tip.transform.SetParent(pivot, false);
                tip.transform.localPosition = new Vector3(0f, 0.9f, 0f);
                var trail = tip.AddComponent<TrailRenderer>();
                trail.time = 0.2f;
                trail.startWidth = 0.05f;
                trail.endWidth = 0f;
                trail.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Line.mat");
                changed = true;
            }
            // The placeholder box blade made before the model arrived.
            var box = pivot.Find("Blade");
            if (box != null)
            {
                Object.DestroyImmediate(box.gameObject);
                changed = true;
            }
            EnsureSwordModel(pivot, ref changed);
            EnsureBladeLength(pivot, SwordBladeLength, ref changed);

            var component = sword.GetComponent<PlayerSword>();
            if (component == null)
            {
                component = sword.gameObject.AddComponent<PlayerSword>();
                changed = true;
            }
            var so = new SerializedObject(component);
            so.FindProperty("config").objectReferenceValue = config;
            so.FindProperty("movement").objectReferenceValue = player;
            so.FindProperty("pivot").objectReferenceValue = pivot;
            changed |= so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureSwordModel(Transform pivot, ref bool changed)
        {
            var model = pivot.Find(SwordModelName);
            if (model == null)
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(SwordModelPath);
                if (asset == null) throw new System.InvalidOperationException($"Sword model missing: {SwordModelPath}");
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, pivot);
                instance.name = SwordModelName;
                model = instance.transform;
                changed = true;
            }
            foreach (var (part, material, color) in SwordMaterials)
            {
                var child = model.Find(part);
                var renderer = child != null ? child.GetComponent<MeshRenderer>() : null;
                if (renderer == null) throw new System.InvalidOperationException($"Sword model has no '{part}' mesh");
                var mat = BuilderUtil.EnsureLitMaterial($"{BossFightPaths.SettingsFolder}/{material}.mat", color);
                if (renderer.sharedMaterial != mat)
                {
                    renderer.sharedMaterials = new[] { mat };
                    changed = true;
                }
                if (renderer.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off)
                {
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    changed = true;
                }
            }
            // PlayerSword's overlap query hits every layer, so the held model must not carry a collider.
            foreach (var collider in model.GetComponentsInChildren<Collider>(true))
            {
                Object.DestroyImmediate(collider);
                changed = true;
            }
        }

        private static void BuildFlashlight(Transform camera, BossFightConfig config, ref bool changed)
        {
            var root = EnsureChild(camera, FlashlightName, ref changed);
            var visualRoot = EnsureChild(root, "FlashlightVisual", ref changed);
            if (visualRoot.childCount == 0)
            {
                visualRoot.localPosition = new Vector3(-0.32f, -0.28f, 0.5f);
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(FlashlightModel);
                GameObject mesh = null;
                if (model != null && model.GetComponentInChildren<MeshFilter>() != null)
                {
                    mesh = (GameObject)PrefabUtility.InstantiatePrefab(model, visualRoot);
                    var bounds = new Bounds(mesh.transform.position, Vector3.zero);
                    foreach (var r in mesh.GetComponentsInChildren<Renderer>())
                    {
                        bounds.Encapsulate(r.bounds);
                    }
                    float longest = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
                    if (longest > 1e-4f)
                    {
                        mesh.transform.localScale = Vector3.one * (0.3f / longest);
                    }
                }
                else
                {
                    mesh = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    mesh.transform.SetParent(visualRoot, false);
                    mesh.transform.localScale = new Vector3(0.06f, 0.12f, 0.06f);
                    mesh.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                }
                foreach (var col in mesh.GetComponentsInChildren<Collider>())
                {
                    Object.DestroyImmediate(col);
                }
                changed = true;
            }

            var beamTransform = EnsureChild(root, BeamName, ref changed);
            var beam = beamTransform.GetComponent<Light>();
            if (beam == null)
            {
                beamTransform.localPosition = new Vector3(-0.32f, -0.2f, 0.6f);
                beam = beamTransform.gameObject.AddComponent<Light>();
                beam.type = LightType.Spot;
                changed = true;
            }

            var component = root.GetComponent<PlayerFlashlight>();
            if (component == null)
            {
                component = root.gameObject.AddComponent<PlayerFlashlight>();
                changed = true;
            }
            var so = new SerializedObject(component);
            so.FindProperty("config").objectReferenceValue = config;
            so.FindProperty("beam").objectReferenceValue = beam;
            so.FindProperty("visual").objectReferenceValue = visualRoot;
            changed |= so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Transform EnsureChild(Transform parent, string name, ref bool changed)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                return existing;
            }
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            changed = true;
            return go.transform;
        }
    }
}
