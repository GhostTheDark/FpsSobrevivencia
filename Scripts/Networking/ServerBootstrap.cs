using UnityEngine;

/// <summary>
/// Inicializador do Servidor Dedicado
/// Este script configura o servidor quando executado em modo headless
/// </summary>
public class ServerBootstrap : MonoBehaviour
{
    [Header("Server Configuration")]
    [SerializeField] private string serverName = "Rust Clone Server";
    [SerializeField] private int maxPlayers = 100;
    [SerializeField] private int port = 7777;
    [SerializeField] private bool autoStart = true;

    [Header("World Settings")]
    [SerializeField] private int worldSeed = 12345;
    [SerializeField] private int worldSize = 4000;

    [Header("Server Rules")]
    [SerializeField] private bool pvpEnabled = true;
    [SerializeField] private bool raidingEnabled = true;
    [SerializeField] private float gatherRateMultiplier = 1.0f;
    [SerializeField] private float craftTimeMultiplier = 1.0f;

    private bool isServerRunning = false;

    private void Start()
    {
        // Detecta se está rodando em modo headless (servidor dedicado)
        bool isHeadless = SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null;

        if (isHeadless || autoStart)
        {
            InitializeServer();
        }
        else
        {
            Debug.Log("[SERVER] Headless mode not detected. Use ServerBootstrap.StartServer() to start manually.");
        }
    }

    /// <summary>
    /// Inicializa todos os sistemas do servidor
    /// </summary>
    public void InitializeServer()
    {
        if (isServerRunning)
        {
            Debug.LogWarning("[SERVER] Server is already running!");
            return;
        }

        Debug.Log("========================================");
        Debug.Log($"[SERVER] Starting {serverName}");
        Debug.Log($"[SERVER] Port: {port}");
        Debug.Log($"[SERVER] Max Players: {maxPlayers}");
        Debug.Log($"[SERVER] World Seed: {worldSeed}");
        Debug.Log("========================================");

        // 1. Inicializa Network Manager
        NetworkManager.Instance.StartServer();

        // 2. Configura callbacks de rede
        SetupNetworkCallbacks();

        // 3. Inicializa World Manager
        InitializeWorld();

        // 4. Inicializa sistemas do servidor
        InitializeServerSystems();

        // 5. Carrega dados salvos (se existirem)
        LoadServerData();

        isServerRunning = true;

        Debug.Log("[SERVER] Server is ready and listening for connections!");
    }

    /// <summary>
    /// Configura callbacks de eventos de rede
    /// </summary>
    private void SetupNetworkCallbacks()
    {
        NetworkManager.Instance.OnClientConnected += OnPlayerConnected;
        NetworkManager.Instance.OnClientDisconnected += OnPlayerDisconnected;
        NetworkManager.Instance.OnMessageReceived += OnNetworkMessageReceived;

        Debug.Log("[SERVER] Network callbacks configured");
    }

    /// <summary>
    /// Inicializa o mundo do jogo
    /// </summary>
    private void InitializeWorld()
    {
        // Será implementado no ServerWorldManager
        Debug.Log($"[SERVER] Initializing world with seed {worldSeed}");
        
        // TODO: Gerar terreno procedural
        // TODO: Spawnar recursos
        // TODO: Spawnar monumentos
        // TODO: Configurar zonas de radiação
        
        Debug.Log("[SERVER] World initialized");
    }

    /// <summary>
    /// Inicializa todos os sistemas do servidor
    /// </summary>
    private void InitializeServerSystems()
    {
        Debug.Log("[SERVER] Initializing server systems...");

        // Player Manager
        GameObject playerManagerObj = new GameObject("ServerPlayerManager");
        playerManagerObj.AddComponent<ServerPlayerManager>();

        // World Manager
        GameObject worldManagerObj = new GameObject("ServerWorldManager");
        worldManagerObj.AddComponent<ServerWorldManager>();

        // Save System
        GameObject saveSystemObj = new GameObject("ServerSaveSystem");
        saveSystemObj.AddComponent<ServerSaveSystem>();

        // Loot Spawner
        GameObject lootSpawnerObj = new GameObject("ServerLootSpawner");
        lootSpawnerObj.AddComponent<ServerLootSpawner>();

        Debug.Log("[SERVER] All systems initialized");
    }

    /// <summary>
    /// Carrega dados salvos do servidor
    /// </summary>
    private void LoadServerData()
    {
        // Será implementado no ServerSaveSystem
        Debug.Log("[SERVER] Loading saved data...");
        
        // TODO: Carregar bases dos jogadores
        // TODO: Carregar inventários
        // TODO: Carregar posições dos jogadores
        
        Debug.Log("[SERVER] Server data loaded");
    }

    #region NETWORK CALLBACKS

    /// <summary>
    /// Callback quando um jogador conecta
    /// </summary>
    private void OnPlayerConnected(int clientId)
    {
        Debug.Log($"[SERVER] Player {clientId} connected");

        // Envia informações do servidor
        NetworkMessage welcomeMessage = new NetworkMessage
        {
            type = MessageType.WorldState,
            clientId = clientId
        };
        welcomeMessage.SetString($"Welcome to {serverName}!");

        NetworkManager.Instance.SendToClient(clientId, welcomeMessage);

        // Spawna o jogador no mundo
        SpawnPlayer(clientId);

        // Notifica outros jogadores
        BroadcastPlayerJoined(clientId);
    }

    /// <summary>
    /// Callback quando um jogador desconecta
    /// </summary>
    private void OnPlayerDisconnected(int clientId)
    {
        Debug.Log($"[SERVER] Player {clientId} disconnected");

        // Salva dados do jogador
        SavePlayerData(clientId);

        // Remove do mundo
        DespawnPlayer(clientId);

        // Notifica outros jogadores
        BroadcastPlayerLeft(clientId);
    }

    /// <summary>
    /// Callback quando recebe mensagem de rede
    /// </summary>
    private void OnNetworkMessageReceived(NetworkMessage message)
    {
        // Roteamento de mensagens será expandido nos próximos scripts
        switch (message.type)
        {
            case MessageType.ChatMessage:
                HandleChatMessage(message);
                break;

            case MessageType.PlayerMovement:
                // Será tratado no NetworkTransform
                break;

            case MessageType.WeaponFire:
                // Será tratado no NetworkCombat
                break;

            // Outros tipos serão adicionados conforme necessário
        }
    }

    #endregion

    #region PLAYER MANAGEMENT

    /// <summary>
    /// Spawna um jogador no mundo
    /// </summary>
    private void SpawnPlayer(int clientId)
    {
        // Será implementado no ServerPlayerManager
        Debug.Log($"[SERVER] Spawning player {clientId}");

        // TODO: Verificar se jogador tem sleeping bag
        // TODO: Se não, spawnar em praia aleatória
        // TODO: Criar entidade do jogador
        // TODO: Enviar informações de spawn para o cliente

        NetworkMessage spawnMessage = new NetworkMessage
        {
            type = MessageType.PlayerSpawn,
            clientId = clientId
        };

        // Posição inicial (será substituída por lógica real)
        Vector3 spawnPosition = new Vector3(
            Random.Range(-worldSize/2, worldSize/2),
            100f,
            Random.Range(-worldSize/2, worldSize/2)
        );

        spawnMessage.SetVector3(spawnPosition);
        NetworkManager.Instance.SendToClient(clientId, spawnMessage);
    }

    /// <summary>
    /// Remove um jogador do mundo
    /// </summary>
    private void DespawnPlayer(int clientId)
    {
        Debug.Log($"[SERVER] Despawning player {clientId}");
        
        // TODO: Remover entidade
        // TODO: Dropar itens se morreu
        // TODO: Atualizar base (decay timer)
    }

    /// <summary>
    /// Salva dados de um jogador
    /// </summary>
    private void SavePlayerData(int clientId)
    {
        Debug.Log($"[SERVER] Saving data for player {clientId}");
        
        // Será implementado no ServerSaveSystem
    }

    /// <summary>
    /// Notifica outros jogadores sobre novo jogador
    /// </summary>
    private void BroadcastPlayerJoined(int clientId)
    {
        NetworkMessage message = new NetworkMessage
        {
            type = MessageType.ChatMessage,
            clientId = -1 // Sistema
        };
        message.SetString($"Player {clientId} joined the server");

        NetworkManager.Instance.SendToAllExcept(clientId, message);
    }

    /// <summary>
    /// Notifica outros jogadores sobre jogador que saiu
    /// </summary>
    private void BroadcastPlayerLeft(int clientId)
    {
        NetworkMessage message = new NetworkMessage
        {
            type = MessageType.ChatMessage,
            clientId = -1 // Sistema
        };
        message.SetString($"Player {clientId} left the server");

        NetworkManager.Instance.SendToAllClients(message);
    }

    #endregion

    #region CHAT

    /// <summary>
    /// Processa mensagem de chat
    /// </summary>
    private void HandleChatMessage(NetworkMessage message)
    {
        string chatText = message.GetString();
        Debug.Log($"[CHAT] Player {message.clientId}: {chatText}");

        // Valida mensagem
        if (string.IsNullOrEmpty(chatText) || chatText.Length > 256)
            return;

        // TODO: Filtro de palavrões
        // TODO: Anti-spam

        // Envia para todos os jogadores
        NetworkManager.Instance.SendToAllClients(message);
    }

    #endregion

    /// <summary>
    /// Para o servidor e salva tudo
    /// </summary>
    public void ShutdownServer()
    {
        if (!isServerRunning) return;

        Debug.Log("[SERVER] Shutting down server...");

        // Salva todos os dados
        SaveAllData();

        // Desconecta todos os jogadores
        DisconnectAllPlayers();

        // Para o network manager
        NetworkManager.Instance.Disconnect();

        isServerRunning = false;

        Debug.Log("[SERVER] Server shutdown complete");
    }

    /// <summary>
    /// Salva todos os dados do servidor
    /// </summary>
    private void SaveAllData()
    {
        Debug.Log("[SERVER] Saving all data...");
        
        // Será implementado no ServerSaveSystem
        // TODO: Salvar bases
        // TODO: Salvar jogadores
        // TODO: Salvar estado do mundo
    }

    /// <summary>
    /// Desconecta todos os jogadores
    /// </summary>
    private void DisconnectAllPlayers()
    {
        Debug.Log("[SERVER] Disconnecting all players...");
        
        // NetworkManager já tem essa funcionalidade no Disconnect()
    }

    private void OnApplicationQuit()
    {
        ShutdownServer();
    }

    private void OnDestroy()
    {
        // Remove callbacks
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnClientConnected -= OnPlayerConnected;
            NetworkManager.Instance.OnClientDisconnected -= OnPlayerDisconnected;
            NetworkManager.Instance.OnMessageReceived -= OnNetworkMessageReceived;
        }
    }
}