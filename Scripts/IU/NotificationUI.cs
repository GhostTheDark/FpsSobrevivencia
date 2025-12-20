
// ============================================================================
// NotificationUI.cs
// ============================================================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class NotificationUI : MonoBehaviour
{
    [SerializeField] private float notificationDuration = 3f;
    private List<Notification> notifications = new List<Notification>();

    private void Awake()
    {
        RectTransform rect = gameObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(1, 1);
        rect.anchoredPosition = new Vector2(-20, -20);
        rect.sizeDelta = new Vector2(300, 400);

        VerticalLayoutGroup layout = gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperRight;
        layout.spacing = 10;
    }

    private void Update()
    {
        for (int i = notifications.Count - 1; i >= 0; i--)
        {
            notifications[i].lifetime -= Time.deltaTime;
            if (notifications[i].lifetime <= 0)
            {
                Destroy(notifications[i].obj);
                notifications.RemoveAt(i);
            }
        }
    }

    public void ShowNotification(string message, NotificationType type)
    {
        GameObject notifObj = new GameObject("Notification");
        notifObj.transform.SetParent(transform);

        Image bg = notifObj.AddComponent<Image>();
        bg.color = GetColorForType(type);

        TextMeshProUGUI text = new GameObject("Text").AddComponent<TextMeshProUGUI>();
        text.transform.SetParent(notifObj.transform);
        text.text = message;
        text.fontSize = 16;
        text.alignment = TextAlignmentOptions.Center;

        notifications.Add(new Notification { 
            obj = notifObj, 
            lifetime = notificationDuration 
        });
    }

    private Color GetColorForType(NotificationType type)
    {
        switch (type)
        {
            case NotificationType.Info: return new Color(0.2f, 0.5f, 0.8f, 0.9f);
            case NotificationType.Warning: return new Color(0.8f, 0.6f, 0.2f, 0.9f);
            case NotificationType.Error: return new Color(0.8f, 0.2f, 0.2f, 0.9f);
            case NotificationType.Success: return new Color(0.2f, 0.8f, 0.3f, 0.9f);
            default: return Color.white;
        }
    }

    private class Notification
    {
        public GameObject obj;
        public float lifetime;
    }
}

public enum NotificationType { Info, Warning, Error, Success }
