using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class BarbaraPrimaryAttack : CharacterPrimaryAttack
{
    [Header("arm and gun root container")]
    [SerializeField] private GameObject armAndGunHolder;
    [SerializeField] private SpriteRenderer armSpriteRenderer;
    [SerializeField] private Transform gunTransform;
    [SerializeField] private Transform firePoint;

    [Header("5 arm sprites (0=down, 1=down-diag, 2=forward, 3=up-diag, 4=up)")]
    [SerializeField] private Sprite[] armAngleSprites = new Sprite[5];

    [Header("tap shot settings")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float fireRate = 0.2f;
    [SerializeField] private int bulletDamage = 15;
    [SerializeField] private float bulletSpeed = 18f;
    [SerializeField] private float postAttackLingerDuration = 0.1f; // exact 0.1s pause after shooting
    [SerializeField] private AudioClip shootSFX;
    [SerializeField] private GameObject muzzleFlashVFX;

    [Header("charge meter ui")]
    [SerializeField] private GameObject chargeMeterRoot;
    [SerializeField] private Slider chargeSlider;
    [SerializeField] private Image chargeFillImage;
    [SerializeField] private Color normalChargeColor = new Color(0f, 1f, 1f, 1f); // cyan
    [SerializeField] private Color maxChargeColor1 = new Color(1f, 0.85f, 0f, 1f); // gold
    [SerializeField] private Color maxChargeColor2 = Color.white;
    [SerializeField] private float maxFlickerSpeed = 30f;

    [Header("charged burst shot")]
    [SerializeField] private float chargeStartDelay = 0.15f; // time holding after first shot before charging starts
    [SerializeField] private float maxChargeTime = 1.0f;     // time in seconds to reach full charge
    [SerializeField] private int burstBulletCount = 5;
    [SerializeField] private float burstFireDelay = 0.05f;
    [SerializeField] private int burstBulletDamage = 22;

    // exact custom offsets for the 5 angles
    private readonly Vector3[] gunPositions = new Vector3[5]
    {
        new Vector3(0f, -0.12f, 0f),
        new Vector3(0.059f, -0.106f, 0f),
        new Vector3(0.1f, 0.01f, 0f),
        new Vector3(0.091f, 0.073f, 0f),
        new Vector3(0.05f, 0.116f, 0f)
    };

    private readonly float[] gunRotationsZ = new float[5] { -90f, -45f, 0f, 45f, 90f };

    private float fireTimer = 0f;
    private float lingerTimer = 0f;
    private float holdTimer = 0f;
    private bool isHoldingAfterShot = false;
    private bool isCharging = false;
    private bool isExecutingBurst = false;
    private int currentAngleIndex = 2;
    private Vector2 snappedAimDirection = Vector2.right;

    protected override void Awake()
    {
        base.Awake();
        SetAimingState(false);
        UpdateChargeMeterUI(0f, false);

        if (chargeSlider != null)
        {
            chargeSlider.minValue = 0f;
            chargeSlider.maxValue = 1f;
            chargeSlider.value = 0f;
        }
    }

    private void OnDisable()
    {
        SetAimingState(false);
        isHoldingAfterShot = false;
        isCharging = false;
        holdTimer = 0f;
        lingerTimer = 0f;
        isExecutingBurst = false;
        UpdateChargeMeterUI(0f, false);

        if (playerController != null)
        {
            playerController.moveSpeedMultiplier = 1f;
        }
    }

    protected override void Update()
    {
        base.Update();

        if (fireTimer > 0f) fireTimer -= Time.deltaTime;

        // if in cinematic cutscene, make sure arm is hidden and cancel shooting states
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsCinematicActive)
        {
            SetAimingState(false);
            isCharging = false;
            lingerTimer = 0f;
            holdTimer = 0f;
            UpdateChargeMeterUI(0f, false);
            return;
        }

        // determine if player is actively locked by shooting, burst, or linger
        bool isLocked = isCharging || isExecutingBurst || (lingerTimer > 0f);

        if (playerController != null)
        {
            playerController.isInputLocked = isLocked;
        }

        if (isLocked)
        {
            SetAimingState(true);

            if (lingerTimer > 0f && !isCharging && !isExecutingBurst)
            {
                lingerTimer -= Time.deltaTime;
                if (lingerTimer <= 0f)
                {
                    // linger ended: unlock movement and return to normal idle
                    SetAimingState(false);
                    if (playerController != null) playerController.isInputLocked = false;
                }
            }
        }
        else
        {
            SetAimingState(false);
        }
    }

    protected override void HandleAttack()
    {
        if (isExecutingBurst) return;

        bool isHolding = false;
        bool wasPressed = false;
        bool wasReleased = false;

        if (InputSystem.actions != null && InputSystem.actions["Attack"] != null)
        {
            isHolding = InputSystem.actions["Attack"].IsPressed();
            wasPressed = InputSystem.actions["Attack"].WasPressedThisFrame();
            wasReleased = InputSystem.actions["Attack"].WasReleasedThisFrame();
        }
        else if (Mouse.current != null)
        {
            isHolding = Mouse.current.leftButton.isPressed;
            wasPressed = Mouse.current.leftButton.wasPressedThisFrame;
            wasReleased = Mouse.current.leftButton.wasReleasedThisFrame;
        }

        // 1. click down: shoot first bullet instantly
        if (wasPressed && playerController != null && playerController.isGrounded && fireTimer <= 0f && !isCharging)
        {
            ShootSingleBullet();
            fireTimer = fireRate;
            isHoldingAfterShot = true;
            holdTimer = 0f;
            isCharging = false;
            UpdateChargeMeterUI(0f, false);
        }

        // 2. continue holding after first shot: start charging burst
        if (isHolding && isHoldingAfterShot && playerController != null && playerController.isGrounded)
        {
            holdTimer += Time.deltaTime;

            if (holdTimer >= chargeStartDelay)
            {
                isCharging = true;
                UpdateAimAndGunPlacement();

                // calculate charge progress from 0 to 1
                float currentCharge = holdTimer - chargeStartDelay;
                float progress = Mathf.Clamp01(currentCharge / maxChargeTime);
                UpdateChargeMeterUI(progress, true);
            }
        }

        // 3. release button: fire burst if max charged, otherwise cancel
        if (wasReleased)
        {
            if (isCharging)
            {
                float currentCharge = holdTimer - chargeStartDelay;

                if (currentCharge >= maxChargeTime)
                {
                    StartCoroutine(FireChargedBurstRoutine());
                }

                // hide charge meter immediately on release
                UpdateChargeMeterUI(0f, false);
                isCharging = false;
            }

            isHoldingAfterShot = false;
            holdTimer = 0f;
        }
    }

    private void SetAimingState(bool aiming)
    {
        if (armAndGunHolder != null)
        {
            armAndGunHolder.SetActive(aiming);
        }

        if (animator != null)
        {
            animator.SetBool("IsShooting", aiming);
        }
    }

    private void UpdateChargeMeterUI(float progress, bool isVisible)
    {
        bool shouldShow = isVisible && progress > 0.02f;

        if (chargeMeterRoot != null)
        {
            chargeMeterRoot.SetActive(shouldShow);
        }

        if (chargeSlider != null)
        {
            chargeSlider.gameObject.SetActive(shouldShow);
            chargeSlider.value = Mathf.Clamp01(progress);

            if (chargeSlider.fillRect != null && chargeSlider.fillRect.TryGetComponent<Image>(out var sliderImg))
            {
                if (progress >= 1f)
                {
                    bool toggle = Mathf.Sin(Time.time * maxFlickerSpeed) > 0f;
                    sliderImg.color = toggle ? maxChargeColor1 : maxChargeColor2;
                }
                else
                {
                    sliderImg.color = normalChargeColor;
                }
            }
        }

        if (chargeFillImage != null)
        {
            chargeFillImage.fillAmount = Mathf.Clamp01(progress);

            if (progress >= 1f)
            {
                bool toggle = Mathf.Sin(Time.time * maxFlickerSpeed) > 0f;
                chargeFillImage.color = toggle ? maxChargeColor1 : maxChargeColor2;
            }
            else
            {
                chargeFillImage.color = normalChargeColor;
            }
        }
    }

    private void UpdateAimAndGunPlacement()
    {
        Vector2 mouseWorldPos = GetMouseWorldPosition();
        Vector2 playerPos = transform.position;

        float dirSign = (mouseWorldPos.x >= playerPos.x) ? 1f : -1f;
        transform.localScale = new Vector3(dirSign * 2f, transform.localScale.y, transform.localScale.z);

        if (playerController != null)
        {
            playerController.SetFacingDirection(dirSign);
        }

        Vector2 diff = mouseWorldPos - playerPos;
        float localX = diff.x * dirSign;
        float localY = diff.y;
        float angleDeg = Mathf.Atan2(localY, localX) * Mathf.Rad2Deg;

        if (angleDeg < -67.5f) currentAngleIndex = 0;
        else if (angleDeg < -22.5f) currentAngleIndex = 1;
        else if (angleDeg <= 22.5f) currentAngleIndex = 2;
        else if (angleDeg <= 67.5f) currentAngleIndex = 3;
        else currentAngleIndex = 4;

        if (armSpriteRenderer != null && armAngleSprites.Length > currentAngleIndex && armAngleSprites[currentAngleIndex] != null)
        {
            armSpriteRenderer.sprite = armAngleSprites[currentAngleIndex];
        }

        if (gunTransform != null)
        {
            gunTransform.localPosition = gunPositions[currentAngleIndex];
            gunTransform.localRotation = Quaternion.Euler(0f, 0f, gunRotationsZ[currentAngleIndex]);
        }

        float rad = gunRotationsZ[currentAngleIndex] * Mathf.Deg2Rad;
        snappedAimDirection = new Vector2(Mathf.Cos(rad) * dirSign, Mathf.Sin(rad)).normalized;
    }

    private void ShootSingleBullet()
    {
        if (bulletPrefab == null) return;

        SetAimingState(true);
        UpdateAimAndGunPlacement();

        Vector2 mousePos = GetMouseWorldPosition();
        Vector3 spawnPos = firePoint != null ? firePoint.position : (gunTransform != null ? gunTransform.position : transform.position);

        if (shootSFX != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(shootSFX, spawnPos);

        if (muzzleFlashVFX != null)
            Instantiate(muzzleFlashVFX, spawnPos, Quaternion.identity);

        GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
        if (bullet.TryGetComponent<PlayerProjectile>(out var proj))
        {
            proj.damage = bulletDamage;
            proj.speed = bulletSpeed;
            proj.LaunchCurved(snappedAimDirection, mousePos);
        }

        // root player for exact 0.1s linger
        lingerTimer = postAttackLingerDuration;
    }

    private IEnumerator FireChargedBurstRoutine()
    {
        isExecutingBurst = true;
        SetAimingState(true);

        Vector2 mousePos = GetMouseWorldPosition();
        Vector3 spawnPos = firePoint != null ? firePoint.position : (gunTransform != null ? gunTransform.position : transform.position);

        for (int i = 0; i < burstBulletCount; i++)
        {
            UpdateAimAndGunPlacement();

            if (shootSFX != null && AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(shootSFX, spawnPos, 1.2f);

            if (muzzleFlashVFX != null)
                Instantiate(muzzleFlashVFX, spawnPos, Quaternion.identity);

            GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
            if (bullet.TryGetComponent<PlayerProjectile>(out var proj))
            {
                proj.damage = burstBulletDamage;
                proj.speed = bulletSpeed * 1.15f;
                proj.LaunchCurved(snappedAimDirection, mousePos);
            }

            yield return new WaitForSeconds(burstFireDelay);
        }

        isExecutingBurst = false;
        lingerTimer = postAttackLingerDuration;
    }
}