using System.Collections;
using System.Collections.Generic;
using BossFight.Rules;
using UnityEngine;

namespace BossFight
{
    // The Witch's only attack, Phase2: a purple arm reaches a wall book, a slash arc sweeps, the book glows and
    // hangs (telegraph), then flies at where the player stood at release time.
    public class WitchBookAttack : MonoBehaviour
    {
        [SerializeField]
        private BossFightConfig config;
        [SerializeField]
        private BossDirector director;
        [SerializeField]
        private Transform witch;
        [SerializeField]
        private PlayerHitReceiver receiver;
        [SerializeField]
        private ThrowableBook[] books;
        [SerializeField]
        private Material magicMaterial;
        [SerializeField]
        private Vector3 handOffset = new Vector3(0f, 1.3f, 0f);
        [SerializeField]
        private float arcRadius = 2.2f;
        [SerializeField]
        private float arcDistance = 1.5f;
        [SerializeField]
        private float arcSweepDegrees = 120f;
        [SerializeField]
        private float hangRiseMeters = 0.3f;
        [SerializeField]
        private AudioClip castSound;

        private BookThrowRules rules;
        private LineRenderer arm;
        private Transform arcPivot;
        private TrailRenderer arcTrail;
        private Coroutine sequence;
        private ThrowableBook current;

        private void Awake()
        {
            var settings = BossFightConfig.SettingsOf(config);
            rules = new BookThrowRules(settings);
            if (director == null) director = FindFirstObjectByType<BossDirector>();
            if (receiver == null) receiver = FindFirstObjectByType<PlayerHitReceiver>();
            if (witch == null)
            {
                var boss = FindFirstObjectByType<WitchBoss>();
                if (boss != null) witch = boss.transform;
            }
            if (books == null || books.Length == 0) books = FindObjectsByType<ThrowableBook>(FindObjectsSortMode.None);
            BuildEffects();
        }

        private void BuildEffects()
        {
            var material = magicMaterial != null ? magicMaterial : new Material(Shader.Find("Sprites/Default"));
            var armObject = new GameObject("BookMagicArm");
            armObject.transform.SetParent(transform, false);
            arm = armObject.AddComponent<LineRenderer>();
            arm.positionCount = 2;
            arm.useWorldSpace = true;
            arm.widthMultiplier = 0.18f;
            arm.sharedMaterial = material;
            arm.startColor = new Color(0.8f, 0.3f, 1f, 0.9f);
            arm.endColor = new Color(0.6f, 0.15f, 1f, 0.9f);
            arm.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            arm.enabled = false;

            var pivotObject = new GameObject("BookSlashArc");
            pivotObject.transform.SetParent(transform, false);
            arcPivot = pivotObject.transform;
            var tip = new GameObject("ArcTip");
            tip.transform.SetParent(arcPivot, false);
            tip.transform.localPosition = new Vector3(0f, arcRadius, 0f);
            arcTrail = tip.AddComponent<TrailRenderer>();
            arcTrail.time = 0.4f;
            arcTrail.widthMultiplier = 0.6f;
            arcTrail.sharedMaterial = material;
            arcTrail.startColor = new Color(0.85f, 0.4f, 1f, 1f);
            arcTrail.endColor = new Color(0.5f, 0.1f, 0.9f, 0f);
            arcTrail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            arcTrail.emitting = false;
        }

        private bool Phase2 => director != null && director.Phase == BossPhase.Phase2;

        private void Update()
        {
            bool phase2 = Phase2;
            if (!phase2 && sequence != null)
            {
                CancelSequence();
                return;
            }
            if (rules.Tick(Time.deltaTime, phase2, sequence != null) && witch != null && receiver != null)
            {
                sequence = StartCoroutine(ThrowRoutine());
            }
        }

        private Vector3 Hand => witch.position + handOffset;

        private Vector3 PlayerCenter()
        {
            var controller = receiver.GetComponent<CharacterController>();
            return controller != null ? receiver.transform.TransformPoint(controller.center) : receiver.transform.position + Vector3.up;
        }

        private ThrowableBook PickBook()
        {
            var candidates = new List<ThrowableBook>();
            foreach (var book in books)
            {
                if (book != null && book.IsShelved) candidates.Add(book);
            }
            if (candidates.Count == 0)
            {
                // Everything is on the floor: the longest-settled book goes back to its shelf.
                ThrowableBook oldest = null;
                foreach (var book in books)
                {
                    if (book != null && book.IsInert && (oldest == null || book.InertSince < oldest.InertSince)) oldest = book;
                }
                if (oldest == null) return null;
                oldest.ResetToShelf();
                return oldest;
            }
            var positions = new List<Vector3>(candidates.Count);
            foreach (var book in candidates) positions.Add(book.transform.position);
            int index = rules.PickBook(positions, PlayerCenter());
            return index < 0 ? null : candidates[index];
        }

        private IEnumerator ThrowRoutine()
        {
            current = PickBook();
            if (current == null)
            {
                sequence = null;
                yield break;
            }
            current.Claim();
            if (castSound != null) AudioSource.PlayClipAtPoint(castSound, witch.position);

            // The arm extends from the witch's hand to the book.
            arm.enabled = true;
            float t = 0f;
            while (t < rules.ArmSeconds)
            {
                t += Time.deltaTime;
                SetArm(Mathf.Clamp01(t / Mathf.Max(rules.ArmSeconds, 0.01f)));
                yield return null;
            }
            SetArm(1f);

            // The slash arc sweeps while the book lights up.
            current.Glow();
            var toBook = (current.transform.position - Hand).normalized;
            arcPivot.position = Hand + toBook * arcDistance;
            arcPivot.rotation = Quaternion.LookRotation(toBook);
            arcTrail.Clear();
            arcTrail.emitting = true;
            t = 0f;
            var home = current.transform.position;
            while (t < rules.SweepSeconds)
            {
                t += Time.deltaTime;
                float f = Mathf.Clamp01(t / Mathf.Max(rules.SweepSeconds, 0.01f));
                arcPivot.rotation = Quaternion.LookRotation(toBook) * Quaternion.Euler(0f, 0f, Mathf.Lerp(-arcSweepDegrees * 0.5f, arcSweepDegrees * 0.5f, f));
                SetArm(1f);
                yield return null;
            }
            arcTrail.emitting = false;

            // The glowing hang is the telegraph the player reads before release.
            t = 0f;
            while (t < rules.TelegraphSeconds)
            {
                t += Time.deltaTime;
                float f = Mathf.Clamp01(t / Mathf.Max(rules.TelegraphSeconds, 0.01f));
                current.transform.position = home + Vector3.up * (hangRiseMeters * f);
                SetArm(1f);
                yield return null;
            }

            // Release at where the player is now; the book does not home after this.
            arm.enabled = false;
            current.Launch(PlayerCenter(), rules, receiver, PlayerCenter, () => Phase2);
            current = null;
            sequence = null;
        }

        private void SetArm(float extend)
        {
            if (current == null || witch == null) return;
            var hand = Hand;
            arm.SetPosition(0, hand);
            arm.SetPosition(1, Vector3.Lerp(hand, current.transform.position, extend));
        }

        private void CancelSequence()
        {
            if (sequence != null) StopCoroutine(sequence);
            sequence = null;
            arm.enabled = false;
            arcTrail.emitting = false;
            if (current != null && current.State == ThrowableBook.BookState.Claimed) current.ResetToShelf();
            current = null;
        }
    }
}
