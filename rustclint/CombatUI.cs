using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace RustlikeGame.Combat.UI
{
    /// <summary>
    /// UI de combate - Crosshair, ammo, hitmarker, kill feed
    /// </summary>
    public class CombatUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private WeaponManager weaponManager;

        [Header("Crosshair")]
        [SerializeField] private RectTransform crosshair;
        [SerializeField] private Image crosshairImage;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color enemyColor = Color.red;

        [Header("Hitmarker")]
        [SerializeField] private Image hitmarker;
        [SerializeField] private Color hitmarkerColor = Color.white;
        [SerializeField] private Color headshotColor = Color.red;
        [SerializeField] private float hitmarkerDuration = 0.2f;

        [Header("Ammo Counter")]
        [SerializeField] private TextMeshProUGUI ammoText;
        [SerializeField] private TextMeshProUGUI reserveAmmoText;
        [SerializeField] private Image reloadBar;
        [SerializeField] private GameObject reloadBarContainer;

        [Header("Weapon Info")]
        [SerializeField] private TextMeshProUGUI weaponNameText;
        [SerializeField] private Image weaponIcon;

        [Header("Damage Numbers")]
        [SerializeField] private GameObject damageNumberPrefab;
        [SerializeField] private Transform damageNumberParent;

        [Header("Kill Feed")]
        [SerializeField] private Transform killFeedContainer;
        [SerializeField] private GameObject killFeedEntryPrefab;
        [SerializeField] private int maxKillFeedEntries = 5;

        private void Start()
        {
            if (weaponManager != null)
            {
                weaponManager.OnWeaponEquipped += OnWeaponEquipped;
                weaponManager.OnAmmoChanged += OnAmmoChanged;
                weaponManager.OnReloadStarted += OnReloadStarted;
                weaponManager.OnReloadCompleted += OnReloadCompleted;
            }

            if (hitmarker != null)
            {
                hitmarker.gameObject.SetActive(false);
            }

            if (reloadBarContainer != null)
            {
                reloadBarContainer.SetActive(false);
            }
        }

        private void Update()
        {
            UpdateReloadBar();
        }

        /// <summary>
        /// Quando arma é equipada
        /// </summary>
        private void OnWeaponEquipped(WeaponData weapon)
        {
            if (weapon == null)
            {
                // Sem arma equipada
                if (weaponNameText != null) weaponNameText.text = "";
                if (ammoText != null) ammoText.text = "";
                if (reserveAmmoText != null) reserveAmmoText.text = "";
                if (weaponIcon != null) weaponIcon.gameObject.SetActive(false);
                return;
            }

            // Atualiza info da arma
            if (weaponNameText != null)
            {
                weaponNameText.text = weapon.WeaponName;
            }

            if (weaponIcon != null && weapon.Icon != null)
            {
                weaponIcon.sprite = weapon.Icon;
                weaponIcon.gameObject.SetActive(true);
            }

            // Atualiza munição
            OnAmmoChanged(weaponManager.GetCurrentAmmo(), weaponManager.GetReserveAmmo());
        }

        /// <summary>
        /// Quando munição muda
        /// </summary>
        private void OnAmmoChanged(int current, int reserve)
        {
            var weapon = weaponManager.GetCurrentWeapon();
            if (weapon == null || !weapon.IsRanged())
            {
                if (ammoText != null) ammoText.text = "";
                if (reserveAmmoText != null) reserveAmmoText.text = "";
                return;
            }

            if (ammoText != null)
            {
                ammoText.text = current.ToString();
                
                // Muda cor se munição baixa
                if (current == 0)
                {
                    ammoText.color = Color.red;
                }
                else if (current <= weapon.MagazineSize * 0.2f)
                {
                    ammoText.color = Color.yellow;
                }
                else
                {
                    ammoText.color = Color.white;
                }
            }

            if (reserveAmmoText != null)
            {
                reserveAmmoText.text = $"/ {reserve}";
            }
        }

        /// <summary>
        /// Quando recarga inicia
        /// </summary>
        private void OnReloadStarted()
        {
            if (reloadBarContainer != null)
            {
                reloadBarContainer.SetActive(true);
            }
        }

        /// <summary>
        /// Quando recarga completa
        /// </summary>
        private void OnReloadCompleted()
        {
            if (reloadBarContainer != null)
            {
                reloadBarContainer.SetActive(false);
            }
        }

        /// <summary>
        /// Atualiza barra de reload
        /// </summary>
        private void UpdateReloadBar()
        {
            if (reloadBar == null || weaponManager == null) return;
            if (!weaponManager.IsReloading()) return;

            float progress = weaponManager.GetReloadProgress();
            reloadBar.fillAmount = progress;
        }

        /// <summary>
        /// Mostra hitmarker quando acerta
        /// </summary>
        public void ShowHitmarker(bool isHeadshot = false)
        {
            if (hitmarker == null) return;

            StopAllCoroutines();
            StartCoroutine(HitmarkerRoutine(isHeadshot));
        }

        private IEnumerator HitmarkerRoutine(bool isHeadshot)
        {
            hitmarker.gameObject.SetActive(true);
            hitmarker.color = isHeadshot ? headshotColor : hitmarkerColor;

            // Animação rápida de escala
            Vector3 originalScale = hitmarker.transform.localScale;
            hitmarker.transform.localScale = originalScale * 1.5f;

            float elapsed = 0f;
            while (elapsed < hitmarkerDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / hitmarkerDuration;
                
                hitmarker.transform.localScale = Vector3.Lerp(originalScale * 1.5f, originalScale, t);
                
                Color col = hitmarker.color;
                col.a = Mathf.Lerp(1f, 0f, t);
                hitmarker.color = col;

                yield return null;
            }

            hitmarker.gameObject.SetActive(false);
        }

        /// <summary>
        /// Mostra número de dano flutuante
        /// </summary>
        public void ShowDamageNumber(Vector3 worldPosition, float damage, bool isCritical = false)
        {
            if (damageNumberPrefab == null || damageNumberParent == null) return;

            // Converte posição 3D para 2D na tela
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPosition);
            
            if (screenPos.z < 0) return; // Atrás da câmera

            // Instancia número de dano
            GameObject dmgNumber = Instantiate(damageNumberPrefab, damageNumberParent);
            RectTransform rect = dmgNumber.GetComponent<RectTransform>();
            
            if (rect != null)
            {
                rect.position = screenPos;
            }

            // Configura texto
            var text = dmgNumber.GetComponent<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = Mathf.RoundToInt(damage).ToString();
                text.color = isCritical ? Color.red : Color.white;
                text.fontSize = isCritical ? 48 : 32;
            }

            // Anima e destroi
            StartCoroutine(AnimateDamageNumber(dmgNumber));
        }

        private IEnumerator AnimateDamageNumber(GameObject dmgNumber)
        {
            RectTransform rect = dmgNumber.GetComponent<RectTransform>();
            CanvasGroup canvasGroup = dmgNumber.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = dmgNumber.AddComponent<CanvasGroup>();
            }

            Vector3 startPos = rect.anchoredPosition;
            float duration = 1f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Move para cima
                rect.anchoredPosition = startPos + Vector3.up * 100f * t;

                // Fade out
                canvasGroup.alpha = 1f - t;

                yield return null;
            }

            Destroy(dmgNumber);
        }

        /// <summary>
        /// Adiciona entrada no kill feed
        /// </summary>
        public void AddKillFeedEntry(string killerName, string victimName, string weaponName, bool isHeadshot)
        {
            if (killFeedContainer == null || killFeedEntryPrefab == null) return;

            // Cria nova entrada
            GameObject entry = Instantiate(killFeedEntryPrefab, killFeedContainer);
            
            // Configura texto
            var text = entry.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                string message = $"{killerName} [{weaponName}] {victimName}";
                if (isHeadshot) message += " 💥 HEADSHOT";
                text.text = message;
            }

            // Remove entradas antigas
            if (killFeedContainer.childCount > maxKillFeedEntries)
            {
                Destroy(killFeedContainer.GetChild(0).gameObject);
            }

            // Auto-remove após 5 segundos
            Destroy(entry, 5f);
        }

        /// <summary>
        /// Muda cor da crosshair
        /// </summary>
        public void SetCrosshairColor(bool isEnemy)
        {
            if (crosshairImage == null) return;
            crosshairImage.color = isEnemy ? enemyColor : normalColor;
        }
    }
}
