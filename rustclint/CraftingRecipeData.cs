using System;
using System.Collections.Generic;

namespace RustlikeClient.Crafting
{
    /// <summary>
    /// Representa uma receita de crafting no cliente
    /// </summary>
    [Serializable]
    public class CraftingRecipeData
    {
        public int id;
        public string recipeName;
        public int resultItemId;
        public int resultQuantity;
        public float craftingTime;
        public int requiredWorkbench;
        public List<IngredientData> ingredients;

        public CraftingRecipeData()
        {
            ingredients = new List<IngredientData>();
        }

        /// <summary>
        /// Verifica se o player tem recursos suficientes
        /// </summary>
        public bool CanCraft(UI.InventoryManager inventoryManager)
        {
            if (inventoryManager == null) return false;

            foreach (var ingredient in ingredients)
            {
                if (!inventoryManager.HasItem(ingredient.itemId))
                    return false;

                int count = inventoryManager.CountItem(ingredient.itemId);
                if (count < ingredient.quantity)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Pega nome do resultado (do ItemDatabase)
        /// </summary>
        public string GetResultItemName()
        {
            var itemData = Items.ItemDatabase.Instance?.GetItem(resultItemId);
            return itemData != null ? itemData.itemName : $"Item {resultItemId}";
        }

        /// <summary>
        /// Pega ícone do resultado (do ItemDatabase)
        /// </summary>
        public UnityEngine.Sprite GetResultItemIcon()
        {
            var itemData = Items.ItemDatabase.Instance?.GetItem(resultItemId);
            return itemData?.icon;
        }

        /// <summary>
        /// Retorna descrição dos ingredientes
        /// </summary>
        public string GetIngredientsDescription()
        {
            var parts = new List<string>();

            foreach (var ingredient in ingredients)
            {
                var itemData = Items.ItemDatabase.Instance?.GetItem(ingredient.itemId);
                string itemName = itemData != null ? itemData.itemName : $"Item {ingredient.itemId}";
                parts.Add($"{ingredient.quantity}x {itemName}");
            }

            return string.Join(", ", parts);
        }

        public override string ToString()
        {
            return $"{recipeName} ({craftingTime}s) -> {resultQuantity}x Item {resultItemId}";
        }
    }

    /// <summary>
    /// Ingrediente de uma receita
    /// </summary>
    [Serializable]
    public class IngredientData
    {
        public int itemId;
        public int quantity;

        public override string ToString()
        {
            return $"{quantity}x Item {itemId}";
        }
    }

    /// <summary>
    /// Item na fila de crafting
    /// </summary>
    [Serializable]
    public class CraftQueueItemData
    {
        public int recipeId;
        public float progress;        // 0.0 a 1.0
        public float remainingTime;   // Em segundos

        public bool IsComplete => progress >= 1.0f;

        public string GetProgressText()
        {
            return $"{(progress * 100f):F0}%";
        }

        public string GetRemainingTimeText()
        {
            if (remainingTime <= 0f)
                return "Pronto!";

            if (remainingTime < 60f)
                return $"{remainingTime:F1}s";

            int minutes = (int)(remainingTime / 60f);
            int seconds = (int)(remainingTime % 60f);
            return $"{minutes}m {seconds}s";
        }
    }
}