using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sincroniza inventário pela rede
/// Server Authoritative - servidor controla TODO o inventário
/// Cliente envia requisições, servidor valida e atualiza
/// </summary>
public class NetworkInventory : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool showDebug = false;

    // Identidade
    private int clientId = -1;
    private bool isLocalPlayer = false;
    private bool isInitialized = false;

    // Componente local
    private InventorySystem inventorySystem;

    private void Awake()
    {
        inventorySystem = GetComponent<InventorySystem>();
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

        // Se for servidor, inicializa inventário vazio
        if (NetworkManager.Instance.isServer && inventorySystem != null)
        {
            inventorySystem.InitializeInventory(clientId);
        }

        Debug.Log($"[NetworkInventory] Initialized (ID: {clientId}, Local: {isLocalPlayer})");
    }

    #region CLIENT REQUESTS

    /// <summary>
    /// Cliente solicita pegar um item
    /// </summary>
    public void RequestPickupItem(int itemId, Vector3 worldPosition)
    {
        if (!isLocalPlayer || !isInitialized) return;

        NetworkMessage message = new NetworkMessage
        {
            type = MessageType.ItemPickup,
            clientId = clientId
        };

        using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
        using (System.IO.BinaryWriter writer = new System.IO.BinaryWriter(ms))
        {
            writer.Write(itemId);
            writer.Write(worldPosition.x);
            writer.Write(worldPosition.y);
            writer.Write(worldPosition.z);

            message.data = ms.ToArray();
        }

        NetworkManager.Instance.SendToServer(message);

        if (showDebug)
            Debug.Log($"[NetworkInventory] Requested pickup item {itemId}");
    }

    /// <summary>
    /// Cliente solicita dropar um item
    /// </summary>
    public void RequestDropItem(int slotIndex, int amount)
    {
        if (!isLocalPlayer || !isInitialized) return;

        NetworkMessage message = new NetworkMessage
        {
            type = MessageType.ItemDrop,
            clientId = clientId
        };

        using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
        using (System.IO.BinaryWriter writer = new System.IO.BinaryWriter(ms))
        {
            writer.Write(slotIndex);
            writer.Write(amount);

            message.data = ms.ToArray();
        }

        NetworkManager.Instance.SendToServer(message);

        if (showDebug)
            Debug.Log($"[NetworkInventory] Requested drop item from slot {slotIndex}");
    }

    /// <summary>
    /// Cliente solicita mover item entre slots
    /// </summary>
    public void RequestMoveItem(int fromSlot, int toSlot, int amount)
    {
        if (!isLocalPlayer || !isInitialized) return;

        NetworkMessage message = new NetworkMessage
        {
            type = MessageType.ItemTransfer,
            clientId = clientId
        };

        using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
        using (System.IO.BinaryWriter writer = new System.IO.BinaryWriter(ms))
        {
            writer.Write(fromSlot);
            writer.Write(toSlot);
            writer.Write(amount);

            message.data = ms.ToArray();
        }

        NetworkManager.Instance.SendToServer(message);

        if (showDebug)
            Debug.Log($"[NetworkInventory] Requested move item from {fromSlot} to {toSlot}");
    }

    /// <summary>
    /// Cliente solicita usar item
    /// </summary>
    public void RequestUseItem(int slotIndex)
    {
        if (!isLocalPlayer || !isInitialized) return;

        // TODO: Implementar quando tivermos sistema de uso de itens
        if (showDebug)
            Debug.Log($"[NetworkInventory] Requested use item at slot {slotIndex}");
    }

    #endregion

    #region SERVER PROCESSING

    /// <summary>
    /// Servidor processa requisição de pickup
    /// </summary>
    private void ServerProcessPickup(NetworkMessage message)
    {
        if (!NetworkManager.Instance.isServer) return;

        int itemId;
        Vector3 worldPosition;

        using (System.IO.MemoryStream ms = new System.IO.MemoryStream(message.data))
        using (System.IO.BinaryReader reader = new System.IO.BinaryReader(ms))
        {
            itemId = reader.ReadInt32();
            worldPosition = new Vector3(
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle()
            );
        }

        // VALIDAÇÃO: Jogador está perto do item?
        float distance = Vector3.Distance(transform.position, worldPosition);
        if (distance > 3f)
        {
            Debug.LogWarning($"[NetworkInventory] Player {message.clientId} too far from item!");
            return;
        }

        // VALIDAÇÃO: Item existe no mundo?
        // (Será implementado com sistema de loot)

        // VALIDAÇÃO: Inventário tem espaço?
        if (inventorySystem != null)
        {
            bool success = inventorySystem.AddItem(itemId, 1);
            
            if (success)
            {
                // Remove item do mundo
                // (Será implementado com sistema de loot)

                // Envia atualização de inventário
                SendInventoryUpdate(message.clientId);

                if (showDebug)
                    Debug.Log($"[NetworkInventory] Player {message.clientId} picked up item {itemId}");
            }
            else
            {
                if (showDebug)
                    Debug.Log($"[NetworkInventory] Player {message.clientId} inventory full!");
            }
        }
    }

    /// <summary>
    /// Servidor processa requisição de drop
    /// </summary>
    private void ServerProcessDrop(NetworkMessage message)
    {
        if (!NetworkManager.Instance.isServer) return;

        int slotIndex;
        int amount;

        using (System.IO.MemoryStream ms = new System.IO.MemoryStream(message.data))
        using (System.IO.BinaryReader reader = new System.IO.BinaryReader(ms))
        {
            slotIndex = reader.ReadInt32();
            amount = reader.ReadInt32();
        }

        // VALIDAÇÃO: Slot é válido?
        if (inventorySystem != null)
        {
            var item = inventorySystem.GetItemAtSlot(slotIndex);
            if (item != null && item.amount >= amount)
            {
                // Remove do inventário
                inventorySystem.RemoveItem(slotIndex, amount);

                // Spawna item no mundo (na frente do jogador)
                Vector3 dropPosition = transform.position + transform.forward * 1.5f;
                // (Será implementado com sistema de loot)

                // Envia atualização de inventário
                SendInventoryUpdate(message.clientId);

                if (showDebug)
                    Debug.Log($"[NetworkInventory] Player {message.clientId} dropped {amount}x item from slot {slotIndex}");
            }
        }
    }

    /// <summary>
    /// Servidor processa requisição de mover item
    /// </summary>
    private void ServerProcessMove(NetworkMessage message)
    {
        if (!NetworkManager.Instance.isServer) return;

        int fromSlot;
        int toSlot;
        int amount;

        using (System.IO.MemoryStream ms = new System.IO.MemoryStream(message.data))
        using (System.IO.BinaryReader reader = new System.IO.BinaryReader(ms))
        {
            fromSlot = reader.ReadInt32();
            toSlot = reader.ReadInt32();
            amount = reader.ReadInt32();
        }

        if (inventorySystem != null)
        {
            bool success = inventorySystem.MoveItem(fromSlot, toSlot, amount);
            
            if (success)
            {
                // Envia atualização de inventário
                SendInventoryUpdate(message.clientId);

                if (showDebug)
                    Debug.Log($"[NetworkInventory] Player {message.clientId} moved item from {fromSlot} to {toSlot}");
            }
        }
    }

    #endregion

    #region INVENTORY SYNC

    /// <summary>
    /// Envia estado completo do inventário para o cliente
    /// </summary>
    private void SendInventoryUpdate(int targetClientId)
    {
        if (!NetworkManager.Instance.isServer) return;
        if (inventorySystem == null) return;

        NetworkMessage message = new NetworkMessage
        {
            type = MessageType.InventoryUpdate,
            clientId = targetClientId
        };

        // Serializa inventário completo
        message.data = SerializeInventory();

        NetworkManager.Instance.SendToClient(targetClientId, message);

        if (showDebug)
            Debug.Log($"[NetworkInventory] Sent inventory update to client {targetClientId}");
    }

    /// <summary>
    /// Serializa inventário para envio
    /// </summary>
    private byte[] SerializeInventory()
    {
        using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
        using (System.IO.BinaryWriter writer = new System.IO.BinaryWriter(ms))
        {
            // Número de slots
            int slotCount = inventorySystem.GetSlotCount();
            writer.Write(slotCount);

            // Para cada slot
            for (int i = 0; i < slotCount; i++)
            {
                var item = inventorySystem.GetItemAtSlot(i);
                
                if (item != null)
                {
                    writer.Write(true); // Slot tem item
                    writer.Write(item.itemId);
                    writer.Write(item.amount);
                    writer.Write(item.durability);
                }
                else
                {
                    writer.Write(false); // Slot vazio
                }
            }

            return ms.ToArray();
        }
    }

    /// <summary>
    /// Recebe e aplica atualização de inventário
    /// </summary>
    private void OnInventoryUpdate(NetworkMessage message)
    {
        if (message.clientId != clientId) return;
        if (inventorySystem == null) return;

        using (System.IO.MemoryStream ms = new System.IO.MemoryStream(message.data))
        using (System.IO.BinaryReader reader = new System.IO.BinaryReader(ms))
        {
            int slotCount = reader.ReadInt32();

            for (int i = 0; i < slotCount; i++)
            {
                bool hasItem = reader.ReadBoolean();
                
                if (hasItem)
                {
                    int itemId = reader.ReadInt32();
                    int amount = reader.ReadInt32();
                    float durability = reader.ReadSingle();

                    inventorySystem.SetSlot(i, itemId, amount, durability);
                }
                else
                {
                    inventorySystem.ClearSlot(i);
                }
            }
        }

        if (showDebug)
            Debug.Log($"[NetworkInventory] Received inventory update");

        // Atualiza UI se for jogador local
        if (isLocalPlayer)
        {
            // TODO: Atualizar InventoryUI
        }
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
            case MessageType.ItemPickup:
                ServerProcessPickup(message);
                break;

            case MessageType.ItemDrop:
                ServerProcessDrop(message);
                break;

            case MessageType.ItemTransfer:
                ServerProcessMove(message);
                break;

            case MessageType.InventoryUpdate:
                OnInventoryUpdate(message);
                break;
        }
    }

    #endregion

    #region PUBLIC METHODS

    /// <summary>
    /// Força sincronização completa do inventário
    /// </summary>
    public void SyncInventory()
    {
        if (NetworkManager.Instance.isServer)
        {
            SendInventoryUpdate(clientId);
        }
    }

    /// <summary>
    /// (APENAS SERVIDOR) Adiciona item diretamente ao inventário
    /// </summary>
    public void ServerAddItem(int itemId, int amount)
    {
        if (!NetworkManager.Instance.isServer) return;
        if (inventorySystem == null) return;

        bool success = inventorySystem.AddItem(itemId, amount);
        
        if (success)
        {
            SendInventoryUpdate(clientId);
            
            if (showDebug)
                Debug.Log($"[NetworkInventory] Server added {amount}x item {itemId} to player {clientId}");
        }
    }

    /// <summary>
    /// (APENAS SERVIDOR) Remove item diretamente do inventário
    /// </summary>
    public void ServerRemoveItem(int itemId, int amount)
    {
        if (!NetworkManager.Instance.isServer) return;
        if (inventorySystem == null) return;

        bool success = inventorySystem.RemoveItemById(itemId, amount);
        
        if (success)
        {
            SendInventoryUpdate(clientId);
            
            if (showDebug)
                Debug.Log($"[NetworkInventory] Server removed {amount}x item {itemId} from player {clientId}");
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