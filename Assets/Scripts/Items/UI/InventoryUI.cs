using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private Inventory inventory;
    [SerializeField] private GameObject itemSlotPrefab;
    [SerializeField] private Transform slotContainer;
    [SerializeField] private TextMeshProUGUI selectedItemNameText;
    [SerializeField] private AudioClip scrollItemSound;

    private int selectedSlotIndex = 0;
    private List<ItemSlotUI> spawnedSlots = new List<ItemSlotUI>();

    private void Start()
    {
        ConnectToInventoryInstance();
    }

    private void OnEnable()
    {
        ConnectToInventoryInstance();
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

    private void Update()
    {
        if (inventory == null)
        {
            ConnectToInventoryInstance();
            if (inventory == null) return;
        }

        int stackCount = inventory.itemStacks.Count;

        // read input (uses new input system but JUST IN CASE, it falls back to the og one)
        float scrollY = 0f;
        try
        {
            if (InputSystem.actions != null && InputSystem.actions["Scroll"] != null)
            {
                scrollY = InputSystem.actions["Scroll"].ReadValue<Vector2>().y;
            }
        }
        catch { }

        if (scrollY == 0f && Mouse.current != null)
        {
            scrollY = Mouse.current.scroll.ReadValue().y;
        }

        if (stackCount > 0)
        {
            int prevIndex = selectedSlotIndex;

            if (scrollY > 0f)
            {
                selectedSlotIndex--;
                if (selectedSlotIndex < 0) selectedSlotIndex = stackCount - 1;
            }
            else if (scrollY < 0f)
            {
                selectedSlotIndex++;
                if (selectedSlotIndex >= stackCount) selectedSlotIndex = 0;
            }

            selectedSlotIndex = Mathf.Clamp(selectedSlotIndex, 0, stackCount - 1);

            if (selectedSlotIndex != prevIndex && scrollItemSound != null)
            {
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlaySFX(scrollItemSound, transform.position, 0.6f);
            }
        }
        else
        {
            selectedSlotIndex = 0;
        }

        // READ USE ITEM INPUT VIA INPUT SYSTEM ACTIONS BUT FALL BACK TO OG INPUT JUST IN CASE
        bool useItemPressed = false;
        try
        {
            if (InputSystem.actions != null && InputSystem.actions["UseItem"] != null)
            {
                useItemPressed = InputSystem.actions["UseItem"].WasPressedThisFrame();
            }
        }
        catch { }

        if (!useItemPressed && Keyboard.current != null)
        {
            useItemPressed = Keyboard.current.rKey.wasPressedThisFrame;
        }

        if (useItemPressed && stackCount > 0)
        {
            UseSelectedItem();
        }

        RefreshInventoryUI();
    }

    private void UseSelectedItem()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && inventory != null)
        {
            inventory.UseItemStack(selectedSlotIndex, player);
        }
    }

    private void RefreshInventoryUI()
    {
        // stop if inventory or ui setup missing
        if (inventory == null || slotContainer == null || itemSlotPrefab == null) return;

        // create more slots if inventory have more items
        while (spawnedSlots.Count < inventory.itemStacks.Count)
        {
            GameObject newObj = Instantiate(itemSlotPrefab, slotContainer);
            ItemSlotUI newSlot = newObj.GetComponent<ItemSlotUI>();
            spawnedSlots.Add(newSlot);
        }

        while (spawnedSlots.Count > inventory.itemStacks.Count)
        {
            int lastIndex = spawnedSlots.Count - 1;
            Destroy(spawnedSlots[lastIndex].gameObject);
            spawnedSlots.RemoveAt(lastIndex);
        }

        for (int i = 0; i < inventory.itemStacks.Count; i++)
        {
            ItemStack stack = inventory.itemStacks[i];

            spawnedSlots[i].SetupSlot(stack.itemData.itemImage, stack.count);
            spawnedSlots[i].UpdateCooldownBar(stack.currentCooldown, stack.itemData.cooldownTime);

            bool isSelected = (i == selectedSlotIndex);
            spawnedSlots[i].SetSelected(isSelected);
        }

        if (selectedItemNameText != null)
        {
            if (inventory.itemStacks.Count > 0 && selectedSlotIndex >= 0 && selectedSlotIndex < inventory.itemStacks.Count)
            {
                ItemStack selectedStack = inventory.itemStacks[selectedSlotIndex];
                if (selectedStack != null && selectedStack.itemData != null)
                {
                    selectedItemNameText.text = selectedStack.itemData.itemName;
                }
            }
            else
            {
                selectedItemNameText.text = "";
            }
        }
    }
}