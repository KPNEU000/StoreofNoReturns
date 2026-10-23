
using UnityEngine;
using System;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

public class FrontdeskTrigger : MonoBehaviour
{
    public Action CustomerInService;

    [SerializeField]
    private MoneyManager gameManager;
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] Image frontSprite;
    [SerializeField] Image backSprite;
    [SerializeField] TextMeshProUGUI dopText;
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI accessoriesText;
    [SerializeField] TextMeshProUGUI priceText;
    [SerializeField] InspectCostume inspectCostume;

    void Start()
    {

    }

    void Update()
    {

    }

    void OnTriggerEnter(Collider col)
    {
        if (col.GetComponent<CustomerBehaviour>() != null &&
            col.gameObject.name == "Witch")
        {
            Debug.Log("Witch detected. Skipping normal customer dialogue.");
            return;
        }

        Debug.Log("customer in service");

        if (col.transform.CompareTag("Customer"))
        {
            col.GetComponent<CustomerBehaviour>().StopWalking();
            StoreofNoReturnsSfx.Post(
                StoreofNoReturnsSfx.CustomerArrive,
                col.gameObject
            );
            CustomerInService?.Invoke();

            dialogueManager.StartDialogue(
                col.GetComponent<CustomerScript>().GetInkFile()
            );

            gameManager.GetCurrentCustomer(
                col.GetComponent<CustomerScript>()
            );

            gameManager.GetCurrentCostume(
                col.GetComponent<CustomerScript>().getCostume()
            );

            CostumeScriptData data =
                col.GetComponent<CustomerScript>().getCostume();

            frontSprite.sprite = data.getFrontSprite();
            backSprite.sprite = data.getBackSprite();
            dopText.text = data.getDateofPurchase();
            nameText.text = "Name: " + data.getName();
            accessoriesText.text = "Accessories: \n" + data.getAccessories();
            priceText.text = "$" + data.getReturnValue().ToString();

            inspectCostume.setActiveCostume(data.getName());
        }
    }
}
