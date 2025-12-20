using UnityEngine;
using System;

/// <summary>
/// Representa uma peça de construção no mundo
/// Foundation, Wall, Floor, Doorway, etc
/// Suporta upgrade de materiais e decay
/// </summary>
public class BuildingPiece : MonoBehaviour
{
    [Header("Building Info")]
    [SerializeField] private BuildingPieceType pieceType = BuildingPieceType.Foundation;
    [SerializeField] private BuildingGrade currentGrade = BuildingGrade.Twig;
    [SerializeField] private int buildingId = -1;
    [SerializeField] private int ownerId = -1;

    [Header("Health")]
    [SerializeField] private float currentHealth = 100f;
    [SerializeField] private float maxHealthTwig = 50f;
    [SerializeField] private float maxHealthWood = 250f;
    [SerializeField] private float maxHealthStone = 500f;
    [SerializeField] private float maxHealthMetal = 1000f;
    [SerializeField] private float maxHealthArmored = 2000f;

    [Header("Materials")]
    [SerializeField] private Material twigMaterial;
    [SerializeField] private Material woodMaterial;
    [SerializeField] private Material stoneMaterial;
    [SerializeField] private Material metalMaterial;
    [SerializeField] private Material armoredMaterial;

    [Header("Decay")]
    [SerializeField] private bool hasDecay = true;
    [SerializeField] private float decayTickInterval = 60f; // Segundos entre ticks
    [SerializeField] private float decayDamagePerTick = 1f;
    private float nextDecayTime = 0f;
    private bool isInCupboardRange = false;

    [Header("Sockets")]
    [SerializeField] private BuildingSocket[] sockets;

    [Header("Visual")]
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private Material highlightMaterial;
    private Material originalMaterial;
    private bool isHighlighted = false;

    [Header("Debug")]
    [SerializeField] private bool showDebug = false;

    // Estado
    private bool isInitialized = false;
    private bool isDestroyed = false;

    // Callbacks
    public Action<BuildingPiece, float> OnDamageTaken;
    public Action<BuildingPiece> OnDestroyed;
    public Action<BuildingPiece, BuildingGrade> OnUpgraded;

    private void Awake()
    {
        if (meshRenderer == null)
            meshRenderer = GetComponentInChildren<MeshRenderer>();

        if (meshRenderer != null)
            originalMaterial = meshRenderer.material;

        // Calcula decay inicial
        nextDecayTime = Time.time + decayTickInterval;
    }

    /// <summary>
    /// Inicializa a building piece
    /// </summary>
    public void Initialize(int id, int owner, BuildingGrade grade)
    {
        if (isInitialized)
        {
            Debug.LogWarning($"[BuildingPiece] Already initialized!");
            return;
        }

        buildingId = id;
        ownerId = owner;
        currentGrade = grade;

        // Define saúde inicial
        currentHealth = GetMaxHealthForGrade(grade);

        // Aplica material
        ApplyMaterialForGrade(grade);

        // Registra no sistema
        BuildingSystem.RegisterBuildingPiece(this);

        isInitialized = true;

        if (showDebug)
            Debug.Log($"[BuildingPiece] {pieceType} initialized (ID: {buildingId}, Owner: {ownerId}, Grade: {grade})");
    }

    private void Update()
    {
        if (!isInitialized || isDestroyed) return;

        // Processo de decay
        if (hasDecay && !isInCupboardRange)
        {
            ProcessDecay();
        }
    }

    #region HEALTH & DAMAGE

    /// <summary>
    /// Aplica dano à peça
    /// </summary>
    public void TakeDamage(float damage, int attackerId, DamageType damageType)
    {
        if (isDestroyed) return;

        // Modificadores de dano baseado no material
        float damageMultiplier = GetDamageMultiplier(damageType);
        float finalDamage = damage * damageMultiplier;

        currentHealth -= finalDamage;
        currentHealth = Mathf.Max(0, currentHealth);

        OnDamageTaken?.Invoke(this, finalDamage);

        if (showDebug)
            Debug.Log($"[BuildingPiece] Took {finalDamage:F1} damage. Health: {currentHealth:F1}/{GetMaxHealth():F1}");

        // Verifica destruição
        if (currentHealth <= 0)
        {
            DestroyPiece(attackerId);
        }

        // TODO: Efeitos visuais de dano
        // TODO: Som de impacto
    }

    /// <summary>
    /// Retorna multiplicador de dano baseado no tipo
    /// </summary>
    private float GetDamageMultiplier(DamageType damageType)
    {
        switch (currentGrade)
        {
            case BuildingGrade.Twig:
                // Twig é fraco a tudo
                return damageType == DamageType.Melee ? 2f : 1f;

            case BuildingGrade.Wood:
                // Madeira é fraca a fogo e machado
                if (damageType == DamageType.Fire)
                    return 3f;
                if (damageType == DamageType.Melee)
                    return 1.5f;
                return 1f;

            case BuildingGrade.Stone:
                // Pedra é fraca a explosivos e picareta
                if (damageType == DamageType.Explosion)
                    return 2f;
                if (damageType == DamageType.Melee)
                    return 1.2f;
                return 0.5f;

            case BuildingGrade.Metal:
                // Metal é forte contra tudo menos explosivos
                if (damageType == DamageType.Explosion)
                    return 1.5f;
                return 0.3f;

            case BuildingGrade.Armored:
                // Armored é muito resistente
                if (damageType == DamageType.Explosion)
                    return 1f;
                return 0.1f;

            default:
                return 1f;
        }
    }

    /// <summary>
    /// Destrói a peça
    /// </summary>
    public void DestroyPiece(int destroyerId)
    {
        if (isDestroyed) return;

        isDestroyed = true;

        OnDestroyed?.Invoke(this);

        // Unregister
        BuildingSystem.UnregisterBuildingPiece(buildingId);

        if (showDebug)
            Debug.Log($"[BuildingPiece] {pieceType} destroyed by player {destroyerId}");

        // TODO: Dropar recursos proporcionais
        // TODO: Efeitos de destruição
        // TODO: Som de destruição

        // Remove do servidor
        if (NetworkManager.Instance != null && NetworkManager.Instance.isServer)
        {
            // Notifica clientes
            NetworkMessage message = new NetworkMessage
            {
                type = MessageType.DestroyBuilding
            };
            message.SetInt(buildingId);
            NetworkManager.Instance.SendToAllClients(message);
        }

        Destroy(gameObject, 0.1f);
    }

    #endregion

    #region UPGRADE

    /// <summary>
    /// Faz upgrade do material
    /// </summary>
    public bool UpgradeMaterial(int newGradeInt)
    {
        BuildingGrade newGrade = (BuildingGrade)newGradeInt;

        if (newGrade <= currentGrade)
        {
            if (showDebug)
                Debug.LogWarning($"[BuildingPiece] Cannot downgrade! Current: {currentGrade}, Requested: {newGrade}");
            return false;
        }

        if (newGrade == BuildingGrade.Armored && currentGrade != BuildingGrade.Metal)
        {
            if (showDebug)
                Debug.LogWarning("[BuildingPiece] Must upgrade to Metal before Armored!");
            return false;
        }

        // Salva porcentagem de vida
        float healthPercent = currentHealth / GetMaxHealth();

        // Faz upgrade
        currentGrade = newGrade;

        // Restaura porcentagem de vida com novo max
        currentHealth = GetMaxHealth() * healthPercent;

        // Aplica novo material
        ApplyMaterialForGrade(newGrade);

        OnUpgraded?.Invoke(this, newGrade);

        if (showDebug)
            Debug.Log($"[BuildingPiece] Upgraded to {newGrade}");

        // TODO: Efeitos de upgrade
        // TODO: Som de upgrade

        return true;
    }

    /// <summary>
    /// Aplica material visual baseado no grade
    /// </summary>
    private void ApplyMaterialForGrade(BuildingGrade grade)
    {
        if (meshRenderer == null) return;

        Material mat = null;

        switch (grade)
        {
            case BuildingGrade.Twig: mat = twigMaterial; break;
            case BuildingGrade.Wood: mat = woodMaterial; break;
            case BuildingGrade.Stone: mat = stoneMaterial; break;
            case BuildingGrade.Metal: mat = metalMaterial; break;
            case BuildingGrade.Armored: mat = armoredMaterial; break;
        }

        if (mat != null)
        {
            meshRenderer.material = mat;
            originalMaterial = mat;
        }
    }

    #endregion

    #region DECAY

    /// <summary>
    /// Processa decay da construção
    /// </summary>
    private void ProcessDecay()
    {
        if (Time.time < nextDecayTime) return;

        nextDecayTime = Time.time + decayTickInterval;

        // Aplica dano de decay
        TakeDamage(decayDamagePerTick, -1, DamageType.Generic);

        if (showDebug)
            Debug.Log($"[BuildingPiece] Decay tick! Health: {currentHealth:F1}");
    }

    /// <summary>
    /// Define se está dentro de range de Tool Cupboard
    /// </summary>
    public void SetInCupboardRange(bool inRange)
    {
        isInCupboardRange = inRange;

        if (showDebug && isInCupboardRange != inRange)
            Debug.Log($"[BuildingPiece] Cupboard range: {inRange}");
    }

    /// <summary>
    /// Repara a peça (usado por Tool Cupboard com upkeep)
    /// </summary>
    public void Repair(float amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, GetMaxHealth());

        if (showDebug)
            Debug.Log($"[BuildingPiece] Repaired {amount}. Health: {currentHealth:F1}");
    }

    #endregion

    #region HELPERS

    /// <summary>
    /// Retorna vida máxima baseada no grade atual
    /// </summary>
    public float GetMaxHealth()
    {
        return GetMaxHealthForGrade(currentGrade);
    }

    /// <summary>
    /// Retorna vida máxima para um grade específico
    /// </summary>
    private float GetMaxHealthForGrade(BuildingGrade grade)
    {
        switch (grade)
        {
            case BuildingGrade.Twig: return maxHealthTwig;
            case BuildingGrade.Wood: return maxHealthWood;
            case BuildingGrade.Stone: return maxHealthStone;
            case BuildingGrade.Metal: return maxHealthMetal;
            case BuildingGrade.Armored: return maxHealthArmored;
            default: return maxHealthTwig;
        }
    }

    /// <summary>
    /// Retorna porcentagem de vida (0-1)
    /// </summary>
    public float GetHealthPercent()
    {
        return currentHealth / GetMaxHealth();
    }

    #endregion

    #region HIGHLIGHT

    /// <summary>
    /// Ativa/desativa highlight (para upgrade mode)
    /// </summary>
    public void SetHighlight(bool highlight)
    {
        if (meshRenderer == null) return;

        isHighlighted = highlight;

        if (highlight && highlightMaterial != null)
        {
            meshRenderer.material = highlightMaterial;
        }
        else
        {
            meshRenderer.material = originalMaterial;
        }
    }

    #endregion

    #region GETTERS

    public BuildingPieceType GetPieceType() => pieceType;
    public BuildingGrade GetCurrentGrade() => currentGrade;
    public int GetBuildingId() => buildingId;
    public int GetOwnerId() => ownerId;
    public float GetCurrentHealth() => currentHealth;
    public bool IsDestroyed() => isDestroyed;
    public BuildingSocket[] GetSockets() => sockets;

    #endregion

    #region DEBUG

    private void OnDrawGizmos()
    {
        if (!showDebug) return;

        // Desenha indicador de saúde
        Gizmos.color = Color.Lerp(Color.red, Color.green, GetHealthPercent());
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.3f);

        // Desenha sockets
        if (sockets != null)
        {
            Gizmos.color = Color.yellow;
            foreach (var socket in sockets)
            {
                if (socket.socketTransform != null)
                {
                    Gizmos.DrawWireSphere(socket.socketTransform.position, 0.1f);
                    Gizmos.DrawRay(socket.socketTransform.position, socket.socketTransform.forward * 0.3f);
                }
            }
        }

        #if UNITY_EDITOR
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 2.5f,
            $"{pieceType}\n{currentGrade}\nHP: {currentHealth:F0}/{GetMaxHealth():F0}"
        );
        #endif
    }

    #endregion

    private void OnDestroy()
    {
        // Cleanup
        if (isInitialized)
        {
            BuildingSystem.UnregisterBuildingPiece(buildingId);
        }
    }
}

/// <summary>
/// Tipos de peças de construção
/// </summary>
public enum BuildingPieceType
{
    Foundation,
    Floor,
    Wall,
    WallLow,
    Doorway,
    WindowWall,
    Stairs,
    Roof,
    RoofTriangle,
    Frame,
    DoorFrame
}

/// <summary>
/// Socket para snap de construção
/// </summary>
[System.Serializable]
public class BuildingSocket
{
    public string socketName;
    public SocketType socketType;
    public Transform socketTransform;
    public bool isOccupied = false;
}

/// <summary>
/// Tipos de socket
/// </summary>
public enum SocketType
{
    Foundation,      // Para colocar fundação
    Floor,           // Para colocar piso
    Wall,            // Para colocar parede
    Ceiling,         // Para colocar teto
    Doorway,         // Para colocar porta
    Window,          // Para colocar janela
    Any              // Aceita qualquer tipo
}