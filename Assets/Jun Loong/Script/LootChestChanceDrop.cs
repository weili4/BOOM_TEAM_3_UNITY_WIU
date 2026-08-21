using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class LootChestChanceDrop : MonoBehaviour
{
    /*===================================================================================================================*/
    private bool PlayerNearChest = false;
    private bool IsChestOpen = false;
    /*===================================================================================================================*/

    [System.Serializable]
    private class LootItem
    {
        public GameObject DropItems;
        public int DropChance; // Please Set it to Max of 100
    }

    [SerializeField] private List<LootItem> DropItemsList = new List<LootItem>();
    private GameObject ItemToSpawn;
    private bool ItemFound = false;

    /*===================================================================================================================*/
    [Header("Inventory and Item to check")]
    [SerializeField] private bool NeedItemToOpen = false;
    [SerializeField] private Inventory inventory;
    [SerializeField] private string ItemNameToSearch = "Key";
    /*===================================================================================================================*/

    /*===================================================================================================================*/
    private Animator BoxAnimator;
    /*===================================================================================================================*/

    [Header("Debugging")]
    [SerializeField] private bool ShowDebugLogs = false;

    void Start()
    {
        ConnectToInventoryInstance();
        BoxAnimator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if (DropItemsList == null || DropItemsList.Count == 0)
        {
            Debug.Log("No items assigned to this chest!");
        }
        else if (PlayerNearChest && !IsChestOpen)
        {
            if (InputSystem.actions["Interact"].WasPressedThisFrame())
            {
                if (NeedItemToOpen)
                {
                    bool HasItemToUnlock = false;
                    int ItemIndex = -1;

                    for (int index = 0; index < inventory.itemStacks.Count; index++)
                    {
                        if (inventory.itemStacks[index].itemData.itemName == ItemNameToSearch)
                        {
                            HasItemToUnlock = true;
                            ItemIndex = index;
                        }
                    }
                    if (HasItemToUnlock)
                    {
                        inventory.RemoveItem(ItemIndex);
                    }
                    else
                    {
                        Debug.Log("You do not have the item to unlock it");
                        return;
                    }
                }

                IsChestOpen = true;
                int RandomValue = Random.Range(1 , 101);
                ItemFound = false;

                if (ShowDebugLogs) Debug.Log("Random Number Rolled: " + RandomValue);

                foreach (LootItem DropItem in DropItemsList)
                {
                    if (!ItemFound && RandomValue <= DropItem.DropChance)
                    {
                        if (ShowDebugLogs) Debug.Log("Item Matched: " + DropItem.DropItems.name + " With " + DropItem.DropChance);
                        ItemToSpawn = DropItem.DropItems;
                        ItemFound = true;
                    }
                    else
                    {
                        if (ShowDebugLogs) Debug.Log("Item Missed: " + DropItem.DropItems.name + " With " + DropItem.DropChance);
                    }
                }

                BoxAnimator.SetBool("IsBoxOpen", true);
                Instantiate(ItemToSpawn, transform.position, Quaternion.identity);
            }
        }
    }

    /*===================================================================================================================*/
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !IsChestOpen)
        {
            // Set a input icon as Active
            PlayerNearChest = true;
        }
    }
    /*===================================================================================================================*/

    /*===================================================================================================================*/
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // Set a input icon as Active
            PlayerNearChest = false;
        }
    }
    /*===================================================================================================================*/

    /*===================================================================================================================*/
    private void ConnectToInventoryInstance()
    {
        if (Inventory.Instance != null)
        {
            inventory = Inventory.Instance;
        }
        else if (inventory == null)
        {
            inventory = FindFirstObjectByType<Inventory>();
        }
    }
    /*===================================================================================================================*/

}
