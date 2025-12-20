using UnityEngine;

/// <summary>
/// Sistema de fome do jogador
/// Diminui com o tempo, causa dano quando chega a 0
/// </summary>
public class PlayerHunger : MonoBehaviour
{
    [Header("Hunger Settings")]
    [SerializeField] private float maxHunger = 100f;
    [SerializeField] private float currentHunger = 100f;
    [SerializeField] private float hungerDecayRate = 0.5f; // Hunger perdida por segundo (normal)
    [SerializeField] private float hungerDecayWhileRunning = 1.5f; // Hunger perdida ao correr

    [Header("Effects")]
    [SerializeField] private float lowHungerThreshold = 30f; // Abaixo disso começa ter efeitos
    [SerializeField] private float speedPenaltyAtLowHunger = 0.7f; // 30% mais lento

    [Header("Debug")]
    [SerializeField] private bool showDebug = false;

    // Estado
    private int clientId = -1;
    private bool isLocalPlayer = false;
    private bool isInitialized = false;
    private bool isLowHunger = false;

    // Componentes
    private PlayerController playerController;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        currentHunger = maxHunger;
    }

    /// <summary>
    /// Inicializa o componente
    /// </summary>
    public void Initialize(int id, bool isLocal)
    {
        clientId = id;
        isLocalPlayer = isLocal;
        isInitialized = true;

        Debug.Log($"[PlayerHunger] Initialized (ID: {clientId}, Local: {isLocal})");
    }

    private void Update()
    {
        if (!isInitialized) return;

        // Decai fome com o tempo
        DecayHunger();

        // Verifica efeitos de fome baixa
        CheckHungerEffects();
    }

    #region HUNGER DECAY

    /// <summary>
    /// Decai fome com o tempo
    /// </summary>
    private void DecayHunger()
    {
        // Taxa base de decay
        float decayAmount = hungerDecayRate * Time.deltaTime;

        // Aumenta decay se estiver correndo
        if (playerController != null && playerController.IsSprinting())
        {
            decayAmount = hungerDecayWhileRunning * Time.deltaTime;
        }

        currentHunger -= decayAmount;
        currentHunger = Mathf.Max(0, currentHunger);

        if (showDebug && currentHunger % 10 < 0.1f)
            Debug.Log($"[PlayerHunger] Hunger: {currentHunger:F1}/{maxHunger:F1}");
    }

    /// <summary>
    /// Verifica efeitos de fome baixa
    /// </summary>
    private void CheckHungerEffects()
    {
        bool wasLowHunger = isLowHunger;
        isLowHunger = currentHunger < lowHungerThreshold;

        // Log quando entra/sai de low hunger
        if (isLowHunger && !wasLowHunger && showDebug)
        {
            Debug.LogWarning("[PlayerHunger] Low hunger! Movement speed reduced.");
        }
        else if (!isLowHunger && wasLowHunger && showDebug)
        {
            Debug.Log("[PlayerHunger] Hunger restored. Movement speed normal.");
        }

        // TODO: Aplicar penalty de velocidade
        // Isso será feito quando criarmos modificadores de stats
    }

    #endregion

    #region EATING

    /// <summary>
    /// Come comida e restaura fome
    /// </summary>
    public void Eat(float amount)
    {
        currentHunger += amount;
        currentHunger = Mathf.Min(currentHunger, maxHunger);

        if (showDebug)
            Debug.Log($"[PlayerHunger] Ate food! Restored {amount:F1} hunger. Current: {currentHunger:F1}/{maxHunger:F1}");
    }

    /// <summary>
    /// Adiciona fome diretamente
    /// </summary>
    public void AddHunger(float amount)
    {
        Eat(amount);
    }

    #endregion

    #region PUBLIC METHODS

    /// <summary>
    /// Reseta fome para o máximo
    /// </summary>
    public void ResetHunger()
    {
        currentHunger = maxHunger;
        isLowHunger = false;
    }

    /// <summary>
    /// Retorna fome atual
    /// </summary>
    public float GetHunger() => currentHunger;

    /// <summary>
    /// Retorna fome máxima
    /// </summary>
    public float GetMaxHunger() => maxHunger;

    /// <summary>
    /// Retorna porcentagem de fome (0-1)
    /// </summary>
    public float GetHungerPercent() => currentHunger / maxHunger;

    /// <summary>
    /// Retorna se está com fome baixa
    /// </summary>
    public bool IsLowHunger() => isLowHunger;

    /// <summary>
    /// Retorna se está morrendo de fome
    /// </summary>
    public bool IsStarving() => currentHunger <= 0;

    /// <summary>
    /// Retorna penalty de velocidade por fome
    /// </summary>
    public float GetSpeedPenalty()
    {
        if (!isLowHunger) return 1f;
        return speedPenaltyAtLowHunger;
    }

    #endregion

    #region DEBUG

    private void OnGUI()
    {
        if (!showDebug || !isLocalPlayer) return;

        // Ícone/barra de fome
        float iconSize = 30f;
        float x = 20f;
        float y = Screen.height - 100f;

        // Background
        GUI.color = Color.black;
        GUI.Box(new Rect(x - 2, y - 2, iconSize + 4, iconSize + 4), "");

        // Hunger icon (apenas cor por enquanto)
        Color hungerColor = Color.Lerp(Color.red, Color.green, GetHungerPercent());
        GUI.color = hungerColor;
        GUI.Box(new Rect(x, y, iconSize, iconSize), "");

        // Texto
        GUI.color = Color.white;
        GUI.Label(new Rect(x + iconSize + 5, y, 100, iconSize), 
            $"Hunger: {currentHunger:F0}%", 
            new GUIStyle { alignment = TextAnchor.MiddleLeft });

        // Warning se low hunger
        if (isLowHunger)
        {
            GUI.color = Color.red;
            GUI.Label(new Rect(x + iconSize + 5, y + 15, 100, 20), 
                "LOW HUNGER!", 
                new GUIStyle { alignment = TextAnchor.MiddleLeft, fontSize = 10 });
        }
    }

    #endregion
}