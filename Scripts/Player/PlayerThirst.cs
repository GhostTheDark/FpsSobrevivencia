using UnityEngine;

/// <summary>
/// Sistema de sede do jogador
/// Diminui mais rápido que fome, causa dano quando chega a 0
/// </summary>
public class PlayerThirst : MonoBehaviour
{
    [Header("Thirst Settings")]
    [SerializeField] private float maxThirst = 100f;
    [SerializeField] private float currentThirst = 100f;
    [SerializeField] private float thirstDecayRate = 0.8f; // Sede perdida por segundo (mais rápido que fome)
    [SerializeField] private float thirstDecayWhileRunning = 2f; // Sede perdida ao correr
    [SerializeField] private float thirstDecayInHeat = 1.5f; // Multiplier em temperaturas altas

    [Header("Effects")]
    [SerializeField] private float lowThirstThreshold = 30f;
    [SerializeField] private float staminaPenaltyAtLowThirst = 0.5f; // 50% menos stamina regen

    [Header("Debug")]
    [SerializeField] private bool showDebug = false;

    // Estado
    private int clientId = -1;
    private bool isLocalPlayer = false;
    private bool isInitialized = false;
    private bool isLowThirst = false;

    // Componentes
    private PlayerController playerController;
    private PlayerTemperature playerTemperature;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        playerTemperature = GetComponent<PlayerTemperature>();
        currentThirst = maxThirst;
    }

    /// <summary>
    /// Inicializa o componente
    /// </summary>
    public void Initialize(int id, bool isLocal)
    {
        clientId = id;
        isLocalPlayer = isLocal;
        isInitialized = true;

        Debug.Log($"[PlayerThirst] Initialized (ID: {clientId}, Local: {isLocal})");
    }

    private void Update()
    {
        if (!isInitialized) return;

        // Decai sede com o tempo
        DecayThirst();

        // Verifica efeitos de sede baixa
        CheckThirstEffects();
    }

    #region THIRST DECAY

    /// <summary>
    /// Decai sede com o tempo
    /// </summary>
    private void DecayThirst()
    {
        // Taxa base de decay
        float decayAmount = thirstDecayRate * Time.deltaTime;

        // Aumenta decay se estiver correndo
        if (playerController != null && playerController.IsSprinting())
        {
            decayAmount = thirstDecayWhileRunning * Time.deltaTime;
        }

        // Aumenta decay se estiver com calor
        if (playerTemperature != null && playerTemperature.GetTemperature() > 30f)
        {
            decayAmount *= thirstDecayInHeat;
        }

        currentThirst -= decayAmount;
        currentThirst = Mathf.Max(0, currentThirst);

        if (showDebug && currentThirst % 10 < 0.1f)
            Debug.Log($"[PlayerThirst] Thirst: {currentThirst:F1}/{maxThirst:F1}");
    }

    /// <summary>
    /// Verifica efeitos de sede baixa
    /// </summary>
    private void CheckThirstEffects()
    {
        bool wasLowThirst = isLowThirst;
        isLowThirst = currentThirst < lowThirstThreshold;

        // Log quando entra/sai de low thirst
        if (isLowThirst && !wasLowThirst && showDebug)
        {
            Debug.LogWarning("[PlayerThirst] Low thirst! Stamina regeneration reduced.");
        }
        else if (!isLowThirst && wasLowThirst && showDebug)
        {
            Debug.Log("[PlayerThirst] Thirst restored. Stamina regeneration normal.");
        }

        // TODO: Aplicar penalty de stamina regen
    }

    #endregion

    #region DRINKING

    /// <summary>
    /// Bebe água e restaura sede
    /// </summary>
    public void Drink(float amount)
    {
        currentThirst += amount;
        currentThirst = Mathf.Min(currentThirst, maxThirst);

        if (showDebug)
            Debug.Log($"[PlayerThirst] Drank water! Restored {amount:F1} thirst. Current: {currentThirst:F1}/{maxThirst:F1}");
    }

    /// <summary>
    /// Adiciona sede diretamente
    /// </summary>
    public void AddThirst(float amount)
    {
        Drink(amount);
    }

    #endregion

    #region PUBLIC METHODS

    /// <summary>
    /// Reseta sede para o máximo
    /// </summary>
    public void ResetThirst()
    {
        currentThirst = maxThirst;
        isLowThirst = false;
    }

    /// <summary>
    /// Retorna sede atual
    /// </summary>
    public float GetThirst() => currentThirst;

    /// <summary>
    /// Retorna sede máxima
    /// </summary>
    public float GetMaxThirst() => maxThirst;

    /// <summary>
    /// Retorna porcentagem de sede (0-1)
    /// </summary>
    public float GetThirstPercent() => currentThirst / maxThirst;

    /// <summary>
    /// Retorna se está com sede baixa
    /// </summary>
    public bool IsLowThirst() => isLowThirst;

    /// <summary>
    /// Retorna se está morrendo de sede
    /// </summary>
    public bool IsDehydrated() => currentThirst <= 0;

    /// <summary>
    /// Retorna penalty de stamina regen por sede
    /// </summary>
    public float GetStaminaRegenPenalty()
    {
        if (!isLowThirst) return 1f;
        return staminaPenaltyAtLowThirst;
    }

    #endregion

    #region DEBUG

    private void OnGUI()
    {
        if (!showDebug || !isLocalPlayer) return;

        // Ícone/barra de sede
        float iconSize = 30f;
        float x = 20f;
        float y = Screen.height - 60f;

        // Background
        GUI.color = Color.black;
        GUI.Box(new Rect(x - 2, y - 2, iconSize + 4, iconSize + 4), "");

        // Thirst icon
        Color thirstColor = Color.Lerp(Color.red, Color.cyan, GetThirstPercent());
        GUI.color = thirstColor;
        GUI.Box(new Rect(x, y, iconSize, iconSize), "");

        // Texto
        GUI.color = Color.white;
        GUI.Label(new Rect(x + iconSize + 5, y, 100, iconSize), 
            $"Thirst: {currentThirst:F0}%", 
            new GUIStyle { alignment = TextAnchor.MiddleLeft });

        // Warning se low thirst
        if (isLowThirst)
        {
            GUI.color = Color.red;
            GUI.Label(new Rect(x + iconSize + 5, y + 15, 100, 20), 
                "DEHYDRATED!", 
                new GUIStyle { alignment = TextAnchor.MiddleLeft, fontSize = 10 });
        }
    }

    #endregion
}