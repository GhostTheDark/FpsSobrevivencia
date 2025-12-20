using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sistema global de decay para todas as construções
/// Gerencia decay de todas as BuildingPieces no servidor
/// Apenas servidor executa este sistema
/// </summary>
public class BaseDecaySystem : MonoBehaviour
{
    public static BaseDecaySystem Instance { get; private set; }

    [Header("Decay Settings")]
    [SerializeField] private bool enableDecay = true;
    [SerializeField] private float decayCheckInterval = 60f; // Verifica decay a cada 60s
    [SerializeField] private float decayGracePeriod = 86400f; // 24 horas sem decay inicial

    [Header("Decay Rates (damage per hour)")]
    [SerializeField] private float twigDecayRate = 60f; // Twig decai rápido (1h)
    [SerializeField] private float woodDecayRate = 12f; // Wood decai em ~20h
    [SerializeField] private float stoneDecayRate = 6f; // Stone decai em ~80h
    [SerializeField] private float metalDecayRate = 3f; // Metal decai em ~330h
    [SerializeField] private float armoredDecayRate = 1.5f; // Armored decai em ~1300h

    [Header("Outside Protection Multiplier")]
    [SerializeField] private float outsideDecayMultiplier = 2f; // Sem TC decai 2x mais rápido

    [Header("Foundation Protection")]
    [SerializeField] private float foundationDecayDelay = 172800f; // 48h de proteção extra para fundações

    [Header("Debug")]
    [SerializeField] private bool showDebug = false;

    // Registry de todas as construções
    private Dictionary<int, BuildingPieceDecayData> buildingRegistry = new Dictionary<int, BuildingPieceDecayData>();
    private float nextDecayCheckTime = 0f;

    // Tool Cupboards
    private List<ToolCupboard> activeCupboards = new List<ToolCupboard>();

    private void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        nextDecayCheckTime = Time.time + decayCheckInterval;
    }

    private void Update()
    {
        // Apenas servidor executa decay
        if (NetworkManager.Instance == null || !NetworkManager.Instance.isServer)
            return;

        if (!enableDecay)
            return;

        // Verifica decay periodicamente
        if (Time.time >= nextDecayCheckTime)
        {
            ProcessDecayTick();
            nextDecayCheckTime = Time.time + decayCheckInterval;
        }
    }

    #region REGISTRATION

    /// <summary>
    /// Registra uma building piece no sistema de decay
    /// </summary>
    public void RegisterBuilding(BuildingPiece piece)
    {
        if (piece == null) return;

        int id = piece.GetBuildingId();
        
        if (buildingRegistry.ContainsKey(id))
        {
            if (showDebug)
                Debug.LogWarning($"[BaseDecaySystem] Building {id} already registered!");
            return;
        }

        BuildingPieceDecayData data = new BuildingPieceDecayData
        {
            piece = piece,
            buildTime = Time.time,
            lastDecayTime = Time.time,
            isProtected = false
        };

        buildingRegistry.Add(id, data);

        if (showDebug)
            Debug.Log($"[BaseDecaySystem] Registered building {id} ({piece.GetPieceType()})");
    }

    /// <summary>
    /// Remove building piece do sistema
    /// </summary>
    public void UnregisterBuilding(int buildingId)
    {
        if (buildingRegistry.Remove(buildingId))
        {
            if (showDebug)
                Debug.Log($"[BaseDecaySystem] Unregistered building {buildingId}");
        }
    }

    /// <summary>
    /// Registra Tool Cupboard
    /// </summary>
    public void RegisterCupboard(ToolCupboard cupboard)
    {
        if (!activeCupboards.Contains(cupboard))
        {
            activeCupboards.Add(cupboard);
            
            if (showDebug)
                Debug.Log($"[BaseDecaySystem] Registered cupboard {cupboard.GetCupboardId()}");
        }
    }

    /// <summary>
    /// Remove Tool Cupboard
    /// </summary>
    public void UnregisterCupboard(ToolCupboard cupboard)
    {
        if (activeCupboards.Remove(cupboard))
        {
            if (showDebug)
                Debug.Log($"[BaseDecaySystem] Unregistered cupboard {cupboard.GetCupboardId()}");
        }
    }

    #endregion

    #region DECAY PROCESSING

    /// <summary>
    /// Processa tick de decay para todas as construções
    /// </summary>
    private void ProcessDecayTick()
    {
        if (showDebug)
            Debug.Log($"[BaseDecaySystem] Processing decay for {buildingRegistry.Count} buildings");

        // Atualiza proteção de TC
        UpdateCupboardProtection();

        // Lista para remover (destruídas)
        List<int> toRemove = new List<int>();

        // Processa cada building
        foreach (var kvp in buildingRegistry)
        {
            BuildingPieceDecayData data = kvp.Value;

            if (data.piece == null || data.piece.IsDestroyed())
            {
                toRemove.Add(kvp.Key);
                continue;
            }

            // Calcula e aplica decay
            ApplyDecayToBuilding(data);
        }

        // Remove destruídas
        foreach (int id in toRemove)
        {
            buildingRegistry.Remove(id);
        }

        if (showDebug && toRemove.Count > 0)
            Debug.Log($"[BaseDecaySystem] Removed {toRemove.Count} destroyed buildings from registry");
    }

    /// <summary>
    /// Atualiza quais buildings estão protegidas por TC
    /// </summary>
    private void UpdateCupboardProtection()
    {
        // Reseta proteção
        foreach (var data in buildingRegistry.Values)
        {
            data.isProtected = false;
        }

        // Verifica cada cupboard
        foreach (var cupboard in activeCupboards)
        {
            if (cupboard == null)
                continue;

            // Apenas protege se tem upkeep
            if (!cupboard.HasUpkeep())
                continue;

            // Verifica cada building no range
            foreach (var data in buildingRegistry.Values)
            {
                if (data.piece == null)
                    continue;

                float distance = Vector3.Distance(
                    cupboard.transform.position,
                    data.piece.transform.position
                );

                if (distance <= cupboard.GetUpkeepRange())
                {
                    data.isProtected = true;
                }
            }
        }
    }

    /// <summary>
    /// Aplica decay a uma building piece
    /// </summary>
    private void ApplyDecayToBuilding(BuildingPieceDecayData data)
    {
        BuildingPiece piece = data.piece;
        
        // Verifica grace period
        float timeSinceBuilt = Time.time - data.buildTime;
        if (timeSinceBuilt < decayGracePeriod)
        {
            if (showDebug)
                Debug.Log($"[BaseDecaySystem] Building {piece.GetBuildingId()} in grace period");
            return;
        }

        // Fundações tem delay extra
        if (piece.GetPieceType() == BuildingPieceType.Foundation)
        {
            if (timeSinceBuilt < foundationDecayDelay)
                return;
        }

        // Calcula damage de decay
        float decayRate = GetDecayRateForGrade(piece.GetCurrentGrade());

        // Multiplica se não está protegido por TC
        if (!data.isProtected)
        {
            decayRate *= outsideDecayMultiplier;
        }

        // Converte de dano/hora para dano/intervalo
        float intervalHours = decayCheckInterval / 3600f;
        float decayDamage = decayRate * intervalHours;

        // Aplica dano
        piece.TakeDamage(decayDamage, -1, DamageType.Generic);

        data.lastDecayTime = Time.time;

        if (showDebug)
        {
            Debug.Log($"[BaseDecaySystem] Applied {decayDamage:F1} decay damage to building {piece.GetBuildingId()} " +
                     $"(Grade: {piece.GetCurrentGrade()}, Protected: {data.isProtected})");
        }
    }

    /// <summary>
    /// Retorna taxa de decay baseada no material
    /// </summary>
    private float GetDecayRateForGrade(BuildingGrade grade)
    {
        switch (grade)
        {
            case BuildingGrade.Twig: return twigDecayRate;
            case BuildingGrade.Wood: return woodDecayRate;
            case BuildingGrade.Stone: return stoneDecayRate;
            case BuildingGrade.Metal: return metalDecayRate;
            case BuildingGrade.Armored: return armoredDecayRate;
            default: return woodDecayRate;
        }
    }

    #endregion

    #region QUERIES

    /// <summary>
    /// Retorna número de buildings registradas
    /// </summary>
    public int GetRegisteredBuildingsCount()
    {
        return buildingRegistry.Count;
    }

    /// <summary>
    /// Retorna número de cupboards ativos
    /// </summary>
    public int GetActiveCupboardsCount()
    {
        return activeCupboards.Count;
    }

    /// <summary>
    /// Retorna número de buildings protegidas
    /// </summary>
    public int GetProtectedBuildingsCount()
    {
        int count = 0;
        foreach (var data in buildingRegistry.Values)
        {
            if (data.isProtected)
                count++;
        }
        return count;
    }

    /// <summary>
    /// Força decay check imediato (para testes)
    /// </summary>
    [ContextMenu("Force Decay Check")]
    public void ForceDecayCheck()
    {
        ProcessDecayTick();
    }

    #endregion

    #region DEBUG

    private void OnGUI()
    {
        if (!showDebug) return;
        if (NetworkManager.Instance == null || !NetworkManager.Instance.isServer) return;

        float width = 300f;
        float height = 150f;
        float x = Screen.width - width - 20f;
        float y = 20f;

        GUI.color = Color.black;
        GUI.Box(new Rect(x - 2, y - 2, width + 4, height + 4), "");

        GUI.color = Color.white;
        GUILayout.BeginArea(new Rect(x, y, width, height));
        GUILayout.Label("=== BASE DECAY SYSTEM ===", new GUIStyle { alignment = TextAnchor.MiddleCenter });
        GUILayout.Label($"Decay Enabled: {enableDecay}");
        GUILayout.Label($"Total Buildings: {buildingRegistry.Count}");
        GUILayout.Label($"Protected: {GetProtectedBuildingsCount()}");
        GUILayout.Label($"Unprotected: {buildingRegistry.Count - GetProtectedBuildingsCount()}");
        GUILayout.Label($"Active Cupboards: {activeCupboards.Count}");
        GUILayout.Label($"Next Check: {(nextDecayCheckTime - Time.time):F0}s");
        GUILayout.EndArea();
    }

    /// <summary>
    /// Lista todas as buildings no console
    /// </summary>
    [ContextMenu("List All Buildings")]
    public void ListAllBuildings()
    {
        Debug.Log($"=== REGISTERED BUILDINGS ({buildingRegistry.Count}) ===");
        
        foreach (var kvp in buildingRegistry)
        {
            BuildingPieceDecayData data = kvp.Value;
            if (data.piece != null)
            {
                Debug.Log($"[{kvp.Key}] {data.piece.GetPieceType()} - " +
                         $"Grade: {data.piece.GetCurrentGrade()}, " +
                         $"HP: {data.piece.GetCurrentHealth():F0}/{data.piece.GetMaxHealth():F0}, " +
                         $"Protected: {data.isProtected}");
            }
        }
    }

    #endregion
}

/// <summary>
/// Dados de decay de uma building piece
/// </summary>
public class BuildingPieceDecayData
{
    public BuildingPiece piece;
    public float buildTime;
    public float lastDecayTime;
    public bool isProtected;
}