using UnityEngine;

namespace RustlikeClient.World
{
    /// <summary>
    /// Tipos de recursos (deve coincidir com o servidor)
    /// </summary>
    public enum ResourceType : byte
    {
        Tree = 0,
        Stone = 1,
        MetalOre = 2,
        SulfurOre = 3
    }

    /// <summary>
    /// Representa um recurso visual no mundo (árvore, pedra, etc)
    /// </summary>
    public class ResourceNode : MonoBehaviour
    {
        [Header("Resource Data")]
        public int resourceId;
        public ResourceType resourceType;
        public float health;
        public float maxHealth;
        public bool isAlive = true;

        [Header("Visual Components")]
        public MeshRenderer meshRenderer;
        public Collider resourceCollider;
        public GameObject destructionEffect;

        [Header("Health Bar (Optional)")]
        public GameObject healthBarCanvas;
        public UnityEngine.UI.Image healthBarFill;

        [Header("Materials")]
        public Material normalMaterial;
        public Material damagedMaterial;
        public Material criticalMaterial;

        private Color _originalColor;
        private bool _isShowingDamage = false;

        private void Awake()
        {
            if (meshRenderer == null)
                meshRenderer = GetComponent<MeshRenderer>();

            if (resourceCollider == null)
                resourceCollider = GetComponent<Collider>();

            if (meshRenderer != null && meshRenderer.material != null)
            {
                _originalColor = meshRenderer.material.color;
            }

            // Health bar escondida por padrão
            if (healthBarCanvas != null)
                healthBarCanvas.SetActive(false);
        }

        /// <summary>
        /// Inicializa o recurso com dados do servidor
        /// </summary>
        public void Initialize(int id, ResourceType type, Vector3 position, float hp, float maxHp)
        {
            resourceId = id;
            resourceType = type;
            transform.position = position;
            health = hp;
            maxHealth = maxHp;
            isAlive = true;

            gameObject.name = $"{type}_{id}";

            // Aplica visual baseado no tipo
            ApplyVisualForType(type);

            UpdateHealthBar();
        }

        /// <summary>
        /// Aplica cor/material baseado no tipo de recurso
        /// </summary>
        private void ApplyVisualForType(ResourceType type)
        {
            if (meshRenderer == null) return;

            Color color = type switch
            {
                ResourceType.Tree => new Color(0.3f, 0.6f, 0.2f),      // Verde
                ResourceType.Stone => new Color(0.5f, 0.5f, 0.5f),     // Cinza
                ResourceType.MetalOre => new Color(0.7f, 0.7f, 0.8f),  // Cinza Metálico
                ResourceType.SulfurOre => new Color(0.9f, 0.9f, 0.3f), // Amarelo
                _ => Color.white
            };

            meshRenderer.material.color = color;
            _originalColor = color;
        }

        /// <summary>
        /// Atualiza saúde do recurso
        /// </summary>
        public void UpdateHealth(float newHealth, float newMaxHealth)
        {
            health = newHealth;
            maxHealth = newMaxHealth;

            UpdateHealthBar();
            UpdateVisualDamage();

            // Mostra health bar quando toma dano
            if (health < maxHealth && healthBarCanvas != null)
            {
                healthBarCanvas.SetActive(true);
            }
        }

        /// <summary>
        /// Atualiza visual da health bar
        /// </summary>
        private void UpdateHealthBar()
        {
            if (healthBarFill != null && maxHealth > 0)
            {
                float fillAmount = health / maxHealth;
                healthBarFill.fillAmount = fillAmount;

                // Muda cor baseado na vida
                if (fillAmount > 0.6f)
                    healthBarFill.color = Color.green;
                else if (fillAmount > 0.3f)
                    healthBarFill.color = Color.yellow;
                else
                    healthBarFill.color = Color.red;
            }
        }

        /// <summary>
        /// Atualiza visual do recurso baseado no dano
        /// </summary>
        private void UpdateVisualDamage()
        {
            if (meshRenderer == null) return;

            float healthPercent = health / maxHealth;

            // Troca material baseado na vida
            if (healthPercent > 0.6f)
            {
                if (normalMaterial != null)
                    meshRenderer.material = normalMaterial;
                else
                    meshRenderer.material.color = _originalColor;
            }
            else if (healthPercent > 0.3f)
            {
                if (damagedMaterial != null)
                    meshRenderer.material = damagedMaterial;
                else
                    meshRenderer.material.color = Color.Lerp(Color.red, _originalColor, healthPercent);
            }
            else
            {
                if (criticalMaterial != null)
                    meshRenderer.material = criticalMaterial;
                else
                    meshRenderer.material.color = Color.Lerp(Color.black, Color.red, healthPercent * 2);
            }
        }

        /// <summary>
        /// Efeito visual ao tomar dano
        /// </summary>
        public void ShowHitEffect()
        {
            if (_isShowingDamage) return;

            StartCoroutine(HitFlashCoroutine());
        }

        private System.Collections.IEnumerator HitFlashCoroutine()
        {
            _isShowingDamage = true;

            if (meshRenderer != null)
            {
                Color originalColor = meshRenderer.material.color;
                meshRenderer.material.color = Color.white;

                yield return new WaitForSeconds(0.1f);

                meshRenderer.material.color = originalColor;
            }

            _isShowingDamage = false;
        }

        /// <summary>
        /// Destrói o recurso (animação e efeito)
        /// </summary>
        public void DestroyResource()
        {
            isAlive = false;

            // Spawna efeito de destruição
            if (destructionEffect != null)
            {
                GameObject effect = Instantiate(destructionEffect, transform.position, Quaternion.identity);
                Destroy(effect, 2f);
            }

            // Desabilita collider para não poder mais interagir
            if (resourceCollider != null)
                resourceCollider.enabled = false;

            // Animação de destruição (escala diminui)
            StartCoroutine(DestroyAnimationCoroutine());
        }

        private System.Collections.IEnumerator DestroyAnimationCoroutine()
        {
            Vector3 originalScale = transform.localScale;
            float duration = 0.5f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);
                yield return null;
            }

            gameObject.SetActive(false);
        }

        /// <summary>
        /// Respawna o recurso
        /// </summary>
        public void RespawnResource(float hp, float maxHp)
        {
            health = hp;
            maxHealth = maxHp;
            isAlive = true;

            // Reabilita collider
            if (resourceCollider != null)
                resourceCollider.enabled = true;

            // Esconde health bar
            if (healthBarCanvas != null)
                healthBarCanvas.SetActive(false);

            // Restaura visual
            ApplyVisualForType(resourceType);
            UpdateHealthBar();

            // Animação de spawn
            gameObject.SetActive(true);
            StartCoroutine(RespawnAnimationCoroutine());
        }

        private System.Collections.IEnumerator RespawnAnimationCoroutine()
        {
            Vector3 targetScale = Vector3.one;
            transform.localScale = Vector3.zero;

            float duration = 0.5f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, t);
                yield return null;
            }

            transform.localScale = targetScale;
        }

        /// <summary>
        /// Mostra informação ao passar mouse
        /// </summary>
        private void OnMouseEnter()
        {
            if (!isAlive) return;

            // Mostra health bar
            if (healthBarCanvas != null && health < maxHealth)
            {
                healthBarCanvas.SetActive(true);
            }

            // Destaca o recurso
            if (meshRenderer != null)
            {
                meshRenderer.material.color = Color.Lerp(_originalColor, Color.white, 0.3f);
            }
        }

        private void OnMouseExit()
        {
            // Esconde health bar se estiver full
            if (healthBarCanvas != null && health >= maxHealth)
            {
                healthBarCanvas.SetActive(false);
            }

            // Restaura cor
            if (meshRenderer != null && isAlive)
            {
                meshRenderer.material.color = _originalColor;
            }
        }

        /// <summary>
        /// Para debug
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = isAlive ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position, 1f);

            // Mostra raio de interação
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 5f);
        }
    }
}