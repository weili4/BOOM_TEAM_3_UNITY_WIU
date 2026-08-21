using UnityEngine;
using UnityEngine.InputSystem;

public class AndroidPrimaryAttack : CharacterPrimaryAttack
{
    [Header("combo combo settings")]
    [SerializeField] private float comboResetTime = 0.9f; // resets to hit 1 if player waits too long
    [SerializeField] private float attackCooldown = 0.22f; // time between combo swings

    [Header("damage per combo hit (1, 2, 3)")]
    [SerializeField] private int hit1Damage = 15;
    [SerializeField] private int hit2Damage = 20;
    [SerializeField] private int hit3Damage = 35; // finisher hit

    [Header("wide range hitbox size")]
    [SerializeField] private Vector2 wideHitboxSize = new Vector2(2.4f, 1.6f);
    [SerializeField] private Vector2 hitboxOffset = new Vector2(1.2f, 0f);
    [SerializeField] private LayerMask enemyLayer;

    [Header("audio clips")]
    [SerializeField] private AudioClip swingSFX;
    [SerializeField] private AudioClip finisherSFX;
    [SerializeField] private GameObject hitVFXPrefab;

    private int comboStep = 0; // 0 = ready for hit 1, 1 = hit 2, 2 = hit 3
    private float comboTimer = 0f;
    private float attackTimer = 0f;

    protected override void Update()
    {
        base.Update();

        if (attackTimer > 0f) attackTimer -= Time.deltaTime;

        // reset combo back to step 0 if player pauses too long between clicks
        if (comboStep > 0)
        {
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0f)
            {
                comboStep = 0;
            }
        }
    }

    protected override void HandleAttack()
    {
        bool attackPressed = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

        if (attackPressed && attackTimer <= 0f)
        {
            ExecuteComboHit();
            attackTimer = attackCooldown;
            comboTimer = comboResetTime;
        }
    }

    private void ExecuteComboHit()
    {
        comboStep++;
        int damageToDeal = hit1Damage;
        float knockback = 5f;

        if (comboStep == 1)
        {
            damageToDeal = hit1Damage;
            if (animator != null) animator.SetTrigger("Attack1");
            if (swingSFX != null && AudioManager.Instance != null) AudioManager.Instance.PlaySFX(swingSFX, transform.position);
        }
        else if (comboStep == 2)
        {
            damageToDeal = hit2Damage;
            if (animator != null) animator.SetTrigger("Attack2");
            if (swingSFX != null && AudioManager.Instance != null) AudioManager.Instance.PlaySFX(swingSFX, transform.position);
        }
        else if (comboStep >= 3)
        {
            damageToDeal = hit3Damage;
            knockback = 9f; // big finisher knockback
            if (animator != null) animator.SetTrigger("Attack3");
            if (finisherSFX != null && AudioManager.Instance != null) AudioManager.Instance.PlaySFX(finisherSFX, transform.position);

            comboStep = 0; // reset combo after finisher
        }

        // detect and hit enemies with wide hitbox
        float facingDir = Mathf.Sign(transform.localScale.x);
        Vector2 center = (Vector2)transform.position + new Vector2(hitboxOffset.x * facingDir, hitboxOffset.y);

        if (hitVFXPrefab != null)
        {
            Instantiate(hitVFXPrefab, center, Quaternion.identity);
        }

        Collider2D[] hitEnemies = Physics2D.OverlapBoxAll(center, wideHitboxSize, 0f, enemyLayer);
        foreach (var col in hitEnemies)
        {
            if (col.CompareTag("Player") || col.CompareTag("Ally")) continue;

            if (col.TryGetComponent<Damageable>(out var enemy))
            {
                enemy.TakeDamage(damageToDeal, new Vector2(facingDir, 0.3f), knockback);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        float facingDir = Mathf.Sign(transform.localScale.x);
        Vector2 center = (Vector2)transform.position + new Vector2(hitboxOffset.x * facingDir, hitboxOffset.y);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(center, wideHitboxSize);
    }
}