using UnityEngine;

[CreateAssetMenu(fileName = "StateAction", menuName = "Scriptable Objects/StateAction")]
public abstract class StateAction : ScriptableObject
{
    public abstract void Act(StateController controller); // each state action must have its own action logic or it BREAKS
}