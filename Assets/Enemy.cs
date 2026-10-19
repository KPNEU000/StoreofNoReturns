using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    Animator animator;
    NavMeshAgent agent;
    ParticleSystem ps;
    SkinnedMeshRenderer smr;
    public float hp;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        ps = GetComponentInChildren<ParticleSystem>();
        smr = GetComponentInChildren<SkinnedMeshRenderer>();
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
        StartCoroutine("HurtAnimation");
    }

    public IEnumerator HurtAnimation()
    {
        smr.material.color = Color.red;
        ps.Emit(30);
        yield return new WaitForSeconds(0.2f);
        smr.material.color = Color.white;
    }

    public void Die() //Only used within an animation, don't call this method in script
    {
        Destroy(this);
    }
}
