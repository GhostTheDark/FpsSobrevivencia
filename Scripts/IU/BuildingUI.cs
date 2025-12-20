using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// UI do sistema de construção
/// Mostra peças disponíveis e custos
/// </summary>
public class BuildingUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject buildingPanel;
    [SerializeField] private Transform categoryContainer;
    [SerializeField] private Transform itemContainer;
    [SerializeField] private GameObject categoryButtonPrefab;
    [SerializeField] private GameObject buildingItemPrefab;

    [Header("Preview")]
    [SerializeField] private GameObject previewPanel;
    [SerializeField] private Image previewImage;
    [SerializeField] private TextMeshProUGUI previewName;
    [SerializeField] private TextMeshProUGUI previewDescription;
    [SerializeField] private Transform requirementsContainer;
    [SerializeField] private GameObject requirementItemPrefab;

    [Header("Colors")]
    [SerializeField] private Color selectedCategoryColor = new Color(0.8f, 0.6f, 0.2f);
    [SerializeField] private Color normalCategoryColor = new Color(0.3f, 0.3f, 0.3f);
    [SerializeField] private Color canBuildColor = Color.green;
    [SerializeField] private Color cannotBuildColor = Color.red;

    private BuildingSystem buildingSystem;
    private InventorySystem inventorySystem;
    private bool isOpen = false;
    private string currentCategory = "Foundation";
    private List<ItemData> currentItems = new List<ItemData>();
    private Dictionary<string, Button> categoryButtons = new Dictionary<string, Button>();

    private void Awake()
    {
        CreateUI();
        buildingPanel.SetActive(false);
        previewPanel.SetActive(false);
    }

    private void CreateUI()
    {
        // Cria painel principal se não existir
        if (buildingPanel == null)
        {
            GameObject panel = new GameObject("BuildingPanel");
            panel.transform.SetParent(transform);
            
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(1, 1);
            rect.sizeDelta = Vector2.zero;
            
            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.9f);
            
            buildingPanel = panel;
        }

        CreateCategoryButtons();
        CreateItemGrid();
        CreatePreviewPanel();
    }

    private void CreateCategoryButtons()
    {
        if (categoryContainer == null)
        {
            GameObject container = new GameObject("CategoryContainer");
            container.transform.SetParent(buildingPanel.transform);
            
            RectTransform rect = container.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0.9f);
            rect.anchorMax = new Vector2(1, 1);
            rect.sizeDelta = Vector2.zero;
            
            HorizontalLayoutGroup layout = container.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.padding = new RectOffset(20, 20, 10, 10);
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            
            categoryContainer = container.transform;
        }

        string[] categories = { "Foundation", "Walls", "Floors", "Doors", "Roofs", "Stairs", "Misc" };
        
        foreach (string category in categories)
        {
            GameObject btnObj = new GameObject($"Category_{category}");
            btnObj.transform.SetParent(categoryContainer);
            
            Button btn = btnObj.AddComponent<Button>();
            Image btnImage = btnObj.AddComponent<Image>();
            btnImage.color = normalCategoryColor;
            
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform);
            
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = category;
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 18;
            
            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            
            categoryButtons[category] = btn;
            
            string cat = category; // Captura para closure
            btn.onClick.AddListener(() => OnCategorySelected(cat));
        }
    }

    private void CreateItemGrid()
    {
        if (itemContainer == null)
        {
            GameObject scrollObj = new GameObject("BuildingItemsScroll");
            scrollObj.transform.SetParent(buildingPanel.transform);
            
            RectTransform scrollRect = scrollObj.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0, 0);
            scrollRect.anchorMax = new Vector2(0.7f, 0.9f);
            scrollRect.sizeDelta = Vector2.zero;
            
            ScrollRect scroll = scrollObj.AddComponent<ScrollRect>();
            
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollObj.transform);
            RectTransform vpRect = viewport.AddComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.sizeDelta = Vector2.zero;
            viewport.AddComponent<Image>();
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            
            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform);
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            
            GridLayoutGroup grid = content.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(120, 140);
            grid.spacing = new Vector2(10, 10);
            grid.padding = new RectOffset(10, 10, 10, 10);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            
            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            scroll.content = contentRect;
            scroll.viewport = vpRect;
            scroll.vertical = true;
            scroll.horizontal = false;
            
            itemContainer = content.transform;
        }
    }

    private void CreatePreviewPanel()
    {
        if (previewPanel == null)
        {
            GameObject panel = new GameObject("PreviewPanel");
            panel.transform.SetParent(buildingPanel.transform);
            
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.7f, 0);
            rect.anchorMax = new Vector2(1, 0.9f);
            rect.sizeDelta = Vector2.zero;
            
            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            
            previewPanel = panel;
        }

        // Preview Image
        GameObject imgObj = new GameObject("PreviewImage");
        imgObj.transform.SetParent(previewPanel.transform);
        RectTransform imgRect = imgObj.AddComponent<RectTransform>();
        imgRect.anchorMin = new Vector2(0.1f, 0.6f);
        imgRect.anchorMax = new Vector2(0.9f, 0.9f);
        imgRect.sizeDelta = Vector2.zero;
        previewImage = imgObj.AddComponent<Image>();
        previewImage.preserveAspect = true;

        // Preview Name
        GameObject nameObj = new GameObject("PreviewName");
        nameObj.transform.SetParent(previewPanel.transform);
        RectTransform nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0.1f, 0.5f);
        nameRect.anchorMax = new Vector2(0.9f, 0.6f);
        nameRect.sizeDelta = Vector2.zero;
        previewName = nameObj.AddComponent<TextMeshProUGUI>();
        previewName.fontSize = 24;
        previewName.alignment = TextAlignmentOptions.Center;
        previewName.fontStyle = FontStyles.Bold;

        // Preview Description
        GameObject descObj = new GameObject("PreviewDescription");
        descObj.transform.SetParent(previewPanel.transform);
        RectTransform descRect = descObj.AddComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0.1f, 0.4f);
        descRect.anchorMax = new Vector2(0.9f, 0.5f);
        descRect.sizeDelta = Vector2.zero;
        previewDescription = descObj.AddComponent<TextMeshProUGUI>();
        previewDescription.fontSize = 14;
        previewDescription.alignment = TextAlignmentOptions.Center;

        // Requirements Container
        GameObject reqScrollObj = new GameObject("RequirementsScroll");
        reqScrollObj.transform.SetParent(previewPanel.transform);
        RectTransform reqScrollRect = reqScrollObj.AddComponent<RectTransform>();
        reqScrollRect.anchorMin = new Vector2(0.1f, 0.1f);
        reqScrollRect.anchorMax = new Vector2(0.9f, 0.35f);
        reqScrollRect.sizeDelta = Vector2.zero;

        GameObject reqContent = new GameObject("RequirementsContent");
        reqContent.transform.SetParent(reqScrollObj.transform);
        VerticalLayoutGroup layout = reqContent.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 5;
        layout.childForceExpandHeight = false;
        requirementsContainer = reqContent.transform;
    }

    public void BindToBuildingSystem(BuildingSystem system, InventorySystem inventory)
    {
        buildingSystem = system;
        inventorySystem = inventory;
    }

    public void Open()
    {
        isOpen = true;
        buildingPanel.SetActive(true);
        
        OnCategorySelected("Foundation");
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Close()
    {
        isOpen = false;
        buildingPanel.SetActive(false);
        previewPanel.SetActive(false);
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public bool IsOpen() => isOpen;

    private void OnCategorySelected(string category)
    {
        currentCategory = category;
        
        // Update category button colors
        foreach (var kvp in categoryButtons)
        {
            ColorBlock colors = kvp.Value.colors;
            colors.normalColor = kvp.Key == category ? selectedCategoryColor : normalCategoryColor;
            kvp.Value.colors = colors;
        }
        
        RefreshItemList();
    }

    private void RefreshItemList()
    {
        // Clear existing items
        foreach (Transform child in itemContainer)
        {
            Destroy(child.gameObject);
        }

        currentItems.Clear();
        
        // Get items from database
        List<ItemData> allItems = ItemDatabase.Instance.GetAllItems();
        
        foreach (ItemData item in allItems)
        {
            if (item.isBuildingPiece && item.buildingPrefab != null)
            {
                // TODO: Filter by category based on item properties
                currentItems.Add(item);
                CreateBuildingItemButton(item);
            }
        }
    }

    private void CreateBuildingItemButton(ItemData item)
    {
        GameObject btnObj = new GameObject($"BuildingItem_{item.itemId}");
        btnObj.transform.SetParent(itemContainer);
        
        RectTransform rect = btnObj.AddComponent<RectTransform>();
        
        Image bg = btnObj.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        
        Button btn = btnObj.AddComponent<Button>();
        
        // Icon
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(btnObj.transform);
        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.1f, 0.3f);
        iconRect.anchorMax = new Vector2(0.9f, 0.9f);
        iconRect.sizeDelta = Vector2.zero;
        Image icon = iconObj.AddComponent<Image>();
        icon.sprite = item.icon;
        icon.preserveAspect = true;
        
        // Name
        GameObject nameObj = new GameObject("Name");
        nameObj.transform.SetParent(btnObj.transform);
        RectTransform nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0, 0);
        nameRect.anchorMax = new Vector2(1, 0.3f);
        nameRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = item.itemName;
        nameText.fontSize = 14;
        nameText.alignment = TextAlignmentOptions.Center;
        
        btn.onClick.AddListener(() => OnBuildingItemSelected(item));
    }

    private void OnBuildingItemSelected(ItemData item)
    {
        ShowPreview(item);
        
        if (buildingSystem != null && item.buildingPrefab != null)
        {
            buildingSystem.SetBuildingPrefab(item.buildingPrefab);
            buildingSystem.EnterBuildingMode(item.buildingPrefab);
            Close();
        }
    }

    private void ShowPreview(ItemData item)
    {
        previewPanel.SetActive(true);
        
        previewImage.sprite = item.icon;
        previewName.text = item.itemName;
        previewDescription.text = item.description;
        
        // Clear requirements
        foreach (Transform child in requirementsContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Show requirements
        if (item.recipe != null && item.recipe.ingredients != null)
        {
            foreach (var ingredient in item.recipe.ingredients)
            {
                CreateRequirementItem(ingredient);
            }
        }
    }

    private void CreateRequirementItem(CraftingIngredient ingredient)
    {
        ItemData ingredientItem = ItemDatabase.Instance.GetItem(ingredient.itemId);
        if (ingredientItem == null) return;
        
        GameObject reqObj = new GameObject($"Requirement_{ingredient.itemId}");
        reqObj.transform.SetParent(requirementsContainer);
        
        HorizontalLayoutGroup layout = reqObj.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10;
        layout.childForceExpandHeight = false;
        layout.childControlHeight = false;
        
        // Icon
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(reqObj.transform);
        Image icon = iconObj.AddComponent<Image>();
        icon.sprite = ingredientItem.icon;
        LayoutElement iconLayout = iconObj.AddComponent<LayoutElement>();
        iconLayout.preferredWidth = 30;
        iconLayout.preferredHeight = 30;
        
        // Name + Amount
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(reqObj.transform);
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.fontSize = 14;
        
        int hasAmount = inventorySystem != null ? inventorySystem.CountItem(ingredient.itemId) : 0;
        bool hasEnough = hasAmount >= ingredient.amount;
        
        text.text = $"{ingredientItem.itemName}: {hasAmount}/{ingredient.amount}";
        text.color = hasEnough ? canBuildColor : cannotBuildColor;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            if (isOpen)
                Close();
            else
                Open();
        }
        
        if (Input.GetKeyDown(KeyCode.Escape) && isOpen)
        {
            Close();
        }
    }
}