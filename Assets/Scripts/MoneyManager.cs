using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    bool lightingCut = false;
    bool heatingCut = false;

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

    [SerializeField]
    private Slider star1;
    [SerializeField]
    private Slider star2;
    [SerializeField]
    private Slider star3;
    [SerializeField]
    private Slider star4;
    [SerializeField]
    private Slider star5;


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
            lightingCutOffAlert.SetActive(true);
        }

        if (currentMoney <= heatingCutOff && !heatingCut)
        {
            heatingCut = true;
            playerMovement.speed = playerMovement.speed * 0.5f;
            heatingCutOffAlert.SetActive(true);
        }
    }

    void returnCostumeAction(CostumeScriptData costume)
    {
        currentMoney -= costume.getReturnValue();
    }

    public void ratingDecreaser(float amount)
    {
        if (star5.value > star5.minValue)
        {
            if (star5.value > amount)
            {
                star5.value -= amount;
            }
            else
            {
                amount -= star5.value;
                star5.value = star5.minValue;
            }
        }
        if (star4.value > star4.minValue && amount > 0)
        {
            if (star4.value > amount)
            {
                star4.value -= amount;
            }
            else
            {
                amount -= star4.value;
                star4.value = star4.minValue;
            }
        }
        if (star3.value > star3.minValue && amount > 0)
        {
            if (star3.value > amount)
            {
                star3.value -= amount;
            }
            else
            {
                amount -= star3.value;
                star3.value = star3.minValue;
            }
        }
        if (star2.value > star2.minValue && amount > 0)
        {
            if (star2.value > amount)
            {
                star2.value -= amount;
            }
            else
            {
                amount -= star2.value;
                star2.value = star2.minValue;
            }
        }
        if (star1.value > star1.minValue && amount > 0)
        {
            if (star1.value > amount)
            {
                star1.value -= amount;
            }
            else
            {
                star1.value = star1.minValue;
                amount = 0;
            }
        }
        else
        {
            //damn u suck at ur job
        }
    }

    public void ratingIncreaser(float amount)
    {
        if (star1.value < star1.maxValue)
        {
            if (star1.value + amount > star1.maxValue)
            {
                amount -= (star1.maxValue - star1.value);
                star1.value = star1.maxValue;
            }
            else
            {
                star1.value += amount;
            }
        }
        if (star2.value < star2.maxValue && amount > 0)
        {
            if (star2.value + amount > star2.maxValue)
            {
                amount -= (star2.maxValue - star2.value);
                star2.value = star2.maxValue;
            }
            else
            {
                star2.value += amount;
            }
        }
        if (star3.value < star3.maxValue && amount > 0)
        {
            if (star3.value + amount > star3.maxValue)
            {
                amount -= (star3.maxValue - star3.value);
                star3.value = star3.maxValue;
            }
            else
            {
                star3.value += amount;
            }
        }
        if (star4.value < star4.maxValue && amount > 0)
        {
            if (star4.value + amount > star4.maxValue)
            {
                amount -= (star4.maxValue - star4.value);
                star4.value = star4.maxValue;
            }
            else
            {
                star4.value += amount;
            }
        }
        if (star5.value < star5.maxValue && amount > 0)
        {
            if (star5.value + amount > star5.maxValue)
            {
                amount = 0;
                star5.value = star5.maxValue;
            }
            else
            {
                star5.value += amount;
            }
        }
        else
        {
            //damn try hard
        }
    }
}
