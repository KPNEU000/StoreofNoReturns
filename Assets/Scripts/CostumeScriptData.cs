using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public enum invalidReturnReason
{
    TooLateToReturn,
    RemovedTags,
    Stained,
    SlightlyDamaged,
    NoReceipt,
    MissingAccessories,
    WrongCostume,
    Ripped
}

[CreateAssetMenu(fileName = "NewCostumeData", menuName = "CostumeData")]
public class CostumeScriptData : ScriptableObject
{
    [SerializeField]
    private string costumeName = "";

    [SerializeField]
    private float returnValue = 0;

    [SerializeField]
    private GameObject costumeModel;

    [SerializeField]
    private string dateOfPurchase = "";

    [SerializeField]
    private string conditionStatus;

    [SerializeField]
    private bool returnable = false;

    [SerializeField]
    private List<invalidReturnReason> reasons;

    public float getReturnValue()
    {
        return returnValue;
    }

    public List<invalidReturnReason> getReasons()
    {
        return reasons;
    }

    public void removeReason(invalidReturnReason clearedReason)
    {
        reasons.Remove(clearedReason);
    }
}
