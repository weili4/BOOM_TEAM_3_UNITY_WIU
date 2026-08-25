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
    [SerializeField] private TextMeshProUGUI ItemDescriptionText;
    [SerializeField] private TextMeshProUGUI ItemStatText;
    [SerializeField] private GameObject UseBtn;
    [SerializeField] private GameObject MaxItemText;
    private int selectedItemIndex = -1;
    /*===================================================================================================================*/

    /*===================================================================================================================*/
    private bool MovingItem = false;
    [SerializeField] private GameObject MoveItemIcon;
    private int FirstItemIndex = -1;
    private int SecondItemIndex = -1;
    /*===================================================================================================================*/

    /*===================================================================================================================*/
    void Start()
    {
        ConnectToInventoryInstance();
        slots = itemsSlots.GetComponentsInChildren<Inventory_Slots>();
        for (int index = 0; index < slots.Length; index++)
        {
            slots[index].SetInventoryUiReference(this.GetComponent<Inventory_Ui_Behaviour>());
        }

        RefreshInventoryUI();

        ClearSelection();
    }
    /*===================================================================================================================*/

    /*===================================================================================================================*/
    void Update()
    {
        RefreshInventoryUI();

        MoveSelection();

        CheckUseItemPressed();
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
            if (index < inventory.itemStacks.Count)
            {
                slots[index].Update_Slot(index, inventory.itemStacks[index].itemData.itemImage, inventory.itemStacks[index].count);
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
        if (MovingItem)
        {
            if (FirstItemIndex == -1)
            {
                FirstItemIndex = SelectedItemIndex;
            }
            else if (SecondItemIndex == -1)
            {
                SecondItemIndex = SelectedItemIndex;
                SwapSlots(FirstItemIndex, SecondItemIndex);

                FirstItemIndex = -1;
                SecondItemIndex = -1;
                MovingItem = false;
                MoveItemIcon.SetActive(MovingItem);
            }
            return;
        }

        selectedItemIndex = SelectedItemIndex;
        UpdateDescriptionPanel();

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
    }
    /*===================================================================================================================*/

    /*===================================================================================================================*/
    public void OnSelectedItemUse()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && inventory != null && selectedItemIndex != -1)
        {
            if (inventory.itemStacks[selectedItemIndex].itemData.UseableItem == true)
            {
                inventory.UseItemStack(selectedItemIndex, player);
            }
            ClearSelection();
        }
    }
    /*===================================================================================================================*/

    /*===================================================================================================================*/
    private void MoveSelection()
    {
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
    private void CheckUseItemPressed()
    {
        bool UseItemPressed = false;
        UseItemPressed = InputSystem.actions["UseItem"].WasPressedThisFrame();
        if (UseItemPressed)
        {
            OnSelectedItemUse();
            selectedItemIndex = -1;
        }
    }
    /*===================================================================================================================*/

    /*===================================================================================================================*/
    private void ClearSelection()
    {
        if(inventory.itemStacks.Count == 0)
        {
            selectedItemIndex = -1;
        }

        for (int index = 0; index < slots.Length; index++)
        {
            slots[index].SetSelected(false);
        }

        ItemName.text = " ";
        ItemDescriptionText.text = " ";
        ItemStatText.text = " ";
        UseBtn.SetActive(false);
        MaxItemText.SetActive(false);
    }
    /*===================================================================================================================*/

    /*===================================================================================================================*/
    private void UpdateDescriptionPanel()
    {
        // Updates the Description Panel with the selected item
        ItemName.text = inventory.itemStacks[selectedItemIndex].itemData.itemName;
        ItemDescriptionText.text = inventory.itemStacks[selectedItemIndex].itemData.itemDescription;
        ItemStatText.text = inventory.itemStacks[selectedItemIndex].itemEffect.GetEffectValue();

        if (inventory.itemStacks[selectedItemIndex].itemData.UseableItem == true)
        {
            UseBtn.SetActive(true);
            MaxItemText.SetActive(true);
        }
        else
        {
            UseBtn.SetActive(false);
            MaxItemText.SetActive(false);
        }

        //Debug.Log("Item Name: " + inventory.itemStacks[selectedItemIndex].itemData.itemName);
        //Debug.Log("Item Amount: " + inventory.itemStacks[selectedItemIndex].count);
        //Debug.Log("Item Description: " + inventory.itemStacks[selectedItemIndex].itemData.itemDescription);
    }
    /*===================================================================================================================*/

    /*===================================================================================================================*/
    private void SwapSlots(int indexA, int indexB)
    {
        if (indexA < 0 || indexA >= inventory.itemStacks.Count) return;
        if (indexB < 0 || indexB >= inventory.itemStacks.Count) return;

        ItemStack temp = inventory.itemStacks[indexA];
        inventory.itemStacks[indexA] = inventory.itemStacks[indexB];
        inventory.itemStacks[indexB] = temp;

        RefreshInventoryUI();
    }
    /*===================================================================================================================*/

    /*===================================================================================================================*/
    public void IsMovingItem()
    {
        MovingItem = !MovingItem;
        MoveItemIcon.SetActive(MovingItem);

        FirstItemIndex = -1;
        SecondItemIndex = -1;

        ClearSelection();
    }
    /*===================================================================================================================*/
}