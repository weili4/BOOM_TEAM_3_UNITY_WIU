using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(Rigidbody2D))]
public class BossGrenade : MonoBehaviour
{
    [Header("Explosion Settings")]
    [SerializeField] private float fuseTime = 1.5f;
    [SerializeField] private float explosionRadius = 2.5f;
    [SerializeField] private int explosionDamage = 20;
    [SerializeField] private LayerMask damageableLayers;
    [SerializeField] private bool explodeOnGroundImpact = true;
    [SerializeField] private LayerMask groundLayer;

    [Header("Radius Indicator Visuals")]
    [SerializeField] private bool showRadiusIndicator = true;
    [SerializeField] private Color radiusColor = new Color(1f, 0f, 0f, 0.35f); // Transparent Red
    [SerializeField] private float borderWidth = 0.08f;
    [SerializeField] private int circleSegments = 36;

    [Header("Visual & Audio FX")]
    [SerializeField] private GameObject explosionFXPrefab;
    [SerializeField] private AudioClip explosionSound;
    [SerializeField] private CinemachineImpulseSource impulseSource;
    [SerializeField] private float cameraShakeIntensity = 0.5f;

    private LineRenderer radiusLine;
    private bool hasExploded = false;

    private void Start()
    {
        if (impulseSource == null)
            impulseSource = GetComponent<CinemachineImpulseSource>();

        if (showRadiusIndicator)
        {
            SetupRadiusIndicator();
        }

        // Start countdown timer immediately upon spawn
        StartCoroutine(FuseRoutine());
    }

    private void Update()
    {
        if (showRadiusIndicator && radiusLine != null && !hasExploded)
        {
            UpdateRadiusCirclePosition();
        }
    }

    public void Initialize(int damage, float radius, float fuse, LayerMask layers)
    {
        this.explosionDamage = damage;
        this.explosionRadius = radius;
        this.fuseTime = fuse;
        this.damageableLayers = layers;

        if (radiusLine != null)
        {
            DrawCircle(explosionRadius);
        }
    }

    private void SetupRadiusIndicator()
    {
        // Dynamically add LineRenderer if not already attached
        if (!TryGetComponent<LineRenderer>(out radiusLine))
        {
            radiusLine = gameObject.AddComponent<LineRenderer>();
        }

        radiusLine.useWorldSpace = true;
        radiusLine.loop = true;
        radiusLine.positionCount = circleSegments;
        radiusLine.startWidth = borderWidth;
        radiusLine.endWidth = borderWidth;

        // Use default unlit line shader if material is not assigned
        radiusLine.material = new Material(Shader.Find("Sprites/Default"));
        radiusLine.startColor = radiusColor;
        radiusLine.endColor = radiusColor;

        DrawCircle(explosionRadius);
    }

    private void DrawCircle(float radius)
    {
        if (radiusLine == null) return;

        float deltaAngle = (2f * Mathf.PI) / circleSegments;
        for (int i = 0; i < circleSegments; i++)
        {
            float angle = i * deltaAngle;
            Vector3 point = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
            radiusLine.SetPosition(i, transform.position + point);
        }
    }

    private void UpdateRadiusCirclePosition()
    {
        // Re-align circle center with current projectile position
        float deltaAngle = (2f * Mathf.PI) / circleSegments;
        for (int i = 0; i < circleSegments; i++)
        {
            float angle = i * deltaAngle;
            Vector3 offset = new Vector3(Mathf.Cos(angle) * explosionRadius, Mathf.Sin(angle) * explosionRadius, 0f);
            radiusLine.SetPosition(i, transform.position + offset);
        }
    }

    private IEnumerator FuseRoutine()
    {
        yield return new WaitForSeconds(fuseTime);
        Explode();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (explodeOnGroundImpact && !hasExploded)
        {
            if (((1 << collision.gameObject.layer) & groundLayer) != 0)
            {
                StopAllCoroutines();
                Explode();
            }
        }
    }

    private void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;

        if (radiusLine != null)
        {
            radiusLine.enabled = false;
        }

        // 1. Spawn Explosion Particles
        if (explosionFXPrefab != null)
        {
            Instantiate(explosionFXPrefab, transform.position, explosionFXPrefab.transform.rotation);
        }

        // 2. Play Audio
        if (explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(explosionSound, transform.position);
        }

        // 3. Trigger Camera Shake
        if (impulseSource != null)
        {
            impulseSource.GenerateImpulse(cameraShakeIntensity);
        }

        // 4. Radius Damage Check
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius, damageableLayers);
        foreach (Collider2D hit in hits)
        {
            if (hit.TryGetComponent<Damageable>(out Damageable targetHealth))
            {
                targetHealth.TakeDamage(explosionDamage);
            }
        }

        // 5. Destroy Projectile Object
        Destroy(gameObject, 0.1f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}