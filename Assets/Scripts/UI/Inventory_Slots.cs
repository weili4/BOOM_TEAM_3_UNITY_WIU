using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Inventory_Slots : MonoBehaviour
{
    /*===================================================================================================================*/
    // References
    [SerializeField] private Inventory_Ui_Behaviour InventoryUiBehaviour;
    [SerializeField] private Inventory inventory;
    /*===================================================================================================================*/

    /*===================================================================================================================*/
    // UI Elements
    [SerializeField] private GameObject SlotIcon;
    [SerializeField] private GameObject SlotCount;
    [SerializeField] private GameObject SlotButton;
    [SerializeField] private GameObject SelectedOverlay;
    [SerializeField] private TextMeshProUGUI SlotCountText;
    /*===================================================================================================================*/

    /*===================================================================================================================*/
    // Internal State
    [SerializeField] private int CurrentItemIndex; // Testing to check if is the correct index
    /*===================================================================================================================*/


    /*===================================================================================================================*/
    void Start()
    {
        ConnectToInventoryInstance();
        Hide_Slot();
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
    public void Update_Slot(int ItemIndex)
    {
        CurrentItemIndex = ItemIndex;
        SlotIcon.SetActive(true);
        SlotCount.SetActive(true);
        SlotButton.SetActive(true);
        SlotIcon.GetComponent<Image>().sprite = inventory.itemStacks[ItemIndex].itemData.itemImage;
        SlotCountText.text = "x " + inventory.itemStacks[ItemIndex].count;
    }
    /*===================================================================================================================*/

    /*===================================================================================================================*/
    public void OnUseItem()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && inventory != null)
        {
            inventory.UseItemStack(CurrentItemIndex, player);
        }
    }
    /*===================================================================================================================*/

    /*===================================================================================================================*/
    public void OnSelectingItem()
    {
        InventoryUiBehaviour.SelectedItem(CurrentItemIndex);
    }
    /*===================================================================================================================*/

    /*===================================================================================================================*/
    public void Hide_Slot()
    {
        SelectedOverlay.SetActive(false);
        SlotIcon.SetActive(false);
        SlotCount.SetActive(false);
        SlotButton.SetActive(false);
    }
    /*===================================================================================================================*/

    /*===================================================================================================================*/
    public void SetSelected(bool isSelected)
    {
        SelectedOverlay.SetActive(isSelected);
    }
    /*===================================================================================================================*/
}
