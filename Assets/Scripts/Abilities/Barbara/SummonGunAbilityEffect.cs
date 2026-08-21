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

    // reminder: keep tracked guns inside a dictionary per user so scriptable objects dont leak or break between party swaps
    private Dictionary<GameObject, List<GameObject>> spawnedGunsPerUser = new Dictionary<GameObject, List<GameObject>>();

    public override void Activate(GameObject user, Vector2 mouseWorldPos)
    {
        if (user == null || gunPrefab == null) return;

        // always clean up old guns first before spawning new ones
        Deactivate(user);

        List<GameObject> guns = new List<GameObject>();

        for (int i = 0; i < gunOffsets.Length; i++)
        {
            // calculate local spawn position based on current user transform
            Vector3 spawnPos = user.transform.position + gunOffsets[i];
            GameObject gunObj = Instantiate(gunPrefab, spawnPos, Quaternion.identity);

            // pass follow target and offset to the gun script
            if (gunObj.TryGetComponent<AutoAimGun_Behaviour>(out var gunScript))
            {
                gunScript.SetFollowTarget(user.transform, gunOffsets[i]);
            }

            // remember to add each gun to the list so deactivate can find them later
            guns.Add(gunObj);
        }

        // save spawned guns list for this specific character
        spawnedGunsPerUser[user] = guns;

        if (user.TryGetComponent<Animator>(out var anim))
        {
            anim.SetTrigger("IsAbilityOneUsed");
        }
    }

    public override void Deactivate(GameObject user)
    {
        if (user == null) return;

        // iterate through all guns in this user's list safely
        if (spawnedGunsPerUser.TryGetValue(user, out var guns))
        {
            foreach (var gun in guns)
            {
                if (gun != null)
                {
                    // play destroy vfx on gun before destroying it
                    if (gun.TryGetComponent<AutoAimGun_Behaviour>(out var gunScript))
                    {
                        gunScript.SpawnParticleOnDestroy();
                    }
                    Destroy(gun);
                }
            }

            // clean up dictionary key so memory stays clear
            spawnedGunsPerUser.Remove(user);
        }
    }
}