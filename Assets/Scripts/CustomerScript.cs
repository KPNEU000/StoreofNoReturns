using System.Linq;
using UnityEngine;

public class CustomerScript : MonoBehaviour
{
    [SerializeField]
    private ReputationManager repManager;
    
    [SerializeField]
    private CostumeScriptData customerCostume;

    [SerializeField]
    private string[] dialogue;

    [SerializeField]
    private Ink.UnityIntegration.InkFile inkFile;

    [SerializeField]
    private bool genuineReason;

    [SerializeField]
    private int wrongReasonAmount = -2;

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
           
            //go to sympatetic dialogue sentence for possible explanation of refund
            if (reason == invalidReturnReason.Stained || reason == invalidReturnReason.MissingAccessories || reason == invalidReturnReason.SlightlyDamaged)
            {
                if (genuineReason)
                {
                    customerCostume.removeReason(reason);
                }
                else
                {
                    //scummy reason
                }
            }
            else
            {
                //go to scummy return reason
            }

        }
        else
        {
            //call reputation manager and provide dialogue calling clerk a dick
            repManager.ChangeReputation(wrongReasonAmount);
        }
    }

    public CostumeScriptData getCostume()
    {
        return customerCostume;
    }

    public Ink.UnityIntegration.InkFile GetInkFile()
    {
        return inkFile;
    }
}
