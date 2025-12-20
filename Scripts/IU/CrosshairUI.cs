using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Crosshair dinâmico que muda baseado em contexto
/// </summary>
public class CrosshairUI : MonoBehaviour
{
    [Header("Crosshair Parts")]
    [SerializeField] private Image centerDot;
    [SerializeField] private Image topLine;
    [SerializeField] private Image bottomLine;
    [SerializeField] private Image leftLine;
    [SerializeField] private Image rightLine;

    [Header("Settings")]
    [SerializeField] private float baseSize = 2f;
    [SerializeField] private float baseGap = 10f;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color enemyColor = Color.red;
    [SerializeField] private Color friendlyColor = Color.green;

    [Header("Dynamic Spread")]
    [SerializeField] private bool dynamicSpread = true;
    [SerializeField] private float spreadMultiplier = 20f;
    [SerializeField] private float spreadRecoverySpeed = 5f;

    [Header("Hit Feedback")]
    [SerializeField] private bool flashOnHit = true;
    [SerializeField] private float flashDuration = 0.1f;

    // Estado
    private float currentSpread = 0f;
    private float targetSpread = 0f;
    private bool isFlashing = false;
    private float flashTimer = 0f;

    private void Awake()
    {
        CreateCrosshair();
    }

    private void Update()
    {
        UpdateSpread();
        UpdateFlash();
    }

    #region UI CREATION

    private void CreateCrosshair()
    {
        RectTransform rect = gameObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(100, 100);

        // Center dot
        centerDot = CreateCrosshairPart("CenterDot", Vector2.zero, new Vector2(baseSize, baseSize));

        // Top
        topLine = CreateCrosshairPart("Top", new Vector2(0, baseGap), new Vector2(baseSize, 10));

        // Bottom
        bottomLine = CreateCrosshairPart("Bottom", new Vector2(0, -baseGap), new Vector2(baseSize, 10));

        // Left
        leftLine = CreateCrosshairPart("Left", new Vector2(-baseGap, 0), new Vector2(10, baseSize));

        // Right
        rightLine = CreateCrosshairPart("Right", new Vector2(baseGap, 0), new Vector2(10, baseSize));

        SetColor(normalColor);
    }

    private Image CreateCrosshairPart(string name, Vector2 position, Vector2 size)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(transform);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image img = obj.AddComponent<Image>();
        img.color = normalColor;

        return img;
    }

    #endregion

    #region SPREAD

    private void UpdateSpread()
    {
        if (!dynamicSpread) return;

        // Lerp to target spread
        currentSpread = Mathf.Lerp(currentSpread, targetSpread, spreadRecoverySpeed * Time.deltaTime);

        // Apply spread to lines
        float gap = baseGap + currentSpread * spreadMultiplier;

        if (topLine != null)
            topLine.rectTransform.anchoredPosition = new Vector2(0, gap);

        if (bottomLine != null)
            bottomLine.rectTransform.anchoredPosition = new Vector2(0, -gap);

        if (leftLine != null)
            leftLine.rectTransform.anchoredPosition = new Vector2(-gap, 0);

        if (rightLine != null)
            rightLine.rectTransform.anchoredPosition = new Vector2(gap, 0);

        // Recover spread
        targetSpread = Mathf.Max(0, targetSpread - spreadRecoverySpeed * Time.deltaTime);
    }

    public void AddSpread(float amount)
    {
        targetSpread = Mathf.Min(1f, targetSpread + amount);
    }

    #endregion

    #region FLASH

    private void UpdateFlash()
    {
        if (!isFlashing) return;

        flashTimer -= Time.deltaTime;

        if (flashTimer <= 0)
        {
            isFlashing = false;
            SetColor(normalColor);
        }
    }

    public void Flash(Color color)
    {
        if (!flashOnHit) return;

        SetColor(color);
        isFlashing = true;
        flashTimer = flashDuration;
    }

    #endregion

    #region PUBLIC METHODS

    public void SetColor(Color color)
    {
        if (centerDot != null) centerDot.color = color;
        if (topLine != null) topLine.color = color;
        if (bottomLine != null) bottomLine.color = color;
        if (leftLine != null) leftLine.color = color;
        if (rightLine != null) rightLine.color = color;
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    public void OnEnemyHovered()
    {
        SetColor(enemyColor);
    }

    public void OnFriendlyHovered()
    {
        SetColor(friendlyColor);
    }

    public void OnNothingHovered()
    {
        if (!isFlashing)
            SetColor(normalColor);
    }

    public void OnHit()
    {
        Flash(Color.yellow);
    }

    public void OnHeadshot()
    {
        Flash(Color.red);
    }

    #endregion
}