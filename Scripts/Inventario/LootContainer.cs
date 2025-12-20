using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Container de loot no mundo (caixas, barris, corpos)
/// Pode ser aberto por jogadores
/// Server Authoritative
/// </summary>
public class LootContainer : MonoBehaviour
{
    [Header("Container Info")]
    [SerializeField] private string containerName = "Loot Container";
    [SerializeField] private int containerSize = 12; // Número de slots
    [SerializeField] private LootContainerType containerType = LootContainerType.Crate;

    [Header("Loot Table")]
    [SerializeField] private LootTable lootTable;
    [SerializeField] private bool spawnLootOnStart = true;

    [Header("Access")]
    [SerializeField] private bool requiresKey = false;
    [SerializeField] private int keyItemId = -1;
    [SerializeField] private bool isLocked = false;

    [Header("Respawn")]
    [SerializeField] private bool canRespawn = true;
    [SerializeField] private float respawnTime = 300f; // 5 minutos
    private float nextRespawnTime = 0f;

    [Header("Debug")]
    [SerializeField] private bool showDebug = false;

    // Inventário do container
    private InventorySlot[] containerSlots;
    private bool isInitialized = false;
    private bool hasBeenLooted = false;

    // Jogadores que estão acessando
    private List<int> playersAccessing = new List<int>();

    // Network ID
    private int containerId = -1;

    private void Start()
    {
        InitializeContainer();

        if (spawnLootOnStart)
        {
            SpawnLoot();
        }
    }

    /// <summary>
    /// Inicializa o container
    /// </summary>
    private void InitializeContainer()
    {
        if (isInitialized) return;

        // Gera ID único
        containerId = GetInstanceID();

        // Cria slots
        containerSlots = new InventorySlot[containerSize];
        for (int i = 0; i < containerSize; i++)
        {
            containerSlots[i] = new InventorySlot();
        }

        isInitialized = true;

        if (showDebug)
            Debug.Log($"[LootContainer] {containerName} initialized with {containerSize} slots");
    }

    private void Update()
    {
        // Respawn de loot
        if (canRespawn && hasBeenLooted && Time.time >= nextRespawnTime)
        {
            RespawnLoot();
        }
    }

    #region LOOT GENERATION

    /// <summary>
    /// Spawna loot baseado na loot table
    /// </summary>
    public void SpawnLoot()
    {
        if (!isInitialized || lootTable == null)
            return;

        // Limpa container
        ClearContainer();

        // Gera loot
        List<LootDrop> drops = lootTable.GenerateLoot();

        foreach (var drop in drops)
        {
            AddItemToContainer(drop.itemId, drop.amount);
        }

        hasBeenLooted = false;

        if (showDebug)
            Debug.Log($"[LootContainer] {containerName} spawned {drops.Count} items");
    }

    /// <summary>
    /// Respawna loot após timer
    /// </summary>
    private void RespawnLoot()
    {
        SpawnLoot();
        
        if (showDebug)
            Debug.Log($"[LootContainer] {containerName} respawned loot");
    }

    #endregion

    #region CONTAINER OPERATIONS

    /// <summary>
    /// Adiciona item ao container
    /// </summary>
    public bool AddItemToContainer(int itemId, int amount, float durability = 100f)
    {
        ItemData itemData = ItemDatabase.Instance.GetItem(itemId);
        if (itemData == null) return false;

        // Procura slot vazio
        for (int i = 0; i < containerSize; i++)
        {
            if (!containerSlots[i].HasItem())
            {
                containerSlots[i].itemId = itemId;
                containerSlots[i].amount = amount;
                containerSlots[i].durability = durability;
                return true;
            }
        }

        if (showDebug)
            Debug.LogWarning($"[LootContainer] {containerName} is full!");

        return false;
    }

    /// <summary>
    /// Remove item do container
    /// </summary>
    public bool RemoveItemFromContainer(int slotIndex, int amount)
    {
        if (slotIndex < 0 || slotIndex >= containerSize)
            return false;

        if (!containerSlots[slotIndex].HasItem())
            return false;

        if (containerSlots[slotIndex].amount < amount)
            return false;

        containerSlots[slotIndex].amount -= amount;

        if (containerSlots[slotIndex].amount <= 0)
        {
            containerSlots[slotIndex].Clear();
        }

        // Marca como lootado se ficou vazio
        if (IsEmpty())
        {
            OnContainerLooted();
        }

        return true;
    }

    /// <summary>
    /// Limpa container
    /// </summary>
    public void ClearContainer()
    {
        for (int i = 0; i < containerSize; i++)
        {
            containerSlots[i].Clear();
        }
    }

    /// <summary>
    /// Verifica se container está vazio
    /// </summary>
    public bool IsEmpty()
    {
        for (int i = 0; i < containerSize; i++)
        {
            if (containerSlots[i].HasItem())
                return false;
        }
        return true;
    }

    #endregion

    #region PLAYER ACCESS

    /// <summary>
    /// Jogador tenta abrir container
    /// </summary>
    public bool TryOpen(int playerId, InventorySystem playerInventory)
    {
        // Verifica lock
        if (isLocked)
        {
            if (requiresKey && playerInventory != null)
            {
                // Verifica se tem chave
                if (playerInventory.CountItem(keyItemId) <= 0)
                {
                    if (showDebug)
                        Debug.Log($"[LootContainer] Player {playerId} doesn't have key!");
                    return false;
                }
            }
            else
            {
                if (showDebug)
                    Debug.Log($"[LootContainer] Container is locked!");
                return false;
            }
        }

        // Adiciona à lista de acessos
        if (!playersAccessing.Contains(playerId))
        {
            playersAccessing.Add(playerId);
        }

        if (showDebug)
            Debug.Log($"[LootContainer] Player {playerId} opened {containerName}");

        return true;
    }

    /// <summary>
    /// Jogador fecha container
    /// </summary>
    public void CloseContainer(int playerId)
    {
        playersAccessing.Remove(playerId);

        if (showDebug)
            Debug.Log($"[LootContainer] Player {playerId} closed {containerName}");
    }

    /// <summary>
    /// Transfere item do container para jogador
    /// </summary>
    public bool TransferToPlayer(int slotIndex, int amount, InventorySystem playerInventory)
    {
        if (playerInventory == null)
            return false;

        if (slotIndex < 0 || slotIndex >= containerSize)
            return false;

        InventorySlot slot = containerSlots[slotIndex];
        if (!slot.HasItem())
            return false;

        if (slot.amount < amount)
            amount = slot.amount;

        // Tenta adicionar ao inventário do jogador
        if (playerInventory.AddItem(slot.itemId, amount, slot.durability))
        {
            RemoveItemFromContainer(slotIndex, amount);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Transfere item do jogador para container
    /// </summary>
    public bool TransferFromPlayer(int itemId, int amount, float durability, InventorySystem playerInventory)
    {
        if (playerInventory == null)
            return false;

        // Verifica se container tem espaço
        bool hasSpace = false;
        for (int i = 0; i < containerSize; i++)
        {
            if (!containerSlots[i].HasItem())
            {
                hasSpace = true;
                break;
            }
        }

        if (!hasSpace)
            return false;

        // Remove do jogador
        if (playerInventory.RemoveItemById(itemId, amount))
        {
            // Adiciona ao container
            return AddItemToContainer(itemId, amount, durability);
        }

        return false;
    }

    #endregion

    #region EVENTS

    /// <summary>
    /// Chamado quando container é completamente lootado
    /// </summary>
    private void OnContainerLooted()
    {
        hasBeenLooted = true;

        if (canRespawn)
        {
            nextRespawnTime = Time.time + respawnTime;

            if (showDebug)
                Debug.Log($"[LootContainer] {containerName} will respawn in {respawnTime}s");
        }
        else
        {
            // Container único, destrói
            if (showDebug)
                Debug.Log($"[LootContainer] {containerName} was looted and won't respawn");
            
            Destroy(gameObject, 5f);
        }
    }

    #endregion

    #region GETTERS

    /// <summary>
    /// Retorna slots do container
    /// </summary>
    public InventorySlot[] GetSlots() => containerSlots;

    /// <summary>
    /// Retorna tamanho do container
    /// </summary>
    public int GetSize() => containerSize;

    /// <summary>
    /// Retorna nome do container
    /// </summary>
    public string GetName() => containerName;

    /// <summary>
    /// Retorna se está locked
    /// </summary>
    public bool IsLocked() => isLocked;

    /// <summary>
    /// Define lock
    /// </summary>
    public void SetLocked(bool locked)
    {
        isLocked = locked;
    }

    /// <summary>
    /// Retorna ID do container
    /// </summary>
    public int GetContainerId() => containerId;

    #endregion

    #region DEBUG

    private void OnDrawGizmos()
    {
        if (!showDebug) return;

        // Desenha esfera sobre o container
        Gizmos.color = isLocked ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position + Vector3.up, 0.5f);

        // Label
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 1.5f,
            $"{containerName}\n{(IsEmpty() ? "Empty" : "Has Loot")}"
        );
        #endif
    }

    #endregion
}

/// <summary>
/// Tipos de container
/// </summary>
public enum LootContainerType
{
    Crate,          // Caixa normal
    Barrel,         // Barril
    MilitaryCrate,  // Caixa militar
    EliteCrate,     // Caixa elite
    SupplyDrop,     // Air drop
    PlayerCorpse,   // Corpo de jogador
    AnimalCorpse    // Corpo de animal
}

/// <summary>
/// Tabela de loot (ScriptableObject)
/// </summary>
[CreateAssetMenu(fileName = "New Loot Table", menuName = "Rust Clone/Loot/Loot Table")]
public class LootTable : ScriptableObject
{
    [Header("Loot Settings")]
    public int minItems = 1;
    public int maxItems = 5;

    [Header("Loot Entries")]
    public LootEntry[] entries;

    /// <summary>
    /// Gera loot aleatório baseado nas chances
    /// </summary>
    public List<LootDrop> GenerateLoot()
    {
        List<LootDrop> drops = new List<LootDrop>();

        if (entries == null || entries.Length == 0)
            return drops;

        int itemCount = Random.Range(minItems, maxItems + 1);

        for (int i = 0; i < itemCount; i++)
        {
            LootEntry entry = RollLoot();
            if (entry != null)
            {
                int amount = Random.Range(entry.minAmount, entry.maxAmount + 1);
                drops.Add(new LootDrop
                {
                    itemId = entry.itemId,
                    amount = amount
                });
            }
        }

        return drops;
    }

    /// <summary>
    /// Rola chance para cada item
    /// </summary>
    private LootEntry RollLoot()
    {
        float totalWeight = 0f;
        foreach (var entry in entries)
        {
            totalWeight += entry.dropChance;
        }

        float roll = Random.Range(0f, totalWeight);
        float current = 0f;

        foreach (var entry in entries)
        {
            current += entry.dropChance;
            if (roll <= current)
                return entry;
        }

        return null;
    }
}

/// <summary>
/// Entrada na tabela de loot
/// </summary>
[System.Serializable]
public class LootEntry
{
    public int itemId;
    public int minAmount = 1;
    public int maxAmount = 1;
    [Range(0f, 100f)]
    public float dropChance = 50f;
}

/// <summary>
/// Item dropado
/// </summary>
public class LootDrop
{
    public int itemId;
    public int amount;
}