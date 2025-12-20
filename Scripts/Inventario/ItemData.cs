using UnityEngine;

/// <summary>
/// Define todos os dados de um item do jogo
/// ScriptableObject para fácil criação de itens no editor
/// </summary>
[CreateAssetMenu(fileName = "New Item", menuName = "Rust Clone/Items/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Basic Info")]
    public int itemId;
    public string itemName = "New Item";
    [TextArea(3, 6)]
    public string description = "Item description";
    public Sprite icon;
    public GameObject worldPrefab; // Prefab quando dropado no mundo
    public GameObject heldPrefab; // Prefab nas mãos do jogador

    [Header("Item Type")]
    public ItemType itemType = ItemType.Resource;
    public ItemCategory category = ItemCategory.Misc;

    [Header("Stack")]
    public bool isStackable = true;
    public int maxStackSize = 1000;

    [Header("Durability")]
    public bool hasDurability = false;
    public float maxDurability = 100f;
    public float durabilityLossPerUse = 1f;

    [Header("Crafting")]
    public bool isCraftable = true;
    public float craftTime = 1f; // Segundos
    public CraftingStation requiredStation = CraftingStation.None;
    public CraftingRecipe recipe;

    [Header("Usage")]
    public bool isConsumable = false;
    public float consumeTime = 1f;
    public ConsumableEffect[] consumableEffects;

    [Header("Weapon Stats (if weapon)")]
    public WeaponType weaponType = WeaponType.None;
    public float damage = 0f;
    public float attackSpeed = 1f;
    public float range = 2f;
    public int magazineSize = 0;
    public AmmoType ammoType = AmmoType.None;

    [Header("Wearable Stats (if wearable)")]
    public WearableSlot wearableSlot = WearableSlot.Head;
    public float armorValue = 0f;
    public float coldProtection = 0f;
    public float heatProtection = 0f;
    public float radiationProtection = 0f;

    [Header("Building (if building piece)")]
    public bool isBuildingPiece = false;
    public GameObject buildingPrefab;
    public BuildingGrade buildingGrade = BuildingGrade.Wood;

    [Header("Market")]
    public int scrapValue = 0; // Valor em scrap
    public bool canBeResearched = true;
    public int researchCost = 75;

    [Header("Rarity")]
    public ItemRarity rarity = ItemRarity.Common;

    /// <summary>
    /// Retorna cor baseada na raridade
    /// </summary>
    public Color GetRarityColor()
    {
        switch (rarity)
        {
            case ItemRarity.Common: return Color.white;
            case ItemRarity.Uncommon: return Color.green;
            case ItemRarity.Rare: return Color.blue;
            case ItemRarity.Epic: return new Color(0.6f, 0f, 1f); // Roxo
            case ItemRarity.Legendary: return new Color(1f, 0.5f, 0f); // Laranja
            default: return Color.white;
        }
    }

    /// <summary>
    /// Valida dados do item
    /// </summary>
    private void OnValidate()
    {
        // Auto-gera ID baseado no nome se for 0
        if (itemId == 0)
        {
            itemId = itemName.GetHashCode();
        }

        // Valida max stack
        if (maxStackSize < 1)
            maxStackSize = 1;

        // Itens não stackable devem ter max = 1
        if (!isStackable)
            maxStackSize = 1;

        // Armas sempre tem durabilidade
        if (itemType == ItemType.Weapon)
            hasDurability = true;
    }
}

/// <summary>
/// Tipos de item
/// </summary>
public enum ItemType
{
    Resource,      // Madeira, pedra, minério
    Tool,          // Machado, picareta
    Weapon,        // Armas
    Wearable,      // Roupas, armaduras
    Consumable,    // Comida, remédios
    Ammo,          // Munição
    Building,      // Peças de construção
    Component,     // Componentes de crafting
    Deployable,    // Tool cupboard, fornalha
    Misc           // Outros
}

/// <summary>
/// Categoria de item (para organização)
/// </summary>
public enum ItemCategory
{
    Resources,
    Tools,
    Weapons,
    Clothing,
    Medical,
    Food,
    Ammunition,
    Construction,
    Components,
    Deployables,
    Misc
}

/// <summary>
/// Raridade do item
/// </summary>
public enum ItemRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}

/// <summary>
/// Estações de crafting
/// </summary>
public enum CraftingStation
{
    None,           // Sem estação (crafta na mão)
    Workbench1,     // Tier 1
    Workbench2,     // Tier 2
    Workbench3,     // Tier 3
    Furnace,        // Fornalha
    ResearchTable,  // Research table
}

/// <summary>
/// Tipos de arma
/// </summary>
public enum WeaponType
{
    None,
    Melee,
    Bow,
    Pistol,
    Rifle,
    Shotgun,
    SMG,
    LMG,
    Launcher
}

/// <summary>
/// Tipos de munição
/// </summary>
public enum AmmoType
{
    None,
    Arrow,
    Pistol_556,
    Rifle_556,
    Shotgun_12Gauge,
    Rocket,
    Explosive_556
}

/// <summary>
/// Receita de crafting
/// </summary>
[System.Serializable]
public class CraftingRecipe
{
    public CraftingIngredient[] ingredients;
    public int outputAmount = 1;

    /// <summary>
    /// Verifica se o jogador tem todos os ingredientes
    /// </summary>
    public bool CanCraft(InventorySystem inventory)
    {
        if (ingredients == null || ingredients.Length == 0)
            return false;

        foreach (var ingredient in ingredients)
        {
            if (inventory.CountItem(ingredient.itemId) < ingredient.amount)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Consome ingredientes do inventário
    /// </summary>
    public bool ConsumeIngredients(InventorySystem inventory)
    {
        if (!CanCraft(inventory))
            return false;

        foreach (var ingredient in ingredients)
        {
            if (!inventory.RemoveItemById(ingredient.itemId, ingredient.amount))
                return false;
        }

        return true;
    }
}

/// <summary>
/// Ingrediente de crafting
/// </summary>
[System.Serializable]
public class CraftingIngredient
{
    public int itemId;
    public int amount;
}

/// <summary>
/// Efeito de consumível
/// </summary>
[System.Serializable]
public class ConsumableEffect
{
    public ConsumableEffectType effectType;
    public float amount;
    public float duration = 0f; // 0 = instantâneo

    public void Apply(PlayerStats playerStats)
    {
        if (playerStats == null) return;

        switch (effectType)
        {
            case ConsumableEffectType.RestoreHealth:
                playerStats.GetHealthComponent()?.Heal(amount);
                break;

            case ConsumableEffectType.RestoreHunger:
                playerStats.GetHungerComponent()?.AddHunger(amount);
                break;

            case ConsumableEffectType.RestoreThirst:
                playerStats.GetThirstComponent()?.AddThirst(amount);
                break;

            case ConsumableEffectType.RestoreStamina:
                playerStats.GetStaminaComponent()?.AddStamina(amount);
                break;

            case ConsumableEffectType.Poison:
                // TODO: Implementar sistema de status effects
                Debug.Log($"Applied poison: {amount} damage");
                break;

            case ConsumableEffectType.Radiation:
                // TODO: Implementar radiação
                Debug.Log($"Applied radiation: {amount}");
                break;
        }
    }
}

/// <summary>
/// Tipos de efeito de consumível
/// </summary>
public enum ConsumableEffectType
{
    RestoreHealth,
    RestoreHunger,
    RestoreThirst,
    RestoreStamina,
    Poison,
    Radiation,
    Bleeding,
    Buff,
    Debuff
}

/// <summary>
/// Grades de construção
/// </summary>
public enum BuildingGrade
{
    Twig,
    Wood,
    Stone,
    Metal,
    Armored
}