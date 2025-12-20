using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Database singleton que armazena todos os itens do jogo
/// Carrega automaticamente de Resources/Items
/// </summary>
public class ItemDatabase : MonoBehaviour
{
    public static ItemDatabase Instance { get; private set; }

    [Header("Database")]
    [SerializeField] private List<ItemData> allItems = new List<ItemData>();

    // Cache por ID para acesso rápido
    private Dictionary<int, ItemData> itemDictionary = new Dictionary<int, ItemData>();

    private void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Carrega e indexa itens
        LoadAllItems();
        BuildItemDictionary();
    }

    /// <summary>
    /// Carrega todos os itens de Resources
    /// </summary>
    private void LoadAllItems()
    {
        // Carrega de Resources/Items
        ItemData[] items = Resources.LoadAll<ItemData>("Items");
        
        allItems.Clear();
        allItems.AddRange(items);

        Debug.Log($"[ItemDatabase] Loaded {allItems.Count} items");
    }

    /// <summary>
    /// Constrói dicionário para acesso rápido por ID
    /// </summary>
    private void BuildItemDictionary()
    {
        itemDictionary.Clear();

        foreach (var item in allItems)
        {
            if (item == null)
            {
                Debug.LogWarning("[ItemDatabase] Null item found!");
                continue;
            }

            if (itemDictionary.ContainsKey(item.itemId))
            {
                Debug.LogWarning($"[ItemDatabase] Duplicate item ID: {item.itemId} ({item.itemName})");
                continue;
            }

            itemDictionary.Add(item.itemId, item);
        }

        Debug.Log($"[ItemDatabase] Indexed {itemDictionary.Count} items");
    }

    #region GET ITEMS

    /// <summary>
    /// Retorna item por ID
    /// </summary>
    public ItemData GetItem(int itemId)
    {
        if (itemDictionary.TryGetValue(itemId, out ItemData item))
            return item;

        Debug.LogWarning($"[ItemDatabase] Item {itemId} not found!");
        return null;
    }

    /// <summary>
    /// Retorna item por nome
    /// </summary>
    public ItemData GetItemByName(string itemName)
    {
        foreach (var item in allItems)
        {
            if (item.itemName.Equals(itemName, System.StringComparison.OrdinalIgnoreCase))
                return item;
        }

        Debug.LogWarning($"[ItemDatabase] Item '{itemName}' not found!");
        return null;
    }

    /// <summary>
    /// Retorna todos os itens
    /// </summary>
    public List<ItemData> GetAllItems() => allItems;

    /// <summary>
    /// Retorna itens por tipo
    /// </summary>
    public List<ItemData> GetItemsByType(ItemType type)
    {
        List<ItemData> result = new List<ItemData>();
        
        foreach (var item in allItems)
        {
            if (item.itemType == type)
                result.Add(item);
        }

        return result;
    }

    /// <summary>
    /// Retorna itens por categoria
    /// </summary>
    public List<ItemData> GetItemsByCategory(ItemCategory category)
    {
        List<ItemData> result = new List<ItemData>();
        
        foreach (var item in allItems)
        {
            if (item.category == category)
                result.Add(item);
        }

        return result;
    }

    /// <summary>
    /// Retorna itens craftáveis
    /// </summary>
    public List<ItemData> GetCraftableItems()
    {
        List<ItemData> result = new List<ItemData>();
        
        foreach (var item in allItems)
        {
            if (item.isCraftable && item.recipe != null && item.recipe.ingredients != null)
                result.Add(item);
        }

        return result;
    }

    /// <summary>
    /// Retorna itens craftáveis em uma estação específica
    /// </summary>
    public List<ItemData> GetCraftableItemsAtStation(CraftingStation station)
    {
        List<ItemData> result = new List<ItemData>();
        
        foreach (var item in allItems)
        {
            if (item.isCraftable && item.requiredStation == station)
                result.Add(item);
        }

        return result;
    }

    #endregion

    #region VALIDATION

    /// <summary>
    /// Valida se item existe
    /// </summary>
    public bool ItemExists(int itemId)
    {
        return itemDictionary.ContainsKey(itemId);
    }

    /// <summary>
    /// Valida todos os itens do database
    /// </summary>
    [ContextMenu("Validate All Items")]
    public void ValidateAllItems()
    {
        Debug.Log("=== VALIDATING ITEM DATABASE ===");

        int errors = 0;
        int warnings = 0;

        // Verifica IDs duplicados
        HashSet<int> seenIds = new HashSet<int>();
        foreach (var item in allItems)
        {
            if (item == null)
            {
                Debug.LogError("Null item in database!");
                errors++;
                continue;
            }

            if (seenIds.Contains(item.itemId))
            {
                Debug.LogError($"Duplicate ID {item.itemId}: {item.itemName}");
                errors++;
            }
            seenIds.Add(item.itemId);

            // Valida receitas
            if (item.isCraftable)
            {
                if (item.recipe == null || item.recipe.ingredients == null || item.recipe.ingredients.Length == 0)
                {
                    Debug.LogWarning($"{item.itemName}: Craftable but no recipe!");
                    warnings++;
                }
                else
                {
                    // Valida ingredientes
                    foreach (var ingredient in item.recipe.ingredients)
                    {
                        if (!ItemExists(ingredient.itemId))
                        {
                            Debug.LogError($"{item.itemName}: Recipe uses non-existent item ID {ingredient.itemId}");
                            errors++;
                        }
                    }
                }
            }

            // Valida prefabs
            if (item.worldPrefab == null)
            {
                Debug.LogWarning($"{item.itemName}: No world prefab!");
                warnings++;
            }

            // Valida ícone
            if (item.icon == null)
            {
                Debug.LogWarning($"{item.itemName}: No icon!");
                warnings++;
            }
        }

        Debug.Log($"=== VALIDATION COMPLETE ===");
        Debug.Log($"Total items: {allItems.Count}");
        Debug.Log($"Errors: {errors}");
        Debug.Log($"Warnings: {warnings}");
    }

    #endregion

    #region HELPER METHODS

    /// <summary>
    /// Cria itens de exemplo para teste
    /// </summary>
    [ContextMenu("Create Example Items")]
    public void CreateExampleItems()
    {
        Debug.Log("[ItemDatabase] Creating example items...");
        
        // Este método seria usado no editor para criar itens de exemplo
        // No build real, você criaria os ScriptableObjects manualmente no editor

        #if UNITY_EDITOR
        CreateExampleResource("Wood", 1000);
        CreateExampleResource("Stone", 1001);
        CreateExampleResource("Metal Ore", 1002);
        CreateExampleTool("Stone Hatchet", 2000);
        CreateExampleTool("Stone Pickaxe", 2001);
        CreateExampleWeapon("Bow", 3000);
        CreateExampleConsumable("Apple", 4000);
        #endif
    }

#if UNITY_EDITOR
    private void CreateExampleResource(string name, int id)
    {
        ItemData item = ScriptableObject.CreateInstance<ItemData>();
        item.itemId = id;
        item.itemName = name;
        item.itemType = ItemType.Resource;
        item.category = ItemCategory.Resources;
        item.isStackable = true;
        item.maxStackSize = 1000;
        item.description = $"{name} - Basic resource";

        UnityEditor.AssetDatabase.CreateAsset(item, $"Assets/Resources/Items/{name}.asset");
        Debug.Log($"Created: {name}");
    }

    private void CreateExampleTool(string name, int id)
    {
        ItemData item = ScriptableObject.CreateInstance<ItemData>();
        item.itemId = id;
        item.itemName = name;
        item.itemType = ItemType.Tool;
        item.category = ItemCategory.Tools;
        item.isStackable = false;
        item.hasDurability = true;
        item.maxDurability = 100f;
        item.description = $"{name} - Basic tool";

        UnityEditor.AssetDatabase.CreateAsset(item, $"Assets/Resources/Items/{name}.asset");
        Debug.Log($"Created: {name}");
    }

    private void CreateExampleWeapon(string name, int id)
    {
        ItemData item = ScriptableObject.CreateInstance<ItemData>();
        item.itemId = id;
        item.itemName = name;
        item.itemType = ItemType.Weapon;
        item.category = ItemCategory.Weapons;
        item.isStackable = false;
        item.hasDurability = true;
        item.damage = 50f;
        item.range = 30f;
        item.description = $"{name} - Basic weapon";

        UnityEditor.AssetDatabase.CreateAsset(item, $"Assets/Resources/Items/{name}.asset");
        Debug.Log($"Created: {name}");
    }

    private void CreateExampleConsumable(string name, int id)
    {
        ItemData item = ScriptableObject.CreateInstance<ItemData>();
        item.itemId = id;
        item.itemName = name;
        item.itemType = ItemType.Consumable;
        item.category = ItemCategory.Food;
        item.isStackable = true;
        item.maxStackSize = 20;
        item.isConsumable = true;
        item.consumeTime = 2f;
        item.description = $"{name} - Food item";

        item.consumableEffects = new ConsumableEffect[]
        {
            new ConsumableEffect
            {
                effectType = ConsumableEffectType.RestoreHunger,
                amount = 20f
            },
            new ConsumableEffect
            {
                effectType = ConsumableEffectType.RestoreHealth,
                amount = 5f
            }
        };

        UnityEditor.AssetDatabase.CreateAsset(item, $"Assets/Resources/Items/{name}.asset");
        Debug.Log($"Created: {name}");
    }
#endif

    #endregion

    #region DEBUG

    /// <summary>
    /// Lista todos os itens no console
    /// </summary>
    [ContextMenu("List All Items")]
    public void ListAllItems()
    {
        Debug.Log($"=== ITEM DATABASE ({allItems.Count} items) ===");
        
        foreach (var item in allItems)
        {
            if (item != null)
                Debug.Log($"[{item.itemId}] {item.itemName} ({item.itemType})");
        }
    }

    #endregion
}