using UnityEngine;

[System.Serializable]
public class StateTransition
{
    public StateDecision decision;
    public State trueState;
    public State falseState;
}