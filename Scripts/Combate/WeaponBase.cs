using UnityEngine;

/// <summary>
/// Classe base para todas as armas do jogo
/// Define comportamento comum e interface padrão
/// </summary>
public abstract class WeaponBase : MonoBehaviour
{
    [Header("Weapon Info")]
    [SerializeField] protected int weaponId;
    [SerializeField] protected string weaponName = "Weapon";
    [SerializeField] protected WeaponType weaponType = WeaponType.Melee;

    [Header("Stats")]
    [SerializeField] protected float damage = 10f;
    [SerializeField] protected float range = 2f;
    [SerializeField] protected float attackRate = 1f; // Ataques por segundo
    [SerializeField] protected DamageType damageType = DamageType.Melee;

    [Header("Durability")]
    [SerializeField] protected bool hasDurability = true;
    [SerializeField] protected float maxDurability = 100f;
    [SerializeField] protected float currentDurability = 100f;
    [SerializeField] protected float durabilityLossPerUse = 1f;

    [Header("Animation")]
    [SerializeField] protected Animator weaponAnimator;
    [SerializeField] protected string attackAnimationTrigger = "Attack";
    [SerializeField] protected string reloadAnimationTrigger = "Reload";

    [Header("Audio")]
    [SerializeField] protected AudioClip attackSound;
    [SerializeField] protected AudioClip emptySound;
    [SerializeField] protected AudioClip breakSound;

    [Header("Effects")]
    [SerializeField] protected GameObject muzzleFlashPrefab;
    [SerializeField] protected Transform muzzlePoint;
    [SerializeField] protected GameObject impactEffectPrefab;

    [Header("Camera")]
    [SerializeField] protected float cameraShakeAmount = 0.1f;

    [Header("Debug")]
    [SerializeField] protected bool showDebug = false;

    // Estado
    protected bool isEquipped = false;
    protected bool isAttacking = false;
    protected float nextAttackTime = 0f;
    protected bool isBroken = false;

    // Owner
    protected int ownerId = -1;
    protected PlayerController ownerController;
    protected PlayerCamera ownerCamera;
    protected NetworkCombat ownerNetworkCombat;

    // Componentes
    protected AudioSource audioSource;

    protected virtual void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        currentDurability = maxDurability;
    }

    #region INITIALIZATION

    /// <summary>
    /// Inicializa a arma com seu dono
    /// </summary>
    public virtual void Initialize(int owner, PlayerController controller)
    {
        ownerId = owner;
        ownerController = controller;
        
        if (controller != null)
        {
            ownerCamera = controller.GetComponent<PlayerCamera>();
            ownerNetworkCombat = controller.GetComponent<NetworkCombat>();
        }

        if (showDebug)
            Debug.Log($"[WeaponBase] {weaponName} initialized for player {ownerId}");
    }

    /// <summary>
    /// Equipa a arma
    /// </summary>
    public virtual void Equip()
    {
        isEquipped = true;
        gameObject.SetActive(true);

        if (showDebug)
            Debug.Log($"[WeaponBase] {weaponName} equipped");

        // TODO: Animação de equip
    }

    /// <summary>
    /// Desequipa a arma
    /// </summary>
    public virtual void Unequip()
    {
        isEquipped = false;
        gameObject.SetActive(false);

        if (showDebug)
            Debug.Log($"[WeaponBase] {weaponName} unequipped");
    }

    #endregion

    #region ATTACK

    /// <summary>
    /// Ataque primário (deve ser implementado pelas classes filhas)
    /// </summary>
    public virtual void PrimaryAttack()
    {
        if (!CanAttack())
        {
            if (showDebug)
                Debug.LogWarning($"[WeaponBase] Cannot attack with {weaponName}");
            return;
        }

        // Reseta timer de ataque
        nextAttackTime = Time.time + (1f / attackRate);

        // Consome durabilidade
        ConsumeDurability();

        // Animação
        PlayAttackAnimation();

        // Som
        PlayAttackSound();

        // Camera shake
        ApplyCameraShake();

        if (showDebug)
            Debug.Log($"[WeaponBase] {weaponName} primary attack");
    }

    /// <summary>
    /// Ataque secundário (opcional, pode ser overridden)
    /// </summary>
    public virtual void SecondaryAttack()
    {
        if (showDebug)
            Debug.Log($"[WeaponBase] {weaponName} has no secondary attack");
    }

    /// <summary>
    /// Verifica se pode atacar
    /// </summary>
    protected virtual bool CanAttack()
    {
        if (!isEquipped)
            return false;

        if (isBroken)
            return false;

        if (Time.time < nextAttackTime)
            return false;

        if (isAttacking)
            return false;

        return true;
    }

    #endregion

    #region DURABILITY

    /// <summary>
    /// Consome durabilidade ao usar
    /// </summary>
    protected virtual void ConsumeDurability()
    {
        if (!hasDurability) return;

        currentDurability -= durabilityLossPerUse;
        currentDurability = Mathf.Max(0, currentDurability);

        if (currentDurability <= 0 && !isBroken)
        {
            Break();
        }

        if (showDebug)
            Debug.Log($"[WeaponBase] {weaponName} durability: {currentDurability:F0}/{maxDurability:F0}");
    }

    /// <summary>
    /// Quebra a arma
    /// </summary>
    protected virtual void Break()
    {
        isBroken = true;

        if (showDebug)
            Debug.LogWarning($"[WeaponBase] {weaponName} is broken!");

        // Som de quebra
        if (breakSound != null && audioSource != null)
            audioSource.PlayOneShot(breakSound);

        // TODO: Efeitos visuais
        // TODO: Notificar inventário
    }

    /// <summary>
    /// Repara a arma
    /// </summary>
    public virtual void Repair(float amount)
    {
        if (!hasDurability) return;

        currentDurability += amount;
        currentDurability = Mathf.Min(currentDurability, maxDurability);

        if (currentDurability > 0)
            isBroken = false;

        if (showDebug)
            Debug.Log($"[WeaponBase] {weaponName} repaired to {currentDurability:F0}");
    }

    #endregion

    #region ANIMATION & EFFECTS

    /// <summary>
    /// Reproduz animação de ataque
    /// </summary>
    protected virtual void PlayAttackAnimation()
    {
        if (weaponAnimator != null)
        {
            weaponAnimator.SetTrigger(attackAnimationTrigger);
        }
    }

    /// <summary>
    /// Reproduz som de ataque
    /// </summary>
    protected virtual void PlayAttackSound()
    {
        if (attackSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(attackSound);
        }
    }

    /// <summary>
    /// Aplica camera shake
    /// </summary>
    protected virtual void ApplyCameraShake()
    {
        if (ownerCamera != null)
        {
            ownerCamera.AddShake(cameraShakeAmount);
        }
    }

    /// <summary>
    /// Spawna efeito de muzzle flash
    /// </summary>
    protected virtual void SpawnMuzzleFlash()
    {
        if (muzzleFlashPrefab != null && muzzlePoint != null)
        {
            GameObject flash = Instantiate(muzzleFlashPrefab, muzzlePoint.position, muzzlePoint.rotation);
            Destroy(flash, 0.1f);
        }
    }

    /// <summary>
    /// Spawna efeito de impacto
    /// </summary>
    protected virtual void SpawnImpactEffect(Vector3 position, Vector3 normal)
    {
        if (impactEffectPrefab != null)
        {
            Quaternion rotation = Quaternion.LookRotation(normal);
            GameObject impact = Instantiate(impactEffectPrefab, position, rotation);
            Destroy(impact, 2f);
        }
    }

    #endregion

    #region RELOAD

    /// <summary>
    /// Recarrega (para armas de fogo)
    /// </summary>
    public virtual void Reload()
    {
        if (showDebug)
            Debug.Log($"[WeaponBase] {weaponName} has no reload");
    }

    /// <summary>
    /// Verifica se pode recarregar
    /// </summary>
    public virtual bool CanReload()
    {
        return false;
    }

    #endregion

    #region GETTERS

    public int GetWeaponId() => weaponId;
    public string GetWeaponName() => weaponName;
    public WeaponType GetWeaponType() => weaponType;
    public float GetDamage() => damage;
    public float GetRange() => range;
    public float GetAttackRate() => attackRate;
    public DamageType GetDamageType() => damageType;
    public float GetDurability() => currentDurability;
    public float GetMaxDurability() => maxDurability;
    public float GetDurabilityPercent() => currentDurability / maxDurability;
    public bool IsBroken() => isBroken;
    public bool IsEquipped() => isEquipped;

    #endregion

    #region DEBUG

    protected virtual void OnGUI()
    {
        if (!showDebug || !isEquipped) return;

        float width = 250f;
        float height = 120f;
        float x = Screen.width - width - 20f;
        float y = Screen.height - height - 20f;

        GUI.color = Color.black;
        GUI.Box(new Rect(x - 2, y - 2, width + 4, height + 4), "");

        GUI.color = Color.white;
        GUILayout.BeginArea(new Rect(x, y, width, height));
        GUILayout.Label($"=== {weaponName.ToUpper()} ===", new GUIStyle { alignment = TextAnchor.MiddleCenter });
        GUILayout.Label($"Damage: {damage}");
        GUILayout.Label($"Range: {range}m");
        GUILayout.Label($"Rate: {attackRate}/s");
        
        if (hasDurability)
        {
            float durabilityPercent = GetDurabilityPercent();
            GUI.color = Color.Lerp(Color.red, Color.green, durabilityPercent);
            GUILayout.Label($"Durability: {currentDurability:F0}/{maxDurability:F0} ({durabilityPercent * 100f:F0}%)");
        }

        GUILayout.EndArea();
    }

    #endregion
}

/// <summary>
/// Tipos de arma (já definido em ItemData, mas repetido aqui para referência)
/// </summary>
public enum WeaponType
{
    None,
    Melee,
    Bow,
    Pistol,
    Rifle,
    Shotgun,
    SMG,
    LMG,
    Launcher
}