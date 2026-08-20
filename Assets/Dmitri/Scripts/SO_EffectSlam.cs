using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

[CreateAssetMenu(fileName = "SO_EffectSlam", menuName = "Party/Effects/Slam Effect")]
public class SO_EffectSlam : AbilityEffect
{
    [Header("Visual Effects (VFX)")]
    public GameObject groundImpactVFXPrefab;

    [Header("Damage & AoE Settings")]
    public int damage = 35;
    public float slamRadius = 3.5f;

    [Tooltip("Delay in seconds before the slam hitbox and impact trigger upon hitting the ground.")]
    public float hitboxSpawnDelay = 0.1f;

    [Tooltip("Duration in seconds that the hitbox stays active to detect and damage enemies.")]
    public float hitboxLifetime = 0.2f;

    [Header("Grounded Slam Settings")]
    [Tooltip("Upward force applied when executing the slam while grounded.")]
    public float groundSlamJumpForce = 12f;

    [Tooltip("Time in seconds the player ascends into the air before forcing the downward slam.")]
    public float jumpUpDuration = 0.15f;

    [Header("Air Slam Physics")]
    public float downwardForce = 30f;

    [Tooltip("Raycast distance used to detect when the player is close to the ground.")]
    public float groundCheckDistance = 0.3f;

    [Header("Camera Shake Settings")]
    public bool enableCameraShake = true;
    public float shakeForce = 1.0f;

    [Header("Layer Collision")]
    public LayerMask groundLayer;
    public LayerMask enemyLayer;

    [Header("Audio Settings")]
    public AudioClip slamSFX;
    [Range(0f, 1f)] public float soundVolume = 1.0f;

    [Header("Gizmo Settings")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color gizmoColor = Color.red;
    [SerializeField] private float gizmoDisplayDuration = 1.0f;

    private Dictionary<GameObject, Coroutine> activeCoroutines = new Dictionary<GameObject, Coroutine>();

    public override void Activate(GameObject user, Vector2 mouseWorldPos)
    {
        if (user == null) return;

        Deactivate(user);

        bool isGrounded = CheckIsGrounded(user);
        MonoBehaviour runner = user.GetComponent<MonoBehaviour>();

        if (runner == null) return;

        if (isGrounded && groundSlamJumpForce > 0f)
        {
            activeCoroutines[user] = runner.StartCoroutine(GroundedSlamRoutine(user));
        }
        else if (isGrounded)
        {
            if (user.TryGetComponent<Animator>(out Animator animator))
            {
                animator.SetTrigger("Slam");
            }

            TriggerSlamImpact(user);
        }
        else
        {
            activeCoroutines[user] = runner.StartCoroutine(AirSlamRoutine(user));
        }
    }

    private IEnumerator GroundedSlamRoutine(GameObject user)
    {
        if (!user.TryGetComponent<Rigidbody2D>(out Rigidbody2D rb)) yield break;

        Animator animator = user.GetComponent<Animator>();
        if (animator == null) animator = user.GetComponentInChildren<Animator>();

        if (animator != null)
        {
            animator.SetBool("IsJumping", true);
        }

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, groundSlamJumpForce);

        if (jumpUpDuration > 0f)
        {
            yield return new WaitForSeconds(jumpUpDuration);
        }

        if (animator != null)
        {
            animator.SetTrigger("Slam");
        }

        yield return AirSlamRoutine(user);
    }

    private IEnumerator AirSlamRoutine(GameObject user)
    {
        if (user == null || !user.TryGetComponent<Rigidbody2D>(out Rigidbody2D rb)) yield break;

        Animator animator = user.GetComponent<Animator>();
        if (animator == null) animator = user.GetComponentInChildren<Animator>();

        if (animator != null)
        {
            animator.SetTrigger("Slam");
        }

        while (!CheckIsGrounded(user))
        {
            if (user == null) yield break;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Min(rb.linearVelocity.y, -downwardForce));
            yield return null;
        }

        ContactFilter2D groundFilter = new ContactFilter2D();
        groundFilter.SetLayerMask(groundLayer);
        groundFilter.useLayerMask = true;

        while (!rb.IsTouching(groundFilter))
        {
            if (user == null) yield break;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -downwardForce);
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;

        if (animator != null)
        {
            animator.SetBool("IsGrounded", true);
        }

        TriggerSlamImpact(user);
    }

    private void TriggerSlamImpact(GameObject user)
    {
        if (user == null) return;

        MonoBehaviour runner = user.GetComponent<MonoBehaviour>();
        if (runner == null) return;

        if (hitboxSpawnDelay > 0f)
        {
            activeCoroutines[user] = runner.StartCoroutine(DelayedHitboxRoutine(user));
        }
        else
        {
            activeCoroutines[user] = runner.StartCoroutine(ActiveHitboxRoutine(user));
        }
    }

    private IEnumerator DelayedHitboxRoutine(GameObject user)
    {
        yield return new WaitForSeconds(hitboxSpawnDelay);
        yield return ActiveHitboxRoutine(user);
    }

    private IEnumerator ActiveHitboxRoutine(GameObject user)
    {
        if (user == null) yield break;

        Vector2 center = user.transform.position;
        HashSet<GameObject> damagedEnemies = new HashSet<GameObject>();

        // Trigger Cinemachine Camera Shake
        if (enableCameraShake)
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

        // Draw Gizmo on impact
        if (showGizmos)
        {
            SlamGizmoDrawer drawer = user.AddComponent<SlamGizmoDrawer>();
            drawer.Initialize(slamRadius, gizmoColor, Mathf.Max(hitboxLifetime, gizmoDisplayDuration));
        }

        // Spawn Impact VFX
        if (groundImpactVFXPrefab != null)
        {
            GameObject vfxInstance = Instantiate(groundImpactVFXPrefab, center, Quaternion.identity);
            if (vfxInstance.TryGetComponent<ParticleSystem>(out ParticleSystem ps))
            {
                float totalDuration = ps.main.duration + ps.main.startLifetime.constantMax;
                Destroy(vfxInstance, totalDuration);
            }
            else
            {
                Destroy(vfxInstance, 2.0f);
            }
        }

        // Play SFX
        if (slamSFX != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(slamSFX, center, soundVolume);
        }

        // Active Hitbox Loop
        float timer = 0f;
        do
        {
            if (user == null) break;

            center = user.transform.position;
            Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(center, slamRadius, enemyLayer);

            foreach (Collider2D hit in hitEnemies)
            {
                GameObject enemyObj = GetRootHitObject(hit.gameObject);
                if (!damagedEnemies.Contains(enemyObj))
                {
                    damagedEnemies.Add(enemyObj);
                    if (enemyObj.TryGetComponent<Damageable>(out Damageable damageable))
                    {
                        damageable.TakeDamage(damage);
                    }
                }
            }

            if (hitboxLifetime <= 0f) break;

            timer += Time.deltaTime;
            yield return null;

        } while (timer < hitboxLifetime);

        activeCoroutines.Remove(user);
    }

    private bool CheckIsGrounded(GameObject user)
    {
        Vector2 origin = user.transform.position;

        if (user.TryGetComponent<Collider2D>(out Collider2D col))
        {
            origin = new Vector2(col.bounds.center.x, col.bounds.min.y);
        }

        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundCheckDistance, groundLayer);
        return hit.collider != null;
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
        if (user != null && activeCoroutines.TryGetValue(user, out Coroutine coroutine))
        {
            MonoBehaviour runner = user.GetComponent<MonoBehaviour>();
            if (runner != null && coroutine != null)
            {
                runner.StopCoroutine(coroutine);
            }
            activeCoroutines.Remove(user);
        }
    }
}