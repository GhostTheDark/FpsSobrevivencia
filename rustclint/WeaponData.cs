using UnityEngine;

namespace RustlikeGame.Combat
{
    /// <summary>
    /// ScriptableObject que define uma arma
    /// </summary>
    [CreateAssetMenu(fileName = "New Weapon", menuName = "Rustlike/Weapon")]
    public class WeaponData : ScriptableObject
    {
        [Header("Basic Info")]
        public int ItemId;
        public string WeaponName;
        [TextArea] public string Description;
        public Sprite Icon;

        [Header("Weapon Type")]
        public WeaponType Type;
        public AmmoType AmmoType = AmmoType.None;

        [Header("Damage")]
        public float BaseDamage = 10f;
        public float HeadshotMultiplier = 2f;
        public float LegShotMultiplier = 0.8f;
        public DamageType DamageType;

        [Header("Range & Accuracy")]
        public float Range = 10f;
        public float OptimalRange = 5f;
        [Range(0f, 1f)] public float Accuracy = 0.9f;

        [Header("Fire Rate")]
        public float FireRate = 1f; // Shots per second
        public bool IsAutomatic = false;

        [Header("Ammo (Ranged only)")]
        public int MagazineSize = 30;
        public float ReloadTime = 2f;

        [Header("Melee")]
        public float SwingSpeed = 1f;

        [Header("Effects")]
        public float KnockbackForce = 5f;
        public float RecoilAmount = 0.1f;

        [Header("Visuals")]
        public GameObject WeaponPrefab;
        public GameObject MuzzleFlashPrefab;
        public GameObject ImpactEffectPrefab;

        [Header("Audio")]
        public AudioClip FireSound;
        public AudioClip ReloadSound;
        public AudioClip EmptySound;
        public AudioClip HitSound;

        // Helper Methods
        public float GetFireInterval() => 1f / FireRate;
        public bool IsRanged() => Type == WeaponType.Ranged;
        public bool IsMelee() => Type == WeaponType.Melee || Type == WeaponType.Tool;
    }

    public enum WeaponType
    {
        None = 0,
        Melee = 1,
        Ranged = 2,
        Throwable = 3,
        Tool = 4
    }

    public enum AmmoType
    {
        None = 0,
        Arrow = 1,
        PistolAmmo = 2,
        RifleAmmo = 3,
        Shotgun = 4,
        Explosive = 5
    }

    public enum DamageType
    {
        Generic = 0,
        Melee = 1,
        Bullet = 2,
        Explosion = 3,
        Fall = 4,
        Hunger = 5,
        Thirst = 6,
        Cold = 7,
        Heat = 8,
        Radiation = 9,
        Bleeding = 10
    }

    public enum HitboxType
    {
        Body = 0,
        Head = 1,
        Legs = 2,
        Arms = 3
    }
}
