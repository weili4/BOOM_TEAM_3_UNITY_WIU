using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Damageable))]
public class BossDeathHandler : MonoBehaviour
{
    [Header("Components & Components to Disable")]
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Collider2D mainCollider;
    [SerializeField] private SpriteRenderer[] spriteRenderers;
    [SerializeField] private Behaviour[] scriptsToDisable; // e.g., BossAI, Movement, EnemyAttack scripts

    [Header("Death Animation & Delays")]
    [SerializeField] private string deathAnimStateName = "Death"; // Direct state name for instant play
    [SerializeField] private string deathAnimTrigger = "IsDead";
    [SerializeField] private float deathSequenceDuration = 2.5f;

    [Header("Fade Out Settings")]
    [SerializeField] private bool enableFadeOut = true;
    [SerializeField] private float fadeDuration = 1.5f;
    [SerializeField] private float delayBeforeFade = 0.5f;

    [Header("Audio & Visual Effects")]
    [SerializeField] private GameObject deathVFXPrefab;
    [SerializeField] private AudioClip deathSFX;

    private Damageable damageable;
    private bool isDead = false;

    private void Awake()
    {
        damageable = GetComponent<Damageable>();

        if (animator == null) animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (mainCollider == null) mainCollider = GetComponent<Collider2D>();

        if (spriteRenderers == null || spriteRenderers.Length == 0)
        {
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        }
    }

    private void OnEnable()
    {
        if (damageable != null)
        {
            damageable.onHealthChanged.AddListener(CheckHealth);
        }
    }

    private void OnDisable()
    {
        if (damageable != null)
        {
            damageable.onHealthChanged.RemoveListener(CheckHealth);
        }
    }

    private void CheckHealth(int currentHealth, int maxHealth)
    {
        if (currentHealth <= 0 && !isDead)
        {
            StartCoroutine(DeathSequenceRoutine());
        }
    }

    private IEnumerator DeathSequenceRoutine()
    {
        isDead = true;

        // 1. Instantly stop all active AI Coroutines and disable scripts
        if (scriptsToDisable != null)
        {
            foreach (var script in scriptsToDisable)
            {
                if (script != null)
                {
                    // Cast to MonoBehaviour to access StopAllCoroutines
                    if (script is MonoBehaviour mono)
                    {
                        mono.StopAllCoroutines(); // Kills active attack delays/loops
                    }

                    script.enabled = false;
                }
            }
        }

        // 2. Disable physics, colliders, and root motion
        if (mainCollider != null) mainCollider.enabled = false;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        // 3. Force instant transition to Death Animation
        if (animator != null)
        {
            animator.applyRootMotion = false; // Prevents animation driving position during death

            // Clear common attack triggers to avoid animation queuing
            animator.ResetTrigger("Attack");

            if (!string.IsNullOrEmpty(deathAnimStateName))
            {
                // Force plays state immediately, interrupting active attacks
                animator.Play(deathAnimStateName, 0, 0f);
            }
            else if (!string.IsNullOrEmpty(deathAnimTrigger))
            {
                animator.SetTrigger(deathAnimTrigger);
            }
        }

        // 4. Play VFX & Sound
        if (deathVFXPrefab != null)
        {
            Instantiate(deathVFXPrefab, transform.position, Quaternion.identity);
        }

        if (deathSFX != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(deathSFX, transform.position);
        }

        // 5. Fade out sprite renderers if enabled
        if (enableFadeOut && spriteRenderers != null && spriteRenderers.Length > 0)
        {
            StartCoroutine(FadeOutRoutine());
        }

        // 6. Wait for sequence duration
        yield return new WaitForSeconds(deathSequenceDuration);

        // 7. Clean up Boss GameObject
        Destroy(gameObject);
    }

    private IEnumerator FadeOutRoutine()
    {
        if (delayBeforeFade > 0f)
        {
            yield return new WaitForSeconds(delayBeforeFade);
        }

        Color[] initialColors = new Color[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                initialColors[i] = spriteRenderers[i].color;
            }
        }

        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(1.0f, 0.0f, elapsedTime / fadeDuration);

            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null)
                {
                    Color c = initialColors[i];
                    c.a = initialColors[i].a * alpha;
                    spriteRenderers[i].color = c;
                }
            }

            yield return null;
        }

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                Color c = initialColors[i];
                c.a = 0f;
                spriteRenderers[i].color = c;
            }
        }
    }
}