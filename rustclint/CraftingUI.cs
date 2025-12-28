using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RustlikeClient.UI
{
    /// <summary>
    /// Interface principal do sistema de crafting
    /// </summary>
    public class CraftingUI : MonoBehaviour
    {
        public static CraftingUI Instance { get; private set; }

        [Header("UI References")]
        public GameObject craftingPanel;
        public Transform recipesContainer;
        public Transform queueContainer;
        public GameObject recipeButtonPrefab;
        public GameObject queueItemPrefab;

        [Header("Category Tabs (Optional)")]
        public Button allButton;
        public Button toolsButton;
        public Button weaponsButton;
        public Button buildingButton;

        [Header("Search (Optional)")]
        public TMP_InputField searchInput;

        private List<GameObject> _recipeButtons = new List<GameObject>();
        private List<GameObject> _queueItems = new List<GameObject>();
        private string _currentFilter = "All";
        private bool _isOpen = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (craftingPanel != null)
            {
                craftingPanel.SetActive(false);
            }

            // Setup category buttons
            if (allButton != null)
                allButton.onClick.AddListener(() => FilterByCategory("All"));
            
            if (toolsButton != null)
                toolsButton.onClick.AddListener(() => FilterByCategory("Tools"));
            
            if (weaponsButton != null)
                weaponsButton.onClick.AddListener(() => FilterByCategory("Weapons"));
            
            if (buildingButton != null)
                buildingButton.onClick.AddListener(() => FilterByCategory("Building"));

            // Setup search
            if (searchInput != null)
            {
                searchInput.onValueChanged.AddListener(OnSearchChanged);
            }
        }

        private void Start()
        {
            // Subscribe to crafting events
            if (Crafting.CraftingManager.Instance != null)
            {
                Crafting.CraftingManager.Instance.OnRecipeAdded += OnRecipeAdded;
                Crafting.CraftingManager.Instance.OnQueueUpdated += RefreshQueue;
            }
        }

        /// <summary>
        /// Abre/fecha menu de crafting
        /// </summary>
        public void Toggle()
        {
            if (_isOpen)
                Close();
            else
                Open();
        }

        /// <summary>
        /// Abre menu de crafting
        /// </summary>
        public void Open()
        {
            if (craftingPanel != null)
            {
                craftingPanel.SetActive(true);
                _isOpen = true;

                // Libera cursor
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                // Refresh
                RefreshRecipes();
                RefreshQueue();

                Debug.Log("[CraftingUI] Menu de crafting aberto");
            }
        }

        /// <summary>
        /// Fecha menu de crafting
        /// </summary>
        public void Close()
        {
            if (craftingPanel != null)
            {
                craftingPanel.SetActive(false);
                _isOpen = false;

                // Trava cursor novamente
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                Debug.Log("[CraftingUI] Menu de crafting fechado");
            }
        }

        /// <summary>
        /// Callback quando uma receita é adicionada
        /// </summary>
        private void OnRecipeAdded(Crafting.CraftingRecipeData recipe)
        {
            CreateRecipeButton(recipe);
        }

        /// <summary>
        /// Cria botão de receita
        /// </summary>
        private void CreateRecipeButton(Crafting.CraftingRecipeData recipe)
        {
            if (recipeButtonPrefab == null || recipesContainer == null)
            {
                Debug.LogError("[CraftingUI] recipeButtonPrefab ou recipesContainer não configurado!");
                return;
            }

            GameObject buttonObj = Instantiate(recipeButtonPrefab, recipesContainer);
            
            // Configura RecipeButton
            var recipeButton = buttonObj.GetComponent<RecipeButtonUI>();
            if (recipeButton != null)
            {
                recipeButton.SetRecipe(recipe);
            }

            _recipeButtons.Add(buttonObj);
        }

        /// <summary>
        /// Atualiza todas as receitas
        /// </summary>
        public void RefreshRecipes()
        {
            // Limpa botões existentes
            foreach (var button in _recipeButtons)
            {
                Destroy(button);
            }
            _recipeButtons.Clear();

            // Cria novos botões
            var recipes = Crafting.CraftingManager.Instance?.GetAllRecipes();
            if (recipes != null)
            {
                foreach (var recipe in recipes)
                {
                    CreateRecipeButton(recipe);
                }
            }

            Debug.Log($"[CraftingUI] {_recipeButtons.Count} receitas carregadas");
        }

        /// <summary>
        /// Atualiza fila de crafting
        /// </summary>
        public void RefreshQueue()
        {
            // Limpa items existentes
            foreach (var item in _queueItems)
            {
                Destroy(item);
            }
            _queueItems.Clear();

            // Cria novos items
            var queue = Crafting.CraftingManager.Instance?.GetCraftingQueue();
            if (queue != null)
            {
                for (int i = 0; i < queue.Count; i++)
                {
                    CreateQueueItem(queue[i], i);
                }
            }
        }

        /// <summary>
        /// Cria item na fila
        /// </summary>
        private void CreateQueueItem(Crafting.CraftQueueItemData queueItem, int index)
        {
            if (queueItemPrefab == null || queueContainer == null)
            {
                Debug.LogError("[CraftingUI] queueItemPrefab ou queueContainer não configurado!");
                return;
            }

            GameObject itemObj = Instantiate(queueItemPrefab, queueContainer);
            
            // Configura QueueItemUI
            var queueItemUI = itemObj.GetComponent<QueueItemUI>();
            if (queueItemUI != null)
            {
                queueItemUI.SetQueueItem(queueItem, index);
            }

            _queueItems.Add(itemObj);
        }

        /// <summary>
        /// Filtra por categoria
        /// </summary>
        private void FilterByCategory(string category)
        {
            _currentFilter = category;
            
            // TODO: Implementar filtro real quando receitas tiverem categoria
            Debug.Log($"[CraftingUI] Filtrando por: {category}");
        }

        /// <summary>
        /// Callback de busca
        /// </summary>
        private void OnSearchChanged(string searchTerm)
        {
            // TODO: Implementar busca
            Debug.Log($"[CraftingUI] Buscando: {searchTerm}");
        }

        /// <summary>
        /// Verifica se está aberto
        /// </summary>
        public bool IsOpen() => _isOpen;

        private void Update()
        {
            // ESC fecha menu
            if (_isOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                Close();
            }

            // Atualiza queue periodicamente
            if (_isOpen && Time.frameCount % 30 == 0) // A cada 0.5s
            {
                UpdateQueueProgress();
            }
        }

        /// <summary>
        /// Atualiza progresso visual da fila
        /// </summary>
        private void UpdateQueueProgress()
        {
            var queue = Crafting.CraftingManager.Instance?.GetCraftingQueue();
            if (queue == null || queue.Count != _queueItems.Count) return;

            for (int i = 0; i < _queueItems.Count; i++)
            {
                var queueItemUI = _queueItems[i].GetComponent<QueueItemUI>();
                if (queueItemUI != null)
                {
                    queueItemUI.UpdateProgress(queue[i]);
                }
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe
            if (Crafting.CraftingManager.Instance != null)
            {
                Crafting.CraftingManager.Instance.OnRecipeAdded -= OnRecipeAdded;
                Crafting.CraftingManager.Instance.OnQueueUpdated -= RefreshQueue;
            }

            // Remove listeners
            if (allButton != null)
                allButton.onClick.RemoveAllListeners();
            
            if (toolsButton != null)
                toolsButton.onClick.RemoveAllListeners();
            
            if (weaponsButton != null)
                weaponsButton.onClick.RemoveAllListeners();
            
            if (buildingButton != null)
                buildingButton.onClick.RemoveAllListeners();

            if (searchInput != null)
                searchInput.onValueChanged.RemoveAllListeners();
        }
    }
}