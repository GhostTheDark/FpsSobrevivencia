using UnityEngine;

/// <summary>
/// Sincroniza combate pela rede
/// Server Authoritative - servidor valida TODOS os hits e danos
/// Cliente envia disparo, servidor calcula hit e distribui dano
/// </summary>
public class NetworkCombat : MonoBehaviour
{
    [Header("Network Settings")]
    [SerializeField] private bool validateHitsOnServer = true;
    [SerializeField] private float maxRaycastDistance = 300f;

    [Header("Anti-Cheat")]
    [SerializeField] private float maxFireRate = 0.05f; // Min 50ms entre disparos
    [SerializeField] private float maxHeadshotAngle = 45f; // Ângulo máximo para headshot

    [Header("Debug")]
    [SerializeField] private bool showDebug = false;

    // Identidade
    private int clientId = -1;
    private bool isLocalPlayer = false;
    private bool isInitialized = false;

    // Anti-Cheat
    private float lastFireTime = 0f;
    private int shotsThisSecond = 0;
    private float shotCounterResetTime = 0f;
    private const int MAX_SHOTS_PER_SECOND = 30;

    // Componentes
    private PlayerHealth playerHealth;
    private NetworkPlayer networkPlayer;

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        networkPlayer = GetComponent<NetworkPlayer>();
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

        Debug.Log($"[NetworkCombat] Initialized (ID: {clientId}, Local: {isLocalPlayer})");
    }

    #region CLIENT SIDE

    /// <summary>
    /// Cliente dispara arma
    /// </summary>
    public void FireWeapon(Vector3 origin, Vector3 direction, int weaponId, float damage)
    {
        if (!isLocalPlayer || !isInitialized) return;

        // Anti-cheat: rate limit
        float timeSinceLastShot = Time.time - lastFireTime;
        if (timeSinceLastShot < maxFireRate)
        {
            if (showDebug)
                Debug.LogWarning("[NetworkCombat] Fire rate too high, ignoring shot");
            return;
        }

        // Anti-cheat: shots per second
        if (Time.time > shotCounterResetTime)
        {
            shotsThisSecond = 0;
            shotCounterResetTime = Time.time + 1f;
        }

        shotsThisSecond++;
        if (shotsThisSecond > MAX_SHOTS_PER_SECOND)
        {
            Debug.LogWarning("[NetworkCombat] Too many shots per second!");
            return;
        }

        lastFireTime = Time.time;

        // Cliente faz raycast local para feedback instantâneo
        RaycastHit clientHit;
        bool hitSomething = Physics.Raycast(origin, direction, out clientHit, maxRaycastDistance);

        if (hitSomething && showDebug)
        {
            Debug.Log($"[NetworkCombat] Client hit: {clientHit.collider.name} at {clientHit.point}");
            Debug.DrawLine(origin, clientHit.point, Color.red, 1f);
        }

        // Envia disparo para servidor validar
        SendWeaponFireToServer(origin, direction, weaponId, damage);

        // TODO: Efeitos visuais locais (tracer, muzzle flash)
        // TODO: Som do disparo
    }

    /// <summary>
    /// Envia disparo para o servidor
    /// </summary>
    private void SendWeaponFireToServer(Vector3 origin, Vector3 direction, int weaponId, float damage)
    {
        NetworkMessage message = new NetworkMessage
        {
            type = MessageType.WeaponFire,
            clientId = clientId
        };

        // Serializa dados do disparo
        using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
        using (System.IO.BinaryWriter writer = new System.IO.BinaryWriter(ms))
        {
            // Origin
            writer.Write(origin.x);
            writer.Write(origin.y);
            writer.Write(origin.z);

            // Direction
            writer.Write(direction.x);
            writer.Write(direction.y);
            writer.Write(direction.z);

            // Weapon info
            writer.Write(weaponId);
            writer.Write(damage);

            message.data = ms.ToArray();
        }

        NetworkManager.Instance.SendToServer(message, useTCP: false);

        if (showDebug)
            Debug.Log($"[NetworkCombat] Sent weapon fire to server");
    }

    #endregion

    #region SERVER SIDE

    /// <summary>
    /// Servidor processa disparo do cliente
    /// </summary>
    private void ServerProcessWeaponFire(NetworkMessage message)
    {
        if (!NetworkManager.Instance.isServer) return;

        // Desserializa dados
        Vector3 origin;
        Vector3 direction;
        int weaponId;
        float damage;

        using (System.IO.MemoryStream ms = new System.IO.MemoryStream(message.data))
        using (System.IO.BinaryReader reader = new System.IO.BinaryReader(ms))
        {
            origin = new Vector3(
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle()
            );

            direction = new Vector3(
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle()
            );

            weaponId = reader.ReadInt32();
            damage = reader.ReadSingle();
        }

        // VALIDAÇÃO: Posição do disparo deve estar perto da posição do jogador
        float distanceFromPlayer = Vector3.Distance(origin, transform.position);
        if (distanceFromPlayer > 5f)
        {
            Debug.LogWarning($"[NetworkCombat] Player {message.clientId} fired from suspicious position!");
            return;
        }

        // VALIDAÇÃO: Direção deve ser normalizada
        if (Mathf.Abs(direction.magnitude - 1f) > 0.1f)
        {
            Debug.LogWarning($"[NetworkCombat] Player {message.clientId} sent invalid direction!");
            return;
        }

        // Servidor faz raycast autoritativo
        RaycastHit serverHit;
        bool hitSomething = Physics.Raycast(origin, direction, out serverHit, maxRaycastDistance);

        if (hitSomething)
        {
            if (showDebug)
            {
                Debug.Log($"[NetworkCombat] Server validated hit on {serverHit.collider.name}");
                Debug.DrawLine(origin, serverHit.point, Color.green, 2f);
            }

            // Verifica se acertou um jogador
            NetworkPlayer hitPlayer = serverHit.collider.GetComponentInParent<NetworkPlayer>();
            if (hitPlayer != null)
            {
                // Aplica dano
                ApplyDamageToPlayer(
                    hitPlayer.clientId,
                    message.clientId,
                    damage,
                    serverHit.point,
                    serverHit.normal,
                    DamageType.Ballistic,
                    serverHit.collider.name
                );
            }
            else
            {
                // Acertou objeto do mundo (parede, recurso, etc)
                ProcessWorldHit(serverHit, message.clientId);
            }

            // Envia confirmação do hit para todos os clientes (para efeitos visuais)
            BroadcastHitConfirmation(message.clientId, serverHit.point, serverHit.normal);
        }
        else
        {
            if (showDebug)
                Debug.Log("[NetworkCombat] Server validated shot - no hit");
        }
    }

    /// <summary>
    /// Aplica dano a um jogador (apenas servidor)
    /// </summary>
    private void ApplyDamageToPlayer(
        int targetId, 
        int attackerId, 
        float damage, 
        Vector3 hitPoint, 
        Vector3 hitNormal,
        DamageType damageType,
        string hitBone)
    {
        if (!NetworkManager.Instance.isServer) return;

        // VALIDAÇÃO: Verifica multiplicador de dano por parte do corpo
        float damageMultiplier = GetBodyPartMultiplier(hitBone);
        float finalDamage = damage * damageMultiplier;

        if (showDebug)
        {
            Debug.Log($"[NetworkCombat] Player {attackerId} hit player {targetId} on {hitBone} for {finalDamage} damage");
        }

        // Cria dados de combate
        CombatData combatData = new CombatData
        {
            attackerId = attackerId,
            targetId = targetId,
            damage = finalDamage,
            hitPosition = hitPoint,
            hitNormal = hitNormal,
            damageType = damageType
        };

        // Envia dano para o jogador alvo
        NetworkMessage damageMessage = new NetworkMessage
        {
            type = MessageType.ApplyDamage,
            clientId = targetId,
            data = combatData.Serialize()
        };

        NetworkManager.Instance.SendToAllClients(damageMessage);

        // Aplica dano localmente no servidor
        // (Será implementado quando criarmos PlayerHealth)
        // playerHealth.TakeDamage(finalDamage, attackerId, damageType);
    }

    /// <summary>
    /// Processa hit em objeto do mundo
    /// </summary>
    private void ProcessWorldHit(RaycastHit hit, int shooterId)
    {
        // Verifica se acertou recurso
        // (Será implementado quando criarmos sistema de recursos)

        // Verifica se acertou construção
        // (Será implementado quando criarmos sistema de building)

        if (showDebug)
            Debug.Log($"[NetworkCombat] World object hit: {hit.collider.name}");
    }

    /// <summary>
    /// Envia confirmação de hit para todos
    /// </summary>
    private void BroadcastHitConfirmation(int shooterId, Vector3 hitPoint, Vector3 hitNormal)
    {
        NetworkMessage message = new NetworkMessage
        {
            type = MessageType.PlayerHit,
            clientId = shooterId
        };

        using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
        using (System.IO.BinaryWriter writer = new System.IO.BinaryWriter(ms))
        {
            writer.Write(hitPoint.x);
            writer.Write(hitPoint.y);
            writer.Write(hitPoint.z);
            writer.Write(hitNormal.x);
            writer.Write(hitNormal.y);
            writer.Write(hitNormal.z);

            message.data = ms.ToArray();
        }

        NetworkManager.Instance.SendToAllClients(message, useTCP: false);
    }

    /// <summary>
    /// Retorna multiplicador de dano por parte do corpo
    /// </summary>
    private float GetBodyPartMultiplier(string boneName)
    {
        // Head = 2x
        if (boneName.ToLower().Contains("head"))
            return 2.0f;

        // Chest = 1x
        if (boneName.ToLower().Contains("spine") || boneName.ToLower().Contains("chest"))
            return 1.0f;

        // Arms/Legs = 0.75x
        return 0.75f;
    }

    #endregion

    #region RECEIVE DAMAGE

    /// <summary>
    /// Recebe dano de outro jogador
    /// </summary>
    private void OnDamageReceived(CombatData combatData)
    {
        if (combatData.targetId != clientId) return;

        if (showDebug)
        {
            Debug.Log($"[NetworkCombat] Received {combatData.damage} damage from player {combatData.attackerId}");
        }

        // Aplica dano localmente
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(combatData.damage, combatData.attackerId, combatData.damageType);
        }

        // TODO: Efeitos visuais de hit (sangue, tela vermelha)
        // TODO: Som de hit
        // TODO: Hit marker para o atacante
    }

    /// <summary>
    /// Recebe confirmação de hit (para efeitos visuais)
    /// </summary>
    private void OnHitConfirmation(NetworkMessage message)
    {
        if (message.clientId != clientId) return;

        using (System.IO.MemoryStream ms = new System.IO.MemoryStream(message.data))
        using (System.IO.BinaryReader reader = new System.IO.BinaryReader(ms))
        {
            Vector3 hitPoint = new Vector3(
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle()
            );

            Vector3 hitNormal = new Vector3(
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle()
            );

            // TODO: Spawnar efeito de impacto
            // TODO: Mostrar hit marker
            // TODO: Som de hit

            if (showDebug)
                Debug.Log($"[NetworkCombat] Hit confirmed at {hitPoint}");
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
            case MessageType.WeaponFire:
                ServerProcessWeaponFire(message);
                break;

            case MessageType.ApplyDamage:
                CombatData combatData = CombatData.Deserialize(message.data);
                OnDamageReceived(combatData);
                break;

            case MessageType.PlayerHit:
                OnHitConfirmation(message);
                break;
        }
    }

    #endregion

    #region PUBLIC METHODS

    /// <summary>
    /// Disparo de arma genérico (chamado pelo WeaponBase)
    /// </summary>
    public void Fire(Vector3 origin, Vector3 direction, int weaponId, float damage)
    {
        FireWeapon(origin, direction, weaponId, damage);
    }

    /// <summary>
    /// Ataque melee
    /// </summary>
    public void MeleeAttack(Vector3 origin, Vector3 direction, float damage, float range)
    {
        if (!isLocalPlayer) return;

        // Similar ao FireWeapon, mas com range limitado
        RaycastHit hit;
        if (Physics.Raycast(origin, direction, out hit, range))
        {
            SendWeaponFireToServer(origin, direction, -1, damage); // -1 = melee
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