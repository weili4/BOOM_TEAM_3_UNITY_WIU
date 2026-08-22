using UnityEngine;

public class PlaneProjectile : HazardBase
{
    private float speed = 16f;
    private Camera mainCam;

    public void Initialize(float flySpeed)
    {
        speed = flySpeed;
        mainCam = Camera.main;
    }

    private void Update()
    {
        // fly horizontally to the left
        transform.position += Vector3.left * speed * Time.deltaTime;

        // despawn when past the left edge of the screen
        if (mainCam != null)
        {
            Vector3 viewPos = mainCam.WorldToViewportPoint(transform.position);
            if (viewPos.x < -0.2f)
            {
                Destroy(gameObject);
            }
        }
    }
}