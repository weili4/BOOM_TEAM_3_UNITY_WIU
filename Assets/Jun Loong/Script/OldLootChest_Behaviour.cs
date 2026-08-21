using UnityEngine;
using UnityEngine.InputSystem;

public class LootChest_Behaviour : MonoBehaviour
{
    /*===================================================================================================================*/
    private bool PlayerNearChest = false;
    private bool IsChestOpen = false;
    /*===================================================================================================================*/

    [SerializeField] private GameObject[] ArrayOfItemDrop;
    private GameObject ItemToSpawn;

    /*===================================================================================================================*/
    [Header("Inventory and Item to check")]
    [SerializeField] private bool NeedItemToOpen = false;
    [SerializeField] private Inventory inventory;
    [SerializeField] private string ItemNameToSearch = "Key";
    /*===================================================================================================================*/


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ConnectToInventoryInstance();
    }

    /*===================================================================================================================*/
    // Update is called once per frame
    void Update()
    {
        if (ArrayOfItemDrop == null || ArrayOfItemDrop.Length == 0)
        {
            Debug.Log("No items assigned to this chest!");
        }
        else if (PlayerNearChest && !IsChestOpen)
        {
            if (InputSystem.actions["Interact"].WasPressedThisFrame())
            {
                if(NeedItemToOpen)
                {
                    bool HasItemToUnlock = false;
                    int ItemIndex = -1;

                    for (int index = 0; index < inventory.itemStacks.Count; index++)
                    {
                        if(inventory.itemStacks[index].itemData.itemName == ItemNameToSearch)
                        {
                            HasItemToUnlock = true;
                            ItemIndex = index;
                        }
                    }
                    if(HasItemToUnlock)
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
                int randomValue = Random.Range(0, ArrayOfItemDrop.Length);
                ItemToSpawn = ArrayOfItemDrop[randomValue];
                Instantiate(ItemToSpawn, transform.position, Quaternion.identity);
            }
        }
    }
    /*===================================================================================================================*/

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
