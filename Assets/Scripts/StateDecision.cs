using UnityEngine;

[CreateAssetMenu(fileName = "StateDecision", menuName = "Scriptable Objects/StateDecision")]
public abstract class StateDecision : ScriptableObject
{
    public abstract bool Decide(StateController controller);
}