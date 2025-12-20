using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Zona de radiação
/// Causa dano contínuo a jogadores sem proteção adequada
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class RadiationZone : MonoBehaviour
{
    [Header("Radiation Settings")]
    [SerializeField] private float radiationLevel = 10f; // Rads por segundo
    [SerializeField] private float radius = 50f;
    [SerializeField] private bool isActive = true;

    [Header("Effects")]
    [SerializeField] private GameObject radiationParticlesPrefab;
    [SerializeField] private Color radiationColor = new Color(0, 1, 0, 0.3f);
    [SerializeField] private AudioClip geigerCounterSound;

    [Header("Debug")]
    [SerializeField] private bool showDebug = false;
    [SerializeField] private bool showGizmos = true;

    // Estado
    private bool isInitialized = false;
    private SphereCollider triggerCollider;
    private Dictionary<int, PlayerRadiationData> playersInZone = new Dictionary<int, PlayerRadiationData>();

    // Efeitos
    private GameObject particleEffect;
    private AudioSource audioSource;

    private void Awake()
    {
        triggerCollider = GetComponent<SphereCollider>();
        triggerCollider.isTrigger = true;
        triggerCollider.radius = radius;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    /// <summary>
    /// Inicializa zona de radiação
    /// </summary>
    public void Initialize(float radLevel, float radRadius)
    {
        if (isInitialized) return;

        radiationLevel = radLevel;
        radius = radRadius;

        if (triggerCollider != null)
            triggerCollider.radius = radius;

        // Spawna efeitos
        SpawnEffects();

        isInitialized = true;

        if (showDebug)
            Debug.Log($"[RadiationZone] Initialized: {radiationLevel} rads/s, {radius}m radius");
    }

    private void Start()
    {
        if (!isInitialized)
            Initialize(radiationLevel, radius);
    }

    private void Update()
    {
        if (!isActive || !isInitialized) return;

        // Aplica radiação a todos os jogadores na zona
        ApplyRadiationToPlayers();
    }

    #region RADIATION

    /// <summary>
    /// Aplica radiação a jogadores na zona
    /// </summary>
    private void ApplyRadiationToPlayers()
    {
        if (playersInZone.Count == 0) return;

        // Lista para remover jogadores que saíram
        List<int> playersToRemove = new List<int>();

        foreach (var kvp in playersInZone)
        {
            int playerId = kvp.Key;
            PlayerRadiationData data = kvp.Value;

            // Verifica se jogador ainda existe
            if (data.player == null)
            {
                playersToRemove.Add(playerId);
                continue;
            }

            // Calcula radiação efetiva baseada em proteção
            float effectiveRadiation = CalculateEffectiveRadiation(data);

            // Aplica dano
            if (effectiveRadiation > 0)
            {
                PlayerHealth health = data.player.GetComponent<PlayerHealth>();
                if (health != null)
                {
                    health.TakeDamage(effectiveRadiation * Time.deltaTime, -1, DamageType.Radiation);
                }
            }

            // Atualiza tempo na zona
            data.timeInZone += Time.deltaTime;

            if (showDebug && Time.frameCount % 60 == 0)
            {
                Debug.Log($"[RadiationZone] Player {playerId} taking {effectiveRadiation:F1} rads/s " +
                         $"(Protection: {data.radiationProtection * 100f:F0}%)");
            }
        }

        // Remove jogadores que saíram
        foreach (int playerId in playersToRemove)
        {
            playersInZone.Remove(playerId);
        }
    }

    /// <summary>
    /// Calcula radiação efetiva considerando proteção do jogador
    /// </summary>
    private float CalculateEffectiveRadiation(PlayerRadiationData data)
    {
        // Reduz radiação baseado em proteção (0-1)
        float effectiveRads = radiationLevel * (1f - data.radiationProtection);
        
        return Mathf.Max(0f, effectiveRads);
    }

    /// <summary>
    /// Calcula proteção contra radiação do jogador
    /// </summary>
    private float CalculateRadiationProtection(GameObject player)
    {
        float totalProtection = 0f;

        // TODO: Verificar equipamento do jogador
        // InventorySystem inventory = player.GetComponent<InventorySystem>();
        // if (inventory != null)
        // {
        //     // Verifica cada peça de roupa
        //     // Soma proteção de cada item
        // }

        // Por enquanto, retorna 0 (sem proteção)
        return Mathf.Clamp01(totalProtection);
    }

    #endregion

    #region TRIGGERS

    private void OnTriggerEnter(Collider other)
    {
        if (!isActive) return;

        // Verifica se é jogador
        NetworkPlayer player = other.GetComponentInParent<NetworkPlayer>();
        if (player != null)
        {
            OnPlayerEnter(player);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!isActive) return;

        // Verifica se é jogador
        NetworkPlayer player = other.GetComponentInParent<NetworkPlayer>();
        if (player != null)
        {
            OnPlayerExit(player);
        }
    }

    /// <summary>
    /// Jogador entrou na zona
    /// </summary>
    private void OnPlayerEnter(NetworkPlayer player)
    {
        if (playersInZone.ContainsKey(player.clientId))
            return;

        PlayerRadiationData data = new PlayerRadiationData
        {
            player = player.gameObject,
            enterTime = Time.time,
            timeInZone = 0f,
            radiationProtection = CalculateRadiationProtection(player.gameObject)
        };

        playersInZone.Add(player.clientId, data);

        if (showDebug)
            Debug.Log($"[RadiationZone] Player {player.clientId} entered radiation zone");

        // TODO: Efeitos visuais/sonoros para o jogador
        // Som de geiger counter
        // Overlay de tela verde
    }

    /// <summary>
    /// Jogador saiu da zona
    /// </summary>
    private void OnPlayerExit(NetworkPlayer player)
    {
        if (playersInZone.Remove(player.clientId))
        {
            if (showDebug)
                Debug.Log($"[RadiationZone] Player {player.clientId} left radiation zone");

            // TODO: Remove efeitos visuais/sonoros
        }
    }

    #endregion

    #region EFFECTS

    /// <summary>
    /// Spawna efeitos visuais e sonoros
    /// </summary>
    private void SpawnEffects()
    {
        // Partículas de radiação
        if (radiationParticlesPrefab != null)
        {
            particleEffect = Instantiate(radiationParticlesPrefab, transform);
            
            // Ajusta scale baseado no raio
            float scale = radius / 25f; // Normaliza para raio de 25m
            particleEffect.transform.localScale = Vector3.one * scale;
        }

        // Som de geiger counter
        if (geigerCounterSound != null && audioSource != null)
        {
            audioSource.clip = geigerCounterSound;
            audioSource.loop = true;
            audioSource.spatialBlend = 1f;
            audioSource.maxDistance = radius;
            audioSource.Play();
        }
    }

    #endregion

    #region PUBLIC METHODS

    /// <summary>
    /// Ativa/desativa zona
    /// </summary>
    public void SetActive(bool active)
    {
        isActive = active;

        if (particleEffect != null)
            particleEffect.SetActive(active);

        if (audioSource != null)
        {
            if (active)
                audioSource.Play();
            else
                audioSource.Stop();
        }

        if (showDebug)
            Debug.Log($"[RadiationZone] Active: {active}");
    }

    /// <summary>
    /// Define nível de radiação
    /// </summary>
    public void SetRadiationLevel(float level)
    {
        radiationLevel = level;
        
        if (showDebug)
            Debug.Log($"[RadiationZone] Radiation level set to {level} rads/s");
    }

    /// <summary>
    /// Define raio da zona
    /// </summary>
    public void SetRadius(float newRadius)
    {
        radius = newRadius;

        if (triggerCollider != null)
            triggerCollider.radius = radius;

        if (showDebug)
            Debug.Log($"[RadiationZone] Radius set to {radius}m");
    }

    public float GetRadiationLevel() => radiationLevel;
    public float GetRadius() => radius;
    public bool IsActive() => isActive;
    public int GetPlayersInZoneCount() => playersInZone.Count;

    #endregion

    #region GIZMOS

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Gizmos.color = radiationColor;
        Gizmos.DrawWireSphere(transform.position, radius);

        // Desenha área com cor mais opaca
        Gizmos.color = new Color(radiationColor.r, radiationColor.g, radiationColor.b, 0.1f);
        Gizmos.DrawSphere(transform.position, radius);

        #if UNITY_EDITOR
        // Label
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * radius,
            $"☢ Radiation\n{radiationLevel} rads/s",
            new GUIStyle
            {
                alignment = TextAnchor.MiddleCenter,
                normal = new GUIStyleState { textColor = Color.green }
            }
        );
        #endif
    }

    #endregion
}

/// <summary>
/// Dados de jogador na zona de radiação
/// </summary>
public class PlayerRadiationData
{
    public GameObject player;
    public float enterTime;
    public float timeInZone;
    public float radiationProtection; // 0-1
}