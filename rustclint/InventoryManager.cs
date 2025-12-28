using System.Collections.Generic;
using UnityEngine;

namespace RustlikeClient.UI
{
    /// <summary>
    /// ⭐ ATUALIZADO: Sistema completo de uso de itens (Hotbar + Double Click)
    /// </summary>
    public class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance { get; private set; }

        [Header("Settings")]
        public const int INVENTORY_SIZE = 24;
        public const int HOTBAR_SIZE = 6;

        [Header("Input Settings")]
        [Tooltip("Tecla para abrir/fechar inventário")]
        public KeyCode inventoryKey = KeyCode.E;
        
        [Tooltip("Teclas alternativas para abrir inventário")]
        public KeyCode[] alternativeKeys = { KeyCode.Tab, KeyCode.I };

        [Header("Audio Feedback (Optional)")]
        public AudioClip inventoryOpenSound;
        public AudioClip inventoryCloseSound;
        public AudioClip itemUseSound;
        public AudioClip itemMoveSound;
        
        private AudioSource _audioSource;

        // Estado local do inventário (sincronizado com servidor)
        private Dictionary<int, SlotData> _slots = new Dictionary<int, SlotData>();
        private int _selectedHotbarSlot = 0;

        // Referências de UI
        private InventoryUI _inventoryUI;
        private HotbarUI _hotbarUI;

        // Estado do cursor antes de abrir inventário
        private CursorLockMode _previousCursorLockMode;
        private bool _previousCursorVisible;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Inicializa slots vazios
            for (int i = 0; i < INVENTORY_SIZE; i++)
            {
                _slots[i] = new SlotData { itemId = -1, quantity = 0 };
            }

            // Setup audio
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f; // 2D sound

            Debug.Log("[InventoryManager] Inicializado com sistema de uso de itens");
        }

        private void Start()
        {
            _inventoryUI = FindObjectOfType<InventoryUI>();
            _hotbarUI = FindObjectOfType<HotbarUI>();

            if (_inventoryUI == null)
            {
                Debug.LogWarning("[InventoryManager] InventoryUI não encontrado na cena!");
            }

            if (_hotbarUI == null)
            {
                Debug.LogWarning("[InventoryManager] HotbarUI não encontrado na cena!");
            }
        }

        /// <summary>
        /// Atualiza inventário completo (recebido do servidor)
        /// </summary>
        public void UpdateInventory(Network.InventoryUpdatePacket packet)
        {
            Debug.Log($"[InventoryManager] 📦 Recebendo update do servidor: {packet.Slots.Count} itens");

            // Limpa todos os slots
            for (int i = 0; i < INVENTORY_SIZE; i++)
            {
                _slots[i] = new SlotData { itemId = -1, quantity = 0 };
            }

            // Atualiza com dados do servidor
            foreach (var slotData in packet.Slots)
            {
                _slots[slotData.SlotIndex] = new SlotData
                {
                    itemId = slotData.ItemId,
                    quantity = slotData.Quantity
                };

                Debug.Log($"  → Slot {slotData.SlotIndex}: Item {slotData.ItemId} x{slotData.Quantity}");
            }

            // Atualiza UI
            RefreshUI();
        }

        /// <summary>
        /// ⭐ NOVO: Usa item do slot (envia para servidor e mostra feedback)
        /// </summary>
        public async void UseItem(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= INVENTORY_SIZE) return;
            if (_slots[slotIndex].itemId <= 0) return;

            // Verifica se é consumível
            var itemData = Items.ItemDatabase.Instance?.GetItem(_slots[slotIndex].itemId);
            if (itemData == null)
            {
                Debug.LogWarning($"[InventoryManager] Item {_slots[slotIndex].itemId} não encontrado no database");
                return;
            }

            if (!itemData.isConsumable)
            {
                Debug.Log($"[InventoryManager] ⚠️ {itemData.itemName} não é consumível");
                ShowNotification($"{itemData.itemName} não pode ser usado", NotificationType.Warning);
                return;
            }

            Debug.Log($"[InventoryManager] 🍴 Usando {itemData.itemName} do slot {slotIndex}");

            // Feedback visual imediato (animação do slot)
            AnimateSlotUse(slotIndex);

            // Envia para servidor
            var packet = new Network.ItemUsePacket { SlotIndex = slotIndex };
            await Network.NetworkManager.Instance.SendPacketAsync(
                Network.PacketType.ItemUse,
                packet.Serialize()
            );

            // Feedback sonoro
            PlaySound(itemUseSound);

            // Notificação
            ShowNotification($"Usou {itemData.itemName}", NotificationType.Success);
        }

        /// <summary>
        /// Move item entre slots (envia para servidor)
        /// </summary>
        public async void MoveItem(int fromSlot, int toSlot)
        {
            if (fromSlot == toSlot) return;
            if (fromSlot < 0 || fromSlot >= INVENTORY_SIZE) return;
            if (toSlot < 0 || toSlot >= INVENTORY_SIZE) return;

            Debug.Log($"[InventoryManager] 🔄 Movendo item: {fromSlot} → {toSlot}");

            var packet = new Network.ItemMovePacket
            {
                FromSlot = fromSlot,
                ToSlot = toSlot
            };

            await Network.NetworkManager.Instance.SendPacketAsync(
                Network.PacketType.ItemMove,
                packet.Serialize()
            );

            // Feedback imediato
            PlaySound(itemMoveSound);
        }

        /// <summary>
        /// Seleciona slot da hotbar (teclas 1-6)
        /// </summary>
        public void SelectHotbarSlot(int index)
        {
            if (index < 0 || index >= HOTBAR_SIZE) return;

            _selectedHotbarSlot = index;
            Debug.Log($"[InventoryManager] 🎯 Hotbar slot selecionado: {index + 1}");

            // Atualiza visual da hotbar
            if (_hotbarUI != null)
            {
                _hotbarUI.SetSelectedSlot(index);
            }
        }

        /// <summary>
        /// ⭐ NOVO: Usa item do slot selecionado da hotbar
        /// </summary>
        public void UseSelectedHotbarItem()
        {
            UseItem(_selectedHotbarSlot);
        }

        /// <summary>
        /// ⭐ NOVO: Animação visual ao usar item
        /// </summary>
        private void AnimateSlotUse(int slotIndex)
        {
            // Encontra o slot UI e anima
            InventorySlotUI slotUI = null;

            // Procura na hotbar primeiro
            if (_hotbarUI != null && slotIndex < HOTBAR_SIZE)
            {
                slotUI = _hotbarUI.GetSlotUI(slotIndex);
            }

            // Procura no inventário
            if (slotUI == null && _inventoryUI != null)
            {
                slotUI = _inventoryUI.GetSlotUI(slotIndex);
            }

            if (slotUI != null)
            {
                slotUI.PlayUseAnimation();
            }
        }

        /// <summary>
        /// Atualiza todas as UIs
        /// </summary>
        private void RefreshUI()
        {
            // Atualiza inventário completo
            if (_inventoryUI != null)
            {
                _inventoryUI.RefreshAllSlots(_slots);
            }

            // Atualiza hotbar
            if (_hotbarUI != null)
            {
                _hotbarUI.RefreshAllSlots(_slots);
            }
        }

        /// <summary>
        /// Pega dados de um slot
        /// </summary>
        public SlotData GetSlot(int index)
        {
            return _slots.TryGetValue(index, out var slot) ? slot : new SlotData { itemId = -1, quantity = 0 };
        }

        /// <summary>
        /// Verifica se tem item
        /// </summary>
        public bool HasItem(int itemId)
        {
            foreach (var slot in _slots.Values)
            {
                if (slot.itemId == itemId && slot.quantity > 0)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Conta quantidade de um item
        /// </summary>
        public int CountItem(int itemId)
        {
            int count = 0;
            foreach (var slot in _slots.Values)
            {
                if (slot.itemId == itemId)
                    count += slot.quantity;
            }
            return count;
        }

        /// <summary>
        /// Toca som de feedback
        /// </summary>
        private void PlaySound(AudioClip clip)
        {
            if (clip != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(clip);
            }
        }

        /// <summary>
        /// ⭐ NOVO: Mostra notificação
        /// </summary>
        private void ShowNotification(string message, NotificationType type)
        {
            if (NotificationManager.Instance != null)
            {
                switch (type)
                {
                    case NotificationType.Success:
                        NotificationManager.Instance.ShowSuccess(message);
                        break;
                    case NotificationType.Warning:
                        NotificationManager.Instance.ShowWarning(message);
                        break;
                    case NotificationType.Error:
                        NotificationManager.Instance.ShowError(message);
                        break;
                    default:
                        NotificationManager.Instance.ShowNotification(message);
                        break;
                }
            }
        }

        /// <summary>
        /// Abre inventário
        /// </summary>
        public void OpenInventory()
        {
            if (_inventoryUI == null) return;
            if (_inventoryUI.IsOpen()) return;

            _inventoryUI.Open();
            PlaySound(inventoryOpenSound);

            Debug.Log("[InventoryManager] 📂 Inventário aberto");
        }

        /// <summary>
        /// Fecha inventário
        /// </summary>
        public void CloseInventory()
        {
            if (_inventoryUI == null) return;
            if (!_inventoryUI.IsOpen()) return;

            _inventoryUI.Close();
            PlaySound(inventoryCloseSound);

            Debug.Log("[InventoryManager] 📁 Inventário fechado");
        }

        /// <summary>
        /// Alterna inventário
        /// </summary>
        public void ToggleInventory()
        {
            if (_inventoryUI == null) return;

            if (_inventoryUI.IsOpen())
            {
                CloseInventory();
            }
            else
            {
                OpenInventory();
            }
        }

        /// <summary>
        /// Verifica se o inventário está aberto
        /// </summary>
        public bool IsInventoryOpen()
        {
            return _inventoryUI != null && _inventoryUI.IsOpen();
        }

        /// <summary>
        /// ⭐ ATUALIZADO: Hotkeys do inventário + USO DE ITENS
        /// </summary>
        private void Update()
        {
            // Tecla E para abrir/fechar inventário
            if (Input.GetKeyDown(inventoryKey))
            {
                ToggleInventory();
            }

            // Teclas alternativas (Tab, I)
            foreach (var key in alternativeKeys)
            {
                if (Input.GetKeyDown(key))
                {
                    ToggleInventory();
                    break;
                }
            }

            // ESC fecha inventário
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (IsInventoryOpen())
                {
                    CloseInventory();
                }
            }

            // ⭐ NOVO: Teclas 1-6 para SELECIONAR e USAR da hotbar
            if (!IsInventoryOpen())
            {
                for (int i = 0; i < HOTBAR_SIZE; i++)
                {
                    // Detecta tecla pressionada (Alpha1 = tecla 1, Alpha2 = tecla 2, etc)
                    if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                    {
                        SelectHotbarSlot(i);
                        
                        // ⭐ USA ITEM IMEDIATAMENTE ao pressionar a tecla
                        UseSelectedHotbarItem();
                    }
                }

                // Mouse scroll: Navega hotbar (SEM usar)
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                if (scroll > 0f)
                {
                    SelectHotbarSlot((_selectedHotbarSlot - 1 + HOTBAR_SIZE) % HOTBAR_SIZE);
                }
                else if (scroll < 0f)
                {
                    SelectHotbarSlot((_selectedHotbarSlot + 1) % HOTBAR_SIZE);
                }
            }
        }
    }

    /// <summary>
    /// Dados de um slot do inventário
    /// </summary>
    [System.Serializable]
    public class SlotData
    {
        public int itemId;
        public int quantity;
    }

    /// <summary>
    /// Tipos de notificação
    /// </summary>
    public enum NotificationType
    {
        Success,
        Warning,
        Error,
        Info
    }
}