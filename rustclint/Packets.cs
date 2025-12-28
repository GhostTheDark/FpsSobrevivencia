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
        
        // ⭐ NOVO: Sistema de Gathering/Recursos
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
		CraftQueueUpdate = 29
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

    // ⭐ NOVO: Pacotes de Gathering/Recursos

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
        public byte Type; // 0=Tree, 1=Stone, 2=Metal, 3=Sulfur
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
        public int ToolType; // 0=Mão, 1=Machado, 2=Picareta

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
	/// <summary>
/// Cliente solicita crafting de uma receita
/// </summary>
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

/// <summary>
/// Servidor confirma início do crafting
/// </summary>
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

/// <summary>
/// Servidor notifica que crafting foi completo
/// </summary>
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

/// <summary>
/// Cliente solicita cancelamento de crafting
/// </summary>
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

/// <summary>
/// Servidor envia sincronização de receitas
/// </summary>
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

/// <summary>
/// Servidor envia atualização da fila de crafting
/// </summary>
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
}