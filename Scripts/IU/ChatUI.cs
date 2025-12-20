// ============================================================================
// ChatUI.cs
// ============================================================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ChatUI : MonoBehaviour
{
    [SerializeField] private int maxMessages = 50;
    [SerializeField] private float messageFadeTime = 10f;
    
    private TMP_InputField inputField;
    private ScrollRect scrollRect;
    private Transform messageContainer;
    private List<ChatMessage> messages = new List<ChatMessage>();

    private void Awake() { CreateChatUI(); }

    private void CreateChatUI()
    {
        RectTransform rect = gameObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(0, 0.5f);
        rect.pivot = new Vector2(0, 0);
        rect.anchoredPosition = new Vector2(20, 20);
        rect.sizeDelta = new Vector2(400, 0);

        GameObject scrollObj = new GameObject("ScrollView");
        scrollObj.transform.SetParent(transform);
        scrollRect = scrollObj.AddComponent<ScrollRect>();
        
        GameObject content = new GameObject("Content");
        content.transform.SetParent(scrollObj.transform);
        messageContainer = content.transform;
        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.childForceExpandHeight = false;

        GameObject inputObj = new GameObject("Input");
        inputObj.transform.SetParent(transform);
        inputField = inputObj.AddComponent<TMP_InputField>();
        inputField.placeholder = new GameObject("Placeholder").AddComponent<TextMeshProUGUI>();
        inputField.textComponent = new GameObject("Text").AddComponent<TextMeshProUGUI>();
    }

    public void AddMessage(string text, int senderId)
    {
        GameObject msgObj = new GameObject($"Message_{messages.Count}");
        msgObj.transform.SetParent(messageContainer);
        TextMeshProUGUI msgText = msgObj.AddComponent<TextMeshProUGUI>();
        msgText.text = $"[{senderId}]: {text}";
        msgText.fontSize = 14;

        ChatMessage msg = new ChatMessage { obj = msgObj, spawnTime = Time.time };
        messages.Add(msg);

        if (messages.Count > maxMessages)
        {
            Destroy(messages[0].obj);
            messages.RemoveAt(0);
        }
    }

    public void FocusInput()
    {
        if (inputField != null)
        {
            inputField.ActivateInputField();
        }
    }

    private class ChatMessage
    {
        public GameObject obj;
        public float spawnTime;
    }
}