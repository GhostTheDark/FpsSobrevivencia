using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RustlikeServer.Items
{
    /// <summary>
    /// Tipos de itens no jogo
    /// </summary>
    public enum ItemType
    {
        Consumable,    // Comida, água, remédios
        Resource,      // Madeira, pedra, metal
        Tool,          // Machado, picareta, arma
        Building,      // Fundação, parede, porta
        Clothing       // Roupas, armadura
    }

    /// <summary>
    /// Categoria de consumíveis
    /// </summary>
    public enum ConsumableType
    {
        None,          // Não é consumível
        Food,          // Restaura fome
        Water,         // Restaura sede
        Medicine,      // Restaura vida
        Hybrid         // Restaura múltiplas stats
    }

    /// <summary>
    /// ⭐ ATUALIZADO: Definição de item com suporte JSON
    /// </summary>
    [Serializable]
    public class ItemDefinition
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("description")]
        public string Description { get; set; }
        
        [JsonPropertyName("type")]
        public string Type { get; set; } // Como string no JSON
        
        [JsonPropertyName("maxStack")]
        public int MaxStack { get; set; }
        
        [JsonPropertyName("isConsumable")]
        public bool IsConsumable { get; set; }
        
        // Efeitos do consumível
        [JsonPropertyName("consumableType")]
        public string ConsumableCategory { get; set; } // Como string no JSON
        
        [JsonPropertyName("healthRestore")]
        public float HealthRestore { get; set; }
        
        [JsonPropertyName("hungerRestore")]
        public float HungerRestore { get; set; }
        
        [JsonPropertyName("thirstRestore")]
        public float ThirstRestore { get; set; }

        // Propriedades auxiliares (não serializadas)
        [JsonIgnore]
        public ItemType ItemTypeEnum
        {
            get => ParseItemType(Type);
        }

        [JsonIgnore]
        public ConsumableType ConsumableTypeEnum
        {
            get => ParseConsumableType(ConsumableCategory);
        }

        private ItemType ParseItemType(string type)
        {
            return type switch
            {
                "Consumable" => ItemType.Consumable,
                "Resource" => ItemType.Resource,
                "Tool" => ItemType.Tool,
                "Building" => ItemType.Building,
                "Clothing" => ItemType.Clothing,
                _ => ItemType.Resource
            };
        }

        private ConsumableType ParseConsumableType(string type)
        {
            return type switch
            {
                "Food" => ConsumableType.Food,
                "Water" => ConsumableType.Water,
                "Medicine" => ConsumableType.Medicine,
                "Hybrid" => ConsumableType.Hybrid,
                _ => ConsumableType.None
            };
        }

        public override string ToString()
        {
            return $"{Name} (ID: {Id}, Stack: {MaxStack})";
        }
    }

    /// <summary>
    /// ⭐ NOVO: Estrutura do arquivo JSON
    /// </summary>
    [Serializable]
    public class ItemsDatabase
    {
        [JsonPropertyName("items")]
        public List<ItemDefinition> Items { get; set; }
    }

    /// <summary>
    /// Instância de um item no inventário
    /// </summary>
    public class ItemStack
    {
        public int ItemId { get; set; }
        public int Quantity { get; set; }
        public ItemDefinition Definition { get; set; }

        public ItemStack(ItemDefinition definition, int quantity = 1)
        {
            Definition = definition;
            ItemId = definition.Id;
            Quantity = Math.Min(quantity, definition.MaxStack);
        }

        /// <summary>
        /// Verifica se pode adicionar mais itens à stack
        /// </summary>
        public bool CanAddMore(int amount)
        {
            return (Quantity + amount) <= Definition.MaxStack;
        }

        /// <summary>
        /// Adiciona quantidade à stack
        /// </summary>
        public int Add(int amount)
        {
            int space = Definition.MaxStack - Quantity;
            int toAdd = Math.Min(amount, space);
            Quantity += toAdd;
            return amount - toAdd; // Retorna sobra
        }

        /// <summary>
        /// Remove quantidade da stack
        /// </summary>
        public bool Remove(int amount)
        {
            if (amount > Quantity) return false;
            Quantity -= amount;
            return true;
        }

        public bool IsEmpty() => Quantity <= 0;
    }

    /// <summary>
    /// ⭐ ATUALIZADO: Database de itens carregado de JSON
    /// </summary>
    public static class ItemDatabase
    {
        private static Dictionary<int, ItemDefinition> _items = new Dictionary<int, ItemDefinition>();
        private const string ITEMS_FILE = "items.json";

        static ItemDatabase()
        {
            LoadItems();
        }

        /// <summary>
        /// ⭐ NOVO: Carrega itens do arquivo JSON
        /// </summary>
        private static void LoadItems()
        {
            Console.WriteLine("[ItemDatabase] Carregando itens do JSON...");

            if (!File.Exists(ITEMS_FILE))
            {
                Console.WriteLine($"[ItemDatabase] ⚠️ Arquivo {ITEMS_FILE} não encontrado! Criando arquivo padrão...");
                CreateDefaultItemsFile();
            }

            try
            {
                string json = File.ReadAllText(ITEMS_FILE);
                
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                };

                var database = JsonSerializer.Deserialize<ItemsDatabase>(json, options);

                if (database?.Items != null)
                {
                    foreach (var item in database.Items)
                    {
                        _items[item.Id] = item;
                    }

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"[ItemDatabase] ✅ {_items.Count} itens carregados de {ITEMS_FILE}");
                    Console.ResetColor();

                    // Log de itens carregados
                    LogLoadedItems();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ItemDatabase] ❌ Erro ao carregar itens: {ex.Message}");
                Console.ResetColor();
                
                // Se falhar, cria arquivo padrão
                CreateDefaultItemsFile();
            }
        }

        /// <summary>
        /// ⭐ NOVO: Cria arquivo JSON padrão
        /// </summary>
        private static void CreateDefaultItemsFile()
        {
            Console.WriteLine("[ItemDatabase] Criando items.json padrão...");

            var defaultItems = new ItemsDatabase
            {
                Items = new List<ItemDefinition>()
            };

            // Adiciona itens padrão (mesmos do código original)
            defaultItems.Items.AddRange(new[]
            {
                // Consumíveis - Comida
                new ItemDefinition { Id = 1, Name = "Apple", Description = "Uma maçã fresca. Restaura fome.", Type = "Consumable", MaxStack = 10, IsConsumable = true, ConsumableCategory = "Food", HealthRestore = 0, HungerRestore = 20, ThirstRestore = 5 },
                new ItemDefinition { Id = 2, Name = "Cooked Meat", Description = "Carne cozida. Muito nutritivo.", Type = "Consumable", MaxStack = 20, IsConsumable = true, ConsumableCategory = "Food", HealthRestore = 0, HungerRestore = 50, ThirstRestore = 0 },
                new ItemDefinition { Id = 3, Name = "Chocolate Bar", Description = "Barra de chocolate. Energia rápida.", Type = "Consumable", MaxStack = 10, IsConsumable = true, ConsumableCategory = "Food", HealthRestore = 0, HungerRestore = 30, ThirstRestore = 10 },

                // Consumíveis - Água
                new ItemDefinition { Id = 4, Name = "Water Bottle", Description = "Garrafa de água. Mata a sede.", Type = "Consumable", MaxStack = 5, IsConsumable = true, ConsumableCategory = "Water", HealthRestore = 0, HungerRestore = 0, ThirstRestore = 50 },
                new ItemDefinition { Id = 5, Name = "Soda Can", Description = "Refrigerante. Hidrata e energiza.", Type = "Consumable", MaxStack = 10, IsConsumable = true, ConsumableCategory = "Water", HealthRestore = 0, HungerRestore = 10, ThirstRestore = 40 },

                // Consumíveis - Remédios
                new ItemDefinition { Id = 6, Name = "Bandage", Description = "Bandagem. Restaura 20 HP.", Type = "Consumable", MaxStack = 10, IsConsumable = true, ConsumableCategory = "Medicine", HealthRestore = 20, HungerRestore = 0, ThirstRestore = 0 },
                new ItemDefinition { Id = 7, Name = "Medical Syringe", Description = "Seringa médica. Restaura 50 HP.", Type = "Consumable", MaxStack = 5, IsConsumable = true, ConsumableCategory = "Medicine", HealthRestore = 50, HungerRestore = 0, ThirstRestore = 0 },
                new ItemDefinition { Id = 8, Name = "Large Medkit", Description = "Kit médico grande. Full heal.", Type = "Consumable", MaxStack = 3, IsConsumable = true, ConsumableCategory = "Medicine", HealthRestore = 100, HungerRestore = 0, ThirstRestore = 0 },

                // Consumíveis - Híbridos
                new ItemDefinition { Id = 9, Name = "Survival Ration", Description = "Ração de sobrevivência. Restaura tudo um pouco.", Type = "Consumable", MaxStack = 5, IsConsumable = true, ConsumableCategory = "Hybrid", HealthRestore = 10, HungerRestore = 30, ThirstRestore = 30 },
                new ItemDefinition { Id = 10, Name = "Energy Drink", Description = "Bebida energética. Boost completo!", Type = "Consumable", MaxStack = 5, IsConsumable = true, ConsumableCategory = "Hybrid", HealthRestore = 20, HungerRestore = 40, ThirstRestore = 60 },

                // Recursos
                new ItemDefinition { Id = 100, Name = "Wood", Description = "Madeira. Material de construção básico.", Type = "Resource", MaxStack = 1000, IsConsumable = false, ConsumableCategory = "None", HealthRestore = 0, HungerRestore = 0, ThirstRestore = 0 },
                new ItemDefinition { Id = 101, Name = "Stone", Description = "Pedra. Mais resistente que madeira.", Type = "Resource", MaxStack = 1000, IsConsumable = false, ConsumableCategory = "None", HealthRestore = 0, HungerRestore = 0, ThirstRestore = 0 },
                new ItemDefinition { Id = 102, Name = "Metal Ore", Description = "Minério de metal. Muito valioso.", Type = "Resource", MaxStack = 500, IsConsumable = false, ConsumableCategory = "None", HealthRestore = 0, HungerRestore = 0, ThirstRestore = 0 },
                new ItemDefinition { Id = 103, Name = "Sulfur Ore", Description = "Minério de enxofre. Usado em explosivos.", Type = "Resource", MaxStack = 500, IsConsumable = false, ConsumableCategory = "None", HealthRestore = 0, HungerRestore = 0, ThirstRestore = 0 },
            });

            SaveItems(defaultItems);
            
            // Recarrega após criar
            LoadItems();
        }

        /// <summary>
        /// ⭐ NOVO: Salva itens no JSON
        /// </summary>
        private static void SaveItems(ItemsDatabase database)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                string json = JsonSerializer.Serialize(database, options);
                File.WriteAllText(ITEMS_FILE, json);

                Console.WriteLine($"[ItemDatabase] ✅ Itens salvos em {ITEMS_FILE}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ItemDatabase] ❌ Erro ao salvar itens: {ex.Message}");
            }
        }

        /// <summary>
        /// ⭐ NOVO: Log dos itens carregados
        /// </summary>
        private static void LogLoadedItems()
        {
            Console.WriteLine("\n========== ITENS CARREGADOS ==========");
            
            var consumables = 0;
            var resources = 0;
            var tools = 0;
            var buildings = 0;

            foreach (var item in _items.Values)
            {
                switch (item.ItemTypeEnum)
                {
                    case ItemType.Consumable: consumables++; break;
                    case ItemType.Resource: resources++; break;
                    case ItemType.Tool: tools++; break;
                    case ItemType.Building: buildings++; break;
                }
            }

            Console.WriteLine($"Consumíveis: {consumables}");
            Console.WriteLine($"Recursos: {resources}");
            Console.WriteLine($"Ferramentas: {tools}");
            Console.WriteLine($"Construção: {buildings}");
            Console.WriteLine($"TOTAL: {_items.Count}");
            Console.WriteLine("=====================================\n");
        }

        public static ItemDefinition GetItem(int itemId)
        {
            return _items.TryGetValue(itemId, out var item) ? item : null;
        }

        public static bool ItemExists(int itemId)
        {
            return _items.ContainsKey(itemId);
        }

        public static IEnumerable<ItemDefinition> GetAllItems()
        {
            return _items.Values;
        }

        public static IEnumerable<ItemDefinition> GetConsumables()
        {
            foreach (var item in _items.Values)
            {
                if (item.IsConsumable)
                    yield return item;
            }
        }

        /// <summary>
        /// ⭐ NOVO: Recarrega itens do JSON (útil para hot-reload)
        /// </summary>
        public static void Reload()
        {
            _items.Clear();
            LoadItems();
        }
    }
}