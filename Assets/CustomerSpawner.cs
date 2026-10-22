using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Video;

public class CustomerSpawner : MonoBehaviour
{
    [SerializeField] List<GameObject> customerPrefabs;
    [SerializeField] List<GameObject> linePositions;
    [SerializeField] List<CustomerBehaviour> activeCustomers;
    [SerializeField] GameObject witch;
    int customerIndex = -1;
    private void Start() {
        NewCustomer();
    }
    public void NewCustomer()
    {
        customerIndex++;
        if(customerIndex >= customerPrefabs.Count)
        {
            return;
        }
        var customer = customerPrefabs[customerIndex];
        print(customer.name);
        customer.gameObject.SetActive(true);
        customer.transform.position = transform.position;
        activeCustomers.Add(customer.GetComponent<CustomerBehaviour>());
        CustomerBehaviour customerBehaviour = customer.GetComponent<CustomerBehaviour>();
        customerBehaviour.lineIndex = customerIndex;
        print(customerBehaviour.lineIndex);
        print(linePositions[customerBehaviour.lineIndex].transform);
        customerBehaviour.agent.SetDestination(linePositions[customerBehaviour.lineIndex].transform.position);
        customerBehaviour.agent.speed = 6;
    }

    public void UpdateLinePosition()
    {
        print("Moving forward in line");
        foreach (CustomerBehaviour customerBehaviour in activeCustomers)
        {
            print(customerBehaviour.gameObject.name + "moved forward in line");
            customerBehaviour.lineIndex--;
            if (customerBehaviour.lineIndex >= 0)
            {
                customerBehaviour.SetTarget(linePositions[customerBehaviour.lineIndex].transform);
            }
        }
    }
    
    public void WitchComes()
    {
        foreach(GameObject customer in customerPrefabs)
        {
            customer.SetActive(false);
        }
        CustomerBehaviour customerBehaviour = witch.GetComponent<CustomerBehaviour>();
        customerBehaviour.agent.SetDestination(linePositions[customerBehaviour.lineIndex].transform.position);
        customerBehaviour.agent.speed = 6;
    }


}
