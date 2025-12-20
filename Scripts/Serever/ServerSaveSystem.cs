// ============================================================================
// ServerSaveSystem.cs
// ============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Sistema de salvamento do servidor
/// Salva inventários, posições, bases, etc em JSON
/// APENAS SERVIDOR
/// </summary>
public class ServerSaveSystem : MonoBehaviour
{
    public static ServerSaveSystem Instance { get; private set; }

    [Header("Save Settings")]
    [SerializeField] private string saveFolder = "ServerSaves";
    [SerializeField] private bool autoSave = true;
    [SerializeField] private float autoSaveInterval = 300f; // 5 minutos
    [SerializeField] private int maxBackups = 5;

    [Header("What to Save")]
    [SerializeField] private bool savePlayers = true;
    [SerializeField] private bool saveBuildings = true;
    [SerializeField] private bool saveWorld = true;

    [Header("Debug")]
    [SerializeField] private bool showDebug = false;

    // Paths
    private string savePath;
    private string backupPath;

    // Timer
    private float nextAutoSaveTime = 0f;

    // Save data
    private WorldSaveData worldData;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Configura paths
        savePath = Path.Combine(Application.persistentDataPath, saveFolder);
        backupPath = Path.Combine(savePath, "Backups");

        // Cria diretórios
        if (!Directory.Exists(savePath))
            Directory.CreateDirectory(savePath);
        
        if (!Directory.Exists(backupPath))
            Directory.CreateDirectory(backupPath);
    }

    private void Start()
    {
        if (NetworkManager.Instance == null || !NetworkManager.Instance.isServer)
        {
            enabled = false;
            return;
        }

        nextAutoSaveTime = Time.time + autoSaveInterval;
        Debug.Log($"[ServerSaveSystem] Save path: {savePath}");
    }

    private void Update()
    {
        if (!NetworkManager.Instance.isServer) return;

        if (autoSave && Time.time >= nextAutoSaveTime)
        {
            SaveAll();
            nextAutoSaveTime = Time.time + autoSaveInterval;
        }
    }

    #region SAVE ALL

    /// <summary>
    /// Salva tudo
    /// </summary>
    public void SaveAll()
    {
        if (showDebug)
            Debug.Log("[ServerSaveSystem] Saving all data...");

        worldData = new WorldSaveData
        {
            saveTime = DateTime.Now.ToString(),
            worldSeed = WorldGenerator.Instance != null ? WorldGenerator.Instance.GetWorldSeed() : 0,
            players = new List<PlayerSaveData>(),
            buildings = new List<BuildingSaveData>()
        };

        if (savePlayers)
            SavePlayers();

        if (saveBuildings)
            SaveBuildings();

        // Escreve JSON
        string json = JsonUtility.ToJson(worldData, true);
        string filePath = Path.Combine(savePath, "world.json");
        
        File.WriteAllText(filePath, json);

        if (showDebug)
            Debug.Log($"[ServerSaveSystem] Save complete! Players: {worldData.players.Count}, Buildings: {worldData.buildings.Count}");
    }

    #endregion

    #region SAVE PLAYERS

    private void SavePlayers()
    {
        if (ServerPlayerManager.Instance == null) return;

        List<int> playerIds = ServerPlayerManager.Instance.GetActivePlayerIds();

        foreach (int playerId in playerIds)
        {
            GameObject playerObj = ServerPlayerManager.Instance.GetPlayerObject(playerId);
            if (playerObj == null) continue;

            PlayerSaveData playerData = new PlayerSaveData
            {
                clientId = playerId,
                position = playerObj.transform.position,
                rotation = playerObj.transform.rotation.eulerAngles,
                inventory = SavePlayerInventory(playerObj),
                stats = SavePlayerStats(playerObj)
            };

            worldData.players.Add(playerData);
        }
    }

    private InventorySaveData SavePlayerInventory(GameObject player)
    {
        InventorySystem inventory = player.GetComponent<InventorySystem>();
        if (inventory == null) return null;

        InventorySaveData data = new InventorySaveData
        {
            slots = new List<SlotSaveData>()
        };

        int slotCount = inventory.GetSlotCount();
        for (int i = 0; i < slotCount; i++)
        {
            var slot = inventory.GetItemAtSlot(i);
            if (slot != null && slot.HasItem())
            {
                data.slots.Add(new SlotSaveData
                {
                    slotIndex = i,
                    itemId = slot.itemId,
                    amount = slot.amount,
                    durability = slot.durability
                });
            }
        }

        return data;
    }

    private StatsSaveData SavePlayerStats(GameObject player)
    {
        PlayerHealth health = player.GetComponent<PlayerHealth>();
        PlayerHunger hunger = player.GetComponent<PlayerHunger>();
        PlayerThirst thirst = player.GetComponent<PlayerThirst>();

        return new StatsSaveData
        {
            health = health != null ? health.GetHealth() : 100f,
            hunger = hunger != null ? hunger.GetHunger() : 100f,
            thirst = thirst != null ? thirst.GetThirst() : 100f
        };
    }

    #endregion

    #region SAVE BUILDINGS

    private void SaveBuildings()
    {
        // Encontra todas as BuildingPieces
        BuildingPiece[] allBuildings = FindObjectsOfType<BuildingPiece>();

        foreach (BuildingPiece building in allBuildings)
        {
            BuildingSaveData buildingData = new BuildingSaveData
            {
                buildingId = building.GetBuildingId(),
                ownerId = building.GetOwnerId(),
                position = building.transform.position,
                rotation = building.transform.rotation.eulerAngles,
                pieceType = (int)building.GetPieceType(),
                grade = (int)building.GetCurrentGrade(),
                health = building.GetCurrentHealth()
            };

            worldData.buildings.Add(buildingData);
        }
    }

    #endregion

    #region LOAD ALL

    /// <summary>
    /// Carrega tudo
    /// </summary>
    public bool LoadAll()
    {
        string filePath = Path.Combine(savePath, "world.json");

        if (!File.Exists(filePath))
        {
            Debug.Log("[ServerSaveSystem] No save file found");
            return false;
        }

        try
        {
            string json = File.ReadAllText(filePath);
            worldData = JsonUtility.FromJson<WorldSaveData>(json);

            if (showDebug)
                Debug.Log($"[ServerSaveSystem] Loading save from {worldData.saveTime}");

            if (savePlayers)
                LoadPlayers();

            if (saveBuildings)
                LoadBuildings();

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[ServerSaveSystem] Load failed: {e.Message}");
            return false;
        }
    }

    #endregion

    #region LOAD PLAYERS

    private void LoadPlayers()
    {
        // TODO: Aplicar dados quando jogador conectar
        if (showDebug)
            Debug.Log($"[ServerSaveSystem] Loaded {worldData.players.Count} player saves");
    }

    #endregion

    #region LOAD BUILDINGS

    private void LoadBuildings()
    {
        // TODO: Instanciar buildings salvos
        if (showDebug)
            Debug.Log($"[ServerSaveSystem] Loaded {worldData.buildings.Count} buildings");
    }

    #endregion

    #region BACKUP

    /// <summary>
    /// Cria backup do save atual
    /// </summary>
    public void CreateBackup()
    {
        string sourceFile = Path.Combine(savePath, "world.json");
        if (!File.Exists(sourceFile)) return;

        string backupFile = Path.Combine(backupPath, $"world_backup_{DateTime.Now:yyyyMMdd_HHmmss}.json");
        File.Copy(sourceFile, backupFile);

        // Remove backups antigos
        CleanOldBackups();

        if (showDebug)
            Debug.Log($"[ServerSaveSystem] Backup created: {backupFile}");
    }

    private void CleanOldBackups()
    {
        string[] backups = Directory.GetFiles(backupPath, "*.json");
        
        if (backups.Length > maxBackups)
        {
            Array.Sort(backups);
            int toDelete = backups.Length - maxBackups;
            
            for (int i = 0; i < toDelete; i++)
            {
                File.Delete(backups[i]);
            }
        }
    }

    #endregion

    private void OnApplicationQuit()
    {
        if (NetworkManager.Instance != null && NetworkManager.Instance.isServer)
        {
            CreateBackup();
            SaveAll();
        }
    }
}

[Serializable]
public class WorldSaveData
{
    public string saveTime;
    public int worldSeed;
    public List<PlayerSaveData> players;
    public List<BuildingSaveData> buildings;
}

[Serializable]
public class PlayerSaveData
{
    public int clientId;
    public Vector3 position;
    public Vector3 rotation;
    public InventorySaveData inventory;
    public StatsSaveData stats;
}

[Serializable]
public class InventorySaveData
{
    public List<SlotSaveData> slots;
}

[Serializable]
public class SlotSaveData
{
    public int slotIndex;
    public int itemId;
    public int amount;
    public float durability;
}

[Serializable]
public class StatsSaveData
{
    public float health;
    public float hunger;
    public float thirst;
}

[Serializable]
public class BuildingSaveData
{
    public int buildingId;
    public int ownerId;
    public Vector3 position;
    public Vector3 rotation;
    public int pieceType;
    public int grade;
    public float health;
}

