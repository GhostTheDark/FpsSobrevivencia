using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RustlikeClient.UI
{
    /// <summary>
    /// Representa um item na fila de crafting
    /// </summary>
    public class QueueItemUI : MonoBehaviour
    {
        [Header("UI References")]
        public Image itemIcon;
        public TextMeshProUGUI itemNameText;
        public TextMeshProUGUI progressText;
        public TextMeshProUGUI timeText;
        public Slider progressBar;
        public Button cancelButton;

        [Header("Colors")]
        public Color normalProgressColor = new Color(0.4f, 0.8f, 0.4f);
        public Color completingProgressColor = new Color(1f, 0.8f, 0.2f);

        private Crafting.CraftQueueItemData _queueItem;
        private int _queueIndex;

        private void Awake()
        {
            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(OnCancelClicked);
            }
        }

        /// <summary>
        /// Configura item da fila
        /// </summary>
        public void SetQueueItem(Crafting.CraftQueueItemData queueItem, int index)
        {
            _queueItem = queueItem;
            _queueIndex = index;

            var recipe = Crafting.CraftingManager.Instance?.GetRecipe(queueItem.recipeId);
            
            if (recipe != null)
            {
                // Nome do item
                if (itemNameText != null)
                {
                    itemNameText.text = recipe.recipeName;
                }

                // Ícone
                if (itemIcon != null)
                {
                    var icon = recipe.GetResultItemIcon();
                    if (icon != null)
                    {
                        itemIcon.sprite = icon;
                        itemIcon.color = Color.white;
                    }
                    else
                    {
                        itemIcon.color = GetColorForItem(recipe.resultItemId);
                    }
                }
            }
            else
            {
                if (itemNameText != null)
                {
                    itemNameText.text = $"Recipe {queueItem.recipeId}";
                }
            }

            UpdateProgress(queueItem);
        }

        /// <summary>
        /// Atualiza progresso
        /// </summary>
        public void UpdateProgress(Crafting.CraftQueueItemData queueItem)
        {
            _queueItem = queueItem;

            // Barra de progresso
            if (progressBar != null)
            {
                progressBar.value = queueItem.progress;

                // Muda cor quando perto de completar
                var fill = progressBar.fillRect?.GetComponent<Image>();
                if (fill != null)
                {
                    fill.color = queueItem.progress > 0.9f ? completingProgressColor : normalProgressColor;
                }
            }

            // Texto de progresso
            if (progressText != null)
            {
                progressText.text = queueItem.GetProgressText();
            }

            // Tempo restante
            if (timeText != null)
            {
                timeText.text = queueItem.GetRemainingTimeText();
            }
        }

        /// <summary>
        /// Callback do botão cancelar
        /// </summary>
        private void OnCancelClicked()
        {
            Debug.Log($"[QueueItemUI] Cancelando crafting no índice {_queueIndex}");

            if (Crafting.CraftingManager.Instance != null)
            {
                Crafting.CraftingManager.Instance.CancelCraft(_queueIndex);
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

        private void OnDestroy()
        {
            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveListener(OnCancelClicked);
            }
        }
    }
}