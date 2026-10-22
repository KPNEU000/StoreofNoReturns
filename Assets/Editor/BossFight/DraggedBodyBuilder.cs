using System.Collections.Generic;
using BossFight.Rules;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;

namespace BossFight.Editor
{
    // Builds DraggedCustomer.prefab (a ragdoll made from the human character.fbx, in code, mirroring the Ragdoll Wizard)
    // and wires it plus the costume prefabs into MonsterCustomer.prefab's MonsterDrag.
    public static class DraggedBodyBuilder
    {
        public const string CharacterPrefab = "Assets/Prefabs/CC Attribution Characters/Character.prefab";
        public const string CostumesFolder = "Assets/Prefabs/Costumes";
        public const string BodyPrefab = BossFightPaths.PrefabsFolder + "/DraggedCustomer.prefab";

        // The old 13-piece cut body's mesh asset; deleted when found.
        private const string OldPartsAsset = BossFightPaths.SettingsFolder + "/DraggedCustomerParts.asset";

        public static void Build()
        {
            var config = ConfigBuilder.Load();
            EditorFolders.Ensure(BossFightPaths.PrefabsFolder);
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(BodyPrefab);
            if (existing == null || !IsRiggedBody(existing)) CreateBodyPrefab();
            if (AssetDatabase.LoadAssetAtPath<Object>(OldPartsAsset) != null) AssetDatabase.DeleteAsset(OldPartsAsset);
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
                if (!IsRiggedBody(body)) errors.Add("DraggedCustomer prefab is not the rigged Mixamo customer ragdoll");
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

        // True for a body made from the skinned Mixamo model (has a skinned mesh and a mixamorig Hips rigidbody).
        internal static bool IsRiggedBody(GameObject body)
        {
            if (body.GetComponentInChildren<SkinnedMeshRenderer>(true) == null) return false;
            foreach (var rb in body.GetComponentsInChildren<Rigidbody>(true))
            {
                if (EnemyDeathRules.IsBone(rb.name, "Hips")) return true;
            }
            return false;
        }

        private static void CreateBodyPrefab()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterPrefab);
            if (source == null) throw new System.InvalidOperationException($"Customer prefab missing at {CharacterPrefab}");
            var root = (GameObject)PrefabUtility.InstantiatePrefab(source);
            try
            {
                PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                root.name = "DraggedCustomer";
                root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                MakeBody(root);
                PrefabUtility.SaveAsPrefabAsset(root, BodyPrefab);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // Strips the shop behaviour from a customer model and turns its Mixamo bones into a ragdoll with hitboxes.
        internal static void MakeBody(GameObject root)
        {
            foreach (var c in root.GetComponentsInChildren<Component>(true))
            {
                if (c is Animator || c is MonoBehaviour || c is Collider || c is Rigidbody || c is UnityEngine.AI.NavMeshAgent) Object.DestroyImmediate(c);
            }
            foreach (var skinned in root.GetComponentsInChildren<SkinnedMeshRenderer>(true)) skinned.updateWhenOffscreen = true;
            var bodies = new List<Rigidbody>();
            MonsterRagdoll.BuildBodies(root.transform, bodies);
            if (bodies.Count < 8) throw new System.InvalidOperationException($"{CharacterPrefab} only gave {bodies.Count} ragdoll bodies; are the mixamorig bones missing?");
            foreach (var body in bodies) body.gameObject.AddComponent<BodyHitbox>();
            root.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
        }
    }
}
