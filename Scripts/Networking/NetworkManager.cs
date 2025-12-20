using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

/// <summary>
/// Gerenciador principal de rede - Server Authoritative
/// Controla todas as conexões, mensagens e estado da rede
/// </summary>
public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }

    [Header("Network Configuration")]
    [SerializeField] private int port = 7777;
    [SerializeField] private int maxPlayers = 100;
    [SerializeField] private int tickRate = 30; // Updates por segundo

    [Header("Network State")]
    public bool isServer;
    public bool isClient;
    public bool isConnected;

    // Servidor
    private TcpListener tcpListener;
    private UdpClient udpServer;
    private Dictionary<int, ClientConnection> connectedClients = new Dictionary<int, ClientConnection>();
    private int nextClientId = 1;

    // Cliente
    private TcpClient tcpClient;
    private UdpClient udpClient;
    private NetworkStream clientStream;
    public int localClientId = -1;

    // Network Tick
    private float tickTimer;
    private float tickInterval;

    // Callbacks
    public Action<int> OnClientConnected;
    public Action<int> OnClientDisconnected;
    public Action<NetworkMessage> OnMessageReceived;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        tickInterval = 1f / tickRate;
    }

    #region SERVER

    /// <summary>
    /// Inicia o servidor dedicado
    /// </summary>
    public void StartServer()
    {
        if (isServer) return;

        try
        {
            // TCP para conexões confiáveis (login, chat, etc)
            tcpListener = new TcpListener(IPAddress.Any, port);
            tcpListener.Start();
            tcpListener.BeginAcceptTcpClient(OnClientConnect, null);

            // UDP para dados em tempo real (movimento, combate)
            udpServer = new UdpClient(port);
            udpServer.BeginReceive(OnUDPReceive, null);

            isServer = true;
            Debug.Log($"[SERVER] Started on port {port}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SERVER] Failed to start: {e.Message}");
        }
    }

    /// <summary>
    /// Callback quando cliente conecta via TCP
    /// </summary>
    private void OnClientConnect(IAsyncResult result)
    {
        try
        {
            TcpClient client = tcpListener.EndAcceptTcpClient(result);
            tcpListener.BeginAcceptTcpClient(OnClientConnect, null);

            if (connectedClients.Count >= maxPlayers)
            {
                SendTCPData(client, new NetworkMessage
                {
                    type = MessageType.ServerFull
                });
                client.Close();
                return;
            }

            int clientId = nextClientId++;
            ClientConnection connection = new ClientConnection
            {
                clientId = clientId,
                tcpClient = client,
                stream = client.GetStream()
            };

            connectedClients.Add(clientId, connection);
            
            // Inicia recebimento de dados TCP
            connection.stream.BeginRead(connection.receiveBuffer, 0, 
                ClientConnection.BUFFER_SIZE, OnTCPReceive, connection);

            // Envia ID para o cliente
            SendTCPData(client, new NetworkMessage
            {
                type = MessageType.AssignClientId,
                clientId = clientId
            });

            Debug.Log($"[SERVER] Client {clientId} connected from {client.Client.RemoteEndPoint}");
            OnClientConnected?.Invoke(clientId);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SERVER] Client connect error: {e.Message}");
        }
    }

    /// <summary>
    /// Recebe dados TCP do cliente
    /// </summary>
    private void OnTCPReceive(IAsyncResult result)
    {
        ClientConnection connection = (ClientConnection)result.AsyncState;
        
        try
        {
            int bytesRead = connection.stream.EndRead(result);
            
            if (bytesRead <= 0)
            {
                DisconnectClient(connection.clientId);
                return;
            }

            // Processa mensagem
            NetworkMessage message = NetworkMessage.Deserialize(connection.receiveBuffer, bytesRead);
            message.clientId = connection.clientId;
            ProcessMessage(message);

            // Continua recebendo
            connection.stream.BeginRead(connection.receiveBuffer, 0, 
                ClientConnection.BUFFER_SIZE, OnTCPReceive, connection);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SERVER] TCP receive error: {e.Message}");
            DisconnectClient(connection.clientId);
        }
    }

    /// <summary>
    /// Recebe dados UDP
    /// </summary>
    private void OnUDPReceive(IAsyncResult result)
    {
        try
        {
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
            byte[] data = udpServer.EndReceive(result, ref remoteEP);
            udpServer.BeginReceive(OnUDPReceive, null);

            NetworkMessage message = NetworkMessage.Deserialize(data, data.Length);
            ProcessMessage(message);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SERVER] UDP receive error: {e.Message}");
        }
    }

    /// <summary>
    /// Desconecta um cliente
    /// </summary>
    public void DisconnectClient(int clientId)
    {
        if (!connectedClients.ContainsKey(clientId)) return;

        ClientConnection connection = connectedClients[clientId];
        connection.tcpClient?.Close();
        connectedClients.Remove(clientId);

        Debug.Log($"[SERVER] Client {clientId} disconnected");
        OnClientDisconnected?.Invoke(clientId);
    }

    /// <summary>
    /// Envia mensagem TCP para um cliente específico
    /// </summary>
    public void SendToClient(int clientId, NetworkMessage message, bool useTCP = true)
    {
        if (!isServer || !connectedClients.ContainsKey(clientId)) return;

        ClientConnection connection = connectedClients[clientId];
        byte[] data = NetworkMessage.Serialize(message);

        if (useTCP)
        {
            SendTCPData(connection.tcpClient, message);
        }
        else
        {
            if (connection.udpEndPoint != null)
            {
                udpServer.Send(data, data.Length, connection.udpEndPoint);
            }
        }
    }

    /// <summary>
    /// Envia mensagem para todos os clientes
    /// </summary>
    public void SendToAllClients(NetworkMessage message, bool useTCP = true)
    {
        foreach (var kvp in connectedClients)
        {
            SendToClient(kvp.Key, message, useTCP);
        }
    }

    /// <summary>
    /// Envia mensagem para todos exceto um cliente
    /// </summary>
    public void SendToAllExcept(int excludeClientId, NetworkMessage message, bool useTCP = true)
    {
        foreach (var kvp in connectedClients)
        {
            if (kvp.Key != excludeClientId)
            {
                SendToClient(kvp.Key, message, useTCP);
            }
        }
    }

    #endregion

    #region CLIENT

    /// <summary>
    /// Conecta ao servidor
    /// </summary>
    public void ConnectToServer(string ipAddress)
    {
        if (isClient) return;

        try
        {
            // TCP
            tcpClient = new TcpClient();
            tcpClient.BeginConnect(ipAddress, port, OnServerConnect, null);

            Debug.Log($"[CLIENT] Connecting to {ipAddress}:{port}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[CLIENT] Connection failed: {e.Message}");
        }
    }

    /// <summary>
    /// Callback quando conecta ao servidor
    /// </summary>
    private void OnServerConnect(IAsyncResult result)
    {
        try
        {
            tcpClient.EndConnect(result);
            clientStream = tcpClient.GetStream();

            // UDP
            udpClient = new UdpClient();
            
            // Converte explicitamente EndPoint para IPEndPoint
            EndPoint serverEndPoint = tcpClient.Client.RemoteEndPoint;
            if (serverEndPoint is IPEndPoint ipEndPoint)
            {
                udpClient.Connect(ipEndPoint);
            }
            else
            {
                Debug.LogError("[CLIENT] Remote endpoint is not an IPEndPoint");
                Disconnect();
                return;
            }

            isClient = true;
            isConnected = true;

            // Começa a receber dados
            byte[] receiveBuffer = new byte[ClientConnection.BUFFER_SIZE];
            clientStream.BeginRead(receiveBuffer, 0, receiveBuffer.Length, 
                OnClientReceive, receiveBuffer);

            Debug.Log("[CLIENT] Connected to server");
        }
        catch (Exception e)
        {
            Debug.LogError($"[CLIENT] Server connect error: {e.Message}");
            isConnected = false;
        }
    }

    /// <summary>
    /// Recebe dados do servidor (cliente)
    /// </summary>
    private void OnClientReceive(IAsyncResult result)
    {
        try
        {
            byte[] buffer = (byte[])result.AsyncState;
            int bytesRead = clientStream.EndRead(result);

            if (bytesRead <= 0)
            {
                Disconnect();
                return;
            }

            NetworkMessage message = NetworkMessage.Deserialize(buffer, bytesRead);
            ProcessMessage(message);

            clientStream.BeginRead(buffer, 0, buffer.Length, OnClientReceive, buffer);
        }
        catch (Exception e)
        {
            Debug.LogError($"[CLIENT] Receive error: {e.Message}");
            Disconnect();
        }
    }

    /// <summary>
    /// Desconecta do servidor
    /// </summary>
    public void Disconnect()
    {
        if (isClient)
        {
            tcpClient?.Close();
            udpClient?.Close();
            isClient = false;
            isConnected = false;
            Debug.Log("[CLIENT] Disconnected");
        }

        if (isServer)
        {
            tcpListener?.Stop();
            udpServer?.Close();
            connectedClients.Clear();
            isServer = false;
            Debug.Log("[SERVER] Stopped");
        }
    }

    /// <summary>
    /// Envia mensagem para o servidor
    /// </summary>
    public void SendToServer(NetworkMessage message, bool useTCP = true)
    {
        if (!isClient || !isConnected) return;

        byte[] data = NetworkMessage.Serialize(message);

        try
        {
            if (useTCP)
            {
                clientStream.Write(data, 0, data.Length);
            }
            else
            {
                udpClient.Send(data, data.Length);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[CLIENT] Send error: {e.Message}");
        }
    }

    #endregion

    #region MESSAGE PROCESSING

    /// <summary>
    /// Processa mensagens recebidas
    /// </summary>
    private void ProcessMessage(NetworkMessage message)
    {
        switch (message.type)
        {
            case MessageType.AssignClientId:
                localClientId = message.clientId;
                Debug.Log($"[CLIENT] Assigned ID: {localClientId}");
                break;

            case MessageType.ServerFull:
                Debug.LogWarning("[CLIENT] Server is full");
                Disconnect();
                break;

            default:
                OnMessageReceived?.Invoke(message);
                break;
        }
    }

    /// <summary>
    /// Helper para enviar dados TCP
    /// </summary>
    private void SendTCPData(TcpClient client, NetworkMessage message)
    {
        try
        {
            byte[] data = NetworkMessage.Serialize(message);
            NetworkStream stream = client.GetStream();
            stream.Write(data, 0, data.Length);
        }
        catch (Exception e)
        {
            Debug.LogError($"[TCP] Send error: {e.Message}");
        }
    }

    #endregion

    private void Update()
    {
        // Network Tick para atualizações regulares
        tickTimer += Time.deltaTime;
        if (tickTimer >= tickInterval)
        {
            tickTimer = 0;
            NetworkTick();
        }
    }

    /// <summary>
    /// Chamado a cada tick de rede (30 vezes por segundo)
    /// </summary>
    private void NetworkTick()
    {
        if (isServer)
        {
            // Servidor processa e envia atualizações de estado
            // Isso será expandido nos próximos scripts
        }
    }

    private void OnApplicationQuit()
    {
        Disconnect();
    }
}

/// <summary>
/// Representa uma conexão de cliente no servidor
/// </summary>
public class ClientConnection
{
    public const int BUFFER_SIZE = 4096;
    
    public int clientId;
    public TcpClient tcpClient;
    public NetworkStream stream;
    public IPEndPoint udpEndPoint;
    public byte[] receiveBuffer = new byte[BUFFER_SIZE];
}
