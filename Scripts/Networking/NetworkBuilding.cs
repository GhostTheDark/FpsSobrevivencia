using UnityEngine;

/// <summary>
/// Sincroniza construções pela rede
/// Server Authoritative - servidor valida TODAS as construções
/// Cliente envia requisição, servidor valida e instancia
/// </summary>
public class NetworkBuilding : MonoBehaviour
{
    [Header("Building Settings")]
    [SerializeField] private float maxBuildDistance = 5f;
    [SerializeField] private LayerMask buildingLayer;

    [Header("Anti-Cheat")]
    [SerializeField] private float minBuildInterval = 0.5f; // Min 500ms entre construções

    [Header("Debug")]
    [SerializeField] private bool showDebug = false;

    // Identidade
    private int clientId = -1;
    private bool isLocalPlayer = false;
    private bool isInitialized = false;

    // Anti-cheat
    private float lastBuildTime = 0f;

    // Componentes
    private BuildingSystem buildingSystem;

    private void Awake()
    {
        buildingSystem = GetComponent<BuildingSystem>();
    }

    /// <summary>
    /// Inicializa o componente
    /// </summary>
    public void Initialize(int id, bool isLocal)
    {
        clientId = id;
        isLocalPlayer = isLocal;
        isInitialized = true;

        // Registra callbacks
        NetworkManager.Instance.OnMessageReceived += OnNetworkMessage;

        Debug.Log($"[NetworkBuilding] Initialized (ID: {clientId}, Local: {isLocalPlayer})");
    }

    #region CLIENT REQUESTS

    /// <summary>
    /// Cliente solicita colocar uma peça de construção
    /// </summary>
    public void RequestPlaceBuilding(int buildingPrefabId, Vector3 position, Quaternion rotation)
    {
        if (!isLocalPlayer || !isInitialized) return;

        // Anti-cheat: rate limit
        if (Time.time - lastBuildTime < minBuildInterval)
        {
            if (showDebug)
                Debug.LogWarning("[NetworkBuilding] Building too fast!");
            return;
        }

        lastBuildTime = Time.time;

        NetworkMessage message = new NetworkMessage
        {
            type = MessageType.PlaceBuilding,
            clientId = clientId
        };

        using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
        using (System.IO.BinaryWriter writer = new System.IO.BinaryWriter(ms))
        {
            writer.Write(buildingPrefabId);
            
            // Position
            writer.Write(position.x);
            writer.Write(position.y);
            writer.Write(position.z);
            
            // Rotation (apenas Y para simplificar)
            writer.Write(rotation.eulerAngles.y);

            message.data = ms.ToArray();
        }

        NetworkManager.Instance.SendToServer(message);

        if (showDebug)
            Debug.Log($"[NetworkBuilding] Requested place building {buildingPrefabId} at {position}");
    }

    /// <summary>
    /// Cliente solicita destruir uma construção
    /// </summary>
    public void RequestDestroyBuilding(int buildingInstanceId)
    {
        if (!isLocalPlayer || !isInitialized) return;

        NetworkMessage message = new NetworkMessage
        {
            type = MessageType.DestroyBuilding,
            clientId = clientId
        };

        message.SetInt(buildingInstanceId);

        NetworkManager.Instance.SendToServer(message);

        if (showDebug)
            Debug.Log($"[NetworkBuilding] Requested destroy building {buildingInstanceId}");
    }

    /// <summary>
    /// Cliente solicita upgrade de uma construção
    /// </summary>
    public void RequestUpgradeBuilding(int buildingInstanceId, int newMaterialTier)
    {
        if (!isLocalPlayer || !isInitialized) return;

        NetworkMessage message = new NetworkMessage
        {
            type = MessageType.UpgradeBuilding,
            clientId = clientId
        };

        using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
        using (System.IO.BinaryWriter writer = new System.IO.BinaryWriter(ms))
        {
            writer.Write(buildingInstanceId);
            writer.Write(newMaterialTier);

            message.data = ms.ToArray();
        }

        NetworkManager.Instance.SendToServer(message);

        if (showDebug)
            Debug.Log($"[NetworkBuilding] Requested upgrade building {buildingInstanceId} to tier {newMaterialTier}");
    }

    #endregion

    #region SERVER PROCESSING

    /// <summary>
    /// Servidor processa requisição de construção
    /// </summary>
    private void ServerProcessPlaceBuilding(NetworkMessage message)
    {
        if (!NetworkManager.Instance.isServer) return;

        int buildingPrefabId;
        Vector3 position;
        Quaternion rotation;

        using (System.IO.MemoryStream ms = new System.IO.MemoryStream(message.data))
        using (System.IO.BinaryReader reader = new System.IO.BinaryReader(ms))
        {
            buildingPrefabId = reader.ReadInt32();
            
            position = new Vector3(
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle()
            );
            
            float rotationY = reader.ReadSingle();
            rotation = Quaternion.Euler(0, rotationY, 0);
        }

        // VALIDAÇÃO 1: Jogador está perto o suficiente?
        float distance = Vector3.Distance(transform.position, position);
        if (distance > maxBuildDistance)
        {
            Debug.LogWarning($"[NetworkBuilding] Player {message.clientId} trying to build too far away!");
            return;
        }

        // VALIDAÇÃO 2: Posição é válida? (não tem overlap, está em fundação, etc)
        if (!IsValidBuildPosition(position, buildingPrefabId))
        {
            if (showDebug)
                Debug.Log($"[NetworkBuilding] Invalid build position for player {message.clientId}");
            return;
        }

        // VALIDAÇÃO 3: Jogador tem recursos necessários?
        if (!HasRequiredResources(message.clientId, buildingPrefabId))
        {
            if (showDebug)
                Debug.Log($"[NetworkBuilding] Player {message.clientId} doesn't have required resources");
            return;
        }

        // VALIDAÇÃO 4: Está dentro de área de Tool Cupboard?
        if (!IsInAuthorizedArea(message.clientId, position))
        {
            if (showDebug)
                Debug.Log($"[NetworkBuilding] Player {message.clientId} not authorized to build here");
            return;
        }

        // TUDO VÁLIDO: Instancia a construção
        int buildingInstanceId = SpawnBuilding(buildingPrefabId, position, rotation, message.clientId);

        // Consome recursos do jogador
        ConsumeResources(message.clientId, buildingPrefabId);

        // Envia confirmação para todos os clientes
        BroadcastBuildingPlaced(buildingInstanceId, buildingPrefabId, position, rotation, message.clientId);

        if (showDebug)
            Debug.Log($"[NetworkBuilding] Player {message.clientId} placed building {buildingPrefabId} (ID: {buildingInstanceId})");
    }

    /// <summary>
    /// Servidor processa requisição de destruição
    /// </summary>
    private void ServerProcessDestroyBuilding(NetworkMessage message)
    {
        if (!NetworkManager.Instance.isServer) return;

        int buildingInstanceId = message.GetInt();

        // VALIDAÇÃO: Building existe?
        GameObject buildingObj = GetBuildingById(buildingInstanceId);
        if (buildingObj == null)
        {
            Debug.LogWarning($"[NetworkBuilding] Building {buildingInstanceId} not found!");
            return;
        }

        BuildingPiece buildingPiece = buildingObj.GetComponent<BuildingPiece>();
        if (buildingPiece == null) return;

        // VALIDAÇÃO: Jogador é dono ou tem autorização?
        if (!IsAuthorizedToDestroy(message.clientId, buildingPiece))
        {
            Debug.LogWarning($"[NetworkBuilding] Player {message.clientId} not authorized to destroy building {buildingInstanceId}");
            return;
        }

        // Destrói a construção
        Destroy(buildingObj);

        // Envia para todos os clientes
        BroadcastBuildingDestroyed(buildingInstanceId);

        if (showDebug)
            Debug.Log($"[NetworkBuilding] Player {message.clientId} destroyed building {buildingInstanceId}");
    }

    /// <summary>
    /// Servidor processa requisição de upgrade
    /// </summary>
    private void ServerProcessUpgradeBuilding(NetworkMessage message)
    {
        if (!NetworkManager.Instance.isServer) return;

        int buildingInstanceId;
        int newMaterialTier;

        using (System.IO.MemoryStream ms = new System.IO.MemoryStream(message.data))
        using (System.IO.BinaryReader reader = new System.IO.BinaryReader(ms))
        {
            buildingInstanceId = reader.ReadInt32();
            newMaterialTier = reader.ReadInt32();
        }

        // VALIDAÇÃO: Building existe?
        GameObject buildingObj = GetBuildingById(buildingInstanceId);
        if (buildingObj == null) return;

        BuildingPiece buildingPiece = buildingObj.GetComponent<BuildingPiece>();
        if (buildingPiece == null) return;

        // VALIDAÇÃO: Jogador tem recursos?
        if (!HasUpgradeResources(message.clientId, buildingPiece, newMaterialTier))
        {
            if (showDebug)
                Debug.Log($"[NetworkBuilding] Player {message.clientId} doesn't have upgrade resources");
            return;
        }

        // Aplica upgrade
        buildingPiece.UpgradeMaterial(newMaterialTier);

        // Consome recursos
        ConsumeUpgradeResources(message.clientId, buildingPiece, newMaterialTier);

        // Envia para todos
        BroadcastBuildingUpgraded(buildingInstanceId, newMaterialTier);

        if (showDebug)
            Debug.Log($"[NetworkBuilding] Building {buildingInstanceId} upgraded to tier {newMaterialTier}");
    }

    #endregion

    #region VALIDATION HELPERS

    /// <summary>
    /// Verifica se posição de construção é válida
    /// </summary>
    private bool IsValidBuildPosition(Vector3 position, int buildingPrefabId)
    {
        // TODO: Verificar overlap com outras construções
        // TODO: Verificar se está em fundação válida
        // TODO: Verificar terreno
        return true; // Temporário
    }

    /// <summary>
    /// Verifica se jogador tem recursos necessários
    /// </summary>
    private bool HasRequiredResources(int playerId, int buildingPrefabId)
    {
        // TODO: Verificar inventário do jogador
        return true; // Temporário
    }

    /// <summary>
    /// Verifica se está em área autorizada (Tool Cupboard)
    /// </summary>
    private bool IsInAuthorizedArea(int playerId, Vector3 position)
    {
        // TODO: Verificar Tool Cupboards próximos
        return true; // Temporário
    }

    /// <summary>
    /// Verifica se jogador pode destruir construção
    /// </summary>
    private bool IsAuthorizedToDestroy(int playerId, BuildingPiece building)
    {
        // TODO: Verificar ownership
        // TODO: Verificar Tool Cupboard
        return true; // Temporário
    }

    /// <summary>
    /// Verifica se jogador tem recursos para upgrade
    /// </summary>
    private bool HasUpgradeResources(int playerId, BuildingPiece building, int newTier)
    {
        // TODO: Verificar inventário
        return true; // Temporário
    }

    #endregion

    #region BUILDING MANAGEMENT

    /// <summary>
    /// Spawna construção no servidor
    /// </summary>
    private int SpawnBuilding(int prefabId, Vector3 position, Quaternion rotation, int ownerId)
    {
        // TODO: Carregar prefab do building
        // TODO: Instanciar
        // TODO: Configurar BuildingPiece
        // TODO: Adicionar ao sistema de gerenciamento

        int instanceId = Random.Range(1000, 9999); // Temporário
        return instanceId;
    }

    /// <summary>
    /// Busca building por ID
    /// </summary>
    private GameObject GetBuildingById(int instanceId)
    {
        // TODO: Buscar no sistema de gerenciamento
        return null; // Temporário
    }

    /// <summary>
    /// Consome recursos do jogador
    /// </summary>
    private void ConsumeResources(int playerId, int buildingPrefabId)
    {
        // TODO: Remover recursos do inventário via NetworkInventory
    }

    /// <summary>
    /// Consome recursos de upgrade
    /// </summary>
    private void ConsumeUpgradeResources(int playerId, BuildingPiece building, int newTier)
    {
        // TODO: Remover recursos do inventário
    }

    #endregion

    #region BROADCAST

    /// <summary>
    /// Envia confirmação de construção para todos
    /// </summary>
    private void BroadcastBuildingPlaced(int instanceId, int prefabId, Vector3 position, Quaternion rotation, int ownerId)
    {
        NetworkMessage message = new NetworkMessage
        {
            type = MessageType.PlaceBuilding,
            clientId = ownerId
        };

        using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
        using (System.IO.BinaryWriter writer = new System.IO.BinaryWriter(ms))
        {
            writer.Write(instanceId);
            writer.Write(prefabId);
            writer.Write(position.x);
            writer.Write(position.y);
            writer.Write(position.z);
            writer.Write(rotation.eulerAngles.y);

            message.data = ms.ToArray();
        }

        NetworkManager.Instance.SendToAllClients(message);
    }

    /// <summary>
    /// Envia destruição para todos
    /// </summary>
    private void BroadcastBuildingDestroyed(int instanceId)
    {
        NetworkMessage message = new NetworkMessage
        {
            type = MessageType.DestroyBuilding
        };
        message.SetInt(instanceId);

        NetworkManager.Instance.SendToAllClients(message);
    }

    /// <summary>
    /// Envia upgrade para todos
    /// </summary>
    private void BroadcastBuildingUpgraded(int instanceId, int newTier)
    {
        NetworkMessage message = new NetworkMessage
        {
            type = MessageType.UpgradeBuilding
        };

        using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
        using (System.IO.BinaryWriter writer = new System.IO.BinaryWriter(ms))
        {
            writer.Write(instanceId);
            writer.Write(newTier);

            message.data = ms.ToArray();
        }

        NetworkManager.Instance.SendToAllClients(message);
    }

    #endregion

    #region CLIENT RECEIVE

    /// <summary>
    /// Cliente recebe confirmação de construção
    /// </summary>
    private void OnBuildingPlaced(NetworkMessage message)
    {
        int instanceId;
        int prefabId;
        Vector3 position;
        Quaternion rotation;

        using (System.IO.MemoryStream ms = new System.IO.MemoryStream(message.data))
        using (System.IO.BinaryReader reader = new System.IO.BinaryReader(ms))
        {
            instanceId = reader.ReadInt32();
            prefabId = reader.ReadInt32();
            
            position = new Vector3(
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle()
            );
            
            rotation = Quaternion.Euler(0, reader.ReadSingle(), 0);
        }

        // TODO: Instanciar building no cliente
        
        if (showDebug)
            Debug.Log($"[NetworkBuilding] Building placed: {prefabId} at {position}");
    }

    /// <summary>
    /// Cliente recebe destruição de construção
    /// </summary>
    private void OnBuildingDestroyed(NetworkMessage message)
    {
        int instanceId = message.GetInt();

        // TODO: Destruir building no cliente
        
        if (showDebug)
            Debug.Log($"[NetworkBuilding] Building destroyed: {instanceId}");
    }

    /// <summary>
    /// Cliente recebe upgrade de construção
    /// </summary>
    private void OnBuildingUpgraded(NetworkMessage message)
    {
        int instanceId;
        int newTier;

        using (System.IO.MemoryStream ms = new System.IO.MemoryStream(message.data))
        using (System.IO.BinaryReader reader = new System.IO.BinaryReader(ms))
        {
            instanceId = reader.ReadInt32();
            newTier = reader.ReadInt32();
        }

        // TODO: Atualizar visual do building
        
        if (showDebug)
            Debug.Log($"[NetworkBuilding] Building upgraded: {instanceId} to tier {newTier}");
    }

    #endregion

    #region NETWORK CALLBACKS

    /// <summary>
    /// Callback quando recebe mensagem de rede
    /// </summary>
    private void OnNetworkMessage(NetworkMessage message)
    {
        switch (message.type)
        {
            case MessageType.PlaceBuilding:
                if (NetworkManager.Instance.isServer)
                    ServerProcessPlaceBuilding(message);
                else
                    OnBuildingPlaced(message);
                break;

            case MessageType.DestroyBuilding:
                if (NetworkManager.Instance.isServer)
                    ServerProcessDestroyBuilding(message);
                else
                    OnBuildingDestroyed(message);
                break;

            case MessageType.UpgradeBuilding:
                if (NetworkManager.Instance.isServer)
                    ServerProcessUpgradeBuilding(message);
                else
                    OnBuildingUpgraded(message);
                break;
        }
    }

    #endregion

    private void OnDestroy()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnMessageReceived -= OnNetworkMessage;
        }
    }
}