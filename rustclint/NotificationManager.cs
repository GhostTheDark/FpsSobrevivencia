using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace RustlikeClient.UI
{
    /// <summary>
    /// Sistema de notificações (toast messages) para feedback ao jogador
    /// </summary>
    public class NotificationManager : MonoBehaviour
    {
        public static NotificationManager Instance { get; private set; }

        [Header("UI References")]
        public Transform notificationsContainer;
        public GameObject notificationPrefab;

        [Header("Settings")]
        public float notificationDuration = 3f;
        public float fadeInDuration = 0.3f;
        public float fadeOutDuration = 0.5f;
        public int maxNotifications = 5;

        private Queue<NotificationData> _notificationQueue = new Queue<NotificationData>();
        private List<GameObject> _activeNotifications = new List<GameObject>();
        private bool _isProcessingQueue = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Se não tiver container, cria um
            if (notificationsContainer == null)
            {
                CreateNotificationContainer();
            }

            // Se não tiver prefab, cria um simples
            if (notificationPrefab == null)
            {
                CreateSimpleNotificationPrefab();
            }

            Debug.Log("[NotificationManager] Inicializado");
        }

        /// <summary>
        /// Mostra uma notificação
        /// </summary>
        public void ShowNotification(string message, Color? color = null)
        {
            var data = new NotificationData
            {
                Message = message,
                Color = color ?? Color.white,
                Duration = notificationDuration
            };

            _notificationQueue.Enqueue(data);

            if (!_isProcessingQueue)
            {
                StartCoroutine(ProcessQueueCoroutine());
            }
        }

        /// <summary>
        /// Mostra notificação de sucesso (verde)
        /// </summary>
        public void ShowSuccess(string message)
        {
            ShowNotification(message, new Color(0.2f, 0.8f, 0.2f));
        }

        /// <summary>
        /// Mostra notificação de erro (vermelho)
        /// </summary>
        public void ShowError(string message)
        {
            ShowNotification(message, new Color(0.9f, 0.2f, 0.2f));
        }

        /// <summary>
        /// Mostra notificação de aviso (amarelo)
        /// </summary>
        public void ShowWarning(string message)
        {
            ShowNotification(message, new Color(0.9f, 0.8f, 0.2f));
        }

        /// <summary>
        /// Mostra notificação de info (azul)
        /// </summary>
        public void ShowInfo(string message)
        {
            ShowNotification(message, new Color(0.3f, 0.6f, 1f));
        }

        /// <summary>
        /// Processa fila de notificações
        /// </summary>
        private IEnumerator ProcessQueueCoroutine()
        {
            _isProcessingQueue = true;

            while (_notificationQueue.Count > 0)
            {
                // Remove notificações antigas se passar do limite
                while (_activeNotifications.Count >= maxNotifications)
                {
                    RemoveOldestNotification();
                    yield return new WaitForSeconds(0.1f);
                }

                var data = _notificationQueue.Dequeue();
                StartCoroutine(ShowNotificationCoroutine(data));

                yield return new WaitForSeconds(0.2f); // Pequeno delay entre notificações
            }

            _isProcessingQueue = false;
        }

        /// <summary>
        /// Mostra uma notificação individual
        /// </summary>
        private IEnumerator ShowNotificationCoroutine(NotificationData data)
        {
            GameObject notifObj = Instantiate(notificationPrefab, notificationsContainer);
            _activeNotifications.Add(notifObj);

            // Configura texto e cor
            var text = notifObj.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = data.Message;
                text.color = data.Color;
            }

            // Fade in
            CanvasGroup canvasGroup = notifObj.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = notifObj.AddComponent<CanvasGroup>();
            }

            canvasGroup.alpha = 0f;
            float elapsed = 0f;

            // Fade in
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
                yield return null;
            }

            canvasGroup.alpha = 1f;

            // Espera
            yield return new WaitForSeconds(data.Duration);

            // Fade out
            elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
              //  canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
                yield return null;
            }

            // Remove
            _activeNotifications.Remove(notifObj);
            Destroy(notifObj);
        }

        /// <summary>
        /// Remove notificação mais antiga
        /// </summary>
        private void RemoveOldestNotification()
        {
            if (_activeNotifications.Count > 0)
            {
                GameObject oldest = _activeNotifications[0];
                _activeNotifications.RemoveAt(0);
                Destroy(oldest);
            }
        }

        /// <summary>
        /// Cria container se não existir
        /// </summary>
        private void CreateNotificationContainer()
        {
            // Procura Canvas na cena
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[NotificationManager] Nenhum Canvas encontrado na cena!");
                return;
            }

            GameObject container = new GameObject("NotificationsContainer");
            container.transform.SetParent(canvas.transform);

            RectTransform rect = container.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0, -50);
            rect.sizeDelta = new Vector2(400, 600);

            // Vertical Layout Group para empilhar notificações
            var layout = container.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            notificationsContainer = container.transform;
        }

        /// <summary>
        /// Cria prefab simples se não existir
        /// </summary>
        private void CreateSimpleNotificationPrefab()
        {
            GameObject prefab = new GameObject("NotificationPrefab");

            // Background
            var image = prefab.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

            RectTransform rect = prefab.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400, 60);

            // Text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(prefab.transform);

            var text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "Notification";
            text.fontSize = 18;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 5);
            textRect.offsetMax = new Vector2(-10, -5);

            // Canvas Group
            prefab.AddComponent<CanvasGroup>();

            notificationPrefab = prefab;
        }

        private class NotificationData
        {
            public string Message;
            public Color Color;
            public float Duration;
        }
    }
}