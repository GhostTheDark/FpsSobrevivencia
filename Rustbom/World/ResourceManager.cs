using System;
using System.Collections.Generic;
using System.Linq;

namespace RustlikeServer.World
{
    /// <summary>
    /// Gerencia todos os recursos do mundo (spawn, respawn, coleta)
    /// </summary>
    public class ResourceManager
    {
        private Dictionary<int, ResourceNode> _resources;
        private int _nextResourceId;
        private readonly object _resourcesLock = new object();

        // Configurações de spawn
        private const int TREES_COUNT = 50;
        private const int STONES_COUNT = 30;
        private const int METAL_COUNT = 15;
        private const int SULFUR_COUNT = 10;
        private const float SPAWN_RADIUS = 100f;
        private const float RESPAWN_TIME = 300f; // 5 minutos

        public ResourceManager()
        {
            _resources = new Dictionary<int, ResourceNode>();
            _nextResourceId = 1;
        }

        /// <summary>
        /// Inicializa recursos no mundo
        /// </summary>
        public void Initialize()
        {
            Console.WriteLine("[ResourceManager] Inicializando recursos do mundo...");

            // Spawna árvores
            for (int i = 0; i < TREES_COUNT; i++)
            {
                SpawnResource(ResourceType.Tree);
            }

            // Spawna pedras
            for (int i = 0; i < STONES_COUNT; i++)
            {
                SpawnResource(ResourceType.Stone);
            }

            // Spawna minério de metal
            for (int i = 0; i < METAL_COUNT; i++)
            {
                SpawnResource(ResourceType.MetalOre);
            }

            // Spawna enxofre
            for (int i = 0; i < SULFUR_COUNT; i++)
            {
                SpawnResource(ResourceType.SulfurOre);
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[ResourceManager] ✅ {_resources.Count} recursos spawned!");
            Console.ResetColor();
        }

        /// <summary>
        /// Spawna um recurso em posição aleatória
        /// </summary>
        private void SpawnResource(ResourceType type)
        {
            int id = _nextResourceId++;
            Vector3 position = GetRandomSpawnPosition();
            
            var resource = new ResourceNode(id, type, position);
            
            lock (_resourcesLock)
            {
                _resources[id] = resource;
            }
        }

        /// <summary>
        /// Gera posição aleatória dentro do raio de spawn
        /// </summary>
        private Vector3 GetRandomSpawnPosition()
        {
            float x = UnityEngine.Random.Range(-SPAWN_RADIUS, SPAWN_RADIUS);
            float z = UnityEngine.Random.Range(-SPAWN_RADIUS, SPAWN_RADIUS);
            float y = 0.5f; // Altura do chão
            
            return new Vector3(x, y, z);
        }

        /// <summary>
        /// Processa coleta de recurso por um jogador
        /// </summary>
        public GatherResult GatherResource(int resourceId, float damage, int toolType, Player player)
        {
            lock (_resourcesLock)
            {
                if (!_resources.TryGetValue(resourceId, out var resource))
                {
                    Console.WriteLine($"[ResourceManager] ⚠️ Recurso {resourceId} não encontrado");
                    return null;
                }

                if (!resource.IsAlive)
                {
                    Console.WriteLine($"[ResourceManager] ⚠️ Recurso {resourceId} já foi destruído");
                    return null;
                }

                // Verifica distância (anti-cheat)
                float distance = CalculateDistance(player.Position, resource.Position);
                if (distance > 5f)
                {
                    Console.WriteLine($"[ResourceManager] ⚠️ {player.Name} muito longe do recurso ({distance:F1}m)");
                    return null;
                }

                // Aplica dano e pega resultado
                var result = resource.TakeDamage(damage, toolType);

                if (result != null)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"[ResourceManager] 🪓 {player.Name} coletou de {resource.Type} (ID: {resourceId})");
                    Console.WriteLine($"   → Wood: {result.WoodGained}, Stone: {result.StoneGained}, Metal: {result.MetalGained}, Sulfur: {result.SulfurGained}");
                    Console.WriteLine($"   → HP do recurso: {resource.Health:F0}/{resource.MaxHealth:F0}");
                    Console.ResetColor();

                    if (result.WasDestroyed)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"[ResourceManager] 💥 Recurso {resourceId} destruído! Respawn em {RESPAWN_TIME}s");
                        Console.ResetColor();
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// Calcula distância entre dois pontos
        /// </summary>
        private float CalculateDistance(Vector3 a, Vector3 b)
        {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            float dz = a.Z - b.Z;
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        /// <summary>
        /// Update periódico (respawn de recursos)
        /// </summary>
        public void Update()
        {
            lock (_resourcesLock)
            {
                foreach (var resource in _resources.Values)
                {
                    if (resource.CanRespawn(RESPAWN_TIME))
                    {
                        resource.Respawn();
                        
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"[ResourceManager] ♻️ Recurso {resource.Type} (ID: {resource.Id}) respawned");
                        Console.ResetColor();
                    }
                }
            }
        }

        /// <summary>
        /// Pega todos os recursos para enviar ao cliente
        /// </summary>
        public List<ResourceNode> GetAllResources()
        {
            lock (_resourcesLock)
            {
                return _resources.Values.Where(r => r.IsAlive).ToList();
            }
        }

        /// <summary>
        /// Pega recurso por ID
        /// </summary>
        public ResourceNode GetResource(int id)
        {
            lock (_resourcesLock)
            {
                return _resources.TryGetValue(id, out var resource) ? resource : null;
            }
        }

        /// <summary>
        /// Conta recursos por tipo
        /// </summary>
        public int CountResourcesByType(ResourceType type)
        {
            lock (_resourcesLock)
            {
                return _resources.Values.Count(r => r.Type == type && r.IsAlive);
            }
        }

        /// <summary>
        /// Para debug
        /// </summary>
        public void PrintStatus()
        {
            Console.WriteLine("\n========== RESOURCE MANAGER STATUS ==========");
            Console.WriteLine($"Total Resources: {_resources.Count}");
            Console.WriteLine($"Trees: {CountResourcesByType(ResourceType.Tree)}");
            Console.WriteLine($"Stones: {CountResourcesByType(ResourceType.Stone)}");
            Console.WriteLine($"Metal: {CountResourcesByType(ResourceType.MetalOre)}");
            Console.WriteLine($"Sulfur: {CountResourcesByType(ResourceType.SulfurOre)}");
            Console.WriteLine("============================================\n");
        }
    }
}