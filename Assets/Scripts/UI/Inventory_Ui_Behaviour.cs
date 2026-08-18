using UnityEngine;

public class Inventory_Ui_Behaviour : MonoBehaviour
{
    [SerializeField] private Inventory inventory;
    [SerializeField] private Transform itemsParent;
    private Inventory_Slots[] slots;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ConnectToInventoryInstance();
        //HideAll();
        slots = itemsParent.GetComponentsInChildren<Inventory_Slots>();
        RefreshInventoryUI();
    }

    // Update is called once per frame
    void Update()
    {
        RefreshInventoryUI();
    }

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

    //private void HideAll()
    //{
    //    ItemSlot_1.SetActive(false);
    //}

    private void RefreshInventoryUI()
    {
        for (int index = 0; index < slots.Length; index++)
        {
            if(index < inventory.itemStacks.Count)
            {
                slots[index].Update_Slot(index);
            }
            else
            {
                slots[index].Hide_Slot();
            }
        }
    }
}
