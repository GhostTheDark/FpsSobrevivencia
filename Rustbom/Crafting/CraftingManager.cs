using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using RustlikeServer.World;

namespace RustlikeServer.Crafting
{
    /// <summary>
    /// ⭐ NOVO: Gerencia receitas de crafting e craftings em progresso
    /// </summary>
    public class CraftingManager
    {
        private Dictionary<int, CraftingRecipe> _recipes;
        private Dictionary<int, List<CraftingProgress>> _playerCraftingQueues; // PlayerId -> Queue de crafts
        private readonly object _craftingLock = new object();

        private const string RECIPES_FILE = "recipes.json";
        private const int MAX_QUEUE_SIZE = 5; // Máximo de itens na fila de crafting

        public CraftingManager()
        {
            _recipes = new Dictionary<int, CraftingRecipe>();
            _playerCraftingQueues = new Dictionary<int, List<CraftingProgress>>();
        }

        /// <summary>
        /// Inicializa o sistema de crafting
        /// </summary>
        public void Initialize()
        {
            Console.WriteLine("[CraftingManager] Inicializando sistema de crafting...");

            LoadRecipes();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[CraftingManager] ✅ {_recipes.Count} receitas carregadas!");
            Console.ResetColor();
        }

        /// <summary>
        /// Carrega receitas do arquivo JSON
        /// </summary>
        private void LoadRecipes()
        {
            if (!File.Exists(RECIPES_FILE))
            {
                Console.WriteLine($"[CraftingManager] ⚠️ Arquivo {RECIPES_FILE} não encontrado, criando receitas padrão...");
                CreateDefaultRecipes();
                SaveRecipes();
                return;
            }

            try
            {
                string json = File.ReadAllText(RECIPES_FILE);
                var recipesList = JsonSerializer.Deserialize<List<CraftingRecipe>>(json);

                if (recipesList != null)
                {
                    foreach (var recipe in recipesList)
                    {
                        _recipes[recipe.Id] = recipe;
                    }

                    Console.WriteLine($"[CraftingManager] ✅ {recipesList.Count} receitas carregadas de {RECIPES_FILE}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CraftingManager] ❌ Erro ao carregar receitas: {ex.Message}");
                CreateDefaultRecipes();
            }
        }

        /// <summary>
        /// Salva receitas no arquivo JSON
        /// </summary>
        private void SaveRecipes()
        {
            try
            {
                var recipesList = _recipes.Values.ToList();
                
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                string json = JsonSerializer.Serialize(recipesList, options);
                File.WriteAllText(RECIPES_FILE, json);

                Console.WriteLine($"[CraftingManager] ✅ Receitas salvas em {RECIPES_FILE}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CraftingManager] ❌ Erro ao salvar receitas: {ex.Message}");
            }
        }

        /// <summary>
        /// Cria receitas padrão
        /// </summary>
        private void CreateDefaultRecipes()
        {
            Console.WriteLine("[CraftingManager] Criando receitas padrão...");

            // === FERRAMENTAS BÁSICAS ===

            // Machado de Pedra (Stone Hatchet)
            AddRecipe(new CraftingRecipe
            {
                Id = 1,
                Name = "Stone Hatchet",
                Description = "Ferramenta básica para cortar árvores",
                ResultItemId = 201, // ID do machado
                ResultQuantity = 1,
                CraftingTime = 3f,
                RequiredWorkbench = 0,
                Category = "Tools",
                IsDefault = true,
                Ingredients = new List<CraftingIngredient>
                {
                    new CraftingIngredient { ItemId = 100, Quantity = 10 }, // 10 Wood
                    new CraftingIngredient { ItemId = 101, Quantity = 5 }   // 5 Stone
                }
            });

            // Picareta de Pedra (Stone Pickaxe)
            AddRecipe(new CraftingRecipe
            {
                Id = 2,
                Name = "Stone Pickaxe",
                Description = "Ferramenta básica para minerar",
                ResultItemId = 202,
                ResultQuantity = 1,
                CraftingTime = 3f,
                RequiredWorkbench = 0,
                Category = "Tools",
                IsDefault = true,
                Ingredients = new List<CraftingIngredient>
                {
                    new CraftingIngredient { ItemId = 100, Quantity = 10 }, // 10 Wood
                    new CraftingIngredient { ItemId = 101, Quantity = 8 }   // 8 Stone
                }
            });

            // === ARMAS ===

            // Lança (Spear)
            AddRecipe(new CraftingRecipe
            {
                Id = 3,
                Name = "Wooden Spear",
                Description = "Arma de curto alcance",
                ResultItemId = 301,
                ResultQuantity = 1,
                CraftingTime = 5f,
                RequiredWorkbench = 0,
                Category = "Weapons",
                IsDefault = true,
                Ingredients = new List<CraftingIngredient>
                {
                    new CraftingIngredient { ItemId = 100, Quantity = 30 }, // 30 Wood
                    new CraftingIngredient { ItemId = 101, Quantity = 5 }   // 5 Stone
                }
            });

            // Arco (Bow)
            AddRecipe(new CraftingRecipe
            {
                Id = 4,
                Name = "Hunting Bow",
                Description = "Arma de longo alcance básica",
                ResultItemId = 302,
                ResultQuantity = 1,
                CraftingTime = 8f,
                RequiredWorkbench = 0,
                Category = "Weapons",
                IsDefault = true,
                Ingredients = new List<CraftingIngredient>
                {
                    new CraftingIngredient { ItemId = 100, Quantity = 50 }, // 50 Wood
                    new CraftingIngredient { ItemId = 400, Quantity = 10 }  // 10 Cloth (precisa adicionar)
                }
            });

            // === CONSTRUÇÃO ===

            // Fundação de Madeira (Wood Foundation)
            AddRecipe(new CraftingRecipe
            {
                Id = 5,
                Name = "Wood Foundation",
                Description = "Fundação básica de madeira",
                ResultItemId = 501,
                ResultQuantity = 1,
                CraftingTime = 10f,
                RequiredWorkbench = 0,
                Category = "Building",
                IsDefault = true,
                Ingredients = new List<CraftingIngredient>
                {
                    new CraftingIngredient { ItemId = 100, Quantity = 100 } // 100 Wood
                }
            });

            // Parede de Madeira (Wood Wall)
            AddRecipe(new CraftingRecipe
            {
                Id = 6,
                Name = "Wood Wall",
                Description = "Parede básica de madeira",
                ResultItemId = 502,
                ResultQuantity = 1,
                CraftingTime = 5f,
                RequiredWorkbench = 0,
                Category = "Building",
                IsDefault = true,
                Ingredients = new List<CraftingIngredient>
                {
                    new CraftingIngredient { ItemId = 100, Quantity = 50 } // 50 Wood
                }
            });

            // Porta de Madeira (Wood Door)
            AddRecipe(new CraftingRecipe
            {
                Id = 7,
                Name = "Wood Door",
                Description = "Porta de madeira",
                ResultItemId = 503,
                ResultQuantity = 1,
                CraftingTime = 8f,
                RequiredWorkbench = 0,
                Category = "Building",
                IsDefault = true,
                Ingredients = new List<CraftingIngredient>
                {
                    new CraftingIngredient { ItemId = 100, Quantity = 75 } // 75 Wood
                }
            });

            // === WORKBENCH ===

            // Workbench Level 1
            AddRecipe(new CraftingRecipe
            {
                Id = 8,
                Name = "Workbench Level 1",
                Description = "Permite craftar itens mais avançados",
                ResultItemId = 601,
                ResultQuantity = 1,
                CraftingTime = 15f,
                RequiredWorkbench = 0,
                Category = "Crafting",
                IsDefault = true,
                Ingredients = new List<CraftingIngredient>
                {
                    new CraftingIngredient { ItemId = 100, Quantity = 200 }, // 200 Wood
                    new CraftingIngredient { ItemId = 101, Quantity = 50 }   // 50 Stone
                }
            });

            // === FERRAMENTAS AVANÇADAS (Requer Workbench 1) ===

            // Machado de Metal (Metal Hatchet)
            AddRecipe(new CraftingRecipe
            {
                Id = 9,
                Name = "Metal Hatchet",
                Description = "Machado de metal mais eficiente",
                ResultItemId = 203,
                ResultQuantity = 1,
                CraftingTime = 10f,
                RequiredWorkbench = 1,
                Category = "Tools",
                IsDefault = false,
                Ingredients = new List<CraftingIngredient>
                {
                    new CraftingIngredient { ItemId = 100, Quantity = 15 }, // 15 Wood
                    new CraftingIngredient { ItemId = 102, Quantity = 20 }  // 20 Metal
                }
            });

            // Picareta de Metal (Metal Pickaxe)
            AddRecipe(new CraftingRecipe
            {
                Id = 10,
                Name = "Metal Pickaxe",
                Description = "Picareta de metal mais eficiente",
                ResultItemId = 204,
                ResultQuantity = 1,
                CraftingTime = 10f,
                RequiredWorkbench = 1,
                Category = "Tools",
                IsDefault = false,
                Ingredients = new List<CraftingIngredient>
                {
                    new CraftingIngredient { ItemId = 100, Quantity = 15 }, // 15 Wood
                    new CraftingIngredient { ItemId = 102, Quantity = 25 }  // 25 Metal
                }
            });

            // === ARMAS AVANÇADAS ===

            // Revólver (Revolver)
            AddRecipe(new CraftingRecipe
            {
                Id = 11,
                Name = "Revolver",
                Description = "Arma de fogo básica",
                ResultItemId = 303,
                ResultQuantity = 1,
                CraftingTime = 20f,
                RequiredWorkbench = 1,
                Category = "Weapons",
                IsDefault = false,
                Ingredients = new List<CraftingIngredient>
                {
                    new CraftingIngredient { ItemId = 100, Quantity = 50 },  // 50 Wood
                    new CraftingIngredient { ItemId = 102, Quantity = 75 }   // 75 Metal
                }
            });

            // Munição (Pistol Ammo)
            AddRecipe(new CraftingRecipe
            {
                Id = 12,
                Name = "Pistol Ammo",
                Description = "Munição para revólver",
                ResultItemId = 304,
                ResultQuantity = 10,
                CraftingTime = 5f,
                RequiredWorkbench = 1,
                Category = "Weapons",
                IsDefault = false,
                Ingredients = new List<CraftingIngredient>
                {
                    new CraftingIngredient { ItemId = 102, Quantity = 5 },   // 5 Metal
                    new CraftingIngredient { ItemId = 103, Quantity = 10 }   // 10 Sulfur
                }
            });

            // === CONSTRUÇÃO AVANÇADA ===

            // Fundação de Pedra (Stone Foundation)
            AddRecipe(new CraftingRecipe
            {
                Id = 13,
                Name = "Stone Foundation",
                Description = "Fundação mais resistente de pedra",
                ResultItemId = 504,
                ResultQuantity = 1,
                CraftingTime = 15f,
                RequiredWorkbench = 1,
                Category = "Building",
                IsDefault = false,
                Ingredients = new List<CraftingIngredient>
                {
                    new CraftingIngredient { ItemId = 101, Quantity = 300 } // 300 Stone
                }
            });

            Console.WriteLine($"[CraftingManager] ✅ {_recipes.Count} receitas padrão criadas");
        }

        /// <summary>
        /// Adiciona uma receita
        /// </summary>
        private void AddRecipe(CraftingRecipe recipe)
        {
            _recipes[recipe.Id] = recipe;
        }

        /// <summary>
        /// Inicia crafting de uma receita
        /// </summary>
        public CraftResult StartCrafting(int playerId, int recipeId, PlayerInventory inventory)
        {
            lock (_craftingLock)
            {
                // Verifica se a receita existe
                if (!_recipes.TryGetValue(recipeId, out var recipe))
                {
                    return new CraftResult
                    {
                        Success = false,
                        Message = "Receita não encontrada"
                    };
                }

                // Verifica se tem workbench necessária (TODO: implementar sistema de workbench)
                // if (recipe.RequiredWorkbench > 0) { ... }

                // Verifica se tem espaço na fila
                if (!_playerCraftingQueues.ContainsKey(playerId))
                {
                    _playerCraftingQueues[playerId] = new List<CraftingProgress>();
                }

                var queue = _playerCraftingQueues[playerId];

                if (queue.Count >= MAX_QUEUE_SIZE)
                {
                    return new CraftResult
                    {
                        Success = false,
                        Message = "Fila de crafting cheia"
                    };
                }

                // Verifica e consome recursos
                if (!recipe.CanCraft(inventory))
                {
                    return new CraftResult
                    {
                        Success = false,
                        Message = "Recursos insuficientes"
                    };
                }

                if (!recipe.ConsumeIngredients(inventory))
                {
                    return new CraftResult
                    {
                        Success = false,
                        Message = "Erro ao consumir recursos"
                    };
                }

                // Adiciona à fila
                var progress = new CraftingProgress(recipeId, playerId, recipe.CraftingTime);
                queue.Add(progress);

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"[CraftingManager] 🔨 Player {playerId} iniciou crafting de {recipe.Name}");
                Console.ResetColor();

                return new CraftResult
                {
                    Success = true,
                    Message = $"Crafting iniciado: {recipe.Name}",
                    RecipeId = recipeId,
                    Duration = recipe.CraftingTime
                };
            }
        }

        /// <summary>
        /// Cancela crafting
        /// </summary>
        public bool CancelCrafting(int playerId, int queueIndex)
        {
            lock (_craftingLock)
            {
                if (!_playerCraftingQueues.TryGetValue(playerId, out var queue))
                    return false;

                if (queueIndex < 0 || queueIndex >= queue.Count)
                    return false;

                queue.RemoveAt(queueIndex);
                return true;
            }
        }

        /// <summary>
        /// Atualiza craftings em progresso
        /// </summary>
        public List<CraftCompleteResult> Update()
        {
            var completedCrafts = new List<CraftCompleteResult>();

            lock (_craftingLock)
            {
                foreach (var kvp in _playerCraftingQueues.ToList())
                {
                    int playerId = kvp.Key;
                    var queue = kvp.Value;

                    for (int i = queue.Count - 1; i >= 0; i--)
                    {
                        var progress = queue[i];

                        if (progress.CheckCompletion())
                        {
                            // Crafting completo!
                            var recipe = _recipes[progress.RecipeId];

                            completedCrafts.Add(new CraftCompleteResult
                            {
                                PlayerId = playerId,
                                RecipeId = progress.RecipeId,
                                ResultItemId = recipe.ResultItemId,
                                ResultQuantity = recipe.ResultQuantity
                            });

                            queue.RemoveAt(i);

                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"[CraftingManager] ✅ Player {playerId} completou crafting de {recipe.Name}");
                            Console.ResetColor();
                        }
                    }
                }
            }

            return completedCrafts;
        }

        /// <summary>
        /// Pega fila de crafting de um player
        /// </summary>
        public List<CraftingProgress> GetPlayerQueue(int playerId)
        {
            lock (_craftingLock)
            {
                if (_playerCraftingQueues.TryGetValue(playerId, out var queue))
                {
                    return new List<CraftingProgress>(queue);
                }
                return new List<CraftingProgress>();
            }
        }

        /// <summary>
        /// Pega todas as receitas
        /// </summary>
        public List<CraftingRecipe> GetAllRecipes()
        {
            return _recipes.Values.ToList();
        }

        /// <summary>
        /// Pega receita por ID
        /// </summary>
        public CraftingRecipe GetRecipe(int recipeId)
        {
            return _recipes.TryGetValue(recipeId, out var recipe) ? recipe : null;
        }

        /// <summary>
        /// Pega receitas desbloqueadas por um player
        /// </summary>
        public List<CraftingRecipe> GetUnlockedRecipes(int playerLevel)
        {
            return _recipes.Values
                .Where(r => r.IsDefault || r.UnlockLevel <= playerLevel)
                .ToList();
        }
    }

    /// <summary>
    /// Resultado de iniciar crafting
    /// </summary>
    public class CraftResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int RecipeId { get; set; }
        public float Duration { get; set; }
    }

    /// <summary>
    /// Resultado de crafting completo
    /// </summary>
    public class CraftCompleteResult
    {
        public int PlayerId { get; set; }
        public int RecipeId { get; set; }
        public int ResultItemId { get; set; }
        public int ResultQuantity { get; set; }
    }
}