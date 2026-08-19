using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Inventory_Slots : MonoBehaviour
{
    /*===================================================================================================================*/
    // References
    [SerializeField] private Inventory_Ui_Behaviour InventoryUiBehaviour;
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
        Hide_Slot();
    }
    /*===================================================================================================================*/

    /*===================================================================================================================*/
    public void SetInventoryUiReference(Inventory_Ui_Behaviour InvUiBehaviour)
    {
        InventoryUiBehaviour = InvUiBehaviour;
    }
    /*===================================================================================================================*/

    /*===================================================================================================================*/
    public void Update_Slot(int ItemIndex, Sprite ItemImage, int ItemCount)
    {
        CurrentItemIndex = ItemIndex;
        SlotIcon.SetActive(true);
        SlotCount.SetActive(true);
        SlotButton.SetActive(true);
        SlotIcon.GetComponent<Image>().sprite = ItemImage;
        SlotCountText.text = "x" + ItemCount;
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
