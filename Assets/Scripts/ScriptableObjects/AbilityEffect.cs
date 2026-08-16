using UnityEngine;

public abstract class AbilityEffect : ScriptableObject
{
    public abstract void Activate(GameObject user, Vector2 mouseWorldPos);
    public abstract void Deactivate(GameObject user);
}