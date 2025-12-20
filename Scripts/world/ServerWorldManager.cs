using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gerenciador do mundo no servidor
/// Controla tempo, clima, dia/noite, eventos globais
/// APENAS SERVIDOR
/// </summary>
public class ServerWorldManager : MonoBehaviour
{
    public static ServerWorldManager Instance { get; private set; }

    [Header("Time Settings")]
    [SerializeField] private float dayDurationMinutes = 45f; // 45 minutos real = 1 dia in-game
    [SerializeField] private float startTimeOfDay = 12f; // Começa ao meio-dia
    [SerializeField] private bool timeProgresses = true;

    [Header("Day/Night Cycle")]
    [SerializeField] private float sunriseTime = 6f;
    [SerializeField] private float sunsetTime = 18f;
    [SerializeField] private Light directionalLight;
    [SerializeField] private AnimationCurve lightIntensityCurve;
    [SerializeField] private Gradient lightColorGradient;

    [Header("Weather")]
    [SerializeField] private bool enableWeather = true;
    [SerializeField] private float weatherChangeInterval = 300f; // 5 minutos
    [SerializeField] private WeatherType currentWeather = WeatherType.Clear;

    [Header("Airdrops")]
    [SerializeField] private bool enableAirdrops = true;
    [SerializeField] private float airdropInterval = 1800f; // 30 minutos
    [SerializeField] private GameObject airdropPrefab;

    [Header("Events")]
    [SerializeField] private bool enableEvents = true;
    [SerializeField] private WorldEvent[] worldEvents;

    [Header("Performance")]
    [SerializeField] private float entityUpdateRate = 0.1f; // Atualiza entidades a cada 100ms
    [SerializeField] private int maxEntitiesPerFrame = 100;

    [Header("Debug")]
    [SerializeField] private bool showDebug = false;

    // Estado do tempo
    private float currentTimeOfDay = 12f; // 0-24 horas
    private int currentDay = 1;
    private float timeScale = 1f;

    // Timers
    private float nextWeatherChangeTime = 0f;
    private float nextAirdropTime = 0f;
    private float nextEntityUpdateTime = 0f;

    // Entidades ativas
    private List<GameObject> activeEntities = new List<GameObject>();
    private List<GameObject> activeAirdrops = new List<GameObject>();

    // Callbacks
    public System.Action<float> OnTimeChanged; // 0-24 horas
    public System.Action<bool> OnDayNightChanged; // true = dia, false = noite
    public System.Action<WeatherType> OnWeatherChanged;
    public System.Action<int> OnNewDay;

    private void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        currentTimeOfDay = startTimeOfDay;
        
        // Calcula timescale baseado na duração do dia
        timeScale = 24f / (dayDurationMinutes * 60f);
    }

    private void Start()
    {
        // Apenas servidor executa
        if (NetworkManager.Instance == null || !NetworkManager.Instance.isServer)
        {
            enabled = false;
            return;
        }

        InitializeWorld();
    }

    /// <summary>
    /// Inicializa o mundo
    /// </summary>
    private void InitializeWorld()
    {
        Debug.Log("[ServerWorldManager] Initializing world systems...");

        // Gera mundo
        if (WorldGenerator.Instance != null)
        {
            WorldGenerator.Instance.GenerateWorld();
        }

        // Configura luz inicial
        UpdateDayNightCycle();

        // Configura clima inicial
        nextWeatherChangeTime = Time.time + weatherChangeInterval;

        // Configura airdrop
        if (enableAirdrops)
        {
            nextAirdropTime = Time.time + airdropInterval;
        }

        Debug.Log("[ServerWorldManager] World initialized");
    }

    private void Update()
    {
        if (!NetworkManager.Instance.isServer) return;

        // Atualiza tempo
        if (timeProgresses)
        {
            UpdateTime();
        }

        // Atualiza clima
        if (enableWeather && Time.time >= nextWeatherChangeTime)
        {
            ChangeWeather();
        }

        // Spawna airdrop
        if (enableAirdrops && Time.time >= nextAirdropTime)
        {
            SpawnAirdrop();
        }

        // Atualiza entidades
        if (Time.time >= nextEntityUpdateTime)
        {
            UpdateEntities();
            nextEntityUpdateTime = Time.time + entityUpdateRate;
        }
    }

    #region TIME SYSTEM

    /// <summary>
    /// Atualiza tempo do jogo
    /// </summary>
    private void UpdateTime()
    {
        float oldTime = currentTimeOfDay;
        bool wasDay = IsDay();

        // Avança tempo
        currentTimeOfDay += Time.deltaTime * timeScale;

        // Novo dia
        if (currentTimeOfDay >= 24f)
        {
            currentTimeOfDay -= 24f;
            currentDay++;
            OnNewDay?.Invoke(currentDay);

            if (showDebug)
                Debug.Log($"[ServerWorldManager] Day {currentDay} started");
        }

        // Callback de mudança de tempo
        if (Mathf.Abs(currentTimeOfDay - oldTime) > 0.1f)
        {
            OnTimeChanged?.Invoke(currentTimeOfDay);
        }

        // Atualiza ciclo dia/noite
        UpdateDayNightCycle();

        // Callback de mudança dia/noite
        bool isDay = IsDay();
        if (wasDay != isDay)
        {
            OnDayNightChanged?.Invoke(isDay);

            if (showDebug)
                Debug.Log($"[ServerWorldManager] {(isDay ? "Day" : "Night")} time");
        }
    }

    /// <summary>
    /// Atualiza iluminação baseada na hora do dia
    /// </summary>
    private void UpdateDayNightCycle()
    {
        if (directionalLight == null) return;

        // Normaliza tempo para 0-1
        float timeNormalized = currentTimeOfDay / 24f;

        // Rotação do sol
        float sunAngle = (timeNormalized - 0.25f) * 360f;
        directionalLight.transform.rotation = Quaternion.Euler(sunAngle, 0, 0);

        // Intensidade da luz
        if (lightIntensityCurve != null)
        {
            directionalLight.intensity = lightIntensityCurve.Evaluate(timeNormalized);
        }

        // Cor da luz
        if (lightColorGradient != null)
        {
            directionalLight.color = lightColorGradient.Evaluate(timeNormalized);
        }
    }

    /// <summary>
    /// Verifica se é dia
    /// </summary>
    public bool IsDay()
    {
        return currentTimeOfDay >= sunriseTime && currentTimeOfDay < sunsetTime;
    }

    /// <summary>
    /// Verifica se é noite
    /// </summary>
    public bool IsNight()
    {
        return !IsDay();
    }

    /// <summary>
    /// Define hora do dia manualmente
    /// </summary>
    public void SetTimeOfDay(float time)
    {
        currentTimeOfDay = Mathf.Clamp(time, 0f, 24f);
        UpdateDayNightCycle();

        if (showDebug)
            Debug.Log($"[ServerWorldManager] Time set to {currentTimeOfDay:F1}h");
    }

    #endregion

    #region WEATHER SYSTEM

    /// <summary>
    /// Muda clima
    /// </summary>
    private void ChangeWeather()
    {
        WeatherType oldWeather = currentWeather;

        // Escolhe clima baseado em probabilidades
        float roll = Random.value;

        if (roll < 0.6f)
            currentWeather = WeatherType.Clear;
        else if (roll < 0.8f)
            currentWeather = WeatherType.Cloudy;
        else if (roll < 0.9f)
            currentWeather = WeatherType.Rain;
        else if (roll < 0.95f)
            currentWeather = WeatherType.Fog;
        else
            currentWeather = WeatherType.Storm;

        // Neve apenas durante a noite ou em biomas frios
        if (IsNight() && Random.value < 0.1f)
        {
            currentWeather = WeatherType.Snow;
        }

        if (currentWeather != oldWeather)
        {
            OnWeatherChanged?.Invoke(currentWeather);

            if (showDebug)
                Debug.Log($"[ServerWorldManager] Weather changed to {currentWeather}");

            // TODO: Broadcast para clientes
            BroadcastWeatherChange();
        }

        nextWeatherChangeTime = Time.time + weatherChangeInterval;
    }

    /// <summary>
    /// Envia mudança de clima para todos os clientes
    /// </summary>
    private void BroadcastWeatherChange()
    {
        // TODO: Enviar via NetworkManager
        if (showDebug)
            Debug.Log($"[ServerWorldManager] Broadcasting weather: {currentWeather}");
    }

    /// <summary>
    /// Define clima manualmente
    /// </summary>
    public void SetWeather(WeatherType weather)
    {
        currentWeather = weather;
        OnWeatherChanged?.Invoke(currentWeather);
        BroadcastWeatherChange();
    }

    #endregion

    #region AIRDROPS

    /// <summary>
    /// Spawna airdrop no mapa
    /// </summary>
    private void SpawnAirdrop()
    {
        if (airdropPrefab == null) return;

        // Posição aleatória no mapa
        Vector3 spawnPos = GetRandomAirdropPosition();

        GameObject airdrop = Instantiate(airdropPrefab, spawnPos, Quaternion.identity);
        activeAirdrops.Add(airdrop);

        if (showDebug)
            Debug.Log($"[ServerWorldManager] Airdrop spawned at {spawnPos}");

        // TODO: Broadcast para clientes (som de avião, notificação)
        BroadcastAirdropSpawned(spawnPos);

        nextAirdropTime = Time.time + airdropInterval;
    }

    /// <summary>
    /// Retorna posição aleatória para airdrop
    /// </summary>
    private Vector3 GetRandomAirdropPosition()
    {
        if (WorldGenerator.Instance != null)
        {
            int worldSize = WorldGenerator.Instance.GetWorldSize();
            
            float x = Random.Range(-worldSize / 2f + 200, worldSize / 2f - 200);
            float z = Random.Range(-worldSize / 2f + 200, worldSize / 2f - 200);
            float y = 200f; // Altura inicial do airdrop

            return new Vector3(x, y, z);
        }

        return new Vector3(0, 200f, 0);
    }

    /// <summary>
    /// Notifica clientes sobre airdrop
    /// </summary>
    private void BroadcastAirdropSpawned(Vector3 position)
    {
        // TODO: Enviar via NetworkManager
        if (showDebug)
            Debug.Log($"[ServerWorldManager] Broadcasting airdrop at {position}");
    }

    #endregion

    #region ENTITIES

    /// <summary>
    /// Registra entidade no mundo
    /// </summary>
    public void RegisterEntity(GameObject entity)
    {
        if (!activeEntities.Contains(entity))
        {
            activeEntities.Add(entity);
        }
    }

    /// <summary>
    /// Remove entidade do mundo
    /// </summary>
    public void UnregisterEntity(GameObject entity)
    {
        activeEntities.Remove(entity);
    }

    /// <summary>
    /// Atualiza todas as entidades
    /// </summary>
    private void UpdateEntities()
    {
        // Atualiza em lotes para não sobrecarregar
        int entitiesUpdated = 0;

        for (int i = activeEntities.Count - 1; i >= 0 && entitiesUpdated < maxEntitiesPerFrame; i--)
        {
            if (activeEntities[i] == null)
            {
                activeEntities.RemoveAt(i);
                continue;
            }

            // TODO: Lógica de atualização de entidades
            // (verificar distância de jogadores, ativar/desativar, etc)

            entitiesUpdated++;
        }
    }

    #endregion

    #region EVENTS

    /// <summary>
    /// Dispara evento global do mundo
    /// </summary>
    public void TriggerWorldEvent(string eventName)
    {
        if (!enableEvents || worldEvents == null) return;

        foreach (var worldEvent in worldEvents)
        {
            if (worldEvent.eventName == eventName)
            {
                StartWorldEvent(worldEvent);
                break;
            }
        }
    }

    /// <summary>
    /// Inicia evento do mundo
    /// </summary>
    private void StartWorldEvent(WorldEvent worldEvent)
    {
        if (showDebug)
            Debug.Log($"[ServerWorldManager] Starting world event: {worldEvent.eventName}");

        // TODO: Implementar lógica de eventos
        // Exemplos: horda de animais, heli de ataque, eclipse, etc
    }

    #endregion

    #region PUBLIC METHODS

    public float GetTimeOfDay() => currentTimeOfDay;
    public int GetCurrentDay() => currentDay;
    public WeatherType GetCurrentWeather() => currentWeather;
    public int GetActiveEntityCount() => activeEntities.Count;

    /// <summary>
    /// Pausa/despausa progresso do tempo
    /// </summary>
    public void SetTimeProgression(bool enabled)
    {
        timeProgresses = enabled;
        
        if (showDebug)
            Debug.Log($"[ServerWorldManager] Time progression: {enabled}");
    }

    #endregion

    #region DEBUG

    private void OnGUI()
    {
        if (!showDebug) return;
        if (!NetworkManager.Instance.isServer) return;

        float width = 300f;
        float height = 180f;
        float x = Screen.width - width - 20f;
        float y = 20f;

        GUI.color = Color.black;
        GUI.Box(new Rect(x - 2, y - 2, width + 4, height + 4), "");

        GUI.color = Color.white;
        GUILayout.BeginArea(new Rect(x, y, width, height));
        
        GUILayout.Label("=== SERVER WORLD ===", new GUIStyle { alignment = TextAnchor.MiddleCenter });
        GUILayout.Label($"Day: {currentDay}");
        GUILayout.Label($"Time: {currentTimeOfDay:F1}h ({(IsDay() ? "Day" : "Night")})");
        GUILayout.Label($"Weather: {currentWeather}");
        GUILayout.Label($"Active Entities: {activeEntities.Count}");
        GUILayout.Label($"Active Airdrops: {activeAirdrops.Count}");
        GUILayout.Label($"Next Airdrop: {(nextAirdropTime - Time.time):F0}s");
        GUILayout.Label($"Next Weather: {(nextWeatherChangeTime - Time.time):F0}s");

        GUILayout.EndArea();
    }

    /// <summary>
    /// Comandos de debug
    /// </summary>
    [ContextMenu("Set Day")]
    private void DebugSetDay() => SetTimeOfDay(12f);

    [ContextMenu("Set Night")]
    private void DebugSetNight() => SetTimeOfDay(0f);

    [ContextMenu("Spawn Airdrop")]
    private void DebugSpawnAirdrop() => SpawnAirdrop();

    [ContextMenu("Change Weather")]
    private void DebugChangeWeather() => ChangeWeather();

    #endregion
}

/// <summary>
/// Dados de evento do mundo
/// </summary>
[System.Serializable]
public class WorldEvent
{
    public string eventName;
    public string description;
    public float duration = 300f; // 5 minutos
    public GameObject eventPrefab;
    public bool notifyPlayers = true;
}