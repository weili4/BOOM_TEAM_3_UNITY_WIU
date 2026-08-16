using Unity.Cinemachine;
using UnityEngine;

public class Grenade : MonoBehaviour
{
    // GRENADE WITH CAMERA SHAKE

    [Header("FUSE SETTINGS")]
    public float fuseTime = 1.5f;

    [Header("EXPLOSION SPAWN SETTINGS")]
    public GameObject shrapnelPrefab;
    public int projectileCount = 8;
    public float initialSpeed = 8f;

    [Header("CAMERA SHAKE AND EFFECTS")]
    [SerializeField] private CinemachineImpulseSource impulseSource;
    [SerializeField] private float explosionShakeForce = 2.5f;
    public GameObject explosionVFX;
    public AudioClip explosionSound;

    private void Awake()
    {
        if (impulseSource == null)
            impulseSource = GetComponent<CinemachineImpulseSource>();
    }

    private void Start()
    {
        IgnorePlayerCollision();
        Invoke(nameof(Explode), fuseTime);
    }

    private void IgnorePlayerCollision()
    {
        Collider2D grenadeCollider = GetComponent<Collider2D>();
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null && grenadeCollider != null)
        {
            Collider2D[] playerColliders = player.GetComponents<Collider2D>();
            foreach (Collider2D playerCol in playerColliders)
            {
                Physics2D.IgnoreCollision(grenadeCollider, playerCol);
            }
        }
    }

    private void Explode()
    {
        // TRIGGER CAMERA SHAKE RECOIL ON EXPLOSION
        if (impulseSource != null)
        {
            impulseSource.GenerateImpulse(explosionShakeForce);
        }

        if (explosionSound != null)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(explosionSound, transform.position, 1.4f);
            else
                AudioSource.PlayClipAtPoint(explosionSound, transform.position);
        }

        if (explosionVFX != null)
            Instantiate(explosionVFX, transform.position, Quaternion.identity);

        if (shrapnelPrefab != null)
        {
            float angleStep = 360f / projectileCount;
            float angle = 0f;

            for (int i = 0; i < projectileCount; i++)
            {
                float dirX = Mathf.Cos(angle * Mathf.Deg2Rad);
                float dirY = Mathf.Sin(angle * Mathf.Deg2Rad);
                Vector2 moveDir = new Vector2(dirX, dirY).normalized;

                GameObject shrapnel = Instantiate(shrapnelPrefab, transform.position, Quaternion.identity);

                if (shrapnel.TryGetComponent<HomingShrapnel>(out HomingShrapnel script))
                {
                    script.Initialize(moveDir, initialSpeed);
                }

                angle += angleStep;
            }
        }

        Destroy(gameObject);
    }
}