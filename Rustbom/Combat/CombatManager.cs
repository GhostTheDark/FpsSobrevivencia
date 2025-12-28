using System;
using System.Collections.Generic;
using System.Linq;
using RustlikeServer.World;

namespace RustlikeServer.Combat
{
    /// <summary>
    /// Gerenciador principal de combate - Server authoritative
    /// </summary>
    public class CombatManager
    {
        private List<CombatLogEntry> _combatLog;
        private const int MAX_LOG_ENTRIES = 100;

        public CombatManager()
        {
            _combatLog = new List<CombatLogEntry>();
            Console.WriteLine("[CombatManager] Sistema de combate inicializado");
        }

        /// <summary>
        /// Processa ataque melee (corpo a corpo)
        /// </summary>
        public AttackResult ProcessMeleeAttack(
            Player attacker,
            Player victim,
            int weaponItemId,
            HitboxType hitbox,
            Vector3 direction)
        {
            // Validações básicas
            if (attacker == null || victim == null)
            {
                return new AttackResult { Success = false, Message = "Jogadores inválidos" };
            }

            if (attacker.IsDead() || victim.IsDead())
            {
                return new AttackResult { Success = false, Message = "Um dos jogadores está morto" };
            }

            // Anti-spam: Verifica se pode atacar
            if (!attacker.Combat.CanAttack())
            {
                return new AttackResult { Success = false, Message = "Ataque muito rápido (spam)" };
            }

            // Pega definição da arma
            var weaponDef = WeaponDatabase.GetWeapon(weaponItemId);
            if (weaponDef == null)
            {
                return new AttackResult { Success = false, Message = "Arma inválida" };
            }

            // Verifica se é arma melee
            if (weaponDef.Type != WeaponType.Melee && weaponDef.Type != WeaponType.Tool)
            {
                return new AttackResult { Success = false, Message = "Não é uma arma melee" };
            }

            // Calcula distância
            float distance = attacker.Position.Distance(victim.Position);
            if (distance > weaponDef.Range)
            {
                return new AttackResult 
                { 
                    Success = false, 
                    Message = $"Muito longe (dist: {distance:F1}m, max: {weaponDef.Range}m)" 
                };
            }

            // Calcula dano
            float damage = CalculateDamage(weaponDef, hitbox);

            // Aplica dano
            bool wasKilled = ApplyDamage(victim, damage, World.DamageType.Melee, attacker.Id, attacker.Name);

            // Registra ataque
            attacker.Combat.RegisterAttack();
            attacker.Combat.RegisterDamageDealt(damage);
            victim.Combat.RegisterDamageTaken(damage);

            // Log de combate
            LogCombat(attacker.Id, attacker.Name, victim.Id, victim.Name, 
                     weaponDef.Name, damage, hitbox, wasKilled, distance);

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[CombatManager] ⚔️ {attacker.Name} atingiu {victim.Name} com {weaponDef.Name}");
            Console.WriteLine($"   → Hitbox: {hitbox} | Dano: {damage:F1} | Morte: {wasKilled}");
            Console.ResetColor();

            return new AttackResult
            {
                Success = true,
                Message = "Hit!",
                DamageDealt = damage,
                WasKilled = wasKilled,
                Hitbox = hitbox,
                Distance = distance
            };
        }

        /// <summary>
        /// Processa ataque ranged (tiros)
        /// </summary>
        public AttackResult ProcessRangedAttack(
            Player attacker,
            Player victim,
            WeaponState weaponState,
            HitboxType hitbox,
            Vector3 shootDirection,
            float distance)
        {
            // Validações básicas
            if (attacker == null || victim == null || weaponState == null)
            {
                return new AttackResult { Success = false, Message = "Dados inválidos" };
            }

            if (attacker.IsDead() || victim.IsDead())
            {
                return new AttackResult { Success = false, Message = "Um dos jogadores está morto" };
            }

            var weaponDef = weaponState.Definition;

            // Verifica se é arma ranged
            if (weaponDef.Type != WeaponType.Ranged)
            {
                return new AttackResult { Success = false, Message = "Não é uma arma ranged" };
            }

            // Verifica fire rate
            if (!weaponState.CanFire())
            {
                return new AttackResult { Success = false, Message = "Fire rate muito rápido" };
            }

            // Verifica munição
            if (weaponState.CurrentAmmo <= 0)
            {
                return new AttackResult 
                { 
                    Success = false, 
                    Message = "Sem munição",
                    RemainingAmmo = 0
                };
            }

            // Verifica distância
            if (distance > weaponDef.Range)
            {
                return new AttackResult 
                { 
                    Success = false, 
                    Message = $"Muito longe (max: {weaponDef.Range}m)",
                    RemainingAmmo = weaponState.CurrentAmmo
                };
            }

            // Consome munição
            weaponState.ConsumeAmmo();

            // Calcula dano base
            float damage = CalculateDamage(weaponDef, hitbox);

            // Aplica damage falloff (reduz dano com distância)
            if (distance > weaponDef.OptimalRange)
            {
                float falloffPercent = (distance - weaponDef.OptimalRange) / 
                                      (weaponDef.Range - weaponDef.OptimalRange);
                falloffPercent = Math.Clamp(falloffPercent, 0f, 1f);
                float damageReduction = falloffPercent * 0.5f; // Até 50% de redução
                damage *= (1f - damageReduction);
            }

            // Aplica dano
            bool wasKilled = ApplyDamage(victim, damage, World.DamageType.Bullet, attacker.Id, attacker.Name);

            // Registra ataque
            attacker.Combat.RegisterAttack();
            attacker.Combat.RegisterDamageDealt(damage);
            victim.Combat.RegisterDamageTaken(damage);

            // Log de combate
            LogCombat(attacker.Id, attacker.Name, victim.Id, victim.Name, 
                     weaponDef.Name, damage, hitbox, wasKilled, distance);

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[CombatManager] 🔫 {attacker.Name} atirou em {victim.Name} com {weaponDef.Name}");
            Console.WriteLine($"   → Hitbox: {hitbox} | Dano: {damage:F1} | Morte: {wasKilled} | Dist: {distance:F1}m");
            Console.ResetColor();

            return new AttackResult
            {
                Success = true,
                Message = "Hit!",
                DamageDealt = damage,
                WasKilled = wasKilled,
                Hitbox = hitbox,
                Distance = distance,
                RemainingAmmo = weaponState.CurrentAmmo
            };
        }

        /// <summary>
        /// Calcula dano com multiplicadores de hitbox
        /// </summary>
        private float CalculateDamage(WeaponDefinition weapon, HitboxType hitbox)
        {
            float baseDamage = weapon.BaseDamage;

            switch (hitbox)
            {
                case HitboxType.Head:
                    return baseDamage * weapon.HeadshotMultiplier;
                
                case HitboxType.Legs:
                case HitboxType.Arms:
                    return baseDamage * weapon.LegShotMultiplier;
                
                case HitboxType.Body:
                default:
                    return baseDamage;
            }
        }

        /// <summary>
        /// Aplica dano no player
        /// </summary>
        private bool ApplyDamage(Player victim, float damage, World.DamageType damageType, int attackerId, string attackerName)
        {
            // Aplica dano
            victim.TakeDamage(damage, damageType);

            // Verifica se morreu
            if (victim.IsDead())
            {
                HandlePlayerDeath(victim, attackerId, attackerName);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Handle de morte do player
        /// </summary>
        private void HandlePlayerDeath(Player victim, int killerId, string killerName)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[CombatManager] ☠️ {victim.Name} foi morto por {killerName}");
            Console.ResetColor();

            // TODO: Drop de itens
            // TODO: Broadcast de morte
        }

        /// <summary>
        /// Registra evento de combate no log
        /// </summary>
        private void LogCombat(
            int attackerId, string attackerName,
            int victimId, string victimName,
            string weaponUsed, float damage,
            HitboxType hitbox, bool wasKilled, float distance)
        {
            var entry = new CombatLogEntry
            {
                AttackerId = attackerId,
                AttackerName = attackerName,
                VictimId = victimId,
                VictimName = victimName,
                WeaponUsed = weaponUsed,
                DamageDealt = damage,
                Hitbox = hitbox,
                WasKilled = wasKilled,
                Distance = distance,
                Timestamp = DateTime.Now
            };

            _combatLog.Add(entry);

            // Mantém apenas os últimos N eventos
            if (_combatLog.Count > MAX_LOG_ENTRIES)
            {
                _combatLog.RemoveAt(0);
            }
        }

        /// <summary>
        /// Pega log de combate recente
        /// </summary>
        public List<CombatLogEntry> GetRecentCombatLog(int count = 10)
        {
            return _combatLog.TakeLast(count).ToList();
        }

        /// <summary>
        /// Pega mortes de um player específico
        /// </summary>
        public List<CombatLogEntry> GetPlayerDeaths(int playerId)
        {
            return _combatLog.Where(e => e.VictimId == playerId && e.WasKilled).ToList();
        }

        /// <summary>
        /// Pega kills de um player específico
        /// </summary>
        public List<CombatLogEntry> GetPlayerKills(int playerId)
        {
            return _combatLog.Where(e => e.AttackerId == playerId && e.WasKilled).ToList();
        }

        /// <summary>
        /// Calcula distância 2D (ignora Y)
        /// </summary>
        public static float Distance2D(Vector3 a, Vector3 b)
        {
            float dx = a.X - b.X;
            float dz = a.Z - b.Z;
            return (float)Math.Sqrt(dx * dx + dz * dz);
        }
    }

    // ==================== CLASSES DE RESULTADO ====================

    /// <summary>
    /// Resultado de um ataque
    /// </summary>
    public class AttackResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public float DamageDealt { get; set; }
        public bool WasKilled { get; set; }
        public HitboxType Hitbox { get; set; }
        public float Distance { get; set; }
        public int RemainingAmmo { get; set; } = -1;
    }

    /// <summary>
    /// Entrada de log de combate
    /// </summary>
    public class CombatLogEntry
    {
        public int AttackerId { get; set; }
        public string AttackerName { get; set; } = "";
        public int VictimId { get; set; }
        public string VictimName { get; set; } = "";
        public string WeaponUsed { get; set; } = "";
        public float DamageDealt { get; set; }
        public HitboxType Hitbox { get; set; }
        public bool WasKilled { get; set; }
        public float Distance { get; set; }
        public DateTime Timestamp { get; set; }

        public override string ToString()
        {
            string hitboxStr = Hitbox == HitboxType.Head ? "💥 HEADSHOT" : Hitbox.ToString();
            string killStr = WasKilled ? "☠️" : "";
            return $"[{Timestamp:HH:mm:ss}] {AttackerName} → {VictimName} | {WeaponUsed} | {DamageDealt:F1} dmg | {hitboxStr} {killStr}";
        }
    }
}