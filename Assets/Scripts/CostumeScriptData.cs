using System;
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
    private List<invalidReturnReason> reasons;

    [SerializeField]
    private Sprite computerViewSpriteFront;
    [SerializeField]
    private Sprite computerViewSpriteBack;
    [SerializeField]
    private string accessories;

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

    public Sprite getFrontSprite()
    {
        return computerViewSpriteFront;
    }

    public Sprite getBackSprite()
    {
        return computerViewSpriteBack;
    }

    public string getAccessories()
    {
        return accessories;
    }

    public string getDateofPurchase()
    {
        return dateOfPurchase;
    }

    public string getName()
    {
        return costumeName;
    }
}

