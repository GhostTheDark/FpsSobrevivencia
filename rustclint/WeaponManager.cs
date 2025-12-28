using UnityEngine;
using RustlikeClient.Network;

namespace RustlikeGame.Combat
{
    /// <summary>
    /// Gerencia armas equipadas do jogador local
    /// </summary>
    public class WeaponManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private AimSystem aimSystem;
        [SerializeField] private Transform weaponHolder;
        [SerializeField] private NetworkManager networkManager;
        
        [Header("Weapon Database")]
        [SerializeField] private WeaponData[] availableWeapons;

        // Current weapon state
        private WeaponData _currentWeapon;
        private GameObject _currentWeaponObject;
        private int _currentAmmo;
        private int _reserveAmmo;
        private bool _isReloading;
        private float _lastFireTime;
        private float _reloadStartTime;

        // Events
        public System.Action<WeaponData> OnWeaponEquipped;
        public System.Action OnWeaponFired;
        public System.Action OnReloadStarted;
        public System.Action OnReloadCompleted;
        public System.Action<int, int> OnAmmoChanged; // current, reserve

        private void Update()
        {
            if (_currentWeapon == null) return;

            HandleInput();
            UpdateReload();
        }

        /// <summary>
        /// Equipa uma arma pelo ItemId
        /// </summary>
        public void EquipWeapon(int weaponItemId, int slotIndex)
        {
            // Busca arma no banco de dados
            WeaponData weaponData = GetWeaponData(weaponItemId);
            if (weaponData == null)
            {
                Debug.LogError($"[WeaponManager] Arma {weaponItemId} não encontrada no banco de dados!");
                return;
            }

            // Remove arma anterior
            if (_currentWeaponObject != null)
            {
                Destroy(_currentWeaponObject);
            }

            // Equipa nova arma
            _currentWeapon = weaponData;
            _currentAmmo = weaponData.MagazineSize;
            _reserveAmmo = 0; // Será atualizado pelo servidor

            // Instancia visual da arma
            if (weaponData.WeaponPrefab != null)
            {
                _currentWeaponObject = Instantiate(weaponData.WeaponPrefab, weaponHolder);
                _currentWeaponObject.transform.localPosition = Vector3.zero;
                _currentWeaponObject.transform.localRotation = Quaternion.identity;
            }

            Debug.Log($"[WeaponManager] Equipou: {weaponData.WeaponName}");
            OnWeaponEquipped?.Invoke(_currentWeapon);

            // Envia para servidor
            networkManager.SendWeaponEquip(weaponItemId, slotIndex);
        }

        /// <summary>
        /// Desequipa arma atual
        /// </summary>
        public void UnequipWeapon()
        {
            if (_currentWeaponObject != null)
            {
                Destroy(_currentWeaponObject);
            }

            _currentWeapon = null;
            _currentAmmo = 0;
            _reserveAmmo = 0;

            OnWeaponEquipped?.Invoke(null);
            networkManager.SendWeaponEquip(0, -1);
        }

        /// <summary>
        /// Handle de input do jogador
        /// </summary>
        private void HandleInput()
        {
            // Fire
            if (_currentWeapon.IsAutomatic)
            {
                if (Input.GetMouseButton(0))
                {
                    TryFire();
                }
            }
            else
            {
                if (Input.GetMouseButtonDown(0))
                {
                    TryFire();
                }
            }

            // Reload
            if (Input.GetKeyDown(KeyCode.R))
            {
                TryReload();
            }
        }

        /// <summary>
        /// Tenta disparar a arma
        /// </summary>
        private void TryFire()
        {
            if (_currentWeapon == null) return;
            if (_isReloading) return;

            // Verifica fire rate
            float fireInterval = _currentWeapon.GetFireInterval();
            if (Time.time - _lastFireTime < fireInterval)
            {
                return;
            }

            // Arma ranged - verifica munição
            if (_currentWeapon.IsRanged())
            {
                if (_currentAmmo <= 0)
                {
                    // Som de arma vazia
                    if (_currentWeapon.EmptySound != null)
                    {
                        AudioSource.PlayClipAtPoint(_currentWeapon.EmptySound, transform.position);
                    }
                    return;
                }
            }

            _lastFireTime = Time.time;

            // Dispara baseado no tipo
            if (_currentWeapon.IsRanged())
            {
                FireRanged();
            }
            else if (_currentWeapon.IsMelee())
            {
                FireMelee();
            }

            OnWeaponFired?.Invoke();
        }

        /// <summary>
        /// Dispara arma ranged (tiro)
        /// </summary>
        private void FireRanged()
        {
            // Pega alvo da mira
            var target = aimSystem.GetCurrentTarget();
            Vector3 aimPoint = aimSystem.GetAimPoint();
            Vector3 shootDirection = aimSystem.GetAimDirection();
            float distance = aimSystem.GetDistanceToTarget();

            // Detecta hitbox
            HitboxType hitbox = HitboxType.Body;
            int targetPlayerId = -1;

            if (target != null)
            {
                var networkPlayer = target.GetComponentInParent<NetworkPlayer>();
                if (networkPlayer != null)
                {
                    targetPlayerId = networkPlayer.PlayerId;
                    hitbox = aimSystem.DetectHitbox(aimSystem.GetCurrentHit());
                }
            }

            // Efeitos visuais
            PlayFireEffects();
            ApplyRecoil();

            // Envia para servidor
            networkManager.SendRangedAttack(
                targetPlayerId,
                _currentWeapon.ItemId,
                hitbox,
                shootDirection,
                distance
            );

            // Consome munição localmente (será confirmado pelo servidor)
            _currentAmmo = Mathf.Max(0, _currentAmmo - 1);
            OnAmmoChanged?.Invoke(_currentAmmo, _reserveAmmo);
        }

        /// <summary>
        /// Ataque melee
        /// </summary>
        private void FireMelee()
        {
            // Pega alvo da mira
            var target = aimSystem.GetCurrentTarget();
            Vector3 direction = aimSystem.GetAimDirection();

            // Verifica se acertou alguém na range
            if (target != null && aimSystem.GetDistanceToTarget() <= _currentWeapon.Range)
            {
                var networkPlayer = target.GetComponentInParent<NetworkPlayer>();
                if (networkPlayer != null)
                {
                    HitboxType hitbox = aimSystem.DetectHitbox(aimSystem.GetCurrentHit());

                    // Envia para servidor
                    networkManager.SendMeleeAttack(
                        networkPlayer.PlayerId,
                        _currentWeapon.ItemId,
                        hitbox,
                        direction
                    );
                }
            }

            // Efeitos visuais
            PlayMeleeEffects();
        }

        /// <summary>
        /// Tenta recarregar a arma
        /// </summary>
        private void TryReload()
        {
            if (_currentWeapon == null) return;
            if (!_currentWeapon.IsRanged()) return;
            if (_isReloading) return;
            if (_currentAmmo >= _currentWeapon.MagazineSize) return;
            if (_reserveAmmo <= 0) return;

            StartReload();
        }

        /// <summary>
        /// Inicia recarga
        /// </summary>
        private void StartReload()
        {
            _isReloading = true;
            _reloadStartTime = Time.time;

            // Som de reload
            if (_currentWeapon.ReloadSound != null)
            {
                AudioSource.PlayClipAtPoint(_currentWeapon.ReloadSound, transform.position);
            }

            // Envia para servidor
            networkManager.SendWeaponReload(_currentWeapon.ItemId);

            OnReloadStarted?.Invoke();
            Debug.Log($"[WeaponManager] Recarregando {_currentWeapon.WeaponName}...");
        }

        /// <summary>
        /// Atualiza estado de reload
        /// </summary>
        private void UpdateReload()
        {
            if (!_isReloading) return;

            float elapsed = Time.time - _reloadStartTime;
            if (elapsed >= _currentWeapon.ReloadTime)
            {
                CompleteReload();
            }
        }

        /// <summary>
        /// Completa recarga
        /// </summary>
        private void CompleteReload()
        {
            _isReloading = false;

            // Calcula quantos bullets recarregar
            int ammoNeeded = _currentWeapon.MagazineSize - _currentAmmo;
            int ammoToReload = Mathf.Min(ammoNeeded, _reserveAmmo);

            _currentAmmo += ammoToReload;
            _reserveAmmo -= ammoToReload;

            OnReloadCompleted?.Invoke();
            OnAmmoChanged?.Invoke(_currentAmmo, _reserveAmmo);

            Debug.Log($"[WeaponManager] Recarga completa! {_currentAmmo}/{_currentWeapon.MagazineSize}");
        }

        /// <summary>
        /// Atualiza estado da arma vindo do servidor
        /// </summary>
        public void UpdateWeaponState(int weaponItemId, int currentAmmo, int reserveAmmo, bool isReloading)
        {
            _currentAmmo = currentAmmo;
            _reserveAmmo = reserveAmmo;
            _isReloading = isReloading;

            OnAmmoChanged?.Invoke(_currentAmmo, _reserveAmmo);
        }

        /// <summary>
        /// Efeitos de tiro
        /// </summary>
        private void PlayFireEffects()
        {
            // Som
            if (_currentWeapon.FireSound != null)
            {
                AudioSource.PlayClipAtPoint(_currentWeapon.FireSound, transform.position, 0.5f);
            }

            // Muzzle flash
            if (_currentWeapon.MuzzleFlashPrefab != null && _currentWeaponObject != null)
            {
                var muzzle = Instantiate(_currentWeapon.MuzzleFlashPrefab, 
                    _currentWeaponObject.transform.position, 
                    _currentWeaponObject.transform.rotation);
                Destroy(muzzle, 0.1f);
            }

            // Efeito de impacto
            Vector3 impactPoint = aimSystem.GetAimPoint();
            if (_currentWeapon.ImpactEffectPrefab != null)
            {
                var impact = Instantiate(_currentWeapon.ImpactEffectPrefab, impactPoint, Quaternion.identity);
                Destroy(impact, 2f);
            }
        }

        /// <summary>
        /// Efeitos de melee
        /// </summary>
        private void PlayMeleeEffects()
        {
            // Som de swing
            if (_currentWeapon.FireSound != null)
            {
                AudioSource.PlayClipAtPoint(_currentWeapon.FireSound, transform.position);
            }

            // TODO: Animação de swing
        }

        /// <summary>
        /// Aplica recoil na câmera
        /// </summary>
        private void ApplyRecoil()
        {
            // TODO: Implementar recoil na câmera
            float recoil = _currentWeapon.RecoilAmount;
        }

        /// <summary>
        /// Busca WeaponData pelo ItemId
        /// </summary>
        private WeaponData GetWeaponData(int itemId)
        {
            foreach (var weapon in availableWeapons)
            {
                if (weapon.ItemId == itemId)
                {
                    return weapon;
                }
            }
            return null;
        }

        // Getters
        public WeaponData GetCurrentWeapon() => _currentWeapon;
        public int GetCurrentAmmo() => _currentAmmo;
        public int GetReserveAmmo() => _reserveAmmo;
        public bool IsReloading() => _isReloading;
        public float GetReloadProgress() => _isReloading ? (Time.time - _reloadStartTime) / _currentWeapon.ReloadTime : 0f;
    }
}
