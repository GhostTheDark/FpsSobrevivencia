using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace RustlikeClient.UI
{
    /// <summary>
    /// 🐛 DEBUGGER: Visualiza problemas de drag & drop e raycasts
    /// Adicione este componente em um GameObject vazio na cena para debug
    /// </summary>
    public class InventoryDebugger : MonoBehaviour
    {
        [Header("Debug Settings")]
        public bool showDebugInfo = true;
        public bool showRaycastHits = true;
        public bool logDragEvents = true;
        public KeyCode toggleKey = KeyCode.F8;

        [Header("Visual Debug")]
        public Color raycastHitColor = Color.green;
        public float raycastHitSize = 20f;

        private TextMeshProUGUI _debugText;
        private Canvas _debugCanvas;
        private bool _isEnabled = true;

        private void Start()
        {
            CreateDebugUI();
        }

        private void Update()
        {
            // Toggle debug
            if (Input.GetKeyDown(toggleKey))
            {
                _isEnabled = !_isEnabled;
                if (_debugCanvas != null)
                    _debugCanvas.gameObject.SetActive(_isEnabled);
                
                Debug.Log($"[InventoryDebugger] Debug {(_isEnabled ? "ATIVADO" : "DESATIVADO")}");
            }

            if (!_isEnabled) return;

            UpdateDebugInfo();

            if (showRaycastHits)
            {
                DebugRaycastHits();
            }
        }

        /// <summary>
        /// Cria UI de debug
        /// </summary>
        private void CreateDebugUI()
        {
            // Cria Canvas
            GameObject canvasObj = new GameObject("DebugCanvas");
            _debugCanvas = canvasObj.AddComponent<Canvas>();
            _debugCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _debugCanvas.sortingOrder = 999; // Sempre no topo

            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            // Cria painel de fundo
            GameObject panelObj = new GameObject("DebugPanel");
            panelObj.transform.SetParent(canvasObj.transform, false);

            Image panelImage = panelObj.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.8f);

            RectTransform panelRect = panelObj.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 0);
            panelRect.anchorMax = new Vector2(0, 0);
            panelRect.pivot = new Vector2(0, 0);
            panelRect.anchoredPosition = new Vector2(10, 10);
            panelRect.sizeDelta = new Vector2(400, 300);

            // Cria texto
            GameObject textObj = new GameObject("DebugText");
            textObj.transform.SetParent(panelObj.transform, false);

            _debugText = textObj.AddComponent<TextMeshProUGUI>();
            _debugText.fontSize = 14;
            _debugText.color = Color.white;
            _debugText.alignment = TextAlignmentOptions.TopLeft;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 10);
            textRect.offsetMax = new Vector2(-10, -10);

            Debug.Log("[InventoryDebugger] ✅ Debug UI criado. Pressione F8 para toggle");
        }

        /// <summary>
        /// Atualiza informações de debug
        /// </summary>
        private void UpdateDebugInfo()
        {
            if (_debugText == null) return;

            string info = "=== INVENTORY DEBUG (F8) ===\n\n";

            // 1. EventSystem
            EventSystem eventSystem = EventSystem.current;
            info += $"EventSystem: {(eventSystem != null ? "✅ OK" : "❌ FALTA")}\n";

            if (eventSystem != null)
            {
                info += $"  Current Selected: {(eventSystem.currentSelectedGameObject != null ? eventSystem.currentSelectedGameObject.name : "None")}\n";
            }

            // 2. Canvas
            Canvas[] canvases = FindObjectsOfType<Canvas>();
            info += $"\nCanvases: {canvases.Length}\n";
            
            foreach (Canvas canvas in canvases)
            {
                if (canvas.name.Contains("Debug")) continue; // Skip debug canvas
                
                GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
                info += $"  {canvas.name}: {(raycaster != null ? "✅" : "❌ SEM RAYCASTER")}\n";
            }

            // 3. InventoryManager
            info += $"\nInventoryManager: {(InventoryManager.Instance != null ? "✅ OK" : "❌ FALTA")}\n";
            
            if (InventoryManager.Instance != null)
            {
                info += $"  Inventory Open: {InventoryManager.Instance.IsInventoryOpen()}\n";
            }

            // 4. Mouse Position & Raycasts
            info += $"\nMouse Position: {Input.mousePosition}\n";
            info += $"Mouse Buttons:\n";
            info += $"  Left: {(Input.GetMouseButton(0) ? "🖱️ PRESSED" : "Released")}\n";
            info += $"  Right: {(Input.GetMouseButton(1) ? "🖱️ PRESSED" : "Released")}\n";

            // 5. Raycast Results
            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };

            var results = new System.Collections.Generic.List<RaycastResult>();
            if (eventSystem != null)
            {
                eventSystem.RaycastAll(pointerData, results);
            }

            info += $"\nRaycast Hits: {results.Count}\n";
            
            for (int i = 0; i < Mathf.Min(results.Count, 5); i++)
            {
                var result = results[i];
                InventorySlotUI slot = result.gameObject.GetComponent<InventorySlotUI>();
                
                if (slot != null)
                {
                    info += $"  {i + 1}. 🎯 SLOT {slot.slotIndex} ({(slot.IsEmpty() ? "Empty" : $"Item {slot.GetItemId()}")})\n";
                }
                else
                {
                    info += $"  {i + 1}. {result.gameObject.name}\n";
                }
            }

            // 6. Inventory Slots
            InventorySlotUI[] slots = FindObjectsOfType<InventorySlotUI>();
            info += $"\nInventory Slots: {slots.Length}\n";
            
            int slotsWithItems = 0;
            int slotsWithCanvasGroup = 0;
            
            foreach (var slot in slots)
            {
                if (!slot.IsEmpty()) slotsWithItems++;
                if (slot.GetComponent<CanvasGroup>() != null) slotsWithCanvasGroup++;
            }
            
            info += $"  With Items: {slotsWithItems}\n";
            info += $"  With CanvasGroup: {slotsWithCanvasGroup}/{slots.Length}\n";

            if (slotsWithCanvasGroup < slots.Length)
            {
                info += $"  ⚠️ FALTAM CanvasGroups!\n";
            }

            // 7. Warnings
            info += "\n";
            
            if (eventSystem == null)
            {
                info += "❌ CRIAR EventSystem!\n";
            }
            
            if (canvases.Length == 0 || !System.Array.Exists(canvases, c => c.GetComponent<GraphicRaycaster>() != null))
            {
                info += "❌ Canvas SEM Graphic Raycaster!\n";
            }
            
            if (slotsWithCanvasGroup < slots.Length)
            {
                info += "⚠️ Alguns slots sem CanvasGroup\n";
            }

            _debugText.text = info;
        }

        /// <summary>
        /// Visualiza hits de raycast
        /// </summary>
        private void DebugRaycastHits()
        {
            if (EventSystem.current == null) return;

            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };

            var results = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            foreach (var result in results)
            {
                InventorySlotUI slot = result.gameObject.GetComponent<InventorySlotUI>();
                
                if (slot != null)
                {
                    // Desenha círculo verde nos slots detectados
                    Debug.DrawLine(
                        Camera.main.ScreenToWorldPoint(new Vector3(result.screenPosition.x - raycastHitSize, result.screenPosition.y, 10)),
                        Camera.main.ScreenToWorldPoint(new Vector3(result.screenPosition.x + raycastHitSize, result.screenPosition.y, 10)),
                        raycastHitColor
                    );
                }
            }
        }

        /// <summary>
        /// Desenha no screen space
        /// </summary>
        private void OnGUI()
        {
            if (!_isEnabled || !showRaycastHits) return;

            // Desenha círculo no mouse
            Vector3 mousePos = Input.mousePosition;
            mousePos.y = Screen.height - mousePos.y; // Inverte Y para GUI

            GUI.color = Color.yellow;
            GUI.Box(new Rect(mousePos.x - 5, mousePos.y - 5, 10, 10), "");

            // Se estiver segurando mouse, mostra indicador
            if (Input.GetMouseButton(0))
            {
                GUI.color = Color.red;
                GUI.Box(new Rect(mousePos.x - 10, mousePos.y - 10, 20, 20), "");
            }
        }

        /// <summary>
        /// Força verificação de todos os slots
        /// </summary>
        [ContextMenu("Check All Slots")]
        public void CheckAllSlots()
        {
            InventorySlotUI[] slots = FindObjectsOfType<InventorySlotUI>();
            
            Debug.Log("========== INVENTORY SLOTS CHECK ==========");
            Debug.Log($"Total Slots: {slots.Length}");
            
            int missingCanvasGroup = 0;
            int missingReferences = 0;
            
            foreach (var slot in slots)
            {
                if (slot.GetComponent<CanvasGroup>() == null)
                {
                    Debug.LogWarning($"⚠️ Slot {slot.slotIndex} sem CanvasGroup!", slot.gameObject);
                    
                    // Auto-fix
                    slot.gameObject.AddComponent<CanvasGroup>();
                    missingCanvasGroup++;
                }
                
                if (slot.itemIcon == null || slot.quantityText == null || slot.backgroundImage == null)
                {
                    Debug.LogWarning($"⚠️ Slot {slot.slotIndex} com referências faltando!", slot.gameObject);
                    missingReferences++;
                }
            }
            
            Debug.Log($"Missing CanvasGroups: {missingCanvasGroup} (auto-fixed)");
            Debug.Log($"Missing References: {missingReferences}");
            Debug.Log("==========================================");
        }

        /// <summary>
        /// Adiciona CanvasGroup em todos os slots
        /// </summary>
        [ContextMenu("Add CanvasGroup To All Slots")]
        public void AddCanvasGroupToAllSlots()
        {
            InventorySlotUI[] slots = FindObjectsOfType<InventorySlotUI>();
            int added = 0;
            
            foreach (var slot in slots)
            {
                if (slot.GetComponent<CanvasGroup>() == null)
                {
                    slot.gameObject.AddComponent<CanvasGroup>();
                    added++;
                }
            }
            
            Debug.Log($"[InventoryDebugger] ✅ Adicionado CanvasGroup em {added} slots");
        }
    }
}