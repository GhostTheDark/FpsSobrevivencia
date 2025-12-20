using UnityEngine;

/// <summary>
/// Hitbox de parte do corpo
/// Cada jogador tem múltiplas hitboxes (cabeça, peito, pernas, etc)
/// Usado para calcular multiplicadores de dano
/// </summary>
[RequireComponent(typeof(Collider))]
public class Hitbox : MonoBehaviour
{
    [Header("Hitbox Info")]
    [SerializeField] private HitboxType hitboxType = HitboxType.Chest;
    [SerializeField] private float damageMultiplier = 1f;

    [Header("Armor Protection")]
    [SerializeField] private bool hasArmorSlot = true;
    [SerializeField] private ArmorSlot armorSlot = ArmorSlot.Body;

    [Header("Visual")]
    [SerializeField] private bool showGizmos = false;
    [SerializeField] private Color gizmoColor = Color.red;

    [Header("Debug")]
    [SerializeField] private bool showDebug = false;

    // Owner
    private int ownerId = -1;
    private PlayerHealth ownerHealth;
    private InventorySystem ownerInventory;

    // Componentes
    private Collider hitboxCollider;

    private void Awake()
    {
        hitboxCollider = GetComponent<Collider>();
        
        // Hitbox deve ser trigger
        if (hitboxCollider != null)
        {
            hitboxCollider.isTrigger = true;
        }

        // Busca owner
        NetworkPlayer owner = GetComponentInParent<NetworkPlayer>();
        if (owner != null)
        {
            Initialize(owner.clientId, owner.GetComponent<PlayerHealth>(), owner.GetComponent<InventorySystem>());
        }
    }

    /// <summary>
    /// Inicializa a hitbox
    /// </summary>
    public void Initialize(int owner, PlayerHealth health, InventorySystem inventory)
    {
        ownerId = owner;
        ownerHealth = health;
        ownerInventory = inventory;

        if (showDebug)
            Debug.Log($"[Hitbox] {hitboxType} hitbox initialized for player {ownerId}");
    }

    #region DAMAGE PROCESSING

    /// <summary>
    /// Processa hit nesta hitbox
    /// Chamado pelo sistema de combate ao detectar colisão
    /// </summary>
    public float ProcessHit(float baseDamage, DamageType damageType, int attackerId)
    {
        if (ownerId == attackerId)
        {
            // Não pode dar dano em si mesmo
            return 0f;
        }

        // Calcula dano final
        float armorValue = GetArmorValue();
        float finalDamage = CalculateFinalDamage(baseDamage, damageType, armorValue);

        // Aplica dano
        if (ownerHealth != null)
        {
            ownerHealth.TakeDamage(finalDamage, attackerId, damageType);
        }

        // Efeitos visuais
        SpawnHitEffect();

        if (showDebug)
        {
            Debug.Log($"[Hitbox] {hitboxType} hit! Base: {baseDamage}, Final: {finalDamage:F1}, " +
                     $"Armor: {armorValue}");
        }

        return finalDamage;
    }

    /// <summary>
    /// Calcula dano final considerando multiplicador e armadura
    /// </summary>
    private float CalculateFinalDamage(float baseDamage, DamageType damageType, float armorValue)
    {
        if (DamageSystem.Instance != null)
        {
            return DamageSystem.Instance.CalculateDamage(
                baseDamage,
                damageType,
                hitboxType,
                armorValue
            );
        }

        // Fallback se não tem DamageSystem
        float damage = baseDamage * damageMultiplier;

        // Redução de armadura básica
        if (armorValue > 0)
        {
            float reduction = Mathf.Clamp01(armorValue / 100f) * 0.7f; // Max 70% reduction
            damage *= (1f - reduction);
        }

        return damage;
    }

    #endregion

    #region ARMOR

    /// <summary>
    /// Retorna valor de armadura nesta hitbox
    /// </summary>
    private float GetArmorValue()
    {
        if (!hasArmorSlot || ownerInventory == null)
            return 0f;

        // TODO: Buscar armadura equipada no slot correspondente
        // Por enquanto retorna 0
        return 0f;
    }

    /// <summary>
    /// Retorna item de armadura equipado
    /// </summary>
    private ItemData GetEquippedArmor()
    {
        if (!hasArmorSlot || ownerInventory == null)
            return null;

        // TODO: Implementar quando tiver sistema de equipamento completo
        // InventorySlot armorSlot = ownerInventory.GetWearableSlot(this.armorSlot);
        // if (armorSlot != null && armorSlot.HasItem())
        // {
        //     return ItemDatabase.Instance.GetItem(armorSlot.itemId);
        // }

        return null;
    }

    #endregion

    #region EFFECTS

    /// <summary>
    /// Spawna efeito de hit (sangue, faísca, etc)
    /// </summary>
    private void SpawnHitEffect()
    {
        // TODO: Efeitos de hit baseados no tipo
        // Sangue para hitbox orgânica
        // Faísca para metal
        // Partículas de tecido para roupa
    }

    #endregion

    #region GETTERS

    public HitboxType GetHitboxType() => hitboxType;
    public float GetDamageMultiplier() => damageMultiplier;
    public int GetOwnerId() => ownerId;
    public bool HasArmorSlot() => hasArmorSlot;
    public ArmorSlot GetArmorSlot() => armorSlot;

    /// <summary>
    /// Retorna se é headshot
    /// </summary>
    public bool IsHeadshot()
    {
        return hitboxType == HitboxType.Head;
    }

    #endregion

    #region GIZMOS

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Collider col = GetComponent<Collider>();
        if (col == null) return;

        Gizmos.color = gizmoColor;

        // Desenha baseado no tipo de collider
        if (col is BoxCollider)
        {
            BoxCollider box = col as BoxCollider;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(box.center, box.size);
        }
        else if (col is SphereCollider)
        {
            SphereCollider sphere = col as SphereCollider;
            Gizmos.DrawWireSphere(transform.TransformPoint(sphere.center), sphere.radius * transform.lossyScale.x);
        }
        else if (col is CapsuleCollider)
        {
            CapsuleCollider capsule = col as CapsuleCollider;
            // Simplified capsule drawing
            Vector3 center = transform.TransformPoint(capsule.center);
            float radius = capsule.radius * transform.lossyScale.x;
            Gizmos.DrawWireSphere(center, radius);
        }

        #if UNITY_EDITOR
        // Label com tipo
        UnityEditor.Handles.Label(
            transform.position,
            hitboxType.ToString(),
            new GUIStyle
            {
                alignment = TextAnchor.MiddleCenter,
                normal = new GUIStyleState { textColor = gizmoColor }
            }
        );
        #endif
    }

    #endregion
}

/// <summary>
/// Slots de armadura correspondentes às hitboxes
/// </summary>
public enum ArmorSlot
{
    Head,       // Capacete
    Body,       // Colete/Peito
    Legs,       // Calças
    Hands,      // Luvas
    Feet        // Botas
}