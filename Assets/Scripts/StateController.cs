using UnityEngine;

public class StateController : MonoBehaviour
{
    public State currentState;
    public State remainState;

    void Start()
    {
        currentState.Initialize(this);
    }

    void Update()
    {
        currentState.Execute(this);
        currentState.CheckTransitions(this);
    }

    public void TransitionToState(State nextState)
    {
        if (nextState != remainState)
        {
            currentState.End(this);
            currentState = nextState;
            currentState.Initialize(this);
        }
    }
}