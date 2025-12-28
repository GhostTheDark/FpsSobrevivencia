using System.Collections.Generic;
using UnityEngine;

namespace RustlikeClient.World
{
    /// <summary>
    /// Gerenciador de recursos no cliente (spawna e sincroniza recursos visuais)
    /// </summary>
    public class ResourceManager : MonoBehaviour
    {
        public static ResourceManager Instance { get; private set; }

        [Header("Resource Prefabs")]
        [Tooltip("Prefab para árvores")]
        public GameObject treePrefab;
        
        [Tooltip("Prefab para pedras")]
        public GameObject stonePrefab;
        
        [Tooltip("Prefab para minério de metal")]
        public GameObject metalOrePrefab;
        
        [Tooltip("Prefab para enxofre")]
        public GameObject sulfurOrePrefab;

        [Header("Auto-Create Prefabs (Debug)")]
        [Tooltip("Se true, cria prefabs simples automaticamente se não estiverem configurados")]
        public bool autoCreatePrefabs = true;

        // Dicionário de recursos spawned (ID -> GameObject)
        private Dictionary<int, ResourceNode> _resources = new Dictionary<int, ResourceNode>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Auto-cria prefabs se necessário
            if (autoCreatePrefabs)
            {
                EnsurePrefabsExist();
            }

            Debug.Log("[ResourceManager] Inicializado no cliente");
        }

        /// <summary>
        /// Spawna todos os recursos recebidos do servidor
        /// </summary>
        public void SpawnResources(List<Network.ResourceData> resourcesData)
        {
            Debug.Log($"[ResourceManager] Spawning {resourcesData.Count} recursos");

            // Limpa recursos existentes
            ClearAllResources();

            foreach (var data in resourcesData)
            {
                SpawnResource(data);
            }

            Debug.Log($"[ResourceManager] ✅ {_resources.Count} recursos spawned");
        }

        /// <summary>
        /// Spawna um recurso individual
        /// </summary>
        private void SpawnResource(Network.ResourceData data)
        {
            GameObject prefab = GetPrefabForType((ResourceType)data.Type);
            
            if (prefab == null)
            {
                Debug.LogError($"[ResourceManager] Prefab não encontrado para tipo {(ResourceType)data.Type}");
                return;
            }

            Vector3 position = new Vector3(data.PosX, data.PosY, data.PosZ);
            GameObject resourceObj = Instantiate(prefab, position, Quaternion.identity, transform);

            // Configura ResourceNode
            ResourceNode node = resourceObj.GetComponent<ResourceNode>();
            if (node == null)
            {
                node = resourceObj.AddComponent<ResourceNode>();
            }

            node.Initialize(data.Id, (ResourceType)data.Type, position, data.Health, data.MaxHealth);

            _resources[data.Id] = node;

            Debug.Log($"[ResourceManager] Spawned {(ResourceType)data.Type} ID:{data.Id} at {position}");
        }

        /// <summary>
        /// Atualiza saúde de um recurso
        /// </summary>
        public void UpdateResourceHealth(int resourceId, float health, float maxHealth)
        {
            if (_resources.TryGetValue(resourceId, out ResourceNode node))
            {
                node.UpdateHealth(health, maxHealth);
                node.ShowHitEffect();
            }
        }

        /// <summary>
        /// Destrói um recurso
        /// </summary>
        public void DestroyResource(int resourceId)
        {
            if (_resources.TryGetValue(resourceId, out ResourceNode node))
            {
                Debug.Log($"[ResourceManager] Destruindo recurso {resourceId}");
                node.DestroyResource();
            }
        }

        /// <summary>
        /// Respawna um recurso
        /// </summary>
        public void RespawnResource(int resourceId, float health, float maxHealth)
        {
            if (_resources.TryGetValue(resourceId, out ResourceNode node))
            {
                Debug.Log($"[ResourceManager] Respawnando recurso {resourceId}");
                node.RespawnResource(health, maxHealth);
            }
        }

        /// <summary>
        /// Limpa todos os recursos
        /// </summary>
        public void ClearAllResources()
        {
            foreach (var resource in _resources.Values)
            {
                if (resource != null && resource.gameObject != null)
                {
                    Destroy(resource.gameObject);
                }
            }

            _resources.Clear();
            Debug.Log("[ResourceManager] Todos os recursos limpos");
        }

        /// <summary>
        /// Pega recurso por ID
        /// </summary>
        public ResourceNode GetResource(int id)
        {
            return _resources.TryGetValue(id, out ResourceNode node) ? node : null;
        }

        /// <summary>
        /// Conta recursos por tipo
        /// </summary>
        public int CountResourcesByType(ResourceType type)
        {
            int count = 0;
            foreach (var resource in _resources.Values)
            {
                if (resource.resourceType == type && resource.isAlive)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Pega prefab baseado no tipo de recurso
        /// </summary>
        private GameObject GetPrefabForType(ResourceType type)
        {
            return type switch
            {
                ResourceType.Tree => treePrefab,
                ResourceType.Stone => stonePrefab,
                ResourceType.MetalOre => metalOrePrefab,
                ResourceType.SulfurOre => sulfurOrePrefab,
                _ => null
            };
        }

        /// <summary>
        /// Garante que prefabs existam (cria simples se não existirem)
        /// </summary>
        private void EnsurePrefabsExist()
        {
            if (treePrefab == null)
            {
                treePrefab = CreateSimplePrefab("Tree", new Color(0.3f, 0.6f, 0.2f), new Vector3(1f, 3f, 1f));
                Debug.Log("[ResourceManager] Tree prefab criado automaticamente");
            }

            if (stonePrefab == null)
            {
                stonePrefab = CreateSimplePrefab("Stone", new Color(0.5f, 0.5f, 0.5f), new Vector3(2f, 1.5f, 2f));
                Debug.Log("[ResourceManager] Stone prefab criado automaticamente");
            }

            if (metalOrePrefab == null)
            {
                metalOrePrefab = CreateSimplePrefab("MetalOre", new Color(0.7f, 0.7f, 0.8f), new Vector3(1.5f, 1.2f, 1.5f));
                Debug.Log("[ResourceManager] MetalOre prefab criado automaticamente");
            }

            if (sulfurOrePrefab == null)
            {
                sulfurOrePrefab = CreateSimplePrefab("SulfurOre", new Color(0.9f, 0.9f, 0.3f), new Vector3(1.5f, 1.2f, 1.5f));
                Debug.Log("[ResourceManager] SulfurOre prefab criado automaticamente");
            }
        }

        /// <summary>
        /// Cria um prefab simples (cubo com cor)
        /// </summary>
        private GameObject CreateSimplePrefab(string name, Color color, Vector3 scale)
        {
            GameObject prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            prefab.name = $"{name}Prefab";
            prefab.transform.localScale = scale;

            // Material
            MeshRenderer renderer = prefab.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = color;
                renderer.material = mat;
            }

            // Adiciona ResourceNode
            prefab.AddComponent<ResourceNode>();

            // Desativa (é um prefab)
            prefab.SetActive(false);

            return prefab;
        }

        /// <summary>
        /// Para debug no Inspector
        /// </summary>
        private void OnGUI()
        {
            if (Input.GetKey(KeyCode.F6))
            {
                GUI.Box(new Rect(10, 560, 250, 120), "Resource Manager (F6)");
                
                int treeCount = CountResourcesByType(ResourceType.Tree);
                int stoneCount = CountResourcesByType(ResourceType.Stone);
                int metalCount = CountResourcesByType(ResourceType.MetalOre);
                int sulfurCount = CountResourcesByType(ResourceType.SulfurOre);
                
                GUI.Label(new Rect(20, 585, 230, 20), $"Total: {_resources.Count} recursos");
                GUI.Label(new Rect(20, 605, 230, 20), $"Trees: {treeCount}");
                GUI.Label(new Rect(20, 625, 230, 20), $"Stones: {stoneCount}");
                GUI.Label(new Rect(20, 645, 230, 20), $"Metal: {metalCount}");
                GUI.Label(new Rect(20, 665, 230, 20), $"Sulfur: {sulfurCount}");
            }
        }

        /// <summary>
        /// Desenha gizmos de todos os recursos
        /// </summary>
        private void OnDrawGizmos()
        {
            if (_resources == null || _resources.Count == 0) return;

            foreach (var resource in _resources.Values)
            {
                if (resource == null) continue;

                Gizmos.color = resource.isAlive ? Color.green : Color.red;
                Gizmos.DrawWireSphere(resource.transform.position, 0.5f);
            }
        }

        [ContextMenu("Print Resource Stats")]
        public void PrintResourceStats()
        {
            Debug.Log("========== RESOURCE MANAGER STATS ==========");
            Debug.Log($"Total Resources: {_resources.Count}");
            Debug.Log($"Trees: {CountResourcesByType(ResourceType.Tree)}");
            Debug.Log($"Stones: {CountResourcesByType(ResourceType.Stone)}");
            Debug.Log($"Metal: {CountResourcesByType(ResourceType.MetalOre)}");
            Debug.Log($"Sulfur: {CountResourcesByType(ResourceType.SulfurOre)}");
            Debug.Log("============================================");
        }
    }
}