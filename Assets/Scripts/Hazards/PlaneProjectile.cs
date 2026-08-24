using UnityEngine;

public class PlaneProjectile : HazardBase
{
    private float speed = 16f;
    private Camera mainCam;
    private ParticleSystem trailParticles;
    private bool isDespawning = false;

    private void Awake()
    {
        trailParticles = GetComponentInChildren<ParticleSystem>();
    }

    public void Initialize(float flySpeed)
    {
        speed = flySpeed;
        mainCam = Camera.main;
    }

    private void Update()
    {
        if (isDespawning) return;

        // fly horizontally to the left
        transform.position += Vector3.left * speed * Time.deltaTime;

        if (mainCam == null) mainCam = Camera.main;

        // check if plane has passed the left screen edge
        if (mainCam != null)
        {
            Vector3 viewPos = mainCam.WorldToViewportPoint(transform.position);

            if (viewPos.x < -0.25f)
            {
                DespawnPlane();
            }
        }
    }

    private void DespawnPlane()
    {
        isDespawning = true;

        // unparent trail and let existing particles fade out naturally in the sky
        if (trailParticles != null)
        {
            trailParticles.transform.SetParent(null);
            trailParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);

            // calculate particle lifetime so it destroys itself after smoke fades
            float maxLifetime = trailParticles.main.startLifetime.constantMax;
            Destroy(trailParticles.gameObject, maxLifetime + 0.5f);
        }

        // destroy plane body immediately
        Destroy(gameObject);
    }
}