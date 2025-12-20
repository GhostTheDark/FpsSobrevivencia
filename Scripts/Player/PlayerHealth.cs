using UnityEngine;
using System;

/// <summary>
/// Sistema de vida do jogador
/// Server Authoritative - servidor controla TODO o dano
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;
    [SerializeField] private float healthRegenRate = 1f; // HP por segundo
    [SerializeField] private float healthRegenDelay = 10f; // Segundos sem dano para começar regenerar

    [Header("Damage Settings")]
    [SerializeField] private float fallDamageThreshold = 10f; // Velocidade mínima de queda
    [SerializeField] private float fallDamageMultiplier = 10f;

    [Header("Debug")]
    [SerializeField] private bool showDebug = false;

    // Estado
    private int clientId = -1;
    private bool isLocalPlayer = false;
    private bool isInitialized = false;
    private bool isDead = false;

    // Regeneração
    private float timeSinceLastDamage = 0f;
    private bool isRegenerating = false;

    // Componentes
    private PlayerStats playerStats;
    private NetworkPlayer networkPlayer;
    private CharacterController characterController;

    // Callbacks
    public Action<float, int, DamageType> OnDamageTaken;
    public Action<int, DamageType> OnDeath;
    public Action OnRespawn;

    private void Awake()
    {
        playerStats = GetComponent<PlayerStats>();
        networkPlayer = GetComponent<NetworkPlayer>();
        characterController = GetComponent<CharacterController>();
    }

    /// <summary>
    /// Inicializa o componente
    /// </summary>
    public void Initialize(int id, bool isLocal)
    {
        clientId = id;
        isLocalPlayer = isLocal;
        isInitialized = true;

        currentHealth = maxHealth;
        isDead = false;

        Debug.Log($"[PlayerHealth] Initialized (ID: {clientId}, Local: {isLocal})");
    }

    private void Update()
    {
        if (!isInitialized || isDead) return;

        // Regeneração de vida
        UpdateHealthRegen();

        // Dano de queda
        CheckFallDamage();
    }

    #region DAMAGE

    /// <summary>
    /// Aplica dano ao jogador
    /// </summary>
    public void TakeDamage(float damage, int attackerId, DamageType damageType)
    {
        if (isDead || damage <= 0) return;

        // Reduz vida
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        // Reseta timer de regeneração
        timeSinceLastDamage = 0f;
        isRegenerating = false;

        // Callbacks
        OnDamageTaken?.Invoke(damage, attackerId, damageType);

        if (showDebug)
            Debug.Log($"[PlayerHealth] Took {damage:F1} damage from {attackerId} ({damageType}). Health: {currentHealth:F1}/{maxHealth:F1}");

        // Verifica morte
        if (currentHealth <= 0)
        {
            Die(attackerId, damageType);
        }

        // Efeitos visuais (apenas local)
        if (isLocalPlayer)
        {
            // TODO: Tela vermelha
            // TODO: Som de dor
            // TODO: Camera shake
            PlayerCamera cam = GetComponent<PlayerCamera>();
            if (cam != null)
                cam.AddShake(damage * 0.01f);
        }
    }

    /// <summary>
    /// Mata o jogador
    /// </summary>
    public void Die(int killerId, DamageType damageType)
    {
        if (isDead) return;

        isDead = true;
        currentHealth = 0;

        // Notifica PlayerStats
        if (playerStats != null)
            playerStats.SetAlive(false);

        // Callbacks
        OnDeath?.Invoke(killerId, damageType);

        if (showDebug)
            Debug.Log($"[PlayerHealth] Player {clientId} died! Killer: {killerId}, Type: {damageType}");

        // Efeitos visuais (apenas local)
        if (isLocalPlayer)
        {
            // TODO: Tela preta
            // TODO: UI de morte
            // TODO: Desabilitar controles
            PlayerController controller = GetComponent<PlayerController>();
            if (controller != null)
                controller.SetControlsEnabled(false);
        }

        // Servidor: dropa itens, notifica outros jogadores
        if (NetworkManager.Instance != null && NetworkManager.Instance.isServer)
        {
            OnServerPlayerDeath(killerId, damageType);
        }
    }

    /// <summary>
    /// Lógica de morte no servidor
    /// </summary>
    private void OnServerPlayerDeath(int killerId, DamageType damageType)
    {
        // TODO: Dropar inventário
        // TODO: Spawnar corpo/mochila
        // TODO: Registrar estatística de kill

        // Envia mensagem de morte para todos
        NetworkMessage deathMessage = new NetworkMessage
        {
            type = MessageType.PlayerDeath,
            clientId = clientId
        };

        using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
        using (System.IO.BinaryWriter writer = new System.IO.BinaryWriter(ms))
        {
            writer.Write(killerId);
            writer.Write((byte)damageType);
            deathMessage.data = ms.ToArray();
        }

        NetworkManager.Instance.SendToAllClients(deathMessage);
    }

    #endregion

    #region HEALING

    /// <summary>
    /// Cura o jogador
    /// </summary>
    public void Heal(float amount)
    {
        if (isDead) return;

        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);

        if (showDebug)
            Debug.Log($"[PlayerHealth] Healed {amount:F1}. Health: {currentHealth:F1}/{maxHealth:F1}");
    }

    /// <summary>
    /// Atualiza regeneração natural de vida
    /// </summary>
    private void UpdateHealthRegen()
    {
        if (currentHealth >= maxHealth) return;

        timeSinceLastDamage += Time.deltaTime;

        // Começa a regenerar após delay
        if (timeSinceLastDamage >= healthRegenDelay)
        {
            if (!isRegenerating)
            {
                isRegenerating = true;
                if (showDebug)
                    Debug.Log("[PlayerHealth] Started health regeneration");
            }

            Heal(healthRegenRate * Time.deltaTime);
        }
    }

    #endregion

    #region FALL DAMAGE

    /// <summary>
    /// Verifica dano de queda
    /// </summary>
    private void CheckFallDamage()
    {
        if (!isLocalPlayer || characterController == null) return;

        // Só aplica dano se estava no ar e agora está no chão
        if (characterController.isGrounded && characterController.velocity.y < -fallDamageThreshold)
        {
            float fallSpeed = Mathf.Abs(characterController.velocity.y);
            float damage = (fallSpeed - fallDamageThreshold) * fallDamageMultiplier;

            if (damage > 0)
            {
                TakeDamage(damage, -1, DamageType.Fall);

                if (showDebug)
                    Debug.Log($"[PlayerHealth] Fall damage! Speed: {fallSpeed:F1}, Damage: {damage:F1}");
            }
        }
    }

    #endregion

    #region RESPAWN

    /// <summary>
    /// Respawna o jogador
    /// </summary>
    public void Respawn()
    {
        isDead = false;
        currentHealth = maxHealth;
        timeSinceLastDamage = 0f;
        isRegenerating = false;

        // Notifica PlayerStats
        if (playerStats != null)
            playerStats.SetAlive(true);

        // Callbacks
        OnRespawn?.Invoke();

        // Reabilita controles (apenas local)
        if (isLocalPlayer)
        {
            PlayerController controller = GetComponent<PlayerController>();
            if (controller != null)
                controller.SetControlsEnabled(true);
        }

        if (showDebug)
            Debug.Log($"[PlayerHealth] Player {clientId} respawned");
    }

    #endregion

    #region PUBLIC METHODS

    /// <summary>
    /// Reseta vida para o máximo
    /// </summary>
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        timeSinceLastDamage = 0f;
        isRegenerating = false;
    }

    /// <summary>
    /// Retorna vida atual
    /// </summary>
    public float GetHealth() => currentHealth;

    /// <summary>
    /// Retorna vida máxima
    /// </summary>
    public float GetMaxHealth() => maxHealth;

    /// <summary>
    /// Retorna porcentagem de vida (0-1)
    /// </summary>
    public float GetHealthPercent() => currentHealth / maxHealth;

    /// <summary>
    /// Retorna se está morto
    /// </summary>
    public bool IsDead() => isDead;

    /// <summary>
    /// Define vida máxima (pode ser usado por buffs/debuffs)
    /// </summary>
    public void SetMaxHealth(float newMax)
    {
        float oldPercent = GetHealthPercent();
        maxHealth = newMax;
        currentHealth = maxHealth * oldPercent; // Mantém porcentagem
    }

    /// <summary>
    /// Retorna se está regenerando
    /// </summary>
    public bool IsRegenerating() => isRegenerating;

    #endregion

    #region DEBUG

    private void OnGUI()
    {
        if (!showDebug || !isLocalPlayer) return;

        // Barra de vida simples
        float barWidth = 200f;
        float barHeight = 20f;
        float x = Screen.width / 2f - barWidth / 2f;
        float y = Screen.height - 50f;

        // Background
        GUI.color = Color.black;
        GUI.Box(new Rect(x - 2, y - 2, barWidth + 4, barHeight + 4), "");

        // Health bar
        GUI.color = Color.Lerp(Color.red, Color.green, GetHealthPercent());
        GUI.Box(new Rect(x, y, barWidth * GetHealthPercent(), barHeight), "");

        // Texto
        GUI.color = Color.white;
        GUI.Label(new Rect(x, y, barWidth, barHeight), 
            $"{currentHealth:F0} / {maxHealth:F0} HP", 
            new GUIStyle { alignment = TextAnchor.MiddleCenter });
    }

    #endregion
}