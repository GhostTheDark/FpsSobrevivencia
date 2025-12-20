using UnityEngine;

/// <summary>
/// Gerencia todos os stats do jogador
/// Coordena vida, fome, sede, temperatura e stamina
/// </summary>
public class PlayerStats : MonoBehaviour
{
    [Header("Identity")]
    private int clientId = -1;
    private bool isLocalPlayer = false;
    private bool isInitialized = false;

    [Header("Debug")]
    [SerializeField] private bool showDebug = false;

    // Componentes de stats
    private PlayerHealth health;
    private PlayerHunger hunger;
    private PlayerThirst thirst;
    private PlayerTemperature temperature;
    private PlayerStamina stamina;

    // Estado geral
    private bool isAlive = true;

    private void Awake()
    {
        // Pega referências dos componentes
        health = GetComponent<PlayerHealth>();
        hunger = GetComponent<PlayerHunger>();
        thirst = GetComponent<PlayerThirst>();
        temperature = GetComponent<PlayerTemperature>();
        stamina = GetComponent<PlayerStamina>();
    }

    /// <summary>
    /// Inicializa os stats
    /// </summary>
    public void Initialize(int id, bool isLocal)
    {
        clientId = id;
        isLocalPlayer = isLocal;
        isInitialized = true;

        // Inicializa todos os componentes
        if (health != null)
            health.Initialize(id, isLocal);

        if (hunger != null)
            hunger.Initialize(id, isLocal);

        if (thirst != null)
            thirst.Initialize(id, isLocal);

        if (temperature != null)
            temperature.Initialize(id, isLocal);

        if (stamina != null)
            stamina.Initialize(id, isLocal);

        Debug.Log($"[PlayerStats] Initialized (ID: {clientId}, Local: {isLocal})");
    }

    private void Update()
    {
        if (!isInitialized) return;

        // Verifica condições que causam dano
        CheckSurvivalDamage();

        // Debug
        if (showDebug && isLocalPlayer)
            DebugInfo();
    }

    #region SURVIVAL DAMAGE

    /// <summary>
    /// Verifica condições de sobrevivência que causam dano
    /// </summary>
    private void CheckSurvivalDamage()
    {
        if (!isAlive || health == null) return;

        // Dano por fome
        if (hunger != null && hunger.GetHunger() <= 0)
        {
            health.TakeDamage(2f * Time.deltaTime, -1, DamageType.Hunger);
        }

        // Dano por sede (mais rápido que fome)
        if (thirst != null && thirst.GetThirst() <= 0)
        {
            health.TakeDamage(5f * Time.deltaTime, -1, DamageType.Thirst);
        }

        // Dano por frio
        if (temperature != null)
        {
            float temp = temperature.GetTemperature();
            if (temp < 0f)
            {
                float coldDamage = Mathf.Abs(temp) * 0.5f * Time.deltaTime;
                health.TakeDamage(coldDamage, -1, DamageType.Cold);
            }
            // Dano por calor
            else if (temp > 100f)
            {
                float heatDamage = (temp - 100f) * 0.3f * Time.deltaTime;
                health.TakeDamage(heatDamage, -1, DamageType.Heat);
            }
        }
    }

    #endregion

    #region PUBLIC METHODS

    /// <summary>
    /// Reseta todos os stats para valores máximos
    /// </summary>
    public void ResetStats()
    {
        if (health != null)
            health.ResetHealth();

        if (hunger != null)
            hunger.ResetHunger();

        if (thirst != null)
            thirst.ResetThirst();

        if (temperature != null)
            temperature.ResetTemperature();

        if (stamina != null)
            stamina.ResetStamina();

        isAlive = true;

        if (showDebug)
            Debug.Log("[PlayerStats] All stats reset");
    }

    /// <summary>
    /// Retorna se o jogador está vivo
    /// </summary>
    public bool IsAlive() => isAlive;

    /// <summary>
    /// Define estado de vida
    /// </summary>
    public void SetAlive(bool alive)
    {
        isAlive = alive;
    }

    /// <summary>
    /// Retorna porcentagem geral de saúde (0-1)
    /// Média de todos os stats
    /// </summary>
    public float GetOverallHealth()
    {
        float total = 0f;
        int count = 0;

        if (health != null)
        {
            total += health.GetHealthPercent();
            count++;
        }

        if (hunger != null)
        {
            total += hunger.GetHungerPercent();
            count++;
        }

        if (thirst != null)
        {
            total += thirst.GetThirstPercent();
            count++;
        }

        if (stamina != null)
        {
            total += stamina.GetStaminaPercent();
            count++;
        }

        return count > 0 ? total / count : 1f;
    }

    /// <summary>
    /// Retorna se o jogador está em condição crítica
    /// </summary>
    public bool IsCritical()
    {
        if (health != null && health.GetHealthPercent() < 0.2f)
            return true;

        if (hunger != null && hunger.GetHungerPercent() < 0.1f)
            return true;

        if (thirst != null && thirst.GetThirstPercent() < 0.1f)
            return true;

        return false;
    }

    /// <summary>
    /// Cura todos os stats
    /// </summary>
    public void HealAll(float amount)
    {
        if (health != null)
            health.Heal(amount);

        if (hunger != null)
            hunger.AddHunger(amount);

        if (thirst != null)
            thirst.AddThirst(amount);
    }

    #endregion

    #region GETTERS

    public PlayerHealth GetHealthComponent() => health;
    public PlayerHunger GetHungerComponent() => hunger;
    public PlayerThirst GetThirstComponent() => thirst;
    public PlayerTemperature GetTemperatureComponent() => temperature;
    public PlayerStamina GetStaminaComponent() => stamina;

    #endregion

    #region DEBUG

    private void DebugInfo()
    {
        string info = "[PlayerStats] ";
        
        if (health != null)
            info += $"HP: {health.GetHealth():F0}/{health.GetMaxHealth():F0} ";

        if (hunger != null)
            info += $"Hunger: {hunger.GetHunger():F0}% ";

        if (thirst != null)
            info += $"Thirst: {thirst.GetThirst():F0}% ";

        if (stamina != null)
            info += $"Stamina: {stamina.GetStamina():F0}% ";

        if (temperature != null)
            info += $"Temp: {temperature.GetTemperature():F0}°C";

        Debug.Log(info);
    }

    private void OnGUI()
    {
        if (!showDebug || !isLocalPlayer) return;

        GUILayout.BeginArea(new Rect(10, 510, 300, 200));
        GUILayout.Box("=== PLAYER STATS ===");
        
        if (health != null)
            GUILayout.Label($"Health: {health.GetHealth():F0}/{health.GetMaxHealth():F0}");
        
        if (hunger != null)
            GUILayout.Label($"Hunger: {hunger.GetHunger():F0}%");
        
        if (thirst != null)
            GUILayout.Label($"Thirst: {thirst.GetThirst():F0}%");
        
        if (stamina != null)
            GUILayout.Label($"Stamina: {stamina.GetStamina():F0}%");
        
        if (temperature != null)
            GUILayout.Label($"Temperature: {temperature.GetTemperature():F1}°C");

        GUILayout.Label($"Overall: {GetOverallHealth() * 100f:F0}%");
        GUILayout.Label($"Critical: {IsCritical()}");
        
        GUILayout.EndArea();
    }

    #endregion
}