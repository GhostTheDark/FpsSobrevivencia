using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RustlikeClient.Crafting
{
    /// <summary>
    /// Gerencia receitas de crafting e fila no cliente
    /// </summary>
    public class CraftingManager : MonoBehaviour
    {
        public static CraftingManager Instance { get; private set; }

        [Header("Settings")]
        public KeyCode craftingMenuKey = KeyCode.C;

        // Receitas carregadas do servidor
        private Dictionary<int, CraftingRecipeData> _recipes = new Dictionary<int, CraftingRecipeData>();

        // Fila de crafting atual
        private List<CraftQueueItemData> _craftingQueue = new List<CraftQueueItemData>();

        // Callbacks
        public System.Action<CraftingRecipeData> OnRecipeAdded;
        public System.Action<int> OnCraftStarted;
        public System.Action<int, int, int> OnCraftComplete; // recipeId, itemId, quantity
        public System.Action OnQueueUpdated;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Debug.Log("[CraftingManager] Inicializado");
        }

        private void Update()
        {
            // Toggle do menu de crafting
            if (Input.GetKeyDown(craftingMenuKey))
            {
                UI.CraftingUI.Instance?.Toggle();
            }
        }

        /// <summary>
        /// Carrega receitas recebidas do servidor
        /// </summary>
        public void LoadRecipes(List<CraftingRecipeData> recipes)
        {
            _recipes.Clear();

            foreach (var recipe in recipes)
            {
                _recipes[recipe.id] = recipe;
                OnRecipeAdded?.Invoke(recipe);
            }

            Debug.Log($"[CraftingManager] {recipes.Count} receitas carregadas");
        }

        /// <summary>
        /// Adiciona uma receita
        /// </summary>
        public void AddRecipe(CraftingRecipeData recipe)
        {
            _recipes[recipe.id] = recipe;
            OnRecipeAdded?.Invoke(recipe);
        }

        /// <summary>
        /// Solicita crafting de uma receita
        /// </summary>
        public async void RequestCraft(int recipeId)
        {
            if (!_recipes.ContainsKey(recipeId))
            {
                Debug.LogWarning($"[CraftingManager] Receita {recipeId} não encontrada");
                return;
            }

            var recipe = _recipes[recipeId];

            // Verifica se tem recursos
            if (!recipe.CanCraft(UI.InventoryManager.Instance))
            {
                Debug.LogWarning($"[CraftingManager] Recursos insuficientes para {recipe.recipeName}");
                
                if (UI.NotificationManager.Instance != null)
                {
                    UI.NotificationManager.Instance.ShowError("Recursos insuficientes!");
                }
                
                return;
            }

            Debug.Log($"[CraftingManager] Solicitando crafting de {recipe.recipeName}");

            // Envia requisição para servidor
            var packet = new Network.CraftRequestPacket
            {
                RecipeId = recipeId
            };

            await Network.NetworkManager.Instance.SendPacketAsync(
                Network.PacketType.CraftRequest,
                packet.Serialize(),
                LiteNetLib.DeliveryMethod.ReliableOrdered
            );
        }

        /// <summary>
        /// Cancela crafting da fila
        /// </summary>
        public async void CancelCraft(int queueIndex)
        {
            if (queueIndex < 0 || queueIndex >= _craftingQueue.Count)
            {
                Debug.LogWarning($"[CraftingManager] Índice inválido: {queueIndex}");
                return;
            }

            Debug.Log($"[CraftingManager] Cancelando crafting no índice {queueIndex}");

            var packet = new Network.CraftCancelPacket
            {
                QueueIndex = queueIndex
            };

            await Network.NetworkManager.Instance.SendPacketAsync(
                Network.PacketType.CraftCancel,
                packet.Serialize(),
                LiteNetLib.DeliveryMethod.ReliableOrdered
            );
        }

        /// <summary>
        /// Notificação de crafting iniciado
        /// </summary>
        public void OnCraftStartedResponse(int recipeId, float duration, bool success, string message)
        {
            if (success)
            {
                Debug.Log($"[CraftingManager] ✅ Crafting iniciado: Receita {recipeId} ({duration}s)");
                
                if (UI.NotificationManager.Instance != null)
                {
                    var recipe = GetRecipe(recipeId);
                    string recipeName = recipe != null ? recipe.recipeName : $"Recipe {recipeId}";
                    UI.NotificationManager.Instance.ShowSuccess($"Crafting iniciado: {recipeName}");
                }

                OnCraftStarted?.Invoke(recipeId);
            }
            else
            {
                Debug.LogWarning($"[CraftingManager] ❌ Falha no crafting: {message}");
                
                if (UI.NotificationManager.Instance != null)
                {
                    UI.NotificationManager.Instance.ShowError(message);
                }
            }
        }

        /// <summary>
        /// Notificação de crafting completo
        /// </summary>
        public void OnCraftCompleted(int recipeId, int resultItemId, int resultQuantity)
        {
            Debug.Log($"[CraftingManager] ✅ Crafting completo! Receita {recipeId} -> {resultQuantity}x Item {resultItemId}");

            var recipe = GetRecipe(recipeId);
            
            if (UI.NotificationManager.Instance != null)
            {
                string itemName = recipe != null ? recipe.GetResultItemName() : $"Item {resultItemId}";
                UI.NotificationManager.Instance.ShowSuccess($"Crafting completo: {resultQuantity}x {itemName}");
            }

            OnCraftComplete?.Invoke(recipeId, resultItemId, resultQuantity);
        }

        /// <summary>
        /// Atualiza fila de crafting
        /// </summary>
        public void UpdateQueue(List<CraftQueueItemData> queueItems)
        {
            _craftingQueue = new List<CraftQueueItemData>(queueItems);
            OnQueueUpdated?.Invoke();
        }

        /// <summary>
        /// Pega receita por ID
        /// </summary>
        public CraftingRecipeData GetRecipe(int recipeId)
        {
            return _recipes.TryGetValue(recipeId, out var recipe) ? recipe : null;
        }

        /// <summary>
        /// Pega todas as receitas
        /// </summary>
        public List<CraftingRecipeData> GetAllRecipes()
        {
            return _recipes.Values.ToList();
        }

        /// <summary>
        /// Pega receitas por categoria
        /// </summary>
        public List<CraftingRecipeData> GetRecipesByCategory(string category)
        {
            // Por enquanto retorna todas (categorias virão nas receitas)
            return GetAllRecipes();
        }

        /// <summary>
        /// Pega fila de crafting atual
        /// </summary>
        public List<CraftQueueItemData> GetCraftingQueue()
        {
            return new List<CraftQueueItemData>(_craftingQueue);
        }

        /// <summary>
        /// Verifica se está craftando
        /// </summary>
        public bool IsCrafting()
        {
            return _craftingQueue.Count > 0;
        }

        /// <summary>
        /// Pega quantidade de itens na fila
        /// </summary>
        public int GetQueueCount()
        {
            return _craftingQueue.Count;
        }

        /// <summary>
        /// Para debug
        /// </summary>
        private void OnGUI()
        {
            if (Input.GetKey(KeyCode.F9))
            {
                GUI.Box(new Rect(10, 800, 300, 150), "Crafting Manager (F9)");
                GUI.Label(new Rect(20, 825, 280, 20), $"Recipes: {_recipes.Count}");
                GUI.Label(new Rect(20, 845, 280, 20), $"Queue: {_craftingQueue.Count}/5");
                
                if (_craftingQueue.Count > 0)
                {
                    GUI.Label(new Rect(20, 865, 280, 20), "Current Craft:");
                    var first = _craftingQueue[0];
                    var recipe = GetRecipe(first.recipeId);
                    string name = recipe != null ? recipe.recipeName : $"Recipe {first.recipeId}";
                    GUI.Label(new Rect(20, 885, 280, 20), $"  {name}");
                    GUI.Label(new Rect(20, 905, 280, 20), $"  Progress: {first.GetProgressText()}");
                    GUI.Label(new Rect(20, 925, 280, 20), $"  Time: {first.GetRemainingTimeText()}");
                }
            }
        }
    }
}