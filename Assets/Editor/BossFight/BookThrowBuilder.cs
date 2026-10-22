using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BossFight.Editor
{
    // Placeholder bookshelves with ThrowableBook books on the Boss Scene side walls, plus the WitchBookAttack object.
    public static class BookThrowBuilder
    {
        public const string AttackName = "WitchBookAttack";
        public const string ShelvesRootName = "BookShelves";
        public const string MagicMaterial = BossFightPaths.SettingsFolder + "/BookMagic.mat";
        private const string ShelfMaterial = BossFightPaths.SettingsFolder + "/BookShelf.mat";
        private const string BookMaterialPrefix = BossFightPaths.SettingsFolder + "/Book";
        private static readonly string[] WallNames = { "Clothing Racks Left Wall", "Clothing Racks Right Wall" };
        private static readonly Color[] BookColors =
        {
            new Color(0.55f, 0.1f, 0.1f), new Color(0.1f, 0.3f, 0.6f), new Color(0.15f, 0.45f, 0.2f),
            new Color(0.6f, 0.45f, 0.1f), new Color(0.35f, 0.15f, 0.45f), new Color(0.25f, 0.25f, 0.25f)
        };

        private const float ShelfWidth = 2.4f;
        private const float ShelfDepth = 0.35f;
        private const float PlankSpacing = 0.6f;
        private const int Rows = 3;
        private const int BooksPerRow = 6;

        public static void Build()
        {
            var config = ConfigBuilder.Load();
            var magic = BossDirectorBuilder.EnsureTransparentMaterial(MagicMaterial, "Universal Render Pipeline/Unlit", new Color(0.7f, 0.25f, 1f, 0.85f));
            var shelfMaterial = EnsureLitMaterial(ShelfMaterial, new Color(0.35f, 0.22f, 0.12f));
            var bookMaterials = new Material[BookColors.Length];
            for (int i = 0; i < BookColors.Length; i++) bookMaterials[i] = EnsureLitMaterial($"{BookMaterialPrefix}{i}.mat", BookColors[i]);

            using (var boss = new BossSceneScope())
            {
                var player = PlayerHitReceiverBuilder.FindPlayer(boss.Scene);
                var receiver = PlayerHitReceiverBuilder.FindInScene<PlayerHitReceiver>(boss.Scene);
                var director = PlayerHitReceiverBuilder.FindInScene<BossDirector>(boss.Scene);
                var witch = PlayerHitReceiverBuilder.FindInScene<WitchBoss>(boss.Scene);
                if (player == null || receiver == null || director == null || witch == null)
                {
                    throw new System.InvalidOperationException("Book throw builder needs the player, PlayerHitReceiver, BossDirector and Witch in Boss Scene");
                }

                bool changed = false;
                var root = BossDirectorBuilder.FindByName(boss.Scene, ShelvesRootName);
                if (root == null)
                {
                    var go = new GameObject(ShelvesRootName);
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, boss.Scene);
                    root = go.transform;
                    changed = true;
                }

                var controller = player.GetComponent<CharacterController>();
                float floorY = controller != null
                    ? player.transform.position.y + controller.center.y - controller.height * 0.5f
                    : player.transform.position.y - 1f;
                for (int i = 0; i < WallNames.Length; i++)
                {
                    var wall = BossDirectorBuilder.FindByName(boss.Scene, WallNames[i]);
                    if (wall == null)
                    {
                        throw new System.InvalidOperationException($"Boss Scene has no '{WallNames[i]}' to place a bookshelf against");
                    }
                    string shelfName = $"Bookshelf {i + 1}";
                    if (root.Find(shelfName) == null)
                    {
                        CreateShelf(root, shelfName, wall, player.transform.position, floorY, shelfMaterial, bookMaterials);
                        changed = true;
                    }
                }

                var books = root.GetComponentsInChildren<ThrowableBook>(true);
                foreach (var book in books)
                {
                    var bookSo = new SerializedObject(book);
                    changed |= RigPrefabBuilder.SetReference(bookSo, "trailMaterial", magic);
                    changed |= bookSo.ApplyModifiedPropertiesWithoutUndo();
                }

                var attack = PlayerHitReceiverBuilder.FindInScene<WitchBookAttack>(boss.Scene);
                if (attack == null)
                {
                    var go = new GameObject(AttackName);
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, boss.Scene);
                    attack = go.AddComponent<WitchBookAttack>();
                    changed = true;
                }
                var so = new SerializedObject(attack);
                changed |= RigPrefabBuilder.SetReference(so, "config", config);
                changed |= RigPrefabBuilder.SetReference(so, "director", director);
                changed |= RigPrefabBuilder.SetReference(so, "witch", witch.transform);
                changed |= RigPrefabBuilder.SetReference(so, "receiver", receiver);
                changed |= RigPrefabBuilder.SetReference(so, "magicMaterial", magic);
                changed |= SetArray(so.FindProperty("books"), books);
                changed |= so.ApplyModifiedPropertiesWithoutUndo();

                if (changed) EditorSceneManager.MarkSceneDirty(boss.Scene);
                boss.SaveIfDirty();
            }
        }

        private static bool SetArray(SerializedProperty property, Object[] values)
        {
            bool same = property.arraySize == values.Length;
            for (int i = 0; same && i < values.Length; i++)
            {
                same = property.GetArrayElementAtIndex(i).objectReferenceValue == values[i];
            }
            if (same) return false;
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            return true;
        }

        // Stands just outside the wall group's bounds on the side away from the player, facing into the room.
        private static void CreateShelf(Transform root, string name, Transform wall, Vector3 playerPosition, float floorY, Material shelfMaterial, Material[] bookMaterials)
        {
            var renderers = wall.GetComponentsInChildren<Renderer>(true);
            var bounds = new Bounds(wall.position, Vector3.zero);
            bool first = true;
            foreach (var r in renderers)
            {
                if (first) { bounds = r.bounds; first = false; }
                else bounds.Encapsulate(r.bounds);
            }
            var toWall = bounds.center - playerPosition;
            toWall.y = 0f;
            bool alongX = Mathf.Abs(toWall.x) >= Mathf.Abs(toWall.z);
            var outward = alongX ? new Vector3(Mathf.Sign(toWall.x), 0f, 0f) : new Vector3(0f, 0f, Mathf.Sign(toWall.z));
            float extent = alongX ? bounds.extents.x : bounds.extents.z;
            var position = bounds.center + outward * (extent + ShelfDepth * 0.5f + 0.05f);
            position.y = floorY;

            var shelf = new GameObject(name).transform;
            shelf.SetParent(root, false);
            shelf.SetPositionAndRotation(position, Quaternion.LookRotation(-outward));

            float height = PlankSpacing * (Rows + 1) + 0.05f;
            AddBox(shelf, "Back", new Vector3(0f, height * 0.5f, -ShelfDepth * 0.5f), new Vector3(ShelfWidth, height, 0.05f), shelfMaterial);
            AddBox(shelf, "SideLeft", new Vector3(-ShelfWidth * 0.5f, height * 0.5f, 0f), new Vector3(0.05f, height, ShelfDepth), shelfMaterial);
            AddBox(shelf, "SideRight", new Vector3(ShelfWidth * 0.5f, height * 0.5f, 0f), new Vector3(0.05f, height, ShelfDepth), shelfMaterial);
            for (int row = 0; row <= Rows; row++)
            {
                float y = PlankSpacing * row + 0.025f;
                AddBox(shelf, $"Plank {row}", new Vector3(0f, y, 0f), new Vector3(ShelfWidth, 0.05f, ShelfDepth), shelfMaterial);
            }

            int n = 0;
            for (int row = 0; row < Rows; row++)
            {
                float x = -ShelfWidth * 0.5f + 0.2f;
                for (int col = 0; col < BooksPerRow; col++)
                {
                    float thickness = 0.1f + 0.04f * ((n * 7) % 3);
                    float bookHeight = 0.3f + 0.05f * ((n * 5) % 4);
                    var book = AddBox(shelf, $"Book {n + 1}", new Vector3(x + thickness * 0.5f, PlankSpacing * row + 0.05f + bookHeight * 0.5f, 0f),
                        new Vector3(thickness, bookHeight, 0.22f), bookMaterials[n % bookMaterials.Length]);
                    book.AddComponent<ThrowableBook>();
                    x += ShelfWidth / BooksPerRow;
                    n++;
                }
            }
        }

        private static GameObject AddBox(Transform parent, string name, Vector3 localPosition, Vector3 scale, Material material)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = localPosition;
            box.transform.localScale = scale;
            box.GetComponent<MeshRenderer>().sharedMaterial = material;
            return box;
        }

        private static Material EnsureLitMaterial(string path, Color color)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new System.InvalidOperationException($"Shader 'Universal Render Pipeline/Lit' not found for {path}");
            var material = new Material(shader);
            material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, path);
            AssetDatabase.SaveAssetIfDirty(material);
            return material;
        }

        public static void Validate(UnityEngine.SceneManagement.Scene scene, List<string> errors)
        {
            if (AssetDatabase.LoadAssetAtPath<Material>(MagicMaterial) == null) errors.Add($"Material missing at {MagicMaterial}");
            var root = BossDirectorBuilder.FindByName(scene, ShelvesRootName);
            if (root == null)
            {
                errors.Add($"Boss Scene has no '{ShelvesRootName}' object");
            }
            else
            {
                if (root.childCount < 2) errors.Add($"Boss Scene '{ShelvesRootName}' needs at least 2 bookshelves");
                var books = root.GetComponentsInChildren<ThrowableBook>(true);
                if (books.Length == 0) errors.Add("Boss Scene bookshelves hold no ThrowableBook");
                foreach (var book in books)
                {
                    if (book.GetComponent<Collider>() == null) errors.Add($"Book '{book.name}' has no collider");
                    if (book.GetComponent<Renderer>() == null) errors.Add($"Book '{book.name}' has no renderer");
                }
            }
            var attack = PlayerHitReceiverBuilder.FindInScene<WitchBookAttack>(scene);
            if (attack == null)
            {
                errors.Add($"Boss Scene has no {AttackName} with a WitchBookAttack component");
                return;
            }
            var so = new SerializedObject(attack);
            foreach (var field in new[] { "config", "director", "witch", "receiver", "magicMaterial" })
            {
                if (so.FindProperty(field).objectReferenceValue == null) errors.Add($"Boss Scene WitchBookAttack: {field} reference is missing");
            }
            if (so.FindProperty("books").arraySize == 0) errors.Add("Boss Scene WitchBookAttack: books list is empty");
        }
    }
}
