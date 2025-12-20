// ============================================================================
// InventoryUI.cs
// ============================================================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int columns = 6;
    [SerializeField] private float slotSize = 70f;
    [SerializeField] private float padding = 10f;

    private InventorySystem inventory;
    private bool isOpen = false;
    private List<InventoryUISlot> slots = new List<InventoryUISlot>();
    private InventoryUISlot draggedSlot;
    private GameObject dragIcon;

    private void Awake()
    {
        CreateInventoryUI();
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (isOpen)
        {
            UpdateDrag();
            UpdateSlots();
        }
    }

    private void CreateInventoryUI()
    {
        RectTransform rect = gameObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        
        float width = columns * (slotSize + padding) + padding;
        float height = 600f;
        rect.sizeDelta = new Vector2(width, height);

        // Background
        Image bg = gameObject.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

        // Title
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(transform);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.anchoredPosition = new Vector2(0, 0);
        titleRect.sizeDelta = new Vector2(0, 40);
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "INVENTORY";
        titleText.fontSize = 24;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontStyle = FontStyles.Bold;

        // Scroll view
        CreateScrollView();
    }

    private void CreateScrollView()
    {
        GameObject scrollObj = new GameObject("ScrollView");
        scrollObj.transform.SetParent(transform);
        RectTransform scrollRect = scrollObj.AddComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0, 0);
        scrollRect.anchorMax = new Vector2(1, 1);
        scrollRect.sizeDelta = new Vector2(-20, -60);
        scrollRect.anchoredPosition = new Vector2(0, -30);

        ScrollRect scroll = scrollObj.AddComponent<ScrollRect>();
        
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollObj.transform);
        RectTransform vpRect = viewport.AddComponent<RectTransform>();
        vpRect.anchorMin = Vector2.zero;
        vpRect.anchorMax = Vector2.one;
        vpRect.sizeDelta = Vector2.zero;
        vpRect.anchoredPosition = Vector2.zero;
        viewport.AddComponent<Image>().color = new Color(0, 0, 0, 0);
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform);
        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.anchoredPosition = Vector2.zero;

        GridLayoutGroup grid = content.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(slotSize, slotSize);
        grid.spacing = new Vector2(padding, padding);
        grid.padding = new RectOffset((int)padding, (int)padding, (int)padding, (int)padding);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = columns;

        scroll.content = contentRect;
        scroll.viewport = vpRect;
        scroll.vertical = true;
        scroll.horizontal = false;
    }

    public void BindToInventory(InventorySystem inv)
    {
        inventory = inv;
        if (inventory != null)
        {
            CreateSlots();
        }
    }

    private void CreateSlots()
    {
        if (inventory == null) return;

        GameObject content = transform.Find("ScrollView/Viewport/Content")?.gameObject;
        if (content == null) return;

        int slotCount = inventory.GetSlotCount();
        for (int i = 0; i < slotCount; i++)
        {
            GameObject slotObj = new GameObject($"Slot_{i}");
            slotObj.transform.SetParent(content.transform);
            
            InventoryUISlot slot = slotObj.AddComponent<InventoryUISlot>();
            slot.Initialize(i, this);
            slots.Add(slot);
        }
    }

    public void Open()
    {
        isOpen = true;
        gameObject.SetActive(true);
    }

    public void Close()
    {
        isOpen = false;
        gameObject.SetActive(false);
    }

    public bool IsOpen() => isOpen;

    private void UpdateSlots()
    {
        if (inventory == null) return;

        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlot invSlot = inventory.GetItemAtSlot(i);
            slots[i].UpdateSlot(invSlot);
        }
    }

    public void OnSlotBeginDrag(InventoryUISlot slot)
    {
        draggedSlot = slot;
        CreateDragIcon(slot);
    }

    public void OnSlotEndDrag(InventoryUISlot targetSlot)
    {
        if (draggedSlot != null && targetSlot != null && inventory != null)
        {
            inventory.MoveItem(draggedSlot.SlotIndex, targetSlot.SlotIndex, 999);
        }

        draggedSlot = null;
        DestroyDragIcon();
    }

    private void CreateDragIcon(InventoryUISlot slot)
    {
        // TODO: Create visual feedback
    }

    private void DestroyDragIcon()
    {
        if (dragIcon != null)
        {
            Destroy(dragIcon);
        }
    }

    private void UpdateDrag()
    {
        if (dragIcon != null)
        {
            dragIcon.transform.position = Input.mousePosition;
        }
    }
}

public class InventoryUISlot : MonoBehaviour
{
    public int SlotIndex { get; private set; }
    
    private Image background;
    private Image icon;
    private TextMeshProUGUI amountText;
    private InventoryUI inventoryUI;

    public void Initialize(int index, InventoryUI ui)
    {
        SlotIndex = index;
        inventoryUI = ui;

        RectTransform rect = gameObject.AddComponent<RectTransform>();

        background = gameObject.AddComponent<Image>();
        background.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);

        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(transform);
        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.sizeDelta = new Vector2(-10, -10);
        icon = iconObj.AddComponent<Image>();
        icon.preserveAspect = true;
        icon.enabled = false;

        GameObject amountObj = new GameObject("Amount");
        amountObj.transform.SetParent(transform);
        RectTransform amountRect = amountObj.AddComponent<RectTransform>();
        amountRect.anchorMin = new Vector2(1, 0);
        amountRect.anchorMax = new Vector2(1, 0);
        amountRect.pivot = new Vector2(1, 0);
        amountRect.anchoredPosition = new Vector2(-5, 5);
        amountRect.sizeDelta = new Vector2(40, 25);
        amountText = amountObj.AddComponent<TextMeshProUGUI>();
        amountText.fontSize = 16;
        amountText.alignment = TextAlignmentOptions.BottomRight;
        amountText.fontStyle = FontStyles.Bold;

        // Add drag handlers
        // TODO: EventTrigger for drag
    }

    public void UpdateSlot(InventorySlot invSlot)
    {
        if (invSlot == null || !invSlot.HasItem())
        {
            icon.enabled = false;
            amountText.text = "";
            return;
        }

        ItemData item = ItemDatabase.Instance.GetItem(invSlot.itemId);
        if (item != null && item.icon != null)
        {
            icon.sprite = item.icon;
            icon.enabled = true;
        }

        if (invSlot.amount > 1)
        {
            amountText.text = invSlot.amount.ToString();
        }
        else
        {
            amountText.text = "";
        }
    }
}