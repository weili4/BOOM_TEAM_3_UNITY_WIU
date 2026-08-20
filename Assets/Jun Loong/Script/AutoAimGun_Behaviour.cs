using UnityEngine;

public class AutoAimGun_Behaviour : MonoBehaviour
{
    /*===================================================================================================================*/
    // Spawn / Lerp Movement
    private Vector3 SpawnPointPosition;
    private Vector3 CurrentPosition;
    [SerializeField] private float moveSpeed = 1f;
    private float lerpTime = 0f;
    /*===================================================================================================================*/

    /*===================================================================================================================*/
    // Enemy Detection
    [SerializeField] private float ScanRangeForEnemy = 5f;
    [SerializeField] private LayerMask EnemyLayer;
    private Transform CurrentEnemyTarget;
    /*===================================================================================================================*/

    /*===================================================================================================================*/
    // Aiming Rotation
    private Vector2 currentDirection = Vector2.right;
    [SerializeField] private float RotatingSpeed = 100f;
    private float RotateTime = 0f;
    private SpriteRenderer GunSprite;
    /*===================================================================================================================*/

    /*===================================================================================================================*/
    // Projectile
    [SerializeField] private GameObject GunFireEffect;
    private SpriteRenderer FireEffectSprite;
    [SerializeField] private GameObject ProjectilePrefab;
    [SerializeField] private LayerMask EnemyAndGroundLayer;
    [SerializeField] private float FiringCooldown = 0.5f;
    private float FiringTimer = 0.0f;
    private bool IsAlreadyFired = false;
    /*===================================================================================================================*/

    /*===================================================================================================================*/
    // Follow Player
    private Transform playerTransform;
    private Vector3 followOffset;
    /*===================================================================================================================*/

    /*===================================================================================================================*/
    void Start()
    {
        GunSprite = GetComponent<SpriteRenderer>();
        FireEffectSprite = GunFireEffect.GetComponent<SpriteRenderer>();
        GunFireEffect.SetActive(false);
    }
    /*===================================================================================================================*/


    [SerializeField] private GameObject DestroyEffectPrefab;



    /*===================================================================================================================*/
    void Update()
    {
        if (lerpTime < 1f)
        {
            lerpTime += Time.deltaTime * moveSpeed;

            Vector3 targetPosition = playerTransform.position + SpawnPointPosition;
            Vector3 startPosition = playerTransform.position + CurrentPosition;

            transform.position = Vector3.Lerp(startPosition, targetPosition, lerpTime);
        }
        else
        {
            transform.position = playerTransform.position + SpawnPointPosition;
            if (!IsAlreadyFired)
            {
                RotateTowardTarget();
            }
        }

        if(IsAlreadyFired)
        {
            if(FiringTimer < FiringCooldown)
            {
                FiringTimer += Time.deltaTime;
            }
            else
            {
                IsAlreadyFired = false;
                FiringTimer = 0.0f;
                GunFireEffect.SetActive(false);
                RotateTowardTarget();
            }
        }
    }
    /*===================================================================================================================*/

    /*===================================================================================================================*/
    private void RotateTowardTarget()
    {
        Collider2D[] EnemiesTarget = Physics2D.OverlapCircleAll(transform.position, ScanRangeForEnemy, EnemyLayer);
        if (EnemiesTarget.Length > 0)
        {
            Transform ClosestEnemy = EnemiesTarget[0].transform;
            float ClosestDist = Vector2.Distance(transform.position, ClosestEnemy.position);

            foreach (var Enemy in EnemiesTarget)
            {
                float distance = Vector2.Distance(transform.position, Enemy.transform.position);
                if (distance < ClosestDist)
                {
                    ClosestDist = distance;
                    ClosestEnemy = Enemy.transform;
                }
            }

            CurrentEnemyTarget = ClosestEnemy;
            Vector2 TargetDirection = Vector2.zero;

            if (RotateTime < 1.0f)
            {
                RotateTime += RotatingSpeed * Mathf.Deg2Rad * Time.deltaTime;
                TargetDirection = ((Vector2)CurrentEnemyTarget.position - ((Vector2)transform.position)).normalized;
                currentDirection = Vector3.RotateTowards(currentDirection, TargetDirection, RotateTime, 0f);

                /*===================================================================================================*/
                // Check if the Gun is aiming to the left then flip the sprite
                if (currentDirection.x < 0.0f)
                {
                    GunSprite.flipY = true;
                }
                /*===================================================================================================*/

                float angle = Mathf.Atan2(currentDirection.y, currentDirection.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
            else
            {
                SpawnBullet(currentDirection);
                GunFireEffect.SetActive(true);
                IsAlreadyFired = true;
                RotateTime = 0f;
            }
        }
    }
    /*===================================================================================================================*/

        /*===================================================================================================================*/
    private void SpawnBullet(Vector2 direction)
    {
        GameObject bullet = Instantiate(ProjectilePrefab, transform.position, Quaternion.identity);
        if (bullet.TryGetComponent<PlayerProjectile>(out PlayerProjectile projectile))
        {
            projectile.HitLayers = EnemyAndGroundLayer;
            projectile.Launch(direction);
        }
    }
    /*===================================================================================================================*/

    /*===================================================================================================================*/
    // called by SummonGunAbilityEffect right after spawning, to tell this gun who to follow
    public void SetFollowTarget(Transform target, Vector3 offset)
    {
        playerTransform = target;
        followOffset = offset;

        SpawnPointPosition = followOffset; // the real intended offset from the player
        CurrentPosition = new Vector3(SpawnPointPosition.x, SpawnPointPosition.y - 2, SpawnPointPosition.z);
    }
    /*===================================================================================================================*/

    /*===================================================================================================================*/
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, ScanRangeForEnemy);
    }
    /*===================================================================================================================*/

    public void SpawnParticleOnDestroy()
    {
        Instantiate(DestroyEffectPrefab, transform.position, Quaternion.identity);
    }
}
