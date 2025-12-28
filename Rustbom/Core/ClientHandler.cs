using System;
using System.Threading.Tasks;
using LiteNetLib;
using RustlikeServer.Network;
using RustlikeServer.World;
using RustlikeServer.Items;
using RustlikeServer.Combat;

namespace RustlikeServer.Core
{
    /// <summary>
    /// ⭐ ATUALIZADO COM SISTEMA DE CRAFTING
    /// </summary>
    public class ClientHandler
    {
        private NetPeer _peer;
        private GameServer _server;
        private Player _player;
        private bool _isRunning;
        private bool _isFullyLoaded = false;

        public ClientHandler(NetPeer peer, GameServer server)
        {
            _peer = peer;
            _server = server;
            _isRunning = true;

            Console.WriteLine($"[ClientHandler] Novo ClientHandler criado para Peer ID: {peer.Id}");
        }

        public async Task ProcessPacketAsync(byte[] data)
        {
            try
            {
                Packet packet = Packet.Deserialize(data);
                if (packet == null) return;

                switch (packet.Type)
                {
                    case PacketType.ConnectionRequest:
                        await HandleConnectionRequest(packet.Data);
                        break;

                    case PacketType.ClientReady:
                        await HandleClientReady();
                        break;

                    case PacketType.PlayerMovement:
                        HandlePlayerMovement(packet.Data);
                        break;

                    case PacketType.Heartbeat:
                        HandleHeartbeat();
                        break;

                    case PacketType.PlayerDisconnect:
                        Disconnect();
                        break;

                    case PacketType.ItemUse:
                        await HandleItemUse(packet.Data);
                        break;

                    case PacketType.ItemMove:
                        await HandleItemMove(packet.Data);
                        break;

                    case PacketType.ResourceHit:
                        await HandleResourceHit(packet.Data);
                        break;

                    // ⭐ NOVO: Handlers de Crafting
                    case PacketType.CraftRequest:
                        await HandleCraftRequest(packet.Data);
                        break;

                    case PacketType.CraftCancel:
                        await HandleCraftCancel(packet.Data);
                        break;
						
					case PacketType.MeleeAttack:
						await HandleMeleeAttack(packet.Data);
						break;
					
					case PacketType.RangedAttack:
						await HandleRangedAttack(packet.Data);
						break;
					
					case PacketType.WeaponEquip:
						await HandleWeaponEquip(packet.Data);
						break;
					
					case PacketType.WeaponReload:
						await HandleWeaponReload(packet.Data);
						break;
					
					case PacketType.RespawnRequest:
						await HandleRespawnRequest();
						break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ClientHandler] Erro ao processar pacote: {ex.Message}");
            }
        }

        private async Task HandleConnectionRequest(byte[] data)
        {
            var request = ConnectionRequestPacket.Deserialize(data);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n[ClientHandler] ===== NOVA CONEXÃO =====");
            Console.WriteLine($"[ClientHandler] Nome: {request.PlayerName}");
            Console.WriteLine($"[ClientHandler] Peer ID: {_peer.Id}");
            Console.WriteLine($"[ClientHandler] Endpoint: {_peer.Address}:{_peer.Port}");
            Console.ResetColor();

            _player = _server.CreatePlayer(request.PlayerName);
            Console.WriteLine($"[ClientHandler] Player criado com ID: {_player.Id}");

            _server.RegisterClient(_player.Id, _peer, this);
            Console.WriteLine($"[ClientHandler] ClientHandler registrado");

            var response = new ConnectionAcceptPacket
            {
                PlayerId = _player.Id,
                SpawnX = _player.Position.X,
                SpawnY = _player.Position.Y,
                SpawnZ = _player.Position.Z
            };

            SendPacket(PacketType.ConnectionAccept, response.Serialize());
            
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[ClientHandler] ✅ ConnectionAccept ENVIADO para {_player.Name} (ID: {_player.Id})");
            Console.WriteLine($"[ClientHandler] ⏳ AGUARDANDO ClientReady do cliente...");
            Console.ResetColor();

            await Task.CompletedTask;
        }

        private async Task HandleClientReady()
        {
            _isFullyLoaded = true;

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"\n[ClientHandler] 📢 CLIENT READY RECEBIDO de {_player.Name} (ID: {_player.Id})");
            Console.WriteLine($"[ClientHandler] Cliente carregou completamente! Iniciando sincronização...");
            Console.ResetColor();

            await Task.Delay(150);

            // Envia inventário
            await SendInventoryUpdate();

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"[ClientHandler] 📤 Enviando players existentes para {_player.Name}...");
            Console.ResetColor();
            await _server.SendExistingPlayersTo(this);

            await Task.Delay(300);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[ClientHandler] 🌲 Enviando recursos do mundo para {_player.Name}...");
            Console.ResetColor();
            await _server.SendResourcesToClient(this);

            await Task.Delay(300);

            // ⭐ NOVO: Envia receitas de crafting
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[ClientHandler] 🔨 Enviando receitas de crafting para {_player.Name}...");
            Console.ResetColor();
            await _server.SendRecipesToClient(this);

            await Task.Delay(300);

            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"[ClientHandler] 📢 Broadcasting spawn de {_player.Name} para outros jogadores...");
            Console.ResetColor();
            _server.BroadcastPlayerSpawn(_player);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[ClientHandler] ✅✅✅ SINCRONIZAÇÃO COMPLETA: {_player.Name} (ID: {_player.Id})");
            Console.ResetColor();
            Console.WriteLine();
        }

        private void HandlePlayerMovement(byte[] data)
        {
            if (_player == null) return;

            var movement = PlayerMovementPacket.Deserialize(data);
            
            _player.UpdatePosition(movement.PosX, movement.PosY, movement.PosZ);
            _player.UpdateRotation(movement.RotX, movement.RotY);
            _player.UpdateHeartbeat();

            _server.BroadcastPlayerMovement(_player, this);
        }

        private void HandleHeartbeat()
        {
            if (_player != null)
            {
                _player.UpdateHeartbeat();
            }
        }

        private async Task HandleItemUse(byte[] data)
        {
            if (_player == null) return;

            var packet = ItemUsePacket.Deserialize(data);
            Console.WriteLine($"[ClientHandler] 🎒 {_player.Name} tentou usar item do slot {packet.SlotIndex}");

            var itemStack = _player.Inventory.GetSlot(packet.SlotIndex);
            if (itemStack == null || itemStack.Definition == null)
            {
                Console.WriteLine($"[ClientHandler] ⚠️ Slot {packet.SlotIndex} vazio");
                return;
            }

            var itemDef = itemStack.Definition;

            if (!itemDef.IsConsumable)
            {
                Console.WriteLine($"[ClientHandler] ⚠️ Item {itemDef.Name} não é consumível");
                return;
            }

            bool canUse = CanUseItem(itemDef, _player.Stats);
            
            if (!canUse)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[ClientHandler] ⚠️ {_player.Name} tentou usar {itemDef.Name} mas stats já estão cheias");
                Console.ResetColor();
                return;
            }

            var consumedItem = _player.Inventory.ConsumeItem(packet.SlotIndex);
            if (consumedItem == null)
            {
                Console.WriteLine($"[ClientHandler] ❌ Erro ao consumir item do slot {packet.SlotIndex}");
                return;
            }

            if (consumedItem.HealthRestore > 0)
                _player.Stats.Heal(consumedItem.HealthRestore);
            
            if (consumedItem.HungerRestore > 0)
                _player.Stats.Eat(consumedItem.HungerRestore);
            
            if (consumedItem.ThirstRestore > 0)
                _player.Stats.Drink(consumedItem.ThirstRestore);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[ClientHandler] ✅ {_player.Name} usou {consumedItem.Name}");
            Console.ResetColor();

            await SendInventoryUpdate();
        }

        private bool CanUseItem(ItemDefinition item, PlayerStats stats)
        {
            if (item.HealthRestore > 0 && stats.Health < 100f)
                return true;

            if (item.HungerRestore > 0 && stats.Hunger < 100f)
                return true;

            if (item.ThirstRestore > 0 && stats.Thirst < 100f)
                return true;

            return false;
        }

        private async Task HandleItemMove(byte[] data)
        {
            if (_player == null) return;

            var packet = ItemMovePacket.Deserialize(data);
            Console.WriteLine($"[ClientHandler] 🎒 {_player.Name} moveu item: {packet.FromSlot} → {packet.ToSlot}");

            bool success = _player.Inventory.MoveItem(packet.FromSlot, packet.ToSlot);
            if (success)
            {
                await SendInventoryUpdate();
            }
        }

        private async Task HandleResourceHit(byte[] data)
        {
            if (_player == null) return;

            var packet = ResourceHitPacket.Deserialize(data);
            
            var result = _server.GatherResource(packet.ResourceId, packet.Damage, packet.ToolType, _player);
            
            if (result != null)
            {
                bool success = false;
                
                if (result.WoodGained > 0)
                    success |= _player.Inventory.AddItem(100, result.WoodGained);
                
                if (result.StoneGained > 0)
                    success |= _player.Inventory.AddItem(101, result.StoneGained);
                
                if (result.MetalGained > 0)
                    success |= _player.Inventory.AddItem(102, result.MetalGained);
                
                if (result.SulfurGained > 0)
                    success |= _player.Inventory.AddItem(103, result.SulfurGained);

                var gatherPacket = new GatherResultPacket
                {
                    WoodGained = result.WoodGained,
                    StoneGained = result.StoneGained,
                    MetalGained = result.MetalGained,
                    SulfurGained = result.SulfurGained
                };
                
                SendPacket(PacketType.GatherResult, gatherPacket.Serialize());

                if (success)
                {
                    await SendInventoryUpdate();
                }

                _server.BroadcastResourceUpdate(packet.ResourceId);

                if (result.WasDestroyed)
                {
                    _server.BroadcastResourceDestroyed(packet.ResourceId);
                }
            }

            await Task.CompletedTask;
        }

        // ==================== ⭐ NOVOS HANDLERS DE CRAFTING ====================

        /// <summary>
        /// ⭐ NOVO: Handle de solicitação de crafting
        /// </summary>
        private async Task HandleCraftRequest(byte[] data)
        {
            if (_player == null) return;

            var packet = CraftRequestPacket.Deserialize(data);
            
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[ClientHandler] 🔨 {_player.Name} solicitou crafting da receita {packet.RecipeId}");
            Console.ResetColor();

            var result = _server.StartCrafting(_player.Id, packet.RecipeId);

            // Envia resposta
            var response = new CraftStartedPacket
            {
                RecipeId = packet.RecipeId,
                Duration = result.Duration,
                Success = result.Success,
                Message = result.Message
            };

            SendPacket(PacketType.CraftStarted, response.Serialize());

            if (result.Success)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[ClientHandler] ✅ Crafting iniciado para {_player.Name}");
                Console.ResetColor();

                // Atualiza inventário (recursos foram consumidos)
                await SendInventoryUpdate();

                // Envia fila de crafting atualizada
                await SendCraftQueueUpdate();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ClientHandler] ❌ Falha no crafting: {result.Message}");
                Console.ResetColor();
            }
        }

        /// <summary>
        /// ⭐ NOVO: Handle de cancelamento de crafting
        /// </summary>
        private async Task HandleCraftCancel(byte[] data)
        {
            if (_player == null) return;

            var packet = CraftCancelPacket.Deserialize(data);
            
            Console.WriteLine($"[ClientHandler] ❌ {_player.Name} cancelou crafting no índice {packet.QueueIndex}");

            bool success = _server.CancelCrafting(_player.Id, packet.QueueIndex);

            if (success)
            {
                await SendCraftQueueUpdate();
            }
        }

        /// <summary>
        /// ⭐ NOVO: Envia fila de crafting atualizada
        /// </summary>
        public async Task SendCraftQueueUpdate()
        {
            if (_player == null) return;

            var queue = _server.GetPlayerCraftQueue(_player.Id);
            var packet = new CraftQueueUpdatePacket();

            foreach (var progress in queue)
            {
                packet.QueueItems.Add(new CraftQueueItem
                {
                    RecipeId = progress.RecipeId,
                    Progress = progress.GetProgress(),
                    RemainingTime = progress.GetRemainingTime()
                });
            }

            SendPacket(PacketType.CraftQueueUpdate, packet.Serialize());
            
            await Task.CompletedTask;
        }

        /// <summary>
        /// ⭐ NOVO: Notifica que crafting foi completo
        /// </summary>
        public async Task NotifyCraftComplete(int recipeId, int resultItemId, int resultQuantity)
        {
            var packet = new CraftCompletePacket
            {
                RecipeId = recipeId,
                ResultItemId = resultItemId,
                ResultQuantity = resultQuantity
            };

            SendPacket(PacketType.CraftComplete, packet.Serialize());

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[ClientHandler] ✅ Notificou {_player.Name} que crafting foi completo");
            Console.ResetColor();

            // Atualiza inventário
            await SendInventoryUpdate();

            // Atualiza fila
            await SendCraftQueueUpdate();
        }
		/// <summary>
/// Handle de ataque melee
/// </summary>
private async Task HandleMeleeAttack(byte[] data)
{
    if (_player == null || _player.IsDead()) return;

    var packet = MeleeAttackPacket.Deserialize(data);
    
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"[ClientHandler] ⚔️ {_player.Name} atacou melee → Player {packet.TargetPlayerId}");
    Console.ResetColor();

    // Pega player alvo
    var victim = _server.GetPlayer(packet.TargetPlayerId);
    if (victim == null)
    {
        SendAttackResult(false, "Alvo não encontrado");
        return;
    }

    if (victim.IsDead())
    {
        SendAttackResult(false, "Alvo já está morto");
        return;
    }

    // Processa ataque via CombatManager
    var direction = new World.Vector3(packet.DirectionX, packet.DirectionY, packet.DirectionZ);
    var hitbox = (Combat.HitboxType)packet.Hitbox;

    var result = _server.ProcessMeleeAttack(
        _player,
        victim,
        packet.WeaponItemId,
        hitbox,
        direction
    );

    // Envia resultado para atacante
    SendAttackResult(
        result.Success,
        result.Message,
        result.DamageDealt,
        result.WasKilled,
        result.Hitbox,
        result.Distance
    );

    // Se acertou, notifica vítima
    if (result.Success)
    {
        _server.NotifyPlayerDamage(
            victim.Id,
            _player.Id,
            result.DamageDealt,
            Combat.DamageType.Melee,
            hitbox,
            direction
        );

        // Se matou, broadcasta
        if (result.WasKilled)
        {
            _server.BroadcastPlayerKilled(
                victim.Id,
                _player.Id,
                _player.Name,
                "Melee", // TODO: Nome da arma
                hitbox,
                result.Distance
            );
        }
    }

    await Task.CompletedTask;
}

/// <summary>
/// Handle de ataque ranged (tiro)
/// </summary>
private async Task HandleRangedAttack(byte[] data)
{
    if (_player == null || _player.IsDead()) return;

    var packet = RangedAttackPacket.Deserialize(data);
    
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"[ClientHandler] 🔫 {_player.Name} atirou → Player {packet.TargetPlayerId}");
    Console.ResetColor();

    // Verifica se tem arma equipada
    if (_player.Combat.EquippedWeapon == null)
    {
        SendAttackResult(false, "Nenhuma arma equipada");
        return;
    }

    // Pega player alvo (pode ser -1 se errou)
    Player victim = null;
    if (packet.TargetPlayerId > 0)
    {
        victim = _server.GetPlayer(packet.TargetPlayerId);
        if (victim == null || victim.IsDead())
        {
            SendAttackResult(false, "Alvo inválido");
            return;
        }
    }

    // Processa tiro
    var shootDirection = new World.Vector3(
        packet.ShootDirectionX,
        packet.ShootDirectionY,
        packet.ShootDirectionZ
    );
    var hitbox = (Combat.HitboxType)packet.Hitbox;

    // Se acertou em alguém
    if (victim != null)
    {
        var result = _server.ProcessRangedAttack(
            _player,
            victim,
            _player.Combat.EquippedWeapon,
            hitbox,
            shootDirection,
            packet.Distance
        );

        // Envia resultado
        SendAttackResult(
            result.Success,
            result.Message,
            result.DamageDealt,
            result.WasKilled,
            result.Hitbox,
            result.Distance,
            result.RemainingAmmo
        );

        if (result.Success)
        {
            // Notifica vítima
            _server.NotifyPlayerDamage(
                victim.Id,
                _player.Id,
                result.DamageDealt,
                Combat.DamageType.Bullet,
                hitbox,
                shootDirection
            );

            // Atualiza estado da arma
            SendWeaponStateUpdate();

            // Se matou, broadcasta
            if (result.WasKilled)
            {
                _server.BroadcastPlayerKilled(
                    victim.Id,
                    _player.Id,
                    _player.Name,
                    _player.Combat.EquippedWeapon.Definition.Name,
                    hitbox,
                    result.Distance
                );
            }
        }
    }
    else
    {
        // Errou - ainda consome munição
        if (_player.Combat.EquippedWeapon.CanFire())
        {
            _player.Combat.EquippedWeapon.ConsumeAmmo();
            SendWeaponStateUpdate();
        }

        SendAttackResult(false, "Miss", 0, false, hitbox, packet.Distance, 
            _player.Combat.EquippedWeapon.CurrentAmmo);
    }

    await Task.CompletedTask;
}

/// <summary>
/// Handle de equipar arma
/// </summary>
private async Task HandleWeaponEquip(byte[] data)
{
    if (_player == null) return;

    var packet = WeaponEquipPacket.Deserialize(data);
    
    Console.WriteLine($"[ClientHandler] 🔫 {_player.Name} equipando arma {packet.WeaponItemId} do slot {packet.SlotIndex}");

    if (packet.WeaponItemId == 0)
    {
        // Desequipar
        _player.Combat.UnequipWeapon();
    }
    else
    {
        // Equipar
        bool success = _player.Combat.EquipWeapon(packet.WeaponItemId, packet.SlotIndex);
        
        if (success)
        {
            SendWeaponStateUpdate();
        }
    }

    await Task.CompletedTask;
}

/// <summary>
/// Handle de recarga
/// </summary>
private async Task HandleWeaponReload(byte[] data)
{
    if (_player == null) return;

    Console.WriteLine($"[ClientHandler] 🔄 {_player.Name} recarregando arma");

    bool success = _player.Combat.ReloadWeapon();
    
    if (success)
    {
        SendWeaponStateUpdate();
    }

    await Task.CompletedTask;
}

/// <summary>
/// Handle de respawn
/// </summary>
private async Task HandleRespawnRequest()
{
    if (_player == null) return;

    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"[ClientHandler] ♻️ {_player.Name} solicitou respawn");
    Console.ResetColor();

    bool canRespawn = _player.IsDead();
    
    if (canRespawn)
    {
        // Respawna player
        _player.Respawn();

        // Envia resposta de sucesso
        var response = new Network.RespawnResponsePacket
        {
            Success = true,
            SpawnX = _player.Position.X,
            SpawnY = _player.Position.Y,
            SpawnZ = _player.Position.Z,
            Message = "Respawn bem-sucedido"
        };

        SendPacket(PacketType.RespawnResponse, response.Serialize());

        // Atualiza inventário (limpa/reseta)
        await SendInventoryUpdate();

        // Atualiza stats
        var statsPacket = new StatsUpdatePacket
        {
            PlayerId = _player.Id,
            Health = _player.Stats.Health,
            Hunger = _player.Stats.Hunger,
            Thirst = _player.Stats.Thirst,
            Temperature = _player.Stats.Temperature
        };

        SendPacket(PacketType.StatsUpdate, statsPacket.Serialize());

        // Broadcasta respawn para outros players
        _server.BroadcastPlayerRespawn(_player);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[ClientHandler] ✅ {_player.Name} respawnou");
        Console.ResetColor();
    }
    else
    {
        // Nega respawn
       var response = new Network.RespawnResponsePacket
        {
            Success = false,
            Message = "Você não está morto"
        };

        SendPacket(PacketType.RespawnResponse, response.Serialize());
    }

    await Task.CompletedTask;
}

// ==================== MÉTODOS AUXILIARES ====================

/// <summary>
/// Envia resultado de ataque para o cliente
/// </summary>
private void SendAttackResult(
    bool success,
    string message,
    float damage = 0,
    bool wasKilled = false,
    Combat.HitboxType hitbox = Combat.HitboxType.Body,
    float distance = 0,
    int remainingAmmo = -1)
{
    var packet = new AttackResultPacket
    {
        Success = success,
        Message = message,
        DamageDealt = damage,
        WasKilled = wasKilled,
        Hitbox = (byte)hitbox,
        Distance = distance,
        RemainingAmmo = remainingAmmo
    };

    SendPacket(PacketType.AttackResult, packet.Serialize());
}

/// <summary>
/// Envia estado atual da arma
/// </summary>
private void SendWeaponStateUpdate()
{
    if (_player?.Combat.EquippedWeapon == null) return;

    var weapon = _player.Combat.EquippedWeapon;
    
    var packet = new WeaponStateUpdatePacket
    {
        WeaponItemId = weapon.Definition.ItemId,
        CurrentAmmo = weapon.CurrentAmmo,
        ReserveAmmo = weapon.ReserveAmmo,
        IsReloading = weapon.IsReloading,
        ReloadProgress = weapon.IsReloading ? 
            (float)(DateTime.Now - weapon.LastReloadTime).TotalSeconds / weapon.Definition.ReloadTime : 0f
    };

    SendPacket(PacketType.WeaponStateUpdate, packet.Serialize());
}




        // ==================== MÉTODOS AUXILIARES ====================

        private async Task SendInventoryUpdate()
        {
            var inventoryPacket = new InventoryUpdatePacket();
            var slots = _player.Inventory.GetAllSlots();

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null)
                {
                    inventoryPacket.Slots.Add(new InventorySlotData
                    {
                        SlotIndex = i,
                        ItemId = slots[i].ItemId,
                        Quantity = slots[i].Quantity
                    });
                }
            }

            SendPacket(PacketType.InventoryUpdate, inventoryPacket.Serialize());
            Console.WriteLine($"[ClientHandler] 📦 Inventário sincronizado: {inventoryPacket.Slots.Count} slots com itens");

            await Task.CompletedTask;
        }

        public void SendPacket(PacketType type, byte[] data, LiteNetLib.DeliveryMethod method = LiteNetLib.DeliveryMethod.ReliableOrdered)
        {
            try
            {
                if (_peer == null || _peer.ConnectionState != ConnectionState.Connected)
                    return;

                _server.SendPacket(_peer, type, data, method);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ClientHandler] ❌ Erro ao enviar pacote: {ex.Message}");
                Console.ResetColor();
            }
        }

        public void Disconnect()
        {
            if (!_isRunning) return;

            _isRunning = false;

            if (_player != null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[ClientHandler] ❌ Jogador {_player.Name} (ID: {_player.Id}) desconectado");
                Console.ResetColor();
                _server.RemovePlayer(_player.Id);
            }

            if (_peer != null && _peer.ConnectionState == ConnectionState.Connected)
            {
                _peer.Disconnect();
            }
        }

        public Player GetPlayer() => _player;
        public NetPeer GetPeer() => _peer;
        public bool IsConnected() => _isRunning && _peer != null && _peer.ConnectionState == ConnectionState.Connected;
        public bool IsFullyLoaded() => _isFullyLoaded;
    }
}