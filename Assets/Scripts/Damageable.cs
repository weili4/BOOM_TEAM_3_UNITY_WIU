using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;

public class Damageable : MonoBehaviour
{
    [Header("health settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth = 100;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    [Header("floating health bar")]
    [SerializeField] private GameObject healthBarPrefab;

    [Header("hit flash settings (enemies and player)")]
    [SerializeField] private float hitFlashDuration = 0.08f; // brief solid white impact flash
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("player invulnerability i-frames")]
    [SerializeField] private bool enableIFrames = true;
    [SerializeField] private float iFrameDuration = 0.8f;
    [SerializeField] private float flashSpeed = 25f; // speed of player flicker

    [Header("hit feedback and screen shake")]
    [SerializeField] private CinemachineImpulseSource source;
    [SerializeField] private float hitShakeForce = 1.2f;
    [SerializeField] private GameObject hitVFXPrefab;
    [SerializeField] private AudioClip hitSound;

    public UnityEvent<int, int> onHealthChanged;

    private bool isInvulnerable = false;
    private float iFrameTimer = 0f;
    private float hitFlashTimer = 0f;
    private Rigidbody2D rb;
    private MaterialPropertyBlock propBlock;
    private static readonly int FlashAmountProp = Shader.PropertyToID("_FlashAmount");

    public bool IsInvulnerable => isInvulnerable;

    private void Awake()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        propBlock = new MaterialPropertyBlock();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (source == null)
            source = GetComponent<CinemachineImpulseSource>();
    }

    private void Start()
    {
        if (!CompareTag("Player") && healthBarPrefab != null)
        {
            GameObject barObj = Instantiate(healthBarPrefab, transform.position, Quaternion.identity);
            WorldHealthBar healthBar = barObj.GetComponent<WorldHealthBar>();
            if (healthBar != null)
            {
                healthBar.Initialize(transform);
                onHealthChanged.AddListener(healthBar.UpdateHealth);
            }
        }

        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void TakeDamage(int damage, Vector2 hitDirection, float knockbackForce = 8.0f)
    {
        if (isInvulnerable || currentHealth <= 0) return;

        // screen shake
        if (source != null)
            source.GenerateImpulse(hitShakeForce);

        // vfx and sfx
        if (hitVFXPrefab != null)
            Instantiate(hitVFXPrefab, transform.position, Quaternion.identity);

        if (hitSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(hitSound, transform.position);

        currentHealth -= damage;
        if (currentHealth < 0) currentHealth = 0;
        onHealthChanged?.Invoke(currentHealth, maxHealth);

        // trigger instant solid white hit flash on anyone who gets hit
        hitFlashTimer = hitFlashDuration;

        // player specific reactions
        if (CompareTag("Player"))
        {
            if (ScreenFlashUI.Instance != null)
            {
                ScreenFlashUI.Instance.TriggerRedFlash();
            }

            if (TryGetComponent<PlayerController>(out var controller))
            {
                float dirX = Mathf.Sign(hitDirection.x);
                if (Mathf.Abs(hitDirection.x) < 0.01f)
                {
                    dirX = -transform.localScale.x;
                }

                Vector2 knockbackVelocity = new Vector2(dirX * knockbackForce, knockbackForce * 0.45f);
                controller.ApplyKnockback(knockbackVelocity, 0.2f);
            }

            if (enableIFrames && currentHealth > 0)
            {
                isInvulnerable = true;
                iFrameTimer = iFrameDuration;
            }
        }
    }

    public void TakeDamage(int damage)
    {
        TakeDamage(damage, Vector2.zero, 0f);
    }

    public void Heal(int amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;
        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void LateUpdate()
    {
        if (spriteRenderer == null) return;

        float flashAmount = 0f;

        // 1. player i-frame flicker
        if (isInvulnerable)
        {
            iFrameTimer -= Time.deltaTime;
            float sine = Mathf.Sin(Time.time * flashSpeed);
            flashAmount = sine > 0f ? 1f : 0f;

            if (iFrameTimer <= 0f)
            {
                isInvulnerable = false;
                flashAmount = 0f;
            }
        }
        // 2. enemy/player instant hit impact flash
        else if (hitFlashTimer > 0f)
        {
            hitFlashTimer -= Time.deltaTime;
            flashAmount = 1f; // solid white
        }

        // apply to shader property block
        spriteRenderer.GetPropertyBlock(propBlock);
        propBlock.SetFloat(FlashAmountProp, flashAmount);
        spriteRenderer.SetPropertyBlock(propBlock);
    }
}