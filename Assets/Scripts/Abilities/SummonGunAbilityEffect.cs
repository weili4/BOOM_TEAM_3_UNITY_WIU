using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

[CreateAssetMenu(fileName = "SummonGunAbility", menuName = "Scriptable Objects/Effects/SummonGunAbility")]
public class SummonGunAbilityEffect : AbilityEffect
{
    public GameObject GunPrefab;

    [SerializeField]
    private Vector3[] GunOffsets = new Vector3[]
    {
            new Vector3(0.5f, -0.2f, 0),
            new Vector3(0.35f, 0.25f, 0),
            new Vector3(-0.5f, -0.2f, 0),
            new Vector3(-0.35f, 0.25f, 0)
    };

    private GameObject[] GunObjects;
    private Vector3[] GunPositions;

    public override void Activate(GameObject user, Vector2 mouseWorldPos)
    {
        GunObjects = new GameObject[GunOffsets.Length];
        GunPositions = new Vector3[GunOffsets.Length];

        for (int i = 0; i < GunObjects.Length; i++)
        {
            GunPositions[i] = user.transform.position + GunOffsets[i];
            GunObjects[i] = Instantiate(GunPrefab, GunPositions[i], Quaternion.identity);
            GunObjects[i].GetComponent<AutoAimGun_Behaviour>().SetFollowTarget(user.transform, GunOffsets[i]);
        }

        user.GetComponent<Animator>().SetTrigger("IsAbilityOneUsed");
    }

    public override void Deactivate(GameObject user)
    {
        for (int i = 0; i < GunObjects.Length; i++)
        {
            GunObjects[i].GetComponent<AutoAimGun_Behaviour>().SpawnParticleOnDestroy();
            Destroy(GunObjects[i]);
        }
        
    }
}