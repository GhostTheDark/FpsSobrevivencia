using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI de barras de status (vida, fome, sede, stamina, temperatura)
/// </summary>
public class StatusBarsUI : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private Image healthBarFill;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private Color healthColorHigh = Color.green;
    [SerializeField] private Color healthColorMid = Color.yellow;
    [SerializeField] private Color healthColorLow = Color.red;

    [Header("Hunger")]
    [SerializeField] private Image hungerBarFill;
    [SerializeField] private Image hungerIcon;

    [Header("Thirst")]
    [SerializeField] private Image thirstBarFill;
    [SerializeField] private Image thirstIcon;

    [Header("Stamina")]
    [SerializeField] private Image staminaBarFill;
    [SerializeField] private CanvasGroup staminaGroup;

    [Header("Temperature")]
    [SerializeField] private Image temperatureBarFill;
    [SerializeField] private TextMeshProUGUI temperatureText;
    [SerializeField] private Color coldColor = Color.cyan;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hotColor = Color.red;

    [Header("Settings")]
    [SerializeField] private bool smoothTransitions = true;
    [SerializeField] private float transitionSpeed = 5f;
    [SerializeField] private bool hideStaminaWhenFull = true;

    // Player stats
    private PlayerStats playerStats;
    private PlayerHealth health;
    private PlayerHunger hunger;
    private PlayerThirst thirst;
    private PlayerStamina stamina;
    private PlayerTemperature temperature;

    // Valores atuais (para smooth transition)
    private float currentHealthFill = 1f;
    private float currentHungerFill = 1f;
    private float currentThirstFill = 1f;
    private float currentStaminaFill = 1f;
    private float currentTemperatureFill = 0.5f;

    private void Awake()
    {
        CreateUI();
    }

    private void Update()
    {
        if (playerStats != null)
        {
            UpdateBars();
        }
    }

    #region UI CREATION

    private void CreateUI()
    {
        RectTransform rect = gameObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(20, -20);
        rect.sizeDelta = new Vector2(300, 200);

        // Health Bar
        CreateHealthBar();

        // Hunger
        CreateHungerBar();

        // Thirst
        CreateThirstBar();

        // Stamina
        CreateStaminaBar();

        // Temperature
        CreateTemperatureBar();
    }

    private void CreateHealthBar()
    {
        GameObject healthObj = CreateBar("HealthBar", new Vector2(0, 0), new Vector2(250, 30));
        
        // Background
        Image bg = healthObj.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        // Fill
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(healthObj.transform);
        RectTransform fillRect = fillObj.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        fillRect.anchoredPosition = Vector2.zero;

        healthBarFill = fillObj.AddComponent<Image>();
        healthBarFill.color = healthColorHigh;
        healthBarFill.type = Image.Type.Filled;
        healthBarFill.fillMethod = Image.FillMethod.Horizontal;

        // Text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(healthObj.transform);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        healthText = textObj.AddComponent<TextMeshProUGUI>();
        healthText.text = "100 / 100";
        healthText.alignment = TextAlignmentOptions.Center;
        healthText.fontSize = 18;
        healthText.fontStyle = FontStyles.Bold;
    }

    private void CreateHungerBar()
    {
        GameObject hungerObj = CreateBar("HungerBar", new Vector2(0, -40), new Vector2(120, 20));
        
        Image bg = hungerObj.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(hungerObj.transform);
        RectTransform fillRect = fillObj.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;

        hungerBarFill = fillObj.AddComponent<Image>();
        hungerBarFill.color = new Color(0.8f, 0.5f, 0.2f);
        hungerBarFill.type = Image.Type.Filled;
        hungerBarFill.fillMethod = Image.FillMethod.Horizontal;
    }

    private void CreateThirstBar()
    {
        GameObject thirstObj = CreateBar("ThirstBar", new Vector2(0, -70), new Vector2(120, 20));
        
        Image bg = thirstObj.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(thirstObj.transform);
        RectTransform fillRect = fillObj.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;

        thirstBarFill = fillObj.AddComponent<Image>();
        thirstBarFill.color = new Color(0.2f, 0.5f, 0.8f);
        thirstBarFill.type = Image.Type.Filled;
        thirstBarFill.fillMethod = Image.FillMethod.Horizontal;
    }

    private void CreateStaminaBar()
    {
        GameObject staminaObj = CreateBar("StaminaBar", new Vector2(0, -100), new Vector2(250, 15));
        
        staminaGroup = staminaObj.AddComponent<CanvasGroup>();

        Image bg = staminaObj.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);

        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(staminaObj.transform);
        RectTransform fillRect = fillObj.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;

        staminaBarFill = fillObj.AddComponent<Image>();
        staminaBarFill.color = Color.yellow;
        staminaBarFill.type = Image.Type.Filled;
        staminaBarFill.fillMethod = Image.FillMethod.Horizontal;
    }

    private void CreateTemperatureBar()
    {
        GameObject tempObj = CreateBar("TemperatureBar", new Vector2(0, -130), new Vector2(120, 20));
        
        Image bg = tempObj.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(tempObj.transform);
        RectTransform fillRect = fillObj.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;

        temperatureBarFill = fillObj.AddComponent<Image>();
        temperatureBarFill.color = normalColor;
        temperatureBarFill.type = Image.Type.Filled;
        temperatureBarFill.fillMethod = Image.FillMethod.Horizontal;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(tempObj.transform);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        temperatureText = textObj.AddComponent<TextMeshProUGUI>();
        temperatureText.text = "20°C";
        temperatureText.alignment = TextAlignmentOptions.Center;
        temperatureText.fontSize = 14;
    }

    private GameObject CreateBar(string name, Vector2 position, Vector2 size)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(transform);
        
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        return obj;
    }

    #endregion

    #region BINDING

    public void BindToPlayer(PlayerStats stats)
    {
        playerStats = stats;
        
        if (playerStats != null)
        {
            health = playerStats.GetHealthComponent();
            hunger = playerStats.GetHungerComponent();
            thirst = playerStats.GetThirstComponent();
            stamina = playerStats.GetStaminaComponent();
            temperature = playerStats.GetTemperatureComponent();
        }
    }

    #endregion

    #region UPDATE

    private void UpdateBars()
    {
        UpdateHealthBar();
        UpdateHungerBar();
        UpdateThirstBar();
        UpdateStaminaBar();
        UpdateTemperatureBar();
    }

    private void UpdateHealthBar()
    {
        if (health == null || healthBarFill == null) return;

        float targetFill = health.GetHealthPercent();

        if (smoothTransitions)
        {
            currentHealthFill = Mathf.Lerp(currentHealthFill, targetFill, transitionSpeed * Time.deltaTime);
        }
        else
        {
            currentHealthFill = targetFill;
        }

        healthBarFill.fillAmount = currentHealthFill;

        // Color gradient
        if (currentHealthFill > 0.5f)
            healthBarFill.color = Color.Lerp(healthColorMid, healthColorHigh, (currentHealthFill - 0.5f) * 2f);
        else
            healthBarFill.color = Color.Lerp(healthColorLow, healthColorMid, currentHealthFill * 2f);

        // Text
        if (healthText != null)
        {
            healthText.text = $"{health.GetHealth():F0} / {health.GetMaxHealth():F0}";
        }
    }

    private void UpdateHungerBar()
    {
        if (hunger == null || hungerBarFill == null) return;

        float targetFill = hunger.GetHungerPercent();

        if (smoothTransitions)
        {
            currentHungerFill = Mathf.Lerp(currentHungerFill, targetFill, transitionSpeed * Time.deltaTime);
        }
        else
        {
            currentHungerFill = targetFill;
        }

        hungerBarFill.fillAmount = currentHungerFill;

        // Warning color
        if (hunger.IsLowHunger())
        {
            hungerBarFill.color = Color.Lerp(Color.red, new Color(0.8f, 0.5f, 0.2f), Mathf.PingPong(Time.time * 2f, 1f));
        }
    }

    private void UpdateThirstBar()
    {
        if (thirst == null || thirstBarFill == null) return;

        float targetFill = thirst.GetThirstPercent();

        if (smoothTransitions)
        {
            currentThirstFill = Mathf.Lerp(currentThirstFill, targetFill, transitionSpeed * Time.deltaTime);
        }
        else
        {
            currentThirstFill = targetFill;
        }

        thirstBarFill.fillAmount = currentThirstFill;

        // Warning color
        if (thirst.IsLowThirst())
        {
            thirstBarFill.color = Color.Lerp(Color.red, new Color(0.2f, 0.5f, 0.8f), Mathf.PingPong(Time.time * 2f, 1f));
        }
    }

    private void UpdateStaminaBar()
    {
        if (stamina == null || staminaBarFill == null) return;

        float targetFill = stamina.GetStaminaPercent();

        if (smoothTransitions)
        {
            currentStaminaFill = Mathf.Lerp(currentStaminaFill, targetFill, transitionSpeed * Time.deltaTime);
        }
        else
        {
            currentStaminaFill = targetFill;
        }

        staminaBarFill.fillAmount = currentStaminaFill;

        // Hide when full
        if (hideStaminaWhenFull && staminaGroup != null)
        {
            float targetAlpha = (currentStaminaFill >= 0.99f && !stamina.IsUsingStamina()) ? 0f : 1f;
            staminaGroup.alpha = Mathf.Lerp(staminaGroup.alpha, targetAlpha, transitionSpeed * Time.deltaTime);
        }
    }

    private void UpdateTemperatureBar()
    {
        if (temperature == null || temperatureBarFill == null) return;

        float temp = temperature.GetTemperature();
        float targetFill = Mathf.Clamp01((temp + 20f) / 80f); // -20 a 60 = 0 a 1

        if (smoothTransitions)
        {
            currentTemperatureFill = Mathf.Lerp(currentTemperatureFill, targetFill, transitionSpeed * Time.deltaTime);
        }
        else
        {
            currentTemperatureFill = targetFill;
        }

        temperatureBarFill.fillAmount = currentTemperatureFill;

        // Color based on temperature
        if (temperature.IsCold())
        {
            temperatureBarFill.color = coldColor;
        }
        else if (temperature.IsHot())
        {
            temperatureBarFill.color = hotColor;
        }
        else
        {
            temperatureBarFill.color = normalColor;
        }

        // Text
        if (temperatureText != null)
        {
            temperatureText.text = $"{temp:F0}°C";
        }
    }

    #endregion
}