using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gerenciador principal do HUD
/// Coordena todas as UIs do jogo (health bars, crosshair, hotbar, etc)
/// </summary>
public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Canvas mainCanvas;
    [SerializeField] private CanvasGroup hudGroup;

    [Header("Status Bars")]
    [SerializeField] private StatusBarsUI statusBars;
    [SerializeField] private CrosshairUI crosshair;
    [SerializeField] private HotbarUI hotbar;

    [Header("Menus")]
    [SerializeField] private InventoryUI inventoryUI;
    [SerializeField] private CraftingUI craftingUI;
    [SerializeField] private BuildingUI buildingUI;

    [Header("Notifications")]
    [SerializeField] private NotificationUI notificationUI;
    [SerializeField] private DamageIndicatorUI damageIndicator;
    [SerializeField] private HitMarkerUI hitMarker;

    [Header("Chat")]
    [SerializeField] private ChatUI chatUI;

    [Header("Menus de Pausa/Morte")]
    [SerializeField] private PauseMenuUI pauseMenu;
    [SerializeField] private DeathScreenUI deathScreen;

    [Header("Loading")]
    [SerializeField] private LoadingScreenUI loadingScreen;

    [Header("Settings")]
    [SerializeField] private bool hideHUDOnInventory = true;
    [SerializeField] private float hudFadeSpeed = 5f;

    // Estado
    private bool isHUDVisible = true;
    private PlayerStats localPlayerStats;
    private InventorySystem localInventory;
    private CraftingSystem localCrafting;

    private void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Cria canvas se não existir
        if (mainCanvas == null)
        {
            CreateMainCanvas();
        }

        // Inicializa componentes
        InitializeComponents();
    }

    private void Start()
    {
        // Mostra loading inicial
        if (loadingScreen != null)
        {
            loadingScreen.Show("Connecting to server...");
        }
    }

    private void Update()
    {
        // Input para abrir menus
        HandleMenuInputs();

        // Atualiza HUD se jogador está ativo
        if (localPlayerStats != null)
        {
            UpdateHUD();
        }
    }

    #region INITIALIZATION

    /// <summary>
    /// Cria canvas principal
    /// </summary>
    private void CreateMainCanvas()
    {
        GameObject canvasObj = new GameObject("MainCanvas");
        canvasObj.transform.SetParent(transform);

        mainCanvas = canvasObj.AddComponent<Canvas>();
        mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        mainCanvas.sortingOrder = 100;

        canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObj.AddComponent<GraphicRaycaster>();

        hudGroup = canvasObj.AddComponent<CanvasGroup>();
    }

    /// <summary>
    /// Inicializa todos os componentes de UI
    /// </summary>
    private void InitializeComponents()
    {
        Debug.Log("[HUDManager] Initializing UI components...");

        // Status bars
        if (statusBars == null)
        {
            GameObject statusObj = new GameObject("StatusBars");
            statusObj.transform.SetParent(mainCanvas.transform);
            statusBars = statusObj.AddComponent<StatusBarsUI>();
        }

        // Crosshair
        if (crosshair == null)
        {
            GameObject crosshairObj = new GameObject("Crosshair");
            crosshairObj.transform.SetParent(mainCanvas.transform);
            crosshair = crosshairObj.AddComponent<CrosshairUI>();
        }

        // Hotbar
        if (hotbar == null)
        {
            GameObject hotbarObj = new GameObject("Hotbar");
            hotbarObj.transform.SetParent(mainCanvas.transform);
            hotbar = hotbarObj.AddComponent<HotbarUI>();
        }

        // Inventory
        if (inventoryUI == null)
        {
            GameObject invObj = new GameObject("InventoryUI");
            invObj.transform.SetParent(mainCanvas.transform);
            inventoryUI = invObj.AddComponent<InventoryUI>();
            inventoryUI.gameObject.SetActive(false);
        }

        // Crafting
        if (craftingUI == null)
        {
            GameObject craftObj = new GameObject("CraftingUI");
            craftObj.transform.SetParent(mainCanvas.transform);
            craftingUI = craftObj.AddComponent<CraftingUI>();
            craftingUI.gameObject.SetActive(false);
        }

        // Building
        if (buildingUI == null)
        {
            GameObject buildObj = new GameObject("BuildingUI");
            buildObj.transform.SetParent(mainCanvas.transform);
            buildingUI = buildObj.AddComponent<BuildingUI>();
            buildingUI.gameObject.SetActive(false);
        }

        // Notifications
        if (notificationUI == null)
        {
            GameObject notifObj = new GameObject("Notifications");
            notifObj.transform.SetParent(mainCanvas.transform);
            notificationUI = notifObj.AddComponent<NotificationUI>();
        }

        // Damage Indicator
        if (damageIndicator == null)
        {
            GameObject damageObj = new GameObject("DamageIndicator");
            damageObj.transform.SetParent(mainCanvas.transform);
            damageIndicator = damageObj.AddComponent<DamageIndicatorUI>();
        }

        // Hit Marker
        if (hitMarker == null)
        {
            GameObject hitObj = new GameObject("HitMarker");
            hitObj.transform.SetParent(mainCanvas.transform);
            hitMarker = hitObj.AddComponent<HitMarkerUI>();
        }

        // Chat
        if (chatUI == null)
        {
            GameObject chatObj = new GameObject("Chat");
            chatObj.transform.SetParent(mainCanvas.transform);
            chatUI = chatObj.AddComponent<ChatUI>();
        }

        // Pause Menu
        if (pauseMenu == null)
        {
            GameObject pauseObj = new GameObject("PauseMenu");
            pauseObj.transform.SetParent(mainCanvas.transform);
            pauseMenu = pauseObj.AddComponent<PauseMenuUI>();
            pauseMenu.gameObject.SetActive(false);
        }

        // Death Screen
        if (deathScreen == null)
        {
            GameObject deathObj = new GameObject("DeathScreen");
            deathObj.transform.SetParent(mainCanvas.transform);
            deathScreen = deathObj.AddComponent<DeathScreenUI>();
            deathScreen.gameObject.SetActive(false);
        }

        // Loading Screen
        if (loadingScreen == null)
        {
            GameObject loadObj = new GameObject("LoadingScreen");
            loadObj.transform.SetParent(mainCanvas.transform);
            loadingScreen = loadObj.AddComponent<LoadingScreenUI>();
        }

        Debug.Log("[HUDManager] All UI components initialized");
    }

    #endregion

    #region PLAYER BINDING

    /// <summary>
    /// Vincula HUD ao jogador local
    /// </summary>
    public void BindToLocalPlayer(GameObject player)
    {
        if (player == null)
        {
            Debug.LogError("[HUDManager] Cannot bind to null player!");
            return;
        }

        localPlayerStats = player.GetComponent<PlayerStats>();
        localInventory = player.GetComponent<InventorySystem>();
        localCrafting = player.GetComponent<CraftingSystem>();

        // Vincula componentes individuais
        if (statusBars != null)
            statusBars.BindToPlayer(localPlayerStats);

        if (hotbar != null && localInventory != null)
            hotbar.BindToInventory(localInventory);

        if (inventoryUI != null && localInventory != null)
            inventoryUI.BindToInventory(localInventory);

        if (craftingUI != null && localCrafting != null)
            craftingUI.BindToCraftingSystem(localCrafting);

        // Hide loading
        if (loadingScreen != null)
        {
            loadingScreen.Hide();
        }

        Debug.Log("[HUDManager] HUD bound to local player");
    }

    #endregion

    #region UPDATE

    /// <summary>
    /// Atualiza HUD
    /// </summary>
    private void UpdateHUD()
    {
        // Atualiza visibilidade do HUD
        UpdateHUDVisibility();
    }

    /// <summary>
    /// Atualiza visibilidade do HUD
    /// </summary>
    private void UpdateHUDVisibility()
    {
        float targetAlpha = isHUDVisible ? 1f : 0f;

        if (hudGroup != null)
        {
            hudGroup.alpha = Mathf.Lerp(hudGroup.alpha, targetAlpha, hudFadeSpeed * Time.deltaTime);
        }
    }

    #endregion

    #region MENU INPUTS

    /// <summary>
    /// Processa inputs de menus
    /// </summary>
    private void HandleMenuInputs()
    {
        // ESC - Pause Menu
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (inventoryUI != null && inventoryUI.IsOpen())
            {
                CloseInventory();
            }
            else if (pauseMenu != null)
            {
                if (pauseMenu.IsOpen())
                    pauseMenu.Close();
                else
                    pauseMenu.Open();
            }
        }

        // TAB - Inventory
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleInventory();
        }

        // C - Crafting
        if (Input.GetKeyDown(KeyCode.C))
        {
            ToggleCrafting();
        }

        // Enter - Chat
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (chatUI != null)
            {
                chatUI.FocusInput();
            }
        }
    }

    #endregion

    #region MENU CONTROLS

    /// <summary>
    /// Abre/fecha inventário
    /// </summary>
    public void ToggleInventory()
    {
        if (inventoryUI == null) return;

        if (inventoryUI.IsOpen())
        {
            CloseInventory();
        }
        else
        {
            OpenInventory();
        }
    }

    /// <summary>
    /// Abre inventário
    /// </summary>
    public void OpenInventory()
    {
        if (inventoryUI == null) return;

        inventoryUI.Open();

        // Destrava cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Esconde HUD se configurado
        if (hideHUDOnInventory)
        {
            SetHUDVisible(false);
        }
    }

    /// <summary>
    /// Fecha inventário
    /// </summary>
    public void CloseInventory()
    {
        if (inventoryUI == null) return;

        inventoryUI.Close();

        // Trava cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Mostra HUD
        SetHUDVisible(true);
    }

    /// <summary>
    /// Abre/fecha crafting
    /// </summary>
    public void ToggleCrafting()
    {
        if (craftingUI == null) return;

        if (craftingUI.IsOpen())
        {
            craftingUI.Close();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            craftingUI.Open();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    #endregion

    #region PUBLIC METHODS

    /// <summary>
    /// Mostra/esconde HUD
    /// </summary>
    public void SetHUDVisible(bool visible)
    {
        isHUDVisible = visible;
    }

    /// <summary>
    /// Mostra notificação
    /// </summary>
    public void ShowNotification(string message, NotificationType type = NotificationType.Info)
    {
        if (notificationUI != null)
        {
            notificationUI.ShowNotification(message, type);
        }
    }

    /// <summary>
    /// Mostra indicador de dano
    /// </summary>
    public void ShowDamageIndicator(Vector3 direction)
    {
        if (damageIndicator != null)
        {
            damageIndicator.ShowDamage(direction);
        }
    }

    /// <summary>
    /// Mostra hit marker
    /// </summary>
    public void ShowHitMarker(bool isHeadshot = false)
    {
        if (hitMarker != null)
        {
            hitMarker.ShowHit(isHeadshot);
        }
    }

    /// <summary>
    /// Mostra tela de morte
    /// </summary>
    public void ShowDeathScreen(string killerName, float respawnTime)
    {
        if (deathScreen != null)
        {
            deathScreen.Show(killerName, respawnTime);
        }

        // Esconde HUD
        SetHUDVisible(false);
    }

    /// <summary>
    /// Esconde tela de morte
    /// </summary>
    public void HideDeathScreen()
    {
        if (deathScreen != null)
        {
            deathScreen.Hide();
        }

        // Mostra HUD
        SetHUDVisible(true);
    }

    /// <summary>
    /// Adiciona mensagem ao chat
    /// </summary>
    public void AddChatMessage(string message, int senderId = -1)
    {
        if (chatUI != null)
        {
            chatUI.AddMessage(message, senderId);
        }
    }

    /// <summary>
    /// Mostra loading screen
    /// </summary>
    public void ShowLoading(string message)
    {
        if (loadingScreen != null)
        {
            loadingScreen.Show(message);
        }
    }

    /// <summary>
    /// Esconde loading screen
    /// </summary>
    public void HideLoading()
    {
        if (loadingScreen != null)
        {
            loadingScreen.Hide();
        }
    }

    #endregion

    #region GETTERS

    public StatusBarsUI GetStatusBars() => statusBars;
    public CrosshairUI GetCrosshair() => crosshair;
    public HotbarUI GetHotbar() => hotbar;
    public InventoryUI GetInventoryUI() => inventoryUI;
    public CraftingUI GetCraftingUI() => craftingUI;
    public ChatUI GetChatUI() => chatUI;

    #endregion
}