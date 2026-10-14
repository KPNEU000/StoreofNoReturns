using TMPro;
using UnityEngine;

public class MoneyManager : MonoBehaviour
{
    [Header("GameData")]
    [SerializeField]
    private float startingMoney = 1000;

    [SerializeField]
    private CostumeScriptData[] outfitsToReturn;

    private float currentMoney;

    [Header("UI")]
    [SerializeField]
    private TMP_Text currentMoneyValue;


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
    }

    void returnCostumeAction(int index)
    {
        currentMoney -= outfitsToReturn[index].getReturnValue();
    }
}
