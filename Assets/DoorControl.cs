using System;
using System.Collections;
using UnityEngine;

public class DoorControl : MonoBehaviour
{
    Animator animator;
    CustomerSpawner customerSpawner;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        animator = GetComponent<Animator>();
        customerSpawner = GameObject.FindFirstObjectByType<CustomerSpawner>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Customer"))
        {
            StartCoroutine("Open");
            if(other.GetComponent<CustomerBehaviour>().done)
            {
                customerSpawner.UpdateLinePosition();
                if(other.gameObject.name == "Kelly Darkson") //lmao
                {
                    customerSpawner.WitchComes(); 
                }
            }
        }
    }
    
    IEnumerator Open()
    {
        animator.Play("Open");
        yield return new WaitForSeconds(1f);
        animator.Play("Close");
    }
}
