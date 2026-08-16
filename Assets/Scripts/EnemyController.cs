using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

// ended up not using this cause i have my own classes now for the enemies
public class EnemyController : MonoBehaviour
{
    public enum State
    {
        Idle,
        Attack,
        Chase
    };

    [Header("State Properties")]
    [SerializeField] private State initialState;
    private State currentState;
    [SerializeField] private Transform target;

    [Header("Attack Properties")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private GameObject projectile;
    [SerializeField] private float fireRate = 1f;
    private float timeLastShot = 0;

    [Header("Chase Properties")]
    [SerializeField] private float chaseRange = 5f;
    private Animator animator;
    private AIDestinationSetter aiDestSetter;

    void Awake()
    {
        animator = GetComponent<Animator>();
        aiDestSetter = GetComponent<AIDestinationSetter>();
    }

    void Start()
    {
        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) target = playerObj.transform;
        }

        ChangeState(initialState);
    }

    void Update()
    {
        if (target == null) return;

        if (currentState == State.Idle)
        {
            Idle();
        }
        else if (currentState == State.Attack)
        {
            Attack();
        }
        else if (currentState == State.Chase)
        {
            Chase();
        }
    }

    private void InitializeState(State current)
    {
        if (current == State.Attack)
        {
            timeLastShot = 0;
        }
        else if (current == State.Chase)
        {
            if (animator != null) animator.Play("Bat Move");
        }
    }

    private void EndState(State current)
    {
        if (current == State.Chase)
        {
            if (aiDestSetter != null) aiDestSetter.target = null;
        }
    }

    private void ChangeState(State next)
    {
        EndState(currentState);
        currentState = next;
        InitializeState(currentState);
    }

    private void Idle()
    {
        Flip();

        float dist = Vector3.Distance(target.position, transform.position);
        if (dist <= attackRange)
        {
            ChangeState(State.Attack);
        }
        else if (dist <= chaseRange)
        {
            ChangeState(State.Chase);
        }
    }

    private void Attack()
    {
        Flip();
        Fire(target);

        if (Vector3.Distance(target.position, transform.position) > attackRange)
        {
            ChangeState(State.Idle);
        }
    }

    private void Fire(Transform who)
    {
        timeLastShot += Time.deltaTime;
        if (timeLastShot > fireRate)
        {
            if (projectile != null)
            {
                GameObject spawnObject = Instantiate(projectile, transform.position, Quaternion.identity);
                var direction = (who.position - spawnObject.transform.position).normalized;
                if (spawnObject.TryGetComponent<Rigidbody2D>(out Rigidbody2D rb))
                {
                    rb.linearVelocity = direction * 10;
                }
            }

            timeLastShot = 0;
            if (animator != null) animator.Play("Bat Attack", -1, 0);
        }
    }

    private void Chase()
    {
        Flip();

        if (aiDestSetter != null && aiDestSetter.target == null)
            aiDestSetter.target = target;

        float dist = Vector3.Distance(target.position, transform.position);
        if (dist > chaseRange)
        {
            ChangeState(State.Idle);
        }
        else if (dist < attackRange)
        {
            ChangeState(State.Attack);
        }
    }

    void Flip()
    {
        if (target == null) return;

        float hDiff = (target.position - transform.position).x;
        if (hDiff >= 0.01f)
        {
            transform.localScale = new Vector3(-1.0f, 1.0f, 1.0f);
        }
        else if (hDiff <= -0.01f)
        {
            transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
        }
    }
}