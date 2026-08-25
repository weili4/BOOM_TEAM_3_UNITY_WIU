using UnityEngine;


[CreateAssetMenu(fileName = "BarrierFieldAbility", menuName = "Party/Effects/BarrierFieldAbility")]
public class BarrierFieldAbilityEffect : AbilityEffect
{
    [SerializeField] private GameObject shieldPrefab;
    [SerializeField] private float domeDuration = 10f;

    private GameObject activeShield;
    public override void Activate(GameObject user, Vector2 mouseWorldPos)
    {
        if (shieldPrefab == null || user == null) return;

        activeShield = Instantiate(shieldPrefab, user.transform.position, Quaternion.identity);

        if (activeShield.TryGetComponent<ShieldDome>(out var dome))
        {
            dome.Invoke("DestroySelf", domeDuration);
        }
    }

    public override void Deactivate(GameObject user)
    {
        //if (activeShield != null)
        //{
        //    Destroy(activeShield);
        //}
    }
}
