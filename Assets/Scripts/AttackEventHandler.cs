using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

public class AttackEventHandler : MonoBehaviour
{
    [Header("ATTACK SETTINGS")]
    [SerializeField] public Transform attackPoint;
    [SerializeField] public LayerMask layerToCheck;
    [SerializeField] private float attackRadius = 0.8f;
    [SerializeField] private ComboCounter comboCounter;

    [Header("PUNCH AUDIO CLIPS")]
    [SerializeField] private AudioClip swingWhooshSound;
    [SerializeField] private AudioClip normalHitSound;
    [SerializeField] private AudioClip superPrimedChimeSound;
    [SerializeField] private AudioClip superHitSound;

    [Header("PUNCH VFX PREFABS")]
    [SerializeField] private GameObject normalPunchVFX;
    [SerializeField] private GameObject superPunchVFX;

    [Header("PLAYER VISUAL FEEDBACK")]
    [SerializeField] private SpriteRenderer playerSprite;
    [SerializeField] private Color superPrimedColor = Color.red;
    [SerializeField] private float pulseSpeed = 10f;

    [Header("SUPER ATTACK AUDIO AND VISUALS")]
    [SerializeField] private CinemachineImpulseSource superImpulseSource;
    [SerializeField] private float superImpulseForce = 3.5f;

    [Header("HITSTOP AND CAMERA ZOOM")]
    [SerializeField] private float hitstopDuration = 0.08f;
    [SerializeField] private float zoomAmount = 0.6f;
    [SerializeField] private CinemachineCamera cinemachineCam;

    private bool isSuperPrimed = false;

    private void Awake()
    {
        if (playerSprite == null)
            playerSprite = GetComponent<SpriteRenderer>();

        if (playerSprite == null)
            playerSprite = GetComponentInChildren<SpriteRenderer>();

        if (superImpulseSource == null)
            superImpulseSource = GetComponent<CinemachineImpulseSource>();
    }

    private void Update()
    {
        if (playerSprite == null)
            playerSprite = GetComponentInChildren<SpriteRenderer>();

        if (isSuperPrimed && playerSprite != null)
        {
            float t = Mathf.PingPong(Time.time * pulseSpeed, 1f);
            playerSprite.color = Color.Lerp(Color.white, superPrimedColor, t);
        }
        else if (!isSuperPrimed && playerSprite != null && playerSprite.color != Color.white)
        {
            playerSprite.color = Color.white;
        }
    }

    public void OnComboReady(bool isReady)
    {
        // PLAY SFX
        if (isReady && !isSuperPrimed)
        {
            if (superPrimedChimeSound != null)
            {
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlaySFX(superPrimedChimeSound, transform.position, 1.3f);
                else
                    AudioSource.PlayClipAtPoint(superPrimedChimeSound, transform.position);
            }
        }

        isSuperPrimed = isReady;
    }

    public void AttackCheck()
    {
        if (attackPoint == null) return;

        attackPoint.gameObject.SetActive(true);

        if (swingWhooshSound != null)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(swingWhooshSound, transform.position, 1.2f);
            else
                AudioSource.PlayClipAtPoint(swingWhooshSound, transform.position);
        }

        // use super vfx if attack is primed otherwise use normal vfx
        GameObject vfxToSpawn = isSuperPrimed ? superPunchVFX : normalPunchVFX;
        if (vfxToSpawn != null)
        {
            GameObject spawnedVFX = Instantiate(vfxToSpawn, attackPoint.position, Quaternion.identity);

            if (transform.localScale.x < 0)
            {
                Vector3 scale = spawnedVFX.transform.localScale;
                scale.x = -Mathf.Abs(scale.x);
                spawnedVFX.transform.localScale = scale;
            }
        }

        // check all enemies inside attack range
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius, layerToCheck);

        bool hitAnyEnemy = false;
        bool wasSuper = isSuperPrimed;

        // keep track of enemies already hit so same enemy dont get hit twice
        // hashset keeps only unique items
        HashSet<Damageable> damagedEnemiesThisSwing = new HashSet<Damageable>();

        foreach (Collider2D hit in hits)
        {
            if (hit.gameObject.TryGetComponent<Damageable>(out Damageable damagedObject))
            {
                if (!damagedEnemiesThisSwing.Contains(damagedObject))
                {
                    damagedEnemiesThisSwing.Add(damagedObject);
                    hitAnyEnemy = true;

                    if (wasSuper)
                    {
                        damagedObject.TakeDamage(999);
                        if (comboCounter != null) comboCounter.OnEnemyHit();
                    }
                    else
                    {
                        damagedObject.TakeDamage(10);
                        if (comboCounter != null) comboCounter.OnEnemyHit();
                    }
                }
            }
        }

        if (hitAnyEnemy)
        {
            if (wasSuper)
            {
                if (superHitSound != null)
                {
                    if (AudioManager.Instance != null)
                        AudioManager.Instance.PlaySFX(superHitSound, attackPoint.position, 1.5f);
                    else
                        AudioSource.PlayClipAtPoint(superHitSound, attackPoint.position);
                }

                if (superImpulseSource != null)
                    superImpulseSource.GenerateImpulse(superImpulseForce);

                StartCoroutine(DoHitstopAndZoom(hitstopDuration, zoomAmount));

                isSuperPrimed = false;
            }
            else
            {
                if (normalHitSound != null)
                {
                    if (AudioManager.Instance != null)
                        AudioManager.Instance.PlaySFX(normalHitSound, attackPoint.position, 1.0f);
                    else
                        AudioSource.PlayClipAtPoint(normalHitSound, attackPoint.position);
                }

                StartCoroutine(DoHitstopAndZoom(0.03f, 0.15f));
            }
        }
    }

    public void AttackEnd()
    {
        if (attackPoint != null) attackPoint.gameObject.SetActive(false);
    }

    private IEnumerator DoHitstopAndZoom(float pauseTime, float zoomIn)
    {
        Camera mainCam = Camera.main;
        if (mainCam == null) yield break;

        float startSize = GetCameraOrthoSize(mainCam);

        SetCameraOrthoSize(mainCam, Mathf.Max(1f, startSize - zoomIn));

        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(pauseTime);
        Time.timeScale = 1f;

        float elapsed = 0f;
        float zoomOutDuration = 0.15f;

        while (elapsed < zoomOutDuration)
        {
            elapsed += Time.deltaTime;
            float newSize = Mathf.Lerp(startSize - zoomIn, startSize, elapsed / zoomOutDuration);
            SetCameraOrthoSize(mainCam, newSize);
            yield return null;
        }

        SetCameraOrthoSize(mainCam, startSize);
    }

    private float GetCameraOrthoSize(Camera mainCam)
    {
        if (cinemachineCam != null)
            return cinemachineCam.Lens.OrthographicSize;
        return mainCam.orthographicSize;
    }

    private void SetCameraOrthoSize(Camera mainCam, float size)
    {
        if (cinemachineCam != null)
            cinemachineCam.Lens.OrthographicSize = size;
        else
            mainCam.orthographicSize = size;
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
        }
    }
}