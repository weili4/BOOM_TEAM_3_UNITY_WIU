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

    protected Damageable damageable;
    protected Animator animator;
    protected Transform playerTarget;
    protected AIDestinationSetter aiDestSetter;
    protected bool isDead = false;

    protected virtual void Awake()
    {
        damageable = GetComponent<Damageable>();
        animator = GetComponent<Animator>();
        aiDestSetter = GetComponent<AIDestinationSetter>();
    }

    protected virtual void OnEnable()
    {
        // listen to leader swap events to switch targets instantly
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

    // makes sure enemies never physically block or push players or followers
    protected void IgnorePartyCollisions()
    {
        Collider2D myCol = GetComponent<Collider2D>();
        if (myCol == null) return;

        // ignore active player
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (var p in players)
        {
            foreach (var c in p.GetComponentsInChildren<Collider2D>())
            {
                Physics2D.IgnoreCollision(myCol, c, true);
            }
        }

        // ignore benched followers
        GameObject[] allies = GameObject.FindGameObjectsWithTag("Ally");
        foreach (var a in allies)
        {
            foreach (var c in a.GetComponentsInChildren<Collider2D>())
            {
                Physics2D.IgnoreCollision(myCol, c, true);
            }
        }
    }

    protected virtual void Update()
    {
        if (isDead) return;

        // failsafe target check
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

    // finds the current active leader with tag Player
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

        // update pathfinding destination if using astar
        if (aiDestSetter != null && playerTarget != null)
        {
            aiDestSetter.target = playerTarget;
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