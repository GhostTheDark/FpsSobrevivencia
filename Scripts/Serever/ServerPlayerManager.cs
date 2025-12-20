using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gerencia todos os jogadores no servidor
/// Spawna, desconecta, rastreia posições e estados
/// APENAS SERVIDOR
/// </summary>
public class ServerPlayerManager : MonoBehaviour
{
    public static ServerPlayerManager Instance { get; private set; }

    [Header("Player Settings")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private float respawnTime = 5f;
    [SerializeField] private int maxPlayersPerServer = 100;

    [Header("Spawn Settings")]
    [SerializeField] private bool useBeachSpawns = true;
    [SerializeField] private bool useSleepingBags = true;
    [SerializeField] private float spawnProtectionTime = 10f;

    [Header("Anti-Cheat")]
    [SerializeField] private float maxPlayerSpeed = 20f;
    [SerializeField] private float maxTeleportDistance = 100f;
    [SerializeField] private float positionCheckInterval = 1f;

    [Header("Debug")]
    [SerializeField] private bool showDebug = false;

    // Players ativos
    private Dictionary<int, ServerPlayerData> activePlayers = new Dictionary<int, ServerPlayerData>();
    
    // Players aguardando respawn
    private Dictionary<int, float> respawnQueue = new Dictionary<int, float>();

    // Player prefab instantiation
    private Transform playersContainer;

    // Anti-cheat
    private float nextPositionCheckTime = 0f;

    private void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Cria container para players
        GameObject container = new GameObject("Players");
        playersContainer = container.transform;
    }

    private void Start()
    {
        // Apenas servidor
        if (NetworkManager.Instance == null || !NetworkManager.Instance.isServer)
        {
            enabled = false;
            return;
        }

        // Registra callbacks
        NetworkManager.Instance.OnClientConnected += OnPlayerConnected;
        NetworkManager.Instance.OnClientDisconnected += OnPlayerDisconnected;

        Debug.Log("[ServerPlayerManager] Initialized");
    }

    private void Update()
    {
        if (!NetworkManager.Instance.isServer) return;

        // Processa fila de respawn
        ProcessRespawnQueue();

        // Anti-cheat: valida posições
        if (Time.time >= nextPositionCheckTime)
        {
            ValidatePlayerPositions();
            nextPositionCheckTime = Time.time + positionCheckInterval;
        }
    }

    #region PLAYER CONNECTION

    /// <summary>
    /// Callback quando jogador conecta
    /// </summary>
    private void OnPlayerConnected(int clientId)
    {
        if (activePlayers.Count >= maxPlayersPerServer)
        {
            Debug.LogWarning($"[ServerPlayerManager] Server full! Cannot spawn player {clientId}");
            NetworkManager.Instance.DisconnectClient(clientId);
            return;
        }

        // Verifica se já existe (reconexão)
        if (activePlayers.ContainsKey(clientId))
        {
            Debug.Log($"[ServerPlayerManager] Player {clientId} reconnected");
            ReconnectPlayer(clientId);
            return;
        }

        // Spawna novo jogador
        SpawnPlayer(clientId);
    }

    /// <summary>
    /// Callback quando jogador desconecta
    /// </summary>
    private void OnPlayerDisconnected(int clientId)
    {
        if (!activePlayers.ContainsKey(clientId))
            return;

        ServerPlayerData playerData = activePlayers[clientId];

        // Salva dados antes de remover
        SavePlayerData(clientId);

        // Destrói GameObject
        if (playerData.playerObject != null)
        {
            Destroy(playerData.playerObject);
        }

        // Remove da lista
        activePlayers.Remove(clientId);

        if (showDebug)
            Debug.Log($"[ServerPlayerManager] Player {clientId} disconnected. Active players: {activePlayers.Count}");

        // Notifica outros jogadores
        BroadcastPlayerDisconnected(clientId);
    }

    #endregion

    #region SPAWN

    /// <summary>
    /// Spawna jogador no mundo
    /// </summary>
    private void SpawnPlayer(int clientId)
    {
        if (playerPrefab == null)
        {
            Debug.LogError("[ServerPlayerManager] Player prefab is null!");
            return;
        }

        // Calcula spawn position
        Vector3 spawnPosition = CalculateSpawnPosition(clientId);

        // Instancia player
        GameObject playerObj = Instantiate(playerPrefab, spawnPosition, Quaternion.identity, playersContainer);
        playerObj.name = $"Player_{clientId}";

        // Configura NetworkPlayer
        NetworkPlayer networkPlayer = playerObj.GetComponent<NetworkPlayer>();
        if (networkPlayer != null)
        {
            networkPlayer.Initialize(clientId, false);
        }

        // Cria dados do servidor
        ServerPlayerData playerData = new ServerPlayerData
        {
            clientId = clientId,
            playerObject = playerObj,
            spawnTime = Time.time,
            lastPosition = spawnPosition,
            isAlive = true,
            hasSpawnProtection = true
        };

        activePlayers.Add(clientId, playerData);

        if (showDebug)
            Debug.Log($"[ServerPlayerManager] Player {clientId} spawned at {spawnPosition}");

        // Envia spawn para o cliente
        SendSpawnMessage(clientId, spawnPosition);

        // Notifica outros jogadores
        BroadcastPlayerSpawned(clientId, spawnPosition);

        // Agenda fim da proteção de spawn
        Invoke(nameof(RemoveSpawnProtection) + clientId, spawnProtectionTime);
    }

    /// <summary>
    /// Calcula posição de spawn
    /// </summary>
    private Vector3 CalculateSpawnPosition(int clientId)
    {
        // 1. Tenta usar sleeping bag do jogador
        if (useSleepingBags)
        {
            Vector3 bagPosition = GetSleepingBagPosition(clientId);
            if (bagPosition != Vector3.zero)
            {
                if (showDebug)
                    Debug.Log($"[ServerPlayerManager] Player {clientId} spawning at sleeping bag");
                return bagPosition;
            }
        }

        // 2. Spawn em praia aleatória
        if (useBeachSpawns && WorldGenerator.Instance != null)
        {
            return WorldGenerator.Instance.GetRandomBeachSpawn();
        }

        // 3. Fallback: spawn no centro em altura elevada
        return new Vector3(0, 100f, 0);
    }

    /// <summary>
    /// Retorna posição de sleeping bag do jogador
    /// </summary>
    private Vector3 GetSleepingBagPosition(int clientId)
    {
        // TODO: Implementar sistema de sleeping bags
        // Por enquanto retorna zero (sem bag)
        return Vector3.zero;
    }

    /// <summary>
    /// Envia mensagem de spawn para o cliente
    /// </summary>
    private void SendSpawnMessage(int clientId, Vector3 position)
    {
        NetworkMessage message = new NetworkMessage
        {
            type = MessageType.PlayerSpawn,
            clientId = clientId
        };
        message.SetVector3(position);

        NetworkManager.Instance.SendToClient(clientId, message);
    }

    /// <summary>
    /// Notifica outros jogadores sobre novo spawn
    /// </summary>
    private void BroadcastPlayerSpawned(int clientId, Vector3 position)
    {
        NetworkMessage message = new NetworkMessage
        {
            type = MessageType.PlayerSpawn,
            clientId = clientId
        };
        message.SetVector3(position);

        NetworkManager.Instance.SendToAllExcept(clientId, message);
    }

    #endregion

    #region RESPAWN

    /// <summary>
    /// Adiciona jogador à fila de respawn
    /// </summary>
    public void QueueRespawn(int clientId)
    {
        if (!activePlayers.ContainsKey(clientId))
            return;

        ServerPlayerData playerData = activePlayers[clientId];
        playerData.isAlive = false;

        // Adiciona à fila com tempo de respawn
        float respawnAt = Time.time + respawnTime;
        respawnQueue[clientId] = respawnAt;

        if (showDebug)
            Debug.Log($"[ServerPlayerManager] Player {clientId} queued for respawn in {respawnTime}s");

        // Notifica cliente
        SendRespawnTimerMessage(clientId, respawnTime);
    }

    /// <summary>
    /// Processa fila de respawn
    /// </summary>
    private void ProcessRespawnQueue()
    {
        if (respawnQueue.Count == 0) return;

        List<int> toRespawn = new List<int>();

        // Verifica quem está pronto para respawnar
        foreach (var kvp in respawnQueue)
        {
            if (Time.time >= kvp.Value)
            {
                toRespawn.Add(kvp.Key);
            }
        }

        // Respawna jogadores
        foreach (int clientId in toRespawn)
        {
            RespawnPlayer(clientId);
            respawnQueue.Remove(clientId);
        }
    }

    /// <summary>
    /// Respawna jogador
    /// </summary>
    private void RespawnPlayer(int clientId)
    {
        if (!activePlayers.ContainsKey(clientId))
            return;

        ServerPlayerData playerData = activePlayers[clientId];

        // Calcula nova posição
        Vector3 spawnPosition = CalculateSpawnPosition(clientId);

        // Teleporta player
        if (playerData.playerObject != null)
        {
            playerData.playerObject.transform.position = spawnPosition;

            // Reseta stats
            NetworkPlayer networkPlayer = playerData.playerObject.GetComponent<NetworkPlayer>();
            if (networkPlayer != null)
            {
                networkPlayer.Respawn(spawnPosition);
            }
        }

        // Atualiza dados
        playerData.isAlive = true;
        playerData.hasSpawnProtection = true;
        playerData.spawnTime = Time.time;

        if (showDebug)
            Debug.Log($"[ServerPlayerManager] Player {clientId} respawned at {spawnPosition}");

        // Envia para cliente
        SendSpawnMessage(clientId, spawnPosition);

        // Agenda proteção
        Invoke(nameof(RemoveSpawnProtection) + clientId, spawnProtectionTime);
    }

    /// <summary>
    /// Envia timer de respawn para cliente
    /// </summary>
    private void SendRespawnTimerMessage(int clientId, float timeRemaining)
    {
        NetworkMessage message = new NetworkMessage
        {
            type = MessageType.PlayerDeath,
            clientId = clientId
        };
        message.SetFloat(timeRemaining);

        NetworkManager.Instance.SendToClient(clientId, message);
    }

    #endregion

    #region SPAWN PROTECTION

    /// <summary>
    /// Remove proteção de spawn
    /// </summary>
    private void RemoveSpawnProtection(int clientId)
    {
        if (!activePlayers.ContainsKey(clientId))
            return;

        ServerPlayerData playerData = activePlayers[clientId];
        playerData.hasSpawnProtection = false;

        if (showDebug)
            Debug.Log($"[ServerPlayerManager] Spawn protection removed for player {clientId}");
    }

    /// <summary>
    /// Verifica se jogador tem proteção de spawn
    /// </summary>
    public bool HasSpawnProtection(int clientId)
    {
        if (!activePlayers.ContainsKey(clientId))
            return false;

        return activePlayers[clientId].hasSpawnProtection;
    }

    #endregion

    #region ANTI-CHEAT

    /// <summary>
    /// Valida posições de todos os jogadores
    /// </summary>
    private void ValidatePlayerPositions()
    {
        foreach (var kvp in activePlayers)
        {
            ServerPlayerData playerData = kvp.Value;
            
            if (playerData.playerObject == null)
                continue;

            Vector3 currentPosition = playerData.playerObject.transform.position;

            // Valida velocidade
            float distance = Vector3.Distance(currentPosition, playerData.lastPosition);
            float speed = distance / positionCheckInterval;

            if (speed > maxPlayerSpeed)
            {
                Debug.LogWarning($"[ServerPlayerManager] Player {kvp.Key} moving too fast! Speed: {speed:F1} m/s");
                
                // Teleporta de volta
                playerData.playerObject.transform.position = playerData.lastPosition;
                
                // TODO: Sistema de warnings/kicks
            }

            // Atualiza última posição
            playerData.lastPosition = currentPosition;
        }
    }

    /// <summary>
    /// Valida teleport de jogador
    /// </summary>
    public bool ValidateTeleport(int clientId, Vector3 newPosition)
    {
        if (!activePlayers.ContainsKey(clientId))
            return false;

        ServerPlayerData playerData = activePlayers[clientId];
        
        if (playerData.playerObject == null)
            return false;

        float distance = Vector3.Distance(playerData.playerObject.transform.position, newPosition);

        if (distance > maxTeleportDistance)
        {
            Debug.LogWarning($"[ServerPlayerManager] Player {clientId} attempted suspicious teleport! Distance: {distance:F1}m");
            return false;
        }

        return true;
    }

    #endregion

    #region RECONNECTION

    /// <summary>
    /// Reconecta jogador existente
    /// </summary>
    private void ReconnectPlayer(int clientId)
    {
        ServerPlayerData playerData = activePlayers[clientId];

        if (playerData.playerObject == null)
        {
            // Player foi destruído, respawna novo
            SpawnPlayer(clientId);
            return;
        }

        // Envia estado atual
        Vector3 currentPosition = playerData.playerObject.transform.position;
        SendSpawnMessage(clientId, currentPosition);

        if (showDebug)
            Debug.Log($"[ServerPlayerManager] Player {clientId} reconnected at {currentPosition}");
    }

    #endregion

    #region DATA MANAGEMENT

    /// <summary>
    /// Salva dados do jogador
    /// </summary>
    private void SavePlayerData(int clientId)
    {
        if (!activePlayers.ContainsKey(clientId))
            return;

        // TODO: Integrar com ServerSaveSystem
        if (showDebug)
            Debug.Log($"[ServerPlayerManager] Saving data for player {clientId}");
    }

    /// <summary>
    /// Carrega dados do jogador
    /// </summary>
    private void LoadPlayerData(int clientId)
    {
        // TODO: Integrar com ServerSaveSystem
        if (showDebug)
            Debug.Log($"[ServerPlayerManager] Loading data for player {clientId}");
    }

    #endregion

    #region BROADCAST

    /// <summary>
    /// Notifica desconexão de jogador
    /// </summary>
    private void BroadcastPlayerDisconnected(int clientId)
    {
        NetworkMessage message = new NetworkMessage
        {
            type = MessageType.ClientDisconnect,
            clientId = clientId
        };

        NetworkManager.Instance.SendToAllClients(message);
    }

    #endregion

    #region PUBLIC QUERIES

    /// <summary>
    /// Retorna GameObject do jogador
    /// </summary>
    public GameObject GetPlayerObject(int clientId)
    {
        if (activePlayers.TryGetValue(clientId, out ServerPlayerData data))
        {
            return data.playerObject;
        }
        return null;
    }

    /// <summary>
    /// Retorna se jogador está vivo
    /// </summary>
    public bool IsPlayerAlive(int clientId)
    {
        if (activePlayers.TryGetValue(clientId, out ServerPlayerData data))
        {
            return data.isAlive;
        }
        return false;
    }

    /// <summary>
    /// Retorna número de jogadores ativos
    /// </summary>
    public int GetActivePlayerCount()
    {
        return activePlayers.Count;
    }

    /// <summary>
    /// Retorna lista de IDs de jogadores ativos
    /// </summary>
    public List<int> GetActivePlayerIds()
    {
        return new List<int>(activePlayers.Keys);
    }

    #endregion

    #region DEBUG

    private void OnGUI()
    {
        if (!showDebug) return;
        if (!NetworkManager.Instance.isServer) return;

        float width = 300f;
        float height = 150f;
        float x = 20f;
        float y = 20f;

        GUI.color = Color.black;
        GUI.Box(new Rect(x - 2, y - 2, width + 4, height + 4), "");

        GUI.color = Color.white;
        GUILayout.BeginArea(new Rect(x, y, width, height));
        
        GUILayout.Label("=== SERVER PLAYER MANAGER ===", new GUIStyle { alignment = TextAnchor.MiddleCenter });
        GUILayout.Label($"Active Players: {activePlayers.Count}/{maxPlayersPerServer}");
        GUILayout.Label($"Respawn Queue: {respawnQueue.Count}");
        
        int alivePlayers = 0;
        foreach (var data in activePlayers.Values)
        {
            if (data.isAlive) alivePlayers++;
        }
        GUILayout.Label($"Alive: {alivePlayers}");
        GUILayout.Label($"Dead: {activePlayers.Count - alivePlayers}");

        GUILayout.EndArea();
    }

    [ContextMenu("List All Players")]
    private void ListAllPlayers()
    {
        Debug.Log($"=== ACTIVE PLAYERS ({activePlayers.Count}) ===");
        
        foreach (var kvp in activePlayers)
        {
            ServerPlayerData data = kvp.Value;
            Debug.Log($"[{kvp.Key}] Alive: {data.isAlive}, Protected: {data.hasSpawnProtection}, Position: {data.lastPosition}");
        }
    }

    #endregion

    private void OnDestroy()
    {
        // Remove callbacks
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnClientConnected -= OnPlayerConnected;
            NetworkManager.Instance.OnClientDisconnected -= OnPlayerDisconnected;
        }
    }
}

/// <summary>
/// Dados do jogador no servidor
/// </summary>
public class ServerPlayerData
{
    public int clientId;
    public GameObject playerObject;
    public Vector3 lastPosition;
    public float spawnTime;
    public bool isAlive;
    public bool hasSpawnProtection;
    public int killCount;
    public int deathCount;
}

