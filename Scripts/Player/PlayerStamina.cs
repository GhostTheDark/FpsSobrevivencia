using UnityEngine;

/// <summary>
/// Sistema de stamina do jogador
/// Usado para correr, pular e outras ações
/// </summary>
public class PlayerStamina : MonoBehaviour
{
    [Header("Stamina Settings")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float currentStamina = 100f;
    [SerializeField] private float staminaRegenRate = 15f; // Stamina por segundo
    [SerializeField] private float staminaRegenDelay = 1f; // Segundos sem usar para começar regenerar

    [Header("Costs")]
    [SerializeField] private float sprintCostPerSecond = 10f;
    [SerializeField] private float jumpCost = 10f;

    [Header("Debug")]
    [SerializeField] private bool showDebug = false;

    // Estado
    private int clientId = -1;
    private bool isLocalPlayer = false;
    private bool isInitialized = false;

    // Regeneração
    private float timeSinceLastUse = 0f;
    private bool isRegenerating = false;
    private bool isUsingStamina = false;

    private void Awake()
    {
        currentStamina = maxStamina;
    }

    /// <summary>
    /// Inicializa o componente
    /// </summary>
    public void Initialize(int id, bool isLocal)
    {
        clientId = id;
        isLocalPlayer = isLocal;
        isInitialized = true;

        Debug.Log($"[PlayerStamina] Initialized (ID: {clientId}, Local: {isLocal})");
    }

    private void Update()
    {
        if (!isInitialized) return;

        // Regeneração de stamina
        UpdateStaminaRegen();
    }

    #region STAMINA USAGE

    /// <summary>
    /// Usa stamina
    /// </summary>
    public bool UseStamina(float amount)
    {
        if (currentStamina < amount)
            return false;

        currentStamina -= amount;
        currentStamina = Mathf.Max(0, currentStamina);

        // Reseta timer de regeneração
        timeSinceLastUse = 0f;
        isRegenerating = false;
        isUsingStamina = true;

        if (showDebug)
            Debug.Log($"[PlayerStamina] Used {amount:F1} stamina. Current: {currentStamina:F1}/{maxStamina:F1}");

        return true;
    }

    /// <summary>
    /// Verifica se tem stamina suficiente
    /// </summary>
    public bool HasStamina(float amount = 1f)
    {
        return currentStamina >= amount;
    }

    /// <summary>
    /// Atualiza regeneração de stamina
    /// </summary>
    private void UpdateStaminaRegen()
    {
        if (currentStamina >= maxStamina)
        {
            isRegenerating = false;
            return;
        }

        timeSinceLastUse += Time.deltaTime;

        // Começa a regenerar após delay
        if (timeSinceLastUse >= staminaRegenDelay)
        {
            if (!isRegenerating)
            {
                isRegenerating = true;
                if (showDebug)
                    Debug.Log("[PlayerStamina] Started stamina regeneration");
            }

            AddStamina(staminaRegenRate * Time.deltaTime);
        }

        isUsingStamina = false;
    }

    /// <summary>
    /// Adiciona stamina
    /// </summary>
    public void AddStamina(float amount)
    {
        currentStamina += amount;
        currentStamina = Mathf.Min(currentStamina, maxStamina);
    }

    #endregion

    #region PUBLIC METHODS

    /// <summary>
    /// Reseta stamina para o máximo
    /// </summary>
    public void ResetStamina()
    {
        currentStamina = maxStamina;
        timeSinceLastUse = 0f;
        isRegenerating = false;
    }

    /// <summary>
    /// Retorna stamina atual
    /// </summary>
    public float GetStamina() => currentStamina;

    /// <summary>
    /// Retorna stamina máxima
    /// </summary>
    public float GetMaxStamina() => maxStamina;

    /// <summary>
    /// Retorna porcentagem de stamina (0-1)
    /// </summary>
    public float GetStaminaPercent() => currentStamina / maxStamina;

    /// <summary>
    /// Retorna se está usando stamina
    /// </summary>
    public bool IsUsingStamina() => isUsingStamina;

    /// <summary>
    /// Retorna se está regenerando
    /// </summary>
    public bool IsRegenerating() => isRegenerating;

    /// <summary>
    /// Define stamina máxima (pode ser usado por buffs)
    /// </summary>
    public void SetMaxStamina(float newMax)
    {
        float oldPercent = GetStaminaPercent();
        maxStamina = newMax;
        currentStamina = maxStamina * oldPercent;
    }

    #endregion

    #region DEBUG

    private void OnGUI()
    {
        if (!showDebug || !isLocalPlayer) return;

        // Barra de stamina
        float barWidth = 200f;
        float barHeight = 15f;
        float x = Screen.width / 2f - barWidth / 2f;
        float y = Screen.height - 25f;

        // Background
        GUI.color = Color.black;
        GUI.Box(new Rect(x - 2, y - 2, barWidth + 4, barHeight + 4), "");

        // Stamina bar
        GUI.color = Color.Lerp(Color.red, Color.yellow, GetStaminaPercent());
        GUI.Box(new Rect(x, y, barWidth * GetStaminaPercent(), barHeight), "");

        // Texto
        GUI.color = Color.white;
        GUI.Label(new Rect(x, y, barWidth, barHeight), 
            $"Stamina: {currentStamina:F0}/{maxStamina:F0}", 
            new GUIStyle { alignment = TextAnchor.MiddleCenter, fontSize = 10 });
    }

    #endregion
}