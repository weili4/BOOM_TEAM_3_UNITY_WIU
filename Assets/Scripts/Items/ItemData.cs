using UnityEngine;

[CreateAssetMenu(fileName = "NewItemData", menuName = "Scriptable Objects/ItemData")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite itemImage;
    public float cooldownTime = 3f;
    public AudioClip useSound;

    /*===================================================================================================================*/
    public string itemDescription;
    /*===================================================================================================================*/
}