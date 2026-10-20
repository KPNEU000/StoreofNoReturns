using System;
using UnityEngine;
using UnityEngine.AI;

public class CustomerBehaviour : MonoBehaviour
{

    private NavMeshAgent agent;
    Animator animator;
    bool Walking = true;
    GameObject player;
    Animation barcodeAnim;

    [SerializeField] private Transform target;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        barcodeAnim = GameObject.Find("BarcodeReader").GetComponent<Animation>();
        player = GameObject.FindGameObjectWithTag("Player");
        player = GameObject.Find("Player");
        agent.SetDestination(target.position);
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Walking)
        {
            animator.Play("Walking");
        }
    }

    public void StopWalking()
    {
        Walking = false;
        //**Play handing costume animation chain
        animator.Play("HandCostume");
    }

    public void BarcadeScan()
    {
        barcodeAnim.Play();
    }

    void LateUpdate()
    {
        if(!Walking)
        {
            transform.LookAt(player.transform.position);
        }
    }
}
