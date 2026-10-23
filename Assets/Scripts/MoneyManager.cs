using TMPro;
using UnityEngine;
using System;
using BossFight;
public class MoneyManager : MonoBehaviour
{
    [Header("GameData")]
    [SerializeField]
    private float startingMoney = 1000;
    [SerializeField]
    private float heatingCutOff = 100; // unused
    [SerializeField]
    private GameObject heatingCutOffAlert; // unused
    [SerializeField]
    private float lightingCutOff = 500; // unused (StoreLightingController owns lighting)
    [SerializeField]
    private GameObject lightingCutOffAlert; // unused

    [Header("Reputation")]
    [SerializeField]
    private ReputationManager reputationManager;
    
    bool lightingCut = false; // unused
    bool gameOver = false;
    CustomerScript customer;
    CustomerBehaviour customerBehaviour;
    CostumeScriptData costume;
    int reputationAtInteractionStart;

    private float currentMoney;

    public float CurrentMoney => currentMoney;
    public event Action<float> OnMoneyChanged;

    [Header("UI")]
    [SerializeField]
    private TMP_Text currentMoneyValue;
    [SerializeField]
    private Light[] lights;
    [SerializeField]
    private GameObject flashlight;
    [SerializeField]
    private PlayerMovement playerMovement;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        AngeredCustomerRegistry.Reset();
        lights = GameObject.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.InstanceID);
        currentMoney = startingMoney;
        currentMoneyValue.text = "$"+ currentMoney.ToString();
        OnMoneyChanged?.Invoke(currentMoney);
    }

    public void SubtractMoney(float amount)
    {
        SetMoney(currentMoney - amount);
    }

    // Used by the boss debug jump (P) to start from the test money value.
    public void SetMoney(float amount)
    {
        currentMoney = amount;
        OnMoneyChanged?.Invoke(currentMoney);
        if (currentMoney <= 0 && !gameOver)
        {
            gameOver = true;
            // The Game Over screen shows the reputation the run ended on.
            BossFight.RunScore.Set(0f, reputationManager != null ? reputationManager.CurrentReputation : 0);
            if (SceneLoadingManager.Instance != null)
            {
                SceneLoadingManager.Instance.LoadSceneByName("Game Over Screen");
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("Game Over Screen");
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (currentMoney.ToString() != currentMoneyValue.text)
        {
            currentMoneyValue.text = "$" + currentMoney.ToString();
        }
    }

    public void GetCurrentCustomer(CustomerScript currentCustomer)
    {
        customer = currentCustomer;
        customerBehaviour = customer.GetComponent<CustomerBehaviour>();
        reputationAtInteractionStart = reputationManager != null ? reputationManager.CurrentReputation : 0;
    }

    public void GetCurrentCostume(CostumeScriptData currentCustomer)
    {
        costume = currentCustomer;
    }

    public void ReturnCostumeAction(bool confirmRefund)
    {
        //replace this code to get the current costume we are working with
        if (costume == null)
        {
            Debug.LogWarning("No Costume assigned");
            return;
        }

        //check return
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

        bool isWitch = customerBehaviour.gameObject.name == "Witch";
        // The Witch is the boss, not one of the monsters she brings.
        if (!isWitch && reputationManager != null)
        {
            AngeredCustomerRegistry.Record(customerBehaviour.gameObject.name, reputationAtInteractionStart, reputationManager.CurrentReputation);
        }

        if(isWitch)
        {
            //Start boss battle
            print("boss battle starts");
            var director = FindFirstObjectByType<BossDirector>();
            if (director != null)
            {
                director.StartFight(AngeredCustomerRegistry.Count);
            }
            else
            {
                customerBehaviour.gameObject.GetComponent<Enemy>().enabled = true;
                customerBehaviour.gameObject.tag = "Enemy";
                playerMovement.enableMovement();
            }
        }
    }

    public void ReasoningCheck(string reasonSelected)
    {
        if (customer != null)
        {
            if (Enum.TryParse(reasonSelected, out invalidReturnReason reason))
            {
                customer.CheckReasoning(reason);
            }
            else
            {
                Debug.LogError($"Invalid reason string: {reasonSelected}");
            }
        }
    }

}
