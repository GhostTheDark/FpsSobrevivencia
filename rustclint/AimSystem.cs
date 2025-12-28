using UnityEngine;
using System.Collections.Generic;

namespace RustlikeGame.Combat
{
    /// <summary>
    /// Sistema de mira e detecção de alvos
    /// Usa Raycast para detectar onde o jogador está mirando
    /// </summary>
    public class AimSystem : MonoBehaviour
    {
        [Header("Camera")]
        [SerializeField] private Camera playerCamera;
        
        [Header("Raycast Settings")]
        [SerializeField] private float maxDistance = 100f;
        [SerializeField] private LayerMask targetLayers;
        [SerializeField] private LayerMask obstacleLayer;
        
        [Header("Crosshair")]
        [SerializeField] private RectTransform crosshair;
        [SerializeField] private float crosshairExpandSpeed = 5f;
        [SerializeField] private float crosshairNormalSize = 10f;
        [SerializeField] private float crosshairExpandedSize = 20f;

        // Current aim data
        private RaycastHit _currentHit;
        private bool _isAiming;
        private float _currentCrosshairSize;
        private Vector3 _aimPoint;
        private GameObject _currentTarget;

        // Events
        public System.Action<GameObject> OnTargetChanged;

        private void Start()
        {
            if (playerCamera == null)
                playerCamera = Camera.main;

            _currentCrosshairSize = crosshairNormalSize;
        }

        private void Update()
        {
            PerformAimRaycast();
            UpdateCrosshair();
        }

        /// <summary>
        /// Realiza raycast da câmera para detectar alvos
        /// </summary>
        private void PerformAimRaycast()
        {
            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            
            if (Physics.Raycast(ray, out _currentHit, maxDistance, targetLayers))
            {
                _isAiming = true;
                _aimPoint = _currentHit.point;

                // Verifica se mudou de alvo
                GameObject newTarget = _currentHit.collider.gameObject;
                if (newTarget != _currentTarget)
                {
                    _currentTarget = newTarget;
                    OnTargetChanged?.Invoke(_currentTarget);
                }
            }
            else
            {
                _isAiming = false;
                _aimPoint = ray.GetPoint(maxDistance);
                
                if (_currentTarget != null)
                {
                    _currentTarget = null;
                    OnTargetChanged?.Invoke(null);
                }
            }
        }

        /// <summary>
        /// Atualiza visual da crosshair
        /// </summary>
        private void UpdateCrosshair()
        {
            if (crosshair == null) return;

            // Expande crosshair quando mira em alvo
            float targetSize = _isAiming && IsTargetEnemy() ? crosshairExpandedSize : crosshairNormalSize;
            _currentCrosshairSize = Mathf.Lerp(_currentCrosshairSize, targetSize, Time.deltaTime * crosshairExpandSpeed);
            
            crosshair.sizeDelta = new Vector2(_currentCrosshairSize, _currentCrosshairSize);

            // Muda cor se mirando em inimigo
            if (_isAiming && IsTargetEnemy())
            {
                // TODO: Implementar mudança de cor da crosshair
            }
        }

        /// <summary>
        /// Verifica se o alvo atual é um inimigo
        /// </summary>
        private bool IsTargetEnemy()
        {
            if (_currentTarget == null) return false;

            // Verifica se tem componente de player inimigo
            var otherPlayer = _currentTarget.GetComponentInParent<NetworkPlayer>();
            return otherPlayer != null;
        }

        /// <summary>
        /// Verifica se há linha de visão clara para um ponto
        /// </summary>
        public bool HasLineOfSight(Vector3 target)
        {
            Vector3 direction = target - playerCamera.transform.position;
            float distance = direction.magnitude;

            if (Physics.Raycast(playerCamera.transform.position, direction, distance, obstacleLayer))
            {
                return false; // Há obstáculo no caminho
            }

            return true;
        }

        /// <summary>
        /// Pega o alvo atual sob a mira
        /// </summary>
        public GameObject GetCurrentTarget()
        {
            return _currentTarget;
        }

        /// <summary>
        /// Pega o ponto de impacto do raycast
        /// </summary>
        public Vector3 GetAimPoint()
        {
            return _aimPoint;
        }

        /// <summary>
        /// Pega a direção da mira
        /// </summary>
        public Vector3 GetAimDirection()
        {
            return (playerCamera.transform.forward).normalized;
        }

        /// <summary>
        /// Verifica se está mirando em algo
        /// </summary>
        public bool IsAiming()
        {
            return _isAiming;
        }

        /// <summary>
        /// Pega o RaycastHit atual
        /// </summary>
        public RaycastHit GetCurrentHit()
        {
            return _currentHit;
        }

        /// <summary>
        /// Pega distância até o alvo
        /// </summary>
        public float GetDistanceToTarget()
        {
            if (!_isAiming) return maxDistance;
            return Vector3.Distance(playerCamera.transform.position, _aimPoint);
        }

        /// <summary>
        /// Detecta qual hitbox foi atingida
        /// </summary>
        public HitboxType DetectHitbox(RaycastHit hit)
        {
            // Procura por componente Hitbox no collider
            var hitbox = hit.collider.GetComponent<Hitbox>();
            if (hitbox != null)
            {
                return hitbox.Type;
            }

            // Fallback: detecta por posição (simplificado)
            var target = hit.collider.GetComponentInParent<NetworkPlayer>();
            if (target != null)
            {
                float hitHeight = hit.point.y - target.transform.position.y;
                
                if (hitHeight > 1.5f) return HitboxType.Head;
                if (hitHeight > 0.5f) return HitboxType.Body;
                return HitboxType.Legs;
            }

            return HitboxType.Body;
        }

        // Debug
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            Gizmos.color = Color.red;
            Gizmos.DrawLine(playerCamera.transform.position, _aimPoint);
            
            if (_isAiming)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(_aimPoint, 0.1f);
            }
        }
    }
}
