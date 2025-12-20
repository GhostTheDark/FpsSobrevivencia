using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Menu de pausa com opções do jogo
/// </summary>
public class PauseMenuUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject settingsMenu;
    [SerializeField] private GameObject controlsMenu;

    [Header("Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button controlsButton;
    [SerializeField] private Button disconnectButton;
    [SerializeField] private Button quitButton;

    [Header("Settings")]
    [SerializeField] private Slider mouseSensitivitySlider;
    [SerializeField] private TextMeshProUGUI mouseSensitivityText;
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private TextMeshProUGUI masterVolumeText;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private TMP_Dropdown qualityDropdown;
    [SerializeField] private TMP_Dropdown resolutionDropdown;

    private bool isOpen = false;
    private Resolution[] resolutions;

    private void Awake()
    {
        CreatePauseMenu();
        pausePanel.SetActive(false);
    }

    private void Update()
    {
        // ESC para abrir/fechar
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isOpen)
                Close();
            else
                Open();
        }
    }

    #region UI CREATION

    private void CreatePauseMenu()
    {
        if (pausePanel == null)
        {
            GameObject panel = new GameObject("PausePanel");
            panel.transform.SetParent(transform);
            
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            
            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.9f);
            
            pausePanel = panel;
        }

        CreateMainMenu();
        CreateSettingsMenu();
        CreateControlsMenu();

        // Começa no menu principal
        ShowMainMenu();
    }

    private void CreateMainMenu()
    {
        if (mainMenu == null)
        {
            mainMenu = new GameObject("MainMenu");
            mainMenu.transform.SetParent(pausePanel.transform);
            
            RectTransform rect = mainMenu.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(400, 500);
        }

        // Title
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(mainMenu.transform);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.85f);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
        title.text = "PAUSED";
        title.fontSize = 48;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;

        // Buttons Container
        GameObject buttonsObj = new GameObject("Buttons");
        buttonsObj.transform.SetParent(mainMenu.transform);
        RectTransform buttonsRect = buttonsObj.AddComponent<RectTransform>();
        buttonsRect.anchorMin = new Vector2(0.1f, 0.2f);
        buttonsRect.anchorMax = new Vector2(0.9f, 0.8f);
        buttonsRect.sizeDelta = Vector2.zero;
        
        VerticalLayoutGroup layout = buttonsObj.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 15;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.childControlHeight = true;

        // Resume
        resumeButton = CreateMenuButton("Resume", buttonsObj.transform);
        resumeButton.onClick.AddListener(OnResumeClicked);

        // Settings
        settingsButton = CreateMenuButton("Settings", buttonsObj.transform);
        settingsButton.onClick.AddListener(OnSettingsClicked);

        // Controls
        controlsButton = CreateMenuButton("Controls", buttonsObj.transform);
        controlsButton.onClick.AddListener(OnControlsClicked);

        // Disconnect
        disconnectButton = CreateMenuButton("Disconnect", buttonsObj.transform);
        disconnectButton.onClick.AddListener(OnDisconnectClicked);
        
        // Quit
        quitButton = CreateMenuButton("Quit to Desktop", buttonsObj.transform);
        quitButton.onClick.AddListener(OnQuitClicked);
    }

    private Button CreateMenuButton(string text, Transform parent)
    {
        GameObject btnObj = new GameObject($"Button_{text}");
        btnObj.transform.SetParent(parent);
        
        LayoutElement layoutElement = btnObj.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 50;
        
        Button btn = btnObj.AddComponent<Button>();
        Image btnImage = btnObj.AddComponent<Image>();
        btnImage.color = new Color(0.2f, 0.2f, 0.2f);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        
        TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = text;
        buttonText.fontSize = 20;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.fontStyle = FontStyles.Bold;

        return btn;
    }

    private void CreateSettingsMenu()
    {
        if (settingsMenu == null)
        {
            settingsMenu = new GameObject("SettingsMenu");
            settingsMenu.transform.SetParent(pausePanel.transform);
            
            RectTransform rect = settingsMenu.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(600, 600);
            
            Image bg = settingsMenu.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        }

        // Title
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(settingsMenu.transform);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.9f);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
        title.text = "SETTINGS";
        title.fontSize = 36;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;

        // Settings Container
        GameObject settingsContainer = new GameObject("SettingsContainer");
        settingsContainer.transform.SetParent(settingsMenu.transform);
        RectTransform containerRect = settingsContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.1f, 0.15f);
        containerRect.anchorMax = new Vector2(0.9f, 0.85f);
        containerRect.sizeDelta = Vector2.zero;
        
        VerticalLayoutGroup containerLayout = settingsContainer.AddComponent<VerticalLayoutGroup>();
        containerLayout.spacing = 20;
        containerLayout.childForceExpandHeight = false;
        containerLayout.childForceExpandWidth = true;

        // Mouse Sensitivity
        CreateSliderSetting("Mouse Sensitivity", 0.1f, 10f, 2f, settingsContainer.transform, 
            out mouseSensitivitySlider, out mouseSensitivityText, OnMouseSensitivityChanged);

        // Master Volume
        CreateSliderSetting("Master Volume", 0f, 1f, 0.8f, settingsContainer.transform,
            out masterVolumeSlider, out masterVolumeText, OnMasterVolumeChanged);

        // Fullscreen Toggle
        CreateToggleSetting("Fullscreen", Screen.fullScreen, settingsContainer.transform,
            out fullscreenToggle, OnFullscreenChanged);

        // Quality
        CreateDropdownSetting("Graphics Quality", new string[] { "Low", "Medium", "High", "Ultra" },
            QualitySettings.GetQualityLevel(), settingsContainer.transform, out qualityDropdown, OnQualityChanged);

        // Resolution
        resolutions = Screen.resolutions;
        string[] resolutionStrings = new string[resolutions.Length];
        int currentResolutionIndex = 0;
        for (int i = 0; i < resolutions.Length; i++)
        {
            resolutionStrings[i] = $"{resolutions[i].width} x {resolutions[i].height} @ {resolutions[i].refreshRate}Hz";
            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }
        CreateDropdownSetting("Resolution", resolutionStrings, currentResolutionIndex,
            settingsContainer.transform, out resolutionDropdown, OnResolutionChanged);

        // Back Button
        Button backButton = CreateMenuButton("Back", settingsMenu.transform);
        RectTransform backRect = backButton.GetComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0.3f, 0.05f);
        backRect.anchorMax = new Vector2(0.7f, 0.12f);
        backRect.sizeDelta = Vector2.zero;
        backButton.onClick.AddListener(ShowMainMenu);

        settingsMenu.SetActive(false);
    }

    private void CreateSliderSetting(string label, float minValue, float maxValue, float defaultValue,
        Transform parent, out Slider slider, out TextMeshProUGUI valueText, UnityEngine.Events.UnityAction<float> onValueChanged)
    {
        GameObject settingObj = new GameObject($"Setting_{label}");
        settingObj.transform.SetParent(parent);
        
        LayoutElement layoutElement = settingObj.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 50;

        // Label
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(settingObj.transform);
        RectTransform labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0, 0.5f);
        labelRect.anchorMax = new Vector2(0.4f, 1);
        labelRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.fontSize = 18;
        labelText.alignment = TextAlignmentOptions.Left;

        // Slider
        GameObject sliderObj = new GameObject("Slider");
        sliderObj.transform.SetParent(settingObj.transform);
        RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.4f, 0.2f);
        sliderRect.anchorMax = new Vector2(0.8f, 0.8f);
        sliderRect.sizeDelta = Vector2.zero;
        slider = sliderObj.AddComponent<Slider>();
        slider.minValue = minValue;
        slider.maxValue = maxValue;
        slider.value = defaultValue;
        
        // Slider BG
        GameObject sliderBg = new GameObject("Background");
        sliderBg.transform.SetParent(sliderObj.transform);
        RectTransform sliderBgRect = sliderBg.AddComponent<RectTransform>();
        sliderBgRect.anchorMin = Vector2.zero;
        sliderBgRect.anchorMax = Vector2.one;
        sliderBgRect.sizeDelta = Vector2.zero;
        Image sliderBgImage = sliderBg.AddComponent<Image>();
        sliderBgImage.color = new Color(0.2f, 0.2f, 0.2f);

        // Slider Fill
        GameObject sliderFill = new GameObject("Fill Area");
        sliderFill.transform.SetParent(sliderObj.transform);
        GameObject sliderFillChild = new GameObject("Fill");
        sliderFillChild.transform.SetParent(sliderFill.transform);
        RectTransform sliderFillRect = sliderFillChild.AddComponent<RectTransform>();
        sliderFillRect.sizeDelta = Vector2.zero;
        Image sliderFillImage = sliderFillChild.AddComponent<Image>();
        sliderFillImage.color = new Color(0.8f, 0.6f, 0.2f);
        slider.fillRect = sliderFillRect;

        // Slider Handle
        GameObject sliderHandle = new GameObject("Handle Slide Area");
        sliderHandle.transform.SetParent(sliderObj.transform);
        GameObject sliderHandleChild = new GameObject("Handle");
        sliderHandleChild.transform.SetParent(sliderHandle.transform);
        RectTransform sliderHandleRect = sliderHandleChild.AddComponent<RectTransform>();
        sliderHandleRect.sizeDelta = new Vector2(20, 20);
        Image sliderHandleImage = sliderHandleChild.AddComponent<Image>();
        sliderHandleImage.color = Color.white;
        slider.handleRect = sliderHandleRect;

        // Value Text
        GameObject valueObj = new GameObject("Value");
        valueObj.transform.SetParent(settingObj.transform);
        RectTransform valueRect = valueObj.AddComponent<RectTransform>();
        valueRect.anchorMin = new Vector2(0.8f, 0.5f);
        valueRect.anchorMax = new Vector2(1, 1);
        valueRect.sizeDelta = Vector2.zero;
        valueText = valueObj.AddComponent<TextMeshProUGUI>();
        valueText.text = defaultValue.ToString("F1");
        valueText.fontSize = 18;
        valueText.alignment = TextAlignmentOptions.Right;

        slider.onValueChanged.AddListener(onValueChanged);
    }

    private void CreateToggleSetting(string label, bool defaultValue, Transform parent,
        out Toggle toggle, UnityEngine.Events.UnityAction<bool> onValueChanged)
    {
        GameObject settingObj = new GameObject($"Setting_{label}");
        settingObj.transform.SetParent(parent);
        
        LayoutElement layoutElement = settingObj.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 40;

        // Label
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(settingObj.transform);
        RectTransform labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0, 0);
        labelRect.anchorMax = new Vector2(0.7f, 1);
        labelRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.fontSize = 18;
        labelText.alignment = TextAlignmentOptions.Left;

        // Toggle
        GameObject toggleObj = new GameObject("Toggle");
        toggleObj.transform.SetParent(settingObj.transform);
        RectTransform toggleRect = toggleObj.AddComponent<RectTransform>();
        toggleRect.anchorMin = new Vector2(0.8f, 0.25f);
        toggleRect.anchorMax = new Vector2(0.95f, 0.75f);
        toggleRect.sizeDelta = Vector2.zero;
        toggle = toggleObj.AddComponent<Toggle>();
        toggle.isOn = defaultValue;
        
        GameObject toggleBg = new GameObject("Background");
        toggleBg.transform.SetParent(toggleObj.transform);
        RectTransform toggleBgRect = toggleBg.AddComponent<RectTransform>();
        toggleBgRect.anchorMin = Vector2.zero;
        toggleBgRect.anchorMax = Vector2.one;
        toggleBgRect.sizeDelta = Vector2.zero;
        Image toggleBgImage = toggleBg.AddComponent<Image>();
        toggleBgImage.color = new Color(0.2f, 0.2f, 0.2f);
        toggle.targetGraphic = toggleBgImage;

        GameObject toggleCheckmark = new GameObject("Checkmark");
        toggleCheckmark.transform.SetParent(toggleObj.transform);
        RectTransform toggleCheckmarkRect = toggleCheckmark.AddComponent<RectTransform>();
        toggleCheckmarkRect.anchorMin = Vector2.zero;
        toggleCheckmarkRect.anchorMax = Vector2.one;
        toggleCheckmarkRect.sizeDelta = Vector2.zero;
        Image toggleCheckmarkImage = toggleCheckmark.AddComponent<Image>();
        toggleCheckmarkImage.color = new Color(0.8f, 0.6f, 0.2f);
        toggle.graphic = toggleCheckmarkImage;

        toggle.onValueChanged.AddListener(onValueChanged);
    }

    private void CreateDropdownSetting(string label, string[] options, int defaultValue, Transform parent,
        out TMP_Dropdown dropdown, UnityEngine.Events.UnityAction<int> onValueChanged)
    {
        GameObject settingObj = new GameObject($"Setting_{label}");
        settingObj.transform.SetParent(parent);
        
        LayoutElement layoutElement = settingObj.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 50;

        // Label
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(settingObj.transform);
        RectTransform labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0, 0);
        labelRect.anchorMax = new Vector2(0.4f, 1);
        labelRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.fontSize = 18;
        labelText.alignment = TextAlignmentOptions.Left;

        // Dropdown
        GameObject dropdownObj = new GameObject("Dropdown");
        dropdownObj.transform.SetParent(settingObj.transform);
        RectTransform dropdownRect = dropdownObj.AddComponent<RectTransform>();
        dropdownRect.anchorMin = new Vector2(0.45f, 0.1f);
        dropdownRect.anchorMax = new Vector2(0.95f, 0.9f);
        dropdownRect.sizeDelta = Vector2.zero;
        dropdown = dropdownObj.AddComponent<TMP_Dropdown>();
        
        Image dropdownImage = dropdownObj.AddComponent<Image>();
        dropdownImage.color = new Color(0.2f, 0.2f, 0.2f);
        
        dropdown.options.Clear();
        foreach (string option in options)
        {
            dropdown.options.Add(new TMP_Dropdown.OptionData(option));
        }
        dropdown.value = defaultValue;
        dropdown.RefreshShownValue();

        dropdown.onValueChanged.AddListener(onValueChanged);
    }

    private void CreateControlsMenu()
    {
        if (controlsMenu == null)
        {
            controlsMenu = new GameObject("ControlsMenu");
            controlsMenu.transform.SetParent(pausePanel.transform);
            
            RectTransform rect = controlsMenu.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(600, 600);
            
            Image bg = controlsMenu.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        }

        // Title
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(controlsMenu.transform);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.9f);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
        title.text = "CONTROLS";
        title.fontSize = 36;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;

        // Controls Text
        GameObject controlsText = new GameObject("ControlsText");
        controlsText.transform.SetParent(controlsMenu.transform);
        RectTransform controlsRect = controlsText.AddComponent<RectTransform>();
        controlsRect.anchorMin = new Vector2(0.1f, 0.15f);
        controlsRect.anchorMax = new Vector2(0.9f, 0.85f);
        controlsRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI controls = controlsText.AddComponent<TextMeshProUGUI>();
        controls.fontSize = 16;
        controls.alignment = TextAlignmentOptions.TopLeft;
        controls.text = @"MOVEMENT:
WASD - Move
Shift - Sprint
Ctrl - Crouch
Space - Jump

COMBAT:
Left Click - Primary Attack
Right Click - Aim/Secondary
R - Reload

INTERACTION:
E - Use/Interact
Tab - Inventory
C - Crafting
B - Building Menu

HOTBAR:
1-6 - Select Hotbar Slot
Mouse Wheel - Cycle Hotbar

OTHER:
Esc - Pause Menu";

        // Back Button
        Button backButton = CreateMenuButton("Back", controlsMenu.transform);
        RectTransform backRect = backButton.GetComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0.3f, 0.05f);
        backRect.anchorMax = new Vector2(0.7f, 0.12f);
        backRect.sizeDelta = Vector2.zero;
        backButton.onClick.AddListener(ShowMainMenu);

        controlsMenu.SetActive(false);
    }

    #endregion

    #region NAVIGATION

    private void ShowMainMenu()
    {
        mainMenu.SetActive(true);
        settingsMenu.SetActive(false);
        controlsMenu.SetActive(false);
    }

    private void ShowSettingsMenu()
    {
        mainMenu.SetActive(false);
        settingsMenu.SetActive(true);
        controlsMenu.SetActive(false);
    }

    private void ShowControlsMenu()
    {
        mainMenu.SetActive(false);
        settingsMenu.SetActive(false);
        controlsMenu.SetActive(true);
    }

    #endregion

    #region BUTTON CALLBACKS

    private void OnResumeClicked()
    {
        Close();
    }

    private void OnSettingsClicked()
    {
        ShowSettingsMenu();
    }

    private void OnControlsClicked()
    {
        ShowControlsMenu();
    }

    private void OnDisconnectClicked()
    {
        // TODO: Disconnect from server
        Debug.Log("[PauseMenu] Disconnecting from server...");
        SceneManager.LoadScene("MainMenu"); // Assuming you have a main menu scene
    }

    private void OnQuitClicked()
    {
        Debug.Log("[PauseMenu] Quitting application...");
        Application.Quit();
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

    #endregion

    #region SETTINGS CALLBACKS

    private void OnMouseSensitivityChanged(float value)
    {
        if (mouseSensitivityText != null)
        {
            mouseSensitivityText.text = value.ToString("F1");
        }

        // Apply to camera
        PlayerCamera playerCamera = FindObjectOfType<PlayerCamera>();
        if (playerCamera != null)
        {
            playerCamera.SetMouseSensitivity(value);
        }

        PlayerPrefs.SetFloat("MouseSensitivity", value);
    }

    private void OnMasterVolumeChanged(float value)
    {
        if (masterVolumeText != null)
        {
            masterVolumeText.text = (value * 100f).ToString("F0") + "%";
        }

        AudioListener.volume = value;
        PlayerPrefs.SetFloat("MasterVolume", value);
    }

    private void OnFullscreenChanged(bool value)
    {
        Screen.fullScreen = value;
        PlayerPrefs.SetInt("Fullscreen", value ? 1 : 0);
    }

    private void OnQualityChanged(int value)
    {
        QualitySettings.SetQualityLevel(value);
        PlayerPrefs.SetInt("Quality", value);
    }

    private void OnResolutionChanged(int value)
    {
        if (value >= 0 && value < resolutions.Length)
        {
            Resolution resolution = resolutions[value];
            Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
            PlayerPrefs.SetInt("ResolutionIndex", value);
        }
    }

    #endregion

    #region PUBLIC METHODS

    public void Open()
    {
        isOpen = true;
        pausePanel.SetActive(true);
        ShowMainMenu();
        
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Close()
    {
        isOpen = false;
        pausePanel.SetActive(false);
        
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public bool IsOpen() => isOpen;

    #endregion

    #region LOAD SETTINGS

    private void Start()
    {
        LoadSettings();
    }

    private void LoadSettings()
    {
        // Mouse Sensitivity
        if (PlayerPrefs.HasKey("MouseSensitivity"))
        {
            float sensitivity = PlayerPrefs.GetFloat("MouseSensitivity");
            if (mouseSensitivitySlider != null)
            {
                mouseSensitivitySlider.value = sensitivity;
            }
        }

        // Master Volume
        if (PlayerPrefs.HasKey("MasterVolume"))
        {
            float volume = PlayerPrefs.GetFloat("MasterVolume");
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.value = volume;
            }
            AudioListener.volume = volume;
        }

        // Fullscreen
        if (PlayerPrefs.HasKey("Fullscreen"))
        {
            bool fullscreen = PlayerPrefs.GetInt("Fullscreen") == 1;
            if (fullscreenToggle != null)
            {
                fullscreenToggle.isOn = fullscreen;
            }
        }

        // Quality
        if (PlayerPrefs.HasKey("Quality"))
        {
            int quality = PlayerPrefs.GetInt("Quality");
            if (qualityDropdown != null)
            {
                qualityDropdown.value = quality;
            }
        }

        // Resolution
        if (PlayerPrefs.HasKey("ResolutionIndex"))
        {
            int resolutionIndex = PlayerPrefs.GetInt("ResolutionIndex");
            if (resolutionDropdown != null && resolutionIndex >= 0 && resolutionIndex < resolutions.Length)
            {
                resolutionDropdown.value = resolutionIndex;
            }
        }
    }

    #endregion
}