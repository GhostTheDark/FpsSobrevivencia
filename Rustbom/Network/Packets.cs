using System;
using System.Text;
using System.Collections.Generic;

namespace RustlikeServer.Network
{
    // Tipos de pacotes
    public enum PacketType : byte
    {
        ConnectionRequest = 0,
        ConnectionAccept = 1,
        PlayerSpawn = 2,
        PlayerMovement = 3,
        PlayerDisconnect = 4,
        WorldState = 5,
        Heartbeat = 6,
        ClientReady = 7,
        
        // Sistema de Stats
        StatsUpdate = 8,
        PlayerDeath = 9,
        PlayerRespawn = 10,
        TakeDamage = 11,
        ConsumeItem = 12,
        
        // Sistema de Inventário
        InventoryUpdate = 13,
        ItemUse = 14,
        ItemMove = 15,
        ItemDrop = 16,
        HotbarSelect = 17,
        
        // Sistema de Gathering/Recursos
        ResourcesSync = 18,
        ResourceHit = 19,
        ResourceUpdate = 20,
        ResourceDestroyed = 21,
        ResourceRespawn = 22,
        GatherResult = 23,
        
        // ⭐ NOVO: Sistema de Crafting
        RecipesSync = 24,          // Servidor -> Cliente (envia todas as receitas)
        CraftRequest = 25,         // Cliente -> Servidor (solicita crafting)
        CraftStarted = 26,         // Servidor -> Cliente (crafting iniciado)
        CraftComplete = 27,        // Servidor -> Cliente (crafting completo)
        CraftCancel = 28,          // Cliente -> Servidor (cancela crafting)
        CraftQueueUpdate = 29,
        
        // ⭐ NOVO: Sistema de Combate
        MeleeAttack = 30,
        RangedAttack = 31,
        AttackResult = 32,
        WeaponEquip = 33,
        WeaponReload = 34,
        TakeDamageNotify = 35,
        PlayerKilled = 36,
        RespawnRequest = 37,
        RespawnResponse = 38,
        WeaponStateUpdate = 39,
        CombatLog = 40
		}

    // Classe base para serialização de pacotes
    public class Packet
    {
        public PacketType Type { get; set; }
        public byte[] Data { get; set; }

        public Packet(PacketType type, byte[] data)
        {
            Type = type;
            Data = data;
        }

        public byte[] Serialize()
        {
            byte[] result = new byte[1 + 4 + Data.Length];
            result[0] = (byte)Type;
            BitConverter.GetBytes(Data.Length).CopyTo(result, 1);
            Data.CopyTo(result, 5);
            return result;
        }

        public static Packet Deserialize(byte[] data)
        {
            if (data.Length < 5) return null;
            
            PacketType type = (PacketType)data[0];
            int dataLength = BitConverter.ToInt32(data, 1);
            byte[] packetData = new byte[dataLength];
            Array.Copy(data, 5, packetData, 0, dataLength);
            
            return new Packet(type, packetData);
        }
    }

    // ==================== PACOTES EXISTENTES ====================

    public class ConnectionRequestPacket
    {
        public string PlayerName { get; set; }

        public byte[] Serialize()
        {
            return Encoding.UTF8.GetBytes(PlayerName);
        }

        public static ConnectionRequestPacket Deserialize(byte[] data)
        {
            return new ConnectionRequestPacket
            {
                PlayerName = Encoding.UTF8.GetString(data)
            };
        }
    }

    public class ConnectionAcceptPacket
    {
        public int PlayerId { get; set; }
        public float SpawnX { get; set; }
        public float SpawnY { get; set; }
        public float SpawnZ { get; set; }

        public byte[] Serialize()
        {
            byte[] data = new byte[16];
            BitConverter.GetBytes(PlayerId).CopyTo(data, 0);
            BitConverter.GetBytes(SpawnX).CopyTo(data, 4);
            BitConverter.GetBytes(SpawnY).CopyTo(data, 8);
            BitConverter.GetBytes(SpawnZ).CopyTo(data, 12);
            return data;
        }

        public static ConnectionAcceptPacket Deserialize(byte[] data)
        {
            return new ConnectionAcceptPacket
            {
                PlayerId = BitConverter.ToInt32(data, 0),
                SpawnX = BitConverter.ToSingle(data, 4),
                SpawnY = BitConverter.ToSingle(data, 8),
                SpawnZ = BitConverter.ToSingle(data, 12)
            };
        }
    }

    public class PlayerMovementPacket
    {
        public int PlayerId { get; set; }
        public float PosX { get; set; }
        public float PosY { get; set; }
        public float PosZ { get; set; }
        public float RotX { get; set; }
        public float RotY { get; set; }

        public byte[] Serialize()
        {
            byte[] data = new byte[24];
            BitConverter.GetBytes(PlayerId).CopyTo(data, 0);
            BitConverter.GetBytes(PosX).CopyTo(data, 4);
            BitConverter.GetBytes(PosY).CopyTo(data, 8);
            BitConverter.GetBytes(PosZ).CopyTo(data, 12);
            BitConverter.GetBytes(RotX).CopyTo(data, 16);
            BitConverter.GetBytes(RotY).CopyTo(data, 20);
            return data;
        }

        public static PlayerMovementPacket Deserialize(byte[] data)
        {
            return new PlayerMovementPacket
            {
                PlayerId = BitConverter.ToInt32(data, 0),
                PosX = BitConverter.ToSingle(data, 4),
                PosY = BitConverter.ToSingle(data, 8),
                PosZ = BitConverter.ToSingle(data, 12),
                RotX = BitConverter.ToSingle(data, 16),
                RotY = BitConverter.ToSingle(data, 20)
            };
        }
    }

    public class PlayerSpawnPacket
    {
        public int PlayerId { get; set; }
        public string PlayerName { get; set; }
        public float PosX { get; set; }
        public float PosY { get; set; }
        public float PosZ { get; set; }

        public byte[] Serialize()
        {
            byte[] nameBytes = Encoding.UTF8.GetBytes(PlayerName);
            byte[] data = new byte[20 + nameBytes.Length];
            BitConverter.GetBytes(PlayerId).CopyTo(data, 0);
            BitConverter.GetBytes(nameBytes.Length).CopyTo(data, 4);
            nameBytes.CopyTo(data, 8);
            BitConverter.GetBytes(PosX).CopyTo(data, 8 + nameBytes.Length);
            BitConverter.GetBytes(PosY).CopyTo(data, 12 + nameBytes.Length);
            BitConverter.GetBytes(PosZ).CopyTo(data, 16 + nameBytes.Length);
            return data;
        }

        public static PlayerSpawnPacket Deserialize(byte[] data)
        {
            int nameLength = BitConverter.ToInt32(data, 4);
            return new PlayerSpawnPacket
            {
                PlayerId = BitConverter.ToInt32(data, 0),
                PlayerName = Encoding.UTF8.GetString(data, 8, nameLength),
                PosX = BitConverter.ToSingle(data, 8 + nameLength),
                PosY = BitConverter.ToSingle(data, 12 + nameLength),
                PosZ = BitConverter.ToSingle(data, 16 + nameLength)
            };
        }
    }

    public class StatsUpdatePacket
    {
        public int PlayerId { get; set; }
        public float Health { get; set; }
        public float Hunger { get; set; }
        public float Thirst { get; set; }
        public float Temperature { get; set; }

        public byte[] Serialize()
        {
            byte[] data = new byte[20];
            BitConverter.GetBytes(PlayerId).CopyTo(data, 0);
            BitConverter.GetBytes(Health).CopyTo(data, 4);
            BitConverter.GetBytes(Hunger).CopyTo(data, 8);
            BitConverter.GetBytes(Thirst).CopyTo(data, 12);
            BitConverter.GetBytes(Temperature).CopyTo(data, 16);
            return data;
        }

        public static StatsUpdatePacket Deserialize(byte[] data)
        {
            return new StatsUpdatePacket
            {
                PlayerId = BitConverter.ToInt32(data, 0),
                Health = BitConverter.ToSingle(data, 4),
                Hunger = BitConverter.ToSingle(data, 8),
                Thirst = BitConverter.ToSingle(data, 12),
                Temperature = BitConverter.ToSingle(data, 16)
            };
        }
    }

    public class PlayerDeathPacket
    {
        public int PlayerId { get; set; }
        public string KillerName { get; set; }

        public byte[] Serialize()
        {
            byte[] nameBytes = Encoding.UTF8.GetBytes(KillerName ?? "");
            byte[] data = new byte[8 + nameBytes.Length];
            BitConverter.GetBytes(PlayerId).CopyTo(data, 0);
            BitConverter.GetBytes(nameBytes.Length).CopyTo(data, 4);
            nameBytes.CopyTo(data, 8);
            return data;
        }

        public static PlayerDeathPacket Deserialize(byte[] data)
        {
            int nameLength = BitConverter.ToInt32(data, 4);
            return new PlayerDeathPacket
            {
                PlayerId = BitConverter.ToInt32(data, 0),
                KillerName = nameLength > 0 ? Encoding.UTF8.GetString(data, 8, nameLength) : ""
            };
        }
    }

    public class InventoryUpdatePacket
    {
        public List<InventorySlotData> Slots { get; set; }

        public InventoryUpdatePacket()
        {
            Slots = new List<InventorySlotData>();
        }

        public byte[] Serialize()
        {
            List<byte> data = new List<byte>();
            data.AddRange(BitConverter.GetBytes(Slots.Count));

            foreach (var slot in Slots)
            {
                data.AddRange(BitConverter.GetBytes(slot.SlotIndex));
                data.AddRange(BitConverter.GetBytes(slot.ItemId));
                data.AddRange(BitConverter.GetBytes(slot.Quantity));
            }

            return data.ToArray();
        }

        public static InventoryUpdatePacket Deserialize(byte[] data)
        {
            var packet = new InventoryUpdatePacket();
            int offset = 0;

            int slotCount = BitConverter.ToInt32(data, offset);
            offset += 4;

            for (int i = 0; i < slotCount; i++)
            {
                int slotIndex = BitConverter.ToInt32(data, offset);
                offset += 4;
                int itemId = BitConverter.ToInt32(data, offset);
                offset += 4;
                int quantity = BitConverter.ToInt32(data, offset);
                offset += 4;

                packet.Slots.Add(new InventorySlotData
                {
                    SlotIndex = slotIndex,
                    ItemId = itemId,
                    Quantity = quantity
                });
            }

            return packet;
        }
    }

    public class InventorySlotData
    {
        public int SlotIndex { get; set; }
        public int ItemId { get; set; }
        public int Quantity { get; set; }
    }

    public class ItemUsePacket
    {
        public int SlotIndex { get; set; }

        public byte[] Serialize()
        {
            return BitConverter.GetBytes(SlotIndex);
        }

        public static ItemUsePacket Deserialize(byte[] data)
        {
            return new ItemUsePacket
            {
                SlotIndex = BitConverter.ToInt32(data, 0)
            };
        }
    }

    public class ItemMovePacket
    {
        public int FromSlot { get; set; }
        public int ToSlot { get; set; }

        public byte[] Serialize()
        {
            byte[] data = new byte[8];
            BitConverter.GetBytes(FromSlot).CopyTo(data, 0);
            BitConverter.GetBytes(ToSlot).CopyTo(data, 4);
            return data;
        }

        public static ItemMovePacket Deserialize(byte[] data)
        {
            return new ItemMovePacket
            {
                FromSlot = BitConverter.ToInt32(data, 0),
                ToSlot = BitConverter.ToInt32(data, 4)
            };
        }
    }

    // ==================== PACOTES DE RECURSOS ====================

    public class ResourcesSyncPacket
    {
        public List<ResourceData> Resources { get; set; }

        public ResourcesSyncPacket()
        {
            Resources = new List<ResourceData>();
        }

        public byte[] Serialize()
        {
            List<byte> data = new List<byte>();
            data.AddRange(BitConverter.GetBytes(Resources.Count));

            foreach (var res in Resources)
            {
                data.AddRange(BitConverter.GetBytes(res.Id));
                data.Add((byte)res.Type);
                data.AddRange(BitConverter.GetBytes(res.PosX));
                data.AddRange(BitConverter.GetBytes(res.PosY));
                data.AddRange(BitConverter.GetBytes(res.PosZ));
                data.AddRange(BitConverter.GetBytes(res.Health));
                data.AddRange(BitConverter.GetBytes(res.MaxHealth));
            }

            return data.ToArray();
        }

        public static ResourcesSyncPacket Deserialize(byte[] data)
        {
            var packet = new ResourcesSyncPacket();
            int offset = 0;

            int count = BitConverter.ToInt32(data, offset);
            offset += 4;

            for (int i = 0; i < count; i++)
            {
                packet.Resources.Add(new ResourceData
                {
                    Id = BitConverter.ToInt32(data, offset),
                    Type = data[offset + 4],
                    PosX = BitConverter.ToSingle(data, offset + 5),
                    PosY = BitConverter.ToSingle(data, offset + 9),
                    PosZ = BitConverter.ToSingle(data, offset + 13),
                    Health = BitConverter.ToSingle(data, offset + 17),
                    MaxHealth = BitConverter.ToSingle(data, offset + 21)
                });
                offset += 25;
            }

            return packet;
        }
    }

    public class ResourceData
    {
        public int Id { get; set; }
        public byte Type { get; set; }
        public float PosX { get; set; }
        public float PosY { get; set; }
        public float PosZ { get; set; }
        public float Health { get; set; }
        public float MaxHealth { get; set; }
    }

    public class ResourceHitPacket
    {
        public int ResourceId { get; set; }
        public float Damage { get; set; }
        public int ToolType { get; set; }

        public byte[] Serialize()
        {
            byte[] data = new byte[12];
            BitConverter.GetBytes(ResourceId).CopyTo(data, 0);
            BitConverter.GetBytes(Damage).CopyTo(data, 4);
            BitConverter.GetBytes(ToolType).CopyTo(data, 8);
            return data;
        }

        public static ResourceHitPacket Deserialize(byte[] data)
        {
            return new ResourceHitPacket
            {
                ResourceId = BitConverter.ToInt32(data, 0),
                Damage = BitConverter.ToSingle(data, 4),
                ToolType = BitConverter.ToInt32(data, 8)
            };
        }
    }

    public class ResourceUpdatePacket
    {
        public int ResourceId { get; set; }
        public float Health { get; set; }
        public float MaxHealth { get; set; }

        public byte[] Serialize()
        {
            byte[] data = new byte[12];
            BitConverter.GetBytes(ResourceId).CopyTo(data, 0);
            BitConverter.GetBytes(Health).CopyTo(data, 4);
            BitConverter.GetBytes(MaxHealth).CopyTo(data, 8);
            return data;
        }

        public static ResourceUpdatePacket Deserialize(byte[] data)
        {
            return new ResourceUpdatePacket
            {
                ResourceId = BitConverter.ToInt32(data, 0),
                Health = BitConverter.ToSingle(data, 4),
                MaxHealth = BitConverter.ToSingle(data, 8)
            };
        }
    }

    public class ResourceDestroyedPacket
    {
        public int ResourceId { get; set; }

        public byte[] Serialize()
        {
            return BitConverter.GetBytes(ResourceId);
        }

        public static ResourceDestroyedPacket Deserialize(byte[] data)
        {
            return new ResourceDestroyedPacket
            {
                ResourceId = BitConverter.ToInt32(data, 0)
            };
        }
    }

    public class ResourceRespawnPacket
    {
        public int ResourceId { get; set; }
        public float Health { get; set; }
        public float MaxHealth { get; set; }

        public byte[] Serialize()
        {
            byte[] data = new byte[12];
            BitConverter.GetBytes(ResourceId).CopyTo(data, 0);
            BitConverter.GetBytes(Health).CopyTo(data, 4);
            BitConverter.GetBytes(MaxHealth).CopyTo(data, 8);
            return data;
        }

        public static ResourceRespawnPacket Deserialize(byte[] data)
        {
            return new ResourceRespawnPacket
            {
                ResourceId = BitConverter.ToInt32(data, 0),
                Health = BitConverter.ToSingle(data, 4),
                MaxHealth = BitConverter.ToSingle(data, 8)
            };
        }
    }

    public class GatherResultPacket
    {
        public int WoodGained { get; set; }
        public int StoneGained { get; set; }
        public int MetalGained { get; set; }
        public int SulfurGained { get; set; }

        public byte[] Serialize()
        {
            byte[] data = new byte[16];
            BitConverter.GetBytes(WoodGained).CopyTo(data, 0);
            BitConverter.GetBytes(StoneGained).CopyTo(data, 4);
            BitConverter.GetBytes(MetalGained).CopyTo(data, 8);
            BitConverter.GetBytes(SulfurGained).CopyTo(data, 12);
            return data;
        }

        public static GatherResultPacket Deserialize(byte[] data)
        {
            return new GatherResultPacket
            {
                WoodGained = BitConverter.ToInt32(data, 0),
                StoneGained = BitConverter.ToInt32(data, 4),
                MetalGained = BitConverter.ToInt32(data, 8),
                SulfurGained = BitConverter.ToInt32(data, 12)
            };
        }
    }

    // ==================== ⭐ NOVOS PACOTES DE CRAFTING ====================

    /// <summary>
    /// Envia todas as receitas para o cliente
    /// </summary>
    public class RecipesSyncPacket
    {
        public List<RecipeData> Recipes { get; set; }

        public RecipesSyncPacket()
        {
            Recipes = new List<RecipeData>();
        }

        public byte[] Serialize()
        {
            List<byte> data = new List<byte>();
            data.AddRange(BitConverter.GetBytes(Recipes.Count));

            foreach (var recipe in Recipes)
            {
                // Recipe ID
                data.AddRange(BitConverter.GetBytes(recipe.Id));
                
                // Recipe Name
                byte[] nameBytes = Encoding.UTF8.GetBytes(recipe.Name);
                data.AddRange(BitConverter.GetBytes(nameBytes.Length));
                data.AddRange(nameBytes);
                
                // Result Item
                data.AddRange(BitConverter.GetBytes(recipe.ResultItemId));
                data.AddRange(BitConverter.GetBytes(recipe.ResultQuantity));
                
                // Crafting Time
                data.AddRange(BitConverter.GetBytes(recipe.CraftingTime));
                
                // Required Workbench
                data.AddRange(BitConverter.GetBytes(recipe.RequiredWorkbench));
                
                // Ingredients
                data.AddRange(BitConverter.GetBytes(recipe.Ingredients.Count));
                foreach (var ingredient in recipe.Ingredients)
                {
                    data.AddRange(BitConverter.GetBytes(ingredient.ItemId));
                    data.AddRange(BitConverter.GetBytes(ingredient.Quantity));
                }
            }

            return data.ToArray();
        }

        public static RecipesSyncPacket Deserialize(byte[] data)
        {
            var packet = new RecipesSyncPacket();
            int offset = 0;

            int recipeCount = BitConverter.ToInt32(data, offset);
            offset += 4;

            for (int i = 0; i < recipeCount; i++)
            {
                var recipe = new RecipeData();
                
                recipe.Id = BitConverter.ToInt32(data, offset);
                offset += 4;
                
                int nameLength = BitConverter.ToInt32(data, offset);
                offset += 4;
                recipe.Name = Encoding.UTF8.GetString(data, offset, nameLength);
                offset += nameLength;
                
                recipe.ResultItemId = BitConverter.ToInt32(data, offset);
                offset += 4;
                recipe.ResultQuantity = BitConverter.ToInt32(data, offset);
                offset += 4;
                
                recipe.CraftingTime = BitConverter.ToSingle(data, offset);
                offset += 4;
                
                recipe.RequiredWorkbench = BitConverter.ToInt32(data, offset);
                offset += 4;
                
                int ingredientCount = BitConverter.ToInt32(data, offset);
                offset += 4;
                
                for (int j = 0; j < ingredientCount; j++)
                {
                    int itemId = BitConverter.ToInt32(data, offset);
                    offset += 4;
                    int quantity = BitConverter.ToInt32(data, offset);
                    offset += 4;
                    
                    recipe.Ingredients.Add(new IngredientData
                    {
                        ItemId = itemId,
                        Quantity = quantity
                    });
                }
                
                packet.Recipes.Add(recipe);
            }

            return packet;
        }
    }

    public class RecipeData
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int ResultItemId { get; set; }
        public int ResultQuantity { get; set; }
        public float CraftingTime { get; set; }
        public int RequiredWorkbench { get; set; }
        public List<IngredientData> Ingredients { get; set; }

        public RecipeData()
        {
            Ingredients = new List<IngredientData>();
        }
    }

    public class IngredientData
    {
        public int ItemId { get; set; }
        public int Quantity { get; set; }
    }

    /// <summary>
    /// Cliente solicita crafting de uma receita
    /// </summary>
    public class CraftRequestPacket
    {
        public int RecipeId { get; set; }

        public byte[] Serialize()
        {
            return BitConverter.GetBytes(RecipeId);
        }

        public static CraftRequestPacket Deserialize(byte[] data)
        {
            return new CraftRequestPacket
            {
                RecipeId = BitConverter.ToInt32(data, 0)
            };
        }
    }

    /// <summary>
    /// Servidor confirma início do crafting
    /// </summary>
    public class CraftStartedPacket
    {
        public int RecipeId { get; set; }
        public float Duration { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }

        public byte[] Serialize()
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes(Message ?? "");
            byte[] data = new byte[13 + messageBytes.Length];
            
            BitConverter.GetBytes(RecipeId).CopyTo(data, 0);
            BitConverter.GetBytes(Duration).CopyTo(data, 4);
            data[8] = Success ? (byte)1 : (byte)0;
            BitConverter.GetBytes(messageBytes.Length).CopyTo(data, 9);
            messageBytes.CopyTo(data, 13);
            
            return data;
        }

        public static CraftStartedPacket Deserialize(byte[] data)
        {
            int messageLength = BitConverter.ToInt32(data, 9);
            return new CraftStartedPacket
            {
                RecipeId = BitConverter.ToInt32(data, 0),
                Duration = BitConverter.ToSingle(data, 4),
                Success = data[8] == 1,
                Message = messageLength > 0 ? Encoding.UTF8.GetString(data, 13, messageLength) : ""
            };
        }
    }

    /// <summary>
    /// Servidor notifica que crafting foi completo
    /// </summary>
    public class CraftCompletePacket
    {
        public int RecipeId { get; set; }
        public int ResultItemId { get; set; }
        public int ResultQuantity { get; set; }

        public byte[] Serialize()
        {
            byte[] data = new byte[12];
            BitConverter.GetBytes(RecipeId).CopyTo(data, 0);
            BitConverter.GetBytes(ResultItemId).CopyTo(data, 4);
            BitConverter.GetBytes(ResultQuantity).CopyTo(data, 8);
            return data;
        }

        public static CraftCompletePacket Deserialize(byte[] data)
        {
            return new CraftCompletePacket
            {
                RecipeId = BitConverter.ToInt32(data, 0),
                ResultItemId = BitConverter.ToInt32(data, 4),
                ResultQuantity = BitConverter.ToInt32(data, 8)
            };
        }
    }

    /// <summary>
    /// Cliente solicita cancelamento de crafting
    /// </summary>
    public class CraftCancelPacket
    {
        public int QueueIndex { get; set; }

        public byte[] Serialize()
        {
            return BitConverter.GetBytes(QueueIndex);
        }

        public static CraftCancelPacket Deserialize(byte[] data)
        {
            return new CraftCancelPacket
            {
                QueueIndex = BitConverter.ToInt32(data, 0)
            };
        }
    }

    /// <summary>
    /// Servidor envia atualização da fila de crafting
    /// </summary>
    public class CraftQueueUpdatePacket
    {
        public List<CraftQueueItem> QueueItems { get; set; }

        public CraftQueueUpdatePacket()
        {
            QueueItems = new List<CraftQueueItem>();
        }

        public byte[] Serialize()
        {
            List<byte> data = new List<byte>();
            data.AddRange(BitConverter.GetBytes(QueueItems.Count));

            foreach (var item in QueueItems)
            {
                data.AddRange(BitConverter.GetBytes(item.RecipeId));
                data.AddRange(BitConverter.GetBytes(item.Progress));
                data.AddRange(BitConverter.GetBytes(item.RemainingTime));
            }

            return data.ToArray();
        }

        public static CraftQueueUpdatePacket Deserialize(byte[] data)
        {
            var packet = new CraftQueueUpdatePacket();
            int offset = 0;

            int count = BitConverter.ToInt32(data, offset);
            offset += 4;

            for (int i = 0; i < count; i++)
            {
                packet.QueueItems.Add(new CraftQueueItem
                {
                    RecipeId = BitConverter.ToInt32(data, offset),
                    Progress = BitConverter.ToSingle(data, offset + 4),
                    RemainingTime = BitConverter.ToSingle(data, offset + 8)
                });
                offset += 12;
            }

            return packet;
        }
    }
	/// <summary>
/// Servidor confirma respawn
/// </summary>
[Serializable]
public class RespawnResponsePacket
{
    public bool Success;
    public float SpawnX;
    public float SpawnY;
    public float SpawnZ;
    public string Message;

    public byte[] Serialize()
    {
        byte[] messageBytes = Encoding.UTF8.GetBytes(Message ?? "");
        byte[] data = new byte[17 + messageBytes.Length];
        
        data[0] = Success ? (byte)1 : (byte)0;
        BitConverter.GetBytes(SpawnX).CopyTo(data, 1);
        BitConverter.GetBytes(SpawnY).CopyTo(data, 5);
        BitConverter.GetBytes(SpawnZ).CopyTo(data, 9);
        BitConverter.GetBytes(messageBytes.Length).CopyTo(data, 13);
        messageBytes.CopyTo(data, 17);
        
        return data;
    }

    public static RespawnResponsePacket Deserialize(byte[] data)
    {
        bool success = data[0] == 1;
        float spawnX = BitConverter.ToSingle(data, 1);
        float spawnY = BitConverter.ToSingle(data, 5);
        float spawnZ = BitConverter.ToSingle(data, 9);
        int messageLength = BitConverter.ToInt32(data, 13);
        string message = messageLength > 0 ? Encoding.UTF8.GetString(data, 17, messageLength) : "";
        
        return new RespawnResponsePacket
        {
            Success = success,
            SpawnX = spawnX,
            SpawnY = spawnY,
            SpawnZ = spawnZ,
            Message = message
        };
    }
}

    public class CraftQueueItem
    {
        public int RecipeId { get; set; }
        public float Progress { get; set; }       // 0.0 a 1.0
        public float RemainingTime { get; set; }  // Em segundos
    }

}