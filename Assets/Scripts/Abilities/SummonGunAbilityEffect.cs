using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SummonGunAbility", menuName = "Party/Effects/SummonGunAbility")]
public class SummonGunAbilityEffect : AbilityEffect
{
    public GameObject gunPrefab;

    [SerializeField]
    private Vector3[] gunOffsets = new Vector3[]
    {
        new Vector3(0.5f, -0.2f, 0),
        new Vector3(0.35f, 0.25f, 0),
        new Vector3(-0.5f, -0.2f, 0),
        new Vector3(-0.35f, 0.25f, 0)
    };

    private Dictionary<GameObject, List<GameObject>> spawnedGunsPerUser = new Dictionary<GameObject, List<GameObject>>();

    public override void Activate(GameObject user, Vector2 mouseWorldPos)
    {
        if (user == null || gunPrefab == null) return;

        Deactivate(user);

        List<GameObject> guns = new List<GameObject>();

        for (int i = 0; i < gunOffsets.Length; i++)
        {
            GunPositions[i] = user.transform.position + GunOffsets[i];
            GunObjects[i] = Instantiate(GunPrefab, GunPositions[i], Quaternion.identity);
            GunObjects[i].GetComponent<AutoAimGun_Behaviour>().SetFollowTarget(user.transform, GunOffsets[i]);
        }

        spawnedGunsPerUser[user] = guns;

        if (user.TryGetComponent<Animator>(out var anim))
        {
            anim.SetTrigger("IsAbilityOneUsed");
        }
    }

    public override void Deactivate(GameObject user)
    {
        if (user != null && spawnedGunsPerUser.TryGetValue(user, out var guns))
        {
            GunObjects[i].GetComponent<AutoAimGun_Behaviour>().SpawnParticleOnDestroy();
            Destroy(GunObjects[i]);
        }
    }
}