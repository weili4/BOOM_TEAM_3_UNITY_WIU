using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ItemStack
{
    public ItemData itemData;
    public ItemEffect itemEffect;
    public int count = 1;
    public float currentCooldown = 0f;
}

public class Inventory : MonoBehaviour
{
    // SINGLETON

    public static Inventory Instance { get; private set; }

    public List<ItemStack> itemStacks = new List<ItemStack>();
    public int maxItems = 10;

    public int MaxStackSize = 5;

    private void Awake()
    {
        // keep one inventory instance so items dont get duplicated
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // keep inventory when changing scene
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Update()
    {
        // reduce item cooldown every frame
        foreach (var stack in itemStacks)
        {
            if (stack.currentCooldown > 0)
            {
                stack.currentCooldown -= Time.deltaTime;
            }
        }
    }

    public bool AddItemStack(ItemData data, ItemEffect effect)
    {
        // find existing stack so same item can be grouped together
        ItemStack existingStack = itemStacks.Find(s => s.itemData == data);

        /*===========================================================================================================*/
        // Check if the exisiting stack is already full if so return false to tell item to not abke to pick up anymore
        if (existingStack != null)
        {
            if (existingStack.count >= MaxStackSize)
            {
                return false; // stack is already full
            }
            existingStack.count++;
        }
        else
        {
            // create new stack if item dont exist yet
            ItemStack newStack = new ItemStack();
            newStack.itemData = data;
            newStack.itemEffect = effect;
            newStack.count = 1;
            newStack.currentCooldown = 0f;
            itemStacks.Add(newStack);
        }
        return true;
        /*===========================================================================================================*/
    }

    public bool AddItem(ItemInstance item)
    {
        if (item != null)
        {
            AddItemStack(item.itemData, item.itemEffect);
            return true;
        }
        return false;
    }

    public void DisplayItems()
    {
        foreach (var stack in itemStacks)
        {
            if (stack != null && stack.itemData != null)
            {
                Debug.Log("item name: " + stack.itemData.itemName + " count: " + stack.count);
            }
        }
    }

    public ItemInstance GetItem(int index)
    {
        if (index >= 0 && index < itemStacks.Count)
        {
            ItemStack stack = itemStacks[index];
            return new ItemInstance(stack.itemData, stack.itemEffect);
        }
        return null;
    }

    public void RemoveItem(int index)
    {
        if (index >= 0 && index < itemStacks.Count)
        {
            itemStacks[index].count--;
            if (itemStacks[index].count <= 0)
            {
                itemStacks.RemoveAt(index);
            }
        }
    }

    public bool UseItemStack(int slotIndex, GameObject user)
    {
        if (slotIndex < 0 || slotIndex >= itemStacks.Count) return false;

        ItemStack stack = itemStacks[slotIndex];

        // only use item if got item and cooldown finish
        if (stack.count > 0 && stack.currentCooldown <= 0)
        {
            if (stack.itemEffect != null)
            {
                stack.itemEffect.Use(user);
            }

            // PLAY ITEM USE SFX VIA AUDIO MIXER
            if (stack.itemData != null && stack.itemData.useSound != null)
            {
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlaySFX(stack.itemData.useSound, user.transform.position, 1.2f);
                else
                    AudioSource.PlayClipAtPoint(stack.itemData.useSound, user.transform.position);
            }

            stack.currentCooldown = stack.itemData.cooldownTime;
            stack.count--;

            if (stack.count <= 0)
            {
                itemStacks.RemoveAt(slotIndex);
            }

            return true;
        }

        return false;
    }
}