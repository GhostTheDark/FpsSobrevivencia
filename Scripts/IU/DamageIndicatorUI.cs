// ============================================================================
// DamageIndicatorUI.cs
// ============================================================================
using UnityEngine;
using UnityEngine.UI;

public class DamageIndicatorUI : MonoBehaviour
{
    [SerializeField] private GameObject indicatorPrefab;
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private float indicatorDistance = 100f;

    private void Awake()
    {
        RectTransform rect = gameObject.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
    }

    public void ShowDamage(Vector3 direction)
    {
        GameObject indicator = new GameObject("DamageIndicator");
        indicator.transform.SetParent(transform);

        RectTransform rect = indicator.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(50, 50);

        float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        Vector2 pos = new Vector2(
            Mathf.Sin(angle * Mathf.Deg2Rad) * indicatorDistance,
            Mathf.Cos(angle * Mathf.Deg2Rad) * indicatorDistance
        );
        rect.anchoredPosition = pos;

        Image img = indicator.AddComponent<Image>();
        img.color = new Color(1, 0, 0, 0.7f);

        Destroy(indicator, fadeDuration);
    }
}