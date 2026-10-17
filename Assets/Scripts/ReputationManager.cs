
using System;
using UnityEngine;

public class ReputationManager : MonoBehaviour
{
    [Header("Reputation Settings")]
    [SerializeField] private int startingReputation = 100;
    [SerializeField] private int minimumReputation = 0;
    [SerializeField] private int maximumReputation = 100;

    [Header("Reputation Changes")]
    [SerializeField] private int correctRefundReward = 1;
    [SerializeField] private int unfairDenialPenalty = 10;
    [SerializeField] private int incorrectRefundPenalty = 0;
    [SerializeField] private int correctDenialReward = 0;

    public int CurrentReputation { get; private set; }

    // Other systems, including UI, can listen for changes.
    public event Action<int> OnReputationChanged;

    private void Awake()
    {
        CurrentReputation = Mathf.Clamp(
            startingReputation,
            minimumReputation,
            maximumReputation
        );
    }

    public void ProcessRefund(bool eligible, bool approved)
    {
        int change;

        if (eligible && approved)
        {
            change = correctRefundReward;
        }
        else if (eligible && !approved)
        {
            change = -unfairDenialPenalty;
        }
        else if (!eligible && approved)
        {
            change = -incorrectRefundPenalty;
        }
        else
        {
            change = correctDenialReward;
        }

        ChangeReputation(change);
    }

    private void ChangeReputation(int amount)
    {
        int newReputation = Mathf.Clamp(
            CurrentReputation + amount,
            minimumReputation,
            maximumReputation
        );

        if (newReputation == CurrentReputation)
            return;

        CurrentReputation = newReputation;

        OnReputationChanged?.Invoke(CurrentReputation);

        Debug.Log($"Reputation: {CurrentReputation}");
    }
}
