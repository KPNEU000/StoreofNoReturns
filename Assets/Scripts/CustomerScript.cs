using System.Linq;
using UnityEngine;

public class CustomerScript : MonoBehaviour
{
    [SerializeField]
    private CostumeScriptData customerCostume;

    [SerializeField]
    private string[] dialogue;
    void Start()
    {
        
    }

    void Update()
    {
        
    }

    //provide the select option that we are doing
    public void CheckReasoning(invalidReturnReason reason)
    {
        if (customerCostume.getReasons().Contains(reason))
        {
            //go to next dialogue sentence for possible explanation of refund
        }
        else
        {
            //call reputation manager and provide dialogue calling clerk a dick
        }
    }

    public CostumeScriptData getCostume()
    {
        return customerCostume;
    }
}
