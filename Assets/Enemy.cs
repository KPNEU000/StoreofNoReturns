using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    public enum EnemyState
    {
        Run,
        Idle,
        Attack,
        Death,
        Vulnerable 
    }
    Animator animator;
    NavMeshAgent agent;
    ParticleSystem ps;
    SkinnedMeshRenderer smr;

    Transform player;

    EnemyState state;
    public float hp;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        state = EnemyState.Idle;
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        ps = GetComponentInChildren<ParticleSystem>();
        smr = GetComponentInChildren<SkinnedMeshRenderer>();

        PlayerMovement.PlayerMoved += ResetNavTarget;
    }

    void OnEnable()
    {
        player = FindFirstObjectByType<PlayerMovement>().transform;
    }

    void ResetNavTarget()
    {
        agent.SetDestination(player.transform.position);
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
