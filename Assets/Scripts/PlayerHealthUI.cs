using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealthUI : MonoBehaviour
{
    [Header("target player")]
    [SerializeField] private Damageable playerDamageable;

    [Header("ui fill")]
    [SerializeField] private Image instantFillImage;
    [SerializeField] private Image catchUpFillImage;

    [Header("Ghost Bar")]
    [SerializeField] private float catchUpDelay = 0.5f;
    [SerializeField] private float catchUpSpeed = 1.5f;

    [Header("text ui")]
    [SerializeField] private TextMeshProUGUI healthText;

    private float targetFillAmount = 1f;
    private float catchUpTimer = 0f;

    private void Start()
    {
        if (playerDamageable == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerDamageable = player.GetComponent<Damageable>();
            }
        }

        if (playerDamageable != null)
        {
            playerDamageable.onHealthChanged.AddListener(UpdateHealthUI);

            // set initial fill amounts immediately
            float initialFill = (float)playerDamageable.CurrentHealth / playerDamageable.MaxHealth;
            targetFillAmount = initialFill;
            if (instantFillImage != null) instantFillImage.fillAmount = initialFill;
            if (catchUpFillImage != null) catchUpFillImage.fillAmount = initialFill;

            UpdateHealthText(playerDamageable.CurrentHealth, playerDamageable.MaxHealth);
        }
    }

    private void Update()
    {
        // smoothly drain the catch up bar
        if (catchUpFillImage != null && catchUpFillImage.fillAmount > targetFillAmount)
        {
            if (catchUpTimer > 0)
            {
                catchUpTimer -= Time.deltaTime;
            }
            else
            {
                // smoothly drain ghost bar down to match green bar
                catchUpFillImage.fillAmount = Mathf.MoveTowards(
                    catchUpFillImage.fillAmount,
                    targetFillAmount,
                    catchUpSpeed * Time.deltaTime
                );
            }
        }
    }

    public void UpdateHealthUI(int currentHealth, int maxHealth)
    {
        float newFill = Mathf.Clamp01((float)currentHealth / maxHealth);
        targetFillAmount = newFill;

        // front bar drops INSTANTLY
        if (instantFillImage != null)
        {
            instantFillImage.fillAmount = newFill;
        }

        // if taking damage, reset ghost bar timer so it waits before draining
        if (catchUpFillImage != null && newFill < catchUpFillImage.fillAmount)
        {
            catchUpTimer = catchUpDelay;
        }
        // if healing, instantly bring ghost bar up so it doesnt get left behind
        else if (catchUpFillImage != null && newFill > catchUpFillImage.fillAmount)
        {
            catchUpFillImage.fillAmount = newFill;
        }

        UpdateHealthText(currentHealth, maxHealth);
    }

    private void UpdateHealthText(int currentHealth, int maxHealth)
    {
        if (healthText != null)
        {
            healthText.text = $"{currentHealth} / {maxHealth}";
        }
    }
}