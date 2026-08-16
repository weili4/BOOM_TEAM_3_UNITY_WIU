using Pathfinding;
using UnityEngine;

[CreateAssetMenu(fileName = "PathfindAction", menuName = "Scriptable Objects/Actions/PathfindAction")]
public class PathfindAction : StateAction
{
    public string targetTag = "";

    public override void Act(StateController controller)
    {
        if (controller.TryGetComponent<AIDestinationSetter>(out AIDestinationSetter aiDestSetter))
        {
            if (targetTag.Equals("")) aiDestSetter.target = null;
            else
            {
                var targetInScene = GameObject.FindGameObjectWithTag(targetTag);
                aiDestSetter.target = targetInScene.transform;
            }
        }
    }
}