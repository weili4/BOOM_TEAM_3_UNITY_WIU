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
    private int selectedItemIndex = -1;
    /*===================================================================================================================*/

    /*===================================================================================================================*/
    // UI References
    //[SerializeField] private GameUIManager gameUIManager; Need to change to PauseMenuManager
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

        ItemName.text = " ";
        ItemDescriptionText.text = " ";
        ItemStatText.text = " ";

        //if (GameUIManager.Instance != null)
        //{
        //    gameUIManager = GameUIManager.Instance;
        //}
        //else if (gameUIManager == null)
        //{
        //    Debug.Log("Need to Manually Find to GameUiManager");
        //}
    }
    /*===================================================================================================================*/

    /*===================================================================================================================*/
    void Update()
    {
        //if (gameUIManager.GetInventoryOpen())
        //{
        //    RefreshInventoryUI();

        //    MoveSelection();

        //    CheckUseItemPressed();
        //}

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
            inventory.UseItemStack(selectedItemIndex, player);
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
    }
    /*===================================================================================================================*/

    /*===================================================================================================================*/
    private void UpdateDescriptionPanel()
    {
        // Updates the Description Panel with the selected item
        ItemName.text = inventory.itemStacks[selectedItemIndex].itemData.itemName;
        ItemDescriptionText.text = inventory.itemStacks[selectedItemIndex].itemData.itemDescription;
        ItemStatText.text = inventory.itemStacks[selectedItemIndex].itemEffect.GetEffectValue();
        //Debug.Log("Item Name: " + inventory.itemStacks[selectedItemIndex].itemData.itemName);
        //Debug.Log("Item Amount: " + inventory.itemStacks[selectedItemIndex].count);
        //Debug.Log("Item Description: " + inventory.itemStacks[selectedItemIndex].itemData.itemDescription);
    }
    /*===================================================================================================================*/
}