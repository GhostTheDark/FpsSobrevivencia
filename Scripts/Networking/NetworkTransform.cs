using UnityEngine;

/// <summary>
/// Sincroniza posição e rotação do jogador pela rede
/// Implementa interpolação e predição para movimento suave
/// Server Authoritative com Client-Side Prediction
/// </summary>
public class NetworkTransform : MonoBehaviour
{
    [Header("Network Settings")]
    [SerializeField] private float sendRate = 30f; // Mensagens por segundo
    [SerializeField] private bool useUDP = true;   // UDP = rápido, TCP = confiável

    [Header("Interpolation")]
    [SerializeField] private float interpolationSpeed = 15f;
    [SerializeField] private float snapThreshold = 5f; // Distância para teleportar

    [Header("Debug")]
    [SerializeField] private bool showDebug = false;

    // Identidade
    private int clientId = -1;
    private bool isLocalPlayer = false;
    private bool isInitialized = false;

    // Envio (apenas local)
    private float sendTimer = 0f;
    private float sendInterval = 0f;
    private Vector3 lastSentPosition;
    private Quaternion lastSentRotation;
    private Vector3 lastSentVelocity;

    // Recebimento (apenas remoto)
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private Vector3 currentVelocity;
    private bool isGrounded;
    private bool isCrouching;
    private bool isSprinting;

    // Client-Side Prediction (local)
    private Vector3 predictedPosition;
    private float lastReconciliationTime;

    // Componentes
    private CharacterController characterController;
    private Rigidbody rb;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        rb = GetComponent<Rigidbody>();

        sendInterval = 1f / sendRate;
    }

    /// <summary>
    /// Inicializa o componente
    /// </summary>
    public void Initialize(int id, bool isLocal)
    {
        clientId = id;
        isLocalPlayer = isLocal;
        isInitialized = true;

        if (isLocalPlayer)
        {
            // Jogador local: envia posição
            lastSentPosition = transform.position;
            lastSentRotation = transform.rotation;
            predictedPosition = transform.position;

            // Registra callback para receber correções do servidor
            NetworkManager.Instance.OnMessageReceived += OnNetworkMessage;
        }
        else
        {
            // Jogador remoto: recebe posição
            targetPosition = transform.position;
            targetRotation = transform.rotation;

            // Registra callback para receber atualizações
            NetworkManager.Instance.OnMessageReceived += OnNetworkMessage;
        }

        Debug.Log($"[NetworkTransform] Initialized (ID: {clientId}, Local: {isLocalPlayer})");
    }

    private void Update()
    {
        if (!isInitialized) return;

        if (isLocalPlayer)
        {
            UpdateLocal();
        }
        else
        {
            UpdateRemote();
        }
    }

    #region LOCAL PLAYER

    /// <summary>
    /// Atualização do jogador local
    /// </summary>
    private void UpdateLocal()
    {
        // Envia posição para o servidor em intervalos
        sendTimer += Time.deltaTime;
        if (sendTimer >= sendInterval)
        {
            sendTimer = 0f;
            SendTransformToServer();
        }

        // Client-Side Prediction
        // (O movimento real é feito pelo PlayerController)
        predictedPosition = transform.position;
    }

    /// <summary>
    /// Envia transformação para o servidor
    /// </summary>
    private void SendTransformToServer()
    {
        // Otimização: só envia se mudou significativamente
        float positionChange = Vector3.Distance(transform.position, lastSentPosition);
        float rotationChange = Quaternion.Angle(transform.rotation, lastSentRotation);

        if (positionChange < 0.01f && rotationChange < 1f)
            return;

        // Prepara dados de movimento
        PlayerMovementData movementData = new PlayerMovementData
        {
            position = transform.position,
            rotation = transform.rotation,
            velocity = GetVelocity(),
            isGrounded = IsGrounded(),
            isCrouching = IsCrouching(),
            isSprinting = IsSprinting()
        };

        // Cria mensagem
        NetworkMessage message = new NetworkMessage
        {
            type = MessageType.PlayerMovement,
            clientId = clientId,
            data = movementData.Serialize()
        };

        // Envia via UDP (mais rápido) ou TCP (mais confiável)
        NetworkManager.Instance.SendToServer(message, useTCP: !useUDP);

        // Atualiza último enviado
        lastSentPosition = transform.position;
        lastSentRotation = transform.rotation;
        lastSentVelocity = movementData.velocity;

        if (showDebug)
            Debug.Log($"[NetworkTransform] Sent position: {transform.position}");
    }

    /// <summary>
    /// Recebe correção do servidor (Server Reconciliation)
    /// </summary>
    private void OnServerCorrection(Vector3 serverPosition)
    {
        float error = Vector3.Distance(predictedPosition, serverPosition);

        if (error > snapThreshold)
        {
            // Erro grande: teleporta
            transform.position = serverPosition;
            predictedPosition = serverPosition;
            
            if (showDebug)
                Debug.LogWarning($"[NetworkTransform] Snapped to server position (error: {error:F2}m)");
        }
        else if (error > 0.1f)
        {
            // Erro médio: corrige suavemente
            transform.position = Vector3.Lerp(transform.position, serverPosition, 0.5f);
            predictedPosition = transform.position;
            
            if (showDebug)
                Debug.Log($"[NetworkTransform] Corrected position (error: {error:F2}m)");
        }

        lastReconciliationTime = Time.time;
    }

    #endregion

    #region REMOTE PLAYER

    /// <summary>
    /// Atualização de jogador remoto
    /// </summary>
    private void UpdateRemote()
    {
        // Interpolação suave para posição alvo
        float distance = Vector3.Distance(transform.position, targetPosition);

        if (distance > snapThreshold)
        {
            // Muito longe: teleporta
            transform.position = targetPosition;
            
            if (showDebug)
                Debug.LogWarning($"[NetworkTransform] Remote player snapped (distance: {distance:F2}m)");
        }
        else if (distance > 0.01f)
        {
            // Interpola suavemente
            transform.position = Vector3.Lerp(
                transform.position, 
                targetPosition, 
                interpolationSpeed * Time.deltaTime
            );
        }

        // Interpolação de rotação
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            interpolationSpeed * Time.deltaTime
        );

        if (showDebug && distance > 0.01f)
        {
            Debug.Log($"[NetworkTransform] Interpolating remote player (distance: {distance:F2}m)");
        }
    }

    /// <summary>
    /// Recebe atualização de posição de outro jogador
    /// </summary>
    private void OnRemotePlayerUpdate(PlayerMovementData data)
    {
        targetPosition = data.position;
        targetRotation = data.rotation;
        currentVelocity = data.velocity;
        isGrounded = data.isGrounded;
        isCrouching = data.isCrouching;
        isSprinting = data.isSprinting;

        if (showDebug)
            Debug.Log($"[NetworkTransform] Received remote update: {targetPosition}");
    }

    #endregion

    #region NETWORK CALLBACKS

    /// <summary>
    /// Callback quando recebe mensagem de rede
    /// </summary>
    private void OnNetworkMessage(NetworkMessage message)
    {
        // Ignora mensagens que não são de movimento
        if (message.type != MessageType.PlayerMovement)
            return;

        // Desserializa dados
        PlayerMovementData movementData = PlayerMovementData.Deserialize(message.data);

        if (isLocalPlayer)
        {
            // Jogador local: recebe correção do servidor
            if (NetworkManager.Instance.isServer)
                return; // Servidor não precisa de correção

            OnServerCorrection(movementData.position);
        }
        else
        {
            // Jogador remoto: recebe atualização de movimento
            if (message.clientId == clientId)
            {
                OnRemotePlayerUpdate(movementData);
            }
        }
    }

    #endregion

    #region SERVER BROADCAST

    /// <summary>
    /// (APENAS SERVIDOR) Envia posição para todos os clientes
    /// </summary>
    public void BroadcastTransform()
    {
        if (!NetworkManager.Instance.isServer) return;

        PlayerMovementData movementData = new PlayerMovementData
        {
            position = transform.position,
            rotation = transform.rotation,
            velocity = GetVelocity(),
            isGrounded = IsGrounded(),
            isCrouching = IsCrouching(),
            isSprinting = IsSprinting()
        };

        NetworkMessage message = new NetworkMessage
        {
            type = MessageType.PlayerMovement,
            clientId = clientId,
            data = movementData.Serialize()
        };

        NetworkManager.Instance.SendToAllExcept(clientId, message, useTCP: !useUDP);
    }

    #endregion

    #region HELPERS

    /// <summary>
    /// Obtém velocidade do jogador
    /// </summary>
    private Vector3 GetVelocity()
    {
        if (characterController != null)
            return characterController.velocity;
        
        if (rb != null)
            return rb.velocity;

        return Vector3.zero;
    }

    /// <summary>
    /// Verifica se está no chão
    /// </summary>
    private bool IsGrounded()
    {
        if (characterController != null)
            return characterController.isGrounded;

        // Raycast simples
        return Physics.Raycast(transform.position, Vector3.down, 0.1f);
    }

    /// <summary>
    /// Verifica se está agachado
    /// </summary>
    private bool IsCrouching()
    {
        // Será implementado no PlayerController
        return false;
    }

    /// <summary>
    /// Verifica se está correndo
    /// </summary>
    private bool IsSprinting()
    {
        // Será implementado no PlayerController
        return false;
    }

    /// <summary>
    /// Teleporta o jogador (sem interpolação)
    /// </summary>
    public void Teleport(Vector3 position)
    {
        transform.position = position;
        targetPosition = position;
        predictedPosition = position;
        lastSentPosition = position;

        if (isLocalPlayer)
        {
            SendTransformToServer();
        }
    }

    #endregion

    private void OnDestroy()
    {
        // Remove callback
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnMessageReceived -= OnNetworkMessage;
        }
    }

    #region GIZMOS

    private void OnDrawGizmos()
    {
        if (!showDebug || !isInitialized) return;

        if (isLocalPlayer)
        {
            // Mostra posição enviada
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(lastSentPosition, 0.2f);
            
            // Mostra predição
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(predictedPosition, 0.15f);
        }
        else
        {
            // Mostra posição alvo
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(targetPosition, 0.2f);
            
            // Linha até alvo
            Gizmos.DrawLine(transform.position, targetPosition);
        }
    }

    #endregion
}