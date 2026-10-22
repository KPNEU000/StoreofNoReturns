using TMPro;
using UnityEngine;
using System;
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
    private float lightingCutOff = 500;
    [SerializeField]
    private GameObject lightingCutOffAlert;

    [Header("Reputation")]
    [SerializeField]
    private ReputationManager reputationManager;
    
    bool lightingCut = false;
    bool gameOver = false;
    CustomerScript customer;
    CustomerBehaviour customerBehaviour;
    CostumeScriptData costume;

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
        lights = GameObject.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.InstanceID);
        currentMoney = startingMoney;
        currentMoneyValue.text = "$"+ currentMoney.ToString();
        OnMoneyChanged?.Invoke(currentMoney);
    }

    public void SubtractMoney(float amount)
    {
        currentMoney -= amount;
        OnMoneyChanged?.Invoke(currentMoney);
        if (currentMoney <= 0 && !gameOver)
        {
            gameOver = true;
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

        if (currentMoney <= lightingCutOff && !lightingCut)
        {
            lightingCut = true;
            flashlight.SetActive(true);
            foreach (Light light in lights)
            {
                light.enabled = false;
            }
            lightingCutOffAlert.SetActive(true);
        }
    }

    public void GetCurrentCustomer(CustomerScript currentCustomer)
    {
        customer = currentCustomer;
        customerBehaviour = customer.GetComponent<CustomerBehaviour>();
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

        if(customerBehaviour.gameObject.name == "Witch")
        {
            //Start boss battle
            print("boss battle starts");
            customerBehaviour.gameObject.GetComponent<Enemy>().enabled = true;
            customerBehaviour.gameObject.tag = "Enemy";
            playerMovement.enableMovement();
        }
    }

    public void ReasoningCheck(string reasonSelected)
    {
        if (customer == null)
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
