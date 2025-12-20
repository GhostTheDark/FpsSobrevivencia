using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sistema de inventário completo estilo Rust
/// Grid-based com stacks, hotbar e containers
/// Server Authoritative
/// </summary>
public class InventorySystem : MonoBehaviour
{
    [Header("Inventory Configuration")]
    [SerializeField] private int mainInventorySize = 24; // 6x4 grid
    [SerializeField] private int hotbarSize = 6;
    [SerializeField] private int wearableSlots = 7; // Head, Chest, Pants, Gloves, Boots, Vest, Belt

    [Header("Debug")]
    [SerializeField] private bool showDebug = false;

    // Slots
    private InventorySlot[] mainInventorySlots;
    private InventorySlot[] hotbarSlots;
    private InventorySlot[] wearableSlots;

    // Estado
    private int clientId = -1;
    private bool isInitialized = false;
    private int selectedHotbarSlot = 0;

    // Callbacks
    public Action<int, InventorySlot> OnSlotChanged;
    public Action<int> OnHotbarSelectionChanged;
    public Action OnInventoryChanged;

    /// <summary>
    /// Inicializa o inventário
    /// </summary>
    public void InitializeInventory(int id)
    {
        if (isInitialized)
        {
            Debug.LogWarning("[InventorySystem] Already initialized!");
            return;
        }

        clientId = id;

        // Cria arrays de slots
        mainInventorySlots = new InventorySlot[mainInventorySize];
        hotbarSlots = new InventorySlot[hotbarSize];
        wearableSlots = new InventorySlot[this.wearableSlots.Length];

        // Inicializa todos os slots vazios
        for (int i = 0; i < mainInventorySize; i++)
            mainInventorySlots[i] = new InventorySlot();

        for (int i = 0; i < hotbarSize; i++)
            hotbarSlots[i] = new InventorySlot();

        for (int i = 0; i < this.wearableSlots.Length; i++)
            this.wearableSlots[i] = new InventorySlot();

        isInitialized = true;

        Debug.Log($"[InventorySystem] Initialized for player {clientId}");
    }

    #region ADD ITEM

    /// <summary>
    /// Adiciona item ao inventário (tenta stack primeiro)
    /// </summary>
    public bool AddItem(int itemId, int amount, float durability = 100f)
    {
        if (!isInitialized)
        {
            Debug.LogError("[InventorySystem] Not initialized!");
            return false;
        }

        ItemData itemData = ItemDatabase.Instance.GetItem(itemId);
        if (itemData == null)
        {
            Debug.LogError($"[InventorySystem] Item {itemId} not found in database!");
            return false;
        }

        int remainingAmount = amount;

        // 1. Tenta empilhar em slots existentes com o mesmo item
        if (itemData.isStackable)
        {
            remainingAmount = TryStackItem(itemId, remainingAmount, durability);
        }

        // 2. Se ainda sobrou, cria novos stacks em slots vazios
        if (remainingAmount > 0)
        {
            remainingAmount = TryAddToEmptySlots(itemId, remainingAmount, durability, itemData);
        }

        // Se conseguiu adicionar pelo menos parte
        bool success = remainingAmount < amount;

        if (success)
        {
            OnInventoryChanged?.Invoke();

            if (showDebug)
                Debug.Log($"[InventorySystem] Added {amount - remainingAmount}x {itemData.itemName}");
        }
        else
        {
            if (showDebug)
                Debug.LogWarning("[InventorySystem] Inventory full!");
        }

        return remainingAmount == 0;
    }

    /// <summary>
    /// Tenta empilhar item em slots existentes
    /// </summary>
    private int TryStackItem(int itemId, int amount, float durability)
    {
        ItemData itemData = ItemDatabase.Instance.GetItem(itemId);
        int remaining = amount;

        // Hotbar
        for (int i = 0; i < hotbarSize && remaining > 0; i++)
        {
            if (hotbarSlots[i].HasItem() && hotbarSlots[i].itemId == itemId)
            {
                remaining = AddToSlot(hotbarSlots, i, itemId, remaining, durability, itemData.maxStackSize);
            }
        }

        // Main inventory
        for (int i = 0; i < mainInventorySize && remaining > 0; i++)
        {
            if (mainInventorySlots[i].HasItem() && mainInventorySlots[i].itemId == itemId)
            {
                remaining = AddToSlot(mainInventorySlots, i, itemId, remaining, durability, itemData.maxStackSize);
            }
        }

        return remaining;
    }

    /// <summary>
    /// Tenta adicionar a slots vazios
    /// </summary>
    private int TryAddToEmptySlots(int itemId, int amount, float durability, ItemData itemData)
    {
        int remaining = amount;

        // Hotbar
        for (int i = 0; i < hotbarSize && remaining > 0; i++)
        {
            if (!hotbarSlots[i].HasItem())
            {
                remaining = AddToSlot(hotbarSlots, i, itemId, remaining, durability, itemData.maxStackSize);
            }
        }

        // Main inventory
        for (int i = 0; i < mainInventorySize && remaining > 0; i++)
        {
            if (!mainInventorySlots[i].HasItem())
            {
                remaining = AddToSlot(mainInventorySlots, i, itemId, remaining, durability, itemData.maxStackSize);
            }
        }

        return remaining;
    }

    /// <summary>
    /// Adiciona quantidade a um slot específico
    /// </summary>
    private int AddToSlot(InventorySlot[] slots, int slotIndex, int itemId, int amount, float durability, int maxStack)
    {
        int spaceInSlot = maxStack - slots[slotIndex].amount;
        int amountToAdd = Mathf.Min(amount, spaceInSlot);

        if (amountToAdd > 0)
        {
            if (!slots[slotIndex].HasItem())
            {
                slots[slotIndex].itemId = itemId;
                slots[slotIndex].amount = amountToAdd;
                slots[slotIndex].durability = durability;
            }
            else
            {
                slots[slotIndex].amount += amountToAdd;
            }

            OnSlotChanged?.Invoke(slotIndex, slots[slotIndex]);
        }

        return amount - amountToAdd;
    }

    #endregion

    #region REMOVE ITEM

    /// <summary>
    /// Remove item do inventário (por slot)
    /// </summary>
    public bool RemoveItem(int slotIndex, int amount)
    {
        InventorySlot slot = GetSlotByIndex(slotIndex);
        if (slot == null || !slot.HasItem())
            return false;

        if (slot.amount < amount)
            return false;

        slot.amount -= amount;

        if (slot.amount <= 0)
        {
            slot.Clear();
        }

        OnSlotChanged?.Invoke(slotIndex, slot);
        OnInventoryChanged?.Invoke();

        if (showDebug)
            Debug.Log($"[InventorySystem] Removed {amount}x from slot {slotIndex}");

        return true;
    }

    /// <summary>
    /// Remove item por ID (busca no inventário)
    /// </summary>
    public bool RemoveItemById(int itemId, int amount)
    {
        int remaining = amount;

        // Hotbar
        for (int i = 0; i < hotbarSize && remaining > 0; i++)
        {
            if (hotbarSlots[i].HasItem() && hotbarSlots[i].itemId == itemId)
            {
                int removeAmount = Mathf.Min(remaining, hotbarSlots[i].amount);
                RemoveItem(i, removeAmount);
                remaining -= removeAmount;
            }
        }

        // Main inventory
        for (int i = 0; i < mainInventorySize && remaining > 0; i++)
        {
            if (mainInventorySlots[i].HasItem() && mainInventorySlots[i].itemId == itemId)
            {
                int removeAmount = Mathf.Min(remaining, mainInventorySlots[i].amount);
                RemoveItem(hotbarSize + i, removeAmount);
                remaining -= removeAmount;
            }
        }

        return remaining == 0;
    }

    #endregion

    #region MOVE ITEM

    /// <summary>
    /// Move item entre slots
    /// </summary>
    public bool MoveItem(int fromSlot, int toSlot, int amount)
    {
        InventorySlot from = GetSlotByIndex(fromSlot);
        InventorySlot to = GetSlotByIndex(toSlot);

        if (from == null || to == null || !from.HasItem())
            return false;

        if (from.amount < amount)
            amount = from.amount;

        // Se slot destino está vazio, move direto
        if (!to.HasItem())
        {
            to.itemId = from.itemId;
            to.amount = amount;
            to.durability = from.durability;

            from.amount -= amount;
            if (from.amount <= 0)
                from.Clear();
        }
        // Se são itens iguais, tenta empilhar
        else if (to.itemId == from.itemId)
        {
            ItemData itemData = ItemDatabase.Instance.GetItem(to.itemId);
            if (itemData != null && itemData.isStackable)
            {
                int spaceInSlot = itemData.maxStackSize - to.amount;
                int amountToMove = Mathf.Min(amount, spaceInSlot);

                to.amount += amountToMove;
                from.amount -= amountToMove;

                if (from.amount <= 0)
                    from.Clear();
            }
            else
            {
                return false; // Não stackable
            }
        }
        // Se são itens diferentes, troca
        else
        {
            // Swap completo dos slots
            InventorySlot temp = new InventorySlot
            {
                itemId = from.itemId,
                amount = from.amount,
                durability = from.durability
            };

            from.itemId = to.itemId;
            from.amount = to.amount;
            from.durability = to.durability;

            to.itemId = temp.itemId;
            to.amount = temp.amount;
            to.durability = temp.durability;
        }

        OnSlotChanged?.Invoke(fromSlot, from);
        OnSlotChanged?.Invoke(toSlot, to);
        OnInventoryChanged?.Invoke();

        if (showDebug)
            Debug.Log($"[InventorySystem] Moved item from {fromSlot} to {toSlot}");

        return true;
    }

    #endregion

    #region HOTBAR

    /// <summary>
    /// Seleciona slot da hotbar
    /// </summary>
    public void SelectHotbarSlot(int slot)
    {
        if (slot < 0 || slot >= hotbarSize)
            return;

        selectedHotbarSlot = slot;
        OnHotbarSelectionChanged?.Invoke(slot);

        if (showDebug)
            Debug.Log($"[InventorySystem] Selected hotbar slot {slot}");
    }

    /// <summary>
    /// Retorna slot selecionado da hotbar
    /// </summary>
    public int GetSelectedHotbarSlot() => selectedHotbarSlot;

    /// <summary>
    /// Retorna item equipado (hotbar selecionado)
    /// </summary>
    public InventorySlot GetEquippedItem()
    {
        return hotbarSlots[selectedHotbarSlot];
    }

    #endregion

    #region WEARABLES

    /// <summary>
    /// Equipa item wearable
    /// </summary>
    public bool EquipWearable(int itemId, WearableSlot slotType)
    {
        int slotIndex = (int)slotType;
        if (slotIndex < 0 || slotIndex >= wearableSlots.Length)
            return false;

        ItemData itemData = ItemDatabase.Instance.GetItem(itemId);
        if (itemData == null || itemData.itemType != ItemType.Wearable)
            return false;

        // Se já tem algo equipado, remove primeiro
        if (wearableSlots[slotIndex].HasItem())
        {
            UnequipWearable(slotType);
        }

        // Equipa
        wearableSlots[slotIndex].itemId = itemId;
        wearableSlots[slotIndex].amount = 1;
        wearableSlots[slotIndex].durability = 100f;

        OnInventoryChanged?.Invoke();

        if (showDebug)
            Debug.Log($"[InventorySystem] Equipped {itemData.itemName} to {slotType}");

        return true;
    }

    /// <summary>
    /// Desequipa item wearable
    /// </summary>
    public bool UnequipWearable(WearableSlot slotType)
    {
        int slotIndex = (int)slotType;
        if (slotIndex < 0 || slotIndex >= wearableSlots.Length)
            return false;

        if (!wearableSlots[slotIndex].HasItem())
            return false;

        int itemId = wearableSlots[slotIndex].itemId;

        // Tenta adicionar ao inventário
        if (AddItem(itemId, 1, wearableSlots[slotIndex].durability))
        {
            wearableSlots[slotIndex].Clear();
            OnInventoryChanged?.Invoke();

            if (showDebug)
                Debug.Log($"[InventorySystem] Unequipped from {slotType}");

            return true;
        }

        return false;
    }

    #endregion

    #region QUERIES

    /// <summary>
    /// Verifica se tem espaço no inventário
    /// </summary>
    public bool HasSpace(int itemId, int amount)
    {
        ItemData itemData = ItemDatabase.Instance.GetItem(itemId);
        if (itemData == null)
            return false;

        int spaceAvailable = 0;

        // Calcula espaço em slots existentes
        if (itemData.isStackable)
        {
            for (int i = 0; i < hotbarSize; i++)
            {
                if (hotbarSlots[i].HasItem() && hotbarSlots[i].itemId == itemId)
                {
                    spaceAvailable += itemData.maxStackSize - hotbarSlots[i].amount;
                }
            }

            for (int i = 0; i < mainInventorySize; i++)
            {
                if (mainInventorySlots[i].HasItem() && mainInventorySlots[i].itemId == itemId)
                {
                    spaceAvailable += itemData.maxStackSize - mainInventorySlots[i].amount;
                }
            }
        }

        // Calcula slots vazios
        int emptySlots = 0;
        for (int i = 0; i < hotbarSize; i++)
            if (!hotbarSlots[i].HasItem()) emptySlots++;

        for (int i = 0; i < mainInventorySize; i++)
            if (!mainInventorySlots[i].HasItem()) emptySlots++;

        spaceAvailable += emptySlots * itemData.maxStackSize;

        return spaceAvailable >= amount;
    }

    /// <summary>
    /// Conta quantos itens tem no inventário
    /// </summary>
    public int CountItem(int itemId)
    {
        int count = 0;

        for (int i = 0; i < hotbarSize; i++)
        {
            if (hotbarSlots[i].HasItem() && hotbarSlots[i].itemId == itemId)
                count += hotbarSlots[i].amount;
        }

        for (int i = 0; i < mainInventorySize; i++)
        {
            if (mainInventorySlots[i].HasItem() && mainInventorySlots[i].itemId == itemId)
                count += mainInventorySlots[i].amount;
        }

        return count;
    }

    /// <summary>
    /// Retorna slot por índice global
    /// </summary>
    public InventorySlot GetSlotByIndex(int index)
    {
        if (index < 0)
            return null;

        if (index < hotbarSize)
            return hotbarSlots[index];

        index -= hotbarSize;
        if (index < mainInventorySize)
            return mainInventorySlots[index];

        return null;
    }

    /// <summary>
    /// Retorna item em slot específico
    /// </summary>
    public InventorySlot GetItemAtSlot(int slotIndex)
    {
        return GetSlotByIndex(slotIndex);
    }

    /// <summary>
    /// Define slot diretamente (usado por sincronização de rede)
    /// </summary>
    public void SetSlot(int slotIndex, int itemId, int amount, float durability)
    {
        InventorySlot slot = GetSlotByIndex(slotIndex);
        if (slot == null) return;

        slot.itemId = itemId;
        slot.amount = amount;
        slot.durability = durability;

        OnSlotChanged?.Invoke(slotIndex, slot);
    }

    /// <summary>
    /// Limpa slot
    /// </summary>
    public void ClearSlot(int slotIndex)
    {
        InventorySlot slot = GetSlotByIndex(slotIndex);
        if (slot == null) return;

        slot.Clear();
        OnSlotChanged?.Invoke(slotIndex, slot);
    }

    /// <summary>
    /// Retorna número total de slots
    /// </summary>
    public int GetSlotCount() => hotbarSize + mainInventorySize;

    #endregion

    #region DEBUG

    /// <summary>
    /// Imprime inventário completo
    /// </summary>
    public void DebugPrintInventory()
    {
        Debug.Log("=== HOTBAR ===");
        for (int i = 0; i < hotbarSize; i++)
        {
            if (hotbarSlots[i].HasItem())
            {
                ItemData item = ItemDatabase.Instance.GetItem(hotbarSlots[i].itemId);
                Debug.Log($"[{i}] {item.itemName} x{hotbarSlots[i].amount}");
            }
        }

        Debug.Log("=== MAIN INVENTORY ===");
        for (int i = 0; i < mainInventorySize; i++)
        {
            if (mainInventorySlots[i].HasItem())
            {
                ItemData item = ItemDatabase.Instance.GetItem(mainInventorySlots[i].itemId);
                Debug.Log($"[{i}] {item.itemName} x{mainInventorySlots[i].amount}");
            }
        }
    }

    #endregion
}

/// <summary>
/// Representa um slot de inventário
/// </summary>
[Serializable]
public class InventorySlot
{
    public int itemId = -1;
    public int amount = 0;
    public float durability = 100f;

    public bool HasItem() => itemId >= 0 && amount > 0;

    public void Clear()
    {
        itemId = -1;
        amount = 0;
        durability = 100f;
    }
}

/// <summary>
/// Tipos de slots wearable
/// </summary>
public enum WearableSlot
{
    Head = 0,
    Chest = 1,
    Pants = 2,
    Gloves = 3,
    Boots = 4,
    Vest = 5,
    Belt = 6
}