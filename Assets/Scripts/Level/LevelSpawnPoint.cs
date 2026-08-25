using UnityEngine;

public class LevelSpawnPoint : MonoBehaviour
{
    public static LevelSpawnPoint Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void OnDrawGizmos()
    {
        // draw green marker in scene view
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 1.5f);
    }
}