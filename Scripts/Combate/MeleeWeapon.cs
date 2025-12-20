using UnityEngine;
using System.Collections;

/// <summary>
/// Arma corpo a corpo (melee)
/// Lança, machado, picareta, espada, etc
/// Pode ser usada tanto em combate quanto para coletar recursos
/// </summary>
public class MeleeWeapon : WeaponBase
{
    [Header("Melee Settings")]
    [SerializeField] private MeleeType meleeType = MeleeType.Weapon;
    [SerializeField] private bool canGatherResources = false;
    [SerializeField] private float swingDuration = 0.5f;

    [Header("Hit Detection")]
    [SerializeField] private LayerMask hitMask;
    [SerializeField] private float hitRadius = 0.5f;
    [SerializeField] private Transform hitPoint;
    [SerializeField] private bool useBoxCast = false;
    [SerializeField] private Vector3 boxCastSize = new Vector3(0.5f, 0.5f, 1f);

    [Header("Gathering")]
    [SerializeField] private float gatherMultiplier = 1f;
    [SerializeField] private ResourceType effectiveAgainst = ResourceType.None;
    [SerializeField] private float effectiveMultiplier = 2f;

    [Header("Combo")]
    [SerializeField] private bool hasComboSystem = false;
    [SerializeField] private int maxComboHits = 3;
    [SerializeField] private float comboResetTime = 2f;
    [SerializeField] private float[] comboDamageMultipliers = { 1f, 1.2f, 1.5f };

    [Header("Heavy Attack")]
    [SerializeField] private bool hasHeavyAttack = false;
    [SerializeField] private float heavyAttackDamage = 2f;
    [SerializeField] private float heavyAttackWindupTime = 0.8f;
    [SerializeField] private float heavyAttackStaminaCost = 20f;

    // Estado
    private int currentComboStep = 0;
    private float lastHitTime = 0f;
    private bool isChargingHeavy = false;
    private float heavyChargeTime = 0f;

    // Hit detection
    private bool hasHitThisSwing = false;

    protected override void Awake()
    {
        base.Awake();

        if (hitPoint == null)
            hitPoint = transform;
    }

    private void Update()
    {
        if (!isEquipped) return;

        // Reset combo se passou tempo
        if (hasComboSystem && Time.time - lastHitTime > comboResetTime)
        {
            ResetCombo();
        }

        // Processa heavy attack charge
        if (isChargingHeavy)
        {
            heavyChargeTime += Time.deltaTime;
        }
    }

    #region ATTACK

    public override void PrimaryAttack()
    {
        if (!CanAttack()) return;

        // Reseta flag de hit
        hasHitThisSwing = false;

        // Base attack
        base.PrimaryAttack();

        // Inicia swing
        StartCoroutine(SwingAttack());

        if (showDebug)
            Debug.Log($"[MeleeWeapon] {weaponName} swing attack");
    }

    public override void SecondaryAttack()
    {
        if (!hasHeavyAttack) return;
        if (!CanAttack()) return;

        StartHeavyAttack();
    }

    /// <summary>
    /// Coroutine do swing
    /// </summary>
    private IEnumerator SwingAttack()
    {
        isAttacking = true;

        // Aguarda metade da animação para fazer hit detection
        yield return new WaitForSeconds(swingDuration * 0.5f);

        // Hit detection
        PerformHitDetection();

        // Aguarda resto da animação
        yield return new WaitForSeconds(swingDuration * 0.5f);

        isAttacking = false;

        // Incrementa combo
        if (hasComboSystem)
        {
            AdvanceCombo();
        }
    }

    /// <summary>
    /// Detecta hits durante o swing
    /// </summary>
    private void PerformHitDetection()
    {
        if (hasHitThisSwing) return;

        Vector3 origin = hitPoint.position;
        Vector3 direction = hitPoint.forward;

        RaycastHit[] hits;

        if (useBoxCast)
        {
            // BoxCast para área maior
            hits = Physics.BoxCastAll(origin, boxCastSize / 2f, direction, hitPoint.rotation, range, hitMask);
        }
        else
        {
            // SphereCast simples
            hits = Physics.SphereCastAll(origin, hitRadius, direction, range, hitMask);
        }

        if (hits.Length > 0)
        {
            ProcessHits(hits);
            hasHitThisSwing = true;
        }

        if (showDebug)
        {
            Debug.DrawRay(origin, direction * range, Color.yellow, 1f);
        }
    }

    /// <summary>
    /// Processa todos os hits detectados
    /// </summary>
    private void ProcessHits(RaycastHit[] hits)
    {
        foreach (RaycastHit hit in hits)
        {
            if (showDebug)
                Debug.Log($"[MeleeWeapon] Hit {hit.collider.name}");

            // Verifica se é jogador
            NetworkPlayer player = hit.collider.GetComponentInParent<NetworkPlayer>();
            if (player != null && player.clientId != ownerId)
            {
                // Hit em jogador
                DealDamageToPlayer(player, hit);
                continue;
            }

            // Verifica se é recurso
            if (canGatherResources)
            {
                // TODO: Sistema de recursos
                // Resource resource = hit.collider.GetComponent<Resource>();
                // if (resource != null)
                // {
                //     GatherResource(resource, hit);
                //     continue;
                // }
            }

            // Verifica se é construção
            BuildingPiece building = hit.collider.GetComponentInParent<BuildingPiece>();
            if (building != null)
            {
                DamageBuilding(building, hit);
                continue;
            }

            // Efeito de impacto genérico
            SpawnImpactEffect(hit.point, hit.normal);
        }
    }

    /// <summary>
    /// Aplica dano a um jogador
    /// </summary>
    private void DealDamageToPlayer(NetworkPlayer target, RaycastHit hit)
    {
        float finalDamage = CalculateDamage();

        // Envia via NetworkCombat
        if (ownerNetworkCombat != null)
        {
            ownerNetworkCombat.MeleeAttack(
                hitPoint.position,
                hitPoint.forward,
                finalDamage,
                range
            );
        }

        // Efeito de impacto
        SpawnImpactEffect(hit.point, hit.normal);

        if (showDebug)
            Debug.Log($"[MeleeWeapon] Dealt {finalDamage} damage to player {target.clientId}");
    }

    /// <summary>
    /// Aplica dano a uma construção
    /// </summary>
    private void DamageBuilding(BuildingPiece building, RaycastHit hit)
    {
        float finalDamage = CalculateDamage();
        
        // Aplica dano direto (servidor validará)
        building.TakeDamage(finalDamage, ownerId, damageType);

        // Efeito de impacto
        SpawnImpactEffect(hit.point, hit.normal);

        if (showDebug)
            Debug.Log($"[MeleeWeapon] Dealt {finalDamage} damage to building");
    }

    /// <summary>
    /// Coleta recurso
    /// </summary>
    private void GatherResource(object resource, RaycastHit hit)
    {
        // TODO: Implementar quando criar sistema de recursos
        if (showDebug)
            Debug.Log($"[MeleeWeapon] Gathering resource");
    }

    /// <summary>
    /// Calcula dano baseado em combo e outros fatores
    /// </summary>
    private float CalculateDamage()
    {
        float finalDamage = damage;

        // Multiplier de combo
        if (hasComboSystem && currentComboStep < comboDamageMultipliers.Length)
        {
            finalDamage *= comboDamageMultipliers[currentComboStep];
        }

        return finalDamage;
    }

    #endregion

    #region HEAVY ATTACK

    /// <summary>
    /// Inicia heavy attack
    /// </summary>
    private void StartHeavyAttack()
    {
        isChargingHeavy = true;
        heavyChargeTime = 0f;

        if (showDebug)
            Debug.Log($"[MeleeWeapon] Charging heavy attack...");

        // TODO: Animação de charge
    }

    /// <summary>
    /// Solta heavy attack (chamado quando solta botão)
    /// </summary>
    public void ReleaseHeavyAttack()
    {
        if (!isChargingHeavy) return;

        isChargingHeavy = false;

        // Verifica se carregou o suficiente
        if (heavyChargeTime >= heavyAttackWindupTime)
        {
            ExecuteHeavyAttack();
        }
        else
        {
            // Não carregou o suficiente, cancela
            if (showDebug)
                Debug.Log($"[MeleeWeapon] Heavy attack cancelled (not charged enough)");
        }
    }

    /// <summary>
    /// Executa heavy attack
    /// </summary>
    private void ExecuteHeavyAttack()
    {
        // Consome stamina
        PlayerStamina stamina = ownerController?.GetComponent<PlayerStamina>();
        if (stamina != null && !stamina.UseStamina(heavyAttackStaminaCost))
        {
            if (showDebug)
                Debug.Log($"[MeleeWeapon] Not enough stamina for heavy attack");
            return;
        }

        if (showDebug)
            Debug.Log($"[MeleeWeapon] Executing heavy attack!");

        // Aplica dano aumentado
        float originalDamage = damage;
        damage *= heavyAttackDamage;

        // Executa ataque normal com dano aumentado
        PrimaryAttack();

        // Restaura dano
        damage = originalDamage;

        // TODO: Efeitos especiais de heavy attack
    }

    #endregion

    #region COMBO

    /// <summary>
    /// Avança no combo
    /// </summary>
    private void AdvanceCombo()
    {
        currentComboStep++;
        if (currentComboStep >= maxComboHits)
        {
            currentComboStep = maxComboHits - 1;
        }

        lastHitTime = Time.time;

        if (showDebug)
            Debug.Log($"[MeleeWeapon] Combo step: {currentComboStep + 1}/{maxComboHits}");
    }

    /// <summary>
    /// Reseta combo
    /// </summary>
    private void ResetCombo()
    {
        if (currentComboStep > 0)
        {
            currentComboStep = 0;

            if (showDebug)
                Debug.Log($"[MeleeWeapon] Combo reset");
        }
    }

    #endregion

    #region GETTERS

    public MeleeType GetMeleeType() => meleeType;
    public bool CanGather() => canGatherResources;
    public int GetComboStep() => currentComboStep;
    public bool IsChargingHeavy() => isChargingHeavy;
    public float GetHeavyChargePercent() => Mathf.Clamp01(heavyChargeTime / heavyAttackWindupTime);

    #endregion

    #region DEBUG

    private void OnDrawGizmosSelected()
    {
        if (hitPoint == null) return;

        // Desenha área de hit
        Gizmos.color = Color.red;
        
        if (useBoxCast)
        {
            Gizmos.matrix = hitPoint.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.forward * range / 2f, boxCastSize);
        }
        else
        {
            Gizmos.DrawWireSphere(hitPoint.position + hitPoint.forward * range, hitRadius);
        }
    }

    protected override void OnGUI()
    {
        base.OnGUI();

        if (!showDebug || !isEquipped) return;

        // Info adicional de melee
        if (hasComboSystem)
        {
            float width = 150f;
            float height = 40f;
            float x = (Screen.width - width) / 2f;
            float y = Screen.height - 150f;

            GUI.color = Color.black;
            GUI.Box(new Rect(x - 2, y - 2, width + 4, height + 4), "");

            GUI.color = Color.yellow;
            GUILayout.BeginArea(new Rect(x, y, width, height));
            GUILayout.Label($"COMBO: {currentComboStep + 1}/{maxComboHits}", 
                new GUIStyle { alignment = TextAnchor.MiddleCenter, fontSize = 16 });
            GUILayout.EndArea();
        }

        // Heavy charge indicator
        if (isChargingHeavy)
        {
            float barWidth = 200f;
            float barHeight = 20f;
            float x = (Screen.width - barWidth) / 2f;
            float y = Screen.height - 100f;

            // Background
            GUI.color = Color.black;
            GUI.Box(new Rect(x - 2, y - 2, barWidth + 4, barHeight + 4), "");

            // Charge bar
            float chargePercent = GetHeavyChargePercent();
            GUI.color = chargePercent >= 1f ? Color.green : Color.yellow;
            GUI.Box(new Rect(x, y, barWidth * chargePercent, barHeight), "");

            // Texto
            GUI.color = Color.white;
            GUI.Label(new Rect(x, y, barWidth, barHeight), 
                "CHARGING HEAVY", 
                new GUIStyle { alignment = TextAnchor.MiddleCenter });
        }
    }

    #endregion
}

/// <summary>
/// Tipos de arma melee
/// </summary>
public enum MeleeType
{
    Weapon,      // Arma de combate pura (espada, lança)
    Tool,        // Ferramenta (machado, picareta)
    Hybrid       // Híbrido (pode combater e coletar)
}

/// <summary>
/// Tipos de recurso (para gathering)
/// </summary>
public enum ResourceType
{
    None,
    Wood,
    Stone,
    Metal,
    Sulfur,
    Cloth
}