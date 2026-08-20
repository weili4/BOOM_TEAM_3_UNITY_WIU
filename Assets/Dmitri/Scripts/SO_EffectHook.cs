using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_EffectHook", menuName = "Party/Effects/Hook Effect")]
public class SO_EffectHook : AbilityEffect
{
    [Header("Hook Prefab")]
    public GameObject hookPrefab;

    [Header("Visual Effects (VFX)")]
    public GameObject groundImpactVFXPrefab;
    public GameObject enemyImpactVFXPrefab;

    [Header("Damage Settings")]
    public int damage = 25;

    [Header("Hook Physics & Speeds")]
    public float throwSpeed = 25f;
    [Tooltip("Pause in seconds between hitting a surface/target and starting the reel.")]
    public float pullDelay = 0.15f;
    public float pullSpeed = 18f;
    public float enemyPullSpeed = 22f;
    public float maxDistance = 20f;
    public float stopDistance = 1.2f;

    [Header("Layer Collision")]
    public LayerMask groundLayer;
    public LayerMask enemyLayer;

    [Header("Audio Settings (Impact/Attach)")]
    public AudioClip attachSFX;
    [Range(0f, 1f)] public float soundVolume = 1.0f;

    private Dictionary<GameObject, GameObject> activeHooks = new Dictionary<GameObject, GameObject>();

    public override void Activate(GameObject user, Vector2 mouseWorldPos)
    {
        if (user == null) return;

        if (user.TryGetComponent<Animator>(out Animator animator))
        {
            animator.SetTrigger("DoubleJump");
        }

        Deactivate(user);

        if (hookPrefab != null)
        {
            Vector2 userPos = user.transform.position;
            Vector2 direction = (mouseWorldPos - userPos).normalized;

            if (direction == Vector2.zero) direction = Vector2.right;

            Quaternion rotation = Quaternion.LookRotation(Vector3.forward, direction);
            GameObject spawnedHook = Instantiate(hookPrefab, userPos, rotation);

            activeHooks[user] = spawnedHook;

            if (spawnedHook.TryGetComponent<HookProjectile>(out HookProjectile hook))
            {
                hook.Initialize(
                    user,
                    pullSpeed,
                    enemyPullSpeed,
                    pullDelay,
                    stopDistance,
                    maxDistance,
                    damage,
                    groundLayer,
                    enemyLayer,
                    attachSFX,
                    soundVolume,
                    groundImpactVFXPrefab,
                    enemyImpactVFXPrefab
                );
            }

            if (spawnedHook.TryGetComponent<Rigidbody2D>(out Rigidbody2D rb))
            {
                rb.linearVelocity = direction * throwSpeed;
            }
        }
    }

    public override void Deactivate(GameObject user)
    {
        if (user != null && activeHooks.TryGetValue(user, out GameObject activeHook))
        {
            if (activeHook != null)
            {
                Destroy(activeHook);
            }
            activeHooks.Remove(user);
        }
    }
}