using System;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Tipos de mensagens de rede
/// </summary>
public enum MessageType : byte
{
    // Conexão
    AssignClientId = 0,
    ServerFull = 1,
    ClientDisconnect = 2,

    // Player
    PlayerSpawn = 10,
    PlayerMovement = 11,
    PlayerRotation = 12,
    PlayerAction = 13,
    PlayerDeath = 14,

    // Combate
    WeaponFire = 20,
    ProjectileSpawn = 21,
    ApplyDamage = 22,
    PlayerHit = 23,

    // Inventário
    InventoryUpdate = 30,
    ItemPickup = 31,
    ItemDrop = 32,
    ItemTransfer = 33,

    // Construção
    PlaceBuilding = 40,
    DestroyBuilding = 41,
    UpgradeBuilding = 42,
    BuildingDamage = 43,

    // Mundo
    ResourceHit = 50,
    ResourceDestroyed = 51,
    LootSpawn = 52,

    // Chat
    ChatMessage = 60,

    // Sincronização
    WorldState = 70,
    EntityUpdate = 71
}

/// <summary>
/// Mensagem de rede serializada
/// Server Authoritative - todas as ações importantes são validadas no servidor
/// </summary>
[Serializable]
public class NetworkMessage
{
    public MessageType type;
    public int clientId;
    public float timestamp;
    public byte[] data;

    public NetworkMessage()
    {
        timestamp = Time.time;
    }

    #region SERIALIZATION

    /// <summary>
    /// Serializa a mensagem para bytes
    /// </summary>
    public static byte[] Serialize(NetworkMessage message)
    {
        using (MemoryStream ms = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(ms))
        {
            writer.Write((byte)message.type);
            writer.Write(message.clientId);
            writer.Write(message.timestamp);
            
            if (message.data != null && message.data.Length > 0)
            {
                writer.Write(message.data.Length);
                writer.Write(message.data);
            }
            else
            {
                writer.Write(0);
            }

            return ms.ToArray();
        }
    }

    /// <summary>
    /// Desserializa bytes para mensagem
    /// </summary>
    public static NetworkMessage Deserialize(byte[] data, int length)
    {
        using (MemoryStream ms = new MemoryStream(data, 0, length))
        using (BinaryReader reader = new BinaryReader(ms))
        {
            NetworkMessage message = new NetworkMessage
            {
                type = (MessageType)reader.ReadByte(),
                clientId = reader.ReadInt32(),
                timestamp = reader.ReadSingle()
            };

            int dataLength = reader.ReadInt32();
            if (dataLength > 0)
            {
                message.data = reader.ReadBytes(dataLength);
            }

            return message;
        }
    }

    #endregion

    #region DATA HELPERS

    /// <summary>
    /// Define dados como Vector3
    /// </summary>
    public void SetVector3(Vector3 value)
    {
        using (MemoryStream ms = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(ms))
        {
            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.z);
            data = ms.ToArray();
        }
    }

    /// <summary>
    /// Lê dados como Vector3
    /// </summary>
    public Vector3 GetVector3()
    {
        if (data == null || data.Length < 12) return Vector3.zero;

        using (MemoryStream ms = new MemoryStream(data))
        using (BinaryReader reader = new BinaryReader(ms))
        {
            return new Vector3(
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle()
            );
        }
    }

    /// <summary>
    /// Define dados como Quaternion
    /// </summary>
    public void SetQuaternion(Quaternion value)
    {
        using (MemoryStream ms = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(ms))
        {
            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.z);
            writer.Write(value.w);
            data = ms.ToArray();
        }
    }

    /// <summary>
    /// Lê dados como Quaternion
    /// </summary>
    public Quaternion GetQuaternion()
    {
        if (data == null || data.Length < 16) return Quaternion.identity;

        using (MemoryStream ms = new MemoryStream(data))
        using (BinaryReader reader = new BinaryReader(ms))
        {
            return new Quaternion(
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle()
            );
        }
    }

    /// <summary>
    /// Define dados como string
    /// </summary>
    public void SetString(string value)
    {
        data = Encoding.UTF8.GetBytes(value);
    }

    /// <summary>
    /// Lê dados como string
    /// </summary>
    public string GetString()
    {
        if (data == null || data.Length == 0) return string.Empty;
        return Encoding.UTF8.GetString(data);
    }

    /// <summary>
    /// Define dados como int
    /// </summary>
    public void SetInt(int value)
    {
        data = BitConverter.GetBytes(value);
    }

    /// <summary>
    /// Lê dados como int
    /// </summary>
    public int GetInt()
    {
        if (data == null || data.Length < 4) return 0;
        return BitConverter.ToInt32(data, 0);
    }

    /// <summary>
    /// Define dados como float
    /// </summary>
    public void SetFloat(float value)
    {
        data = BitConverter.GetBytes(value);
    }

    /// <summary>
    /// Lê dados como float
    /// </summary>
    public float GetFloat()
    {
        if (data == null || data.Length < 4) return 0f;
        return BitConverter.ToSingle(data, 0);
    }

    /// <summary>
    /// Define dados como bool
    /// </summary>
    public void SetBool(bool value)
    {
        data = BitConverter.GetBytes(value);
    }

    /// <summary>
    /// Lê dados como bool
    /// </summary>
    public bool GetBool()
    {
        if (data == null || data.Length < 1) return false;
        return BitConverter.ToBoolean(data, 0);
    }

    #endregion
}

/// <summary>
/// Dados de movimento do jogador
/// </summary>
[Serializable]
public struct PlayerMovementData
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 velocity;
    public bool isGrounded;
    public bool isCrouching;
    public bool isSprinting;

    public byte[] Serialize()
    {
        using (MemoryStream ms = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(ms))
        {
            // Position
            writer.Write(position.x);
            writer.Write(position.y);
            writer.Write(position.z);

            // Rotation (apenas Y para FPS)
            writer.Write(rotation.eulerAngles.y);

            // Velocity
            writer.Write(velocity.x);
            writer.Write(velocity.y);
            writer.Write(velocity.z);

            // States (compactados em 1 byte)
            byte state = 0;
            if (isGrounded) state |= 1;
            if (isCrouching) state |= 2;
            if (isSprinting) state |= 4;
            writer.Write(state);

            return ms.ToArray();
        }
    }

    public static PlayerMovementData Deserialize(byte[] data)
    {
        using (MemoryStream ms = new MemoryStream(data))
        using (BinaryReader reader = new BinaryReader(ms))
        {
            PlayerMovementData movement = new PlayerMovementData
            {
                position = new Vector3(
                    reader.ReadSingle(),
                    reader.ReadSingle(),
                    reader.ReadSingle()
                ),
                rotation = Quaternion.Euler(0, reader.ReadSingle(), 0),
                velocity = new Vector3(
                    reader.ReadSingle(),
                    reader.ReadSingle(),
                    reader.ReadSingle()
                )
            };

            byte state = reader.ReadByte();
            movement.isGrounded = (state & 1) != 0;
            movement.isCrouching = (state & 2) != 0;
            movement.isSprinting = (state & 4) != 0;

            return movement;
        }
    }
}

/// <summary>
/// Dados de combate
/// </summary>
[Serializable]
public struct CombatData
{
    public int attackerId;
    public int targetId;
    public float damage;
    public Vector3 hitPosition;
    public Vector3 hitNormal;
    public DamageType damageType;

    public byte[] Serialize()
    {
        using (MemoryStream ms = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(ms))
        {
            writer.Write(attackerId);
            writer.Write(targetId);
            writer.Write(damage);
            writer.Write(hitPosition.x);
            writer.Write(hitPosition.y);
            writer.Write(hitPosition.z);
            writer.Write(hitNormal.x);
            writer.Write(hitNormal.y);
            writer.Write(hitNormal.z);
            writer.Write((byte)damageType);

            return ms.ToArray();
        }
    }

    public static CombatData Deserialize(byte[] data)
    {
        using (MemoryStream ms = new MemoryStream(data))
        using (BinaryReader reader = new BinaryReader(ms))
        {
            return new CombatData
            {
                attackerId = reader.ReadInt32(),
                targetId = reader.ReadInt32(),
                damage = reader.ReadSingle(),
                hitPosition = new Vector3(
                    reader.ReadSingle(),
                    reader.ReadSingle(),
                    reader.ReadSingle()
                ),
                hitNormal = new Vector3(
                    reader.ReadSingle(),
                    reader.ReadSingle(),
                    reader.ReadSingle()
                ),
                damageType = (DamageType)reader.ReadByte()
            };
        }
    }
}

/// <summary>
/// Tipos de dano
/// </summary>
public enum DamageType : byte
{
    Generic = 0,
    Ballistic = 1,
    Melee = 2,
    Explosion = 3,
    Fire = 4,
    Radiation = 5,
    Fall = 6,
    Hunger = 7,
    Thirst = 8,
    Cold = 9,
    Heat = 10
}