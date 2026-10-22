using System.Collections;
using UnityEngine;

namespace BossFight
{
    // A shop clothing rack the Witch can possess: the frame fades out in purple, then its renderers and
    // colliders go away and the costumes lift off from that spot.
    public class PossessableRack : MonoBehaviour
    {
        [SerializeField]
        private BossFightConfig config;
        [SerializeField]
        private Color possessedColor = new Color(0.6f, 0.1f, 0.9f, 1f);

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private bool possessed;
        private Vector3 releasePoint;

        public bool IsPossessed => possessed;
        // Where costumes appear; stays valid after the frame is gone so the spot can be re-used.
        public Vector3 ReleasePoint => possessed ? releasePoint : CenterPoint();

        private Vector3 CenterPoint()
        {
            var renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return transform.position;
            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return new Vector3(bounds.center.x, transform.position.y, bounds.center.z);
        }

        // Returns false when already possessed. onFaded runs once the frame is gone (NavMesh can be rebuilt then).
        public bool Possess(ClothingSpawner spawner, int count, System.Action onFaded = null)
        {
            if (possessed) return false;
            possessed = true;
            releasePoint = CenterPoint();
            StartCoroutine(FadeRoutine(spawner, count, onFaded));
            return true;
        }

        private IEnumerator FadeRoutine(ClothingSpawner spawner, int count, System.Action onFaded)
        {
            var settings = config != null ? config.ToSettings() : new BossFightSettings();
            float seconds = Mathf.Max(settings.rackFadeSeconds, 0.01f);
            var renderers = GetComponentsInChildren<Renderer>();
            var block = new MaterialPropertyBlock();
            var startScale = transform.localScale;
            var burst = CreateBurst();
            burst.transform.position = releasePoint + Vector3.up * 0.8f;
            burst.Play();
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                float k = Mathf.Clamp01(t / seconds);
                var color = Color.Lerp(Color.white, possessedColor, Mathf.Min(1f, k * 2f));
                foreach (var r in renderers)
                {
                    r.GetPropertyBlock(block);
                    block.SetColor(BaseColorId, color);
                    block.SetColor(ColorId, color);
                    block.SetColor(EmissionColorId, possessedColor * k);
                    r.SetPropertyBlock(block);
                }
                transform.localScale = startScale * Mathf.Lerp(1f, 0.05f, k * k);
                yield return null;
            }
            foreach (var r in renderers)
            {
                r.SetPropertyBlock(null);
                r.enabled = false;
            }
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
            transform.localScale = startScale;
            onFaded?.Invoke();
            if (spawner != null) spawner.Spawn(releasePoint, count);
            Destroy(burst.gameObject, 3f);
        }

        // Procedural purple particle burst; no particle assets needed.
        private ParticleSystem CreateBurst()
        {
            var go = new GameObject("RackPossessBurst");
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startColor = possessedColor;
            main.startLifetime = 1f;
            main.startSpeed = 2f;
            main.startSize = 0.15f;
            main.gravityModifier = -0.2f;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 40) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.6f;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader != null) renderer.material = new Material(shader);
            return ps;
        }
    }
}
