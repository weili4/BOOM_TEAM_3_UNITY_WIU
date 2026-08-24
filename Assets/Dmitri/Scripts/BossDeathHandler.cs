using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Damageable))]
public class BossDeathHandler : MonoBehaviour
{
    [Header("Components & Components to Disable")]
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Collider2D mainCollider;
    [SerializeField] private Behaviour[] scriptsToDisable; // e.g., BossAI, Movement, EnemyAttack scripts

    [Header("Death Animation & Delays")]
    [SerializeField] private string deathAnimTrigger = "Die";
    [SerializeField] private float deathSequenceDuration = 2.5f;

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

        // 1. Disable physics & colliders so player can't bump into or take contact damage
        if (mainCollider != null) mainCollider.enabled = false;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        // 2. Disable all AI/Combat scripts attached to the boss
        if (scriptsToDisable != null)
        {
            foreach (var script in scriptsToDisable)
            {
                if (script != null) script.enabled = false;
            }
        }

        // 3. Play VFX & Sound
        if (deathVFXPrefab != null)
        {
            Instantiate(deathVFXPrefab, transform.position, Quaternion.identity);
        }

        if (deathSFX != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(deathSFX, transform.position);
        }

        // 4. Trigger Animator Death State
        if (animator != null && !string.IsNullOrEmpty("IsDead"))
        {
            animator.SetTrigger("IsDead");
        }

        // 5. Wait for the death sequence/animation to play out
        yield return new WaitForSeconds(deathSequenceDuration);

        // 6. Clean up Boss GameObject
        Destroy(gameObject);
    }
}