using UnityEngine;

[CreateAssetMenu(fileName = "NewCustomerData", menuName = "CustomerData")]
public class CustomerScriptData : ScriptableObject
{
    [SerializeField] private string name = "";

    [SerializeField] private string[] dialogues;

    [SerializeField] private CostumeScriptData returnProduct;

}
