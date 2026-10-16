using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MoneyManager : MonoBehaviour
{
    [Header("GameData")]
    [SerializeField]
    private float startingMoney = 1000;
    [SerializeField]
    private float heatingCutOff = 100;
    [SerializeField]
    private float lightingCutOff = 500;
    bool lightingCut = false;
    bool heatingCut = false;

    [SerializeField]
    private CostumeScriptData[] outfitsToReturn;

    private float currentMoney;

    [Header("UI")]
    [SerializeField]
    private TMP_Text currentMoneyValue;
    [SerializeField]
    private List<Light> lights = new List<Light>();
    [SerializeField]
    private GameObject flashlight;
    [SerializeField]
    private PlayerMovement playerMovement;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
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
        }

        if (currentMoney <= heatingCutOff && !heatingCut)
        {
            heatingCut = true;
            playerMovement.speed = playerMovement.speed * 0.5f;
        }
    }

    void returnCostumeAction(int index)
    {
        currentMoney -= outfitsToReturn[index].getReturnValue();
    }
}
