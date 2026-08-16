using UnityEngine;
using UnityEngine.InputSystem;

// not used anymore because i ended up replacing with item.cs (which handles pickups and item stacking into inventory.cs)
public class ItemPickUp : MonoBehaviour
{

    [SerializeField] private Inventory inventory;

    void Start()
    {
        if (inventory == null)
        {
            inventory = GameObject.Find("Inventory").GetComponent<Inventory>();
        }
    }

    public void PickUp(ItemInstance item)
    {
        inventory.AddItem(item);
        inventory.DisplayItems();
    }

    void Update()
    {
        var interactAction = InputSystem.actions.FindAction("Interact");
        if (interactAction != null && interactAction.WasPressedThisFrame())
        {
            ItemInstance item = inventory.GetItem(0);
            if (item != null)
            {
                item.itemEffect.Use(this.gameObject);
                inventory.RemoveItem(0);
            }
        }
    }
}