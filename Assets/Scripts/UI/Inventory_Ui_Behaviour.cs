using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class Inventory_Ui_Behaviour : MonoBehaviour
{
    /*===================================================================================================================*/
    // Inventory Slot Panel
    [SerializeField] private Inventory inventory;
    [SerializeField] private Transform itemsSlots;
    [SerializeField] private int ColumnSize;
    private Inventory_Slots[] slots;

    private Vector2 moveSelection;
    private bool canMoveSelect;
    /*===================================================================================================================*/

    /*===================================================================================================================*/
    // Inventory Description Panel
    [SerializeField] private TextMeshProUGUI ItemName;
    private int selectedItemIndex = -1;
    /*===================================================================================================================*/

    /*===================================================================================================================*/
    void Start()
    {
        ConnectToInventoryInstance();
        slots = itemsSlots.GetComponentsInChildren<Inventory_Slots>();
        RefreshInventoryUI();

        ItemName.text = " ";
    }
    /*===================================================================================================================*/

    /*===================================================================================================================*/
    void Update()
    {
        RefreshInventoryUI();

        moveSelection = InputSystem.actions["Move"].ReadValue<Vector2>();

        if (moveSelection.x == 0 && moveSelection.y == 0)
        {
            canMoveSelect = true;
        }
        else if (canMoveSelect && !(inventory.itemStacks.Count == 0))
        {
            int newIndex = selectedItemIndex;

            if (moveSelection.x == 1)
            {
                newIndex += 1;
            }
            else if (moveSelection.x == -1)
            {
                newIndex -= 1;
            }
            else if (moveSelection.y == 1)
            {
                newIndex -= ColumnSize;
            }
            else if (moveSelection.y == -1)
            {
                newIndex += ColumnSize;
            }

            if (newIndex >= 0 && newIndex < inventory.itemStacks.Count)
            {
                selectedItemIndex = newIndex;
                SelectedItem(selectedItemIndex);
            }

            canMoveSelect = false;
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

    /*===================================================================================================================*/
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
    /*===================================================================================================================*/

    /*===================================================================================================================*/
    public void SelectedItem(int SelectedItemIndex)
    {
        selectedItemIndex = SelectedItemIndex;

        /*================================================================================================================*/
        for (int index = 0; index < slots.Length; index++)
        {
            if (index == selectedItemIndex)
            {
                slots[index].SetSelected(true);
            }
            else
            {
                slots[index].SetSelected(false);
            }
        }
        /*================================================================================================================*/

        /*================================================================================================================*/
        // Updates the Description Panel with the selected item
        ItemName.text = inventory.itemStacks[selectedItemIndex].itemData.itemName;
        Debug.Log("Item Name: " + inventory.itemStacks[selectedItemIndex].itemData.itemName);
        Debug.Log("Item Amount: " + inventory.itemStacks[selectedItemIndex].count);
        /*================================================================================================================*/
    }
    /*===================================================================================================================*/

    /*===================================================================================================================*/
    public void OnSelectedItemUse()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && inventory != null && selectedItemIndex != -1)
        {
            inventory.UseItemStack(selectedItemIndex, player);
        }
    }
    /*===================================================================================================================*/
}
