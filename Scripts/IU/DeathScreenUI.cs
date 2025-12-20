// ============================================================================
// DeathScreenUI.cs
// ============================================================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DeathScreenUI : MonoBehaviour
{
    private TextMeshProUGUI killerText;
    private TextMeshProUGUI respawnText;
    private float respawnTime;
    private float respawnTimer;

    private void Awake()
    {
        CreateDeathScreen();
        gameObject.SetActive(false);
    }

    private void CreateDeathScreen()
    {
        RectTransform rect = gameObject.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        Image bg = gameObject.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.8f);

        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(transform);
        TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
        title.text = "YOU DIED";
        title.fontSize = 60;
        title.color = Color.red;
        title.alignment = TextAlignmentOptions.Center;

        GameObject killerObj = new GameObject("Killer");
        killerObj.transform.SetParent(transform);
        killerText = killerObj.AddComponent<TextMeshProUGUI>();
        killerText.fontSize = 24;
        killerText.alignment = TextAlignmentOptions.Center;

        GameObject respawnObj = new GameObject("Respawn");
        respawnObj.transform.SetParent(transform);
        respawnText = respawnObj.AddComponent<TextMeshProUGUI>();
        respawnText.fontSize = 20;
        respawnText.alignment = TextAlignmentOptions.Center;
    }

    public void Show(string killerName, float respawnTime)
    {
        gameObject.SetActive(true);
        
        if (string.IsNullOrEmpty(killerName))
        {
            killerText.text = "You died";
        }
        else
        {
            killerText.text = $"Killed by {killerName}";
        }

        this.respawnTime = respawnTime;
        respawnTimer = respawnTime;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (gameObject.activeSelf && respawnTimer > 0)
        {
            respawnTimer -= Time.deltaTime;
            respawnText.text = $"Respawning in {Mathf.Ceil(respawnTimer)}...";
        }
    }
}