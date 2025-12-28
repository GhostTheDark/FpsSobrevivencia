using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiteNetLib;
using LiteNetLib.Utils;
using RustlikeServer.Network;
using RustlikeServer.World;
using RustlikeServer.Crafting;

namespace RustlikeServer.Core
{
    /// <summary>
    /// ⭐ ATUALIZADO COM SISTEMA DE CRAFTING - Servidor autoritativo UDP
    /// </summary>
    public class GameServer : INetEventListener
    {
        private NetManager _netManager;
        private Dictionary<int, Player> _players;
        private Dictionary<NetPeer, ClientHandler> _clients;
        private Dictionary<int, NetPeer> _playerPeers;
        private int _nextPlayerId;
        private bool _isRunning;
        private readonly int _port;
        private readonly object _playersLock = new object();

        // Resource Manager
        private ResourceManager _resourceManager;

        // ⭐ NOVO: Crafting Manager
        private CraftingManager _craftingManager;

        // Stats update
        private const float STATS_UPDATE_RATE = 1f;
        private const float STATS_SYNC_RATE = 2f;
        
        // Resource update
        private const float RESOURCE_UPDATE_RATE = 10f;

        // ⭐ NOVO: Crafting update
        private const float CRAFTING_UPDATE_RATE = 0.5f; // Verifica craftings 2x por segundo

		private Combat.CombatManager _combatManager;
		
        private NetDataWriter _reusableWriter;

        public GameServer(int port = 7777)
        {
            _port = port;
            _players = new Dictionary<int, Player>();
            _clients = new Dictionary<NetPeer, ClientHandler>();
            _playerPeers = new Dictionary<int, NetPeer>();
            _nextPlayerId = 1;
            _isRunning = false;
            _reusableWriter = new NetDataWriter();
            
            // Inicializa Resource Manager
            _resourceManager = new ResourceManager();

            // ⭐ NOVO: Inicializa Crafting Manager
            _craftingManager = new CraftingManager();
			
			_combatManager = new Combat.CombatManager();
        }

        public async Task StartAsync()
        {
            try
            {
                _netManager = new NetManager(this)
                {
                    AutoRecycle = true,
                    UpdateTime = 15,
                    DisconnectTimeout = 10000,
                    PingInterval = 1000,
                    UnconnectedMessagesEnabled = false
                };

                _netManager.Start(_port);
                _isRunning = true;

                Console.WriteLine($"╔════════════════════════════════════════════════╗");
                Console.WriteLine($"║  SERVIDOR RUST-LIKE (LiteNetLib/UDP)           ║");
                Console.WriteLine($"║  Porta: {_port}                                    ║");
                Console.WriteLine($"║  Sistema de Sobrevivência: ATIVO               ║");
                Console.WriteLine($"║  Sistema de Gathering: ATIVO 🪓🪨             ║");
                Console.WriteLine($"║  Sistema de Crafting: ATIVO 🔨                ║");
				Console.WriteLine($"║  Sistema de Combate: ATIVO ⚔️             ║");
                Console.WriteLine($"║  Aguardando conexões...                        ║");
                Console.WriteLine($"╚════════════════════════════════════════════════╝");
                Console.WriteLine();

                // Inicializa recursos do mundo
                _resourceManager.Initialize();

                // ⭐ NOVO: Inicializa crafting
                _craftingManager.Initialize();

                Task updateTask = UpdateLoopAsync();
                Task statsTask = UpdateStatsLoopAsync();
                Task monitorTask = MonitorPlayersAsync();
                Task resourceTask = UpdateResourcesLoopAsync();
                Task craftingTask = UpdateCraftingLoopAsync(); // ⭐ NOVO
				Task combatTask = UpdateCombatLoopAsync();
                await Task.WhenAll(updateTask, statsTask, monitorTask, resourceTask, craftingTask, combatTask);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameServer] Erro fatal: {ex.Message}");
            }
        }

        private async Task UpdateLoopAsync()
        {
            while (_isRunning)
            {
                _netManager.PollEvents();
                await Task.Delay(15);
            }
        }

        // ==================== LITENETLIB CALLBACKS ====================

        public void OnPeerConnected(NetPeer peer)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n[GameServer] 🔗 Cliente conectado: {peer.Address}:{peer.Port}");
            Console.WriteLine($"[GameServer] Peer ID: {peer.Id} | Ping: {peer.Ping}ms");
            Console.ResetColor();

            var handler = new ClientHandler(peer, this);
            _clients[peer] = handler;
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[GameServer] ❌ Cliente desconectado: {peer.Address}:{peer.Port}");
            Console.WriteLine($"[GameServer] Razão: {disconnectInfo.Reason}");
            Console.ResetColor();

            if (_clients.TryGetValue(peer, out var handler))
            {
                handler.Disconnect();
                _clients.Remove(peer);
            }
        }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
        {
            if (_clients.TryGetValue(peer, out var handler))
            {
                byte[] data = reader.GetRemainingBytes();
                _ = handler.ProcessPacketAsync(data);
            }

            reader.Recycle();
        }

        public void OnNetworkError(System.Net.IPEndPoint endPoint, System.Net.Sockets.SocketError socketError)
        {
            Console.WriteLine($"[GameServer] Erro de rede: {socketError} em {endPoint}");
        }

        public void OnNetworkReceiveUnconnected(System.Net.IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
        }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
        }

        public void OnConnectionRequest(ConnectionRequest request)
        {
            request.Accept();
        }

        // ==================== MÉTODOS PÚBLICOS ====================

        public Player CreatePlayer(string name)
        {
            int id = _nextPlayerId++;
            Player player = new Player(id, name);
            
            lock (_playersLock)
            {
                _players[id] = player;
            }
            
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n✅ [GameServer] NOVO PLAYER CRIADO:");
            Console.WriteLine($"   → Nome: {name}");
            Console.WriteLine($"   → ID: {id}");
            Console.WriteLine($"   → Stats iniciais: {player.Stats}");
            Console.WriteLine($"   → Total de jogadores: {_players.Count}");
            Console.ResetColor();
            
            return player;
        }

        public void RemovePlayer(int playerId)
        {
            string playerName = "";
            bool removed = false;
            NetPeer peerToRemove = null;

            lock (_playersLock)
            {
                if (_players.ContainsKey(playerId))
                {
                    playerName = _players[playerId].Name;
                    _players.Remove(playerId);
                    removed = true;
                }

                if (_playerPeers.TryGetValue(playerId, out peerToRemove))
                {
                    _playerPeers.Remove(playerId);
                }
            }

            if (removed)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n❌ [GameServer] PLAYER REMOVIDO:");
                Console.WriteLine($"   → Nome: {playerName}");
                Console.WriteLine($"   → ID: {playerId}");
                Console.WriteLine($"   → Jogadores restantes: {_players.Count}");
                Console.ResetColor();
                
                if (peerToRemove != null && _clients.ContainsKey(peerToRemove))
                {
                    _clients[peerToRemove].Disconnect();
                    _clients.Remove(peerToRemove);
                }

                BroadcastPlayerDisconnect(playerId);
            }
        }

        public void RegisterClient(int playerId, NetPeer peer, ClientHandler handler)
        {
            _playerPeers[playerId] = peer;
            _clients[peer] = handler;
            
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[GameServer] ClientHandler registrado: Player ID {playerId} | Total: {_clients.Count}");
            Console.ResetColor();
        }

        public void SendPacket(NetPeer peer, PacketType type, byte[] data, DeliveryMethod method = DeliveryMethod.ReliableOrdered)
        {
            _reusableWriter.Reset();
            _reusableWriter.Put((byte)type);
            _reusableWriter.Put(data.Length);
            _reusableWriter.Put(data);
            
            peer.Send(_reusableWriter, method);
        }

        public void BroadcastToAll(PacketType type, byte[] data, int excludePlayerId = -1, DeliveryMethod method = DeliveryMethod.ReliableOrdered)
        {
            _reusableWriter.Reset();
            _reusableWriter.Put((byte)type);
            _reusableWriter.Put(data.Length);
            _reusableWriter.Put(data);

            int sentCount = 0;

            foreach (var kvp in _playerPeers)
            {
                if (kvp.Key == excludePlayerId) continue;
                
                kvp.Value.Send(_reusableWriter, method);
                sentCount++;
            }

            if (sentCount > 0 && type != PacketType.PlayerMovement && type != PacketType.StatsUpdate && type != PacketType.ResourceUpdate)
            {
                Console.WriteLine($"[GameServer] Broadcast {type} enviado para {sentCount} jogadores");
            }
        }

        public void BroadcastPlayerSpawn(Player player)
        {
            var spawnPacket = new PlayerSpawnPacket
            {
                PlayerId = player.Id,
                PlayerName = player.Name,
                PosX = player.Position.X,
                PosY = player.Position.Y,
                PosZ = player.Position.Z
            };

            byte[] data = spawnPacket.Serialize();
            BroadcastToAll(PacketType.PlayerSpawn, data, player.Id, DeliveryMethod.ReliableOrdered);
        }

        public void BroadcastPlayerMovement(Player player, ClientHandler sender)
        {
            var movementPacket = new PlayerMovementPacket
            {
                PlayerId = player.Id,
                PosX = player.Position.X,
                PosY = player.Position.Y,
                PosZ = player.Position.Z,
                RotX = player.Rotation.X,
                RotY = player.Rotation.Y
            };

            byte[] data = movementPacket.Serialize();
            BroadcastToAll(PacketType.PlayerMovement, data, player.Id, DeliveryMethod.Sequenced);
        }

        public void BroadcastPlayerDisconnect(int playerId)
        {
            byte[] data = BitConverter.GetBytes(playerId);
            BroadcastToAll(PacketType.PlayerDisconnect, data, playerId, DeliveryMethod.ReliableOrdered);
        }

        public async Task SendExistingPlayersTo(ClientHandler newClient)
        {
            var newPlayerId = newClient.GetPlayer()?.Id ?? -1;
            var newPeer = newClient.GetPeer();
            
            int count = 0;

            List<Player> playersSnapshot;
            lock (_playersLock)
            {
                playersSnapshot = _players.Values.ToList();
            }

            foreach (var player in playersSnapshot)
            {
                if (player.Id == newPlayerId) continue;

                var spawnPacket = new PlayerSpawnPacket
                {
                    PlayerId = player.Id,
                    PlayerName = player.Name,
                    PosX = player.Position.X,
                    PosY = player.Position.Y,
                    PosZ = player.Position.Z
                };

                byte[] data = spawnPacket.Serialize();

                try
                {
                    SendPacket(newPeer, PacketType.PlayerSpawn, data, DeliveryMethod.ReliableOrdered);
                    await Task.Delay(50);
                    count++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[GameServer] Erro ao enviar player: {ex.Message}");
                }
            }
        }

        // ==================== RESOURCE METHODS ====================

        public async Task SendResourcesToClient(ClientHandler client)
        {
            var resources = _resourceManager.GetAllResources();

            var packet = new ResourcesSyncPacket();
            
            foreach (var resource in resources)
            {
                packet.Resources.Add(new ResourceData
                {
                    Id = resource.Id,
                    Type = (byte)resource.Type,
                    PosX = resource.Position.X,
                    PosY = resource.Position.Y,
                    PosZ = resource.Position.Z,
                    Health = resource.Health,
                    MaxHealth = resource.MaxHealth
                });
            }

            SendPacket(client.GetPeer(), PacketType.ResourcesSync, packet.Serialize(), DeliveryMethod.ReliableOrdered);
            
            await Task.CompletedTask;
        }

        public GatherResult GatherResource(int resourceId, float damage, int toolType, Player player)
        {
            return _resourceManager.GatherResource(resourceId, damage, toolType, player);
        }

        public void BroadcastResourceUpdate(int resourceId)
        {
            var resource = _resourceManager.GetResource(resourceId);
            if (resource == null || !resource.IsAlive) return;

            var packet = new ResourceUpdatePacket
            {
                ResourceId = resourceId,
                Health = resource.Health,
                MaxHealth = resource.MaxHealth
            };

            BroadcastToAll(PacketType.ResourceUpdate, packet.Serialize(), -1, DeliveryMethod.Unreliable);
        }

        public void BroadcastResourceDestroyed(int resourceId)
        {
            var packet = new ResourceDestroyedPacket
            {
                ResourceId = resourceId
            };

            BroadcastToAll(PacketType.ResourceDestroyed, packet.Serialize(), -1, DeliveryMethod.ReliableOrdered);
        }

        public void BroadcastResourceRespawn(int resourceId)
        {
            var resource = _resourceManager.GetResource(resourceId);
            if (resource == null || !resource.IsAlive) return;

            var packet = new ResourceRespawnPacket
            {
                ResourceId = resourceId,
                Health = resource.Health,
                MaxHealth = resource.MaxHealth
            };

            BroadcastToAll(PacketType.ResourceRespawn, packet.Serialize(), -1, DeliveryMethod.ReliableOrdered);
        }

        // ==================== ⭐ NOVO: CRAFTING METHODS ====================

        /// <summary>
        /// ⭐ NOVO: Envia receitas de crafting para um cliente
        /// </summary>
        public async Task SendRecipesToClient(ClientHandler client)
        {
            var recipes = _craftingManager.GetAllRecipes();
            
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[GameServer] Enviando {recipes.Count} receitas para {client.GetPlayer()?.Name}");
            Console.ResetColor();

            var packet = new RecipesSyncPacket();
            
            foreach (var recipe in recipes)
            {
                var recipeData = new RecipeData
                {
                    Id = recipe.Id,
                    Name = recipe.Name,
                    ResultItemId = recipe.ResultItemId,
                    ResultQuantity = recipe.ResultQuantity,
                    CraftingTime = recipe.CraftingTime,
                    RequiredWorkbench = recipe.RequiredWorkbench
                };

                foreach (var ingredient in recipe.Ingredients)
                {
                    recipeData.Ingredients.Add(new IngredientData
                    {
                        ItemId = ingredient.ItemId,
                        Quantity = ingredient.Quantity
                    });
                }

                packet.Recipes.Add(recipeData);
            }

            SendPacket(client.GetPeer(), PacketType.RecipesSync, packet.Serialize(), DeliveryMethod.ReliableOrdered);
            
            await Task.CompletedTask;
        }

        /// <summary>
        /// ⭐ NOVO: Inicia crafting para um player
        /// </summary>
        public CraftResult StartCrafting(int playerId, int recipeId)
        {
            var player = GetPlayer(playerId);
            if (player == null)
            {
                return new CraftResult
                {
                    Success = false,
                    Message = "Player não encontrado"
                };
            }

            return _craftingManager.StartCrafting(playerId, recipeId, player.Inventory);
        }

        /// <summary>
        /// ⭐ NOVO: Cancela crafting
        /// </summary>
        public bool CancelCrafting(int playerId, int queueIndex)
        {
            return _craftingManager.CancelCrafting(playerId, queueIndex);
        }

        /// <summary>
        /// ⭐ NOVO: Pega fila de crafting de um player
        /// </summary>
        public List<CraftingProgress> GetPlayerCraftQueue(int playerId)
        {
            return _craftingManager.GetPlayerQueue(playerId);
        }

        /// <summary>
        /// ⭐ NOVO: Loop de atualização de crafting
        /// </summary>
        private async Task UpdateCraftingLoopAsync()
        {
            DateTime lastUpdate = DateTime.Now;

            while (_isRunning)
            {
                await Task.Delay(500); // 2x por segundo

                DateTime now = DateTime.Now;

                if ((now - lastUpdate).TotalSeconds >= CRAFTING_UPDATE_RATE)
                {
                    lastUpdate = now;
                    
                    // Atualiza craftings e pega completados
                    var completedCrafts = _craftingManager.Update();

                    // Processa craftings completados
                    foreach (var completed in completedCrafts)
                    {
                        await HandleCraftComplete(completed);
                    }
                }
            }
        }

        /// <summary>
        /// ⭐ NOVO: Processa crafting completo
        /// </summary>
        private async Task HandleCraftComplete(CraftCompleteResult result)
        {
            var player = GetPlayer(result.PlayerId);
            if (player == null) return;

            // Adiciona item ao inventário
            bool success = player.Inventory.AddItem(result.ResultItemId, result.ResultQuantity);

            if (success)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[GameServer] ✅ Crafting completo! Player {result.PlayerId} recebeu {result.ResultQuantity}x Item {result.ResultItemId}");
                Console.ResetColor();

                // Notifica o cliente
                if (_playerPeers.TryGetValue(result.PlayerId, out var peer) && 
                    _clients.TryGetValue(peer, out var handler))
                {
                    await handler.NotifyCraftComplete(
                        result.RecipeId,
                        result.ResultItemId,
                        result.ResultQuantity
                    );
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[GameServer] ❌ Inventário cheio! Item {result.ResultItemId} perdido");
                Console.ResetColor();
                
                // TODO: Dropar item no chão
            }
        }
/// <summary>
/// Processa ataque melee
/// </summary>
public Combat.AttackResult ProcessMeleeAttack(
    World.Player attacker,
    World.Player victim,
    int weaponItemId,
    Combat.HitboxType hitbox,
    World.Vector3 direction)
{
    return _combatManager.ProcessMeleeAttack(
        attacker,
        victim,
        weaponItemId,
        hitbox,
        direction
    );
}

/// <summary>
/// Processa ataque ranged
/// </summary>
public Combat.AttackResult ProcessRangedAttack(
    World.Player attacker,
    World.Player victim,
    Combat.WeaponState weaponState,
    Combat.HitboxType hitbox,
    World.Vector3 shootDirection,
    float distance)
{
    return _combatManager.ProcessRangedAttack(
        attacker,
        victim,
        weaponState,
        hitbox,
        shootDirection,
        distance
    );
}

/// <summary>
/// Notifica player que tomou dano
/// </summary>
public void NotifyPlayerDamage(
    int victimId,
    int attackerId,
    float damage,
    Combat.DamageType damageType,
    Combat.HitboxType hitbox,
    World.Vector3 direction)
{
    if (!_playerPeers.TryGetValue(victimId, out var peer))
    {
        return;
    }

    var packet = new Network.TakeDamageNotifyPacket
    {
        AttackerId = attackerId,
        Damage = damage,
        DamageType = (byte)damageType,
        Hitbox = (byte)hitbox,
        DirectionX = direction.X,
        DirectionY = direction.Y,
        DirectionZ = direction.Z
    };

    SendPacket(peer, Network.PacketType.TakeDamageNotify, packet.Serialize(), DeliveryMethod.ReliableOrdered);

    Console.WriteLine($"[GameServer] 💥 Notificou {victimId} que tomou {damage:F1} dano");
}

/// <summary>
/// Broadcasta que player foi morto
/// </summary>
public void BroadcastPlayerKilled(
    int victimId,
    int killerId,
    string killerName,
    string weaponUsed,
    Combat.HitboxType hitbox,
    float distance)
{
    var victim = GetPlayer(victimId);
    var killer = GetPlayer(killerId);

    if (victim == null) return;

    // Registra estatísticas
    victim.Combat.RegisterDeath();
    if (killer != null)
    {
        killer.Combat.RegisterKill();
    }

    // Cria pacote
    var packet = new Network.PlayerKilledPacket
    {
        VictimId = victimId,
        KillerId = killerId,
        KillerName = killerName ?? "",
        WeaponUsed = weaponUsed ?? "Unknown",
        Hitbox = (byte)hitbox,
        Distance = distance
    };

    // Broadcasta para todos
    BroadcastToAll(Network.PacketType.PlayerKilled, packet.Serialize(), -1, DeliveryMethod.ReliableOrdered);

    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"\n╔══════════════════════════════════════════════════╗");
    Console.WriteLine($"║  ☠️  KILL FEED                                  ║");
    Console.WriteLine($"╠══════════════════════════════════════════════════╣");
    Console.WriteLine($"║  {killerName} [{weaponUsed}] {victim.Name}");
    
    if (hitbox == Combat.HitboxType.Head)
    {
        Console.WriteLine($"║  💥 HEADSHOT! @ {distance:F1}m");
    }
    else
    {
        Console.WriteLine($"║  Distance: {distance:F1}m");
    }
    
    Console.WriteLine($"╚══════════════════════════════════════════════════╝");
    Console.ResetColor();
}

/// <summary>
/// Broadcasta respawn de player
/// </summary>
public void BroadcastPlayerRespawn(World.Player player)
{
    var spawnPacket = new Network.PlayerSpawnPacket
    {
        PlayerId = player.Id,
        PlayerName = player.Name,
        PosX = player.Position.X,
        PosY = player.Position.Y,
        PosZ = player.Position.Z
    };

    BroadcastToAll(Network.PacketType.PlayerSpawn, spawnPacket.Serialize(), player.Id, DeliveryMethod.ReliableOrdered);

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"[GameServer] ♻️ {player.Name} respawnou e foi broadcastado");
    Console.ResetColor();
}

/// <summary>
/// Loop de atualização de combate (chama Update dos players)
/// </summary>
private async Task UpdateCombatLoopAsync()
{
    while (_isRunning)
    {
        await Task.Delay(100); // 10x por segundo

        List<World.Player> playersSnapshot;
        lock (_playersLock)
        {
            playersSnapshot = _players.Values.ToList();
        }

        foreach (var player in playersSnapshot)
        {
            player.Combat.Update();
        }
    }
}
/// <summary>
/// Pega combat log recente
/// </summary>
public List<Combat.CombatLogEntry> GetRecentCombatLog(int count = 10)
{
    return _combatManager.GetRecentCombatLog(count);
}

/// <summary>
/// Pega kills de um player
/// </summary>
public List<Combat.CombatLogEntry> GetPlayerKills(int playerId)
{
    return _combatManager.GetPlayerKills(playerId);
}

/// <summary>
/// Pega mortes de um player
/// </summary>
public List<Combat.CombatLogEntry> GetPlayerDeaths(int playerId)
{
    return _combatManager.GetPlayerDeaths(playerId);
}

/// <summary>
/// ⭐ Comando de console para ver combat log
/// </summary>
public void ShowCombatLog(int count = 10)
{
    var log = GetRecentCombatLog(count);

    Console.WriteLine("\n╔════════════════════════════════════════════════╗");
    Console.WriteLine($"║  COMBAT LOG - Últimos {log.Count} eventos          ║");
    Console.WriteLine("╠════════════════════════════════════════════════╣");

    foreach (var entry in log)
    {
        Console.WriteLine($"║  {entry}");
    }

    Console.WriteLine("╚════════════════════════════════════════════════╝\n");
}

/// <summary>
/// ⭐ Comando de console para ver stats de um player
/// </summary>

public void ShowPlayerStats(int playerId)
{
    var player = GetPlayer(playerId);
    if (player == null)
    {
        Console.WriteLine($"[GameServer] Player {playerId} não encontrado");
        return;
    }

    Console.WriteLine("\n╔════════════════════════════════════════════════╗");
    Console.WriteLine($"║  PLAYER STATS: {player.Name,-30} ║");
    Console.WriteLine("╠════════════════════════════════════════════════╣");
    Console.WriteLine($"║  {player.Combat.GetStats(),-46} ║");
    Console.WriteLine($"║  In Combat: {(player.Combat.IsInCombat ? "YES" : "NO"),-35} ║");
    
    if (player.Combat.EquippedWeapon != null)
    {
        var weapon = player.Combat.EquippedWeapon;
        Console.WriteLine($"║  Weapon: {weapon.Definition.Name,-37} ║");
        Console.WriteLine($"║  Ammo: {weapon.CurrentAmmo}/{weapon.Definition.MagazineSize} ({weapon.ReserveAmmo} reserve)");
    }
    else
    {
        Console.WriteLine($"║  Weapon: None{"",-38} ║");
    }
    
    Console.WriteLine("╠════════════════════════════════════════════════╣");
    Console.WriteLine($"║  Recent Kills:                                 ║");
    
    var kills = GetPlayerKills(playerId).TakeLast(3);
    foreach (var kill in kills)
    {
        Console.WriteLine($"║    • {kill.VictimName} with {kill.WeaponUsed}");
    }
    
    Console.WriteLine("╚════════════════════════════════════════════════╝\n");
}



        // ==================== STATS SYSTEM ====================

        private async Task UpdateStatsLoopAsync()
        {
            DateTime lastStatsUpdate = DateTime.Now;
            DateTime lastStatsSync = DateTime.Now;

            while (_isRunning)
            {
                await Task.Delay(100);

                DateTime now = DateTime.Now;

                if ((now - lastStatsUpdate).TotalSeconds >= STATS_UPDATE_RATE)
                {
                    lastStatsUpdate = now;
                    UpdateAllPlayersStats();
                }

                if ((now - lastStatsSync).TotalSeconds >= STATS_SYNC_RATE)
                {
                    lastStatsSync = now;
                    SyncAllPlayersStats();
                }
            }
        }

        private void UpdateAllPlayersStats()
        {
            List<Player> playersSnapshot;
            lock (_playersLock)
            {
                playersSnapshot = _players.Values.ToList();
            }

            foreach (var player in playersSnapshot)
            {
                player.UpdateStats();

                if (player.IsDead())
                {
                    HandlePlayerDeath(player);
                }
            }
        }

        private void SyncAllPlayersStats()
        {
            List<Player> playersSnapshot;
            lock (_playersLock)
            {
                playersSnapshot = _players.Values.ToList();
            }

            foreach (var player in playersSnapshot)
            {
                if (_playerPeers.TryGetValue(player.Id, out var peer))
                {
                    var statsPacket = new StatsUpdatePacket
                    {
                        PlayerId = player.Id,
                        Health = player.Stats.Health,
                        Hunger = player.Stats.Hunger,
                        Thirst = player.Stats.Thirst,
                        Temperature = player.Stats.Temperature
                    };

                    SendPacket(peer, PacketType.StatsUpdate, statsPacket.Serialize(), DeliveryMethod.Unreliable);
                }
            }
        }

        private void HandlePlayerDeath(Player player)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[GameServer] ☠️  MORTE: {player.Name} (ID: {player.Id})");
            Console.ResetColor();

            var deathPacket = new PlayerDeathPacket
            {
                PlayerId = player.Id,
                KillerName = ""
            };

            BroadcastToAll(PacketType.PlayerDeath, deathPacket.Serialize(), -1, DeliveryMethod.ReliableOrdered);
        }

        // ==================== RESOURCE UPDATE ====================

        private async Task UpdateResourcesLoopAsync()
        {
            DateTime lastUpdate = DateTime.Now;

            while (_isRunning)
            {
                await Task.Delay(1000);

                DateTime now = DateTime.Now;

                if ((now - lastUpdate).TotalSeconds >= RESOURCE_UPDATE_RATE)
                {
                    lastUpdate = now;
                    _resourceManager.Update();
                }
            }
        }

        // ==================== MONITORING ====================

        private async Task MonitorPlayersAsync()
        {
            while (_isRunning)
            {
                await Task.Delay(5000);

                List<Player> timedOutPlayers;
                lock (_playersLock)
                {
                    timedOutPlayers = _players.Values.Where(p => p.IsTimedOut()).ToList();
                }
                
                foreach (var player in timedOutPlayers)
                {
                    RemovePlayer(player.Id);
                }

                lock (_playersLock)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"\n╔════════════════════════════════════════════════╗");
                    Console.WriteLine($"║  JOGADORES ONLINE: {_players.Count,-2}                         ║");
                    Console.WriteLine($"║  CLIENTS CONECTADOS: {_clients.Count,-2}                      ║");
                    Console.WriteLine($"╚════════════════════════════════════════════════╝");
                    Console.ResetColor();
                }
            }
        }

        public Player GetPlayer(int playerId)
        {
            lock (_playersLock)
            {
                return _players.TryGetValue(playerId, out var player) ? player : null;
            }
        }

        public void Stop()
        {
            _isRunning = false;
            
            foreach (var client in _clients.Values)
            {
                client.Disconnect();
            }

            _netManager?.Stop();
            Console.WriteLine("[GameServer] Servidor encerrado");
        }
    }
}