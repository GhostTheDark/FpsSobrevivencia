using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Inicializador do Cliente
/// Este script configura o cliente e conecta ao servidor
/// </summary>
public class ClientBootstrap : MonoBehaviour
{
    [Header("Connection Settings")]
    [SerializeField] private string serverIP = "127.0.0.1";
    [SerializeField] private bool autoConnect = false;

    [Header("Client Settings")]
    [SerializeField] private string playerName = "Player";
    [SerializeField] private int targetFrameRate = 60;

    private bool isConnected = false;
    private bool isInitialized = false;

    private void Start()
    {
        InitializeClient();

        if (autoConnect)
        {
            ConnectToServer(serverIP);
        }
    }

    /// <summary>
    /// Inicializa todos os sistemas do cliente
    /// </summary>
    private void InitializeClient()
    {
        if (isInitialized)
        {
            Debug.LogWarning("[CLIENT] Client already initialized!");
            return;
        }

        Debug.Log("========================================");
        Debug.Log("[CLIENT] Initializing client systems...");
        Debug.Log("========================================");

        // 1. Configura qualidade gráfica
        SetupGraphics();

        // 2. Configura callbacks de rede
        SetupNetworkCallbacks();

        // 3. Inicializa sistemas do cliente
        InitializeClientSystems();

        // 4. Carrega configurações locais
        LoadClientSettings();

        isInitialized = true;

        Debug.Log("[CLIENT] Client initialized successfully!");
    }

    /// <summary>
    /// Configura qualidade gráfica
    /// </summary>
    private void SetupGraphics()
    {
        Application.targetFrameRate = targetFrameRate;
        QualitySettings.vSyncCount = 0; // Desliga VSync para melhor controle de FPS
        
        // Configura cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log($"[CLIENT] Graphics configured: {targetFrameRate} FPS target");
    }

    /// <summary>
    /// Configura callbacks de eventos de rede
    /// </summary>
    private void SetupNetworkCallbacks()
    {
        NetworkManager.Instance.OnMessageReceived += OnNetworkMessageReceived;
        Debug.Log("[CLIENT] Network callbacks configured");
    }

    /// <summary>
    /// Inicializa todos os sistemas do cliente
    /// </summary>
    private void InitializeClientSystems()
    {
        Debug.Log("[CLIENT] Initializing client systems...");

        // HUD Manager
        GameObject hudObj = new GameObject("HUDManager");
        hudObj.AddComponent<HUDManager>();

        // Input Manager (será criado depois)
        GameObject inputObj = new GameObject("InputManager");
        // inputObj.AddComponent<InputManager>();

        // Audio Manager
        GameObject audioObj = new GameObject("AudioManager");
        // audioObj.AddComponent<AudioManager>();

        Debug.Log("[CLIENT] All client systems initialized");
    }

    /// <summary>
    /// Carrega configurações salvas do cliente
    /// </summary>
    private void LoadClientSettings()
    {
        Debug.Log("[CLIENT] Loading client settings...");

        // Carrega nome do jogador
        playerName = PlayerPrefs.GetString("PlayerName", "Player");

        // Carrega último servidor
        string lastServer = PlayerPrefs.GetString("LastServer", "127.0.0.1");
        serverIP = lastServer;

        // Carrega configurações gráficas
        int quality = PlayerPrefs.GetInt("GraphicsQuality", 2);
        QualitySettings.SetQualityLevel(quality);

        // Carrega configurações de áudio
        float masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1.0f);
        AudioListener.volume = masterVolume;

        Debug.Log($"[CLIENT] Settings loaded - Player: {playerName}");
    }

    /// <summary>
    /// Conecta ao servidor
    /// </summary>
    public void ConnectToServer(string ipAddress)
    {
        if (isConnected)
        {
            Debug.LogWarning("[CLIENT] Already connected to a server!");
            return;
        }

        if (string.IsNullOrEmpty(ipAddress))
        {
            Debug.LogError("[CLIENT] Invalid IP address!");
            return;
        }

        Debug.Log($"[CLIENT] Attempting to connect to {ipAddress}...");

        serverIP = ipAddress;
        NetworkManager.Instance.ConnectToServer(ipAddress);

        // Salva último servidor conectado
        PlayerPrefs.SetString("LastServer", ipAddress);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Desconecta do servidor
    /// </summary>
    public void DisconnectFromServer()
    {
        if (!isConnected) return;

        Debug.Log("[CLIENT] Disconnecting from server...");

        NetworkManager.Instance.Disconnect();
        isConnected = false;

        // Volta para menu principal
        LoadMainMenu();
    }

    #region NETWORK CALLBACKS

    /// <summary>
    /// Callback quando recebe mensagem de rede
    /// </summary>
    private void OnNetworkMessageReceived(NetworkMessage message)
    {
        switch (message.type)
        {
            case MessageType.AssignClientId:
                OnConnectedToServer(message.clientId);
                break;

            case MessageType.ServerFull:
                OnServerFull();
                break;

            case MessageType.PlayerSpawn:
                OnPlayerSpawn(message);
                break;

            case MessageType.ChatMessage:
                OnChatMessage(message);
                break;

            case MessageType.WorldState:
                OnWorldStateReceived(message);
                break;

            case MessageType.PlayerMovement:
                // Será tratado no NetworkPlayer
                break;

            case MessageType.WeaponFire:
                // Será tratado no NetworkCombat
                break;

            // Outros tipos serão adicionados conforme necessário
        }
    }

    /// <summary>
    /// Callback quando conecta com sucesso ao servidor
    /// </summary>
    private void OnConnectedToServer(int clientId)
    {
        isConnected = true;
        Debug.Log($"[CLIENT] Successfully connected! Client ID: {clientId}");

        // Envia informações do jogador
        SendPlayerInfo();

        // Carrega cena do jogo
        LoadGameScene();
    }

    /// <summary>
    /// Callback quando servidor está cheio
    /// </summary>
    private void OnServerFull()
    {
        Debug.LogWarning("[CLIENT] Server is full!");
        
        // TODO: Mostrar UI de servidor cheio
        
        isConnected = false;
    }

    /// <summary>
    /// Callback quando o jogador spawna
    /// </summary>
    private void OnPlayerSpawn(NetworkMessage message)
    {
        Vector3 spawnPosition = message.GetVector3();
        Debug.Log($"[CLIENT] Spawning at position: {spawnPosition}");

        // Spawna o jogador local
        SpawnLocalPlayer(spawnPosition);
    }

    /// <summary>
    /// Callback quando recebe mensagem de chat
    /// </summary>
    private void OnChatMessage(NetworkMessage message)
    {
        string chatText = message.GetString();
        int senderId = message.clientId;

        Debug.Log($"[CHAT] Player {senderId}: {chatText}");

        // TODO: Adicionar ao chat UI
    }

    /// <summary>
    /// Callback quando recebe estado do mundo
    /// </summary>
    private void OnWorldStateReceived(NetworkMessage message)
    {
        string welcomeText = message.GetString();
        Debug.Log($"[CLIENT] {welcomeText}");

        // TODO: Receber lista de jogadores
        // TODO: Receber estado inicial do mundo
    }

    #endregion

    #region PLAYER MANAGEMENT

    /// <summary>
    /// Envia informações do jogador para o servidor
    /// </summary>
    private void SendPlayerInfo()
    {
        NetworkMessage message = new NetworkMessage
        {
            type = MessageType.ChatMessage // Temporário, será substituído por PlayerInfo
        };
        message.SetString($"Player {playerName} connected");

        NetworkManager.Instance.SendToServer(message);
    }

    /// <summary>
    /// Spawna o jogador local no mundo
    /// </summary>
    private void SpawnLocalPlayer(Vector3 position)
    {
        // Carrega prefab do jogador
        GameObject playerPrefab = Resources.Load<GameObject>("Prefabs/Player/LocalPlayer");
        
        if (playerPrefab == null)
        {
            Debug.LogError("[CLIENT] LocalPlayer prefab not found!");
            return;
        }

        // Instancia o jogador
        GameObject playerObj = Instantiate(playerPrefab, position, Quaternion.identity);
        playerObj.name = "LocalPlayer";

        // Configura componentes
        PlayerController controller = playerObj.GetComponent<PlayerController>();
        if (controller != null)
        {
            controller.Initialize(NetworkManager.Instance.localClientId, true);
        }

        // Trava cursor para FPS
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("[CLIENT] Local player spawned successfully");
    }

    #endregion

    #region SCENE MANAGEMENT

    /// <summary>
    /// Carrega cena do jogo
    /// </summary>
    private void LoadGameScene()
    {
        Debug.Log("[CLIENT] Loading game scene...");
        SceneManager.LoadScene("GameScene");
    }

    /// <summary>
    /// Volta para menu principal
    /// </summary>
    private void LoadMainMenu()
    {
        Debug.Log("[CLIENT] Loading main menu...");
        
        // Destrava cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene("MainMenu");
    }

    #endregion

    #region CHAT

    /// <summary>
    /// Envia mensagem de chat
    /// </summary>
    public void SendChatMessage(string text)
    {
        if (!isConnected) return;
        if (string.IsNullOrEmpty(text)) return;

        NetworkMessage message = new NetworkMessage
        {
            type = MessageType.ChatMessage
        };
        message.SetString(text);

        NetworkManager.Instance.SendToServer(message);
    }

    #endregion

    #region SETTINGS

    /// <summary>
    /// Salva nome do jogador
    /// </summary>
    public void SetPlayerName(string name)
    {
        playerName = name;
        PlayerPrefs.SetString("PlayerName", name);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Configura qualidade gráfica
    /// </summary>
    public void SetGraphicsQuality(int level)
    {
        QualitySettings.SetQualityLevel(level);
        PlayerPrefs.SetInt("GraphicsQuality", level);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Configura volume
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        AudioListener.volume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("MasterVolume", volume);
        PlayerPrefs.Save();
    }

    #endregion

    private void OnApplicationQuit()
    {
        DisconnectFromServer();
    }

    private void OnDestroy()
    {
        // Remove callbacks
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnMessageReceived -= OnNetworkMessageReceived;
        }
    }
}