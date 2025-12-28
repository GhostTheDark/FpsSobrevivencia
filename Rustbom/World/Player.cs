using System;
using RustlikeServer.Combat;

namespace RustlikeServer.World
{
    /// <summary>
    /// ⭐ ATUALIZADO COM SISTEMA DE COMBATE
    /// </summary>
    public class Player
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public Vector3 Position { get; set; }
        public Vector2 Rotation { get; set; }
        public DateTime LastHeartbeat { get; set; }
        public bool IsConnected { get; set; }

        // Sistema de Stats
        public PlayerStats Stats { get; private set; }

        // Sistema de Inventário
        public PlayerInventory Inventory { get; private set; }

        // ⭐ NOVO: Sistema de Combate
        public PlayerCombat Combat { get; private set; }

        public Player(int id, string name)
        {
            Id = id;
            Name = name;
            Position = new Vector3(0, 1, 0);
            Rotation = new Vector2(0, 0);
            LastHeartbeat = DateTime.Now;
            IsConnected = true;

            Stats = new PlayerStats();
            Inventory = new PlayerInventory();
            Combat = new PlayerCombat(this); // ⭐ NOVO
        }

        public void UpdatePosition(float x, float y, float z)
        {
            Position = new Vector3(x, y, z);
        }

        public void UpdateRotation(float yaw, float pitch)
        {
            Rotation = new Vector2(yaw, pitch);
        }

        public void UpdateHeartbeat()
        {
            LastHeartbeat = DateTime.Now;
        }

        public bool IsTimedOut()
        {
            return (DateTime.Now - LastHeartbeat).TotalSeconds > 10;
        }

        public void UpdateStats()
        {
            Stats.Update();
        }

        public void TakeDamage(float amount, DamageType type = DamageType.Generic)
        {
            Stats.TakeDamage(amount, type);
        }

        public bool IsDead()
        {
            return Stats.IsDead;
        }

        public void Respawn()
        {
            Stats.Respawn();
            Position = new Vector3(0, 1, 0);
            
            // ⭐ NOVO: Reseta estado de combate
            Combat.OnRespawn();
            
            Console.WriteLine($"[Player] {Name} respawnou");
        }
    }

    /// <summary>
    /// ⭐ NOVO: Gerencia estado de combate do jogador
    /// </summary>
    public class PlayerCombat
    {
        private Player _player;
        
        // Arma equipada
        public WeaponState EquippedWeapon { get; private set; }
        public int EquippedWeaponSlot { get; private set; } = -1;

        // Estado de combate
        public bool IsInCombat { get; private set; }
        public DateTime LastCombatTime { get; private set; }
        public DateTime LastDeathTime { get; private set; }
        private const float COMBAT_TIMEOUT = 30f; // 30 segundos sem combate

        // Cooldowns
        private DateTime _lastAttackTime = DateTime.MinValue;
        private const float MIN_ATTACK_INTERVAL = 0.1f;

        // Estatísticas
        public int Kills { get; private set; }
        public int Deaths { get; private set; }
        public float TotalDamageDealt { get; private set; }
        public float TotalDamageTaken { get; private set; }

        public PlayerCombat(Player player)
        {
            _player = player;
        }

        /// <summary>
        /// Equipa uma arma do inventário
        /// </summary>
        public bool EquipWeapon(int weaponItemId, int slotIndex)
        {
            // Verifica se o item existe no inventário
            var slot = _player.Inventory.GetSlot(slotIndex);
            if (slot == null || slot.ItemId != weaponItemId)
            {
                Console.WriteLine($"[PlayerCombat] Arma não encontrada no slot {slotIndex}");
                return false;
            }

            // Verifica se é uma arma válida
            var weaponDef = WeaponDatabase.GetWeapon(weaponItemId);
            if (weaponDef == null)
            {
                Console.WriteLine($"[PlayerCombat] Item {weaponItemId} não é uma arma");
                return false;
            }

            // Equipa arma
            EquippedWeapon = new WeaponState(weaponDef);
            EquippedWeaponSlot = slotIndex;

            // Carrega munição do inventário (se for ranged)
            if (weaponDef.Type == WeaponType.Ranged && weaponDef.AmmoType != AmmoType.None)
            {
                int ammoItemId = GetAmmoItemId(weaponDef.AmmoType);
                int ammoCount = _player.Inventory.CountItem(ammoItemId);
                EquippedWeapon.ReserveAmmo = ammoCount;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[PlayerCombat] {_player.Name} equipou {weaponDef.Name}");
            Console.ResetColor();

            return true;
        }

        /// <summary>
        /// Desequipa arma atual
        /// </summary>
        public void UnequipWeapon()
        {
            if (EquippedWeapon != null)
            {
                Console.WriteLine($"[PlayerCombat] {_player.Name} desequipou {EquippedWeapon.Definition.Name}");
            }

            EquippedWeapon = null;
            EquippedWeaponSlot = -1;
        }

        /// <summary>
        /// Recarrega arma atual
        /// </summary>
        public bool ReloadWeapon()
        {
            if (EquippedWeapon == null)
            {
                return false;
            }

            if (!EquippedWeapon.StartReload())
            {
                return false;
            }

            Console.WriteLine($"[PlayerCombat] {_player.Name} está recarregando {EquippedWeapon.Definition.Name}");
            return true;
        }

        /// <summary>
        /// Atualiza estado de reload
        /// </summary>
        public void UpdateReload()
        {
            if (EquippedWeapon == null || !EquippedWeapon.IsReloading)
            {
                return;
            }

            if (EquippedWeapon.IsReloadComplete())
            {
                EquippedWeapon.CompleteReload();
                
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"[PlayerCombat] {_player.Name} completou recarga: {EquippedWeapon.CurrentAmmo}/{EquippedWeapon.Definition.MagazineSize}");
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Verifica se pode atacar
        /// </summary>
        public bool CanAttack()
        {
            float elapsed = (float)(DateTime.Now - _lastAttackTime).TotalSeconds;
            return elapsed >= MIN_ATTACK_INTERVAL;
        }

        /// <summary>
        /// Registra ataque realizado
        /// </summary>
        public void RegisterAttack()
        {
            _lastAttackTime = DateTime.Now;
            EnterCombat();
        }

        /// <summary>
        /// Registra dano causado
        /// </summary>
        public void RegisterDamageDealt(float damage)
        {
            TotalDamageDealt += damage;
            EnterCombat();
        }

        /// <summary>
        /// Registra dano recebido
        /// </summary>
        public void RegisterDamageTaken(float damage)
        {
            TotalDamageTaken += damage;
            EnterCombat();
        }

        /// <summary>
        /// Registra kill
        /// </summary>
        public void RegisterKill()
        {
            Kills++;
            Console.WriteLine($"[PlayerCombat] {_player.Name} matou alguém! Total kills: {Kills}");
        }

        /// <summary>
        /// Registra morte
        /// </summary>
        public void RegisterDeath()
        {
            Deaths++;
            LastDeathTime = DateTime.Now;
            IsInCombat = false;
            
            Console.WriteLine($"[PlayerCombat] {_player.Name} morreu! Total deaths: {Deaths}");
        }

        /// <summary>
        /// Entra em combate
        /// </summary>
        private void EnterCombat()
        {
            IsInCombat = true;
            LastCombatTime = DateTime.Now;
        }

        /// <summary>
        /// Atualiza estado de combate
        /// </summary>
        public void Update()
        {
            // Sai de combate após timeout
            if (IsInCombat)
            {
                float elapsed = (float)(DateTime.Now - LastCombatTime).TotalSeconds;
                if (elapsed >= COMBAT_TIMEOUT)
                {
                    IsInCombat = false;
                    Console.WriteLine($"[PlayerCombat] {_player.Name} saiu de combate");
                }
            }

            // Atualiza reload
            UpdateReload();
        }

        /// <summary>
        /// Reseta estado ao respawnar
        /// </summary>
        public void OnRespawn()
        {
            UnequipWeapon();
            IsInCombat = false;
        }

        /// <summary>
        /// Mapeia tipo de munição para item ID
        /// </summary>
        private int GetAmmoItemId(AmmoType ammoType)
        {
            return ammoType switch
            {
                AmmoType.Arrow => 305,          // TODO: Definir IDs corretos
                AmmoType.PistolAmmo => 304,
                AmmoType.RifleAmmo => 306,
                AmmoType.Shotgun => 307,
                _ => -1
            };
        }

        /// <summary>
        /// Pega estatísticas de combate
        /// </summary>
        public string GetStats()
        {
            float kd = Deaths > 0 ? (float)Kills / Deaths : Kills;
            return $"K/D: {Kills}/{Deaths} ({kd:F2}) | Dano: {TotalDamageDealt:F0} / {TotalDamageTaken:F0}";
        }
    }
}