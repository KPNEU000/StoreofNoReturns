
using UnityEngine;

public class ReputationTester : MonoBehaviour
{
    [SerializeField] private ReputationManager reputationManager;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            // Eligible customer receives refund.
            reputationManager.ProcessRefund(true, true);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            // Eligible customer is denied refund.
            reputationManager.ProcessRefund(true, false);
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            // Ineligible customer receives refund.
            reputationManager.ProcessRefund(false, true);
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            // Ineligible customer is denied refund.
            reputationManager.ProcessRefund(false, false);
        }
    }
}
