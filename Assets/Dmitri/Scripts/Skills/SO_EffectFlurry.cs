using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

[CreateAssetMenu(fileName = "SO_EffectFlurry", menuName = "Party/Effects/FlurryEffect")]
public class SO_EffectContinuousHit : AbilityEffect
{
    [Header("Skill Duration & Timing")]
    public float skillDuration = 3.0f;
    [Tooltip("Time delay between hit detection ticks in seconds.")]
    public float tickInterval = 0.2f;

    [Header("Movement Settings")]
    [Tooltip("Multiplier applied to movement speed while active (1.0 = normal speed, 0.5 = 50% speed).")]
    [Range(0f, 1f)] public float moveSpeedMultiplier = 0.5f;

    [Header("Defense & Damage Reduction")]
    [Tooltip("Multiplier applied to incoming damage while active (1.0 = full damage, 0.5 = 50% damage taken, 0 = invulnerable).")]
    [Range(0f, 1f)] public float incomingDamageMultiplier = 0.5f;

    [Header("Damage & Hitbox Settings")]
    public int damagePerTick = 10;
    public Vector2 hitBoxSize = new Vector2(2.0f, 1.5f);
    public Vector2 hitBoxOffset = new Vector2(1.5f, 0f);

    [Header("Layer Collision")]
    public LayerMask enemyLayer;

    [Header("Visual Effects (VFX)")]
    public GameObject hitVFXPrefab;

    [Header("Camera Shake Settings")]
    public bool enableCameraShake = true;
    public float shakeForce = 0.5f;

    [Header("Audio Settings")]
    public AudioClip hitSFX;
    [Range(0f, 1f)] public float soundVolume = 1.0f;

    [Header("Gizmo Settings")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color gizmoColor = Color.red;
    [SerializeField] private float gizmoDisplayDuration = 0.1f;

    private Dictionary<GameObject, Coroutine> activeCoroutines = new Dictionary<GameObject, Coroutine>();
    private Dictionary<GameObject, GameObject> activeVFX = new Dictionary<GameObject, GameObject>();
    private Dictionary<GameObject, float> originalMoveSpeeds = new Dictionary<GameObject, float>();
    private Dictionary<GameObject, float> originalDamageMultipliers = new Dictionary<GameObject, float>();

    public override void Activate(GameObject user, Vector2 mouseWorldPos)
    {
        if (user == null) return;

        Deactivate(user);

        MonoBehaviour runner = user.GetComponent<MonoBehaviour>();
        if (runner == null) return;

        activeCoroutines[user] = runner.StartCoroutine(ContinuousHitRoutine(user));
    }

    private IEnumerator ContinuousHitRoutine(GameObject user)
    {
        float elapsedTime = 0f;

        // Apply movement speed reduction to PlayerController and trigger animation
        if (user.TryGetComponent<PlayerController>(out PlayerController player))
        {
            if (!originalMoveSpeeds.ContainsKey(user))
            {
                originalMoveSpeeds[user] = player.moveSpeed;
            }
            player.moveSpeed = originalMoveSpeeds[user] * moveSpeedMultiplier;

            if (player.animator != null)
            {
                player.animator.SetBool("Flurry", true);
            }
        }

        // Apply incoming damage reduction to Damageable
        if (user.TryGetComponent<Damageable>(out Damageable playerDamageable))
        {
            if (!originalDamageMultipliers.ContainsKey(user))
            {
                originalDamageMultipliers[user] = playerDamageable.incomingDamageMultiplier;
            }
            playerDamageable.incomingDamageMultiplier = originalDamageMultipliers[user] * incomingDamageMultiplier;
        }

        // 1. Spawn looping VFX ONCE at the start
        GameObject vfxInstance = null;
        if (hitVFXPrefab != null)
        {
            float facingDir = Mathf.Sign(user.transform.localScale.x);
            Vector2 offsetPos = new Vector2(hitBoxOffset.x * facingDir, hitBoxOffset.y);
            Vector3 spawnPos = (Vector2)user.transform.position + offsetPos;
            spawnPos.z = -1f;

            Quaternion spawnRotation = Quaternion.Euler(0f, facingDir < 0 ? 180f : 0f, 0f);
            vfxInstance = Instantiate(hitVFXPrefab, spawnPos, spawnRotation);
            activeVFX[user] = vfxInstance;

            ParticleSystem[] particleSystems = vfxInstance.GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem ps in particleSystems)
            {
                ps.Play(true);
            }
        }

        while (elapsedTime < skillDuration)
        {
            if (user == null) break;

            PerformHitDetection(user, vfxInstance);

            yield return new WaitForSeconds(tickInterval);
            elapsedTime += tickInterval;
        }

        // Clean up when duration finishes naturally
        Deactivate(user);
    }

    private void PerformHitDetection(GameObject user, GameObject vfxInstance)
    {
        if (user == null) return;

        float facingDirection = Mathf.Sign(user.transform.localScale.x);
        Vector2 offsetPosition = new Vector2(hitBoxOffset.x * facingDirection, hitBoxOffset.y);
        Vector2 centerPoint = (Vector2)user.transform.position + offsetPosition;

        // 2. Update VFX position to follow player hit box
        if (vfxInstance != null)
        {
            vfxInstance.transform.position = new Vector3(centerPoint.x, centerPoint.y, -1f);
            vfxInstance.transform.rotation = Quaternion.Euler(0f, facingDirection < 0 ? 180f : 0f, 0f);
        }

        // 3. Draw Gizmo on every tick
        if (showGizmos)
        {
            FlurryGizmoDrawer drawer = user.AddComponent<FlurryGizmoDrawer>();
            drawer.Initialize(centerPoint, hitBoxSize, gizmoColor, gizmoDisplayDuration);
        }

        // 4. Perform physics detection and apply damage/audio per enemy
        Collider2D[] hitEnemies = Physics2D.OverlapBoxAll(centerPoint, hitBoxSize, 0f, enemyLayer);

        if (hitEnemies.Length > 0 && enableCameraShake)
        {
            TriggerCameraShake(user);
        }

        HashSet<GameObject> hitObjectsThisTick = new HashSet<GameObject>();

        foreach (Collider2D hit in hitEnemies)
        {
            GameObject enemyObj = GetRootHitObject(hit.gameObject);
            if (hitObjectsThisTick.Contains(enemyObj)) continue;
            hitObjectsThisTick.Add(enemyObj);

            if (enemyObj.TryGetComponent<Damageable>(out Damageable damageable))
            {
                damageable.TakeDamage(damagePerTick);
            }

            if (hitSFX != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(hitSFX, hit.bounds.center, soundVolume);
            }
        }
    }

    private void TriggerCameraShake(GameObject user)
    {
        if (user.TryGetComponent<CinemachineImpulseSource>(out CinemachineImpulseSource impulse))
        {
            impulse.GenerateImpulseWithForce(shakeForce);
        }
        else if (user.GetComponentInChildren<CinemachineImpulseSource>() is CinemachineImpulseSource childImpulse)
        {
            childImpulse.GenerateImpulseWithForce(shakeForce);
        }
    }

    private GameObject GetRootHitObject(GameObject obj)
    {
        if (((1 << obj.layer) & enemyLayer) != 0) return obj;
        if (obj.transform.parent != null && ((1 << obj.transform.parent.gameObject.layer) & enemyLayer) != 0)
        {
            return obj.transform.parent.gameObject;
        }
        return obj;
    }

    public override void Deactivate(GameObject user)
    {
        if (user == null) return;

        // Restore original movement speed to PlayerController and disable animation
        if (originalMoveSpeeds.TryGetValue(user, out float originalSpeed))
        {
            if (user.TryGetComponent<PlayerController>(out PlayerController player))
            {
                player.moveSpeed = originalSpeed;

                if (player.animator != null)
                {
                    player.animator.SetBool("Flurry", false);
                }
            }
            originalMoveSpeeds.Remove(user);
        }
        else if (user.TryGetComponent<PlayerController>(out PlayerController player) && player.animator != null)
        {
            player.animator.SetBool("Flurry", false);
        }

        // Restore original incoming damage multiplier to Damageable
        if (originalDamageMultipliers.TryGetValue(user, out float originalDamageMult))
        {
            if (user.TryGetComponent<Damageable>(out Damageable playerDamageable))
            {
                playerDamageable.incomingDamageMultiplier = originalDamageMult;
            }
            originalDamageMultipliers.Remove(user);
        }

        // Stop Coroutine
        if (activeCoroutines.TryGetValue(user, out Coroutine coroutine))
        {
            MonoBehaviour runner = user.GetComponent<MonoBehaviour>();
            if (runner != null && coroutine != null)
            {
                runner.StopCoroutine(coroutine);
            }
            activeCoroutines.Remove(user);
        }

        // Stop & Destroy Active VFX
        if (activeVFX.TryGetValue(user, out GameObject vfx))
        {
            if (vfx != null)
            {
                ParticleSystem[] systems = vfx.GetComponentsInChildren<ParticleSystem>();
                if (systems.Length > 0)
                {
                    float maxLifetime = 0f;
                    foreach (ParticleSystem ps in systems)
                    {
                        ps.Stop(true, ParticleSystemStopBehavior.StopEmitting); // Stop emitting new particles so existing ones fade out nicely
                        if (ps.main.startLifetime.constantMax > maxLifetime)
                        {
                            maxLifetime = ps.main.startLifetime.constantMax;
                        }
                    }
                    Destroy(vfx, maxLifetime);
                }
                else
                {
                    Destroy(vfx);
                }
            }
            activeVFX.Remove(user);
        }
    }
}

public class FlurryGizmoDrawer : MonoBehaviour
{
    private Vector2 center;
    private Vector2 size;
    private Color gizmoColor;

    public void Initialize(Vector2 centerPosition, Vector2 boxSize, Color color, float displayDuration)
    {
        center = centerPosition;
        size = boxSize;
        gizmoColor = color;
        Destroy(this, displayDuration);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireCube(center, size);
    }
}