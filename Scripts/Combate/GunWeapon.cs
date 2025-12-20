using UnityEngine;

/// <summary>
/// Arma de fogo com sistema completo de munição e recoil
/// Suporta automático, semiautomático e burst
/// Server Authoritative via NetworkCombat
/// </summary>
public class GunWeapon : WeaponBase
{
    [Header("Gun Settings")]
    [SerializeField] private FireMode fireMode = FireMode.Semi;
    [SerializeField] private int burstCount = 3;
    [SerializeField] private float burstDelay = 0.1f;

    [Header("Ammo")]
    [SerializeField] private AmmoType ammoType = AmmoType.Pistol_556;
    [SerializeField] private int magazineSize = 30;
    [SerializeField] private int currentAmmo = 30;
    [SerializeField] private float reloadTime = 2.5f;

    [Header("Ballistics")]
    [SerializeField] private float bulletSpeed = 300f;
    [SerializeField] private float bulletDrop = 0f; // Gravidade do projétil
    [SerializeField] private bool useHitscan = true; // true = instant, false = projectile

    [Header("Recoil")]
    [SerializeField] private Vector2 recoilAmount = new Vector2(1f, 2f); // X = horizontal, Y = vertical
    [SerializeField] private float recoilRecoverySpeed = 5f;
    [SerializeField] private AnimationCurve recoilPattern;

    [Header("Spread")]
    [SerializeField] private float baseSpread = 0.5f; // Graus
    [SerializeField] private float movementSpreadMultiplier = 2f;
    [SerializeField] private float aimSpreadMultiplier = 0.5f;

    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;

    // Estado
    private bool isReloading = false;
    private bool isFiring = false;
    private int burstShotsFired = 0;
    private float nextBurstShotTime = 0f;

    // Recoil acumulado
    private Vector2 currentRecoil = Vector2.zero;
    private int shotsFired = 0;

    // Auto fire
    private bool triggerHeld = false;

    protected override void Awake()
    {
        base.Awake();
        currentAmmo = magazineSize;
    }

    private void Update()
    {
        if (!isEquipped) return;

        // Processa burst
        if (isFiring && fireMode == FireMode.Burst)
        {
            ProcessBurst();
        }

        // Recuperação de recoil
        RecoverRecoil();
    }

    #region ATTACK

    public override void PrimaryAttack()
    {
        if (!CanAttack())
        {
            // Toca som de arma vazia se não tem munição
            if (currentAmmo <= 0 && emptySound != null && audioSource != null)
            {
                audioSource.PlayOneShot(emptySound);
            }
            return;
        }

        // Fire mode logic
        switch (fireMode)
        {
            case FireMode.Semi:
                if (!triggerHeld)
                {
                    FireShot();
                    triggerHeld = true;
                }
                break;

            case FireMode.Auto:
                FireShot();
                break;

            case FireMode.Burst:
                if (!isFiring)
                {
                    StartBurst();
                }
                break;
        }
    }

    /// <summary>
    /// Chamado quando solta o trigger
    /// </summary>
    public void ReleaseTrigger()
    {
        triggerHeld = false;
    }

    /// <summary>
    /// Dispara um tiro
    /// </summary>
    private void FireShot()
    {
        if (currentAmmo <= 0) return;

        // Base attack
        base.PrimaryAttack();

        // Consome munição
        currentAmmo--;

        // Incrementa contador de tiros (para recoil)
        shotsFired++;

        // Aplica recoil
        ApplyRecoil();

        // Muzzle flash
        SpawnMuzzleFlash();

        // Dispara via network
        FireBullet();

        if (showDebug)
            Debug.Log($"[GunWeapon] Fired! Ammo: {currentAmmo}/{magazineSize}");
    }

    /// <summary>
    /// Dispara o projétil/raycast
    /// </summary>
    private void FireBullet()
    {
        if (ownerCamera == null || ownerNetworkCombat == null) return;

        // Pega direção da câmera
        Transform camTransform = ownerCamera.GetCameraTransform();
        Vector3 shootOrigin = camTransform.position;
        Vector3 shootDirection = camTransform.forward;

        // Aplica spread
        shootDirection = ApplySpread(shootDirection);

        if (useHitscan)
        {
            // Hitscan instantâneo
            RaycastHit hit;
            if (Physics.Raycast(shootOrigin, shootDirection, out hit, range))
            {
                // Spawna efeito de impacto
                SpawnImpactEffect(hit.point, hit.normal);

                if (showDebug)
                {
                    Debug.DrawLine(shootOrigin, hit.point, Color.red, 1f);
                }
            }

            // Envia para servidor via NetworkCombat
            ownerNetworkCombat.Fire(shootOrigin, shootDirection, weaponId, damage);
        }
        else
        {
            // Projétil físico
            SpawnProjectile(shootOrigin, shootDirection);
        }
    }

    /// <summary>
    /// Spawna projétil físico
    /// </summary>
    private void SpawnProjectile(Vector3 origin, Vector3 direction)
    {
        if (projectilePrefab == null) return;

        GameObject projObj = Instantiate(projectilePrefab, origin, Quaternion.LookRotation(direction));
        Projectile projectile = projObj.GetComponent<Projectile>();
        
        if (projectile != null)
        {
            projectile.Initialize(ownerId, damage, bulletSpeed, range, damageType);
        }
    }

    /// <summary>
    /// Aplica spread ao tiro
    /// </summary>
    private Vector3 ApplySpread(Vector3 direction)
    {
        float spread = baseSpread;

        // Aumenta spread se estiver se movendo
        if (ownerController != null)
        {
            // TODO: Verificar se está se movendo
            // spread *= movementSpreadMultiplier;
        }

        // TODO: Reduz spread se estiver mirando (ADS)
        // spread *= aimSpreadMultiplier;

        // Aplica spread aleatório
        float spreadX = Random.Range(-spread, spread);
        float spreadY = Random.Range(-spread, spread);

        Quaternion spreadRotation = Quaternion.Euler(spreadY, spreadX, 0);
        return spreadRotation * direction;
    }

    #endregion

    #region BURST

    /// <summary>
    /// Inicia burst fire
    /// </summary>
    private void StartBurst()
    {
        isFiring = true;
        burstShotsFired = 0;
        FireShot();
        nextBurstShotTime = Time.time + burstDelay;
    }

    /// <summary>
    /// Processa burst fire
    /// </summary>
    private void ProcessBurst()
    {
        if (Time.time >= nextBurstShotTime && burstShotsFired < burstCount)
        {
            FireShot();
            burstShotsFired++;
            nextBurstShotTime = Time.time + burstDelay;
        }

        if (burstShotsFired >= burstCount)
        {
            isFiring = false;
        }
    }

    #endregion

    #region RECOIL

    /// <summary>
    /// Aplica recoil à câmera
    /// </summary>
    private void ApplyRecoil()
    {
        // Recoil baseado no padrão
        float recoilMultiplier = 1f;
        if (recoilPattern != null && recoilPattern.length > 0)
        {
            float t = Mathf.Clamp01((float)shotsFired / 30f); // Normaliza até 30 tiros
            recoilMultiplier = recoilPattern.Evaluate(t);
        }

        Vector2 recoil = new Vector2(
            Random.Range(-recoilAmount.x, recoilAmount.x), // Horizontal aleatório
            recoilAmount.y * recoilMultiplier // Vertical baseado no padrão
        );

        currentRecoil += recoil;

        // TODO: Aplicar recoil na câmera
        // Será feito quando integrarmos com PlayerCamera
    }

    /// <summary>
    /// Recuperação de recoil
    /// </summary>
    private void RecoverRecoil()
    {
        if (currentRecoil.magnitude > 0.01f)
        {
            currentRecoil = Vector2.Lerp(currentRecoil, Vector2.zero, recoilRecoverySpeed * Time.deltaTime);
        }
        else
        {
            currentRecoil = Vector2.zero;
            shotsFired = 0;
        }
    }

    #endregion

    #region RELOAD

    public override void Reload()
    {
        if (!CanReload()) return;

        StartReload();
    }

    public override bool CanReload()
    {
        if (isReloading) return false;
        if (currentAmmo >= magazineSize) return false;
        if (!HasAmmoInInventory()) return false;
        return true;
    }

    /// <summary>
    /// Inicia recarga
    /// </summary>
    private void StartReload()
    {
        isReloading = true;

        if (showDebug)
            Debug.Log($"[GunWeapon] Reloading {weaponName}...");

        // Animação
        if (weaponAnimator != null)
        {
            weaponAnimator.SetTrigger(reloadAnimationTrigger);
        }

        // TODO: Som de reload

        // Completa reload após tempo
        Invoke(nameof(CompleteReload), reloadTime);
    }

    /// <summary>
    /// Completa recarga
    /// </summary>
    private void CompleteReload()
    {
        isReloading = false;

        // Calcula quantos cartuchos precisa
        int ammoNeeded = magazineSize - currentAmmo;

        // Consome do inventário
        int ammoTaken = TakeAmmoFromInventory(ammoNeeded);

        currentAmmo += ammoTaken;

        if (showDebug)
            Debug.Log($"[GunWeapon] Reload complete! Ammo: {currentAmmo}/{magazineSize}");
    }

    /// <summary>
    /// Cancela reload (se trocar de arma, etc)
    /// </summary>
    public void CancelReload()
    {
        if (!isReloading) return;

        isReloading = false;
        CancelInvoke(nameof(CompleteReload));

        if (showDebug)
            Debug.Log($"[GunWeapon] Reload cancelled");
    }

    /// <summary>
    /// Verifica se tem munição no inventário
    /// </summary>
    private bool HasAmmoInInventory()
    {
        // TODO: Verificar no inventário via InventorySystem
        return true; // Temporário
    }

    /// <summary>
    /// Pega munição do inventário
    /// </summary>
    private int TakeAmmoFromInventory(int amount)
    {
        // TODO: Remover munição do inventário via InventorySystem
        return amount; // Temporário
    }

    #endregion

    #region OVERRIDE

    protected override bool CanAttack()
    {
        if (!base.CanAttack()) return false;
        if (isReloading) return false;
        if (currentAmmo <= 0) return false;
        return true;
    }

    public override void Unequip()
    {
        base.Unequip();
        CancelReload();
        ReleaseTrigger();
    }

    #endregion

    #region GETTERS

    public int GetCurrentAmmo() => currentAmmo;
    public int GetMagazineSize() => magazineSize;
    public AmmoType GetAmmoType() => ammoType;
    public bool IsReloading() => isReloading;
    public FireMode GetFireMode() => fireMode;

    /// <summary>
    /// Define munição (usado ao equipar arma salva)
    /// </summary>
    public void SetAmmo(int ammo)
    {
        currentAmmo = Mathf.Clamp(ammo, 0, magazineSize);
    }

    #endregion

    #region DEBUG

    protected override void OnGUI()
    {
        base.OnGUI();

        if (!showDebug || !isEquipped) return;

        // Info adicional de arma de fogo
        float width = 250f;
        float height = 80f;
        float x = Screen.width - width - 20f;
        float y = Screen.height - height - 140f;

        GUI.color = Color.black;
        GUI.Box(new Rect(x - 2, y - 2, width + 4, height + 4), "");

        GUI.color = Color.white;
        GUILayout.BeginArea(new Rect(x, y, width, height));
        
        GUILayout.Label($"Ammo: {currentAmmo}/{magazineSize}");
        GUILayout.Label($"Fire Mode: {fireMode}");
        GUILayout.Label($"Recoil: {currentRecoil.magnitude:F2}");
        
        if (isReloading)
        {
            GUI.color = Color.yellow;
            GUILayout.Label("RELOADING...");
        }

        GUILayout.EndArea();
    }

    #endregion
}

/// <summary>
/// Modos de disparo
/// </summary>
public enum FireMode
{
    Semi,   // Semiautomático (1 tiro por clique)
    Auto,   // Automático (segura = dispara)
    Burst   // Rajada (3 tiros por clique)
}