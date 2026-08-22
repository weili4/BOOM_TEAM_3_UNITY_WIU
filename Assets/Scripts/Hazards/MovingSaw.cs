using System.Collections.Generic;
using UnityEngine;

public class MovingSaw : HazardBase
{
    public enum MovementMode { Loop, PingPong }

    [Header("path settings")]
    [SerializeField] private MovementMode movementMode = MovementMode.PingPong;
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float spinSpeed = 360f;

    // enter x and y coordinates directly in the inspector
    [SerializeField] private List<Vector2> waypoints = new List<Vector2>();

    private int currentIndex = 0;
    private bool isReversing = false;

    private void Start()
    {
        // if no waypoints were set, add current position as starting point
        if (waypoints.Count == 0)
        {
            waypoints.Add(transform.position);
        }
    }

    private void Update()
    {
        // spin blade sprite
        transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);

        if (waypoints == null || waypoints.Count < 2) return;

        Vector2 targetPoint = waypoints[currentIndex];
        transform.position = Vector2.MoveTowards(transform.position, targetPoint, moveSpeed * Time.deltaTime);

        if (Vector2.Distance(transform.position, targetPoint) < 0.05f)
        {
            AdvanceWaypoint();
        }
    }

    private void AdvanceWaypoint()
    {
        if (movementMode == MovementMode.Loop)
        {
            currentIndex = (currentIndex + 1) % waypoints.Count;
        }
        else if (movementMode == MovementMode.PingPong)
        {
            if (!isReversing)
            {
                currentIndex++;
                if (currentIndex >= waypoints.Count)
                {
                    currentIndex = waypoints.Count - 2;
                    isReversing = true;
                }
            }
            else
            {
                currentIndex--;
                if (currentIndex < 0)
                {
                    currentIndex = 1;
                    isReversing = false;
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Count < 2) return;

        Gizmos.color = Color.yellow;
        for (int i = 0; i < waypoints.Count; i++)
        {
            Gizmos.DrawWireSphere(waypoints[i], 0.25f);

            int next = (i + 1) % waypoints.Count;
            if (movementMode == MovementMode.PingPong && i == waypoints.Count - 1) continue;

            Gizmos.DrawLine(waypoints[i], waypoints[next]);
        }
    }
}