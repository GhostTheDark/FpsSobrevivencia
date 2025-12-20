using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Tela de carregamento com barra de progresso e mensagens
/// </summary>
public class LoadingScreenUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private Image progressBar;
    [SerializeField] private TextMeshProUGUI loadingText;
    [SerializeField] private TextMeshProUGUI tipText;
    [SerializeField] private Image loadingSpinner;

    [Header("Settings")]
    [SerializeField] private float spinnerSpeed = 200f;
    [SerializeField] private float tipChangeInterval = 5f;

    [Header("Loading Tips")]
    [SerializeField] private string[] loadingTips = new string[]
    {
        "Tip: Build a Tool Cupboard to prevent decay and raiding",
        "Tip: Use sleeping bags as respawn points",
        "Tip: Stone tools are more durable than wooden ones",
        "Tip: Keep an eye on your temperature in cold biomes",
        "Tip: Headshots deal critical damage",
        "Tip: Research blueprints to unlock advanced items",
        "Tip: Metal armor provides the best protection",
        "Tip: Use furnaces to smelt ore into metal fragments",
        "Tip: Always carry bandages for emergencies",
        "Tip: Building near monuments gives access to better loot"
    };

    private bool isActive = false;
    private float currentProgress = 0f;
    private float targetProgress = 0f;
    private float tipTimer = 0f;
    private int currentTipIndex = 0;

    private void Awake()
    {
        CreateLoadingScreen();
        loadingPanel.SetActive(false);
    }

    private void Update()
    {
        if (!isActive) return;

        // Animate progress bar
        if (currentProgress < targetProgress)
        {
            currentProgress = Mathf.MoveTowards(currentProgress, targetProgress, Time.deltaTime * 0.5f);
            if (progressBar != null)
            {
                progressBar.fillAmount = currentProgress;
            }
        }

        // Rotate spinner
        if (loadingSpinner != null)
        {
            loadingSpinner.transform.Rotate(Vector3.forward, -spinnerSpeed * Time.deltaTime);
        }

        // Change tips
        tipTimer += Time.deltaTime;
        if (tipTimer >= tipChangeInterval)
        {
            tipTimer = 0f;
            ShowNextTip();
        }
    }

    #region UI CREATION

    private void CreateLoadingScreen()
    {
        if (loadingPanel == null)
        {
            GameObject panel = new GameObject("LoadingPanel");
            panel.transform.SetParent(transform);
            
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            
            Image bg = panel.AddComponent<Image>();
            bg.color = Color.black;
            
            loadingPanel = panel;
        }

        // Logo / Title
        GameObject logoObj = new GameObject("Logo");
        logoObj.transform.SetParent(loadingPanel.transform);
        RectTransform logoRect = logoObj.AddComponent<RectTransform>();
        logoRect.anchorMin = new Vector2(0.5f, 0.7f);
        logoRect.anchorMax = new Vector2(0.5f, 0.7f);
        logoRect.pivot = new Vector2(0.5f, 0.5f);
        logoRect.sizeDelta = new Vector2(400, 100);
        TextMeshProUGUI logo = logoObj.AddComponent<TextMeshProUGUI>();
        logo.text = "RUST CLONE";
        logo.fontSize = 60;
        logo.fontStyle = FontStyles.Bold;
        logo.alignment = TextAlignmentOptions.Center;
        logo.color = new Color(0.8f, 0.6f, 0.2f);

        // Loading Text
        GameObject loadingTextObj = new GameObject("LoadingText");
        loadingTextObj.transform.SetParent(loadingPanel.transform);
        RectTransform loadingTextRect = loadingTextObj.AddComponent<RectTransform>();
        loadingTextRect.anchorMin = new Vector2(0.5f, 0.5f);
        loadingTextRect.anchorMax = new Vector2(0.5f, 0.5f);
        loadingTextRect.pivot = new Vector2(0.5f, 0.5f);
        loadingTextRect.sizeDelta = new Vector2(400, 40);
        loadingText = loadingTextObj.AddComponent<TextMeshProUGUI>();
        loadingText.text = "Loading...";
        loadingText.fontSize = 24;
        loadingText.alignment = TextAlignmentOptions.Center;

        // Progress Bar Background
        GameObject progressBgObj = new GameObject("ProgressBarBG");
        progressBgObj.transform.SetParent(loadingPanel.transform);
        RectTransform progressBgRect = progressBgObj.AddComponent<RectTransform>();
        progressBgRect.anchorMin = new Vector2(0.5f, 0.45f);
        progressBgRect.anchorMax = new Vector2(0.5f, 0.45f);
        progressBgRect.pivot = new Vector2(0.5f, 0.5f);
        progressBgRect.sizeDelta = new Vector2(500, 20);
        Image progressBg = progressBgObj.AddComponent<Image>();
        progressBg.color = new Color(0.2f, 0.2f, 0.2f);

        // Progress Bar Fill
        GameObject progressFillObj = new GameObject("ProgressBarFill");
        progressFillObj.transform.SetParent(progressBgObj.transform);
        RectTransform progressFillRect = progressFillObj.AddComponent<RectTransform>();
        progressFillRect.anchorMin = Vector2.zero;
        progressFillRect.anchorMax = Vector2.one;
        progressFillRect.sizeDelta = Vector2.zero;
        progressBar = progressFillObj.AddComponent<Image>();
        progressBar.color = new Color(0.8f, 0.6f, 0.2f);
        progressBar.type = Image.Type.Filled;
        progressBar.fillMethod = Image.FillMethod.Horizontal;
        progressBar.fillAmount = 0f;

        // Spinner
        GameObject spinnerObj = new GameObject("Spinner");
        spinnerObj.transform.SetParent(loadingPanel.transform);
        RectTransform spinnerRect = spinnerObj.AddComponent<RectTransform>();
        spinnerRect.anchorMin = new Vector2(0.5f, 0.4f);
        spinnerRect.anchorMax = new Vector2(0.5f, 0.4f);
        spinnerRect.pivot = new Vector2(0.5f, 0.5f);
        spinnerRect.sizeDelta = new Vector2(40, 40);
        loadingSpinner = spinnerObj.AddComponent<Image>();
        loadingSpinner.color = Color.white;
        // TODO: Add spinner sprite

        // Tips Text
        GameObject tipTextObj = new GameObject("TipText");
        tipTextObj.transform.SetParent(loadingPanel.transform);
        RectTransform tipTextRect = tipTextObj.AddComponent<RectTransform>();
        tipTextRect.anchorMin = new Vector2(0.5f, 0.2f);
        tipTextRect.anchorMax = new Vector2(0.5f, 0.2f);
        tipTextRect.pivot = new Vector2(0.5f, 0.5f);
        tipTextRect.sizeDelta = new Vector2(600, 60);
        tipText = tipTextObj.AddComponent<TextMeshProUGUI>();
        tipText.fontSize = 16;
        tipText.alignment = TextAlignmentOptions.Center;
        tipText.color = Color.gray;
        tipText.overflowMode = TextOverflowModes.Truncate;
    }

    #endregion

    #region PUBLIC METHODS

    /// <summary>
    /// Mostra tela de loading
    /// </summary>
    public void Show(string message = "Loading...")
    {
        isActive = true;
        loadingPanel.SetActive(true);
        
        if (loadingText != null)
        {
            loadingText.text = message;
        }

        currentProgress = 0f;
        targetProgress = 0f;
        tipTimer = 0f;
        
        ShowRandomTip();
    }

    /// <summary>
    /// Esconde tela de loading
    /// </summary>
    public void Hide()
    {
        isActive = false;
        loadingPanel.SetActive(false);
    }

    /// <summary>
    /// Define progresso (0-1)
    /// </summary>
    public void SetProgress(float progress)
    {
        targetProgress = Mathf.Clamp01(progress);
    }

    /// <summary>
    /// Define mensagem de loading
    /// </summary>
    public void SetMessage(string message)
    {
        if (loadingText != null)
        {
            loadingText.text = message;
        }
    }

    /// <summary>
    /// Define velocidade de progresso automático
    /// </summary>
    public void SetAutoProgress(float speed)
    {
        // Incrementa progresso automaticamente
        StartCoroutine(AutoProgress(speed));
    }

    #endregion

    #region TIPS

    private void ShowRandomTip()
    {
        if (loadingTips.Length == 0) return;

        currentTipIndex = Random.Range(0, loadingTips.Length);
        if (tipText != null)
        {
            tipText.text = loadingTips[currentTipIndex];
        }
    }

    private void ShowNextTip()
    {
        if (loadingTips.Length == 0) return;

        currentTipIndex = (currentTipIndex + 1) % loadingTips.Length;
        if (tipText != null)
        {
            tipText.text = loadingTips[currentTipIndex];
        }
    }

    #endregion

    #region COROUTINES

    private System.Collections.IEnumerator AutoProgress(float speed)
    {
        while (isActive && targetProgress < 1f)
        {
            targetProgress += speed * Time.deltaTime;
            targetProgress = Mathf.Min(targetProgress, 1f);
            yield return null;
        }
    }

    #endregion

    #region PRESETS

    /// <summary>
    /// Mostra loading para conectar ao servidor
    /// </summary>
    public void ShowConnecting()
    {
        Show("Connecting to server...");
        SetAutoProgress(0.1f);
    }

    /// <summary>
    /// Mostra loading para carregar mundo
    /// </summary>
    public void ShowLoadingWorld()
    {
        Show("Loading world...");
        SetAutoProgress(0.15f);
    }

    /// <summary>
    /// Mostra loading para spawnar jogador
    /// </summary>
    public void ShowSpawning()
    {
        Show("Spawning player...");
        SetProgress(0.9f);
    }

    #endregion
}