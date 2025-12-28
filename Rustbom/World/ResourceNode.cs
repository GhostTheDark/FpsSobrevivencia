using System;

namespace RustlikeServer.World
{
    /// <summary>
    /// Tipos de recursos no mundo
    /// </summary>
    public enum ResourceType
    {
        Tree,          // Madeira
        Stone,         // Pedra
        MetalOre,      // Minério de metal
        SulfurOre      // Enxofre
    }

    /// <summary>
    /// Nó de recurso no mundo (árvore, pedra, etc)
    /// </summary>
    public class ResourceNode
    {
        public int Id { get; set; }
        public ResourceType Type { get; set; }
        public Vector3 Position { get; set; }
        public float Health { get; private set; }
        public float MaxHealth { get; private set; }
        public bool IsAlive { get; private set; }
        public DateTime SpawnTime { get; private set; }
        public DateTime? DeathTime { get; private set; }

        // Drops ao ser destruído
        public int WoodAmount { get; private set; }
        public int StoneAmount { get; private set; }
        public int MetalAmount { get; private set; }
        public int SulfurAmount { get; private set; }

        public ResourceNode(int id, ResourceType type, Vector3 position)
        {
            Id = id;
            Type = type;
            Position = position;
            IsAlive = true;
            SpawnTime = DateTime.Now;
            DeathTime = null;

            // Configura health e drops baseado no tipo
            ConfigureResourceType(type);
        }

        private void ConfigureResourceType(ResourceType type)
        {
            switch (type)
            {
                case ResourceType.Tree:
                    MaxHealth = 200f;
                    WoodAmount = UnityEngine.Random.Range(40, 80);
                    break;

                case ResourceType.Stone:
                    MaxHealth = 500f;
                    StoneAmount = UnityEngine.Random.Range(50, 100);
                    MetalAmount = UnityEngine.Random.Range(0, 10); // Chance de metal
                    break;

                case ResourceType.MetalOre:
                    MaxHealth = 300f;
                    MetalAmount = UnityEngine.Random.Range(30, 60);
                    StoneAmount = UnityEngine.Random.Range(10, 20);
                    break;

                case ResourceType.SulfurOre:
                    MaxHealth = 300f;
                    SulfurAmount = UnityEngine.Random.Range(30, 60);
                    StoneAmount = UnityEngine.Random.Range(10, 20);
                    break;
            }

            Health = MaxHealth;
        }

        /// <summary>
        /// Aplica dano ao recurso (quando player coleta)
        /// </summary>
        public GatherResult TakeDamage(float damage, int toolType)
        {
            if (!IsAlive) return null;

            // Multiplica dano baseado na ferramenta
            float damageMultiplier = GetToolMultiplier(toolType);
            float actualDamage = damage * damageMultiplier;

            Health -= actualDamage;

            // Se morreu, retorna drops
            if (Health <= 0)
            {
                Health = 0;
                IsAlive = false;
                DeathTime = DateTime.Now;

                return new GatherResult
                {
                    ResourceId = Id,
                    WoodGained = WoodAmount,
                    StoneGained = StoneAmount,
                    MetalGained = MetalAmount,
                    SulfurGained = SulfurAmount,
                    WasDestroyed = true
                };
            }

            // Retorna recursos parciais (a cada hit)
            return new GatherResult
            {
                ResourceId = Id,
                WoodGained = (int)(WoodAmount * 0.1f), // 10% por hit
                StoneGained = (int)(StoneAmount * 0.1f),
                MetalGained = (int)(MetalAmount * 0.1f),
                SulfurGained = (int)(SulfurAmount * 0.1f),
                WasDestroyed = false
            };
        }

        /// <summary>
        /// Multiplica dano baseado na ferramenta
        /// </summary>
        private float GetToolMultiplier(int toolType)
        {
            // Tool Types: 0=Mão, 1=Machado, 2=Picareta
            switch (Type)
            {
                case ResourceType.Tree:
                    if (toolType == 1) return 2.0f; // Machado é melhor
                    if (toolType == 2) return 0.5f; // Picareta é ruim
                    return 1.0f; // Mão

                case ResourceType.Stone:
                case ResourceType.MetalOre:
                case ResourceType.SulfurOre:
                    if (toolType == 2) return 2.0f; // Picareta é melhor
                    if (toolType == 1) return 0.5f; // Machado é ruim
                    return 1.0f; // Mão

                default:
                    return 1.0f;
            }
        }

        /// <summary>
        /// Verifica se pode respawnar (após X segundos)
        /// </summary>
        public bool CanRespawn(float respawnTime = 300f)
        {
            if (IsAlive) return false;
            if (DeathTime == null) return false;

            return (DateTime.Now - DeathTime.Value).TotalSeconds >= respawnTime;
        }

        /// <summary>
        /// Respawna o recurso
        /// </summary>
        public void Respawn()
        {
            IsAlive = true;
            SpawnTime = DateTime.Now;
            DeathTime = null;
            ConfigureResourceType(Type);
        }

        public override string ToString()
        {
            return $"{Type} (ID: {Id}) HP: {Health:F0}/{MaxHealth:F0} @ {Position}";
        }
    }

    /// <summary>
    /// Resultado de coleta de recurso
    /// </summary>
    public class GatherResult
    {
        public int ResourceId { get; set; }
        public int WoodGained { get; set; }
        public int StoneGained { get; set; }
        public int MetalGained { get; set; }
        public int SulfurGained { get; set; }
        public bool WasDestroyed { get; set; }
    }

    /// <summary>
    /// Helper para gerar posições de spawn de recursos
    /// </summary>
    public static class UnityEngine
    {
        public static class Random
        {
            private static System.Random _random = new System.Random();

            public static int Range(int min, int max)
            {
                return _random.Next(min, max);
            }

            public static float Range(float min, float max)
            {
                return (float)(_random.NextDouble() * (max - min) + min);
            }
        }
    }
}