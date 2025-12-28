using System;
using System.Collections.Generic;

namespace RustlikeServer.Combat
{
    /// <summary>
    /// 🔫 SISTEMA DE ARMAS - Definições e dados de armas
    /// </summary>
    
    /// <summary>
    /// Tipos de armas
    /// </summary>
    public enum WeaponType
    {
        None = 0,           // Mãos nuas
        Melee = 1,          // Armas corpo a corpo (faca, espada, lança)
        Ranged = 2,         // Armas de longo alcance (arco, pistola, rifle)
        Throwable = 3,      // Armas arremessáveis (granada, lança)
        Tool = 4            // Ferramentas usadas como armas (machado, picareta)
    }

    /// <summary>
    /// Tipos de munição
    /// </summary>
    public enum AmmoType
    {
        None = 0,
        Arrow = 1,          // Flechas (arco)
        PistolAmmo = 2,     // Munição de pistola
        RifleAmmo = 3,      // Munição de rifle
        Shotgun = 4,        // Cartuchos de espingarda
        Explosive = 5       // Explosivos
    }

    /// <summary>
    /// Tipos de dano
    /// </summary>
    public enum DamageType
    {
        Generic = 0,        // Dano genérico
        Melee = 1,          // Dano corpo a corpo
        Bullet = 2,         // Dano de bala
        Explosion = 3,      // Dano de explosão
        Fall = 4,           // Dano de queda
        Hunger = 5,         // Dano de fome
        Thirst = 6,         // Dano de sede
        Cold = 7,           // Dano de frio
        Heat = 8,           // Dano de calor
        Radiation = 9,      // Dano de radiação
        Bleeding = 10       // Dano de sangramento
    }

    /// <summary>
    /// Parte do corpo atingida
    /// </summary>
    public enum HitboxType
    {
        Body = 0,           // Corpo (dano normal)
        Head = 1,           // Cabeça (dano crítico)
        Legs = 2,           // Pernas (dano reduzido)
        Arms = 3            // Braços (dano reduzido)
    }

    /// <summary>
    /// Definição de uma arma
    /// </summary>
    [Serializable]
    public class WeaponDefinition
    {
        public int ItemId { get; set; }                 // ID do item (no ItemDatabase)
        public string Name { get; set; }
        public WeaponType Type { get; set; }
        public DamageType DamageType { get; set; }
        
        // Dano
        public float BaseDamage { get; set; }           // Dano base
        public float HeadshotMultiplier { get; set; }   // Multiplicador de headshot (ex: 2.0 = 200%)
        public float LegShotMultiplier { get; set; }    // Multiplicador de perna (ex: 0.5 = 50%)
        
        // Range
        public float Range { get; set; }                // Alcance máximo
        public float OptimalRange { get; set; }         // Alcance ótimo (sem falloff)
        
        // Fire Rate
        public float FireRate { get; set; }             // Taxa de tiro (tiros por segundo)
        public bool IsAutomatic { get; set; }           // Se é automática
        
        // Munição
        public AmmoType AmmoType { get; set; }
        public int MagazineSize { get; set; }           // Tamanho do pente
        public float ReloadTime { get; set; }           // Tempo de recarga
        
        // Precisão
        public float Accuracy { get; set; }             // Precisão base (0-1, 1 = perfeito)
        public float RecoilAmount { get; set; }         // Recuo por tiro
        
        // Melee específico
        public float SwingSpeed { get; set; }           // Velocidade de ataque melee
        public float KnockbackForce { get; set; }       // Força de empurrão

        public override string ToString()
        {
            return $"{Name} ({Type}) - Damage: {BaseDamage}, Range: {Range}m";
        }
    }

    /// <summary>
    /// Estado de uma arma equipada por um player
    /// </summary>
    public class WeaponState
    {
        public WeaponDefinition Definition { get; set; }
        public int CurrentAmmo { get; set; }            // Munição no pente
        public int ReserveAmmo { get; set; }            // Munição reserva
        public DateTime LastShotTime { get; set; }
        public DateTime LastReloadTime { get; set; }
        public bool IsReloading { get; set; }
        public float CurrentRecoil { get; set; }

        public WeaponState(WeaponDefinition def)
        {
            Definition = def;
            CurrentAmmo = def.MagazineSize;
            ReserveAmmo = 0;
            LastShotTime = DateTime.MinValue;
            LastReloadTime = DateTime.MinValue;
            IsReloading = false;
            CurrentRecoil = 0f;
        }

        /// <summary>
        /// Verifica se pode atirar
        /// </summary>
        public bool CanFire()
        {
            if (IsReloading) return false;
            if (CurrentAmmo <= 0) return false;
            
            // Verifica fire rate
            float timeSinceLastShot = (float)(DateTime.Now - LastShotTime).TotalSeconds;
            float fireInterval = 1f / Definition.FireRate;
            
            return timeSinceLastShot >= fireInterval;
        }

        /// <summary>
        /// Consome munição
        /// </summary>
        public void ConsumeAmmo()
        {
            CurrentAmmo = Math.Max(0, CurrentAmmo - 1);
            LastShotTime = DateTime.Now;
        }

        /// <summary>
        /// Recarrega arma
        /// </summary>
        public bool StartReload()
        {
            if (IsReloading) return false;
            if (CurrentAmmo >= Definition.MagazineSize) return false;
            if (ReserveAmmo <= 0) return false;

            IsReloading = true;
            LastReloadTime = DateTime.Now;
            return true;
        }

        /// <summary>
        /// Completa recarga
        /// </summary>
        public void CompleteReload()
        {
            int needed = Definition.MagazineSize - CurrentAmmo;
            int toReload = Math.Min(needed, ReserveAmmo);

            CurrentAmmo += toReload;
            ReserveAmmo -= toReload;
            IsReloading = false;
        }

        /// <summary>
        /// Verifica se reload está completo
        /// </summary>
        public bool IsReloadComplete()
        {
            if (!IsReloading) return false;
            
            float elapsed = (float)(DateTime.Now - LastReloadTime).TotalSeconds;
            return elapsed >= Definition.ReloadTime;
        }
    }

    /// <summary>
    /// Database de armas
    /// </summary>
    public static class WeaponDatabase
    {
        private static Dictionary<int, WeaponDefinition> _weapons = new Dictionary<int, WeaponDefinition>();

        static WeaponDatabase()
        {
            InitializeWeapons();
        }

        private static void InitializeWeapons()
        {
            // === MÃOS NUAS ===
            AddWeapon(new WeaponDefinition
            {
                ItemId = 0,
                Name = "Fists",
                Type = WeaponType.None,
                DamageType = DamageType.Melee,
                BaseDamage = 10f,
                HeadshotMultiplier = 1.5f,
                LegShotMultiplier = 0.8f,
                Range = 2f,
                OptimalRange = 2f,
                FireRate = 2f,
                IsAutomatic = false,
                SwingSpeed = 0.5f,
                KnockbackForce = 2f
            });

            // === ARMAS MELEE ===

            // Lança de Madeira
            AddWeapon(new WeaponDefinition
            {
                ItemId = 301,
                Name = "Wooden Spear",
                Type = WeaponType.Melee,
                DamageType = DamageType.Melee,
                BaseDamage = 40f,
                HeadshotMultiplier = 2.0f,
                LegShotMultiplier = 0.7f,
                Range = 3f,
                OptimalRange = 3f,
                FireRate = 1f,
                IsAutomatic = false,
                SwingSpeed = 1f,
                KnockbackForce = 5f
            });

            // Machado de Pedra (também é ferramenta)
            AddWeapon(new WeaponDefinition
            {
                ItemId = 201,
                Name = "Stone Hatchet",
                Type = WeaponType.Tool,
                DamageType = DamageType.Melee,
                BaseDamage = 30f,
                HeadshotMultiplier = 1.8f,
                LegShotMultiplier = 0.8f,
                Range = 2.5f,
                OptimalRange = 2.5f,
                FireRate = 1.2f,
                IsAutomatic = false,
                SwingSpeed = 0.8f,
                KnockbackForce = 3f
            });

            // Machado de Metal
            AddWeapon(new WeaponDefinition
            {
                ItemId = 203,
                Name = "Metal Hatchet",
                Type = WeaponType.Tool,
                DamageType = DamageType.Melee,
                BaseDamage = 45f,
                HeadshotMultiplier = 2.0f,
                LegShotMultiplier = 0.8f,
                Range = 2.5f,
                OptimalRange = 2.5f,
                FireRate = 1.5f,
                IsAutomatic = false,
                SwingSpeed = 1f,
                KnockbackForce = 4f
            });

            // === ARMAS RANGED ===

            // Arco
            AddWeapon(new WeaponDefinition
            {
                ItemId = 302,
                Name = "Hunting Bow",
                Type = WeaponType.Ranged,
                DamageType = DamageType.Bullet,
                BaseDamage = 50f,
                HeadshotMultiplier = 3.0f,
                LegShotMultiplier = 0.6f,
                Range = 50f,
                OptimalRange = 30f,
                FireRate = 0.8f,
                IsAutomatic = false,
                AmmoType = AmmoType.Arrow,
                MagazineSize = 1,
                ReloadTime = 1.5f,
                Accuracy = 0.85f,
                RecoilAmount = 0.2f
            });

            // Revólver
            AddWeapon(new WeaponDefinition
            {
                ItemId = 303,
                Name = "Revolver",
                Type = WeaponType.Ranged,
                DamageType = DamageType.Bullet,
                BaseDamage = 35f,
                HeadshotMultiplier = 2.5f,
                LegShotMultiplier = 0.7f,
                Range = 40f,
                OptimalRange = 20f,
                FireRate = 3f,
                IsAutomatic = false,
                AmmoType = AmmoType.PistolAmmo,
                MagazineSize = 6,
                ReloadTime = 2.5f,
                Accuracy = 0.75f,
                RecoilAmount = 0.4f
            });

            Console.WriteLine($"[WeaponDatabase] {_weapons.Count} armas carregadas");
        }

        private static void AddWeapon(WeaponDefinition weapon)
        {
            _weapons[weapon.ItemId] = weapon;
        }

        public static WeaponDefinition GetWeapon(int itemId)
        {
            return _weapons.TryGetValue(itemId, out var weapon) ? weapon : null;
        }

        public static bool IsWeapon(int itemId)
        {
            return _weapons.ContainsKey(itemId);
        }

        public static WeaponDefinition[] GetAllWeapons()
        {
            return _weapons.Values.ToArray();
        }
    }

}