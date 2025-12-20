using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sistema de crafting completo estilo Rust
/// Suporta fila de crafting, estações e blueprints
/// </summary>
public class CraftingSystem : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int maxQueueSize = 5;
    [SerializeField] private bool canCancelCrafting = true;

    [Header("Current Station")]
    [SerializeField] private CraftingStation currentStation = CraftingStation.None;

    [Header("Debug")]
    [SerializeField] private bool showDebug = false;

    // Estado
    private int clientId = -1;
    private bool isLocalPlayer = false;
    private bool isInitialized = false;

    // Fila de crafting
    private List<CraftingJob> craftingQueue = new List<CraftingJob>();
    private CraftingJob currentJob = null;

    // Blueprints aprendidos
    private HashSet<int> learnedBlueprints = new HashSet<int>();

    // Componentes
    private InventorySystem inventorySystem;

    // Callbacks
    public Action<CraftingJob> OnCraftingStarted;
    public Action<CraftingJob> OnCraftingCompleted;
    public Action<CraftingJob> OnCraftingCancelled;
    public Action OnQueueChanged;
    public Action<int> OnBlueprintLearned;

    private void Awake()
    {
        inventorySystem = GetComponent<InventorySystem>();
    }

    /// <summary>
    /// Inicializa o sistema
    /// </summary>
    public void Initialize(int id, bool isLocal)
    {
        clientId = id;
        isLocalPlayer = isLocal;
        isInitialized = true;

        // Aprende todos os itens básicos (sem blueprint necessário)
        LearnBasicRecipes();

        Debug.Log($"[CraftingSystem] Initialized (ID: {clientId}, Local: {isLocal})");
    }

    private void Update()
    {
        if (!isInitialized || !isLocalPlayer) return;

        // Processa crafting atual
        ProcessCurrentCrafting();
    }

    #region CRAFTING QUEUE

    /// <summary>
    /// Adiciona item à fila de crafting
    /// </summary>
    public bool QueueCraft(int itemId, int amount = 1)
    {
        if (!isInitialized || inventorySystem == null)
            return false;

        // Valida item
        ItemData itemData = ItemDatabase.Instance.GetItem(itemId);
        if (itemData == null)
        {
            Debug.LogError($"[CraftingSystem] Item {itemId} not found!");
            return false;
        }

        // Verifica se é craftável
        if (!itemData.isCraftable || itemData.recipe == null)
        {
            if (showDebug)
                Debug.LogWarning($"[CraftingSystem] {itemData.itemName} is not craftable!");
            return false;
        }

        // Verifica blueprint
        if (itemData.canBeResearched && !HasBlueprint(itemId))
        {
            if (showDebug)
                Debug.LogWarning($"[CraftingSystem] Blueprint for {itemData.itemName} not learned!");
            return false;
        }

        // Verifica estação necessária
        if (itemData.requiredStation != CraftingStation.None && currentStation != itemData.requiredStation)
        {
            if (showDebug)
                Debug.LogWarning($"[CraftingSystem] {itemData.itemName} requires {itemData.requiredStation}!");
            return false;
        }

        // Verifica espaço na fila
        if (craftingQueue.Count >= maxQueueSize)
        {
            if (showDebug)
                Debug.LogWarning("[CraftingSystem] Crafting queue is full!");
            return false;
        }

        // Cria job de crafting
        CraftingJob job = new CraftingJob
        {
            itemData = itemData,
            amount = amount,
            totalCraftTime = itemData.craftTime,
            remainingTime = itemData.craftTime,
            startTime = Time.time
        };

        // Adiciona à fila
        craftingQueue.Add(job);
        OnQueueChanged?.Invoke();

        if (showDebug)
            Debug.Log($"[CraftingSystem] Queued {amount}x {itemData.itemName}");

        // Se não tem job atual, inicia
        if (currentJob == null)
        {
            StartNextCraftingJob();
        }

        return true;
    }

    /// <summary>
    /// Cancela crafting atual
    /// </summary>
    public bool CancelCurrentCrafting()
    {
        if (!canCancelCrafting || currentJob == null)
            return false;

        // Devolve recursos (parcialmente se já começou)
        float refundPercent = currentJob.remainingTime / currentJob.totalCraftTime;
        RefundIngredients(currentJob.itemData, refundPercent);

        OnCraftingCancelled?.Invoke(currentJob);

        if (showDebug)
            Debug.Log($"[CraftingSystem] Cancelled crafting {currentJob.itemData.itemName}");

        currentJob = null;

        // Inicia próximo da fila
        StartNextCraftingJob();

        return true;
    }

    /// <summary>
    /// Cancela item específico da fila
    /// </summary>
    public bool CancelQueuedCrafting(int queueIndex)
    {
        if (queueIndex < 0 || queueIndex >= craftingQueue.Count)
            return false;

        CraftingJob job = craftingQueue[queueIndex];

        // Se for o atual, usa método específico
        if (job == currentJob)
            return CancelCurrentCrafting();

        // Remove da fila (ainda não começou, devolve tudo)
        craftingQueue.RemoveAt(queueIndex);
        RefundIngredients(job.itemData, 1f);

        OnQueueChanged?.Invoke();

        if (showDebug)
            Debug.Log($"[CraftingSystem] Removed {job.itemData.itemName} from queue");

        return true;
    }

    /// <summary>
    /// Limpa toda a fila
    /// </summary>
    public void ClearQueue()
    {
        // Cancela atual
        CancelCurrentCrafting();

        // Devolve recursos de todos da fila
        foreach (var job in craftingQueue)
        {
            RefundIngredients(job.itemData, 1f);
        }

        craftingQueue.Clear();
        OnQueueChanged?.Invoke();

        if (showDebug)
            Debug.Log("[CraftingSystem] Queue cleared");
    }

    #endregion

    #region CRAFTING PROCESS

    /// <summary>
    /// Inicia próximo job da fila
    /// </summary>
    private void StartNextCraftingJob()
    {
        if (craftingQueue.Count == 0)
        {
            currentJob = null;
            return;
        }

        currentJob = craftingQueue[0];
        craftingQueue.RemoveAt(0);

        // Consome ingredientes
        if (!currentJob.itemData.recipe.ConsumeIngredients(inventorySystem))
        {
            if (showDebug)
                Debug.LogError($"[CraftingSystem] Failed to consume ingredients for {currentJob.itemData.itemName}!");
            
            currentJob = null;
            StartNextCraftingJob(); // Tenta próximo
            return;
        }

        OnCraftingStarted?.Invoke(currentJob);
        OnQueueChanged?.Invoke();

        if (showDebug)
            Debug.Log($"[CraftingSystem] Started crafting {currentJob.itemData.itemName}");
    }

    /// <summary>
    /// Processa crafting atual
    /// </summary>
    private void ProcessCurrentCrafting()
    {
        if (currentJob == null) return;

        // Reduz tempo
        currentJob.remainingTime -= Time.deltaTime;

        // Completo?
        if (currentJob.remainingTime <= 0)
        {
            CompleteCrafting();
        }
    }

    /// <summary>
    /// Completa crafting atual
    /// </summary>
    private void CompleteCrafting()
    {
        if (currentJob == null) return;

        // Adiciona item ao inventário
        bool success = inventorySystem.AddItem(
            currentJob.itemData.itemId,
            currentJob.itemData.recipe.outputAmount,
            currentJob.itemData.maxDurability
        );

        if (!success)
        {
            if (showDebug)
                Debug.LogWarning($"[CraftingSystem] Failed to add {currentJob.itemData.itemName} to inventory!");
            
            // TODO: Dropar no chão se inventário cheio
        }

        OnCraftingCompleted?.Invoke(currentJob);

        if (showDebug)
            Debug.Log($"[CraftingSystem] Completed crafting {currentJob.itemData.itemName}");

        // Se tinha mais de 1 no amount, adiciona de volta à fila
        if (currentJob.amount > 1)
        {
            currentJob.amount--;
            currentJob.remainingTime = currentJob.totalCraftTime;
            craftingQueue.Insert(0, currentJob);
        }

        currentJob = null;

        // Inicia próximo
        StartNextCraftingJob();
    }

    /// <summary>
    /// Devolve ingredientes ao cancelar
    /// </summary>
    private void RefundIngredients(ItemData itemData, float percent)
    {
        if (itemData.recipe == null || itemData.recipe.ingredients == null)
            return;

        foreach (var ingredient in itemData.recipe.ingredients)
        {
            int refundAmount = Mathf.CeilToInt(ingredient.amount * percent);
            if (refundAmount > 0)
            {
                inventorySystem.AddItem(ingredient.itemId, refundAmount);
            }
        }
    }

    #endregion

    #region BLUEPRINTS

    /// <summary>
    /// Aprende blueprint
    /// </summary>
    public bool LearnBlueprint(int itemId)
    {
        ItemData itemData = ItemDatabase.Instance.GetItem(itemId);
        if (itemData == null || !itemData.canBeResearched)
            return false;

        if (learnedBlueprints.Contains(itemId))
        {
            if (showDebug)
                Debug.Log($"[CraftingSystem] Blueprint for {itemData.itemName} already learned!");
            return false;
        }

        learnedBlueprints.Add(itemId);
        OnBlueprintLearned?.Invoke(itemId);

        if (showDebug)
            Debug.Log($"[CraftingSystem] Learned blueprint: {itemData.itemName}");

        return true;
    }

    /// <summary>
    /// Verifica se tem blueprint
    /// </summary>
    public bool HasBlueprint(int itemId)
    {
        ItemData itemData = ItemDatabase.Instance.GetItem(itemId);
        if (itemData == null)
            return false;

        // Itens que não precisam de blueprint sempre retornam true
        if (!itemData.canBeResearched)
            return true;

        return learnedBlueprints.Contains(itemId);
    }

    /// <summary>
    /// Aprende receitas básicas (sem blueprint necessário)
    /// </summary>
    private void LearnBasicRecipes()
    {
        List<ItemData> allItems = ItemDatabase.Instance.GetAllItems();
        
        foreach (var item in allItems)
        {
            if (item.isCraftable && !item.canBeResearched)
            {
                learnedBlueprints.Add(item.itemId);
            }
        }

        if (showDebug)
            Debug.Log($"[CraftingSystem] Learned {learnedBlueprints.Count} basic recipes");
    }

    /// <summary>
    /// Retorna todos os blueprints aprendidos
    /// </summary>
    public List<int> GetLearnedBlueprints()
    {
        return new List<int>(learnedBlueprints);
    }

    #endregion

    #region CRAFTING STATION

    /// <summary>
    /// Define estação de crafting atual
    /// </summary>
    public void SetCraftingStation(CraftingStation station)
    {
        currentStation = station;

        if (showDebug)
            Debug.Log($"[CraftingSystem] Crafting station set to: {station}");
    }

    /// <summary>
    /// Retorna estação atual
    /// </summary>
    public CraftingStation GetCurrentStation() => currentStation;

    #endregion

    #region QUERIES

    /// <summary>
    /// Verifica se pode craftar item
    /// </summary>
    public bool CanCraft(int itemId)
    {
        ItemData itemData = ItemDatabase.Instance.GetItem(itemId);
        if (itemData == null || !itemData.isCraftable)
            return false;

        // Verifica blueprint
        if (!HasBlueprint(itemId))
            return false;

        // Verifica estação
        if (itemData.requiredStation != CraftingStation.None && currentStation != itemData.requiredStation)
            return false;

        // Verifica ingredientes
        if (itemData.recipe != null && inventorySystem != null)
            return itemData.recipe.CanCraft(inventorySystem);

        return false;
    }

    /// <summary>
    /// Retorna job atual
    /// </summary>
    public CraftingJob GetCurrentJob() => currentJob;

    /// <summary>
    /// Retorna fila de crafting
    /// </summary>
    public List<CraftingJob> GetQueue() => craftingQueue;

    /// <summary>
    /// Retorna se está craftando
    /// </summary>
    public bool IsCrafting() => currentJob != null;

    /// <summary>
    /// Retorna progresso do craft atual (0-1)
    /// </summary>
    public float GetCraftingProgress()
    {
        if (currentJob == null)
            return 0f;

        return 1f - (currentJob.remainingTime / currentJob.totalCraftTime);
    }

    #endregion

    #region DEBUG

    private void OnGUI()
    {
        if (!showDebug || !isLocalPlayer) return;

        float width = 300f;
        float height = 200f;
        float x = Screen.width - width - 20f;
        float y = Screen.height - height - 20f;

        GUI.color = Color.black;
        GUI.Box(new Rect(x - 2, y - 2, width + 4, height + 4), "");

        GUI.color = Color.white;
        GUILayout.BeginArea(new Rect(x, y, width, height));
        
        GUILayout.Label("=== CRAFTING ===", new GUIStyle { alignment = TextAnchor.MiddleCenter });
        GUILayout.Label($"Station: {currentStation}");
        
        if (currentJob != null)
        {
            GUILayout.Label($"Crafting: {currentJob.itemData.itemName}");
            GUILayout.Label($"Progress: {GetCraftingProgress() * 100f:F0}%");
            GUILayout.Label($"Time: {currentJob.remainingTime:F1}s");
        }
        else
        {
            GUILayout.Label("Not crafting");
        }

        GUILayout.Label($"Queue: {craftingQueue.Count}/{maxQueueSize}");
        GUILayout.Label($"Blueprints: {learnedBlueprints.Count}");

        GUILayout.EndArea();
    }

    #endregion
}

/// <summary>
/// Representa um job de crafting
/// </summary>
[Serializable]
public class CraftingJob
{
    public ItemData itemData;
    public int amount;
    public float totalCraftTime;
    public float remainingTime;
    public float startTime;
}