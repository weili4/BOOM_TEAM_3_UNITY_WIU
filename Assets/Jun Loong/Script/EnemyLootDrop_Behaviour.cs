using System.Collections.Generic;
using UnityEngine;

public class EnemyLootDrop_Behaviour : MonoBehaviour
{
    [System.Serializable]
    private class LootItem
    {
        public GameObject DropItems;
        public int DropChance; // Please Set it to Max of 100
    }

    [SerializeField] private List<LootItem> DropItemsList = new List<LootItem>();
    private GameObject ItemToSpawn;
    private bool ItemFound = false;

    // Update is called once per frame
    public void DoDropLoot()
    {
        if (DropItemsList == null || DropItemsList.Count == 0)
        {
            Debug.Log("No items assigned to this enemy!");
        }
        else
        {
            int RandomValue = Random.Range(1, 101);
            ItemFound = false;

            foreach (LootItem DropItem in DropItemsList)
            {
                if (!ItemFound && RandomValue <= DropItem.DropChance)
                {
                    ItemToSpawn = DropItem.DropItems;
                    ItemFound = true;
                }
            }

            if(!ItemFound)
            {
                Vector2 SpawnItemLocation = new Vector2(transform.position.x, transform.position.y + 0.50f);

                GameObject ItemDrop = Instantiate(ItemToSpawn, SpawnItemLocation, Quaternion.identity);
                if (ItemDrop.TryGetComponent<Item>(out Item itemScript))
                {
                    itemScript.StartLaunchEffect(true);
                }
            }

        }
    }
}