using UnityEngine;

namespace RustlikeClient.Player
{
    /// <summary>
    /// Sistema de coleta de recursos (raycast, hit detection, envio ao servidor)
    /// </summary>
    public class GatheringSystem : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Distância máxima para coletar recursos")]
        public float maxGatherDistance = 5f;
        
        [Tooltip("Dano base ao coletar")]
        public float baseDamage = 25f;
        
        [Tooltip("Intervalo entre hits (segundos)")]
        public float hitInterval = 0.5f;

        [Header("Tool Types")]
        [Tooltip("Tipo de ferramenta atual (0=Mão, 1=Machado, 2=Picareta)")]
        public int currentToolType = 0;

        [Header("UI Feedback")]
        public GameObject crosshair;
        public Color normalCrosshairColor = Color.white;
        public Color resourceCrosshairColor = Color.green;

        [Header("Audio")]
        public AudioClip hitTreeSound;
        public AudioClip hitStoneSound;
        public AudioClip hitMetalSound;

        private Camera _camera;
        private AudioSource _audioSource;
        private float _lastHitTime;
        private World.ResourceNode _currentTargetResource;

        private void Awake()
        {
            _camera = Camera.main;
            
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f; // 2D sound
        }

        private void Update()
        {
            // Verifica se inventário está aberto (não pode coletar)
            bool inventoryOpen = UI.InventoryManager.Instance != null && 
                                 UI.InventoryManager.Instance.IsInventoryOpen();
            
            if (inventoryOpen) return;

            // Raycast para detectar recursos
            DetectResource();

            // Input de coleta (botão esquerdo do mouse)
            if (Input.GetMouseButton(0)) // Segurando botão
            {
                TryGatherResource();
            }
        }

        /// <summary>
        /// Detecta recurso no crosshair
        /// </summary>
        private void DetectResource()
        {
            Ray ray = _camera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
            
            if (Physics.Raycast(ray, out RaycastHit hit, maxGatherDistance))
            {
                var resource = hit.collider.GetComponent<World.ResourceNode>();
                
                if (resource != null && resource.isAlive)
                {
                    _currentTargetResource = resource;
                    
                    // Muda cor do crosshair
                    UpdateCrosshairColor(resourceCrosshairColor);
                    
                    return;
                }
            }

            // Não encontrou recurso
            _currentTargetResource = null;
            UpdateCrosshairColor(normalCrosshairColor);
        }

        /// <summary>
        /// Tenta coletar recurso
        /// </summary>
        private void TryGatherResource()
        {
            // Verifica cooldown
            if (Time.time - _lastHitTime < hitInterval)
                return;

            // Verifica se tem recurso no target
            if (_currentTargetResource == null || !_currentTargetResource.isAlive)
                return;

            // Verifica distância
            float distance = Vector3.Distance(transform.position, _currentTargetResource.transform.position);
            if (distance > maxGatherDistance)
            {
                Debug.Log($"[GatheringSystem] Recurso muito longe ({distance:F1}m)");
                return;
            }

            // Envia hit para o servidor
            SendResourceHit(_currentTargetResource.resourceId);

            _lastHitTime = Time.time;

            // Feedback local (som)
            PlayHitSound(_currentTargetResource.resourceType);

            // Animação de hit (pode adicionar depois)
            // PlayHitAnimation();
        }

        /// <summary>
        /// Envia hit para o servidor
        /// </summary>
        private async void SendResourceHit(int resourceId)
        {
            if (Network.NetworkManager.Instance == null) return;

            var packet = new Network.ResourceHitPacket
            {
                ResourceId = resourceId,
                Damage = baseDamage,
                ToolType = currentToolType
            };

            Debug.Log($"[GatheringSystem] 🪓 Coletando recurso {resourceId} (Tool: {currentToolType}, Damage: {baseDamage})");

            await Network.NetworkManager.Instance.SendPacketAsync(
                Network.PacketType.ResourceHit,
                packet.Serialize(),
                LiteNetLib.DeliveryMethod.ReliableOrdered
            );
        }

        /// <summary>
        /// Toca som de hit
        /// </summary>
        private void PlayHitSound(World.ResourceType type)
        {
            AudioClip clip = type switch
            {
                World.ResourceType.Tree => hitTreeSound,
                World.ResourceType.Stone => hitStoneSound,
                World.ResourceType.MetalOre => hitMetalSound,
                World.ResourceType.SulfurOre => hitMetalSound,
                _ => null
            };

            if (clip != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(clip);
            }
        }

        /// <summary>
        /// Atualiza cor do crosshair
        /// </summary>
        private void UpdateCrosshairColor(Color color)
        {
            if (crosshair != null)
            {
                var image = crosshair.GetComponent<UnityEngine.UI.Image>();
                if (image != null)
                {
                    image.color = color;
                }
            }
        }

        /// <summary>
        /// Troca ferramenta (chamado por hotbar)
        /// </summary>
        public void SetToolType(int toolType)
        {
            currentToolType = Mathf.Clamp(toolType, 0, 2);
            Debug.Log($"[GatheringSystem] Ferramenta trocada: {GetToolName(currentToolType)}");
        }

        /// <summary>
        /// Pega nome da ferramenta
        /// </summary>
        private string GetToolName(int toolType)
        {
            return toolType switch
            {
                0 => "Mão",
                1 => "Machado",
                2 => "Picareta",
                _ => "Desconhecida"
            };
        }

        /// <summary>
        /// Mostra feedback de recursos coletados
        /// </summary>
        public void ShowGatherResult(int wood, int stone, int metal, int sulfur)
        {
            string message = "Coletado: ";
            bool hasItems = false;

            if (wood > 0)
            {
                message += $"{wood} Wood ";
                hasItems = true;
            }

            if (stone > 0)
            {
                message += $"{stone} Stone ";
                hasItems = true;
            }

            if (metal > 0)
            {
                message += $"{metal} Metal ";
                hasItems = true;
            }

            if (sulfur > 0)
            {
                message += $"{sulfur} Sulfur ";
                hasItems = true;
            }

            if (hasItems)
            {
                Debug.Log($"[GatheringSystem] ✅ {message}");
                
                // Mostra notificação na tela (se tiver sistema de notificações)
                if (UI.NotificationManager.Instance != null)
                {
                    UI.NotificationManager.Instance.ShowNotification(message);
                }
            }
        }

        /// <summary>
        /// Para debug
        /// </summary>
        private void OnDrawGizmos()
        {
            if (_camera == null) return;

            // Desenha raio do raycast
            Ray ray = _camera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
            Gizmos.color = _currentTargetResource != null ? Color.green : Color.red;
            Gizmos.DrawRay(ray.origin, ray.direction * maxGatherDistance);

            // Desenha esfera de alcance
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, maxGatherDistance);
        }

        private void OnGUI()
        {
            if (Input.GetKey(KeyCode.F7))
            {
                GUI.Box(new Rect(10, 690, 250, 100), "Gathering System (F7)");
                GUI.Label(new Rect(20, 715, 230, 20), $"Tool: {GetToolName(currentToolType)}");
                GUI.Label(new Rect(20, 735, 230, 20), $"Damage: {baseDamage}");
                GUI.Label(new Rect(20, 755, 230, 20), $"Target: {(_currentTargetResource != null ? _currentTargetResource.resourceType.ToString() : "None")}");
                GUI.Label(new Rect(20, 775, 230, 20), $"Cooldown: {Mathf.Max(0, hitInterval - (Time.time - _lastHitTime)):F2}s");
            }
        }
    }
}