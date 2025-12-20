using UnityEngine;

/// <summary>
/// Sistema de temperatura do jogador
/// Afetado por ambiente, roupas e clima
/// Causa dano em temperaturas extremas
/// </summary>
public class PlayerTemperature : MonoBehaviour
{
    [Header("Temperature Settings")]
    [SerializeField] private float currentTemperature = 20f; // Celsius
    [SerializeField] private float idealTemperature = 20f; // Temperatura ideal
    [SerializeField] private float temperatureChangeRate = 2f; // Graus por segundo

    [Header("Temperature Zones")]
    [SerializeField] private float coldThreshold = 0f; // Abaixo disso = frio
    [SerializeField] private float hotThreshold = 35f; // Acima disso = calor
    [SerializeField] private float extremeColdThreshold = -10f; // Congelando
    [SerializeField] private float extremeHotThreshold = 45f; // Superaquecimento

    [Header("Environmental")]
    [SerializeField] private float nightTemperatureDrop = -5f; // Temperatura à noite
    [SerializeField] private float indoorTemperatureBonus = 10f; // Bonus dentro de construções

    [Header("Clothing")]
    [SerializeField] private float clothingInsulation = 0f; // Isolamento das roupas (0-100)

    [Header("Effects")]
    [SerializeField] private bool isCold = false;
    [SerializeField] private bool isHot = false;

    [Header("Debug")]
    [SerializeField] private bool showDebug = false;

    // Estado
    private int clientId = -1;
    private bool isLocalPlayer = false;
    private bool isInitialized = false;

    // Ambiente
    private float environmentalTemperature = 20f;
    private bool isIndoors = false;
    private bool isNight = false;

    private void Awake()
    {
        currentTemperature = idealTemperature;
    }

    /// <summary>
    /// Inicializa o componente
    /// </summary>
    public void Initialize(int id, bool isLocal)
    {
        clientId = id;
        isLocalPlayer = isLocal;
        isInitialized = true;

        Debug.Log($"[PlayerTemperature] Initialized (ID: {clientId}, Local: {isLocal})");
    }

    private void Update()
    {
        if (!isInitialized) return;

        // Calcula temperatura ambiente
        CalculateEnvironmentalTemperature();

        // Atualiza temperatura do jogador
        UpdateTemperature();

        // Verifica efeitos
        CheckTemperatureEffects();
    }

    #region TEMPERATURE CALCULATION

    /// <summary>
    /// Calcula temperatura ambiente baseada em fatores
    /// </summary>
    private void CalculateEnvironmentalTemperature()
    {
        // Começa com temperatura base do bioma (será implementado)
        environmentalTemperature = 20f; // Default

        // Modifica por hora do dia
        if (isNight)
        {
            environmentalTemperature += nightTemperatureDrop;
        }

        // Bonus se estiver dentro de construção
        if (isIndoors)
        {
            environmentalTemperature += indoorTemperatureBonus;
        }

        // TODO: Considerar bioma (deserto = quente, neve = frio)
        // TODO: Considerar clima (chuva = mais frio)
        // TODO: Considerar altitude
    }

    /// <summary>
    /// Atualiza temperatura do jogador
    /// </summary>
    private void UpdateTemperature()
    {
        // Temperatura alvo baseada no ambiente e roupas
        float targetTemperature = CalculateTargetTemperature();

        // Interpola temperatura atual em direção ao alvo
        currentTemperature = Mathf.MoveTowards(
            currentTemperature,
            targetTemperature,
            temperatureChangeRate * Time.deltaTime
        );

        if (showDebug && Time.frameCount % 100 == 0)
            Debug.Log($"[PlayerTemperature] Current: {currentTemperature:F1}°C, Target: {targetTemperature:F1}°C");
    }

    /// <summary>
    /// Calcula temperatura alvo baseada em isolamento de roupas
    /// </summary>
    private float CalculateTargetTemperature()
    {
        // Sem roupas: temperatura do jogador = temperatura ambiente
        // Com roupas: temperatura do jogador se mantém mais próxima do ideal

        float insulation = clothingInsulation / 100f; // 0-1

        // Interpola entre temperatura ambiente e temperatura ideal baseado em isolamento
        return Mathf.Lerp(environmentalTemperature, idealTemperature, insulation);
    }

    #endregion

    #region TEMPERATURE EFFECTS

    /// <summary>
    /// Verifica efeitos da temperatura
    /// </summary>
    private void CheckTemperatureEffects()
    {
        bool wasCold = isCold;
        bool wasHot = isHot;

        // Verifica frio
        isCold = currentTemperature < coldThreshold;

        // Verifica calor
        isHot = currentTemperature > hotThreshold;

        // Logs de mudança de estado
        if (isCold && !wasCold && showDebug)
            Debug.LogWarning($"[PlayerTemperature] Player is cold! ({currentTemperature:F1}°C)");

        if (isHot && !wasHot && showDebug)
            Debug.LogWarning($"[PlayerTemperature] Player is hot! ({currentTemperature:F1}°C)");

        if (!isCold && wasCold && showDebug)
            Debug.Log("[PlayerTemperature] Player warmed up");

        if (!isHot && wasHot && showDebug)
            Debug.Log("[PlayerTemperature] Player cooled down");

        // Efeitos visuais (apenas local)
        if (isLocalPlayer)
        {
            ApplyVisualEffects();
        }
    }

    /// <summary>
    /// Aplica efeitos visuais de temperatura
    /// </summary>
    private void ApplyVisualEffects()
    {
        // TODO: Efeito de breath (vapor) quando frio
        // TODO: Efeito de suor quando quente
        // TODO: Tremor de câmera quando congelando
        // TODO: Distorção de calor quando superaquecendo
    }

    #endregion

    #region ENVIRONMENTAL DETECTION

    /// <summary>
    /// Detecta se está dentro de construção (chamado por trigger)
    /// </summary>
    public void SetIndoors(bool indoors)
    {
        isIndoors = indoors;

        if (showDebug)
            Debug.Log($"[PlayerTemperature] Indoors: {isIndoors}");
    }

    /// <summary>
    /// Define se é noite (chamado por sistema de tempo)
    /// </summary>
    public void SetNightTime(bool night)
    {
        isNight = night;
    }

    #endregion

    #region CLOTHING

    /// <summary>
    /// Define isolamento de roupas
    /// </summary>
    public void SetClothingInsulation(float insulation)
    {
        clothingInsulation = Mathf.Clamp(insulation, 0f, 100f);

        if (showDebug)
            Debug.Log($"[PlayerTemperature] Clothing insulation: {clothingInsulation:F0}%");
    }

    /// <summary>
    /// Adiciona isolamento (ao equipar roupa)
    /// </summary>
    public void AddClothingInsulation(float amount)
    {
        SetClothingInsulation(clothingInsulation + amount);
    }

    /// <summary>
    /// Remove isolamento (ao desequipar roupa)
    /// </summary>
    public void RemoveClothingInsulation(float amount)
    {
        SetClothingInsulation(clothingInsulation - amount);
    }

    #endregion

    #region PUBLIC METHODS

    /// <summary>
    /// Reseta temperatura para ideal
    /// </summary>
    public void ResetTemperature()
    {
        currentTemperature = idealTemperature;
        isCold = false;
        isHot = false;
    }

    /// <summary>
    /// Retorna temperatura atual
    /// </summary>
    public float GetTemperature() => currentTemperature;

    /// <summary>
    /// Retorna temperatura ambiente
    /// </summary>
    public float GetEnvironmentalTemperature() => environmentalTemperature;

    /// <summary>
    /// Retorna se está com frio
    /// </summary>
    public bool IsCold() => isCold;

    /// <summary>
    /// Retorna se está com calor
    /// </summary>
    public bool IsHot() => isHot;

    /// <summary>
    /// Retorna se está congelando (extremo)
    /// </summary>
    public bool IsFreezing() => currentTemperature < extremeColdThreshold;

    /// <summary>
    /// Retorna se está superaquecendo (extremo)
    /// </summary>
    public bool IsOverheating() => currentTemperature > extremeHotThreshold;

    /// <summary>
    /// Retorna se está em temperatura confortável
    /// </summary>
    public bool IsComfortable() => !isCold && !isHot;

    /// <summary>
    /// Retorna isolamento atual
    /// </summary>
    public float GetClothingInsulation() => clothingInsulation;

    #endregion

    #region DEBUG

    private void OnGUI()
    {
        if (!showDebug || !isLocalPlayer) return;

        // Display de temperatura
        float boxWidth = 200f;
        float boxHeight = 80f;
        float x = Screen.width - boxWidth - 20f;
        float y = 20f;

        GUI.color = Color.black;
        GUI.Box(new Rect(x - 2, y - 2, boxWidth + 4, boxHeight + 4), "");

        GUI.color = Color.white;
        GUILayout.BeginArea(new Rect(x, y, boxWidth, boxHeight));
        GUILayout.Label("=== TEMPERATURE ===", new GUIStyle { alignment = TextAnchor.MiddleCenter });
        GUILayout.Label($"Current: {currentTemperature:F1}°C");
        GUILayout.Label($"Environment: {environmentalTemperature:F1}°C");
        GUILayout.Label($"Insulation: {clothingInsulation:F0}%");

        // Status
        if (IsFreezing())
        {
            GUI.color = Color.cyan;
            GUILayout.Label("FREEZING!");
        }
        else if (isCold)
        {
            GUI.color = Color.blue;
            GUILayout.Label("Cold");
        }
        else if (IsOverheating())
        {
            GUI.color = Color.red;
            GUILayout.Label("OVERHEATING!");
        }
        else if (isHot)
        {
            GUI.color = Color.yellow;
            GUILayout.Label("Hot");
        }
        else
        {
            GUI.color = Color.green;
            GUILayout.Label("Comfortable");
        }

        GUILayout.EndArea();
    }

    #endregion
}