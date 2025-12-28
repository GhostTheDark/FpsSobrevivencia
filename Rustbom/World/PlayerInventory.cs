using System;
using System.Collections.Generic;
using System.Linq;
using RustlikeServer.Items;

namespace RustlikeServer.World
{
    /// <summary>
    /// ⭐ ATUALIZADO: Adicionado GetSlot público para validação
    /// </summary>
    public class PlayerInventory
    {
        private const int INVENTORY_SIZE = 24;
        private const int HOTBAR_SIZE = 6;

        private ItemStack[] _slots;
        private int _selectedHotbarSlot = 0;

        public PlayerInventory()
        {
            _slots = new ItemStack[INVENTORY_SIZE];
            
            // Debug: Inicializa com alguns itens para teste
            AddItemDebug(1, 5);   // 5 Apples
            AddItemDebug(4, 3);   // 3 Water Bottles
            AddItemDebug(6, 10);  // 10 Bandages
        }

        public bool AddItem(int itemId, int quantity = 1)
        {
            var itemDef = ItemDatabase.GetItem(itemId);
            if (itemDef == null)
            {
                Console.WriteLine($"[Inventory] Item {itemId} não encontrado no database!");
                return false;
            }

            int remaining = quantity;

            for (int i = 0; i < INVENTORY_SIZE && remaining > 0; i++)
            {
                if (_slots[i] != null && _slots[i].ItemId == itemId)
                {
                    remaining = _slots[i].Add(remaining);
                }
            }

            for (int i = 0; i < INVENTORY_SIZE && remaining > 0; i++)
            {
                if (_slots[i] == null)
                {
                    int toAdd = Math.Min(remaining, itemDef.MaxStack);
                    _slots[i] = new ItemStack(itemDef, toAdd);
                    remaining -= toAdd;
                }
            }

            if (remaining > 0)
            {
                Console.WriteLine($"[Inventory] Inventário cheio! Não foi possível adicionar {remaining}x {itemDef.Name}");
                return false;
            }

            Console.WriteLine($"[Inventory] ✅ Adicionado {quantity}x {itemDef.Name}");
            return true;
        }

        public bool RemoveItem(int itemId, int quantity = 1)
        {
            int remaining = quantity;

            for (int i = 0; i < INVENTORY_SIZE && remaining > 0; i++)
            {
                if (_slots[i] != null && _slots[i].ItemId == itemId)
                {
                    int toRemove = Math.Min(remaining, _slots[i].Quantity);
                    _slots[i].Remove(toRemove);
                    remaining -= toRemove;

                    if (_slots[i].IsEmpty())
                    {
                        _slots[i] = null;
                    }
                }
            }

            return remaining == 0;
        }

        public ItemDefinition ConsumeItem(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= INVENTORY_SIZE)
                return null;

            var stack = _slots[slotIndex];
            if (stack == null || !stack.Definition.IsConsumable)
                return null;

            var itemDef = stack.Definition;

            stack.Remove(1);
            if (stack.IsEmpty())
            {
                _slots[slotIndex] = null;
            }

            Console.WriteLine($"[Inventory] 🍴 Consumiu {itemDef.Name} (slot {slotIndex})");
            return itemDef;
        }

        /// <summary>
        /// ⭐ NOVO: Pega ItemStack de um slot (sem consumir) para validação
        /// </summary>
        public ItemStack GetSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= INVENTORY_SIZE)
                return null;

            return _slots[slotIndex];
        }

        public bool MoveItem(int fromSlot, int toSlot)
        {
            if (fromSlot < 0 || fromSlot >= INVENTORY_SIZE) return false;
            if (toSlot < 0 || toSlot >= INVENTORY_SIZE) return false;
            if (fromSlot == toSlot) return false;

            var fromStack = _slots[fromSlot];
            var toStack = _slots[toSlot];

            if (fromStack == null) return false;

            if (toStack == null)
            {
                _slots[toSlot] = fromStack;
                _slots[fromSlot] = null;
                return true;
            }

            if (fromStack.ItemId == toStack.ItemId)
            {
                int remaining = toStack.Add(fromStack.Quantity);
                if (remaining == 0)
                {
                    _slots[fromSlot] = null;
                }
                else
                {
                    fromStack.Quantity = remaining;
                }
                return true;
            }

            _slots[fromSlot] = toStack;
            _slots[toSlot] = fromStack;
            return true;
        }

        public bool HasItem(int itemId, int quantity = 1)
        {
            int count = 0;
            for (int i = 0; i < INVENTORY_SIZE; i++)
            {
                if (_slots[i] != null && _slots[i].ItemId == itemId)
                {
                    count += _slots[i].Quantity;
                    if (count >= quantity) return true;
                }
            }
            return false;
        }

        public int CountItem(int itemId)
        {
            int count = 0;
            for (int i = 0; i < INVENTORY_SIZE; i++)
            {
                if (_slots[i] != null && _slots[i].ItemId == itemId)
                {
                    count += _slots[i].Quantity;
                }
            }
            return count;
        }

        public void SelectHotbarSlot(int index)
        {
            if (index >= 0 && index < HOTBAR_SIZE)
            {
                _selectedHotbarSlot = index;
            }
        }

        public int GetSelectedHotbarSlot() => _selectedHotbarSlot;

        public ItemStack GetSelectedItem()
        {
            return _slots[_selectedHotbarSlot];
        }

        public ItemStack[] GetAllSlots() => _slots;

        public void Clear()
        {
            for (int i = 0; i < INVENTORY_SIZE; i++)
            {
                _slots[i] = null;
            }
        }

        private void AddItemDebug(int itemId, int quantity)
        {
            var itemDef = ItemDatabase.GetItem(itemId);
            if (itemDef == null) return;

            for (int i = 0; i < INVENTORY_SIZE && quantity > 0; i++)
            {
                if (_slots[i] == null)
                {
                    int toAdd = Math.Min(quantity, itemDef.MaxStack);
                    _slots[i] = new ItemStack(itemDef, toAdd);
                    quantity -= toAdd;
                    break;
                }
            }
        }

        public override string ToString()
        {
            int usedSlots = _slots.Count(s => s != null);
            return $"Inventory: {usedSlots}/{INVENTORY_SIZE} slots used";
        }
    }
}