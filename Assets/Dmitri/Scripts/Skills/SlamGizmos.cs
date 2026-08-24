using UnityEngine;

public class SlamGizmoDrawer : MonoBehaviour
{
    private float radius;
    private Color gizmoColor;

    public void Initialize(float slamRadius, Color color, float displayDuration)
    {
        radius = slamRadius;
        gizmoColor = color;

        // Auto-destroy helper script after the display duration ends
        Destroy(this, displayDuration);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}