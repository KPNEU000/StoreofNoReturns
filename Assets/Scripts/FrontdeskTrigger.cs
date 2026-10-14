using UnityEngine;
using System;

public class FrontdeskTrigger : MonoBehaviour
{
    public Action CustomerInService;
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
        if(col.transform.CompareTag("Customer"))
        {
            CustomerInService?.Invoke();
        Debug.Log("customer in service");
        }
    } 
}
