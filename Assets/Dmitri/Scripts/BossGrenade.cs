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

    [Header("Outer Wireframe Visuals")]
    [SerializeField] private bool showRadiusIndicator = true;
    [SerializeField] private Color outerBoundaryColor = new Color(1f, 0f, 0f, 0.35f);
    [SerializeField] private float borderWidth = 0.08f;
    [SerializeField] private int circleSegments = 36;

    [Header("Inner Filled Circle Visuals")]
    [SerializeField] private GameObject innerCirclePrefab;
    [SerializeField] private Color fillColor = new Color(1f, 0f, 0f, 0.25f);

    [Header("Visual & Audio FX")]
    [SerializeField] private GameObject explosionFXPrefab;
    [SerializeField] private AudioClip explosionSound;
    [SerializeField] private CinemachineImpulseSource impulseSource;
    [SerializeField] private float cameraShakeIntensity = 0.5f;

    private LineRenderer outerRadiusLine;
    private GameObject instantiatedFillObj;
    private Transform fillTransform;
    private Renderer fillRenderer;
    private float baseSpriteWidth = 1f;

    private bool hasExploded = false;
    private float fuseTimer = 0f;

    private void Start()
    {
        if (impulseSource == null)
            impulseSource = GetComponent<CinemachineImpulseSource>();

        if (showRadiusIndicator)
        {
            SetupOuterBoundary();
            SetupFilledCircle();
        }

        StartCoroutine(FuseRoutine());
    }

    private void Update()
    {
        if (showRadiusIndicator && !hasExploded)
        {
            fuseTimer += Time.deltaTime;
            UpdateIndicators();
        }
    }

    public void Initialize(int damage, float radius, float fuse, LayerMask layers)
    {
        this.explosionDamage = damage;
        this.explosionRadius = radius;
        this.fuseTime = fuse;
        this.damageableLayers = layers;

        if (outerRadiusLine != null) DrawCircle(outerRadiusLine, explosionRadius);
    }

    private void SetupOuterBoundary()
    {
        GameObject outerObj = new GameObject("OuterRadiusIndicator");
        outerObj.transform.SetParent(transform);
        outerObj.transform.localPosition = Vector3.zero;

        outerRadiusLine = outerObj.AddComponent<LineRenderer>();
        outerRadiusLine.useWorldSpace = true;
        outerRadiusLine.loop = true;
        outerRadiusLine.positionCount = circleSegments;
        outerRadiusLine.startWidth = borderWidth;
        outerRadiusLine.endWidth = borderWidth;
        outerRadiusLine.material = new Material(Shader.Find("Sprites/Default"));
        outerRadiusLine.startColor = outerBoundaryColor;
        outerRadiusLine.endColor = outerBoundaryColor;

        DrawCircle(outerRadiusLine, explosionRadius);
    }

    private void SetupFilledCircle()
    {
        if (innerCirclePrefab == null) return;

        // Instantiate the user-provided GameObject prefab as a child
        instantiatedFillObj = Instantiate(innerCirclePrefab, transform);
        instantiatedFillObj.transform.localPosition = Vector3.zero;
        fillTransform = instantiatedFillObj.transform;

        // Try getting SpriteRenderer or MeshRenderer to determine bounds and color
        fillRenderer = instantiatedFillObj.GetComponentInChildren<Renderer>();

        if (fillRenderer != null)
        {
            baseSpriteWidth = fillRenderer.bounds.size.x;

            // Apply color tint if it uses a compatible material or SpriteRenderer
            if (fillRenderer is SpriteRenderer sr)
            {
                sr.color = fillColor;
                sr.sortingOrder = -1;
            }
            else if (fillRenderer.material.HasProperty("_Color"))
            {
                fillRenderer.material.color = fillColor;
            }
        }

        // Initialize scale to 0
        fillTransform.localScale = Vector3.zero;
    }

    private void DrawCircle(LineRenderer line, float radius)
    {
        if (line == null) return;

        float deltaAngle = (2f * Mathf.PI) / circleSegments;
        for (int i = 0; i < circleSegments; i++)
        {
            float angle = i * deltaAngle;
            Vector3 point = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
            line.SetPosition(i, transform.position + point);
        }
    }

    private void UpdateIndicators()
    {
        // 1. Update outer boundary wireframe position to follow grenade
        if (outerRadiusLine != null)
        {
            float deltaAngle = (2f * Mathf.PI) / circleSegments;
            for (int i = 0; i < circleSegments; i++)
            {
                float angle = i * deltaAngle;
                Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                outerRadiusLine.SetPosition(i, transform.position + dir * explosionRadius);
            }
        }

        // 2. Expand inner GameObject scale over time
        if (fillTransform != null)
        {
            float progress = Mathf.Clamp01(fuseTimer / fuseTime);

            float targetDiameter = explosionRadius * 2f;
            float currentDiameter = Mathf.Lerp(0f, targetDiameter, progress);
            float scaleFactor = currentDiameter / (baseSpriteWidth > 0 ? baseSpriteWidth : 1f);

            fillTransform.localScale = new Vector3(scaleFactor, scaleFactor, 1f);
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

        if (outerRadiusLine != null) outerRadiusLine.enabled = false;
        if (instantiatedFillObj != null) Destroy(instantiatedFillObj);

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