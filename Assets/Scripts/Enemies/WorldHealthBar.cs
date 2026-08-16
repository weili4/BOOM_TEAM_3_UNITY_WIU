using UnityEngine;
using UnityEngine.UI;

public class WorldHealthBar : MonoBehaviour
{
    [SerializeField] private Slider healthSlider;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Vector3 offset = new Vector3(0, 1.5f, 0);

    private Transform targetEnemy;

    public void Initialize(Transform enemyTransform)
    {
        targetEnemy = enemyTransform;
        transform.SetParent(null); // unparent so it dont inherit rotation or scale
    }

    private void LateUpdate()
    {
        if (targetEnemy == null)
        {
            Destroy(gameObject); // destroy healthbar when enemy dies
            return;
        }

        // follow enemy position and got offset
        // lock rotation upright at Z = 0
        transform.position = targetEnemy.position + offset;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one * 0.01f; // world space ui scale
    }

    public void UpdateHealth(int currentHealth, int maxHealth)
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

        if (canvasGroup != null)
        {
            // show only when damaged and still alive
            if (currentHealth < maxHealth && currentHealth > 0)
                canvasGroup.alpha = 1f;
            else
                canvasGroup.alpha = 0f;
        }
    }
}