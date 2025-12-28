using System;
using System.Text;

namespace RustlikeServer.Network
{
    /// <summary>
    /// 📦 PACOTES DE COMBATE - Comunicação cliente <-> servidor
    /// </summary>

    // Adiciona novos packet types ao enum existente
    public static class CombatPacketTypes
    {
        // Combate
        public const byte MeleeAttack = 30;
        public const byte RangedAttack = 31;
        public const byte AttackResult = 32;
        public const byte WeaponEquip = 33;
        public const byte WeaponReload = 34;
        public const byte TakeDamageNotify = 35;
        public const byte PlayerKilled = 36;
        public const byte PlayerRespawnRequest = 37;
        public const byte PlayerRespawned = 38;
        public const byte WeaponStateUpdate = 39;
    }

    /// <summary>
    /// Cliente envia ataque melee
    /// </summary>
    [Serializable]
    public class MeleeAttackPacket
    {
        public int TargetPlayerId;          // ID do player alvo
        public int WeaponItemId;            // ID da arma usada
        public byte Hitbox;                 // Hitbox atingida
        public float DirectionX;            // Direção do ataque
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

    /// <summary>
    /// Cliente envia ataque ranged (tiro)
    /// </summary>
    [Serializable]
    public class RangedAttackPacket
    {
        public int TargetPlayerId;          // ID do player alvo (ou -1 se errou)
        public int WeaponItemId;            // ID da arma usada
        public byte Hitbox;                 // Hitbox atingida
        public float ShootDirectionX;       // Direção do tiro
        public float ShootDirectionY;
        public float ShootDirectionZ;
        public float Distance;              // Distância do tiro

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

    /// <summary>
    /// Servidor responde com resultado do ataque
    /// </summary>
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

    /// <summary>
    /// Cliente equipa arma
    /// </summary>
    [Serializable]
    public class WeaponEquipPacket
    {
        public int WeaponItemId;            // ID da arma a equipar (ou 0 para desequipar)
        public int SlotIndex;               // Índice do slot de inventário

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

    /// <summary>
    /// Cliente solicita recarga
    /// </summary>
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

    /// <summary>
    /// Servidor notifica cliente que tomou dano
    /// </summary>
    [Serializable]
    public class TakeDamageNotifyPacket
    {
        public int AttackerId;              // ID do atacante (-1 se ambiental)
        public float Damage;
        public byte DamageType;
        public byte Hitbox;
        public float DirectionX;            // Direção do ataque (para efeitos visuais)
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

    /// <summary>
    /// Servidor notifica que player foi morto
    /// </summary>
    [Serializable]
    public class PlayerKilledPacket
    {
        public int VictimId;
        public int KillerId;                // -1 se suicídio/ambiental
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

    /// <summary>
    /// Servidor envia estado da arma (munição, recarga, etc)
    /// </summary>
    [Serializable]
    public class WeaponStateUpdatePacket
    {
        public int WeaponItemId;
        public int CurrentAmmo;
        public int ReserveAmmo;
        public bool IsReloading;
        public float ReloadProgress;        // 0.0 a 1.0

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