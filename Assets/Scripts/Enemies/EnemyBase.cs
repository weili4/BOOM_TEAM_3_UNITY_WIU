using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Damageable))]
public abstract class EnemyBase : MonoBehaviour
{
    // ENEMY BASE CLASS

    [Header("configuration")]
    public EnemyData enemyData;
    [SerializeField] protected GameObject healthBarPrefab;
    protected WorldHealthBar healthBar;

    protected Damageable damageable;
    protected Animator animator;
    protected Transform playerTarget;
    protected bool isDead = false;

    protected virtual void Awake()
    {
        damageable = GetComponent<Damageable>();
        animator = GetComponent<Animator>();
    }

    protected virtual void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            playerTarget = playerObj.transform;

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

        if (damageable != null && damageable.CurrentHealth <= 0)
        {
            Die();
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

        // play death sfx
        if (enemyData != null && enemyData.deathSound != null)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(enemyData.deathSound, transform.position);
            else
                AudioSource.PlayClipAtPoint(enemyData.deathSound, transform.position);
        }

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

    protected virtual void OnDrawGizmosSelected()
    {
        if (enemyData == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, enemyData.chaseRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, enemyData.attackRange);
    }
}