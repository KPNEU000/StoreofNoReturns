using BossFight.Rules;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace BossFight
{
    // A cracked-glass mesh drawn with the BossFight/GlassShard shader, which refracts the camera's opaque texture.
    // HUD mode (followCamera): a jagged shard parented to the player camera, kept over the top-right viewport rect.
    // Otherwise a flat rectangle sized by the builder (the cracked monitor screen).
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class GlassShard : MonoBehaviour
    {
        // Just past the near plane, so world geometry sits behind it; the HUD material ignores depth anyway.
        private const float NearPlaneOffset = 0.02f;

        [SerializeField]
        private BossFightConfig config;
        [SerializeField]
        private Camera viewCamera;
        [SerializeField]
        private bool followCamera = true;

        private BossFightSettings settings;
        private Rect viewport;
        private Mesh mesh;
        private float placedAspect;
        private float placedFov;

        public Rect Viewport => viewport;
        public bool FollowsCamera => followCamera;

        private void Awake()
        {
            settings = BossFightConfig.SettingsOf(config);
            viewport = GlassShardRules.ScreenRect(settings.glassHudWidth, settings.glassHudHeight, settings.glassHudMargin);
            if (viewCamera == null) viewCamera = GetComponentInParent<Camera>();
            if (viewCamera == null) viewCamera = Camera.main;
            mesh = BuildMesh(settings, followCamera);
            GetComponent<MeshFilter>().sharedMesh = mesh;
            var meshRenderer = GetComponent<MeshRenderer>();
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            if (followCamera)
            {
                // Crack size is tuned in the config rather than on the shared material asset.
                var block = new MaterialPropertyBlock();
                meshRenderer.GetPropertyBlock(block);
                block.SetFloat("_CrackScale", settings.glassHudCrackScale);
                block.SetFloat("_CrackWidth", settings.glassHudCrackWidth);
                meshRenderer.SetPropertyBlock(block);
            }
            EnableOpaqueTexture(viewCamera);
        }

        private void OnDestroy()
        {
            if (mesh != null) Destroy(mesh);
        }

        private void LateUpdate()
        {
            if (!followCamera || viewCamera == null) return;
            if (Mathf.Approximately(placedAspect, viewCamera.aspect) && Mathf.Approximately(placedFov, viewCamera.fieldOfView)) return;
            placedAspect = viewCamera.aspect;
            placedFov = viewCamera.fieldOfView;
            Place(transform, viewCamera, viewport);
        }

        // The shader samples _CameraOpaqueTexture, which URP only produces when the camera asks for it.
        public static void EnableOpaqueTexture(Camera camera)
        {
            if (camera == null) return;
            camera.GetUniversalAdditionalCameraData().requiresColorTexture = true;
        }

        // Puts a unit shard (local [-0.5, 0.5]) over `rect` of the camera's view, facing the camera.
        public static void Place(Transform shard, Camera camera, Rect rect)
        {
            GlassShardRules.CameraPlacement(rect, camera.fieldOfView, camera.aspect, camera.nearClipPlane + NearPlaneOffset, out var position, out var size);
            if (shard.parent != camera.transform) shard.SetParent(camera.transform, false);
            shard.localPosition = position;
            shard.localRotation = Quaternion.identity;
            shard.localScale = new Vector3(size.x, size.y, 1f);
        }

        // Jagged outline for the HUD, a plain rectangle (cracks still fade in at its edges) for the monitor.
        public static Mesh BuildMesh(BossFightSettings settings, bool jagged)
        {
            var outline = jagged
                ? GlassShardRules.Outline(settings.glassHudSeed, settings.glassHudOutlinePoints, settings.glassHudJaggedness)
                : GlassShardRules.Outline(settings.glassHudSeed, 4, 0f);
            GlassShardRules.BuildMesh(outline, settings.glassEdgeFalloffExponent, out var vertices, out var uv, out var edge, out var triangles);
            var result = new Mesh { name = jagged ? "GlassHudShard" : "GlassScreen" };
            result.vertices = vertices;
            result.uv = uv;
            result.uv2 = edge;
            result.triangles = triangles;
            result.RecalculateNormals();
            result.RecalculateBounds();
            return result;
        }
    }
}
