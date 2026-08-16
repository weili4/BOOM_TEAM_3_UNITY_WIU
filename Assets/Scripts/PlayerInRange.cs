using UnityEngine;

[CreateAssetMenu(fileName = "PlayerInRange", menuName = "Scriptable Objects/Decision/PlayerInRange")]
public class PlayerInRange : StateDecision
{
    public float range = 0;
    private string targetTag = "Player";

    public override bool Decide(StateController controller)
    {
        var targetInScene = GameObject.FindGameObjectWithTag(targetTag);
        bool inRange = Vector3.Distance(controller.transform.position, targetInScene.transform.position) <= range;
        return inRange;
    }
}