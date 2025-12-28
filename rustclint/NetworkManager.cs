using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using LiteNetLib;

namespace RustlikeClient.Network
{
    /// <summary>
    /// ⭐ VERSÃO COMPLETA - NetworkManager com todos os sistemas:
    /// - Conexão e autenticação
    /// - Movimento e sincronização
    /// - Stats e inventário
    /// - Gathering (recursos)
    /// - Crafting
    /// - ⭐ COMBATE (melee + ranged + respawn)
    /// </summary>
    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance { get; private set; }

        [Header("Prefabs")]
        public GameObject playerPrefab;
        public GameObject otherPlayerPrefab;

        [Header("Network")]
        private ClientNetworking _networking;
        private int _myPlayerId = -1;
        private GameObject _myPlayer;
        private Dictionary<int, GameObject> _otherPlayers = new Dictionary<int, GameObject>();
        
        [Header("Movement Settings")]
        public float movementSendRate = 0.05f;
        private float _lastMovementSend;

        private Vector3 _pendingSpawnPosition;

        private void Awake()
        {
            Debug.Log("[NetworkManager] ========== AWAKE (VERSÃO COMPLETA) ==========");
            
            if (Instance != null && Instance != this)
            {
                Debug.Log("[NetworkManager] Instância duplicada detectada, destruindo...");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _networking = gameObject.AddComponent<ClientNetworking>();
            _networking.OnPacketReceived += HandlePacket;
            _networking.OnDisconnected += HandleDisconnect;
            
            Debug.Log("[NetworkManager] NetworkManager inicializado (LiteNetLib + Gathering + Crafting + Combat)");
        }

        public async void Connect(string ip, int port, string playerName)
        {
            Debug.Log($"[NetworkManager] ===== INICIANDO CONEXÃO (UDP) =====");
            Debug.Log($"[NetworkManager] IP: {ip}, Port: {port}, Nome: {playerName}");
            
            if (UI.LoadingScreen.Instance != null)
            {
                UI.LoadingScreen.Instance.Show();
                UI.LoadingScreen.Instance.SetProgress(0.1f, "Conectando ao servidor (UDP)...");
            }

            bool connected = await _networking.ConnectAsync(ip, port);
            
            if (connected)
            {
                Debug.Log("[NetworkManager] ✅ Conectado! Enviando ConnectionRequest...");
                
                if (UI.LoadingScreen.Instance != null)
                {
                    UI.LoadingScreen.Instance.SetProgress(0.3f, "Autenticando...");
                }

                var request = new ConnectionRequestPacket { PlayerName = playerName };
                
                await _networking.SendPacketAsync(
                    PacketType.ConnectionRequest, 
                    request.Serialize(),
                    DeliveryMethod.ReliableOrdered
                );
                
                Debug.Log("[NetworkManager] ConnectionRequest enviado");
            }
            else
            {
                Debug.LogError("[NetworkManager] ❌ Falha ao conectar ao servidor");
                
                if (UI.LoadingScreen.Instance != null)
                {
                    UI.LoadingScreen.Instance.Hide();
                }
            }
        }

        private void HandlePacket(Packet packet)
        {
            // Log apenas pacotes importantes (não spam de movimento/stats)
            if (packet.Type != PacketType.PlayerMovement && 
                packet.Type != PacketType.StatsUpdate && 
                packet.Type != PacketType.ResourceUpdate)
            {
                Debug.Log($"[NetworkManager] <<<< PACOTE: {packet.Type} >>>>");
            }
            
            switch (packet.Type)
            {
                case PacketType.ConnectionAccept:
                    HandleConnectionAccept(packet.Data);
                    break;

                case PacketType.PlayerSpawn:
                    HandlePlayerSpawn(packet.Data);
                    break;

                case PacketType.PlayerMovement:
                    HandlePlayerMovement(packet.Data);
                    break;

                case PacketType.PlayerDisconnect:
                    HandlePlayerDisconnect(packet.Data);
                    break;

                case PacketType.StatsUpdate:
                    HandleStatsUpdate(packet.Data);
                    break;

                case PacketType.PlayerDeath:
                    HandlePlayerDeath(packet.Data);
                    break;

                case PacketType.InventoryUpdate:
                    HandleInventoryUpdate(packet.Data);
                    break;

                // Gathering/Recursos
                case PacketType.ResourcesSync:
                    HandleResourcesSync(packet.Data);
                    break;

                case PacketType.ResourceUpdate:
                    HandleResourceUpdate(packet.Data);
                    break;

                case PacketType.ResourceDestroyed:
                    HandleResourceDestroyed(packet.Data);
                    break;

                case PacketType.ResourceRespawn:
                    HandleResourceRespawn(packet.Data);
                    break;

                case PacketType.GatherResult:
                    HandleGatherResult(packet.Data);
                    break;

                // Crafting
                case PacketType.RecipesSync:
                    HandleRecipesSync(packet.Data);
                    break;

                case PacketType.CraftStarted:
                    HandleCraftStarted(packet.Data);
                    break;

                case PacketType.CraftComplete:
                    HandleCraftComplete(packet.Data);
                    break;

                case PacketType.CraftQueueUpdate:
                    HandleCraftQueueUpdate(packet.Data);
                    break;

                // ⭐ COMBATE
                case PacketType.AttackResult:
                    HandleAttackResult(packet.Data);
                    break;

                case PacketType.TakeDamageNotify:
                    HandleTakeDamageNotify(packet.Data);
                    break;

                case PacketType.PlayerKilled:
                    HandlePlayerKilled(packet.Data);
                    break;

                case PacketType.RespawnResponse:
                    HandleRespawnResponse(packet.Data);
                    break;

                case PacketType.WeaponStateUpdate:
                    HandleWeaponStateUpdate(packet.Data);
                    break;
                    
                default:
                    Debug.LogWarning($"[NetworkManager] Tipo de pacote desconhecido: {packet.Type}");
                    break;
            }
        }

        // ==================== CONEXÃO ====================

        private void HandleConnectionAccept(byte[] data)
        {
            Debug.Log("[NetworkManager] ========== CONNECTION ACCEPT ==========");
            
            var response = ConnectionAcceptPacket.Deserialize(data);
            _myPlayerId = response.PlayerId;
            _pendingSpawnPosition = response.SpawnPosition;

            Debug.Log($"[NetworkManager] ✅ Conexão aceita!");
            Debug.Log($"[NetworkManager] Meu Player ID: {_myPlayerId}");
            Debug.Log($"[NetworkManager] Spawn Position: {_pendingSpawnPosition}");
            Debug.Log($"[NetworkManager] Ping: {_networking.GetPing()}ms");

            _otherPlayers.Clear();

            Debug.Log($"[NetworkManager] Iniciando carregamento...");
            
            if (UI.LoadingScreen.Instance != null)
            {
                UI.LoadingScreen.Instance.SetProgress(0.5f, "Carregando mundo...");
            }

            SceneManager.LoadScene("Gameplay");
            StartCoroutine(CompleteLoadingSequence());
        }

        private IEnumerator CompleteLoadingSequence()
        {
            Debug.Log("[NetworkManager] ========== INICIANDO SEQUÊNCIA DE LOADING ==========");
            
            yield return new WaitForSeconds(0.3f);

            if (UI.LoadingScreen.Instance != null)
            {
                UI.LoadingScreen.Instance.SetProgress(0.6f, "Preparando spawn...");
            }

            yield return new WaitForSeconds(0.2f);

            Debug.Log("[NetworkManager] ========== SPAWNING LOCAL PLAYER ==========");
            
            if (playerPrefab == null)
            {
                Debug.LogError("[NetworkManager] ❌ ERRO CRÍTICO: playerPrefab não está configurado!");
                yield break;
            }

            _myPlayer = Instantiate(playerPrefab, _pendingSpawnPosition, Quaternion.identity);
            _myPlayer.name = $"LocalPlayer_{_myPlayerId}";
            
            // Adiciona componentes necessários
            if (_myPlayer.GetComponent<Player.PlayerStatsClient>() == null)
            {
                _myPlayer.AddComponent<Player.PlayerStatsClient>();
            }

            if (_myPlayer.GetComponent<Player.GatheringSystem>() == null)
            {
                _myPlayer.AddComponent<Player.GatheringSystem>();
                Debug.Log("[NetworkManager] ✅ GatheringSystem adicionado ao player");
            }
            
            Debug.Log($"[NetworkManager] ✅ Player local spawned: {_myPlayer.name}");

            if (UI.LoadingScreen.Instance != null)
            {
                UI.LoadingScreen.Instance.SetProgress(0.8f, "Sincronizando jogadores...");
            }

            yield return new WaitForSeconds(0.5f);

            Debug.Log("[NetworkManager] 📢 ENVIANDO CLIENT READY PARA SERVIDOR");
            SendClientReadyAsync();

            if (UI.LoadingScreen.Instance != null)
            {
                UI.LoadingScreen.Instance.SetProgress(0.9f, "Aguardando sincronização...");
            }

            yield return new WaitForSeconds(1.0f);

            if (UI.LoadingScreen.Instance != null)
            {
                UI.LoadingScreen.Instance.SetProgress(1f, "Pronto!");
                yield return new WaitForSeconds(0.3f);
                UI.LoadingScreen.Instance.Hide();
            }

            if (UI.StatsUI.Instance != null)
            {
                UI.StatsUI.Instance.Show();
            }

            Debug.Log($"[NetworkManager] ========== LOADING COMPLETO ==========");

            StartCoroutine(SendHeartbeat());
        }

        // ==================== MOVIMENTO ====================

        private void HandlePlayerSpawn(byte[] data)
        {
            var spawn = PlayerSpawnPacket.Deserialize(data);
            
            Debug.Log($"[NetworkManager] Player Spawn: {spawn.PlayerName} (ID: {spawn.PlayerId})");
            
            if (spawn.PlayerId == _myPlayerId)
            {
                Debug.Log($"[NetworkManager] ⏭️ Ignorando spawn do próprio player");
                return;
            }

            SpawnOtherPlayer(spawn);
        }

        private void SpawnOtherPlayer(PlayerSpawnPacket spawn)
        {
            Debug.Log($"[NetworkManager] Spawning other player: {spawn.PlayerName} (ID: {spawn.PlayerId})");

            if (_otherPlayers.ContainsKey(spawn.PlayerId))
            {
                Debug.LogWarning($"[NetworkManager] ⚠️ Jogador {spawn.PlayerId} JÁ EXISTE!");
                return;
            }

            if (otherPlayerPrefab == null)
            {
                Debug.LogError($"[NetworkManager] ❌ ERRO: otherPlayerPrefab é NULL!");
                return;
            }

            GameObject otherPlayer = Instantiate(otherPlayerPrefab, spawn.Position, Quaternion.identity);
            otherPlayer.name = $"Player_{spawn.PlayerId}_{spawn.PlayerName}";
            
            _otherPlayers[spawn.PlayerId] = otherPlayer;

            Debug.Log($"[NetworkManager] ✅ Jogador spawned: {otherPlayer.name}");
        }

        private void HandlePlayerMovement(byte[] data)
        {
            var movement = PlayerMovementPacket.Deserialize(data);
            
            if (movement.PlayerId == _myPlayerId) return;

            if (_otherPlayers.TryGetValue(movement.PlayerId, out GameObject otherPlayer))
            {
                var networkSync = otherPlayer.GetComponent<NetworkPlayerSync>();
                if (networkSync == null)
                {
                    networkSync = otherPlayer.AddComponent<NetworkPlayerSync>();
                }
                
                networkSync.UpdateTargetTransform(movement.Position, movement.Rotation.x);
            }
        }

        private void HandlePlayerDisconnect(byte[] data)
        {
            int playerId = System.BitConverter.ToInt32(data, 0);
            
            Debug.Log($"[NetworkManager] Player Disconnect: ID {playerId}");

            if (_otherPlayers.TryGetValue(playerId, out GameObject player))
            {
                Debug.Log($"[NetworkManager] Destruindo player {player.name}");
                Destroy(player);
                _otherPlayers.Remove(playerId);
            }
        }

        // ==================== STATS ====================

        private void HandleStatsUpdate(byte[] data)
        {
            var stats = StatsUpdatePacket.Deserialize(data);
            
            if (stats.PlayerId != _myPlayerId) return;

            if (_myPlayer != null)
            {
                var playerStats = _myPlayer.GetComponent<Player.PlayerStatsClient>();
                if (playerStats != null)
                {
                    playerStats.UpdateStats(stats.Health, stats.Hunger, stats.Thirst, stats.Temperature);
                }
            }

            if (UI.StatsUI.Instance != null)
            {
                UI.StatsUI.Instance.UpdateStats(stats.Health, stats.Hunger, stats.Thirst, stats.Temperature);
            }
        }

        private void HandlePlayerDeath(byte[] data)
        {
            var death = PlayerDeathPacket.Deserialize(data);
            
            Debug.Log($"[NetworkManager] ========== PLAYER DEATH ==========");
            Debug.Log($"[NetworkManager] Player ID: {death.PlayerId}");

            if (death.PlayerId == _myPlayerId)
            {
                Debug.LogWarning("[NetworkManager] 💀 VOCÊ MORREU!");
                HandleMyDeath();
            }
            else
            {
                if (_otherPlayers.TryGetValue(death.PlayerId, out GameObject player))
                {
                    Debug.Log($"[NetworkManager] Jogador {player.name} morreu");
                }
            }
        }

        private void HandleMyDeath()
        {
            if (_myPlayer != null)
            {
                var controller = _myPlayer.GetComponent<Player.PlayerController>();
                if (controller != null)
                {
                    controller.enabled = false;
                }
            }

            Debug.Log("[NetworkManager] Mostrando tela de morte...");
            // TODO: Mostrar tela de morte se houver
        }

        // ==================== INVENTÁRIO ====================

        private void HandleInventoryUpdate(byte[] data)
        {
            Debug.Log("[NetworkManager] ========== INVENTORY UPDATE ==========");
            
            var inventoryPacket = InventoryUpdatePacket.Deserialize(data);
            Debug.Log($"[NetworkManager] Recebido inventário com {inventoryPacket.Slots.Count} itens");

            if (UI.InventoryManager.Instance != null)
            {
                UI.InventoryManager.Instance.UpdateInventory(inventoryPacket);
            }
            else
            {
                Debug.LogError("[NetworkManager] InventoryManager não encontrado!");
            }
        }

        // ==================== GATHERING ====================

        private void HandleResourcesSync(byte[] data)
        {
            Debug.Log("[NetworkManager] ========== RESOURCES SYNC ==========");
            
            var packet = ResourcesSyncPacket.Deserialize(data);
            Debug.Log($"[NetworkManager] Recebido {packet.Resources.Count} recursos do servidor");

            if (World.ResourceManager.Instance != null)
            {
                World.ResourceManager.Instance.SpawnResources(packet.Resources);
            }
            else
            {
                Debug.LogError("[NetworkManager] ResourceManager não encontrado! Criando...");
                
                GameObject rmObj = new GameObject("ResourceManager");
                DontDestroyOnLoad(rmObj);
                rmObj.AddComponent<World.ResourceManager>();
                
                World.ResourceManager.Instance?.SpawnResources(packet.Resources);
            }
        }

        private void HandleResourceUpdate(byte[] data)
        {
            var packet = ResourceUpdatePacket.Deserialize(data);

            if (World.ResourceManager.Instance != null)
            {
                World.ResourceManager.Instance.UpdateResourceHealth(packet.ResourceId, packet.Health, packet.MaxHealth);
            }
        }

        private void HandleResourceDestroyed(byte[] data)
        {
            var packet = ResourceDestroyedPacket.Deserialize(data);
            
            Debug.Log($"[NetworkManager] 💥 Recurso {packet.ResourceId} foi destruído");

            if (World.ResourceManager.Instance != null)
            {
                World.ResourceManager.Instance.DestroyResource(packet.ResourceId);
            }
        }

        private void HandleResourceRespawn(byte[] data)
        {
            var packet = ResourceRespawnPacket.Deserialize(data);
            
            Debug.Log($"[NetworkManager] ♻️ Recurso {packet.ResourceId} respawnou");

            if (World.ResourceManager.Instance != null)
            {
                World.ResourceManager.Instance.RespawnResource(packet.ResourceId, packet.Health, packet.MaxHealth);
            }
        }

        private void HandleGatherResult(byte[] data)
        {
            var packet = GatherResultPacket.Deserialize(data);
            
            Debug.Log($"[NetworkManager] ✅ Recursos coletados: Wood={packet.WoodGained}, Stone={packet.StoneGained}, Metal={packet.MetalGained}, Sulfur={packet.SulfurGained}");

            if (_myPlayer != null)
            {
                var gatheringSystem = _myPlayer.GetComponent<Player.GatheringSystem>();
                if (gatheringSystem != null)
                {
                    gatheringSystem.ShowGatherResult(
                        packet.WoodGained,
                        packet.StoneGained,
                        packet.MetalGained,
                        packet.SulfurGained
                    );
                }
            }
        }

        // ==================== CRAFTING ====================

        private void HandleRecipesSync(byte[] data)
        {
            Debug.Log("[NetworkManager] ========== RECIPES SYNC ==========");
            
            var packet = RecipesSyncPacket.Deserialize(data);
            Debug.Log($"[NetworkManager] Recebido {packet.Recipes.Count} receitas do servidor");

            var recipes = new List<Crafting.CraftingRecipeData>();

            foreach (var recipeData in packet.Recipes)
            {
                var recipe = new Crafting.CraftingRecipeData
                {
                    id = recipeData.Id,
                    recipeName = recipeData.Name,
                    resultItemId = recipeData.ResultItemId,
                    resultQuantity = recipeData.ResultQuantity,
                    craftingTime = recipeData.CraftingTime,
                    requiredWorkbench = recipeData.RequiredWorkbench
                };

                foreach (var ingredient in recipeData.Ingredients)
                {
                    recipe.ingredients.Add(new Crafting.IngredientData
                    {
                        itemId = ingredient.ItemId,
                        quantity = ingredient.Quantity
                    });
                }

                recipes.Add(recipe);
            }

            if (Crafting.CraftingManager.Instance != null)
            {
                Crafting.CraftingManager.Instance.LoadRecipes(recipes);
                Debug.Log($"[NetworkManager] ✅ {recipes.Count} receitas carregadas no CraftingManager");
            }
            else
            {
                Debug.LogError("[NetworkManager] CraftingManager não encontrado!");
            }
        }

        private void HandleCraftStarted(byte[] data)
        {
            var packet = CraftStartedPacket.Deserialize(data);
            
            Debug.Log($"[NetworkManager] Crafting iniciado: Recipe {packet.RecipeId} ({packet.Duration}s) - {(packet.Success ? "SUCCESS" : "FAILED")}");

            if (Crafting.CraftingManager.Instance != null)
            {
                Crafting.CraftingManager.Instance.OnCraftStartedResponse(
                    packet.RecipeId,
                    packet.Duration,
                    packet.Success,
                    packet.Message
                );
            }
        }

        private void HandleCraftComplete(byte[] data)
        {
            var packet = CraftCompletePacket.Deserialize(data);
            
            Debug.Log($"[NetworkManager] ✅ Crafting completo! Recipe {packet.RecipeId} -> {packet.ResultQuantity}x Item {packet.ResultItemId}");

            if (Crafting.CraftingManager.Instance != null)
            {
                Crafting.CraftingManager.Instance.OnCraftCompleted(
                    packet.RecipeId,
                    packet.ResultItemId,
                    packet.ResultQuantity
                );
            }
        }

        private void HandleCraftQueueUpdate(byte[] data)
        {
            var packet = CraftQueueUpdatePacket.Deserialize(data);

            var queueItems = new List<Crafting.CraftQueueItemData>();

            foreach (var item in packet.QueueItems)
            {
                queueItems.Add(new Crafting.CraftQueueItemData
                {
                    recipeId = item.RecipeId,
                    progress = item.Progress,
                    remainingTime = item.RemainingTime
                });
            }

            if (Crafting.CraftingManager.Instance != null)
            {
                Crafting.CraftingManager.Instance.UpdateQueue(queueItems);
            }
        }

        // ==================== ⭐ COMBATE ====================

        /// <summary>
        /// Handle de resultado de ataque
        /// </summary>
        private void HandleAttackResult(byte[] data)
        {
            var packet = AttackResultPacket.Deserialize(data);
            
            Debug.Log($"[NetworkManager] ⚔️ Attack Result: {(packet.Success ? "HIT" : "MISS")} - {packet.Message}");

            if (packet.Success)
            {
                Debug.Log($"  → Damage: {packet.DamageDealt:F1}");
                Debug.Log($"  → Killed: {packet.WasKilled}");
                Debug.Log($"  → Hitbox: {packet.Hitbox}");

                // Mostra hitmarker
                if (UI.CombatUI.Instance != null)
                {
                    bool isHeadshot = packet.Hitbox == 1; // Head = 1
                    UI.CombatUI.Instance.ShowHitmarker(isHeadshot);
                }
            }

            // Atualiza munição se for ranged
            if (packet.RemainingAmmo >= 0)
            {
                Debug.Log($"  → Remaining Ammo: {packet.RemainingAmmo}");
                // TODO: Atualizar UI de munição
            }
        }

        /// <summary>
        /// Handle quando VOCÊ toma dano
        /// </summary>
        private void HandleTakeDamageNotify(byte[] data)
        {
            var packet = TakeDamageNotifyPacket.Deserialize(data);
            
            Debug.LogWarning($"[NetworkManager] 💥 VOCÊ TOMOU DANO!");
            Debug.LogWarning($"  → Attacker ID: {packet.AttackerId}");
            Debug.LogWarning($"  → Damage: {packet.Damage:F1}");
            Debug.LogWarning($"  → Type: {packet.DamageType}");
            Debug.LogWarning($"  → Hitbox: {packet.Hitbox}");

            // Efeito visual de dano
            if (UI.StatsUI.Instance != null)
            {
                float intensity = Mathf.Clamp01(packet.Damage / 100f);
                UI.StatsUI.Instance.ShowDamageEffect(intensity);
            }

            // Som de dano
            // TODO: Tocar som de impacto

            // Número de dano flutuante
            if (UI.CombatUI.Instance != null && _myPlayer != null)
            {
                bool isCritical = packet.Hitbox == 1; // Head = 1
                UI.CombatUI.Instance.ShowDamageNumber(
                    _myPlayer.transform.position + Vector3.up * 2f,
                    packet.Damage,
                    isCritical
                );
            }
        }

        /// <summary>
        /// Handle quando alguém é morto (kill feed)
        /// </summary>
        private void HandlePlayerKilled(byte[] data)
        {
            var packet = PlayerKilledPacket.Deserialize(data);
            
            Debug.Log($"[NetworkManager] ☠️ KILL FEED:");
            Debug.Log($"  {packet.KillerName} [{packet.WeaponUsed}] {packet.VictimId}");

            bool isHeadshot = packet.Hitbox == 1;

            // Atualiza kill feed na UI
            if (UI.CombatUI.Instance != null)
            {
                string victimName = GetPlayerName(packet.VictimId);
                UI.CombatUI.Instance.AddKillFeedEntry(
                    packet.KillerName,
                    victimName,
                    packet.WeaponUsed,
                    isHeadshot
                );
            }

            // Se você foi morto
            if (packet.VictimId == _myPlayerId)
            {
                Debug.LogError("[NetworkManager] 💀 VOCÊ FOI MORTO!");
                ShowDeathScreen(packet.KillerName, packet.WeaponUsed, packet.Distance, isHeadshot);
            }
        }

        /// <summary>
        /// Mostra tela de morte
        /// </summary>
        private void ShowDeathScreen(string killerName, string weaponUsed, float distance, bool wasHeadshot)
        {
            // Desabilita controles
            if (_myPlayer != null)
            {
                var controller = _myPlayer.GetComponent<Player.PlayerController>();
                if (controller != null)
                {
                    controller.enabled = false;
                }
            }

            // Mostra tela de morte
            var deathScreen = FindObjectOfType<Combat.UI.DeathScreen>();
            if (deathScreen != null)
            {
                deathScreen.ShowDeathScreen(killerName, weaponUsed, distance, wasHeadshot);
            }
        }

        /// <summary>
        /// Handle de resposta de respawn
        /// </summary>
        private void HandleRespawnResponse(byte[] data)
        {
            var packet = RespawnResponsePacket.Deserialize(data);
            
            Debug.Log($"[NetworkManager] Respawn Response: {(packet.Success ? "SUCCESS" : "FAILED")}");

            if (packet.Success)
            {
                Debug.Log($"  → Spawn Position: ({packet.SpawnX}, {packet.SpawnY}, {packet.SpawnZ})");

                // Move player
                if (_myPlayer != null)
                {
                    _myPlayer.transform.position = new Vector3(packet.SpawnX, packet.SpawnY, packet.SpawnZ);

                    // Reabilita controles
                    var controller = _myPlayer.GetComponent<Player.PlayerController>();
                    if (controller != null)
                    {
                        controller.enabled = true;
                    }
                }

                // Esconde tela de morte
                var deathScreen = FindObjectOfType<Combat.UI.DeathScreen>();
                if (deathScreen != null)
                {
                    deathScreen.HideDeathScreen();
                }

                // Mostra UI de stats
                if (UI.StatsUI.Instance != null)
                {
                    UI.StatsUI.Instance.Show();
                }
            }
            else
            {
                Debug.LogError($"[NetworkManager] Respawn falhou: {packet.Message}");
            }
        }

        /// <summary>
        /// Handle de atualização de estado da arma
        /// </summary>
        private void HandleWeaponStateUpdate(byte[] data)
        {
            var packet = WeaponStateUpdatePacket.Deserialize(data);
            
            Debug.Log($"[NetworkManager] Weapon State Update:");
            Debug.Log($"  → Weapon: {packet.WeaponItemId}");
            Debug.Log($"  → Ammo: {packet.CurrentAmmo}/{packet.ReserveAmmo}");
            Debug.Log($"  → Reloading: {packet.IsReloading}");

            // TODO: Atualizar WeaponManager se existir
        }

        // ==================== SEND METHODS (COMBATE) ====================

        /// <summary>
        /// Envia ataque melee para servidor
        /// </summary>
        public async void SendMeleeAttack(int targetPlayerId, int weaponItemId, byte hitbox, Vector3 direction)
        {
            var packet = new MeleeAttackPacket
            {
                TargetPlayerId = targetPlayerId,
                WeaponItemId = weaponItemId,
                Hitbox = hitbox,
                DirectionX = direction.x,
                DirectionY = direction.y,
                DirectionZ = direction.z
            };

            await _networking.SendPacketAsync(
                PacketType.MeleeAttack,
                packet.Serialize(),
                DeliveryMethod.ReliableOrdered
            );

            Debug.Log($"[NetworkManager] 📤 Enviado: MeleeAttack → Player {targetPlayerId}");
        }

        /// <summary>
        /// Envia ataque ranged para servidor
        /// </summary>
        public async void SendRangedAttack(int targetPlayerId, int weaponItemId, byte hitbox, Vector3 shootDirection, float distance)
        {
            var packet = new RangedAttackPacket
            {
                TargetPlayerId = targetPlayerId,
                WeaponItemId = weaponItemId,
                Hitbox = hitbox,
                ShootDirectionX = shootDirection.x,
                ShootDirectionY = shootDirection.y,
                ShootDirectionZ = shootDirection.z,
                Distance = distance
            };

            await _networking.SendPacketAsync(
                PacketType.RangedAttack,
                packet.Serialize(),
                DeliveryMethod.ReliableOrdered
            );

            Debug.Log($"[NetworkManager] 📤 Enviado: RangedAttack → Player {targetPlayerId}");
        }

        /// <summary>
        /// Envia equipar arma para servidor
        /// </summary>
        public async void SendWeaponEquip(int weaponItemId, int slotIndex)
        {
            var packet = new WeaponEquipPacket
            {
                WeaponItemId = weaponItemId,
                SlotIndex = slotIndex
            };

            await _networking.SendPacketAsync(
                PacketType.WeaponEquip,
                packet.Serialize(),
                DeliveryMethod.ReliableOrdered
            );

            Debug.Log($"[NetworkManager] 📤 Enviado: WeaponEquip → Weapon {weaponItemId}");
        }

        /// <summary>
        /// Envia reload para servidor
        /// </summary>
        public async void SendWeaponReload(int weaponItemId)
        {
            var packet = new WeaponReloadPacket
            {
                WeaponItemId = weaponItemId
            };

            await _networking.SendPacketAsync(
                PacketType.WeaponReload,
                packet.Serialize(),
                DeliveryMethod.ReliableOrdered
            );

            Debug.Log($"[NetworkManager] 📤 Enviado: WeaponReload");
        }

        /// <summary>
        /// Envia pedido de respawn para servidor
        /// </summary>
        public async void SendRespawnRequest()
        {
            await _networking.SendPacketAsync(
                PacketType.RespawnRequest,
                new byte[0],
                DeliveryMethod.ReliableOrdered
            );

            Debug.Log($"[NetworkManager] 📤 Enviado: RespawnRequest");
        }

        // ==================== OUTROS SEND METHODS ====================

        private async void SendClientReadyAsync()
        {
            await _networking.SendPacketAsync(
                PacketType.ClientReady, 
                new byte[0],
                DeliveryMethod.ReliableOrdered
            );
        }

        public void SendPlayerMovement(Vector3 position, Vector2 rotation)
        {
            if (Time.time - _lastMovementSend < movementSendRate) return;
            if (!_networking.IsConnected()) return;

            _lastMovementSend = Time.time;

            var movement = new PlayerMovementPacket
            {
                PlayerId = _myPlayerId,
                Position = position,
                Rotation = rotation
            };

            _networking.SendPacket(
                PacketType.PlayerMovement, 
                movement.Serialize(),
                DeliveryMethod.Sequenced
            );
        }

        private IEnumerator SendHeartbeat()
        {
            while (_networking.IsConnected())
            {
                SendHeartbeatAsync();
                yield return new WaitForSeconds(5f);
            }
        }

        private async void SendHeartbeatAsync()
        {
            await _networking.SendPacketAsync(
                PacketType.Heartbeat, 
                new byte[0],
                DeliveryMethod.Unreliable
            );
        }

        // ==================== DISCONNECT ====================

        private void HandleDisconnect()
        {
            Debug.LogWarning("[NetworkManager] ========== DESCONECTADO DO SERVIDOR ==========");
            
            foreach (var player in _otherPlayers.Values)
            {
                if (player != null) Destroy(player);
            }
            _otherPlayers.Clear();

            if (_myPlayer != null) Destroy(_myPlayer);

            if (World.ResourceManager.Instance != null)
            {
                World.ResourceManager.Instance.ClearAllResources();
            }

            if (UI.LoadingScreen.Instance != null)
            {
                UI.LoadingScreen.Instance.Hide();
            }

            if (UI.StatsUI.Instance != null)
            {
                UI.StatsUI.Instance.Hide();
            }

            SceneManager.LoadScene("MainMenu");
        }

        // ==================== HELPERS ====================

        /// <summary>
        /// Pega nome de um player pelo ID
        /// </summary>
        private string GetPlayerName(int playerId)
        {
            if (playerId == _myPlayerId)
            {
                return "You";
            }

            if (_otherPlayers.TryGetValue(playerId, out GameObject player))
            {
                return player.name;
            }

            return $"Player {playerId}";
        }

        // ==================== GETTERS ====================

        public int GetMyPlayerId() => _myPlayerId;
        public bool IsConnected() => _networking.IsConnected();
        public int GetOtherPlayersCount() => _otherPlayers.Count;
        public int GetPing() => _networking.GetPing();

        public async System.Threading.Tasks.Task SendPacketAsync(PacketType type, byte[] data, DeliveryMethod method = DeliveryMethod.ReliableOrdered)
        {
            await _networking.SendPacketAsync(type, data, method);
        }

        // ==================== DEBUG ====================

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                Debug.Log("========================================");
                Debug.Log("========== NETWORK STATUS (COMPLETO) ==========");
                Debug.Log($"My Player ID: {_myPlayerId}");
                Debug.Log($"Connected: {IsConnected()}");
                Debug.Log($"Ping: {GetPing()}ms");
                Debug.Log($"Other Players: {_otherPlayers.Count}");
                
                if (World.ResourceManager.Instance != null)
                {
                    int totalResources = World.ResourceManager.Instance.CountResourcesByType(World.ResourceType.Tree) +
                                       World.ResourceManager.Instance.CountResourcesByType(World.ResourceType.Stone) +
                                       World.ResourceManager.Instance.CountResourcesByType(World.ResourceType.MetalOre) +
                                       World.ResourceManager.Instance.CountResourcesByType(World.ResourceType.SulfurOre);
                    Debug.Log($"Resources Loaded: {totalResources}");
                }
                
                Debug.Log("========================================");
            }
        }
    }
}
