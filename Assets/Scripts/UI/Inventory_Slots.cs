using UnityEngine;
using UnityEngine.UI;

public class Inventory_Slots : MonoBehaviour
{
    [SerializeField] private GameObject SlotIcon;
    [SerializeField] private Inventory inventory;
    [SerializeField] private int CurrentItemIndex;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Hide_Slot();
        ConnectToInventoryInstance();
    }

    // Update is called once per frame
    void Update()
    {
        
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

    public void Update_Slot(int ItemIndex)
    {
        CurrentItemIndex = ItemIndex;
        SlotIcon.SetActive(true);
        SlotIcon.GetComponent<Image>().sprite = inventory.itemStacks[ItemIndex].itemData.itemImage;
    }

    public void OnUseItem()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && inventory != null)
        {
            inventory.UseItemStack(CurrentItemIndex, player);
        }
    }
    public void Hide_Slot()
    {
        SlotIcon.SetActive(false);
    }
}
