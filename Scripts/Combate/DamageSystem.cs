using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Sistema centralizado de dano
/// Gerencia todos os tipos de dano e modificadores
/// Server Authoritative - todas as operações devem ser validadas no servidor
/// </summary>
public class DamageSystem : MonoBehaviour
{
    public static DamageSystem Instance { get; private set; }

    [Header("Damage Multipliers")]
    [SerializeField] private float headshotMultiplier = 2.0f;
    [SerializeField] private float chestMultiplier = 1.0f;
    [SerializeField] private float stomachMultiplier = 0.9f;
    [SerializeField] private float limbMultiplier = 0.75f;

    [Header("Armor")]
    [SerializeField] private bool enableArmorSystem = true;
    [SerializeField] private float maxArmorAbsorption = 0.8f; // 80% de absorção no máximo

    [Header("Damage Types")]
    [SerializeField] private DamageTypeConfig[] damageTypeConfigs;

    [Header("Debug")]
    [SerializeField] private bool showDebug = false;

    // Cache de configurações
    private Dictionary<DamageType, DamageTypeConfig> damageTypeCache = new Dictionary<DamageType, DamageTypeConfig>();

    private void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Cacheia configs
        CacheDamageTypes();
    }

    /// <summary>
    /// Cacheia configs de tipos de dano
    /// </summary>
    private void CacheDamageTypes()
    {
        if (damageTypeConfigs == null) return;

        foreach (var config in damageTypeConfigs)
        {
            if (!damageTypeCache.ContainsKey(config.damageType))
            {
                damageTypeCache.Add(config.damageType, config);
            }
        }
    }

    #region DAMAGE CALCULATION

    /// <summary>
    /// Calcula dano final considerando hitbox, armadura e outros modificadores
    /// </summary>
    public float CalculateDamage(
        float baseDamage,
        DamageType damageType,
        HitboxType hitboxType,
        float armorValue = 0f,
        Dictionary<string, float> additionalModifiers = null)
    {
        float finalDamage = baseDamage;

        // 1. Multiplier de hitbox (parte do corpo)
        finalDamage *= GetHitboxMultiplier(hitboxType);

        // 2. Modificador de tipo de dano
        finalDamage *= GetDamageTypeMultiplier(damageType);

        // 3. Redução de armadura
        if (enableArmorSystem && armorValue > 0)
        {
            finalDamage = ApplyArmorReduction(finalDamage, armorValue, damageType);
        }

        // 4. Modificadores adicionais
        if (additionalModifiers != null)
        {
            foreach (var modifier in additionalModifiers)
            {
                finalDamage *= modifier.Value;
            }
        }

        // 5. Garante dano mínimo
        finalDamage = Mathf.Max(1f, finalDamage);

        if (showDebug)
        {
            Debug.Log($"[DamageSystem] Calculated damage: {baseDamage} -> {finalDamage:F1} " +
                     $"(Hitbox: {hitboxType}, Armor: {armorValue}, Type: {damageType})");
        }

        return finalDamage;
    }

    /// <summary>
    /// Retorna multiplier de hitbox
    /// </summary>
    private float GetHitboxMultiplier(HitboxType hitboxType)
    {
        switch (hitboxType)
        {
            case HitboxType.Head:
                return headshotMultiplier;
            case HitboxType.Chest:
                return chestMultiplier;
            case HitboxType.Stomach:
                return stomachMultiplier;
            case HitboxType.LeftArm:
            case HitboxType.RightArm:
            case HitboxType.LeftLeg:
            case HitboxType.RightLeg:
                return limbMultiplier;
            default:
                return 1f;
        }
    }

    /// <summary>
    /// Retorna multiplier de tipo de dano
    /// </summary>
    private float GetDamageTypeMultiplier(DamageType damageType)
    {
        if (damageTypeCache.TryGetValue(damageType, out DamageTypeConfig config))
        {
            return config.damageMultiplier;
        }
        return 1f;
    }

    /// <summary>
    /// Aplica redução de armadura
    /// </summary>
    private float ApplyArmorReduction(float damage, float armorValue, DamageType damageType)
    {
        // Calcula porcentagem de absorção baseada no valor da armadura
        float absorption = Mathf.Clamp01(armorValue / 100f) * maxArmorAbsorption;

        // Certos tipos de dano ignoram armadura parcialmente
        if (damageTypeCache.TryGetValue(damageType, out DamageTypeConfig config))
        {
            absorption *= config.armorEffectiveness;
        }

        // Aplica absorção
        float reducedDamage = damage * (1f - absorption);

        if (showDebug)
        {
            Debug.Log($"[DamageSystem] Armor reduction: {damage:F1} -> {reducedDamage:F1} " +
                     $"(Absorption: {absorption * 100f:F0}%)");
        }

        return reducedDamage;
    }

    #endregion

    #region DAMAGE APPLICATION

    /// <summary>
    /// Aplica dano a um jogador (SERVER ONLY)
    /// </summary>
    public void ApplyDamageToPlayer(
        int targetId,
        int attackerId,
        float baseDamage,
        DamageType damageType,
        HitboxType hitboxType,
        Vector3 hitPoint,
        Vector3 hitDirection)
    {
        // APENAS SERVIDOR
        if (NetworkManager.Instance == null || !NetworkManager.Instance.isServer)
        {
            Debug.LogWarning("[DamageSystem] ApplyDamageToPlayer called on client!");
            return;
        }

        // TODO: Buscar player target
        // TODO: Calcular armadura
        // TODO: Aplicar dano

        float armorValue = 0f; // TODO: Pegar do inventário

        float finalDamage = CalculateDamage(baseDamage, damageType, hitboxType, armorValue);

        // TODO: Aplicar dano ao PlayerHealth do target

        if (showDebug)
        {
            Debug.Log($"[DamageSystem] Player {attackerId} dealt {finalDamage:F1} damage to player {targetId}");
        }

        // Broadcast para clientes (efeitos visuais)
        BroadcastDamageEvent(targetId, attackerId, finalDamage, hitPoint, hitDirection);
    }

    /// <summary>
    /// Envia evento de dano para todos os clientes
    /// </summary>
    private void BroadcastDamageEvent(
        int targetId,
        int attackerId,
        float damage,
        Vector3 hitPoint,
        Vector3 hitDirection)
    {
        // TODO: Enviar via NetworkManager
        // Clientes usam isso para spawnar efeitos visuais
    }

    #endregion

    #region DAMAGE INFO

    /// <summary>
    /// Retorna descrição de um tipo de dano
    /// </summary>
    public string GetDamageTypeDescription(DamageType damageType)
    {
        if (damageTypeCache.TryGetValue(damageType, out DamageTypeConfig config))
        {
            return config.description;
        }
        return "Unknown damage type";
    }

    /// <summary>
    /// Retorna cor para tipo de dano (UI)
    /// </summary>
    public Color GetDamageTypeColor(DamageType damageType)
    {
        if (damageTypeCache.TryGetValue(damageType, out DamageTypeConfig config))
        {
            return config.damageColor;
        }
        return Color.white;
    }

    #endregion

    #region DAMAGE OVER TIME

    /// <summary>
    /// Aplica dano contínuo (sangramento, veneno, fogo)
    /// </summary>
    public void ApplyDamageOverTime(
        int targetId,
        float damagePerSecond,
        float duration,
        DamageType damageType)
    {
        // TODO: Implementar sistema de status effects
        if (showDebug)
        {
            Debug.Log($"[DamageSystem] Applying {damagePerSecond} DPS for {duration}s to player {targetId}");
        }
    }

    #endregion

    #region COMBAT STATS

    /// <summary>
    /// Registra kill para estatísticas
    /// </summary>
    public void RegisterKill(int killerId, int victimId, DamageType damageType)
    {
        if (showDebug)
        {
            Debug.Log($"[DamageSystem] Player {killerId} killed player {victimId} with {damageType}");
        }

        // TODO: Sistema de estatísticas
        // TODO: Verificar achievements
        // TODO: Notificações
    }

    /// <summary>
    /// Registra hit para estatísticas
    /// </summary>
    public void RegisterHit(int attackerId, int targetId, float damage, bool wasHeadshot)
    {
        if (showDebug)
        {
            Debug.Log($"[DamageSystem] Player {attackerId} hit player {targetId} for {damage:F1} " +
                     $"(Headshot: {wasHeadshot})");
        }

        // TODO: Sistema de estatísticas
        // TODO: Hitmarker para o atacante
    }

    #endregion
}

/// <summary>
/// Configuração de tipo de dano
/// </summary>
[System.Serializable]
public class DamageTypeConfig
{
    public DamageType damageType;
    public string description;
    public Color damageColor = Color.white;
    [Range(0f, 2f)]
    public float damageMultiplier = 1f;
    [Range(0f, 1f)]
    public float armorEffectiveness = 1f; // 1 = armadura funciona 100%, 0 = ignora armadura
}

/// <summary>
/// Tipos de hitbox (partes do corpo)
/// </summary>
public enum HitboxType
{
    Head,
    Chest,
    Stomach,
    LeftArm,
    RightArm,
    LeftLeg,
    RightLeg,
    Generic
}