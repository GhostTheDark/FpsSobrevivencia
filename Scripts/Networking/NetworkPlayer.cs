using UnityEngine;

/// <summary>
/// Representa um jogador na rede (local ou remoto)
/// Gerencia identificação, sincronização básica e referências
/// </summary>
public class NetworkPlayer : MonoBehaviour
{
    [Header("Network Identity")]
    public int clientId = -1;
    public bool isLocalPlayer = false;
    public string playerName = "Player";

    [Header("Network Components")]
    public NetworkTransform networkTransform;
    public NetworkCombat networkCombat;
    public NetworkInventory networkInventory;

    [Header("Player Components")]
    public PlayerController playerController;
    public PlayerStats playerStats;
    public PlayerHealth playerHealth;

    [Header("Visuals")]
    public GameObject firstPersonModel;  // Modelo em 1ª pessoa (apenas local)
    public GameObject thirdPersonModel;  // Modelo em 3ª pessoa (para outros)
    public Transform headTransform;      // Posição da câmera

    // Estado de rede
    private bool isInitialized = false;
    private float lastSyncTime = 0f;
    private const float SYNC_INTERVAL = 0.033f; // ~30Hz

    private void Awake()
    {
        // Obtém referências
        networkTransform = GetComponent<NetworkTransform>();
        networkCombat = GetComponent<NetworkCombat>();
        networkInventory = GetComponent<NetworkInventory>();
        
        playerController = GetComponent<PlayerController>();
        playerStats = GetComponent<PlayerStats>();
        playerHealth = GetComponent<PlayerHealth>();
    }

    /// <summary>
    /// Inicializa o jogador na rede
    /// </summary>
    public void Initialize(int id, bool isLocal)
    {
        if (isInitialized)
        {
            Debug.LogWarning($"[NetworkPlayer] Player {clientId} already initialized!");
            return;
        }

        clientId = id;
        isLocalPlayer = isLocal;

        // Configura nome
        gameObject.name = isLocalPlayer ? "LocalPlayer" : $"RemotePlayer_{clientId}";

        // Configura componentes de rede
        if (networkTransform != null)
            networkTransform.Initialize(clientId, isLocalPlayer);
        
        if (networkCombat != null)
            networkCombat.Initialize(clientId, isLocalPlayer);
        
        if (networkInventory != null)
            networkInventory.Initialize(clientId, isLocalPlayer);

        // Configura visuals
        SetupVisuals();

        // Configura componentes do jogador
        if (playerController != null)
            playerController.Initialize(clientId, isLocalPlayer);

        if (playerStats != null)
            playerStats.Initialize(clientId, isLocalPlayer);

        if (playerHealth != null)
            playerHealth.Initialize(clientId, isLocalPlayer);

        isInitialized = true;

        Debug.Log($"[NetworkPlayer] {gameObject.name} initialized (ID: {clientId}, Local: {isLocalPlayer})");
    }

    /// <summary>
    /// Configura modelos 1ª/3ª pessoa
    /// </summary>
    private void SetupVisuals()
    {
        if (isLocalPlayer)
        {
            // Jogador local: ativa 1ª pessoa, desativa 3ª pessoa
            if (firstPersonModel != null)
                firstPersonModel.SetActive(true);
            
            if (thirdPersonModel != null)
                thirdPersonModel.SetActive(false);

            // Configura layer para não renderizar nas próprias mãos
            SetLayerRecursively(firstPersonModel, LayerMask.NameToLayer("FirstPerson"));
        }
        else
        {
            // Jogador remoto: desativa 1ª pessoa, ativa 3ª pessoa
            if (firstPersonModel != null)
                firstPersonModel.SetActive(false);
            
            if (thirdPersonModel != null)
                thirdPersonModel.SetActive(true);

            // Desativa componentes locais
            if (playerController != null)
                playerController.enabled = false;

            // Desativa câmera se existir
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null)
                cam.enabled = false;
        }
    }

    /// <summary>
    /// Define layer recursivamente
    /// </summary>
    private void SetLayerRecursively(GameObject obj, int layer)
    {
        if (obj == null) return;

        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    private void Update()
    {
        if (!isInitialized) return;

        // Apenas jogador local envia atualizações
        if (isLocalPlayer)
        {
            // Sincronização automática em intervalo
            lastSyncTime += Time.deltaTime;
            if (lastSyncTime >= SYNC_INTERVAL)
            {
                lastSyncTime = 0f;
                SyncToServer();
            }
        }
    }

    /// <summary>
    /// Envia estado atual para o servidor
    /// </summary>
    private void SyncToServer()
    {
        // NetworkTransform já cuida de movimento
        // Aqui enviamos apenas mudanças importantes

        // TODO: Sincronizar animações
        // TODO: Sincronizar estado de equipamento
    }

    #region PUBLIC METHODS

    /// <summary>
    /// Define nome do jogador
    /// </summary>
    public void SetPlayerName(string name)
    {
        playerName = name;
        
        // Atualiza no servidor se for jogador local
        if (isLocalPlayer && NetworkManager.Instance.isConnected)
        {
            // TODO: Enviar atualização de nome
        }
    }

    /// <summary>
    /// Teleporta o jogador (apenas servidor)
    /// </summary>
    public void Teleport(Vector3 position)
    {
        if (!NetworkManager.Instance.isServer) return;

        transform.position = position;

        // Envia para todos os clientes
        NetworkMessage message = new NetworkMessage
        {
            type = MessageType.PlayerMovement,
            clientId = clientId
        };
        message.SetVector3(position);

        NetworkManager.Instance.SendToAllClients(message, useTCP: true);
    }

    /// <summary>
    /// Mata o jogador
    /// </summary>
    public void Kill(int killerId, DamageType damageType)
    {
        if (playerHealth != null)
        {
            playerHealth.Die(killerId, damageType);
        }
    }

    /// <summary>
    /// Respawna o jogador
    /// </summary>
    public void Respawn(Vector3 position)
    {
        transform.position = position;

        if (playerHealth != null)
        {
            playerHealth.Respawn();
        }

        if (playerStats != null)
        {
            playerStats.ResetStats();
        }

        Debug.Log($"[NetworkPlayer] Player {clientId} respawned at {position}");
    }

    #endregion

    #region NETWORK EVENTS

    /// <summary>
    /// Callback quando outro jogador entra no servidor
    /// </summary>
    public void OnPlayerJoined(int otherClientId)
    {
        if (!isLocalPlayer) return;

        Debug.Log($"[NetworkPlayer] Player {otherClientId} joined the game");
        
        // TODO: Mostrar notificação na UI
    }

    /// <summary>
    /// Callback quando outro jogador sai do servidor
    /// </summary>
    public void OnPlayerLeft(int otherClientId)
    {
        if (!isLocalPlayer) return;

        Debug.Log($"[NetworkPlayer] Player {otherClientId} left the game");
        
        // TODO: Mostrar notificação na UI
    }

    #endregion

    private void OnDestroy()
    {
        // Cleanup
        if (isLocalPlayer && NetworkManager.Instance != null && NetworkManager.Instance.isConnected)
        {
            // Notifica desconexão
            NetworkMessage message = new NetworkMessage
            {
                type = MessageType.ClientDisconnect,
                clientId = clientId
            };
            NetworkManager.Instance.SendToServer(message);
        }

        Debug.Log($"[NetworkPlayer] Player {clientId} destroyed");
    }

    #region GIZMOS

    private void OnDrawGizmos()
    {
        if (!isInitialized) return;

        // Desenha esfera colorida sobre o jogador
        Gizmos.color = isLocalPlayer ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.3f);

        // Desenha ID
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 2.5f, 
            $"ID: {clientId}\n{(isLocalPlayer ? "LOCAL" : "REMOTE")}"
        );
        #endif
    }

    #endregion
}