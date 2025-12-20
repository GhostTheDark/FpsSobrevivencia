// ============================================================================
// ServerAIManager.cs  
// ============================================================================

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gerencia NPCs e animais no servidor
/// Scientists, bears, wolves, etc
/// APENAS SERVIDOR
/// </summary>
public class ServerAIManager : MonoBehaviour
{
    public static ServerAIManager Instance { get; private set; }

    [Header("Animals")]
    [SerializeField] private GameObject bearPrefab;
    [SerializeField] private GameObject wolfPrefab;
    [SerializeField] private GameObject deerPrefab;
    [SerializeField] private int maxBears = 10;
    [SerializeField] private int maxWolves = 20;
    [SerializeField] private int maxDeer = 30;

    [Header("NPCs")]
    [SerializeField] private GameObject scientistPrefab;
    [SerializeField] private int maxScientists = 50;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnCheckInterval = 30f;
    [SerializeField] private float minPlayerDistance = 100f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Performance")]
    [SerializeField] private float aiUpdateRate = 0.1f;
    [SerializeField] private int maxAIUpdatesPerFrame = 20;

    [Header("Debug")]
    [SerializeField] private bool showDebug = false;

    private List<AIEntityData> activeEntities = new List<AIEntityData>();
    private float nextSpawnCheckTime = 0f;
    private float nextAIUpdateTime = 0f;
    private int currentUpdateIndex = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (NetworkManager.Instance == null || !NetworkManager.Instance.isServer)
        {
            enabled = false;
            return;
        }

        Invoke(nameof(SpawnInitialAI), 3f);
    }

    private void Update()
    {
        if (!NetworkManager.Instance.isServer) return;

        if (Time.time >= nextSpawnCheckTime)
        {
            CheckAndSpawnAI();
            nextSpawnCheckTime = Time.time + spawnCheckInterval;
        }

        if (Time.time >= nextAIUpdateTime)
        {
            UpdateAIEntities();
            nextAIUpdateTime = Time.time + aiUpdateRate;
        }
    }

    #region SPAWN

    private void SpawnInitialAI()
    {
        Debug.Log("[ServerAIManager] Spawning initial AI...");

        SpawnAnimalType(bearPrefab, maxBears, AIType.Bear);
        SpawnAnimalType(wolfPrefab, maxWolves, AIType.Wolf);
        SpawnAnimalType(deerPrefab, maxDeer, AIType.Deer);

        Debug.Log($"[ServerAIManager] Spawned {activeEntities.Count} AI entities");
    }

    private void SpawnAnimalType(GameObject prefab, int count, AIType type)
    {
        if (prefab == null) return;

        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPos = FindAISpawnPosition();
            
            if (spawnPos == Vector3.zero) continue;

            SpawnAI(prefab, spawnPos, type);
        }
    }

    private GameObject SpawnAI(GameObject prefab, Vector3 position, AIType type)
    {
        GameObject aiObj = Instantiate(prefab, position, Quaternion.identity);

        AIEntityData data = new AIEntityData
        {
            entityObject = aiObj,
            aiType = type,
            spawnPosition = position,
            spawnTime = Time.time,
            isActive = true
        };

        activeEntities.Add(data);

        if (showDebug)
            Debug.Log($"[ServerAIManager] Spawned {type} at {position}");

        return aiObj;
    }

    private Vector3 FindAISpawnPosition()
    {
        if (WorldGenerator.Instance == null) return Vector3.zero;

        int worldSize = WorldGenerator.Instance.GetWorldSize();
        int maxAttempts = 30;

        for (int i = 0; i < maxAttempts; i++)
        {
            float x = Random.Range(-worldSize / 2f, worldSize / 2f);
            float z = Random.Range(-worldSize / 2f, worldSize / 2f);
            Vector3 testPos = new Vector3(x, 1000f, z);

            RaycastHit hit;
            if (Physics.Raycast(testPos, Vector3.down, out hit, 2000f, groundLayer))
            {
                Vector3 groundPos = hit.point;

                BiomeType biome = WorldGenerator.Instance.GetBiomeAt(groundPos);
                if (biome == BiomeType.Ocean) continue;

                if (!IsNearPlayers(groundPos, minPlayerDistance))
                    return groundPos;
            }
        }

        return Vector3.zero;
    }

    private bool IsNearPlayers(Vector3 position, float distance)
    {
        if (ServerPlayerManager.Instance == null) return false;

        List<int> playerIds = ServerPlayerManager.Instance.GetActivePlayerIds();
        
        foreach (int playerId in playerIds)
        {
            GameObject playerObj = ServerPlayerManager.Instance.GetPlayerObject(playerId);
            if (playerObj != null)
            {
                if (Vector3.Distance(position, playerObj.transform.position) < distance)
                    return true;
            }
        }

        return false;
    }

    #endregion

    #region UPDATE

    private void CheckAndSpawnAI()
    {
        // Remove mortos
        activeEntities.RemoveAll(e => e.entityObject == null);

        // Conta por tipo
        int bearCount = 0, wolfCount = 0, deerCount = 0, scientistCount = 0;
        
        foreach (var entity in activeEntities)
        {
            switch (entity.aiType)
            {
                case AIType.Bear: bearCount++; break;
                case AIType.Wolf: wolfCount++; break;
                case AIType.Deer: deerCount++; break;
                case AIType.Scientist: scientistCount++; break;
            }
        }

        // Spawna se necessário
        if (bearCount < maxBears && bearPrefab != null)
            SpawnAnimalType(bearPrefab, maxBears - bearCount, AIType.Bear);
        
        if (wolfCount < maxWolves && wolfPrefab != null)
            SpawnAnimalType(wolfPrefab, maxWolves - wolfCount, AIType.Wolf);
        
        if (deerCount < maxDeer && deerPrefab != null)
            SpawnAnimalType(deerPrefab, maxDeer - deerCount, AIType.Deer);
    }

    private void UpdateAIEntities()
    {
        int updated = 0;

        while (updated < maxAIUpdatesPerFrame && activeEntities.Count > 0)
        {
            if (currentUpdateIndex >= activeEntities.Count)
                currentUpdateIndex = 0;

            AIEntityData entity = activeEntities[currentUpdateIndex];
            
            if (entity.entityObject != null)
            {
                // TODO: Lógica de AI (patrulha, ataque, fuga)
            }

            currentUpdateIndex++;
            updated++;
        }
    }

    #endregion

    #region PUBLIC

    public int GetActiveAICount() => activeEntities.Count;

    #endregion
}

public class AIEntityData
{
    public GameObject entityObject;
    public AIType aiType;
    public Vector3 spawnPosition;
    public float spawnTime;
    public bool isActive;
}

public enum AIType
{
    Bear,
    Wolf,
    Deer,
    Boar,
    Scientist,
    Bandit
}