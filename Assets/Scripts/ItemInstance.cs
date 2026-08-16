using UnityEngine;
[System.Serializable]
public class ItemInstance
{
    public ItemData itemData;
    public ItemEffect itemEffect;
    public ItemInstance(ItemData itemData, ItemEffect itemEffect)
    {
        this.itemData = itemData;
        this.itemEffect = itemEffect;
    }
}
