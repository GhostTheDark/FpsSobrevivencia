using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Monumento / Ponto de Interesse (POI)
/// Locais especiais com loot valioso e possível radiação
/// Exemplos: Power Plant, Military Tunnels, Launch Site, etc
/// </summary>
public class Monument : MonoBehaviour
{
    [Header("Monument Info")]
    [SerializeField] private string monumentName = "Monument";
    [SerializeField] private MonumentType monumentType = MonumentType.Tier1;
    [TextArea(2, 4)]
    [SerializeField] private string description = "A mysterious location";

    [Header("Radiation")]
    [SerializeField] private bool hasRadiation = false;
    [SerializeField] private float radiationLevel = 10f; // Rads por segundo
    [SerializeField] private float radiationRadius = 50f;

    [Header("Loot")]
    [SerializeField] private LootTier lootTier = LootTier.Low;
    [SerializeField] private int minLootCrates = 3;
    [SerializeField] private int maxLootCrates = 8;
    [SerializeField] private GameObject[] lootCratePrefabs;

    [Header("Puzzles")]
    [SerializeField] private bool hasPuzzle = false;
    [SerializeField] private PuzzleType puzzleType = PuzzleType.Keycard;
    [SerializeField] private int requiredKeycardLevel = 1;

    [Header("NPCs")]
    [SerializeField] private bool hasScientists = false;
    [SerializeField] private int minScientists = 2;
    [SerializeField] private int maxScientists = 6;
    [SerializeField] private GameObject scientistPrefab;

    [Header("Respawn")]
    [SerializeField] private bool respawnsLoot = true;
    [SerializeField] private float lootRespawnTime = 1800f; // 30 minutos

    [Header("Map Marker")]
    [SerializeField] private bool showOnMap = true;
    [SerializeField] private Sprite mapIcon;
    [SerializeField] private Color mapIconColor = Color.white;

    [Header("Audio")]
    [SerializeField] private AudioClip ambientSound;
    [SerializeField] private float ambientSoundRadius = 100f;

    [Header("Debug")]
    [SerializeField] private bool showDebug = false;
    [SerializeField] private bool showGizmos = true;

    // Estado
    private bool isInitialized = false;
    private List<GameObject> spawnedLootCrates = new List<GameObject>();
    private List<GameObject> spawnedScientists = new List<GameObject>();
    private List<Transform> lootSpawnPoints = new List<Transform>();
    private List<Transform> npcSpawnPoints = new List<Transform>();

    // Radiação
    private RadiationZone radiationZone;

    // Timers
    private float nextLootRespawnTime = 0f;

    // Jogadores na área
    private List<int> playersInArea = new List<int>();

    private void Start()
    {
        Initialize();
    }

    /// <summary>
    /// Inicializa o monumento
    /// </summary>
    private void Initialize()
    {
        if (isInitialized) return;

        // Encontra spawn points
        FindSpawnPoints();

        // Configura radiação
        if (hasRadiation)
        {
            SetupRadiation();
        }

        // Spawna loot inicial
        SpawnLoot();

        // Spawna NPCs
        if (hasScientists)
        {
            SpawnScientists();
        }

        // Configura áudio ambiente
        if (ambientSound != null)
        {
            SetupAmbientAudio();
        }

        isInitialized = true;

        if (showDebug)
            Debug.Log($"[Monument] {monumentName} initialized");
    }

    private void Update()
    {
        if (!isInitialized) return;

        // Respawn de loot
        if (respawnsLoot && Time.time >= nextLootRespawnTime)
        {
            RespawnLoot();
        }

        // Atualiza jogadores na área
        UpdatePlayersInArea();
    }

    #region SPAWN POINTS

    /// <summary>
    /// Encontra todos os spawn points
    /// </summary>
    private void FindSpawnPoints()
    {
        // Busca por tags ou nomes específicos
        Transform[] allChildren = GetComponentsInChildren<Transform>();

        foreach (Transform child in allChildren)
        {
            if (child.name.Contains("LootSpawn") || child.CompareTag("LootSpawn"))
            {
                lootSpawnPoints.Add(child);
            }
            else if (child.name.Contains("NPCSpawn") || child.CompareTag("NPCSpawn"))
            {
                npcSpawnPoints.Add(child);
            }
        }

        if (showDebug)
        {
            Debug.Log($"[Monument] Found {lootSpawnPoints.Count} loot spawn points");
            Debug.Log($"[Monument] Found {npcSpawnPoints.Count} NPC spawn points");
        }
    }

    #endregion

    #region LOOT

    /// <summary>
    /// Spawna loot no monumento
    /// </summary>
    private void SpawnLoot()
    {
        if (lootCratePrefabs == null || lootCratePrefabs.Length == 0)
        {
            if (showDebug)
                Debug.LogWarning($"[Monument] {monumentName} has no loot crate prefabs!");
            return;
        }

        if (lootSpawnPoints.Count == 0)
        {
            if (showDebug)
                Debug.LogWarning($"[Monument] {monumentName} has no loot spawn points!");
            return;
        }

        // Limpa loot antigo
        ClearLoot();

        // Calcula quantidade de crates
        int crateCount = Random.Range(minLootCrates, maxLootCrates + 1);
        crateCount = Mathf.Min(crateCount, lootSpawnPoints.Count);

        // Shufflea spawn points
        List<Transform> availablePoints = new List<Transform>(lootSpawnPoints);
        ShuffleList(availablePoints);

        // Spawna crates
        for (int i = 0; i < crateCount; i++)
        {
            GameObject cratePrefab = lootCratePrefabs[Random.Range(0, lootCratePrefabs.Length)];
            Transform spawnPoint = availablePoints[i];

            GameObject crate = Instantiate(cratePrefab, spawnPoint.position, spawnPoint.rotation);
            crate.transform.SetParent(transform);
            spawnedLootCrates.Add(crate);
        }

        if (showDebug)
            Debug.Log($"[Monument] Spawned {crateCount} loot crates");
    }

    /// <summary>
    /// Limpa loot atual
    /// </summary>
    private void ClearLoot()
    {
        foreach (GameObject crate in spawnedLootCrates)
        {
            if (crate != null)
                Destroy(crate);
        }
        spawnedLootCrates.Clear();
    }

    /// <summary>
    /// Respawna loot
    /// </summary>
    private void RespawnLoot()
    {
        SpawnLoot();
        nextLootRespawnTime = Time.time + lootRespawnTime;

        if (showDebug)
            Debug.Log($"[Monument] Loot respawned. Next respawn in {lootRespawnTime}s");
    }

    #endregion

    #region NPCs

    /// <summary>
    /// Spawna cientistas
    /// </summary>
    private void SpawnScientists()
    {
        if (scientistPrefab == null)
        {
            if (showDebug)
                Debug.LogWarning($"[Monument] {monumentName} has no scientist prefab!");
            return;
        }

        if (npcSpawnPoints.Count == 0)
        {
            if (showDebug)
                Debug.LogWarning($"[Monument] {monumentName} has no NPC spawn points!");
            return;
        }

        // Calcula quantidade
        int scientistCount = Random.Range(minScientists, maxScientists + 1);
        scientistCount = Mathf.Min(scientistCount, npcSpawnPoints.Count);

        // Shufflea spawn points
        List<Transform> availablePoints = new List<Transform>(npcSpawnPoints);
        ShuffleList(availablePoints);

        // Spawna cientistas
        for (int i = 0; i < scientistCount; i++)
        {
            Transform spawnPoint = availablePoints[i];
            GameObject scientist = Instantiate(scientistPrefab, spawnPoint.position, spawnPoint.rotation);
            scientist.transform.SetParent(transform);
            spawnedScientists.Add(scientist);
        }

        if (showDebug)
            Debug.Log($"[Monument] Spawned {scientistCount} scientists");
    }

    #endregion

    #region RADIATION

    /// <summary>
    /// Configura zona de radiação
    /// </summary>
    private void SetupRadiation()
    {
        GameObject radZoneObj = new GameObject("RadiationZone");
        radZoneObj.transform.SetParent(transform);
        radZoneObj.transform.localPosition = Vector3.zero;

        radiationZone = radZoneObj.AddComponent<RadiationZone>();
        radiationZone.Initialize(radiationLevel, radiationRadius);

        if (showDebug)
            Debug.Log($"[Monument] Radiation zone configured: {radiationLevel} rads/s, {radiationRadius}m radius");
    }

    #endregion

    #region PLAYERS

    /// <summary>
    /// Atualiza lista de jogadores na área
    /// </summary>
    private void UpdatePlayersInArea()
    {
        // TODO: Verificar jogadores próximos
        // Usado para ativar/desativar NPCs, sons, etc
    }

    /// <summary>
    /// Callback quando jogador entra no monumento
    /// </summary>
    private void OnPlayerEnter(int playerId)
    {
        if (!playersInArea.Contains(playerId))
        {
            playersInArea.Add(playerId);

            if (showDebug)
                Debug.Log($"[Monument] Player {playerId} entered {monumentName}");

            // TODO: Notificar jogador sobre o monumento
        }
    }

    /// <summary>
    /// Callback quando jogador sai do monumento
    /// </summary>
    private void OnPlayerExit(int playerId)
    {
        if (playersInArea.Remove(playerId))
        {
            if (showDebug)
                Debug.Log($"[Monument] Player {playerId} left {monumentName}");
        }
    }

    #endregion

    #region AUDIO

    /// <summary>
    /// Configura áudio ambiente
    /// </summary>
    private void SetupAmbientAudio()
    {
        AudioSource audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = ambientSound;
        audioSource.loop = true;
        audioSource.spatialBlend = 1f; // 3D sound
        audioSource.maxDistance = ambientSoundRadius;
        audioSource.Play();
    }

    #endregion

    #region HELPERS

    /// <summary>
    /// Embaralha lista
    /// </summary>
    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }

    #endregion

    #region GETTERS

    public string GetMonumentName() => monumentName;
    public MonumentType GetMonumentType() => monumentType;
    public bool HasRadiation() => hasRadiation;
    public float GetRadiationLevel() => radiationLevel;
    public bool HasPuzzle() => hasPuzzle;
    public int GetRequiredKeycardLevel() => requiredKeycardLevel;

    #endregion

    #region GIZMOS

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        // Desenha bounds do monumento
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(50, 10, 50));

        // Desenha zona de radiação
        if (hasRadiation)
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawWireSphere(transform.position, radiationRadius);
        }

        #if UNITY_EDITOR
        // Label
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 15f,
            monumentName,
            new GUIStyle
            {
                alignment = TextAnchor.MiddleCenter,
                normal = new GUIStyleState { textColor = Color.yellow },
                fontSize = 14
            }
        );
        #endif
    }

    private void OnDrawGizmosSelected()
    {
        // Desenha spawn points
        Gizmos.color = Color.cyan;
        foreach (Transform spawnPoint in lootSpawnPoints)
        {
            if (spawnPoint != null)
                Gizmos.DrawWireSphere(spawnPoint.position, 0.5f);
        }

        Gizmos.color = Color.red;
        foreach (Transform spawnPoint in npcSpawnPoints)
        {
            if (spawnPoint != null)
                Gizmos.DrawWireSphere(spawnPoint.position, 0.5f);
        }
    }

    #endregion
}

/// <summary>
/// Tipos de monumento
/// </summary>
public enum MonumentType
{
    Tier1,          // Pequeno, pouca radiação
    Tier2,          // Médio, radiação moderada
    Tier3,          // Grande, alta radiação
    SafeZone,       // Zona segura (Outpost, Bandit)
    Military,       // Base militar
    Special         // Especial (Launch Site, etc)
}

/// <summary>
/// Níveis de loot
/// </summary>
public enum LootTier
{
    Low,        // Loot básico
    Medium,     // Loot médio
    High,       // Loot alto
    Elite       // Loot elite
}

/// <summary>
/// Tipos de puzzle
/// </summary>
public enum PuzzleType
{
    None,
    Keycard,        // Requer keycard
    Fuse,           // Requer fusível
    Switch,         // Puzzle de switches
    Timer,          // Puzzle com timer
    Complex         // Puzzle complexo
}