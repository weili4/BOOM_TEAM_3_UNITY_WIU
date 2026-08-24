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

        // trigger summon animation (playercontroller detects "Summon Gun Attack" and locks movement automatically)
        if (user.TryGetComponent<Animator>(out var anim))
        {
            anim.SetTrigger("IsAbilityOneUsed");
        }

        // stop existing velocity immediately
        if (user.TryGetComponent<Rigidbody2D>(out var rb))
        {
            rb.linearVelocityX = 0f;
        }

        List<GameObject> guns = new List<GameObject>();

        for (int i = 0; i < gunOffsets.Length; i++)
        {
            Vector3 spawnPos = user.transform.position + gunOffsets[i];
            GameObject gunObj = Instantiate(gunPrefab, spawnPos, Quaternion.identity);

            if (gunObj.TryGetComponent<AutoAimGun_Behaviour>(out var gunScript))
            {
                gunScript.SetFollowTarget(user.transform, gunOffsets[i]);
            }

            guns.Add(gunObj);
        }

        spawnedGunsPerUser[user] = guns;
    }

    public override void Deactivate(GameObject user)
    {
        if (user == null) return;

        if (spawnedGunsPerUser.TryGetValue(user, out var guns))
        {
            foreach (var gun in guns)
            {
                if (gun != null)
                {
                    if (gun.TryGetComponent<AutoAimGun_Behaviour>(out var gunScript))
                    {
                        gunScript.SpawnParticleOnDestroy();
                    }
                    Destroy(gun);
                }
            }

            spawnedGunsPerUser.Remove(user);
        }
    }
}