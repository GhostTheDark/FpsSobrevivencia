using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tool Cupboard (TC) - Sistema de controle de área estilo Rust
/// Previne construção não autorizada e controla decay/upkeep
/// </summary>
public class ToolCupboard : MonoBehaviour
{
    [Header("Cupboard Info")]
    [SerializeField] private int cupboardId = -1;
    [SerializeField] private int ownerId = -1;
    [SerializeField] private List<int> authorizedPlayers = new List<int>();

    [Header("Range")]
    [SerializeField] private float buildingBlockRange = 25f; // Previne construção de não autorizados
    [SerializeField] private float upkeepRange = 30f; // Range de proteção contra decay

    [Header("Upkeep System")]
    [SerializeField] private bool requiresUpkeep = true;
    [SerializeField] private float upkeepTickInterval = 1440f; // 24 minutos (1 dia in-game)
    [SerializeField] private bool hasUpkeepResources = false;
    private float nextUpkeepTime = 0f;

    [Header("Storage")]
    [SerializeField] private int storageSize = 18; // Slots para upkeep resources
    private InventorySlot[] storageSlots;

    [Header("Debug")]
    [SerializeField] private bool showDebug = false;
    [SerializeField] private bool showGizmos = true;

    // Building pieces protegidas
    private List<BuildingPiece> protectedPieces = new List<BuildingPiece>();
    private bool isInitialized = false;

    // Upkeep costs cache
    private Dictionary<BuildingGrade, UpkeepCost> upkeepCosts = new Dictionary<BuildingGrade, UpkeepCost>();

    private void Awake()
    {
        // Inicializa storage
        storageSlots = new InventorySlot[storageSize];
        for (int i = 0; i < storageSize; i++)
        {
            storageSlots[i] = new InventorySlot();
        }

        // Define upkeep costs
        InitializeUpkeepCosts();
    }

    /// <summary>
    /// Inicializa o TC
    /// </summary>
    public void Initialize(int id, int owner)
    {
        if (isInitialized)
        {
            Debug.LogWarning("[ToolCupboard] Already initialized!");
            return;
        }

        cupboardId = id;
        ownerId = owner;

        // Owner é automaticamente autorizado
        authorizedPlayers.Add(owner);

        // Busca building pieces no range
        FindProtectedPieces();

        // Define próximo upkeep
        nextUpkeepTime = Time.time + upkeepTickInterval;

        isInitialized = true;

        if (showDebug)
            Debug.Log($"[ToolCupboard] Initialized (ID: {cupboardId}, Owner: {ownerId})");
    }

    private void Update()
    {
        if (!isInitialized) return;

        // Atualiza lista de peças protegidas periodicamente
        if (Time.frameCount % 300 == 0) // A cada ~5 segundos
        {
            FindProtectedPieces();
        }

        // Processa upkeep
        if (requiresUpkeep && Time.time >= nextUpkeepTime)
        {
            ProcessUpkeep();
        }
    }

    #region AUTHORIZATION

    /// <summary>
    /// Adiciona jogador à lista de autorizados
    /// </summary>
    public bool AuthorizePlayer(int playerId)
    {
        if (authorizedPlayers.Contains(playerId))
            return false;

        authorizedPlayers.Add(playerId);

        if (showDebug)
            Debug.Log($"[ToolCupboard] Player {playerId} authorized");

        return true;
    }

    /// <summary>
    /// Remove jogador da lista de autorizados
    /// </summary>
    public bool DeauthorizePlayer(int playerId)
    {
        // Não pode remover o owner
        if (playerId == ownerId)
            return false;

        bool removed = authorizedPlayers.Remove(playerId);

        if (removed && showDebug)
            Debug.Log($"[ToolCupboard] Player {playerId} deauthorized");

        return removed;
    }

    /// <summary>
    /// Verifica se jogador está autorizado
    /// </summary>
    public bool IsPlayerAuthorized(int playerId)
    {
        return authorizedPlayers.Contains(playerId);
    }

    /// <summary>
    /// Verifica se jogador pode construir nesta área
    /// </summary>
    public bool CanPlayerBuild(int playerId, Vector3 position)
    {
        // Verifica se está no range
        float distance = Vector3.Distance(transform.position, position);
        if (distance > buildingBlockRange)
            return true; // Fora do range, pode construir

        // Dentro do range: só se autorizado
        return IsPlayerAuthorized(playerId);
    }

    #endregion

    #region BUILDING PROTECTION

    /// <summary>
    /// Encontra todas as building pieces no range
    /// </summary>
    private void FindProtectedPieces()
    {
        protectedPieces.Clear();

        // Busca todas as BuildingPieces no range
        Collider[] colliders = Physics.OverlapSphere(transform.position, upkeepRange);
        
        foreach (var col in colliders)
        {
            BuildingPiece piece = col.GetComponentInParent<BuildingPiece>();
            if (piece != null && !protectedPieces.Contains(piece))
            {
                protectedPieces.Add(piece);
                piece.SetInCupboardRange(true);
            }
        }

        if (showDebug)
            Debug.Log($"[ToolCupboard] Protecting {protectedPieces.Count} building pieces");
    }

    /// <summary>
    /// Verifica se uma peça está protegida por este TC
    /// </summary>
    public bool IsProtectingPiece(BuildingPiece piece)
    {
        return protectedPieces.Contains(piece);
    }

    #endregion

    #region UPKEEP SYSTEM

    /// <summary>
    /// Inicializa custos de upkeep
    /// </summary>
    private void InitializeUpkeepCosts()
    {
        // Twig não precisa upkeep
        upkeepCosts[BuildingGrade.Twig] = new UpkeepCost { wood = 0, stone = 0, metal = 0 };
        
        // Wood
        upkeepCosts[BuildingGrade.Wood] = new UpkeepCost { wood = 10, stone = 0, metal = 0 };
        
        // Stone
        upkeepCosts[BuildingGrade.Stone] = new UpkeepCost { wood = 0, stone = 10, metal = 0 };
        
        // Metal
        upkeepCosts[BuildingGrade.Metal] = new UpkeepCost { wood = 0, stone = 0, metal = 10 };
        
        // Armored
        upkeepCosts[BuildingGrade.Armored] = new UpkeepCost { wood = 0, stone = 0, metal = 20 };
    }

    /// <summary>
    /// Processa tick de upkeep
    /// </summary>
    private void ProcessUpkeep()
    {
        nextUpkeepTime = Time.time + upkeepTickInterval;

        if (!requiresUpkeep)
            return;

        // Calcula custo total baseado nas peças
        UpkeepCost totalCost = CalculateTotalUpkeepCost();

        if (showDebug)
            Debug.Log($"[ToolCupboard] Upkeep cost: Wood={totalCost.wood}, Stone={totalCost.stone}, Metal={totalCost.metal}");

        // Verifica se tem recursos
        if (HasUpkeepResources(totalCost))
        {
            // Consome recursos
            ConsumeUpkeepResources(totalCost);
            
            // Repara todas as peças
            RepairAllPieces();

            hasUpkeepResources = true;

            if (showDebug)
                Debug.Log("[ToolCupboard] Upkeep paid, buildings repaired");
        }
        else
        {
            hasUpkeepResources = false;

            if (showDebug)
                Debug.LogWarning("[ToolCupboard] Not enough upkeep resources! Buildings will decay.");
            
            // Remove proteção de decay das peças
            foreach (var piece in protectedPieces)
            {
                if (piece != null)
                    piece.SetInCupboardRange(false);
            }
        }
    }

    /// <summary>
    /// Calcula custo total de upkeep
    /// </summary>
    private UpkeepCost CalculateTotalUpkeepCost()
    {
        UpkeepCost total = new UpkeepCost();

        foreach (var piece in protectedPieces)
        {
            if (piece == null || piece.IsDestroyed())
                continue;

            BuildingGrade grade = piece.GetCurrentGrade();
            if (upkeepCosts.TryGetValue(grade, out UpkeepCost cost))
            {
                total.wood += cost.wood;
                total.stone += cost.stone;
                total.metal += cost.metal;
            }
        }

        return total;
    }

    /// <summary>
    /// Verifica se tem recursos suficientes
    /// </summary>
    private bool HasUpkeepResources(UpkeepCost cost)
    {
        int woodCount = CountItemInStorage(1000); // Wood ID
        int stoneCount = CountItemInStorage(1001); // Stone ID
        int metalCount = CountItemInStorage(1002); // Metal ID

        return woodCount >= cost.wood && 
               stoneCount >= cost.stone && 
               metalCount >= cost.metal;
    }

    /// <summary>
    /// Consome recursos de upkeep
    /// </summary>
    private void ConsumeUpkeepResources(UpkeepCost cost)
    {
        RemoveItemFromStorage(1000, cost.wood); // Wood
        RemoveItemFromStorage(1001, cost.stone); // Stone
        RemoveItemFromStorage(1002, cost.metal); // Metal
    }

    /// <summary>
    /// Repara todas as peças protegidas
    /// </summary>
    private void RepairAllPieces()
    {
        foreach (var piece in protectedPieces)
        {
            if (piece != null && !piece.IsDestroyed())
            {
                float maxHealth = piece.GetMaxHealth();
                piece.Repair(maxHealth); // Reparo completo
            }
        }
    }

    #endregion

    #region STORAGE

    /// <summary>
    /// Adiciona item ao storage do TC
    /// </summary>
    public bool AddItemToStorage(int itemId, int amount)
    {
        // Procura slot vazio ou com mesmo item
        for (int i = 0; i < storageSize; i++)
        {
            if (!storageSlots[i].HasItem())
            {
                storageSlots[i].itemId = itemId;
                storageSlots[i].amount = amount;
                return true;
            }
            else if (storageSlots[i].itemId == itemId)
            {
                storageSlots[i].amount += amount;
                return true;
            }
        }

        return false; // Storage cheio
    }

    /// <summary>
    /// Remove item do storage
    /// </summary>
    private bool RemoveItemFromStorage(int itemId, int amount)
    {
        int remaining = amount;

        for (int i = 0; i < storageSize && remaining > 0; i++)
        {
            if (storageSlots[i].HasItem() && storageSlots[i].itemId == itemId)
            {
                int removeAmount = Mathf.Min(remaining, storageSlots[i].amount);
                storageSlots[i].amount -= removeAmount;
                remaining -= removeAmount;

                if (storageSlots[i].amount <= 0)
                {
                    storageSlots[i].Clear();
                }
            }
        }

        return remaining == 0;
    }

    /// <summary>
    /// Conta quantos itens tem no storage
    /// </summary>
    private int CountItemInStorage(int itemId)
    {
        int count = 0;

        for (int i = 0; i < storageSize; i++)
        {
            if (storageSlots[i].HasItem() && storageSlots[i].itemId == itemId)
            {
                count += storageSlots[i].amount;
            }
        }

        return count;
    }

    /// <summary>
    /// Retorna slots do storage
    /// </summary>
    public InventorySlot[] GetStorageSlots() => storageSlots;

    #endregion

    #region GETTERS

    public int GetCupboardId() => cupboardId;
    public int GetOwnerId() => ownerId;
    public List<int> GetAuthorizedPlayers() => authorizedPlayers;
    public float GetBuildingBlockRange() => buildingBlockRange;
    public float GetUpkeepRange() => upkeepRange;
    public bool HasUpkeep() => hasUpkeepResources;
    public int GetProtectedPiecesCount() => protectedPieces.Count;

    #endregion

    #region DEBUG

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        // Range de building block (vermelho)
        Gizmos.color = new Color(1, 0, 0, 0.2f);
        Gizmos.DrawWireSphere(transform.position, buildingBlockRange);

        // Range de upkeep (verde)
        Gizmos.color = hasUpkeepResources ? new Color(0, 1, 0, 0.2f) : new Color(1, 0.5f, 0, 0.2f);
        Gizmos.DrawWireSphere(transform.position, upkeepRange);

        #if UNITY_EDITOR
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 3f,
            $"Tool Cupboard\nOwner: {ownerId}\nProtected: {protectedPieces.Count}\nUpkeep: {hasUpkeepResources}"
        );
        #endif
    }

    private void OnGUI()
    {
        if (!showDebug || !isInitialized) return;

        float width = 300f;
        float height = 200f;
        float x = 20f;
        float y = Screen.height - height - 20f;

        GUI.color = Color.black;
        GUI.Box(new Rect(x - 2, y - 2, width + 4, height + 4), "");

        GUI.color = Color.white;
        GUILayout.BeginArea(new Rect(x, y, width, height));
        GUILayout.Label("=== TOOL CUPBOARD ===", new GUIStyle { alignment = TextAnchor.MiddleCenter });
        GUILayout.Label($"ID: {cupboardId}");
        GUILayout.Label($"Owner: {ownerId}");
        GUILayout.Label($"Authorized: {authorizedPlayers.Count}");
        GUILayout.Label($"Protected Pieces: {protectedPieces.Count}");
        GUILayout.Label($"Has Upkeep: {hasUpkeepResources}");
        
        UpkeepCost cost = CalculateTotalUpkeepCost();
        GUILayout.Label($"Upkeep Cost:");
        GUILayout.Label($"  Wood: {cost.wood}");
        GUILayout.Label($"  Stone: {cost.stone}");
        GUILayout.Label($"  Metal: {cost.metal}");

        GUILayout.EndArea();
    }

    #endregion
}

/// <summary>
/// Estrutura de custo de upkeep
/// </summary>
[System.Serializable]
public struct UpkeepCost
{
    public int wood;
    public int stone;
    public int metal;
}