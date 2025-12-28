using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace RustlikeClient.UI
{
    /// <summary>
    /// Botão de receita no menu de crafting
    /// </summary>
    public class RecipeButtonUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI References")]
        public Image itemIcon;
        public TextMeshProUGUI itemNameText;
        public TextMeshProUGUI craftTimeText;
        public TextMeshProUGUI ingredientsText;
        public Button craftButton;
        public Image backgroundImage;

        [Header("Colors")]
        public Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        public Color hoverColor = new Color(0.3f, 0.3f, 0.3f, 0.9f);
        public Color canCraftColor = new Color(0.2f, 0.4f, 0.2f, 0.9f);
        public Color cannotCraftColor = new Color(0.4f, 0.2f, 0.2f, 0.9f);

        private Crafting.CraftingRecipeData _recipe;
        private bool _canCraft = false;

        private void Awake()
        {
            if (craftButton != null)
            {
                craftButton.onClick.AddListener(OnCraftClicked);
            }
        }

        /// <summary>
        /// Configura receita
        /// </summary>
        public void SetRecipe(Crafting.CraftingRecipeData recipe)
        {
            _recipe = recipe;
            UpdateVisuals();
            CheckCanCraft();
        }

        /// <summary>
        /// Atualiza visuais
        /// </summary>
        private void UpdateVisuals()
        {
            if (_recipe == null) return;

            // Nome do item
            if (itemNameText != null)
            {
                itemNameText.text = _recipe.recipeName;
            }

            // Ícone
            if (itemIcon != null)
            {
                var icon = _recipe.GetResultItemIcon();
                if (icon != null)
                {
                    itemIcon.sprite = icon;
                    itemIcon.gameObject.SetActive(true);
                    itemIcon.color = Color.white;
                }
                else
                {
                    // Placeholder
                    itemIcon.gameObject.SetActive(true);
                    itemIcon.color = GetColorForItem(_recipe.resultItemId);
                }
            }

            // Tempo de crafting
            if (craftTimeText != null)
            {
                craftTimeText.text = $"{_recipe.craftingTime:F0}s";
            }

            // Ingredientes
            if (ingredientsText != null)
            {
                ingredientsText.text = _recipe.GetIngredientsDescription();
            }
        }

        /// <summary>
        /// Verifica se pode craftar
        /// </summary>
        private void CheckCanCraft()
        {
            if (_recipe == null) return;

            _canCraft = _recipe.CanCraft(InventoryManager.Instance);

            // Atualiza cor do fundo
            if (backgroundImage != null)
            {
                backgroundImage.color = _canCraft ? canCraftColor : cannotCraftColor;
            }

            // Habilita/desabilita botão
            if (craftButton != null)
            {
                craftButton.interactable = _canCraft;
            }
        }

        /// <summary>
        /// Callback do botão de craft
        /// </summary>
        private void OnCraftClicked()
        {
            if (_recipe == null) return;

            Debug.Log($"[RecipeButtonUI] Solicitando craft de {_recipe.recipeName}");

            if (Crafting.CraftingManager.Instance != null)
            {
                Crafting.CraftingManager.Instance.RequestCraft(_recipe.id);
            }
        }

        /// <summary>
        /// Mouse enter
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (backgroundImage != null && !_canCraft)
            {
                backgroundImage.color = hoverColor;
            }

            // Mostra tooltip
            if (_recipe != null && TooltipUI.Instance != null)
            {
                string description = $"{_recipe.recipeName}\n\nTempo: {_recipe.craftingTime}s\n\nIngredientes:\n{_recipe.GetIngredientsDescription()}";
                
                if (_recipe.requiredWorkbench > 0)
                {
                    description += $"\n\nRequer: Workbench Nível {_recipe.requiredWorkbench}";
                }

                TooltipUI.Instance.Show(_recipe.recipeName, description, Input.mousePosition);
            }
        }

        /// <summary>
        /// Mouse exit
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            if (backgroundImage != null)
            {
                backgroundImage.color = _canCraft ? canCraftColor : cannotCraftColor;
            }

            // Esconde tooltip
            if (TooltipUI.Instance != null)
            {
                TooltipUI.Instance.Hide();
            }
        }

        /// <summary>
        /// Cor placeholder baseada no ID
        /// </summary>
        private Color GetColorForItem(int itemId)
        {
            if (itemId >= 200 && itemId < 300) return new Color(0.6f, 0.6f, 0.6f); // Ferramentas
            if (itemId >= 300 && itemId < 400) return new Color(0.8f, 0.3f, 0.3f); // Armas
            if (itemId >= 500 && itemId < 600) return new Color(0.6f, 0.4f, 0.2f); // Construção
            if (itemId >= 600 && itemId < 700) return new Color(0.4f, 0.6f, 0.8f); // Crafting
            return Color.white;
        }

        private void Update()
        {
            // Atualiza periodicamente se pode craftar
            if (Time.frameCount % 60 == 0) // A cada 1s
            {
                CheckCanCraft();
            }
        }

        private void OnDestroy()
        {
            if (craftButton != null)
            {
                craftButton.onClick.RemoveListener(OnCraftClicked);
            }
        }
    }
}