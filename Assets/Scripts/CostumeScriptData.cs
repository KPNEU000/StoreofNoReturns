using UnityEngine;

public enum invalidReturnReason
{
    TooLateToReturn,
    RemovedTags,
    Stained,
    SlightlyDamaged,
    NoReceipt,
    MissingAccessories,
    WrongCostume
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
    private invalidReturnReason[] reasons;

    public float getReturnValue()
    {
        return returnValue;
    }
}
