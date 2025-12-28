using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using RustlikeServer.World;
using RustlikeServer.Items;

namespace RustlikeServer.Crafting
{
    /// <summary>
    /// ⭐ NOVO: Representa uma receita de crafting
    /// </summary>
    [Serializable]
    public class CraftingRecipe
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("resultItemId")]
        public int ResultItemId { get; set; }

        [JsonPropertyName("resultQuantity")]
        public int ResultQuantity { get; set; }

        [JsonPropertyName("craftingTime")]
        public float CraftingTime { get; set; } // Em segundos

        [JsonPropertyName("requiredWorkbench")]
        public int RequiredWorkbench { get; set; } // 0=None, 1=WB1, 2=WB2, 3=WB3

        [JsonPropertyName("category")]
        public string Category { get; set; } // "Tools", "Weapons", "Building", "Consumables"

        [JsonPropertyName("ingredients")]
        public List<CraftingIngredient> Ingredients { get; set; }

        [JsonPropertyName("unlockLevel")]
        public int UnlockLevel { get; set; } // Nível necessário para desbloquear

        [JsonPropertyName("isDefault")]
        public bool IsDefault { get; set; } // Se está desbloqueada por padrão

        public CraftingRecipe()
        {
            Ingredients = new List<CraftingIngredient>();
        }

        /// <summary>
        /// Verifica se o player tem os recursos necessários
        /// </summary>
        public bool CanCraft(PlayerInventory inventory)
        {
            foreach (var ingredient in Ingredients)
            {
                if (!inventory.HasItem(ingredient.ItemId, ingredient.Quantity))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Consome os recursos do inventário
        /// </summary>
        public bool ConsumeIngredients(World.PlayerInventory inventory)
        {
            // Verifica novamente se tem todos os itens
            if (!CanCraft(inventory))
                return false;

            // Consome cada ingrediente
            foreach (var ingredient in Ingredients)
            {
                if (!inventory.RemoveItem(ingredient.ItemId, ingredient.Quantity))
                {
                    // Se falhar em remover algum, algo deu errado
                    Console.WriteLine($"[CraftingRecipe] ERRO: Falha ao consumir {ingredient.ItemId}");
                    return false;
                }
            }

            return true;
        }

        public override string ToString()
        {
            return $"{Name} (ID: {Id}) -> {ResultQuantity}x Item {ResultItemId} [{CraftingTime}s]";
        }
    }

    /// <summary>
    /// Ingrediente de uma receita
    /// </summary>
    [Serializable]
    public class CraftingIngredient
    {
        [JsonPropertyName("itemId")]
        public int ItemId { get; set; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        public override string ToString()
        {
            return $"{Quantity}x Item {ItemId}";
        }
    }

    /// <summary>
    /// Receita em progresso (sendo craftada)
    /// </summary>
    public class CraftingProgress
    {
        public int RecipeId { get; set; }
        public int PlayerId { get; set; }
        public DateTime StartTime { get; set; }
        public float Duration { get; set; }
        public bool IsComplete { get; set; }

        public CraftingProgress(int recipeId, int playerId, float duration)
        {
            RecipeId = recipeId;
            PlayerId = playerId;
            StartTime = DateTime.Now;
            Duration = duration;
            IsComplete = false;
        }

        /// <summary>
        /// Verifica se o crafting está completo
        /// </summary>
        public bool CheckCompletion()
        {
            if (IsComplete) return true;

            float elapsed = (float)(DateTime.Now - StartTime).TotalSeconds;
            
            if (elapsed >= Duration)
            {
                IsComplete = true;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Retorna progresso (0.0 a 1.0)
        /// </summary>
        public float GetProgress()
        {
            if (IsComplete) return 1.0f;

            float elapsed = (float)(DateTime.Now - StartTime).TotalSeconds;
            return Math.Min(1.0f, elapsed / Duration);
        }

        /// <summary>
        /// Tempo restante em segundos
        /// </summary>
        public float GetRemainingTime()
        {
            if (IsComplete) return 0f;

            float elapsed = (float)(DateTime.Now - StartTime).TotalSeconds;
            return Math.Max(0f, Duration - elapsed);
        }
    }
}