using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "GlidingAbilityEffect", menuName = "Party/Effects/GlidingAbilityEffect")]
public class GlidingAbilityEffect : AbilityEffect
{
    [SerializeField] private float SetFallMultiplier = 0.50f;
    [SerializeField] private GameObject windVFXPrefab;
    [SerializeField] private Vector3 windVFXOffset = new Vector3(0f, 0.5f, 0f);

    private GameObject currentVFXInstance;
    private Coroutine CurrentCoroutine;

    public override void Activate(GameObject user, Vector2 mouseWorldPos)
    {
        if (user == null) return;

        // Cache components once on activation
        if (!user.TryGetComponent<PlayerController>(out var Player_Controller) || 
            !user.TryGetComponent<Rigidbody2D>(out var Player_Rigidbody)) return;

        Player_Controller.fallMultiplier = SetFallMultiplier;

        if (user.TryGetComponent<Animator>(out var anim))
        {
            anim.SetBool("IsGliding" , true);
        }

        var Gliding = user.GetComponent<MonoBehaviour>();
        if (Gliding != null)
        {
            if (CurrentCoroutine != null)
            {
                Gliding.StopCoroutine(CurrentCoroutine);
            }

            CurrentCoroutine = Gliding.StartCoroutine(ApplyWindEffect(user , Player_Controller , Player_Rigidbody));
        }
    }

    private IEnumerator ApplyWindEffect(GameObject user, PlayerController Player_Controller, Rigidbody2D Player_Rigidbody)
    {
        while (true)
        {
            if (!Player_Controller.isGrounded && Player_Rigidbody.linearVelocityY < -0.1f)
            {
                if (windVFXPrefab != null && currentVFXInstance == null)
                {
                    Debug.Log("Still Spawning");
                    currentVFXInstance = Instantiate(windVFXPrefab, user.transform);
                    currentVFXInstance.transform.localPosition = windVFXOffset;
                }
            }
            else
            {
                // Destroy particle immediately when on ground or jumping up
                if (currentVFXInstance != null)
                {
                    Destroy(currentVFXInstance);
                    currentVFXInstance = null;
                }
            }

            yield return null;
        }
    }

    public override void Deactivate(GameObject user)
    {
        if (user == null) return;

        PlayerController controller = user.GetComponent<PlayerController>();
        if (controller == null) return;

        controller.fallMultiplier = 2.5f;

        if (user.TryGetComponent<Animator>(out var anim))
        {
            anim.SetBool("IsGliding", false);
        }

        if (currentVFXInstance != null)
        {
            Destroy(currentVFXInstance);
            currentVFXInstance = null;
        }

        var Gliding = user.GetComponent<MonoBehaviour>();
        if (Gliding != null)
        {
            if (CurrentCoroutine != null)
            {
                Gliding.StopCoroutine(CurrentCoroutine);
                CurrentCoroutine = null;
            }
        }
    }
}