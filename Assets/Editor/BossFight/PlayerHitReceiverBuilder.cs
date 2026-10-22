using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace BossFight.Editor
{
    // Puts PlayerHitReceiver (plus its red flash overlay) on the Boss Scene player.
    public static class PlayerHitReceiverBuilder
    {
        public const string FlashCanvasName = "HitFlashCanvas";
        public const string FlashImageName = "HitFlash";

        public static void Build()
        {
            var config = ConfigBuilder.Load();
            using (var boss = new BossSceneScope())
            {
                var player = BuilderUtil.FindPlayer(boss.Scene);
                if (player == null)
                {
                    throw new System.InvalidOperationException("Boss Scene has no PlayerMovement object to receive hits");
                }
                var moneyManager = BuilderUtil.FindInScene<MoneyManager>(boss.Scene);
                var reputationManager = BuilderUtil.FindInScene<ReputationManager>(boss.Scene);
                if (moneyManager == null)
                {
                    throw new System.InvalidOperationException("Boss Scene has no MoneyManager");
                }

                bool changed = false;
                var receiver = player.GetComponent<PlayerHitReceiver>();
                if (receiver == null)
                {
                    receiver = player.gameObject.AddComponent<PlayerHitReceiver>();
                    changed = true;
                }

                var flash = EnsureFlash(boss.Scene, ref changed);

                var so = new SerializedObject(receiver);
                so.FindProperty("config").objectReferenceValue = config;
                so.FindProperty("moneyManager").objectReferenceValue = moneyManager;
                so.FindProperty("reputationManager").objectReferenceValue = reputationManager;
                so.FindProperty("hitFlash").objectReferenceValue = flash;
                changed |= so.ApplyModifiedProperties();

                if (changed)
                {
                    EditorSceneManager.MarkSceneDirty(boss.Scene);
                }
                boss.SaveIfDirty();
            }
        }

        private static Image EnsureFlash(UnityEngine.SceneManagement.Scene scene, ref bool changed)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name != FlashCanvasName)
                {
                    continue;
                }
                var existing = root.GetComponentInChildren<Image>(true);
                if (existing != null)
                {
                    return existing;
                }
            }

            var canvasGo = new GameObject(FlashCanvasName, typeof(Canvas));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(canvasGo, scene);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var imageGo = new GameObject(FlashImageName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageGo.transform.SetParent(canvasGo.transform, false);
            var rect = (RectTransform)imageGo.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = imageGo.GetComponent<Image>();
            image.color = new Color(1f, 0f, 0f, 0f);
            image.raycastTarget = false;
            changed = true;
            return image;
        }
    }
}
