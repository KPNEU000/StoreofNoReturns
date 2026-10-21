using UnityEngine;
using System;
using UnityEngine.UI;

public class FrontdeskTrigger : MonoBehaviour
{
    public Action CustomerInService;

    [SerializeField]
    private MoneyManager gameManager;
    [SerializeField] Image frontSprite;
    [SerializeField] Image backSprite;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerEnter(Collider col)
    {
        Debug.Log("customer in service");
        if(col.transform.CompareTag("Customer"))
        {
            col.GetComponent<CustomerBehaviour>().StopWalking();
            CustomerInService?.Invoke();
            gameManager.GetCurrentCustomer(col.GetComponent<CustomerScript>());
            gameManager.GetCurrentCostume(col.GetComponent<CustomerScript>().getCostume());
            frontSprite.sprite = col.GetComponent<CustomerScript>().getCostume().getFrontSprite();
            backSprite.sprite = col.GetComponent<CustomerScript>().getCostume().getBackSprite();
        }
    } 
}
