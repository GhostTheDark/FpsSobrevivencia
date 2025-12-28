using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RustlikeClient.Items
{
    /// <summary>
    /// ⭐ ATUALIZADO: Item data com suporte JSON
    /// </summary>
    [Serializable]
    public class ItemData
    {
        public int id;
        public string itemName;
        public string description;
        public string type;
        public int maxStack;
        public bool isConsumable;
        public string consumableType;
        public float healthRestore;
        public float hungerRestore;
        public float thirstRestore;
        
        // Não vem do JSON - configurado no Unity
        [NonSerialized]
        public Sprite icon;
    }

    /// <summary>
    /// ⭐ NOVO: Estrutura do JSON (mesmo do servidor)
    /// </summary>
    [Serializable]
    public class ItemsDatabase
    {
        public List<ItemData> items;
    }

    /// <summary>
    /// ⭐ ATUALIZADO: Database local de itens carregado de JSON
    /// </summary>
    public class ItemDatabase : MonoBehaviour
    {
        public static ItemDatabase Instance { get; private set; }

        [Header("JSON Source")]
        [Tooltip("Arquivo items.json (deve estar em StreamingAssets)")]
        public string itemsJsonFileName = "items.json";

        [Header("Item Icons")]
        [Tooltip("Arraste aqui os sprites dos ícones (configure IDs manualmente)")]
        public List<ItemIconMapping> itemIcons = new List<ItemIconMapping>();

        [Header("Auto Setup")]
        [Tooltip("Se true, tenta carregar do JSON automaticamente")]
        public bool autoLoadFromJson = true;

        [Header("Fallback")]
        [Tooltip("Se true e JSON não existir, cria itens padrão")]
        public bool createDefaultIfMissing = true;

        private Dictionary<int, ItemData> _itemDict;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadItemsFromJson();
        }

        /// <summary>
        /// ⭐ NOVO: Carrega itens do JSON
        /// </summary>
        private void LoadItemsFromJson()
        {
            _itemDict = new Dictionary<int, ItemData>();

            if (!autoLoadFromJson)
            {
                Debug.LogWarning("[ItemDatabase] Auto-load desabilitado");
                return;
            }

            string jsonPath = Path.Combine(Application.streamingAssetsPath, itemsJsonFileName);

            // Se não existir no StreamingAssets, tenta na raiz do projeto
            if (!File.Exists(jsonPath))
            {
                jsonPath = Path.Combine(Application.dataPath, "..", itemsJsonFileName);
            }

            if (!File.Exists(jsonPath))
            {
                Debug.LogWarning($"[ItemDatabase] ⚠️ Arquivo {itemsJsonFileName} não encontrado!");
                
                if (createDefaultIfMissing)
                {
                    Debug.Log("[ItemDatabase] Criando itens padrão...");
                    CreateDefaultItems();
                }
                return;
            }

            try
            {
                Debug.Log($"[ItemDatabase] Carregando itens de: {jsonPath}");
                
                string json = File.ReadAllText(jsonPath);
                ItemsDatabase database = JsonUtility.FromJson<ItemsDatabase>(json);

                if (database?.items != null)
                {
                    foreach (var item in database.items)
                    {
                        // Mapeia ícone se existir
                        item.icon = GetIconForItem(item.id);
                        
                        _itemDict[item.id] = item;
                    }

                    Debug.Log($"[ItemDatabase] ✅ {_itemDict.Count} itens carregados do JSON");
                    LogLoadedItems();
                }
                else
                {
                    Debug.LogError("[ItemDatabase] ❌ JSON inválido ou vazio!");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ItemDatabase] ❌ Erro ao carregar JSON: {ex.Message}");
                
                if (createDefaultIfMissing)
                {
                    CreateDefaultItems();
                }
            }
        }

        /// <summary>
        /// ⭐ NOVO: Pega ícone mapeado para um item
        /// </summary>
        private Sprite GetIconForItem(int itemId)
        {
            foreach (var mapping in itemIcons)
            {
                if (mapping.itemId == itemId)
                {
                    return mapping.icon;
                }
            }
            return null;
        }

        /// <summary>
        /// Log dos itens carregados
        /// </summary>
        private void LogLoadedItems()
        {
            int consumables = 0;
            int resources = 0;
            int tools = 0;
            int buildings = 0;

            foreach (var item in _itemDict.Values)
            {
                switch (item.type)
                {
                    case "Consumable": consumables++; break;
                    case "Resource": resources++; break;
                    case "Tool": tools++; break;
                    case "Building": buildings++; break;
                }
            }

            Debug.Log("========== ITENS CARREGADOS (CLIENTE) ==========");
            Debug.Log($"Consumíveis: {consumables}");
            Debug.Log($"Recursos: {resources}");
            Debug.Log($"Ferramentas: {tools}");
            Debug.Log($"Construção: {buildings}");
            Debug.Log($"TOTAL: {_itemDict.Count}");
            Debug.Log("===============================================");
        }

        /// <summary>
        /// Cria itens placeholder (fallback)
        /// </summary>
        private void CreateDefaultItems()
        {
            Debug.Log("[ItemDatabase] Criando itens padrão (hardcoded)...");

            var defaultItems = new List<ItemData>
            {
                // Consumíveis - Comida
                CreateItem(1, "Apple", "Uma maçã fresca. Restaura fome.", "Consumable", 10, true, "Food", 0, 20, 5),
                CreateItem(2, "Cooked Meat", "Carne cozida. Muito nutritivo.", "Consumable", 20, true, "Food", 0, 50, 0),
                CreateItem(3, "Chocolate Bar", "Barra de chocolate. Energia rápida.", "Consumable", 10, true, "Food", 0, 30, 10),

                // Consumíveis - Água
                CreateItem(4, "Water Bottle", "Garrafa de água. Mata a sede.", "Consumable", 5, true, "Water", 0, 0, 50),
                CreateItem(5, "Soda Can", "Refrigerante. Hidrata e energiza.", "Consumable", 10, true, "Water", 0, 10, 40),

                // Consumíveis - Remédios
                CreateItem(6, "Bandage", "Bandagem. Restaura 20 HP.", "Consumable", 10, true, "Medicine", 20, 0, 0),
                CreateItem(7, "Medical Syringe", "Seringa médica. Restaura 50 HP.", "Consumable", 5, true, "Medicine", 50, 0, 0),
                CreateItem(8, "Large Medkit", "Kit médico grande. Full heal.", "Consumable", 3, true, "Medicine", 100, 0, 0),

                // Consumíveis - Híbridos
                CreateItem(9, "Survival Ration", "Ração de sobrevivência. Restaura tudo um pouco.", "Consumable", 5, true, "Hybrid", 10, 30, 30),
                CreateItem(10, "Energy Drink", "Bebida energética. Boost completo!", "Consumable", 5, true, "Hybrid", 20, 40, 60),

                // Recursos
                CreateItem(100, "Wood", "Madeira. Material de construção básico.", "Resource", 1000, false, "None", 0, 0, 0),
                CreateItem(101, "Stone", "Pedra. Mais resistente que madeira.", "Resource", 1000, false, "None", 0, 0, 0),
                CreateItem(102, "Metal Ore", "Minério de metal. Muito valioso.", "Resource", 500, false, "None", 0, 0, 0),
                CreateItem(103, "Sulfur Ore", "Minério de enxofre. Usado em explosivos.", "Resource", 500, false, "None", 0, 0, 0),
            };

            foreach (var item in defaultItems)
            {
                _itemDict[item.id] = item;
            }

            Debug.Log($"[ItemDatabase] {defaultItems.Count} itens padrão criados");
        }

        private ItemData CreateItem(int id, string name, string desc, string type, int maxStack, bool consumable, string consumableType, float health, float hunger, float thirst)
        {
            return new ItemData
            {
                id = id,
                itemName = name,
                description = desc,
                type = type,
                maxStack = maxStack,
                isConsumable = consumable,
                consumableType = consumableType,
                healthRestore = health,
                hungerRestore = hunger,
                thirstRestore = thirst,
                icon = null
            };
        }

        public ItemData GetItem(int itemId)
        {
            return _itemDict.TryGetValue(itemId, out var item) ? item : null;
        }

        public bool ValidateItem(int itemId)
        {
            bool exists = _itemDict.ContainsKey(itemId);
            if (!exists)
            {
                Debug.LogWarning($"[ItemDatabase] Item ID {itemId} não encontrado!");
            }
            return exists;
        }

        /// <summary>
        /// ⭐ NOVO: Recarrega database (útil para hot-reload)
        /// </summary>
        [ContextMenu("Reload Database")]
        public void ReloadDatabase()
        {
            _itemDict.Clear();
            LoadItemsFromJson();
        }

        /// <summary>
        /// ⭐ NOVO: Cria arquivo JSON de exemplo no StreamingAssets
        /// </summary>
        [ContextMenu("Create Example JSON in StreamingAssets")]
        public void CreateExampleJson()
        {
            string streamingPath = Application.streamingAssetsPath;
            
            if (!Directory.Exists(streamingPath))
            {
                Directory.CreateDirectory(streamingPath);
            }

            string jsonPath = Path.Combine(streamingPath, itemsJsonFileName);

            if (File.Exists(jsonPath))
            {
                Debug.LogWarning($"[ItemDatabase] Arquivo {itemsJsonFileName} já existe!");
                return;
            }

            // Cria estrutura de exemplo
            var exampleDatabase = new ItemsDatabase
            {
                items = new List<ItemData>
                {
                    CreateItem(1, "Apple", "Uma maçã fresca", "Consumable", 10, true, "Food", 0, 20, 5),
                    CreateItem(100, "Wood", "Madeira", "Resource", 1000, false, "None", 0, 0, 0),
                }
            };

            string json = JsonUtility.ToJson(exampleDatabase, true);
            File.WriteAllText(jsonPath, json);

            Debug.Log($"[ItemDatabase] ✅ Arquivo de exemplo criado em: {jsonPath}");
        }
    }

    /// <summary>
    /// ⭐ NOVO: Mapeamento de item ID -> Sprite (configurável no Inspector)
    /// </summary>
    [Serializable]
    public class ItemIconMapping
    {
        public int itemId;
        public Sprite icon;
    }
}