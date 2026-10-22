using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;

namespace BossFight.Editor
{
    // Builds DraggedCustomer.prefab (a ragdoll made from the human character.fbx, in code, mirroring the Ragdoll Wizard)
    // and wires it plus the costume prefabs into MonsterCustomer.prefab's MonsterDrag.
    public static class DraggedBodyBuilder
    {
        public const string HumanModel = "Assets/Models/Characters/Human/character.fbx";
        public const string CostumesFolder = "Assets/Prefabs/Costumes";
        public const string BodyPrefab = BossFightPaths.PrefabsFolder + "/DraggedCustomer.prefab";

        public const string PartsAsset = BossFightPaths.SettingsFolder + "/DraggedCustomerParts.asset";

        // character.fbx is one unskinned mesh with no skeleton, so the ragdoll is made by cutting the mesh into body parts
        // (by height and sideways distance, as fractions of the figure's height) and giving each part its own rigidbody.
        private enum Part { Hips, Spine, Head, UpperArm, ForeArm, Hand, Thigh, Shin }

        private const float HeadFrom = 0.84f;
        private const float SpineFrom = 0.58f;
        private const float HipsFrom = 0.48f;
        private const float ThighFrom = 0.26f;
        private const float ArmLateral = 0.115f;
        private const float ArmLowest = 0.38f;
        private const float UpperArmFrom = 0.66f;
        private const float ForeArmFrom = 0.50f;

        public static void Build()
        {
            var config = ConfigBuilder.Load();
            EditorFolders.Ensure(BossFightPaths.PrefabsFolder);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(BodyPrefab) == null) CreateBodyPrefab();
            WireMonster(config);
        }

        public static void Validate(List<string> errors)
        {
            var body = AssetDatabase.LoadAssetAtPath<GameObject>(BodyPrefab);
            if (body == null)
            {
                errors.Add($"DraggedCustomer prefab missing at {BodyPrefab}");
            }
            else
            {
                if (body.GetComponentsInChildren<Rigidbody>(true).Length < 6) errors.Add("DraggedCustomer prefab has too few ragdoll rigidbodies");
                if (body.GetComponentsInChildren<CharacterJoint>(true).Length < 5) errors.Add("DraggedCustomer prefab has too few CharacterJoints");
                if (body.GetComponentsInChildren<BodyHitbox>(true).Length == 0) errors.Add("DraggedCustomer prefab has no BodyHitbox");
            }
            var monster = AssetDatabase.LoadAssetAtPath<GameObject>(MonsterEnemyBuilder.MonsterPrefab);
            var drag = monster != null ? monster.GetComponent<MonsterDrag>() : null;
            if (drag == null)
            {
                errors.Add("MonsterCustomer prefab has no MonsterDrag component");
                return;
            }
            var so = new SerializedObject(drag);
            if (so.FindProperty("config").objectReferenceValue == null) errors.Add("MonsterDrag: config reference is missing");
            if (so.FindProperty("bodyPrefab").objectReferenceValue == null) errors.Add("MonsterDrag: bodyPrefab reference is missing");
            if (so.FindProperty("costumePrefabs").arraySize == 0) errors.Add("MonsterDrag: costumePrefabs is empty");
        }

        private static void WireMonster(BossFightConfig config)
        {
            var body = AssetDatabase.LoadAssetAtPath<GameObject>(BodyPrefab);
            var costumes = new List<GameObject>();
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { CostumesFolder }))
            {
                costumes.Add(AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid)));
            }
            if (costumes.Count == 0) throw new System.InvalidOperationException($"No costume prefabs found in {CostumesFolder}");

            var contents = PrefabUtility.LoadPrefabContents(MonsterEnemyBuilder.MonsterPrefab);
            try
            {
                var drag = contents.GetComponent<MonsterDrag>();
                bool changed = false;
                if (drag == null)
                {
                    drag = contents.AddComponent<MonsterDrag>();
                    changed = true;
                }
                var so = new SerializedObject(drag);
                changed |= BuilderUtil.SetReference(so, "config", config);
                changed |= BuilderUtil.SetReference(so, "bodyPrefab", body);
                var array = so.FindProperty("costumePrefabs");
                if (array.arraySize != costumes.Count) changed = true;
                array.arraySize = costumes.Count;
                for (int i = 0; i < costumes.Count; i++)
                {
                    var element = array.GetArrayElementAtIndex(i);
                    if (element.objectReferenceValue != costumes[i])
                    {
                        element.objectReferenceValue = costumes[i];
                        changed = true;
                    }
                }
                changed |= so.ApplyModifiedPropertiesWithoutUndo();
                if (changed) PrefabUtility.SaveAsPrefabAsset(contents, MonsterEnemyBuilder.MonsterPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static void CreateBodyPrefab()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(HumanModel);
            if (source == null) throw new System.InvalidOperationException($"Human model missing at {HumanModel}");
            EditorFolders.Ensure(BossFightPaths.SettingsFolder);
            var root = new GameObject("DraggedCustomer");
            var temp = (GameObject)PrefabUtility.InstantiatePrefab(source);
            try
            {
                var filter = temp.GetComponentInChildren<MeshFilter>(true);
                var renderer = temp.GetComponentInChildren<MeshRenderer>(true);
                if (filter == null || filter.sharedMesh == null || renderer == null)
                {
                    throw new System.InvalidOperationException($"{HumanModel} has no mesh to build a ragdoll from");
                }
                BuildRagdoll(root.transform, filter, renderer.sharedMaterial);
                root.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
                PrefabUtility.SaveAsPrefabAsset(root, BodyPrefab);
            }
            finally
            {
                Object.DestroyImmediate(temp);
                Object.DestroyImmediate(root);
            }
        }

        private struct PartKey
        {
            public Part Part;
            public int Side;
        }

        private static void BuildRagdoll(Transform root, MeshFilter filter, Material material)
        {
            var mesh = filter.sharedMesh;
            var toRoot = filter.transform.localToWorldMatrix;
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            var uvs = mesh.uv;
            var triangles = mesh.triangles;
            var world = new Vector3[vertices.Length];
            var bounds = new Bounds(toRoot.MultiplyPoint3x4(vertices[0]), Vector3.zero);
            for (int i = 0; i < vertices.Length; i++)
            {
                world[i] = toRoot.MultiplyPoint3x4(vertices[i]);
                bounds.Encapsulate(world[i]);
            }
            float height = bounds.size.y;
            if (height < 0.1f) throw new System.InvalidOperationException($"{HumanModel} figure is {height} m tall; expected an upright human");
            // Sideways is the wider horizontal axis; feet are at the bottom.
            bool sideX = bounds.size.x >= bounds.size.z;
            float centre = sideX ? bounds.center.x : bounds.center.z;

            var groups = new Dictionary<PartKey, List<int>>();
            for (int t = 0; t < triangles.Length; t += 3)
            {
                var c = (world[triangles[t]] + world[triangles[t + 1]] + world[triangles[t + 2]]) / 3f;
                float h = (c.y - bounds.min.y) / height;
                float side = (sideX ? c.x : c.z) - centre;
                var key = Classify(h, side / height);
                if (!groups.TryGetValue(key, out var list))
                {
                    list = new List<int>();
                    groups[key] = list;
                }
                list.Add(t);
            }

            if (AssetDatabase.LoadAssetAtPath<Object>(PartsAsset) != null) AssetDatabase.DeleteAsset(PartsAsset);
            bool assetCreated = false;
            var bones = new Dictionary<PartKey, Transform>();
            foreach (var part in new[] { Part.Hips, Part.Spine, Part.Head, Part.UpperArm, Part.ForeArm, Part.Hand, Part.Thigh, Part.Shin })
            {
                foreach (int side in IsSided(part) ? new[] { -1, 1 } : new[] { 0 })
                {
                    var key = new PartKey { Part = part, Side = side };
                    if (!groups.TryGetValue(key, out var tris) || tris.Count == 0) continue;
                    // Pivot: top centre of the part, so the joint sits where the limb attaches.
                    var partBounds = new Bounds(world[triangles[tris[0]]], Vector3.zero);
                    foreach (int t in tris)
                    {
                        for (int k = 0; k < 3; k++) partBounds.Encapsulate(world[triangles[t + k]]);
                    }
                    var pivot = new Vector3(partBounds.center.x, partBounds.max.y, partBounds.center.z);

                    var go = new GameObject(BoneName(part, side));
                    go.transform.SetParent(ParentOf(part, side, bones, root), false);
                    go.transform.position = pivot;
                    bones[key] = go.transform;

                    var partMesh = SliceMesh(go.name, tris, triangles, world, normals, uvs, pivot);
                    if (!assetCreated)
                    {
                        AssetDatabase.CreateAsset(partMesh, PartsAsset);
                        assetCreated = true;
                    }
                    else
                    {
                        AssetDatabase.AddObjectToAsset(partMesh, PartsAsset);
                    }
                    go.AddComponent<MeshFilter>().sharedMesh = partMesh;
                    go.AddComponent<MeshRenderer>().sharedMaterial = material;
                    AddPhysics(go, part, partMesh.bounds, ParentBody(go.transform, root));
                }
            }
            if (bones.Count < 8) throw new System.InvalidOperationException($"{HumanModel} only split into {bones.Count} body parts");
            AssetDatabase.SaveAssets();
        }

        private static PartKey Classify(float h, float side)
        {
            float lateral = Mathf.Abs(side);
            int sign = side < 0f ? -1 : 1;
            if (h > HeadFrom) return new PartKey { Part = Part.Head };
            if (h > ArmLowest && lateral > ArmLateral)
            {
                var arm = h > UpperArmFrom ? Part.UpperArm : (h > ForeArmFrom ? Part.ForeArm : Part.Hand);
                return new PartKey { Part = arm, Side = sign };
            }
            if (h > SpineFrom) return new PartKey { Part = Part.Spine };
            if (h > HipsFrom) return new PartKey { Part = Part.Hips };
            return new PartKey { Part = h > ThighFrom ? Part.Thigh : Part.Shin, Side = sign };
        }

        private static bool IsSided(Part part)
        {
            return part == Part.UpperArm || part == Part.ForeArm || part == Part.Hand || part == Part.Thigh || part == Part.Shin;
        }

        // Names end in "Hand" so MonsterDrag can pick the wrist; the right-hand one is preferred.
        private static string BoneName(Part part, int side)
        {
            string prefix = side < 0 ? "Left" : (side > 0 ? "Right" : "");
            return prefix + part;
        }

        private static Transform ParentOf(Part part, int side, Dictionary<PartKey, Transform> bones, Transform root)
        {
            switch (part)
            {
                case Part.Hips: return root;
                case Part.Spine: return Lookup(bones, Part.Hips, 0, root);
                case Part.Head:
                case Part.UpperArm: return Lookup(bones, Part.Spine, 0, root);
                case Part.ForeArm: return Lookup(bones, Part.UpperArm, side, root);
                case Part.Hand: return Lookup(bones, Part.ForeArm, side, root);
                case Part.Thigh: return Lookup(bones, Part.Hips, 0, root);
                default: return Lookup(bones, Part.Thigh, side, root);
            }
        }

        private static Transform Lookup(Dictionary<PartKey, Transform> bones, Part part, int side, Transform fallback)
        {
            return bones.TryGetValue(new PartKey { Part = part, Side = side }, out var t) ? t : fallback;
        }

        private static Rigidbody ParentBody(Transform bone, Transform root)
        {
            return bone.parent != null && bone.parent != root ? bone.parent.GetComponent<Rigidbody>() : null;
        }

        private static Mesh SliceMesh(string name, List<int> tris, int[] triangles, Vector3[] world, Vector3[] normals, Vector2[] uvs, Vector3 pivot)
        {
            var map = new Dictionary<int, int>();
            var v = new List<Vector3>();
            var n = new List<Vector3>();
            var uv = new List<Vector2>();
            var indices = new List<int>();
            foreach (int t in tris)
            {
                for (int k = 0; k < 3; k++)
                {
                    int source = triangles[t + k];
                    if (!map.TryGetValue(source, out int index))
                    {
                        index = v.Count;
                        map[source] = index;
                        v.Add(world[source] - pivot);
                        n.Add(normals.Length > source ? normals[source] : Vector3.up);
                        uv.Add(uvs.Length > source ? uvs[source] : Vector2.zero);
                    }
                    indices.Add(index);
                }
            }
            var mesh = new Mesh { name = name };
            mesh.SetVertices(v);
            mesh.SetNormals(n);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(indices, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddPhysics(GameObject go, Part part, Bounds b, Rigidbody parent)
        {
            switch (part)
            {
                case Part.Head:
                case Part.Hand:
                    var sphere = go.AddComponent<SphereCollider>();
                    sphere.center = b.center;
                    sphere.radius = Mathf.Max(0.03f, Mathf.Max(b.extents.x, b.extents.z));
                    break;
                case Part.Hips:
                case Part.Spine:
                    var box = go.AddComponent<BoxCollider>();
                    box.center = b.center;
                    box.size = b.size;
                    break;
                default:
                    var capsule = go.AddComponent<CapsuleCollider>();
                    capsule.direction = 1;
                    capsule.center = b.center;
                    capsule.height = Mathf.Max(b.size.y, 0.05f);
                    capsule.radius = Mathf.Max(0.02f, Mathf.Min(b.extents.x, b.extents.z));
                    break;
            }
            var body = go.AddComponent<Rigidbody>();
            body.mass = MassFor(part);
            body.linearDamping = 0.3f;
            body.angularDamping = 0.5f;
            go.AddComponent<BodyHitbox>();
            if (parent == null) return;
            var joint = go.AddComponent<CharacterJoint>();
            joint.connectedBody = parent;
            joint.axis = Vector3.right;
            joint.swingAxis = Vector3.forward;
            joint.enablePreprocessing = false;
            joint.enableProjection = true;
            joint.lowTwistLimit = new SoftJointLimit { limit = -30f };
            joint.highTwistLimit = new SoftJointLimit { limit = 30f };
            joint.swing1Limit = new SoftJointLimit { limit = 45f };
            joint.swing2Limit = new SoftJointLimit { limit = 45f };
        }

        private static float MassFor(Part part)
        {
            switch (part)
            {
                case Part.Hips: return 12f;
                case Part.Spine: return 10f;
                case Part.Head: return 4f;
                case Part.Thigh:
                case Part.Shin: return 5f;
                default: return 2f;
            }
        }
    }
}
