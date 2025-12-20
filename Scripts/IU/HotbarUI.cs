// ============================================================================
// HotbarUI.cs
// ============================================================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class HotbarUI : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int slotCount = 6;
    [SerializeField] private float slotSize = 60f;
    [SerializeField] private float slotSpacing = 10f;

    [Header("Colors")]
    [SerializeField] private Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    [SerializeField] private Color selectedColor = new Color(0.8f, 0.6f, 0.2f, 0.9f);

    private InventorySystem inventory;
    private List<HotbarSlot> slots = new List<HotbarSlot>();
    private int selectedSlot = 0;

    private void Awake()
    {
        CreateHotbar();
    }

    private void Update()
    {
        HandleInput();
        UpdateSlots();
    }

    private void CreateHotbar()
    {
        RectTransform rect = gameObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0);
        rect.anchorMax = new Vector2(0.5f, 0);
        rect.pivot = new Vector2(0.5f, 0);
        rect.anchoredPosition = new Vector2(0, 20);
        rect.sizeDelta = new Vector2(slotCount * (slotSize + slotSpacing), slotSize + 40);

        for (int i = 0; i < slotCount; i++)
        {
            HotbarSlot slot = CreateSlot(i);
            slots.Add(slot);
        }

        SelectSlot(0);
    }

    private HotbarSlot CreateSlot(int index)
    {
        GameObject slotObj = new GameObject($"Slot_{index}");
        slotObj.transform.SetParent(transform);

        RectTransform rect = slotObj.AddComponent<RectTransform>();
        float xPos = (index - slotCount / 2f) * (slotSize + slotSpacing) + (slotSize + slotSpacing) / 2f;
        rect.anchoredPosition = new Vector2(xPos, slotSize / 2f);
        rect.sizeDelta = new Vector2(slotSize, slotSize);

        HotbarSlot slot = slotObj.AddComponent<HotbarSlot>();
        slot.Initialize(index, slotSize);

        return slot;
    }

    public void BindToInventory(InventorySystem inv)
    {
        inventory = inv;
        if (inventory != null)
        {
            inventory.OnHotbarSelectionChanged += OnHotbarSelectionChanged;
        }
    }

    private void HandleInput()
    {
        for (int i = 0; i < slotCount; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                SelectSlot(i);
            }
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            int newSlot = selectedSlot + (scroll > 0 ? -1 : 1);
            if (newSlot < 0) newSlot = slotCount - 1;
            if (newSlot >= slotCount) newSlot = 0;
            SelectSlot(newSlot);
        }
    }

    private void SelectSlot(int index)
    {
        selectedSlot = index;
        
        for (int i = 0; i < slots.Count; i++)
        {
            slots[i].SetSelected(i == selectedSlot);
        }

        if (inventory != null)
        {
            inventory.SelectHotbarSlot(index);
        }
    }

    private void UpdateSlots()
    {
        if (inventory == null) return;

        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlot invSlot = inventory.GetItemAtSlot(i);
            slots[i].UpdateSlot(invSlot);
        }
    }

    private void OnHotbarSelectionChanged(int slot)
    {
        SelectSlot(slot);
    }
}

public class HotbarSlot : MonoBehaviour
{
    private Image background;
    private Image icon;
    private TextMeshProUGUI amountText;
    private TextMeshProUGUI keyText;
    private int slotIndex;

    public void Initialize(int index, float size)
    {
        slotIndex = index;

        // Background
        background = gameObject.AddComponent<Image>();
        background.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        // Icon
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(transform);
        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.sizeDelta = new Vector2(-10, -10);
        iconRect.anchoredPosition = Vector2.zero;
        icon = iconObj.AddComponent<Image>();
        icon.preserveAspect = true;
        icon.enabled = false;

        // Amount
        GameObject amountObj = new GameObject("Amount");
        amountObj.transform.SetParent(transform);
        RectTransform amountRect = amountObj.AddComponent<RectTransform>();
        amountRect.anchorMin = new Vector2(1, 0);
        amountRect.anchorMax = new Vector2(1, 0);
        amountRect.pivot = new Vector2(1, 0);
        amountRect.anchoredPosition = new Vector2(-5, 5);
        amountRect.sizeDelta = new Vector2(40, 20);
        amountText = amountObj.AddComponent<TextMeshProUGUI>();
        amountText.fontSize = 14;
        amountText.alignment = TextAlignmentOptions.BottomRight;
        amountText.fontStyle = FontStyles.Bold;

        // Key hint
        GameObject keyObj = new GameObject("Key");
        keyObj.transform.SetParent(transform);
        RectTransform keyRect = keyObj.AddComponent<RectTransform>();
        keyRect.anchorMin = new Vector2(0, 1);
        keyRect.anchorMax = new Vector2(0, 1);
        keyRect.pivot = new Vector2(0, 1);
        keyRect.anchoredPosition = new Vector2(5, -5);
        keyRect.sizeDelta = new Vector2(20, 20);
        keyText = keyObj.AddComponent<TextMeshProUGUI>();
        keyText.text = (index + 1).ToString();
        keyText.fontSize = 12;
        keyText.alignment = TextAlignmentOptions.TopLeft;
    }

    public void SetSelected(bool selected)
    {
        background.color = selected ? 
            new Color(0.8f, 0.6f, 0.2f, 0.9f) : 
            new Color(0.2f, 0.2f, 0.2f, 0.8f);
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