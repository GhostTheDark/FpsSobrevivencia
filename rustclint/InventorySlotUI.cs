using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace RustlikeClient.UI
{
    /// <summary>
    /// ⭐ VERSÃO V2 - DRAG & DROP 100% FUNCIONAL
    /// SOLUÇÃO: OnDrop processa PRIMEIRO e seta flag, depois OnEndDrag verifica a flag
    /// </summary>
    public class InventorySlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IDropHandler
    {
        [Header("UI References")]
        public Image itemIcon;
        public TextMeshProUGUI quantityText;
        public Image highlightBorder;
        public Image backgroundImage;

        [Header("Settings")]
        public int slotIndex;
        public Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        public Color highlightColor = new Color(0.4f, 0.4f, 0.1f, 0.8f);
        public Color hotbarColor = new Color(0.3f, 0.3f, 0.3f, 0.9f);

        // Estado do slot
        private int _itemId = -1;
        private int _quantity = 0;
        private Items.ItemData _itemData;
        private bool _isEmpty = true;

        // Drag & drop
        private Canvas _canvas;
        private CanvasGroup _canvasGroup;
        private GameObject _dragIcon;
        private RectTransform _dragIconRect;
        
        // ⭐ SOLUÇÃO: Guarda informação de qual slot está sendo arrastado E se o drop foi processado
        private static InventorySlotUI _currentlyDragging = null;
        private static bool _dropProcessed = false; // ⭐ NOVA FLAG

        private void Awake()
        {
            _canvas = GetComponentInParent<Canvas>();
            
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            
            if (highlightBorder != null)
                highlightBorder.gameObject.SetActive(false);

            UpdateVisuals();
        }

        public void SetItem(int itemId, int quantity)
        {
            _itemId = itemId;
            _quantity = quantity;
            _isEmpty = (itemId <= 0 || quantity <= 0);

            if (!_isEmpty)
            {
                _itemData = Items.ItemDatabase.Instance?.GetItem(itemId);
            }
            else
            {
                _itemData = null;
            }

            UpdateVisuals();
        }

        public void Clear()
        {
            SetItem(-1, 0);
        }

        private void UpdateVisuals()
        {
            if (_isEmpty || _itemData == null)
            {
                if (itemIcon != null)
                {
                    itemIcon.gameObject.SetActive(false);
                }

                if (quantityText != null)
                {
                    quantityText.gameObject.SetActive(false);
                }
            }
            else
            {
                if (itemIcon != null)
                {
                    itemIcon.gameObject.SetActive(true);
                    itemIcon.sprite = _itemData.icon;
                    
                    if (_itemData.icon == null)
                    {
                        itemIcon.color = GetColorForItem(_itemId);
                    }
                    else
                    {
                        itemIcon.color = Color.white;
                    }
                }

                if (quantityText != null)
                {
                    quantityText.gameObject.SetActive(_quantity > 1);
                    quantityText.text = _quantity.ToString();
                }
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = slotIndex < 6 ? hotbarColor : normalColor;
            }
        }

        private Color GetColorForItem(int itemId)
        {
            if (itemId <= 3) return new Color(0.4f, 0.8f, 0.2f);
            if (itemId <= 5) return new Color(0.2f, 0.6f, 1f);
            if (itemId <= 8) return new Color(1f, 0.3f, 0.3f);
            if (itemId <= 10) return new Color(1f, 1f, 0.4f);
            if (itemId >= 100) return new Color(0.6f, 0.4f, 0.2f);
            return Color.white;
        }

        public void Highlight(bool enable)
        {
            if (highlightBorder != null)
            {
                highlightBorder.gameObject.SetActive(enable);
            }

            if (backgroundImage != null && enable)
            {
                backgroundImage.color = highlightColor;
            }
            else if (backgroundImage != null)
            {
                backgroundImage.color = slotIndex < 6 ? hotbarColor : normalColor;
            }
        }

        // ==================== DRAG & DROP (V2 CORRIGIDO) ====================

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_isEmpty)
            {
                Debug.Log($"[InventorySlot] Slot {slotIndex} está vazio");
                return;
            }

            if (_currentlyDragging != null && _currentlyDragging != this)
            {
                Debug.Log("[InventorySlot] Já existe um drag em andamento");
                return;
            }

            // ⭐ RESETA a flag de drop processado
            _dropProcessed = false;
            _currentlyDragging = this;

            Debug.Log($"[InventorySlot] ⭐ BEGIN DRAG slot {slotIndex} (Item: {_itemId})");

            CreateDragIcon();

            _canvasGroup.alpha = 0.5f;
            _canvasGroup.blocksRaycasts = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_dragIcon == null) return;

            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvas.transform as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out localPoint
            );

            _dragIconRect.localPosition = localPoint;
        }

        /// <summary>
        /// ⭐ CRÍTICO: OnDrop é chamado ANTES do OnEndDrag
        /// Aqui setamos a flag _dropProcessed = true
        /// </summary>
        public void OnDrop(PointerEventData eventData)
        {
            Debug.Log($"[InventorySlot] ⭐ ON DROP no slot {slotIndex}");

            // Verifica se há algo sendo arrastado
            if (_currentlyDragging == null)
            {
                Debug.Log($"[InventorySlot] ❌ Nenhum slot sendo arrastado (OnDrop)");
                return;
            }

            // Não pode dropar no próprio slot
            if (_currentlyDragging == this)
            {
                Debug.Log($"[InventorySlot] ❌ Tentou dropar no próprio slot");
                return;
            }

            // ⭐ MOVE O ITEM
            int fromSlot = _currentlyDragging.slotIndex;
            int toSlot = this.slotIndex;

            Debug.Log($"[InventorySlot] ✅ Dropou no slot {toSlot}, movendo de {fromSlot} → {toSlot}");

            InventoryManager.Instance?.MoveItem(fromSlot, toSlot);

            // ⭐ SETA A FLAG: Drop foi processado com sucesso!
            _dropProcessed = true;
        }

        /// <summary>
        /// ⭐ OnEndDrag é chamado DEPOIS do OnDrop
        /// Verifica se _dropProcessed foi setado
        /// </summary>
        public void OnEndDrag(PointerEventData eventData)
        {
            Debug.Log($"[InventorySlot] ⭐ END DRAG slot {slotIndex}");

            // Limpa o ícone de drag
            if (_dragIcon != null)
            {
                Destroy(_dragIcon);
                _dragIcon = null;
            }

            // Restaura visual
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;

            // ⭐ VERIFICA SE O DROP FOI PROCESSADO
            if (_dropProcessed)
            {
                Debug.Log($"[InventorySlot] ✅ Drop foi processado com sucesso!");
            }
            else
            {
                Debug.Log($"[InventorySlot] ❌ Não dropou em slot válido");
            }

            // Limpa as variáveis estáticas
            _currentlyDragging = null;
            _dropProcessed = false;
        }

        private void CreateDragIcon()
        {
            if (_canvas == null || itemIcon == null) return;

            _dragIcon = new GameObject("DragIcon");
            _dragIcon.transform.SetParent(_canvas.transform, false);
            _dragIcon.transform.SetAsLastSibling();

            Image dragImage = _dragIcon.AddComponent<Image>();
            dragImage.sprite = itemIcon.sprite;
            dragImage.color = itemIcon.color;
            dragImage.raycastTarget = false;

            _dragIconRect = _dragIcon.GetComponent<RectTransform>();
            _dragIconRect.sizeDelta = new Vector2(60, 60);
            _dragIconRect.pivot = new Vector2(0.5f, 0.5f);

            CanvasGroup dragCanvasGroup = _dragIcon.AddComponent<CanvasGroup>();
            dragCanvasGroup.alpha = 0.8f;
            dragCanvasGroup.blocksRaycasts = false;

            Debug.Log("[InventorySlot] ✅ Ícone de drag criado");
        }

        // ==================== MOUSE EVENTS ====================

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_isEmpty) return;

            if (eventData.button == PointerEventData.InputButton.Right)
            {
                Debug.Log($"[InventorySlot] 🖱️ Right click no slot {slotIndex}");
                InventoryManager.Instance?.UseItem(slotIndex);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_currentlyDragging == null)
            {
                Highlight(true);
            }

            if (!_isEmpty && _itemData != null && _currentlyDragging == null)
            {
                TooltipUI.Instance?.Show(_itemData.itemName, _itemData.description, Input.mousePosition);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_currentlyDragging == null)
            {
                Highlight(false);
            }
            
            TooltipUI.Instance?.Hide();
        }
		// ==================== FEEDBACK VISUAL ====================

public void PlayUseAnimation()
{
    if (!gameObject.activeInHierarchy) return;

    StopAllCoroutines();
    StartCoroutine(UseAnimationRoutine());
}

private System.Collections.IEnumerator UseAnimationRoutine()
{
    Vector3 originalScale = transform.localScale;
    transform.localScale = originalScale * 1.15f;

    yield return new WaitForSeconds(0.08f);

    transform.localScale = originalScale;
}


        // ==================== GETTERS ====================

        public bool IsEmpty() => _isEmpty;
        public int GetItemId() => _itemId;
        public int GetQuantity() => _quantity;
    }
}