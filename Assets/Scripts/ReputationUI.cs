
using UnityEngine;
using UnityEngine.UI;

public class ReputationUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ReputationManager reputationManager;
    [SerializeField] private Slider[] stars;

    private void OnEnable()
    {
        if (reputationManager != null)
        {
            reputationManager.OnReputationChanged += UpdateRating;
        }
    }

    private void Start()
    {
        if (reputationManager != null)
        {
            UpdateRating(reputationManager.CurrentReputation);
        }
    }

    private void OnDisable()
    {
        if (reputationManager != null)
        {
            reputationManager.OnReputationChanged -= UpdateRating;
        }
    }

    private void UpdateRating(int reputation)
    {
        float rating = Mathf.Clamp01(reputation / 100f) * stars.Length;

        for (int i = 0; i < stars.Length; i++)
        {
            if (stars[i] == null)
                continue;

            float fill = Mathf.Clamp01(rating - i);
            stars[i].SetValueWithoutNotify(fill);
        }
    }
}
