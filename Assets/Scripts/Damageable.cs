using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;

public class Damageable : MonoBehaviour
{
    [Header("HEALTH SETTINGS")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth = 100;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    [Header("FLOATING HEALTH BAR")]
    [SerializeField] private GameObject healthBarPrefab;

    [Header("INVULNERABILITY SETTINGS")]
    [SerializeField] private bool enableIFrames = false;
    [SerializeField] private float iFrameDuration = 1.0f;
    [SerializeField] private float translucentAlpha = 0.3f;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("HIT FEEDBACK AND CAMERA SHAKE")]
    [SerializeField] private CinemachineImpulseSource source;
    [SerializeField] private float hitShakeForce = 1.0f;
    [SerializeField] private GameObject hitVFXPrefab;
    [SerializeField] private AudioClip hitSound;

    public UnityEvent<int, int> onHealthChanged;

    private bool isInvulnerable = false;
    private bool wasTranslucent = false;

    private void Awake()
    {
        currentHealth = maxHealth;
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

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

    private void LateUpdate()
    {
        if (isInvulnerable && spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = translucentAlpha;
            spriteRenderer.color = c;
            wasTranslucent = true;
        }
        else if (!isInvulnerable && wasTranslucent && spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = 1f;
            spriteRenderer.color = c;
            wasTranslucent = false;
        }
    }

    public void TakeDamage(int damage)
    {
        if (isInvulnerable) return;

        if (source != null)
            source.GenerateImpulse(hitShakeForce);

        if (hitVFXPrefab != null)
            Instantiate(hitVFXPrefab, transform.position, Quaternion.identity);

        // PLAY HIT SFX
        if (hitSound != null)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(hitSound, transform.position);
            else
                AudioSource.PlayClipAtPoint(hitSound, transform.position);
        }

        currentHealth -= damage;
        if (currentHealth < 0) currentHealth = 0;

        onHealthChanged?.Invoke(currentHealth, maxHealth);

        if (enableIFrames && currentHealth > 0)
        {
            StartCoroutine(IFrameRoutine());
        }
    }

    public void Heal(int amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;

        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private IEnumerator IFrameRoutine()
    {
        isInvulnerable = true;
        yield return new WaitForSeconds(iFrameDuration);
        isInvulnerable = false;
    }
}