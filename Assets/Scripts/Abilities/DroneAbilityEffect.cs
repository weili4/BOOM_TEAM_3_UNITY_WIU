using UnityEngine;

[CreateAssetMenu(fileName = "DroneAbility", menuName = "Scriptable Objects/Effects/DroneAbility")]
public class DroneAbilityEffect : AbilityEffect
{
    public GameObject dronePrefab;

    public override void Activate(GameObject user, Vector2 mouseWorldPos)
    {
        // check or attach runtime drone holder component on the user
        ActiveDroneHolder holder = user.GetComponent<ActiveDroneHolder>();
        if (holder == null)
        {
            holder = user.AddComponent<ActiveDroneHolder>();
        }

        if (holder.currentDrone == null && dronePrefab != null)
        {
            GameObject drone = Instantiate(dronePrefab, user.transform.position, Quaternion.identity);
            if (drone.TryGetComponent<DroneController>(out DroneController droneCtrl))
            {
                droneCtrl.SetPlayer(user.transform);
            }
            holder.currentDrone = drone;
        }
    }

    public override void Deactivate(GameObject user)
    {
        ActiveDroneHolder holder = user.GetComponent<ActiveDroneHolder>();
        if (holder != null && holder.currentDrone != null)
        {
            Destroy(holder.currentDrone);
            holder.currentDrone = null;
        }
    }
}

// runtime helper component attached dynamically to the player
public class ActiveDroneHolder : MonoBehaviour
{
    public GameObject currentDrone;
}