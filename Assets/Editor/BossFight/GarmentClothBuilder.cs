using System.Collections.Generic;
using BossFight.Rules;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BossFight.Editor
{
    // Gives every possessable rack its own costumes (each costume type hanging under it) as cloth garments. The costume FBXs are static, non-readable,
    // heavily scaled meshes, so each costume type is baked once into a readable mesh (upright as it hangs on the
    // rack, unscaled, centred, clustered to the cloth particle budget) and a garment prefab holding that mesh on a
    // bone-less SkinnedMeshRenderer. Cloth itself is added at spawn (GarmentCloth). Assets are created when missing
    // only: delete them under Garments/ to re-bake.
    public static class GarmentClothBuilder
    {
        public const string MeshFolder = BossFightPaths.SettingsFolder + "/Garments";
        public const string PrefabFolder = BossFightPaths.PrefabsFolder + "/Garments";
        private const string CostumeFolder = "Assets/Prefabs/Costumes/";

        public static void Build()
        {
            var settings = BossFightConfig.SettingsOf(ConfigBuilder.Load());
            EditorFolders.Ensure(MeshFolder);
            EditorFolders.Ensure(PrefabFolder);
            var garments = new Dictionary<GameObject, GameObject>();
            using (var boss = new BossSceneScope())
            {
                bool changed = false;
                foreach (var rack in Racks(boss.Scene))
                {
                    var costumes = new List<GameObject>();
                    var rackGarments = new List<GameObject>();
                    foreach (var (hung, costume) in HungCostumes(rack.transform))
                    {
                        if (!garments.TryGetValue(costume, out var garment))
                        {
                            garment = EnsureGarment(costume, hung, settings);
                            garments[costume] = garment;
                        }
                        costumes.Add(costume);
                        rackGarments.Add(garment);
                    }
                    var so = new SerializedObject(rack);
                    changed |= SetReferences(so, "costumePrefabs", costumes);
                    changed |= SetReferences(so, "garmentPrefabs", rackGarments);
                    changed |= so.ApplyModifiedPropertiesWithoutUndo();
                }
                if (changed) EditorSceneManager.MarkSceneDirty(boss.Scene);
                boss.SaveIfDirty();
            }
            EnsureFallback(garments, settings);
        }

        internal static List<PossessableRack> Racks(UnityEngine.SceneManagement.Scene scene)
        {
            var result = new List<PossessableRack>();
            foreach (var t in ClothingWaveBuilder.FindRackObjects(scene))
            {
                var rack = t.GetComponent<PossessableRack>();
                if (rack != null) result.Add(rack);
            }
            return result;
        }

        // Each distinct costume prefab hanging under the rack (in hierarchy order) with its first hung instance.
        internal static List<(Transform hung, GameObject costume)> HungCostumes(Transform rack)
        {
            var result = new List<(Transform, GameObject)>();
            foreach (var t in rack.GetComponentsInChildren<Transform>(true))
            {
                if (!PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)) continue;
                var costume = CostumeAssetOf(t.gameObject);
                if (costume != null && !result.Exists(r => r.Item2 == costume)) result.Add((t, costume));
            }
            return result;
        }

        private static bool SetReferences(SerializedObject so, string field, List<GameObject> values)
        {
            var prop = so.FindProperty(field);
            if (prop == null || !prop.isArray) throw new System.InvalidOperationException($"{so.targetObject.GetType().Name} has no serialized array '{field}'");
            bool changed = false;
            if (prop.arraySize != values.Count)
            {
                prop.arraySize = values.Count;
                changed = true;
            }
            for (int i = 0; i < values.Count; i++)
            {
                var element = prop.GetArrayElementAtIndex(i);
                if (element.objectReferenceValue == values[i]) continue;
                element.objectReferenceValue = values[i];
                changed = true;
            }
            return changed;
        }

        // Walks the nested-prefab chain (scene -> rack prefab -> costume prefab) to the nearest costume prefab root.
        private static GameObject CostumeAssetOf(GameObject instance)
        {
            Object source = instance;
            for (int i = 0; i < 8; i++)
            {
                source = PrefabUtility.GetCorrespondingObjectFromSource(source);
                if (!(source is GameObject go)) return null;
                var path = AssetDatabase.GetAssetPath(go);
                if (path.StartsWith(CostumeFolder) && go.transform.parent == null && path.EndsWith(".prefab"))
                {
                    return AssetDatabase.LoadAssetAtPath<GameObject>(path);
                }
            }
            return null;
        }

        internal static string GarmentPrefabPath(GameObject costume) => $"{PrefabFolder}/{costume.name}Garment.prefab";

        private static string GarmentMeshPath(GameObject costume) => $"{MeshFolder}/{costume.name}Garment.asset";

        // `hung` is the costume as placed on a rack (gives its upright orientation and scale); null uses the prefab's own transform.
        internal static GameObject EnsureGarment(GameObject costume, Transform hung, BossFightSettings settings)
        {
            var prefabPath = GarmentPrefabPath(costume);
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existing != null) return existing;

            var root = hung != null ? hung : costume.transform;
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(GarmentMeshPath(costume));
            var materials = Bake(root, settings.clothMaxParticles, mesh == null ? costume.name + "Garment" : null, out var baked);
            if (mesh == null)
            {
                mesh = baked;
                AssetDatabase.CreateAsset(mesh, GarmentMeshPath(costume));
            }

            var go = new GameObject(costume.name + "Garment");
            try
            {
                var skin = go.AddComponent<SkinnedMeshRenderer>();
                skin.sharedMesh = mesh;
                skin.sharedMaterials = materials.ToArray();
                skin.localBounds = mesh.bounds;
                skin.updateWhenOffscreen = false;
                PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
            return AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        }

        // Combines every MeshFilter under root into one mesh (a submesh per source submesh) in root-relative,
        // world-oriented, unscaled space, clusters it to the particle budget and centres it on the origin.
        // Returns the materials in submesh order; `mesh` is only built when meshName is non-null.
        internal static List<Material> Bake(Transform root, int maxParticles, string meshName, out Mesh mesh)
        {
            var positions = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var submeshes = new List<int[]>();
            var materials = new List<Material>();
            var origin = Matrix4x4.Translate(-root.position);
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                var source = filter.sharedMesh;
                var renderer = filter.GetComponent<MeshRenderer>();
                if (source == null || renderer == null) continue;
                var matrix = origin * filter.transform.localToWorldMatrix;
                var normalMatrix = matrix.inverse.transpose;
                bool flip = matrix.determinant < 0f;
                int offset = positions.Count;
                var srcPositions = source.vertices;
                var srcNormals = source.normals;
                var srcUvs = source.uv;
                for (int i = 0; i < srcPositions.Length; i++)
                {
                    positions.Add(matrix.MultiplyPoint3x4(srcPositions[i]));
                    normals.Add(i < srcNormals.Length ? normalMatrix.MultiplyVector(srcNormals[i]).normalized : Vector3.up);
                    uvs.Add(i < srcUvs.Length ? srcUvs[i] : Vector2.zero);
                }
                var shared = renderer.sharedMaterials;
                for (int sub = 0; sub < source.subMeshCount; sub++)
                {
                    var tris = source.GetTriangles(sub);
                    for (int t = 0; t < tris.Length; t++) tris[t] += offset;
                    if (flip)
                    {
                        for (int t = 0; t + 2 < tris.Length; t += 3) (tris[t + 1], tris[t + 2]) = (tris[t + 2], tris[t + 1]);
                    }
                    submeshes.Add(tris);
                    materials.Add(shared.Length > 0 ? shared[Mathf.Min(sub, shared.Length - 1)] : null);
                }
            }
            if (positions.Count == 0) throw new System.InvalidOperationException($"Costume '{root.name}' has no meshes to bake into a garment");

            mesh = null;
            if (meshName == null) return materials;
            var clustered = GarmentMeshRules.ClusterToBudget(positions, uvs, submeshes, maxParticles);
            var bounds = new Bounds(clustered.Positions.Length > 0 ? clustered.Positions[0] : Vector3.zero, Vector3.zero);
            foreach (var p in clustered.Positions) bounds.Encapsulate(p);
            var outPositions = new Vector3[clustered.Positions.Length];
            var outNormals = new Vector3[outPositions.Length];
            for (int i = 0; i < outPositions.Length; i++)
            {
                outPositions[i] = clustered.Positions[i] - bounds.center;
                var normal = Vector3.zero;
                foreach (int m in clustered.Members[i]) normal += normals[m];
                outNormals[i] = normal.sqrMagnitude > 1e-8f ? normal.normalized : Vector3.up;
            }
            mesh = new Mesh { name = meshName };
            mesh.indexFormat = outPositions.Length > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.vertices = outPositions;
            mesh.normals = outNormals;
            mesh.uv = clustered.Uvs;
            mesh.subMeshCount = clustered.Triangles.Length;
            for (int sub = 0; sub < clustered.Triangles.Length; sub++) mesh.SetTriangles(clustered.Triangles[sub], sub);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            Debug.Log($"GarmentClothBuilder: baked {meshName}: {positions.Count} -> {outPositions.Length} vertices, {GarmentMeshRules.ParticleCount(outPositions)} cloth particles, size {mesh.bounds.size}");
            return materials;
        }

        // The PossessedClothing prefab's own costume (WizardRobe) gets a garment too, for spawns without a rack.
        private static void EnsureFallback(Dictionary<GameObject, GameObject> garments, BossFightSettings settings)
        {
            var root = PrefabUtility.LoadPrefabContents(ClothingEnemyBuilder.ClothingPrefab);
            try
            {
                var clothing = root.GetComponent<PossessedClothing>();
                var so = new SerializedObject(clothing);
                var costume = so.FindProperty("costumePrefab").objectReferenceValue as GameObject;
                if (costume == null) throw new System.InvalidOperationException("PossessedClothing prefab has no costumePrefab to make a fallback garment from");
                if (!garments.TryGetValue(costume, out var garment)) garment = EnsureGarment(costume, null, settings);
                bool changed = BuilderUtil.SetReference(so, "garmentPrefab", garment);
                changed |= so.ApplyModifiedPropertiesWithoutUndo();
                if (changed) PrefabUtility.SaveAsPrefabAsset(root, ClothingEnemyBuilder.ClothingPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // Called by BossRigValidator with the Boss Scene open.
        public static void Validate(UnityEngine.SceneManagement.Scene scene, List<string> errors)
        {
            foreach (var rack in Racks(scene))
            {
                var hung = HungCostumes(rack.transform);
                if (rack.CostumePrefabs == null || rack.Garments == null || rack.CostumePrefabs.Length != hung.Count || rack.Garments.Length != hung.Count)
                {
                    errors.Add($"Boss Scene rack '{rack.name}': expected {hung.Count} costume/garment pairs for its hung costumes");
                    continue;
                }
                for (int i = 0; i < hung.Count; i++)
                {
                    if (rack.CostumePrefabs[i] != hung[i].costume) errors.Add($"Boss Scene rack '{rack.name}': costumePrefabs[{i}] is not its own costume '{hung[i].costume.name}'");
                    CheckGarment(rack.Garments[i], $"Boss Scene rack '{rack.name}' garment {i}", errors);
                }
            }
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ClothingEnemyBuilder.ClothingPrefab);
            var clothing = prefab != null ? prefab.GetComponent<PossessedClothing>() : null;
            if (clothing != null) CheckGarment(clothing.Garment, "PossessedClothing prefab", errors);
        }

        private static void CheckGarment(GameObject garment, string label, List<string> errors)
        {
            if (garment == null)
            {
                errors.Add($"{label}: garmentPrefab reference is missing");
                return;
            }
            var skin = garment.GetComponent<SkinnedMeshRenderer>();
            if (skin == null || skin.sharedMesh == null || !skin.sharedMesh.isReadable)
            {
                errors.Add($"{label}: garment '{garment.name}' needs a SkinnedMeshRenderer with a readable mesh");
            }
        }
    }
}
