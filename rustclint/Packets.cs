using System;
using System.Text;
using UnityEngine;
using System.Collections.Generic;

namespace RustlikeClient.Network
{
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
        
        // Sistema de Crafting
        RecipesSync = 24,
        CraftRequest = 25,
        CraftStarted = 26,
        CraftComplete = 27,
        CraftCancel = 28,
        CraftQueueUpdate = 29,
        
        // ⭐ Sistema de Combate
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

    // ==================== CONEXÃO ====================

    [Serializable]
    public class ConnectionRequestPacket
    {
        public string PlayerName;

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

    [Serializable]
    public class ConnectionAcceptPacket
    {
        public int PlayerId;
        public Vector3 SpawnPosition;

        public byte[] Serialize()
        {
            byte[] data = new byte[16];
            BitConverter.GetBytes(PlayerId).CopyTo(data, 0);
            BitConverter.GetBytes(SpawnPosition.x).CopyTo(data, 4);
            BitConverter.GetBytes(SpawnPosition.y).CopyTo(data, 8);
            BitConverter.GetBytes(SpawnPosition.z).CopyTo(data, 12);
            return data;
        }

        public static ConnectionAcceptPacket Deserialize(byte[] data)
        {
            return new ConnectionAcceptPacket
            {
                PlayerId = BitConverter.ToInt32(data, 0),
                SpawnPosition = new Vector3(
                    BitConverter.ToSingle(data, 4),
                    BitConverter.ToSingle(data, 8),
                    BitConverter.ToSingle(data, 12)
                )
            };
        }
    }

    // ==================== MOVIMENTO ====================

    [Serializable]
    public class PlayerMovementPacket
    {
        public int PlayerId;
        public Vector3 Position;
        public Vector2 Rotation;

        public byte[] Serialize()
        {
            byte[] data = new byte[24];
            BitConverter.GetBytes(PlayerId).CopyTo(data, 0);
            BitConverter.GetBytes(Position.x).CopyTo(data, 4);
            BitConverter.GetBytes(Position.y).CopyTo(data, 8);
            BitConverter.GetBytes(Position.z).CopyTo(data, 12);
            BitConverter.GetBytes(Rotation.x).CopyTo(data, 16);
            BitConverter.GetBytes(Rotation.y).CopyTo(data, 20);
            return data;
        }

        public static PlayerMovementPacket Deserialize(byte[] data)
        {
            return new PlayerMovementPacket
            {
                PlayerId = BitConverter.ToInt32(data, 0),
                Position = new Vector3(
                    BitConverter.ToSingle(data, 4),
                    BitConverter.ToSingle(data, 8),
                    BitConverter.ToSingle(data, 12)
                ),
                Rotation = new Vector2(
                    BitConverter.ToSingle(data, 16),
                    BitConverter.ToSingle(data, 20)
                )
            };
        }
    }

    [Serializable]
    public class PlayerSpawnPacket
    {
        public int PlayerId;
        public string PlayerName;
        public Vector3 Position;

        public byte[] Serialize()
        {
            byte[] nameBytes = Encoding.UTF8.GetBytes(PlayerName);
            byte[] data = new byte[20 + nameBytes.Length];
            BitConverter.GetBytes(PlayerId).CopyTo(data, 0);
            BitConverter.GetBytes(nameBytes.Length).CopyTo(data, 4);
            nameBytes.CopyTo(data, 8);
            BitConverter.GetBytes(Position.x).CopyTo(data, 8 + nameBytes.Length);
            BitConverter.GetBytes(Position.y).CopyTo(data, 12 + nameBytes.Length);
            BitConverter.GetBytes(Position.z).CopyTo(data, 16 + nameBytes.Length);
            return data;
        }

        public static PlayerSpawnPacket Deserialize(byte[] data)
        {
            int nameLength = BitConverter.ToInt32(data, 4);
            return new PlayerSpawnPacket
            {
                PlayerId = BitConverter.ToInt32(data, 0),
                PlayerName = Encoding.UTF8.GetString(data, 8, nameLength),
                Position = new Vector3(
                    BitConverter.ToSingle(data, 8 + nameLength),
                    BitConverter.ToSingle(data, 12 + nameLength),
                    BitConverter.ToSingle(data, 16 + nameLength)
                )
            };
        }
    }

    // ==================== STATS ====================

    [Serializable]
    public class StatsUpdatePacket
    {
        public int PlayerId;
        public float Health;
        public float Hunger;
        public float Thirst;
        public float Temperature;

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

    [Serializable]
    public class PlayerDeathPacket
    {
        public int PlayerId;
        public string KillerName;

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

    // ==================== INVENTÁRIO ====================

    [Serializable]
    public class InventoryUpdatePacket
    {
        public List<InventorySlotData> Slots;

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

    [Serializable]
    public class InventorySlotData
    {
        public int SlotIndex;
        public int ItemId;
        public int Quantity;
    }

    [Serializable]
    public class ItemUsePacket
    {
        public int SlotIndex;

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

    [Serializable]
    public class ItemMovePacket
    {
        public int FromSlot;
        public int ToSlot;

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

    // ==================== RECURSOS (GATHERING) ====================

    [Serializable]
    public class ResourcesSyncPacket
    {
        public List<ResourceData> Resources;

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
                data.Add(res.Type);
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

    [Serializable]
    public class ResourceData
    {
        public int Id;
        public byte Type;
        public float PosX;
        public float PosY;
        public float PosZ;
        public float Health;
        public float MaxHealth;
    }

    [Serializable]
    public class ResourceHitPacket
    {
        public int ResourceId;
        public float Damage;
        public int ToolType;

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

    [Serializable]
    public class ResourceUpdatePacket
    {
        public int ResourceId;
        public float Health;
        public float MaxHealth;

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

    [Serializable]
    public class ResourceDestroyedPacket
    {
        public int ResourceId;

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

    [Serializable]
    public class ResourceRespawnPacket
    {
        public int ResourceId;
        public float Health;
        public float MaxHealth;

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

    [Serializable]
    public class GatherResultPacket
    {
        public int WoodGained;
        public int StoneGained;
        public int MetalGained;
        public int SulfurGained;

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

    // ==================== CRAFTING ====================

    [Serializable]
    public class RecipesSyncPacket
    {
        public List<RecipeData> Recipes;

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
                data.AddRange(BitConverter.GetBytes(recipe.Id));
                
                byte[] nameBytes = Encoding.UTF8.GetBytes(recipe.Name);
                data.AddRange(BitConverter.GetBytes(nameBytes.Length));
                data.AddRange(nameBytes);
                
                data.AddRange(BitConverter.GetBytes(recipe.ResultItemId));
                data.AddRange(BitConverter.GetBytes(recipe.ResultQuantity));
                data.AddRange(BitConverter.GetBytes(recipe.CraftingTime));
                data.AddRange(BitConverter.GetBytes(recipe.RequiredWorkbench));
                
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

    [Serializable]
    public class RecipeData
    {
        public int Id;
        public string Name;
        public int ResultItemId;
        public int ResultQuantity;
        public float CraftingTime;
        public int RequiredWorkbench;
        public List<IngredientData> Ingredients;

        public RecipeData()
        {
            Ingredients = new List<IngredientData>();
        }
    }

    [Serializable]
    public class IngredientData
    {
        public int ItemId;
        public int Quantity;
    }

    [Serializable]
    public class CraftRequestPacket
    {
        public int RecipeId;

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

    [Serializable]
    public class CraftStartedPacket
    {
        public int RecipeId;
        public float Duration;
        public bool Success;
        public string Message;

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

    [Serializable]
    public class CraftCompletePacket
    {
        public int RecipeId;
        public int ResultItemId;
        public int ResultQuantity;

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

    [Serializable]
    public class CraftCancelPacket
    {
        public int QueueIndex;

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

    [Serializable]
    public class CraftQueueUpdatePacket
    {
        public List<CraftQueueItem> QueueItems;

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

    [Serializable]
    public class CraftQueueItem
    {
        public int RecipeId;
        public float Progress;
        public float RemainingTime;
    }

    // ==================== ⭐ COMBATE ====================

    [Serializable]
    public class MeleeAttackPacket
    {
        public int TargetPlayerId;
        public int WeaponItemId;
        public byte Hitbox;
        public float DirectionX;
        public float DirectionY;
        public float DirectionZ;

        public byte[] Serialize()
        {
            byte[] data = new byte[21];
            BitConverter.GetBytes(TargetPlayerId).CopyTo(data, 0);
            BitConverter.GetBytes(WeaponItemId).CopyTo(data, 4);
            data[8] = Hitbox;
            BitConverter.GetBytes(DirectionX).CopyTo(data, 9);
            BitConverter.GetBytes(DirectionY).CopyTo(data, 13);
            BitConverter.GetBytes(DirectionZ).CopyTo(data, 17);
            return data;
        }

        public static MeleeAttackPacket Deserialize(byte[] data)
        {
            return new MeleeAttackPacket
            {
                TargetPlayerId = BitConverter.ToInt32(data, 0),
                WeaponItemId = BitConverter.ToInt32(data, 4),
                Hitbox = data[8],
                DirectionX = BitConverter.ToSingle(data, 9),
                DirectionY = BitConverter.ToSingle(data, 13),
                DirectionZ = BitConverter.ToSingle(data, 17)
            };
        }
    }

    [Serializable]
    public class RangedAttackPacket
    {
        public int TargetPlayerId;
        public int WeaponItemId;
        public byte Hitbox;
        public float ShootDirectionX;
        public float ShootDirectionY;
        public float ShootDirectionZ;
        public float Distance;

        public byte[] Serialize()
        {
            byte[] data = new byte[25];
            BitConverter.GetBytes(TargetPlayerId).CopyTo(data, 0);
            BitConverter.GetBytes(WeaponItemId).CopyTo(data, 4);
            data[8] = Hitbox;
            BitConverter.GetBytes(ShootDirectionX).CopyTo(data, 9);
            BitConverter.GetBytes(ShootDirectionY).CopyTo(data, 13);
            BitConverter.GetBytes(ShootDirectionZ).CopyTo(data, 17);
            BitConverter.GetBytes(Distance).CopyTo(data, 21);
            return data;
        }

        public static RangedAttackPacket Deserialize(byte[] data)
        {
            return new RangedAttackPacket
            {
                TargetPlayerId = BitConverter.ToInt32(data, 0),
                WeaponItemId = BitConverter.ToInt32(data, 4),
                Hitbox = data[8],
                ShootDirectionX = BitConverter.ToSingle(data, 9),
                ShootDirectionY = BitConverter.ToSingle(data, 13),
                ShootDirectionZ = BitConverter.ToSingle(data, 17),
                Distance = BitConverter.ToSingle(data, 21)
            };
        }
    }

    [Serializable]
    public class AttackResultPacket
    {
        public bool Success;
        public string Message;
        public float DamageDealt;
        public bool WasKilled;
        public byte Hitbox;
        public float Distance;
        public int RemainingAmmo;

        public byte[] Serialize()
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes(Message ?? "");
            byte[] data = new byte[19 + messageBytes.Length];
            
            data[0] = Success ? (byte)1 : (byte)0;
            
            BitConverter.GetBytes(messageBytes.Length).CopyTo(data, 1);
            messageBytes.CopyTo(data, 5);
            
            int offset = 5 + messageBytes.Length;
            BitConverter.GetBytes(DamageDealt).CopyTo(data, offset);
            data[offset + 4] = WasKilled ? (byte)1 : (byte)0;
            data[offset + 5] = Hitbox;
            BitConverter.GetBytes(Distance).CopyTo(data, offset + 6);
            BitConverter.GetBytes(RemainingAmmo).CopyTo(data, offset + 10);
            
            return data;
        }

        public static AttackResultPacket Deserialize(byte[] data)
        {
            bool success = data[0] == 1;
            
            int messageLength = BitConverter.ToInt32(data, 1);
            string message = messageLength > 0 ? Encoding.UTF8.GetString(data, 5, messageLength) : "";
            
            int offset = 5 + messageLength;
            
            return new AttackResultPacket
            {
                Success = success,
                Message = message,
                DamageDealt = BitConverter.ToSingle(data, offset),
                WasKilled = data[offset + 4] == 1,
                Hitbox = data[offset + 5],
                Distance = BitConverter.ToSingle(data, offset + 6),
                RemainingAmmo = BitConverter.ToInt32(data, offset + 10)
            };
        }
    }

    [Serializable]
    public class WeaponEquipPacket
    {
        public int WeaponItemId;
        public int SlotIndex;

        public byte[] Serialize()
        {
            byte[] data = new byte[8];
            BitConverter.GetBytes(WeaponItemId).CopyTo(data, 0);
            BitConverter.GetBytes(SlotIndex).CopyTo(data, 4);
            return data;
        }

        public static WeaponEquipPacket Deserialize(byte[] data)
        {
            return new WeaponEquipPacket
            {
                WeaponItemId = BitConverter.ToInt32(data, 0),
                SlotIndex = BitConverter.ToInt32(data, 4)
            };
        }
    }

    [Serializable]
    public class WeaponReloadPacket
    {
        public int WeaponItemId;

        public byte[] Serialize()
        {
            return BitConverter.GetBytes(WeaponItemId);
        }

        public static WeaponReloadPacket Deserialize(byte[] data)
        {
            return new WeaponReloadPacket
            {
                WeaponItemId = BitConverter.ToInt32(data, 0)
            };
        }
    }

    [Serializable]
    public class TakeDamageNotifyPacket
    {
        public int AttackerId;
        public float Damage;
        public byte DamageType;
        public byte Hitbox;
        public float DirectionX;
        public float DirectionY;
        public float DirectionZ;

        public byte[] Serialize()
        {
            byte[] data = new byte[22];
            BitConverter.GetBytes(AttackerId).CopyTo(data, 0);
            BitConverter.GetBytes(Damage).CopyTo(data, 4);
            data[8] = DamageType;
            data[9] = Hitbox;
            BitConverter.GetBytes(DirectionX).CopyTo(data, 10);
            BitConverter.GetBytes(DirectionY).CopyTo(data, 14);
            BitConverter.GetBytes(DirectionZ).CopyTo(data, 18);
            return data;
        }

        public static TakeDamageNotifyPacket Deserialize(byte[] data)
        {
            return new TakeDamageNotifyPacket
            {
                AttackerId = BitConverter.ToInt32(data, 0),
                Damage = BitConverter.ToSingle(data, 4),
                DamageType = data[8],
                Hitbox = data[9],
                DirectionX = BitConverter.ToSingle(data, 10),
                DirectionY = BitConverter.ToSingle(data, 14),
                DirectionZ = BitConverter.ToSingle(data, 18)
            };
        }
    }

    [Serializable]
    public class PlayerKilledPacket
    {
        public int VictimId;
        public int KillerId;
        public string KillerName;
        public string WeaponUsed;
        public byte Hitbox;
        public float Distance;

        public byte[] Serialize()
        {
            byte[] killerNameBytes = Encoding.UTF8.GetBytes(KillerName ?? "");
            byte[] weaponBytes = Encoding.UTF8.GetBytes(WeaponUsed ?? "");
            
            byte[] data = new byte[18 + killerNameBytes.Length + weaponBytes.Length];
            
            BitConverter.GetBytes(VictimId).CopyTo(data, 0);
            BitConverter.GetBytes(KillerId).CopyTo(data, 4);
            
            BitConverter.GetBytes(killerNameBytes.Length).CopyTo(data, 8);
            killerNameBytes.CopyTo(data, 12);
            
            int offset = 12 + killerNameBytes.Length;
            BitConverter.GetBytes(weaponBytes.Length).CopyTo(data, offset);
            weaponBytes.CopyTo(data, offset + 4);
            
            offset = offset + 4 + weaponBytes.Length;
            data[offset] = Hitbox;
            BitConverter.GetBytes(Distance).CopyTo(data, offset + 1);
            
            return data;
        }

        public static PlayerKilledPacket Deserialize(byte[] data)
        {
            int victimId = BitConverter.ToInt32(data, 0);
            int killerId = BitConverter.ToInt32(data, 4);
            
            int killerNameLength = BitConverter.ToInt32(data, 8);
            string killerName = killerNameLength > 0 ? Encoding.UTF8.GetString(data, 12, killerNameLength) : "";
            
            int offset = 12 + killerNameLength;
            int weaponLength = BitConverter.ToInt32(data, offset);
            string weaponUsed = weaponLength > 0 ? Encoding.UTF8.GetString(data, offset + 4, weaponLength) : "";
            
            offset = offset + 4 + weaponLength;
            byte hitbox = data[offset];
            float distance = BitConverter.ToSingle(data, offset + 1);
            
            return new PlayerKilledPacket
            {
                VictimId = victimId,
                KillerId = killerId,
                KillerName = killerName,
                WeaponUsed = weaponUsed,
                Hitbox = hitbox,
                Distance = distance
            };
        }
    }

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

    [Serializable]
    public class WeaponStateUpdatePacket
    {
        public int WeaponItemId;
        public int CurrentAmmo;
        public int ReserveAmmo;
        public bool IsReloading;
        public float ReloadProgress;

        public byte[] Serialize()
        {
            byte[] data = new byte[17];
            BitConverter.GetBytes(WeaponItemId).CopyTo(data, 0);
            BitConverter.GetBytes(CurrentAmmo).CopyTo(data, 4);
            BitConverter.GetBytes(ReserveAmmo).CopyTo(data, 8);
            data[12] = IsReloading ? (byte)1 : (byte)0;
            BitConverter.GetBytes(ReloadProgress).CopyTo(data, 13);
            return data;
        }

        public static WeaponStateUpdatePacket Deserialize(byte[] data)
        {
            return new WeaponStateUpdatePacket
            {
                WeaponItemId = BitConverter.ToInt32(data, 0),
                CurrentAmmo = BitConverter.ToInt32(data, 4),
                ReserveAmmo = BitConverter.ToInt32(data, 8),
                IsReloading = data[12] == 1,
                ReloadProgress = BitConverter.ToSingle(data, 13)
            };
        }
    }
}
