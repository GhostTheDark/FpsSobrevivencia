// ============================================================================
// ServerLootSpawner.cs
// ============================================================================

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawna loot containers pelo mundo
/// Crates, barris, etc em locais procedurais
/// APENAS SERVIDOR
/// </summary>
public class ServerLootSpawner : MonoBehaviour
{
    public static ServerLootSpawner Instance { get; private set; }

    [Header("Loot Settings")]
    [SerializeField] private GameObject[] cratePrefabs;
    [SerializeField] private GameObject[] barrelPrefabs;
    [SerializeField] private int totalCrates = 100;
    [SerializeField] private int totalBarrels = 200;

    [Header("Spawn Rules")]
    [SerializeField] private float minDistanceBetweenLoot = 20f;
    [SerializeField] private float roadSpawnProbability = 0.3f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Respawn")]
    [SerializeField] private bool enableRespawn = true;
    [SerializeField] private float crateRespawnTime = 1800f; // 30 min
    [SerializeField] private float barrelRespawnTime = 600f; // 10 min

    [Header("Debug")]
    [SerializeField] private bool showDebug = false;

    private List<LootSpawnData> activeLoot = new List<LootSpawnData>();
    private List<Vector3> usedSpawnPositions = new List<Vector3>();

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

        // Aguarda geração do mundo
        Invoke(nameof(SpawnInitialLoot), 2f);
    }

    private void Update()
    {
        if (!NetworkManager.Instance.isServer || !enableRespawn) return;

        CheckRespawns();
    }

    #region SPAWN

    private void SpawnInitialLoot()
    {
        Debug.Log("[ServerLootSpawner] Spawning initial loot...");

        SpawnLootType(cratePrefabs, totalCrates, crateRespawnTime, LootType.Crate);
        SpawnLootType(barrelPrefabs, totalBarrels, barrelRespawnTime, LootType.Barrel);

        Debug.Log($"[ServerLootSpawner] Spawned {activeLoot.Count} loot containers");
    }

    private void SpawnLootType(GameObject[] prefabs, int count, float respawnTime, LootType type)
    {
        if (prefabs == null || prefabs.Length == 0) return;

        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPos = FindLootSpawnPosition();
            
            if (spawnPos == Vector3.zero)
            {
                if (showDebug)
                    Debug.LogWarning($"[ServerLootSpawner] Failed to find spawn for {type}");
                continue;
            }

            GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];
            GameObject lootObj = Instantiate(prefab, spawnPos, Quaternion.identity);

            LootSpawnData data = new LootSpawnData
            {
                lootObject = lootObj,
                spawnPosition = spawnPos,
                respawnTime = respawnTime,
                lootType = type,
                isActive = true
            };

            activeLoot.Add(data);
            usedSpawnPositions.Add(spawnPos);
        }
    }

    private Vector3 FindLootSpawnPosition()
    {
        if (WorldGenerator.Instance == null) return Vector3.zero;

        int worldSize = WorldGenerator.Instance.GetWorldSize();
        int maxAttempts = 50;

        for (int i = 0; i < maxAttempts; i++)
        {
            float x = Random.Range(-worldSize / 2f, worldSize / 2f);
            float z = Random.Range(-worldSize / 2f, worldSize / 2f);
            Vector3 testPos = new Vector3(x, 1000f, z);

            // Raycast para chão
            RaycastHit hit;
            if (Physics.Raycast(testPos, Vector3.down, out hit, 2000f, groundLayer))
            {
                Vector3 groundPos = hit.point;

                // Valida distância de outros loots
                bool tooClose = false;
                foreach (Vector3 existingPos in usedSpawnPositions)
                {
                    if (Vector3.Distance(groundPos, existingPos) < minDistanceBetweenLoot)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (tooClose) continue;

                // Valida bioma (não spawnar na água)
                BiomeType biome = WorldGenerator.Instance.GetBiomeAt(groundPos);
                if (biome == BiomeType.Ocean) continue;

                return groundPos;
            }
        }

        return Vector3.zero;
    }

    #endregion

    #region RESPAWN

    private void CheckRespawns()
    {
        for (int i = activeLoot.Count - 1; i >= 0; i--)
        {
            LootSpawnData data = activeLoot[i];

            if (!data.isActive)
            {
                float timeSinceDespawn = Time.time - data.despawnTime;
                
                if (timeSinceDespawn >= data.respawnTime)
                {
                    RespawnLoot(data);
                }
            }
            else if (data.lootObject == null)
            {
                // Loot foi destruído/lootado
                OnLootLooted(data);
            }
        }
    }

    private void OnLootLooted(LootSpawnData data)
    {
        data.isActive = false;
        data.despawnTime = Time.time;

        if (showDebug)
            Debug.Log($"[ServerLootSpawner] {data.lootType} looted, will respawn in {data.respawnTime}s");
    }

    private void RespawnLoot(LootSpawnData data)
    {
        GameObject[] prefabs = data.lootType == LootType.Crate ? cratePrefabs : barrelPrefabs;
        
        if (prefabs == null || prefabs.Length == 0) return;

        GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];
        data.lootObject = Instantiate(prefab, data.spawnPosition, Quaternion.identity);
        data.isActive = true;

        if (showDebug)
            Debug.Log($"[ServerLootSpawner] {data.lootType} respawned at {data.spawnPosition}");
    }

    #endregion

    #region DEBUG

    [ContextMenu("Force Respawn All")]
    private void ForceRespawnAll()
    {
        foreach (var data in activeLoot)
        {
            if (!data.isActive)
            {
                RespawnLoot(data);
            }
        }
    }

    #endregion
}

public class LootSpawnData
{
    public GameObject lootObject;
    public Vector3 spawnPosition;
    public float respawnTime;
    public float despawnTime;
    public LootType lootType;
    public bool isActive;
}

public enum LootType
{
    Crate,
    Barrel,
    MilitaryCrate,
    EliteCrate
}

