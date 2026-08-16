using Pathfinding;
using UnityEngine;

public class DroneController : MonoBehaviour
{
    // ALLY DRONE CONTROLLER

    public Transform playerTransform;

    [Header("MOVEMENT SETTINGS")]
    public float droneSpeed = 12f;
    public float enemyStandoffDistance = 2.5f;
    public float playerFollowDistance = 1.2f;

    [Header("COMBAT SETTINGS")]
    public float scanRange = 7f;
    public LayerMask enemyLayer;
    public float laserDamagePerSecond = 30f;

    [Header("LOOPING LASER AUDIO")]
    public AudioClip laserLoopSound; // laser sfx
    private AudioSource laserAudioSource;

    private AIDestinationSetter destSetter;
    private AIPath aiPath;
    private Transform currentTarget;
    private LineRenderer laserLine;
    private float damageTimer = 0f;

    void Awake()
    {
        destSetter = GetComponent<AIDestinationSetter>();
        aiPath = GetComponent<AIPath>();
        laserLine = GetComponent<LineRenderer>();

        // setup looping audio source
        laserAudioSource = gameObject.AddComponent<AudioSource>();
        laserAudioSource.loop = true;
        laserAudioSource.playOnAwake = false;
        laserAudioSource.spatialBlend = 0f;

        if (aiPath != null)
        {
            aiPath.enableRotation = false;
            aiPath.orientation = OrientationMode.YAxisForward;
            aiPath.gravity = Vector3.zero;
            aiPath.maxSpeed = droneSpeed;
        }

        if (laserLine != null)
            laserLine.enabled = false;
    }

    public void SetPlayer(Transform player)
    {
        playerTransform = player;
        if (destSetter != null) destSetter.target = playerTransform;
    }

    void Update()
    {
        // keep drone flat so rotation and z position dont affect movement
        transform.rotation = Quaternion.identity;
        Vector3 pos = transform.position;
        pos.z = 0;
        transform.position = pos;

        if (playerTransform == null) return;

        if (aiPath != null) aiPath.maxSpeed = droneSpeed;

        // scan for enemies inside drone attack range
        Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, scanRange, enemyLayer); // scan for enemies inside drone attack range
        if (enemies.Length > 0)
        {
            // find closest enemy so drone always attack nearest target
            Transform closestEnemy = enemies[0].transform;
            float closestDist = Vector2.Distance(transform.position, closestEnemy.position);

            foreach (var enemy in enemies)
            {
                float dist = Vector2.Distance(transform.position, enemy.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestEnemy = enemy.transform;
                }
            }

            currentTarget = closestEnemy;
            if (destSetter != null) destSetter.target = currentTarget;

            // keep drone at safe distance instead of moving directly into enemy
            if (aiPath != null) aiPath.endReachedDistance = enemyStandoffDistance;

            if (laserLine != null)
            {
                laserLine.enabled = true;
                laserLine.SetPosition(0, transform.position);
                laserLine.SetPosition(1, currentTarget.position);
            }

            // PLAY LOOPING LASER SFX
            if (laserLoopSound != null)
            {
                if (laserAudioSource.clip != laserLoopSound)
                {
                    laserAudioSource.clip = laserLoopSound;
                }

                if (!laserAudioSource.isPlaying)
                {
                    laserAudioSource.Play();
                }
            }

            ApplyLaserDamage(currentTarget);
        }
        else
        {
            currentTarget = null;
            if (destSetter != null) destSetter.target = playerTransform;
            if (aiPath != null) aiPath.endReachedDistance = playerFollowDistance;

            if (laserLine != null) laserLine.enabled = false;

            // STOP LOOPING LASER SFX
            if (laserAudioSource != null && laserAudioSource.isPlaying)
            {
                laserAudioSource.Stop();
            }
        }
    }

    private void ApplyLaserDamage(Transform target)
    {
        if (target != null && target.TryGetComponent<Damageable>(out Damageable enemy))
        {
            damageTimer += Time.deltaTime;
            if (damageTimer >= 0.1f)
            {
                // convert damage per second into damage for each 0.1 second tick
                int damageToDeal = Mathf.Max(1, Mathf.RoundToInt(laserDamagePerSecond * 0.1f));
                enemy.TakeDamage(damageToDeal);
                damageTimer = 0f;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, scanRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, enemyStandoffDistance);
    }
}