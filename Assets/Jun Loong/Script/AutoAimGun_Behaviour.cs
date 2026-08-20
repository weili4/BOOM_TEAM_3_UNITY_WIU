using UnityEngine;

public class AutoAimGun_Behaviour : MonoBehaviour
{
    /*===================================================================================================================*/
    // Spawn / Lerp Movement
    private Vector3 SpawnPointPosition;
    private Vector3 CurrentPosition;
    [SerializeField] private float moveSpeed = 2f;
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
    private bool IsAlreadyShot = false;
    /*===================================================================================================================*/

    /*===================================================================================================================*/
    void Start()
    {
        SpawnPointPosition = transform.position;
        CurrentPosition = new Vector3(SpawnPointPosition.x, SpawnPointPosition.y - 2, SpawnPointPosition.z);
        transform.position = CurrentPosition;

        GunSprite = GetComponent<SpriteRenderer>();
        FireEffectSprite = GunFireEffect.GetComponent<SpriteRenderer>();
        GunFireEffect.SetActive(false);
    }
    /*===================================================================================================================*/

    /*===================================================================================================================*/
    void Update()
    {
        if (lerpTime < 1f)
        {
            lerpTime += Time.deltaTime * moveSpeed;
            transform.position = Vector3.Lerp(CurrentPosition, SpawnPointPosition, lerpTime);
        }
        else
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
                    if (!IsAlreadyShot)
                    {
                        SpawnBullet(currentDirection);
                        IsAlreadyShot = true;
                        GunFireEffect.SetActive(true);
                    }

                }
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
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, ScanRangeForEnemy);
    }
    /*===================================================================================================================*/
}
