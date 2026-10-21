using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CustomerBehaviour : MonoBehaviour
{
    public List<Transform> linePositions;
    [SerializeField] public NavMeshAgent agent;
    Animator animator;
    bool Walking = true;
    bool Talking = false;
    GameObject player;
    Animation barcodeAnim;
    CustomerSpawner customerSpawner;

    [SerializeField] private Transform target;
    [SerializeField] private Vector3 exitTarget;
    [SerializeField] public int lineIndex;
    [SerializeField] float remainingDistance;
    [SerializeField] float velocity;
    public bool done = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        exitTarget = transform.position;
        agent = GetComponent<NavMeshAgent>();
        barcodeAnim = GameObject.Find("BarcodeReader").GetComponent<Animation>();
        player = GameObject.FindGameObjectWithTag("Player");
        player = GameObject.Find("Player");
        //agent.SetDestination(target.position);
        animator = GetComponent<Animator>();
        //gameObject.SetActive(false);
        customerSpawner = GameObject.FindFirstObjectByType<CustomerSpawner>();
    }

    public void SetLineIndex(int index)
    {
        lineIndex = index;
    }

    public void SetTarget(Transform targetTransform)
    {
        //
        agent.SetDestination(targetTransform.position);
    }

    // Update is called once per frame
    void Update()
    {
        if (agent.velocity.magnitude > 0)
        {
            animator.SetBool("Walking", true);
        }
        else
        {
            animator.SetBool("Walking", false);
        }
        if(done && agent.remainingDistance <= 1)
        {
            gameObject.SetActive(false);
        }
    }

    public void Leave()
    {
        done = true;
        print("Customer Leaving");
        agent.SetDestination(exitTarget);
        Walking = true;
    }

    public void Respond(bool Positively)
    {
        if (Positively)
        {
            animator.Play("Excited");
        }
        else
        {
            animator.Play("Dissapointed");
        }
    }

    public void StopWalking()
    {
        Walking = false;
        //**Play handing costume animation chain
        Talking = true;
        animator.Play("HandCostume");
    }

    public void BarcadeScan()
    {
        barcodeAnim.Play();
    }

    void LateUpdate()
    {
        if (!Walking)
        {
            transform.LookAt(player.transform.position);
        }

        velocity = agent.velocity.magnitude;
        remainingDistance = agent.remainingDistance;
    }
}
