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
            Vector3 spawnPos = user.transform.position + gunOffsets[i];
            GameObject gun = Instantiate(gunPrefab, spawnPos, Quaternion.identity);
            guns.Add(gun);
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
            foreach (var gun in guns)
            {
                if (gun != null) Destroy(gun);
            }
            spawnedGunsPerUser.Remove(user);
        }
    }
}