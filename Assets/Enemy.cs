using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    Animator animator;
    NavMeshAgent agent;
    public float hp;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if(hp <= 0)
        {
            animator.Play("Death");
        }


        //Animation Control
        if (!agent.isStopped)
        {
            animator.Play("Run");
        }
    }

    public void Attack()
    {
        animator.Play("Attack");
    }

    public void TakeDamage(float damage)
    {
        hp -= damage;
        animator.Play("Hurt");
    }

    public void Die() //Only used within an animation, don't call this method in script
    {
        Destroy(this);
    }
}
