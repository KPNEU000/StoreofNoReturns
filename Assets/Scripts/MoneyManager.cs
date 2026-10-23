
using TMPro;
using UnityEngine;
using System;
using BossFight;

public class MoneyManager : MonoBehaviour
{
    [Header("GameData")]
    [SerializeField] private float startingMoney = 1000;
    [SerializeField] private float heatingCutOff = 100;
    [SerializeField] private GameObject heatingCutOffAlert;
    [SerializeField] private float lightingCutOff = 500;
    [SerializeField] private GameObject lightingCutOffAlert;

    [Header("Reputation")]
    [SerializeField] private ReputationManager reputationManager;

    bool lightingCut = false;
    bool gameOver = false;

    CustomerScript customer;
    CustomerBehaviour customerBehaviour;
    CostumeScriptData costume;
    int reputationAtInteractionStart;

    private float currentMoney;

    public float CurrentMoney => currentMoney;
    public event Action<float> OnMoneyChanged;

    [Header("UI")]
    [SerializeField] private TMP_Text currentMoneyValue;
    [SerializeField] private Light[] lights;
    [SerializeField] private GameObject flashlight;
    [SerializeField] private PlayerMovement playerMovement;

    void Start()
    {
        if (!BossRunState.HasData)
        {
            AngeredCustomerRegistry.Reset();
        }

        lights = GameObject.FindObjectsByType<Light>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.InstanceID
        );

        currentMoney = startingMoney;

        RefreshMoneyUI();
        OnMoneyChanged?.Invoke(currentMoney);
    }

    public void SubtractMoney(float amount)
    {
        SetMoney(currentMoney - amount);
    }

    public void SetMoney(float amount)
    {
        currentMoney = amount;

        RefreshMoneyUI();
        OnMoneyChanged?.Invoke(currentMoney);

        if (currentMoney <= 0 && !gameOver)
        {
            gameOver = true;

            StoreofNoReturnsSfx.Post(
                StoreofNoReturnsSfx.GameOver,
                gameObject
            );

            BossFight.RunScore.Set(
                0f,
                reputationManager != null
                    ? reputationManager.CurrentReputation
                    : 0
            );

            if (SceneLoadingManager.Instance != null)
            {
                SceneLoadingManager.Instance.LoadSceneByName(
                    "Game Over Screen"
                );
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    "Game Over Screen"
                );
            }
        }
    }

    private void RefreshMoneyUI()
    {
        if (currentMoneyValue != null)
        {
            currentMoneyValue.text = "$" + currentMoney.ToString("0");
        }
    }

    void Update()
    {
        RefreshMoneyUI();
    }

    public void GetCurrentCustomer(CustomerScript currentCustomer)
    {
        customer = currentCustomer;
        customerBehaviour = customer.GetComponent<CustomerBehaviour>();

        reputationAtInteractionStart =
            reputationManager != null
                ? reputationManager.CurrentReputation
                : 0;
    }

    public void GetCurrentCostume(CostumeScriptData currentCustomer)
    {
        costume = currentCustomer;
    }

    public void ReturnCostumeAction(bool confirmRefund)
    {
        if (costume == null)
        {
            Debug.LogWarning("No Costume assigned");
            return;
        }

        if (confirmRefund)
        {
            if (costume.getReasons().Count == 0)
            {
                reputationManager.ProcessRefund(true, true);
            }
            else
            {
                reputationManager.ProcessRefund(false, true);
            }

            SubtractMoney(costume.getReturnValue());
            customerBehaviour.Respond(true);
        }
        else
        {
            if (costume.getReasons().Count == 0)
            {
                reputationManager.ProcessRefund(true, false);
            }
            else
            {
                reputationManager.ProcessRefund(false, false);
            }

            customerBehaviour.Respond(false);
        }

        bool isWitch =
            customerBehaviour.gameObject.name == "Witch";

        // The Witch is the boss, not one of the monsters she brings.
        if (!isWitch && reputationManager != null)
        {
            AngeredCustomerRegistry.Record(
                customerBehaviour.gameObject.name,
                reputationAtInteractionStart,
                reputationManager.CurrentReputation
            );
        }
    }

    public void ReasoningCheck(string reasonSelected)
    {
        if (customer != null)
        {
            if (Enum.TryParse(
                reasonSelected,
                out invalidReturnReason reason
            ))
            {
                customer.CheckReasoning(reason);
            }
            else
            {
                Debug.LogError(
                    $"Invalid reason string: {reasonSelected}"
                );
            }
        }
    }
}
