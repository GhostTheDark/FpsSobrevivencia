using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using RustlikeClient.Network;

namespace RustlikeGame.Combat.UI
{
    /// <summary>
    /// Tela de morte e sistema de respawn
    /// </summary>
    public class DeathScreen : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private NetworkManager networkManager;
        [SerializeField] private GameObject deathScreenPanel;

        [Header("Death Info")]
        [SerializeField] private TextMeshProUGUI killerNameText;
        [SerializeField] private TextMeshProUGUI weaponUsedText;
        [SerializeField] private TextMeshProUGUI distanceText;
        [SerializeField] private Image weaponIcon;

        [Header("Stats")]
        [SerializeField] private TextMeshProUGUI survivalTimeText;
        [SerializeField] private TextMeshProUGUI killsText;
        [SerializeField] private TextMeshProUGUI damageDealtText;

        [Header("Respawn")]
        [SerializeField] private Button respawnButton;
        [SerializeField] private TextMeshProUGUI respawnTimerText;
        [SerializeField] private float respawnDelay = 5f;

        private bool _isDead;
        private float _deathTime;
        private float _respawnAvailableTime;

        private void Start()
        {
            if (deathScreenPanel != null)
            {
                deathScreenPanel.SetActive(false);
            }

            if (respawnButton != null)
            {
                respawnButton.onClick.AddListener(OnRespawnClicked);
            }
        }

        private void Update()
        {
            if (_isDead)
            {
                UpdateRespawnTimer();
            }
        }

        /// <summary>
        /// Mostra tela de morte
        /// </summary>
        public void ShowDeathScreen(string killerName, string weaponUsed, float distance, bool wasHeadshot)
        {
            _isDead = true;
            _deathTime = Time.time;
            _respawnAvailableTime = Time.time + respawnDelay;

            if (deathScreenPanel != null)
            {
                deathScreenPanel.SetActive(true);
            }

            // Info do killer
            if (killerNameText != null)
            {
                killerNameText.text = $"Morto por: {killerName}";
            }

            if (weaponUsedText != null)
            {
                string headshotText = wasHeadshot ? " 💥 HEADSHOT" : "";
                weaponUsedText.text = $"{weaponUsed}{headshotText}";
            }

            if (distanceText != null)
            {
                distanceText.text = $"{distance:F1}m";
            }

            // TODO: Carregar stats do jogador
            UpdateStats();

            // Desabilita respawn button temporariamente
            if (respawnButton != null)
            {
                respawnButton.interactable = false;
            }

            // Esconde cursor
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            Debug.Log($"[DeathScreen] Você foi morto por {killerName} com {weaponUsed}");
        }

        /// <summary>
        /// Esconde tela de morte
        /// </summary>
        public void HideDeathScreen()
        {
            _isDead = false;

            if (deathScreenPanel != null)
            {
                deathScreenPanel.SetActive(false);
            }

            // Volta cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            Debug.Log("[DeathScreen] Respawnou!");
        }

        /// <summary>
        /// Atualiza timer de respawn
        /// </summary>
        private void UpdateRespawnTimer()
        {
            float timeUntilRespawn = _respawnAvailableTime - Time.time;

            if (timeUntilRespawn <= 0)
            {
                // Pode respawnar
                if (respawnButton != null && !respawnButton.interactable)
                {
                    respawnButton.interactable = true;
                }

                if (respawnTimerText != null)
                {
                    respawnTimerText.text = "Pressione para Respawnar";
                }
            }
            else
            {
                // Ainda esperando
                if (respawnTimerText != null)
                {
                    respawnTimerText.text = $"Respawn em: {Mathf.CeilToInt(timeUntilRespawn)}s";
                }
            }
        }

        /// <summary>
        /// Quando botão de respawn é clicado
        /// </summary>
        private void OnRespawnClicked()
        {
            float timeUntilRespawn = _respawnAvailableTime - Time.time;
            if (timeUntilRespawn > 0)
            {
                Debug.Log("[DeathScreen] Ainda não pode respawnar!");
                return;
            }

            Debug.Log("[DeathScreen] Solicitando respawn ao servidor...");
            
            // Envia request de respawn para servidor
            networkManager.SendRespawnRequest();

            // Desabilita botão enquanto espera
            if (respawnButton != null)
            {
                respawnButton.interactable = false;
            }

            if (respawnTimerText != null)
            {
                respawnTimerText.text = "Aguardando servidor...";
            }
        }

        /// <summary>
        /// Atualiza stats na tela
        /// </summary>
        private void UpdateStats()
        {
            // TODO: Pegar stats reais do PlayerStats
            float survivalTime = _deathTime; // Placeholder
            int kills = 0; // Placeholder
            float damageDealt = 0f; // Placeholder

            if (survivalTimeText != null)
            {
                int minutes = Mathf.FloorToInt(survivalTime / 60f);
                int seconds = Mathf.FloorToInt(survivalTime % 60f);
                survivalTimeText.text = $"Tempo de Sobrevivência: {minutes:00}:{seconds:00}";
            }

            if (killsText != null)
            {
                killsText.text = $"Kills: {kills}";
            }

            if (damageDealtText != null)
            {
                damageDealtText.text = $"Dano Causado: {damageDealt:F0}";
            }
        }

        /// <summary>
        /// Força respawn imediato (admin/debug)
        /// </summary>
        public void ForceRespawn()
        {
            _respawnAvailableTime = Time.time;
            OnRespawnClicked();
        }

        public bool IsDead() => _isDead;
    }
}
