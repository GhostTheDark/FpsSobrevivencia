using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Zona Segura (Safe Zone)
/// Locais onde PvP é proibido e jogadores não podem atacar uns aos outros
/// Exemplos: Outpost, Bandit Camp
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class SafeZone : MonoBehaviour
{
    [Header("Safe Zone Info")]
    [SerializeField] private string zoneName = "Outpost";
    [SerializeField] private SafeZoneType zoneType = SafeZoneType.Outpost;
    [TextArea(2, 3)]
    [SerializeField] private string welcomeMessage = "Welcome to Outpost. PvP is disabled here.";

    [Header("Rules")]
    [SerializeField] private bool disablePvP = true;
    [SerializeField] private bool disableWeapons = true;
    [SerializeField] private bool kickAggressivePlayers = true;
    [SerializeField] private float aggressiveCooldown = 30f; // Segundos após atacar antes de entrar

    [Header("Services")]
    [SerializeField] private bool hasShop = true;
    [SerializeField] private bool hasRecycler = true;
    [SerializeField] private bool hasWorkbench = true;
    [SerializeField] private bool hasVendingMachines = true;

    [Header("Turrets")]
    [SerializeField] private bool hasTurrets = true;
    [SerializeField] private GameObject[] turrets;
    [SerializeField] private float turretDetectionRange = 50f;

    [Header("Boundaries")]
    [SerializeField] private Vector3 zoneSize = new Vector3(100, 50, 100);

    [Header("Audio")]
    [SerializeField] private AudioClip enterSound;
    [SerializeField] private AudioClip exitSound;
    [SerializeField] private AudioClip warningSound;

    [Header("Debug")]
    [SerializeField] private bool showDebug = false;
    [SerializeField] private bool showGizmos = true;

    // Estado
    private bool isInitialized = false;
    private BoxCollider zoneCollider;
    private Dictionary<int, PlayerSafeZoneData> playersInZone = new Dictionary<int, PlayerSafeZoneData>();

    // Jogadores hostis
    private Dictionary<int, float> hostilePlayers = new Dictionary<int, float>(); // playerId -> time when became hostile

    // Componentes
    private AudioSource audioSource;

    private void Awake()
    {
        zoneCollider = GetComponent<BoxCollider>();
        zoneCollider.isTrigger = true;
        zoneCollider.size = zoneSize;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void Start()
    {
        Initialize();
    }

    /// <summary>
    /// Inicializa safe zone
    /// </summary>
    private void Initialize()
    {
        if (isInitialized) return;

        // Ativa turrets
        if (hasTurrets)
        {
            ActivateTurrets();
        }

        isInitialized = true;

        if (showDebug)
            Debug.Log($"[SafeZone] {zoneName} initialized");
    }

    private void Update()
    {
        if (!isInitialized) return;

        // Monitora jogadores hostis
        CheckHostilePlayers();

        // Atualiza turrets
        if (hasTurrets)
        {
            UpdateTurrets();
        }
    }

    #region PLAYER MANAGEMENT

    private void OnTriggerEnter(Collider other)
    {
        NetworkPlayer player = other.GetComponentInParent<NetworkPlayer>();
        if (player != null)
        {
            TryEnterZone(player);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        NetworkPlayer player = other.GetComponentInParent<NetworkPlayer>();
        if (player != null)
        {
            OnPlayerExitZone(player);
        }
    }

    /// <summary>
    /// Tenta entrar na zona
    /// </summary>
    private void TryEnterZone(NetworkPlayer player)
    {
        int playerId = player.clientId;

        // Verifica se é hostil
        if (IsPlayerHostile(playerId))
        {
            float timeRemaining = GetHostileCooldownRemaining(playerId);
            
            if (showDebug)
                Debug.LogWarning($"[SafeZone] Player {playerId} is hostile, cannot enter! Cooldown: {timeRemaining:F0}s");

            // Ejeta jogador
            EjectPlayer(player);

            // Toca som de aviso
            if (warningSound != null && audioSource != null)
                audioSource.PlayOneShot(warningSound);

            return;
        }

        // Jogador pode entrar
        OnPlayerEnterZone(player);
    }

    /// <summary>
    /// Jogador entra na zona
    /// </summary>
    private void OnPlayerEnterZone(NetworkPlayer player)
    {
        int playerId = player.clientId;

        if (playersInZone.ContainsKey(playerId))
            return;

        PlayerSafeZoneData data = new PlayerSafeZoneData
        {
            player = player.gameObject,
            enterTime = Time.time,
            weaponWasHolstered = false
        };

        playersInZone.Add(playerId, data);

        // Desabilita armas
        if (disableWeapons)
        {
            HolsterPlayerWeapons(player);
        }

        // Mensagem de boas-vindas
        SendWelcomeMessage(playerId);

        // Som de entrada
        if (enterSound != null && audioSource != null)
            audioSource.PlayOneShot(enterSound);

        if (showDebug)
            Debug.Log($"[SafeZone] Player {playerId} entered {zoneName}");
    }

    /// <summary>
    /// Jogador sai da zona
    /// </summary>
    private void OnPlayerExitZone(NetworkPlayer player)
    {
        int playerId = player.clientId;

        if (!playersInZone.TryGetValue(playerId, out PlayerSafeZoneData data))
            return;

        // Reabilita armas
        if (disableWeapons && data.weaponWasHolstered)
        {
            UnholsterPlayerWeapons(player);
        }

        playersInZone.Remove(playerId);

        // Som de saída
        if (exitSound != null && audioSource != null)
            audioSource.PlayOneShot(exitSound);

        if (showDebug)
            Debug.Log($"[SafeZone] Player {playerId} left {zoneName}");
    }

    /// <summary>
    /// Ejeta jogador da zona
    /// </summary>
    private void EjectPlayer(NetworkPlayer player)
    {
        // Empurra jogador para fora
        Vector3 direction = (player.transform.position - transform.position).normalized;
        Vector3 ejectPosition = transform.position + direction * (zoneSize.magnitude + 5f);
        
        // Teleporta para fora
        player.transform.position = ejectPosition;

        if (showDebug)
            Debug.Log($"[SafeZone] Player {player.clientId} ejected from {zoneName}");
    }

    #endregion

    #region HOSTILE PLAYERS

    /// <summary>
    /// Marca jogador como hostil
    /// Chamado quando jogador ataca outro
    /// </summary>
    public void MarkPlayerAsHostile(int playerId)
    {
        if (!hostilePlayers.ContainsKey(playerId))
        {
            hostilePlayers.Add(playerId, Time.time);
        }
        else
        {
            hostilePlayers[playerId] = Time.time;
        }

        if (showDebug)
            Debug.Log($"[SafeZone] Player {playerId} marked as hostile");

        // Se jogador está na zona, ejeta
        if (playersInZone.ContainsKey(playerId) && kickAggressivePlayers)
        {
            PlayerSafeZoneData data = playersInZone[playerId];
            if (data.player != null)
            {
                NetworkPlayer player = data.player.GetComponent<NetworkPlayer>();
                if (player != null)
                {
                    EjectPlayer(player);
                }
            }
        }
    }

    /// <summary>
    /// Verifica se jogador é hostil
    /// </summary>
    private bool IsPlayerHostile(int playerId)
    {
        if (!hostilePlayers.ContainsKey(playerId))
            return false;

        float timeSinceHostile = Time.time - hostilePlayers[playerId];
        return timeSinceHostile < aggressiveCooldown;
    }

    /// <summary>
    /// Retorna tempo restante de cooldown hostil
    /// </summary>
    private float GetHostileCooldownRemaining(int playerId)
    {
        if (!hostilePlayers.ContainsKey(playerId))
            return 0f;

        float timeSinceHostile = Time.time - hostilePlayers[playerId];
        return Mathf.Max(0f, aggressiveCooldown - timeSinceHostile);
    }

    /// <summary>
    /// Limpa jogadores hostis expirados
    /// </summary>
    private void CheckHostilePlayers()
    {
        List<int> toRemove = new List<int>();

        foreach (var kvp in hostilePlayers)
        {
            if (!IsPlayerHostile(kvp.Key))
            {
                toRemove.Add(kvp.Key);
            }
        }

        foreach (int playerId in toRemove)
        {
            hostilePlayers.Remove(playerId);
            
            if (showDebug)
                Debug.Log($"[SafeZone] Player {playerId} is no longer hostile");
        }
    }

    #endregion

    #region WEAPONS

    /// <summary>
    /// Guarda armas do jogador
    /// </summary>
    private void HolsterPlayerWeapons(NetworkPlayer player)
    {
        // TODO: Desabilitar armas do jogador
        // PlayerController controller = player.GetComponent<PlayerController>();
        // if (controller != null)
        // {
        //     controller.HolsterWeapon();
        // }

        if (showDebug)
            Debug.Log($"[SafeZone] Player {player.clientId} weapons holstered");
    }

    /// <summary>
    /// Retorna armas ao jogador
    /// </summary>
    private void UnholsterPlayerWeapons(NetworkPlayer player)
    {
        // TODO: Reabilitar armas
        
        if (showDebug)
            Debug.Log($"[SafeZone] Player {player.clientId} weapons unholstered");
    }

    #endregion

    #region TURRETS

    /// <summary>
    /// Ativa turrets da safe zone
    /// </summary>
    private void ActivateTurrets()
    {
        if (turrets == null || turrets.Length == 0) return;

        foreach (GameObject turret in turrets)
        {
            if (turret != null)
            {
                turret.SetActive(true);
                // TODO: Configurar turret para atacar jogadores hostis
            }
        }

        if (showDebug)
            Debug.Log($"[SafeZone] {turrets.Length} turrets activated");
    }

    /// <summary>
    /// Atualiza turrets para detectar ameaças
    /// </summary>
    private void UpdateTurrets()
    {
        // TODO: Detectar jogadores hostis próximos
        // TODO: Fazer turrets atirarem
    }

    #endregion

    #region MESSAGES

    /// <summary>
    /// Envia mensagem de boas-vindas
    /// </summary>
    private void SendWelcomeMessage(int playerId)
    {
        // TODO: Enviar via chat/UI
        if (showDebug)
            Debug.Log($"[SafeZone] Sent welcome message to player {playerId}: {welcomeMessage}");
    }

    #endregion

    #region PUBLIC METHODS

    public bool IsPvPEnabled() => !disablePvP;
    public bool CanUseWeapons() => !disableWeapons;
    public bool HasService(string serviceName)
    {
        switch (serviceName.ToLower())
        {
            case "shop": return hasShop;
            case "recycler": return hasRecycler;
            case "workbench": return hasWorkbench;
            case "vending": return hasVendingMachines;
            default: return false;
        }
    }

    public int GetPlayersInZoneCount() => playersInZone.Count;
    public string GetZoneName() => zoneName;
    public SafeZoneType GetZoneType() => zoneType;

    #endregion

    #region GIZMOS

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        // Desenha bounds da safe zone
        Gizmos.color = new Color(0, 0, 1, 0.3f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(Vector3.zero, zoneSize);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(Vector3.zero, zoneSize);

        #if UNITY_EDITOR
        // Label
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * (zoneSize.y / 2f + 5f),
            $"🛡️ {zoneName}\nSafe Zone",
            new GUIStyle
            {
                alignment = TextAnchor.MiddleCenter,
                normal = new GUIStyleState { textColor = Color.cyan },
                fontSize = 14
            }
        );
        #endif
    }

    #endregion
}

/// <summary>
/// Tipos de safe zone
/// </summary>
public enum SafeZoneType
{
    Outpost,        // Outpost (loja, recycler)
    BanditCamp,     // Bandit Camp (black market)
    SafeHouse,      // Casa segura
    TradePost,      // Posto de troca
    Custom          // Customizado
}

/// <summary>
/// Dados de jogador na safe zone
/// </summary>
public class PlayerSafeZoneData
{
    public GameObject player;
    public float enterTime;
    public bool weaponWasHolstered;
}