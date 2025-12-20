// ============================================================================
// HitMarkerUI.cs
// ============================================================================
using UnityEngine;
using UnityEngine.UI;

public class HitMarkerUI : MonoBehaviour
{
    [SerializeField] private float displayDuration = 0.2f;
    [SerializeField] private float size = 20f;
    
    private Image[] markers = new Image[4];
    private float timer = 0f;

    private void Awake()
    {
        CreateHitMarker();
        gameObject.SetActive(false);
    }

    private void CreateHitMarker()
    {
        RectTransform rect = gameObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(size * 2, size * 2);

        for (int i = 0; i < 4; i++)
        {
            GameObject line = new GameObject($"Line_{i}");
            line.transform.SetParent(transform);
            markers[i] = line.AddComponent<Image>();
            markers[i].color = Color.white;
        }
    }

    private void Update()
    {
        if (gameObject.activeSelf)
        {
            timer -= Time.deltaTime;
            if (timer <= 0) gameObject.SetActive(false);
        }
    }

    public void ShowHit(bool isHeadshot)
    {
        gameObject.SetActive(true);
        timer = displayDuration;
        
        Color color = isHeadshot ? Color.red : Color.white;
        foreach (var marker in markers)
        {
            if (marker != null) marker.color = color;
        }
    }
}