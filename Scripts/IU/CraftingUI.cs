using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// UI do sistema de crafting
/// Mostra receitas disponíveis, ingredientes necessários e fila de crafting
/// </summary>
public class CraftingUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject craftingPanel;
    [SerializeField] private Transform categoryContainer;
    [SerializeField] private Transform recipeContainer;
    [SerializeField] private GameObject categoryButtonPrefab;
    [SerializeField] private GameObject recipeItemPrefab;

    [Header("Recipe Details")]
    [SerializeField] private GameObject detailsPanel;
    [SerializeField] private Image detailsIcon;
    [SerializeField] private TextMeshProUGUI detailsName;
    [SerializeField] private TextMeshProUGUI detailsDescription;
    [SerializeField] private Transform ingredientsContainer;
    [SerializeField] private GameObject ingredientItemPrefab;
    [SerializeField] private Button craftButton;
    [SerializeField] private TextMeshProUGUI craftButtonText;

    [Header("Crafting Queue")]
    [SerializeField] private GameObject queuePanel;
    [SerializeField] private Transform queueContainer;
    [SerializeField] private GameObject queueItemPrefab;
    [SerializeField] private Image currentCraftProgressBar;
    [SerializeField] private TextMeshProUGUI currentCraftText;

    [Header("Colors")]
    [SerializeField] private Color selectedCategoryColor = new Color(0.8f, 0.6f, 0.2f);
    [SerializeField] private Color normalCategoryColor = new Color(0.3f, 0.3f, 0.3f);
    [SerializeField] private Color canCraftColor = Color.green;
    [SerializeField] private Color cannotCraftColor = Color.red;
    [SerializeField] private Color lockedColor = Color.gray;

    [Header("Settings")]
    [SerializeField] private int craftAmountIncrement = 1;

    private CraftingSystem craftingSystem;
    private InventorySystem inventorySystem;
    private bool isOpen = false;
    private ItemCategory currentCategory = ItemCategory.Resources;
    private ItemData selectedRecipe = null;
    private int craftAmount = 1;

    private Dictionary<ItemCategory, Button> categoryButtons = new Dictionary<ItemCategory, Button>();
    private List<RecipeUIItem> recipeItems = new List<RecipeUIItem>();
    private List<QueueUIItem> queueItems = new List<QueueUIItem>();

    private void Awake()
    {
        CreateUI();
        craftingPanel.SetActive(false);
        detailsPanel.SetActive(false);
        queuePanel.SetActive(false);
    }

    private void Update()
    {
        if (isOpen)
        {
            UpdateCraftingQueue();
            UpdateRecipeAvailability();
        }
    }

    #region UI CREATION

    private void CreateUI()
    {
        // Cria painel principal se não existir
        if (craftingPanel == null)
        {
            GameObject panel = new GameObject("CraftingPanel");
            panel.transform.SetParent(transform);
            
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(1, 1);
            rect.sizeDelta = Vector2.zero;
            
            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.95f);
            
            craftingPanel = panel;
        }

        CreateCategoryButtons();
        CreateRecipeGrid();
        CreateDetailsPanel();
        CreateQueuePanel();
    }

    private void CreateCategoryButtons()
    {
        if (categoryContainer == null)
        {
            GameObject container = new GameObject("CategoryContainer");
            container.transform.SetParent(craftingPanel.transform);
            
            RectTransform rect = container.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0.9f);
            rect.anchorMax = new Vector2(0.6f, 1);
            rect.sizeDelta = Vector2.zero;
            
            HorizontalLayoutGroup layout = container.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 5;
            layout.padding = new RectOffset(10, 10, 5, 5);
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            
            categoryContainer = container.transform;
        }

        // Cria botões de categoria
        ItemCategory[] categories = new ItemCategory[]
        {
            ItemCategory.Resources,
            ItemCategory.Tools,
            ItemCategory.Weapons,
            ItemCategory.Clothing,
            ItemCategory.Food,
            ItemCategory.Construction,
            ItemCategory.Components
        };

        foreach (ItemCategory category in categories)
        {
            GameObject btnObj = new GameObject($"Category_{category}");
            btnObj.transform.SetParent(categoryContainer);
            
            Button btn = btnObj.AddComponent<Button>();
            Image btnImage = btnObj.AddComponent<Image>();
            btnImage.color = normalCategoryColor;
            
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform);
            
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = category.ToString();
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 16;
            
            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            
            categoryButtons[category] = btn;
            
            ItemCategory cat = category;
            btn.onClick.AddListener(() => OnCategorySelected(cat));
        }
    }

    private void CreateRecipeGrid()
    {
        if (recipeContainer == null)
        {
            GameObject scrollObj = new GameObject("RecipeScrollView");
            scrollObj.transform.SetParent(craftingPanel.transform);
            
            RectTransform scrollRect = scrollObj.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0, 0);
            scrollRect.anchorMax = new Vector2(0.6f, 0.9f);
            scrollRect.sizeDelta = Vector2.zero;
            
            ScrollRect scroll = scrollObj.AddComponent<ScrollRect>();
            
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollObj.transform);
            RectTransform vpRect = viewport.AddComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.sizeDelta = Vector2.zero;
            viewport.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            
            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform);
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            
            VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 5;
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            
            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            scroll.content = contentRect;
            scroll.viewport = vpRect;
            scroll.vertical = true;
            scroll.horizontal = false;
            
            recipeContainer = content.transform;
        }
    }

    private void CreateDetailsPanel()
    {
        if (detailsPanel == null)
        {
            GameObject panel = new GameObject("DetailsPanel");
            panel.transform.SetParent(craftingPanel.transform);
            
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.6f, 0.3f);
            rect.anchorMax = new Vector2(1, 1);
            rect.sizeDelta = Vector2.zero;
            
            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);
            
            detailsPanel = panel;
        }

        // Icon
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(detailsPanel.transform);
        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.1f, 0.7f);
        iconRect.anchorMax = new Vector2(0.4f, 0.95f);
        iconRect.sizeDelta = Vector2.zero;
        detailsIcon = iconObj.AddComponent<Image>();
        detailsIcon.preserveAspect = true;

        // Name
        GameObject nameObj = new GameObject("Name");
        nameObj.transform.SetParent(detailsPanel.transform);
        RectTransform nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0.45f, 0.85f);
        nameRect.anchorMax = new Vector2(0.95f, 0.95f);
        nameRect.sizeDelta = Vector2.zero;
        detailsName = nameObj.AddComponent<TextMeshProUGUI>();
        detailsName.fontSize = 22;
        detailsName.fontStyle = FontStyles.Bold;
        detailsName.alignment = TextAlignmentOptions.Left;

        // Description
        GameObject descObj = new GameObject("Description");
        descObj.transform.SetParent(detailsPanel.transform);
        RectTransform descRect = descObj.AddComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0.1f, 0.6f);
        descRect.anchorMax = new Vector2(0.9f, 0.7f);
        descRect.sizeDelta = Vector2.zero;
        detailsDescription = descObj.AddComponent<TextMeshProUGUI>();
        detailsDescription.fontSize = 14;
        detailsDescription.alignment = TextAlignmentOptions.TopLeft;
        detailsDescription.overflowMode = TextOverflowModes.Truncate;

        // Ingredients Label
        GameObject ingredientsLabelObj = new GameObject("IngredientsLabel");
        ingredientsLabelObj.transform.SetParent(detailsPanel.transform);
        RectTransform ingredientsLabelRect = ingredientsLabelObj.AddComponent<RectTransform>();
        ingredientsLabelRect.anchorMin = new Vector2(0.1f, 0.55f);
        ingredientsLabelRect.anchorMax = new Vector2(0.9f, 0.6f);
        ingredientsLabelRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI ingredientsLabel = ingredientsLabelObj.AddComponent<TextMeshProUGUI>();
        ingredientsLabel.text = "REQUIRED INGREDIENTS:";
        ingredientsLabel.fontSize = 14;
        ingredientsLabel.fontStyle = FontStyles.Bold;

        // Ingredients Container
        GameObject ingredientsScrollObj = new GameObject("IngredientsScroll");
        ingredientsScrollObj.transform.SetParent(detailsPanel.transform);
        RectTransform ingredientsScrollRect = ingredientsScrollObj.AddComponent<RectTransform>();
        ingredientsScrollRect.anchorMin = new Vector2(0.1f, 0.25f);
        ingredientsScrollRect.anchorMax = new Vector2(0.9f, 0.55f);
        ingredientsScrollRect.sizeDelta = Vector2.zero;

        GameObject ingredientsContentObj = new GameObject("Content");
        ingredientsContentObj.transform.SetParent(ingredientsScrollObj.transform);
        VerticalLayoutGroup ingredientsLayout = ingredientsContentObj.AddComponent<VerticalLayoutGroup>();
        ingredientsLayout.spacing = 5;
        ingredientsLayout.childForceExpandHeight = false;
        ingredientsLayout.childForceExpandWidth = true;
        ingredientsContainer = ingredientsContentObj.transform;

        // Craft Button
        GameObject craftBtnObj = new GameObject("CraftButton");
        craftBtnObj.transform.SetParent(detailsPanel.transform);
        RectTransform craftBtnRect = craftBtnObj.AddComponent<RectTransform>();
        craftBtnRect.anchorMin = new Vector2(0.1f, 0.05f);
        craftBtnRect.anchorMax = new Vector2(0.9f, 0.15f);
        craftBtnRect.sizeDelta = Vector2.zero;
        
        craftButton = craftBtnObj.AddComponent<Button>();
        Image craftBtnImage = craftBtnObj.AddComponent<Image>();
        craftBtnImage.color = new Color(0.2f, 0.6f, 0.2f);

        GameObject craftBtnTextObj = new GameObject("Text");
        craftBtnTextObj.transform.SetParent(craftBtnObj.transform);
        RectTransform craftBtnTextRect = craftBtnTextObj.AddComponent<RectTransform>();
        craftBtnTextRect.anchorMin = Vector2.zero;
        craftBtnTextRect.anchorMax = Vector2.one;
        craftBtnTextRect.sizeDelta = Vector2.zero;
        craftButtonText = craftBtnTextObj.AddComponent<TextMeshProUGUI>();
        craftButtonText.text = "CRAFT";
        craftButtonText.fontSize = 20;
        craftButtonText.fontStyle = FontStyles.Bold;
        craftButtonText.alignment = TextAlignmentOptions.Center;

        craftButton.onClick.AddListener(OnCraftButtonClicked);
    }

    private void CreateQueuePanel()
    {
        if (queuePanel == null)
        {
            GameObject panel = new GameObject("QueuePanel");
            panel.transform.SetParent(craftingPanel.transform);
            
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.6f, 0);
            rect.anchorMax = new Vector2(1, 0.3f);
            rect.sizeDelta = Vector2.zero;
            
            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);
            
            queuePanel = panel;
        }

        // Title
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(queuePanel.transform);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.9f);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
        title.text = "CRAFTING QUEUE";
        title.fontSize = 18;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;

        // Current Craft Progress
        GameObject progressObj = new GameObject("CurrentProgress");
        progressObj.transform.SetParent(queuePanel.transform);
        RectTransform progressRect = progressObj.AddComponent<RectTransform>();
        progressRect.anchorMin = new Vector2(0.1f, 0.75f);
        progressRect.anchorMax = new Vector2(0.9f, 0.85f);
        progressRect.sizeDelta = Vector2.zero;

        GameObject progressBgObj = new GameObject("Background");
        progressBgObj.transform.SetParent(progressObj.transform);
        RectTransform progressBgRect = progressBgObj.AddComponent<RectTransform>();
        progressBgRect.anchorMin = Vector2.zero;
        progressBgRect.anchorMax = Vector2.one;
        progressBgRect.sizeDelta = Vector2.zero;
        Image progressBg = progressBgObj.AddComponent<Image>();
        progressBg.color = new Color(0.2f, 0.2f, 0.2f);

        GameObject progressFillObj = new GameObject("Fill");
        progressFillObj.transform.SetParent(progressObj.transform);
        RectTransform progressFillRect = progressFillObj.AddComponent<RectTransform>();
        progressFillRect.anchorMin = Vector2.zero;
        progressFillRect.anchorMax = Vector2.one;
        progressFillRect.sizeDelta = Vector2.zero;
        currentCraftProgressBar = progressFillObj.AddComponent<Image>();
        currentCraftProgressBar.color = new Color(0.2f, 0.8f, 0.2f);
        currentCraftProgressBar.type = Image.Type.Filled;
        currentCraftProgressBar.fillMethod = Image.FillMethod.Horizontal;

        GameObject progressTextObj = new GameObject("Text");
        progressTextObj.transform.SetParent(progressObj.transform);
        RectTransform progressTextRect = progressTextObj.AddComponent<RectTransform>();
        progressTextRect.anchorMin = Vector2.zero;
        progressTextRect.anchorMax = Vector2.one;
        progressTextRect.sizeDelta = Vector2.zero;
        currentCraftText = progressTextObj.AddComponent<TextMeshProUGUI>();
        currentCraftText.fontSize = 14;
        currentCraftText.alignment = TextAlignmentOptions.Center;
        currentCraftText.text = "Not crafting";

        // Queue Container
        GameObject queueScrollObj = new GameObject("QueueScroll");
        queueScrollObj.transform.SetParent(queuePanel.transform);
        RectTransform queueScrollRect = queueScrollObj.AddComponent<RectTransform>();
        queueScrollRect.anchorMin = new Vector2(0.1f, 0.1f);
        queueScrollRect.anchorMax = new Vector2(0.9f, 0.7f);
        queueScrollRect.sizeDelta = Vector2.zero;

        GameObject queueContentObj = new GameObject("Content");
        queueContentObj.transform.SetParent(queueScrollObj.transform);
        VerticalLayoutGroup queueLayout = queueContentObj.AddComponent<VerticalLayoutGroup>();
        queueLayout.spacing = 3;
        queueLayout.childForceExpandHeight = false;
        queueLayout.childForceExpandWidth = true;
        queueContainer = queueContentObj.transform;
    }

    #endregion

    #region BINDING

    public void BindToCraftingSystem(CraftingSystem system)
    {
        craftingSystem = system;
        inventorySystem = system.GetComponent<InventorySystem>();

        if (craftingSystem != null)
        {
            craftingSystem.OnCraftingStarted += OnCraftingStarted;
            craftingSystem.OnCraftingCompleted += OnCraftingCompleted;
            craftingSystem.OnQueueChanged += OnQueueChanged;
        }
    }

    #endregion

    #region OPEN/CLOSE

    public void Open()
    {
        isOpen = true;
        craftingPanel.SetActive(true);
        queuePanel.SetActive(true);
        
        OnCategorySelected(ItemCategory.Resources);
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Close()
    {
        isOpen = false;
        craftingPanel.SetActive(false);
        detailsPanel.SetActive(false);
        queuePanel.SetActive(false);
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public bool IsOpen() => isOpen;

    #endregion

    #region CATEGORY

    private void OnCategorySelected(ItemCategory category)
    {
        currentCategory = category;
        
        // Update button colors
        foreach (var kvp in categoryButtons)
        {
            ColorBlock colors = kvp.Value.colors;
            colors.normalColor = kvp.Key == category ? selectedCategoryColor : normalCategoryColor;
            kvp.Value.colors = colors;
        }
        
        RefreshRecipeList();
    }

    #endregion

    #region RECIPE LIST

    private void RefreshRecipeList()
    {
        // Clear existing
        foreach (Transform child in recipeContainer)
        {
            Destroy(child.gameObject);
        }
        recipeItems.Clear();

        // Get recipes for category
        List<ItemData> craftableItems = ItemDatabase.Instance.GetCraftableItemsAtStation(
            craftingSystem != null ? craftingSystem.GetCurrentStation() : CraftingStation.None
        );

        foreach (ItemData item in craftableItems)
        {
            if (item.category == currentCategory)
            {
                CreateRecipeItem(item);
            }
        }
    }

    private void CreateRecipeItem(ItemData item)
    {
        GameObject itemObj = new GameObject($"Recipe_{item.itemId}");
        itemObj.transform.SetParent(recipeContainer);
        
        RectTransform rect = itemObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 60);
        
        Button btn = itemObj.AddComponent<Button>();
        Image bg = itemObj.AddComponent<Image>();
        bg.color = new Color(0.25f, 0.25f, 0.25f);

        // Icon
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(itemObj.transform);
        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0, 0);
        iconRect.anchorMax = new Vector2(0, 1);
        iconRect.pivot = new Vector2(0, 0.5f);
        iconRect.anchoredPosition = new Vector2(5, 0);
        iconRect.sizeDelta = new Vector2(50, 50);
        Image icon = iconObj.AddComponent<Image>();
        icon.sprite = item.icon;
        icon.preserveAspect = true;

        // Name
        GameObject nameObj = new GameObject("Name");
        nameObj.transform.SetParent(itemObj.transform);
        RectTransform nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0.15f, 0.5f);
        nameRect.anchorMax = new Vector2(0.8f, 1);
        nameRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = item.itemName;
        nameText.fontSize = 16;
        nameText.fontStyle = FontStyles.Bold;

        // Craft Time
        GameObject timeObj = new GameObject("CraftTime");
        timeObj.transform.SetParent(itemObj.transform);
        RectTransform timeRect = timeObj.AddComponent<RectTransform>();
        timeRect.anchorMin = new Vector2(0.15f, 0);
        timeRect.anchorMax = new Vector2(0.8f, 0.5f);
        timeRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI timeText = timeObj.AddComponent<TextMeshProUGUI>();
        timeText.text = $"Craft Time: {item.craftTime:F1}s";
        timeText.fontSize = 12;
        timeText.color = Color.gray;

        // Status Icon
        GameObject statusObj = new GameObject("Status");
        statusObj.transform.SetParent(itemObj.transform);
        RectTransform statusRect = statusObj.AddComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0.85f, 0.25f);
        statusRect.anchorMax = new Vector2(0.95f, 0.75f);
        statusRect.sizeDelta = Vector2.zero;
        Image statusIcon = statusObj.AddComponent<Image>();

        RecipeUIItem recipeItem = new RecipeUIItem
        {
            item = item,
            obj = itemObj,
            button = btn,
            statusIcon = statusIcon
        };
        recipeItems.Add(recipeItem);

        btn.onClick.AddListener(() => OnRecipeSelected(item));
    }

    private void UpdateRecipeAvailability()
    {
        if (craftingSystem == null || inventorySystem == null) return;

        foreach (var recipeItem in recipeItems)
        {
            bool canCraft = craftingSystem.CanCraft(recipeItem.item.itemId);
            bool hasBlueprint = craftingSystem.HasBlueprint(recipeItem.item.itemId);

            if (!hasBlueprint)
            {
                recipeItem.statusIcon.color = lockedColor;
            }
            else if (canCraft)
            {
                recipeItem.statusIcon.color = canCraftColor;
            }
            else
            {
                recipeItem.statusIcon.color = cannotCraftColor;
            }
        }
    }

    #endregion

    #region RECIPE DETAILS

    private void OnRecipeSelected(ItemData item)
    {
        selectedRecipe = item;
        craftAmount = 1;
        ShowRecipeDetails(item);
    }

    private void ShowRecipeDetails(ItemData item)
    {
        detailsPanel.SetActive(true);

        detailsIcon.sprite = item.icon;
        detailsName.text = item.itemName;
        detailsDescription.text = item.description;

        // Clear ingredients
        foreach (Transform child in ingredientsContainer)
        {
            Destroy(child.gameObject);
        }

        // Show ingredients
        if (item.recipe != null && item.recipe.ingredients != null)
        {
            foreach (var ingredient in item.recipe.ingredients)
            {
                CreateIngredientItem(ingredient);
            }
        }

        // Update craft button
        UpdateCraftButton();
    }

    private void CreateIngredientItem(CraftingIngredient ingredient)
    {
        ItemData ingredientItem = ItemDatabase.Instance.GetItem(ingredient.itemId);
        if (ingredientItem == null) return;

        GameObject itemObj = new GameObject($"Ingredient_{ingredient.itemId}");
        itemObj.transform.SetParent(ingredientsContainer);
        
        RectTransform rect = itemObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 35);

        HorizontalLayoutGroup layout = itemObj.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10;
        layout.childForceExpandHeight = false;
        layout.childControlHeight = false;
        layout.padding = new RectOffset(5, 5, 5, 5);

        // Icon
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(itemObj.transform);
        Image icon = iconObj.AddComponent<Image>();
        icon.sprite = ingredientItem.icon;
        LayoutElement iconLayout = iconObj.AddComponent<LayoutElement>();
        iconLayout.preferredWidth = 30;
        iconLayout.preferredHeight = 30;

        // Name + Amount
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(itemObj.transform);
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.fontSize = 14;

        int hasAmount = inventorySystem != null ? inventorySystem.CountItem(ingredient.itemId) : 0;
        bool hasEnough = hasAmount >= ingredient.amount;

        text.text = $"{ingredientItem.itemName}: {hasAmount}/{ingredient.amount}";
        text.color = hasEnough ? canCraftColor : cannotCraftColor;
    }

    private void UpdateCraftButton()
    {
        if (selectedRecipe == null || craftingSystem == null)
        {
            craftButton.interactable = false;
            craftButtonText.text = "SELECT RECIPE";
            return;
        }

        bool canCraft = craftingSystem.CanCraft(selectedRecipe.itemId);
        bool hasBlueprint = craftingSystem.HasBlueprint(selectedRecipe.itemId);

        if (!hasBlueprint)
        {
            craftButton.interactable = false;
            craftButtonText.text = "BLUEPRINT REQUIRED";
        }
        else if (canCraft)
        {
            craftButton.interactable = true;
            craftButtonText.text = $"CRAFT x{craftAmount}";
        }
        else
        {
            craftButton.interactable = false;
            craftButtonText.text = "INSUFFICIENT RESOURCES";
        }
    }

    #endregion

    #region CRAFTING

    private void OnCraftButtonClicked()
    {
        if (selectedRecipe == null || craftingSystem == null) return;

        for (int i = 0; i < craftAmount; i++)
        {
            if (!craftingSystem.QueueCraft(selectedRecipe.itemId, 1))
                break;
        }

        UpdateCraftButton();
        RefreshRecipeList();
    }

    #endregion

    #region CRAFTING QUEUE

    private void UpdateCraftingQueue()
    {
        if (craftingSystem == null) return;

        // Update current craft progress
        CraftingJob currentJob = craftingSystem.GetCurrentJob();
        if (currentJob != null)
        {
            float progress = craftingSystem.GetCraftingProgress();
            currentCraftProgressBar.fillAmount = progress;
            currentCraftText.text = $"Crafting: {currentJob.itemData.itemName} ({progress * 100f:F0}%)";
        }
        else
        {
            currentCraftProgressBar.fillAmount = 0;
            currentCraftText.text = "Not crafting";
        }

        // Update queue display
        RefreshQueueDisplay();
    }

    private void RefreshQueueDisplay()
    {
        // Clear existing
        foreach (Transform child in queueContainer)
        {
            Destroy(child.gameObject);
        }
        queueItems.Clear();

        List<CraftingJob> queue = craftingSystem.GetQueue();
        foreach (var job in queue)
        {
            CreateQueueItem(job);
        }
    }

    private void CreateQueueItem(CraftingJob job)
    {
        GameObject itemObj = new GameObject($"QueueItem_{job.itemData.itemId}");
        itemObj.transform.SetParent(queueContainer);
        
        RectTransform rect = itemObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 40);

        Image bg = itemObj.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.2f);

        // Icon
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(itemObj.transform);
        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0, 0);
        iconRect.anchorMax = new Vector2(0, 1);
        iconRect.pivot = new Vector2(0, 0.5f);
        iconRect.anchoredPosition = new Vector2(5, 0);
        iconRect.sizeDelta = new Vector2(30, 30);
        Image icon = iconObj.AddComponent<Image>();
        icon.sprite = job.itemData.icon;

        // Name
        GameObject nameObj = new GameObject("Name");
        nameObj.transform.SetParent(itemObj.transform);
        RectTransform nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0.15f, 0);
        nameRect.anchorMax = new Vector2(0.8f, 1);
        nameRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = job.itemData.itemName;
        nameText.fontSize = 14;
        nameText.alignment = TextAlignmentOptions.Left;

        QueueUIItem queueItem = new QueueUIItem { job = job, obj = itemObj };
        queueItems.Add(queueItem);
    }

    #endregion

    #region CALLBACKS

    private void OnCraftingStarted(CraftingJob job)
    {
        // Refresh UI
        RefreshQueueDisplay();
    }

    private void OnCraftingCompleted(CraftingJob job)
    {
        // Refresh UI
        RefreshQueueDisplay();
        UpdateCraftButton();
    }

    private void OnQueueChanged()
    {
        // Refresh UI
        RefreshQueueDisplay();
    }

    #endregion

    private class RecipeUIItem
    {
        public ItemData item;
        public GameObject obj;
        public Button button;
        public Image statusIcon;
    }

    private class QueueUIItem
    {
        public CraftingJob job;
        public GameObject obj;
    }
}