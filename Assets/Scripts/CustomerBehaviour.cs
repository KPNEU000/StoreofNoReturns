using System;
using UnityEngine;
using UnityEngine.AI;

public class CustomerBehaviour : MonoBehaviour
{

    private NavMeshAgent agent;

    [SerializeField] private Transform target;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        agent.SetDestination(target.position);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
