using System.Collections;
using UnityEngine;

public class PlaneSpawner : MonoBehaviour
{
    [Header("plane settings")]
    [SerializeField] private GameObject planePrefab;
    [SerializeField] private float spawnInterval = 5f;
    [SerializeField] private float warningDuration = 1.0f;
    [SerializeField] private float planeSpeed = 16f;

    [Header("camera tracking warning line")]
    [SerializeField] private LineRenderer warningLine;

    private Camera mainCam;
    private bool isPlayerInZone = false;
    private Coroutine spawnLoopRoutine;

    private void Start()
    {
        if (warningLine != null) warningLine.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") || (collision.transform.root != null && collision.transform.root.CompareTag("Player")))
        {
            isPlayerInZone = true;
            if (spawnLoopRoutine == null)
            {
                spawnLoopRoutine = StartCoroutine(PlaneSpawnLoop());
            }
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !isPlayerInZone)
        {
            isPlayerInZone = true;
            if (spawnLoopRoutine == null)
            {
                spawnLoopRoutine = StartCoroutine(PlaneSpawnLoop());
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInZone = false;
            if (spawnLoopRoutine != null)
            {
                StopCoroutine(spawnLoopRoutine);
                spawnLoopRoutine = null;
            }
            if (warningLine != null) warningLine.enabled = false;
        }
    }

    private IEnumerator PlaneSpawnLoop()
    {
        while (isPlayerInZone)
        {
            yield return new WaitForSeconds(spawnInterval);
            if (!isPlayerInZone) break;

            if (mainCam == null) mainCam = Camera.main;

            GameObject player = null;
            if (PartyManager.Instance != null && PartyManager.Instance.ActivePlayerObj != null)
                player = PartyManager.Instance.ActivePlayerObj;
            else
                player = GameObject.FindGameObjectWithTag("Player");

            if (player == null || mainCam == null) continue;

            // lock Y to player's current height
            float lockedY = player.transform.position.y;

            // show right screen edge warning line tracking the camera
            if (warningLine != null) warningLine.enabled = true;
            float elapsed = 0f;

            while (elapsed < warningDuration)
            {
                elapsed += Time.deltaTime;
                if (mainCam == null) mainCam = Camera.main;

                if (mainCam != null && warningLine != null)
                {
                    Vector3 rightEdge = mainCam.ViewportToWorldPoint(new Vector3(1f, 0.5f, 10f));
                    Vector3 leftEdge = mainCam.ViewportToWorldPoint(new Vector3(0.85f, 0.5f, 10f));

                    warningLine.SetPosition(0, new Vector3(leftEdge.x, lockedY, 0f));
                    warningLine.SetPosition(1, new Vector3(rightEdge.x, lockedY, 0f));
                }

                yield return null;
            }

            if (warningLine != null) warningLine.enabled = false;

            // spawn plane offscreen right
            if (mainCam != null && planePrefab != null)
            {
                Vector3 spawnPos = mainCam.ViewportToWorldPoint(new Vector3(1.15f, 0.5f, 10f));
                spawnPos.y = lockedY;
                spawnPos.z = 0f;

                GameObject plane = Instantiate(planePrefab, spawnPos, Quaternion.identity);
                if (plane.TryGetComponent<PlaneProjectile>(out var p))
                {
                    p.Initialize(planeSpeed);
                }
            }
        }
    }
}