using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Scriptable Objects/EnemyData")]
public class EnemyData : ScriptableObject
{
    public string enemyName = "Enemy";
    public int maxHealth = 100;
    public float moveSpeed = 3f;
    public float chaseSpeed = 5f;
    public float chaseRange = 8f;
    public float attackRange = 5f;
    public float attackCooldown = 2f;
    public int attackDamage = 15;

    [Header("Audio and VFX")]
    public AudioClip attackSound;
    public AudioClip deathSound;
    public GameObject deathVFX;
}