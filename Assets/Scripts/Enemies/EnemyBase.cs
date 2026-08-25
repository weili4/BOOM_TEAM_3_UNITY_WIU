using System.Collections;
using UnityEngine;
using Pathfinding;

[RequireComponent(typeof(Damageable))]
public abstract class EnemyBase : MonoBehaviour
{
    [Header("configuration")]
    public EnemyData enemyData;
    [SerializeField] protected GameObject healthBarPrefab;
    protected WorldHealthBar healthBar;

    [Header("hitstun settings")]
    [SerializeField] protected float hitstunDuration = 0.12f;
    [SerializeField] protected bool isStunImmune = false; // set to true during unstoppable attacks

    protected Damageable damageable;
    protected Animator animator;
    protected Transform playerTarget;
    protected AIDestinationSetter aiDestSetter;
    protected Rigidbody2D rb;
    protected bool isDead = false;
    protected bool isStunned = false;
    protected Coroutine hitstunRoutine;

    public bool IsStunned => isStunned;

    protected virtual void Awake()
    {
        damageable = GetComponent<Damageable>();
        animator = GetComponent<Animator>();
        aiDestSetter = GetComponent<AIDestinationSetter>();
        rb = GetComponent<Rigidbody2D>();
    }

    protected virtual void OnEnable()
    {
        PartyManager.OnLeaderSwapped += HandleLeaderSwapped;
    }

    protected virtual void OnDisable()
    {
        PartyManager.OnLeaderSwapped -= HandleLeaderSwapped;
    }

    protected virtual void Start()
    {
        IgnorePartyCollisions();
        UpdatePlayerTarget();

        if (!CompareTag("Player") && healthBarPrefab != null && damageable != null)
        {
            GameObject barObj = Instantiate(healthBarPrefab, transform.position, Quaternion.identity);
            healthBar = barObj.GetComponent<WorldHealthBar>();
            if (healthBar != null)
            {
                healthBar.Initialize(transform);
                damageable.onHealthChanged.AddListener(healthBar.UpdateHealth);
            }
        }
    }

    protected virtual void Update()
    {
        if (isDead) return;

        if (playerTarget == null || playerTarget.CompareTag("Ally"))
        {
            UpdatePlayerTarget();
        }

        if (damageable != null && damageable.CurrentHealth <= 0)
        {
            Die();
        }
    }

    private void HandleLeaderSwapped(int oldLeaderIdx, int newLeaderIdx)
    {
        UpdatePlayerTarget();
    }

    protected virtual void UpdatePlayerTarget()
    {
        if (PartyManager.Instance != null && PartyManager.Instance.ActivePlayerObj != null)
        {
            playerTarget = PartyManager.Instance.ActivePlayerObj.transform;
        }
        else
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTarget = playerObj.transform;
            }
        }

        if (aiDestSetter != null && playerTarget != null)
        {
            aiDestSetter.target = playerTarget;
        }
    }

    // called when enemy takes damage to interrupt attacks and apply hitstun
    public virtual void ApplyHitstun(Vector2 knockbackDir, float knockbackForce = 4.0f)
    {
        if (isDead || isStunImmune) return;

        if (rb != null)
        {
            rb.linearVelocity = new Vector2(knockbackDir.x * knockbackForce, knockbackForce * 0.4f);
        }

        if (hitstunRoutine != null) StopCoroutine(hitstunRoutine);
        hitstunRoutine = StartCoroutine(HitstunRoutine());
    }

    protected virtual IEnumerator HitstunRoutine()
    {
        isStunned = true;
        InterruptActiveAttack();

        yield return new WaitForSeconds(hitstunDuration);

        isStunned = false;
        hitstunRoutine = null;
    }

    // subclasses override this to cancel active attack animations/coroutines
    protected virtual void InterruptActiveAttack() { }

    // optimized: ignores collisions directly from partymanager without expensive scene hierarchy searches
    protected void IgnorePartyCollisions()
    {
        Collider2D myCol = GetComponent<Collider2D>();
        if (myCol == null || PartyManager.Instance == null) return;

        foreach (var member in PartyManager.Instance.partyMembers)
        {
            if (member.spawnedInstance != null)
            {
                var colliders = member.spawnedInstance.GetComponentsInChildren<Collider2D>();
                foreach (var c in colliders)
                {
                    if (c != null)
                    {
                        Physics2D.IgnoreCollision(myCol, c, true);
                    }
                }
            }
        }
    }

    protected virtual void FlipTowards(Vector3 targetPos)
    {
        float diffX = targetPos.x - transform.position.x;
        if (diffX >= 0.05f)
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        else if (diffX <= -0.05f)
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
    }

    protected virtual void Die()
    {
        if (isDead) return;
        isDead = true;

        if (enemyData != null && enemyData.deathSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(enemyData.deathSound, transform.position);

        if (enemyData != null && enemyData.deathVFX != null)
            Instantiate(enemyData.deathVFX, transform.position, Quaternion.identity);

        if (animator != null)
            animator.SetTrigger("Die");

        /*===================================================================================================================*/
        if (TryGetComponent<EnemyLootDrop_Behaviour>(out EnemyLootDrop_Behaviour EnemyLootDrop))
            EnemyLootDrop.DoDropLoot();
        /*===================================================================================================================*/

        StartCoroutine(FadeOutAndDestroy());
    }

    protected IEnumerator FadeOutAndDestroy()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            float alpha = 1f;
            while (alpha > 0f)
            {
                alpha -= Time.deltaTime * 2f;
                Color c = sr.color;
                c.a = alpha;
                sr.color = c;
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
        }

        Destroy(gameObject);
    }
}