using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "State", menuName = "Scriptable Objects/State")]
public class State : ScriptableObject
{
    [SerializeField] private List<StateAction> initializeActions;
    [SerializeField] private List<StateAction> executeActions;
    [SerializeField] private List<StateAction> endActions;
    [SerializeField] private List<StateTransition> transitions;

    public void Initialize(StateController controller)
    {
        if (initializeActions == null) return;
        foreach (StateAction action in initializeActions) // run actions once when state start
        {
            if (action != null) action.Act(controller);
        }
    }

    public void Execute(StateController controller)
    {
        if (executeActions == null) return;
        foreach (StateAction action in executeActions) // run actions while state is active
        {
            if (action != null) action.Act(controller);
        }
    }

    public void End(StateController controller)
    {
        if (endActions == null) return;
        foreach (StateAction action in endActions) // run actions once before state end
        {
            if (action != null) action.Act(controller);
        }
    }

    public void CheckTransitions(StateController controller)
    {
        if (transitions == null) return;
        foreach (StateTransition transition in transitions)
        {
            if (transition == null || transition.decision == null) continue;

            bool decisionSucceeded = transition.decision.Decide(controller);
            if (decisionSucceeded)
            {
                controller.TransitionToState(transition.trueState);
            }
            else
            {
                controller.TransitionToState(transition.falseState);
            }
        }
    }
}