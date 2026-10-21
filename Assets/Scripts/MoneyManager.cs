using TMPro;
using UnityEngine;
using System;
public class MoneyManager : MonoBehaviour
{
    [Header("GameData")]
    [SerializeField]
    private float startingMoney = 1000;
    [SerializeField]
    private float heatingCutOff = 100;
    [SerializeField]
    private GameObject heatingCutOffAlert;
    [SerializeField]
    private float lightingCutOff = 500;
    [SerializeField]
    private GameObject lightingCutOffAlert;

    [Header("Reputation")]
    [SerializeField]
    private ReputationManager reputationManager;
    
    bool lightingCut = false;
    bool heatingCut = false;
    CustomerScript customer;
    CostumeScriptData costume;

    private float currentMoney;

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

        if (currentMoney <= heatingCutOff && !heatingCut)
        {
            heatingCut = true;
            playerMovement.speed = playerMovement.speed * 0.5f;
            heatingCutOffAlert.SetActive(true);
        }

        if (currentMoney == 0)
        {
            FindFirstObjectByType<SceneLoadingManager>().LoadSceneByName("Game Over");
        }
    }

    public void GetCurrentCustomer(CustomerScript currentCustomer)
    {
        customer = currentCustomer;
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
            currentMoney -= costume.getReturnValue();
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
