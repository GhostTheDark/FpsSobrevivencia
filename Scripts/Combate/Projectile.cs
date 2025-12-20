using UnityEngine;

/// <summary>
/// Projétil físico (flecha, foguete, granada)
/// Movimenta-se fisicamente pelo mundo e detecta colisões
/// Server Authoritative - apenas servidor spawna projéteis
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    [Header("Projectile Info")]
    [SerializeField] private ProjectileType projectileType = ProjectileType.Arrow;
    [SerializeField] private float damage = 20f;
    [SerializeField] private float speed = 50f;
    [SerializeField] private float maxLifetime = 10f;
    [SerializeField] private DamageType damageType = DamageType.Ballistic;

    [Header("Physics")]
    [SerializeField] private bool useGravity = true;
    [SerializeField] private float gravityMultiplier = 1f;
    [SerializeField] private float drag = 0f;

    [Header("Hit Detection")]
    [SerializeField] private LayerMask hitMask;
    [SerializeField] private bool penetratesPlayers = false;
    [SerializeField] private int maxPenetrations = 1;

    [Header("Explosion (if explosive)")]
    [SerializeField] private bool isExplosive = false;
    [SerializeField] private float explosionRadius = 5f;
    [SerializeField] private float explosionDamage = 100f;
    [SerializeField] private GameObject explosionEffectPrefab;

    [Header("Stick Behavior")]
    [SerializeField] private bool canStickToSurfaces = true;
    [SerializeField] private bool canStickToPlayers = true;

    [Header("Effects")]
    [SerializeField] private GameObject impactEffectPrefab;
    [SerializeField] private GameObject trailEffectPrefab;
    [SerializeField] private AudioClip impactSound;
    [SerializeField] private AudioClip flyingSound;

    [Header("Debug")]
    [SerializeField] private bool showDebug = false;

    // Estado
    private int ownerId = -1;
    private bool hasHit = false;
    private bool isInitialized = false;
    private float spawnTime = 0f;
    private int penetrationCount = 0;

    // Componentes
    private Rigidbody rb;
    private Collider col;
    private AudioSource audioSource;
    private GameObject trailEffect;

    // Previous position para ray entre frames
    private Vector3 previousPosition;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    /// <summary>
    /// Inicializa o projétil
    /// </summary>
    public void Initialize(int owner, float dmg, float spd, float lifetime, DamageType dmgType)
    {
        ownerId = owner;
        damage = dmg;
        speed = spd;
        maxLifetime = lifetime;
        damageType = dmgType;

        isInitialized = true;
        spawnTime = Time.time;
        previousPosition = transform.position;

        // Configura física
        if (rb != null)
        {
            rb.useGravity = useGravity;
            rb.drag = drag;
            
            if (useGravity)
                rb.AddForce(Physics.gravity * gravityMultiplier, ForceMode.Acceleration);

            // Aplica velocidade inicial
            rb.velocity = transform.forward * speed;
        }

        // Spawna trail effect
        if (trailEffectPrefab != null)
        {
            trailEffect = Instantiate(trailEffectPrefab, transform);
        }

        // Som de voo
        if (flyingSound != null && audioSource != null)
        {
            audioSource.clip = flyingSound;
            audioSource.loop = true;
            audioSource.Play();
        }

        if (showDebug)
            Debug.Log($"[Projectile] Initialized by player {ownerId}");
    }

    private void Update()
    {
        if (!isInitialized) return;

        // Verifica lifetime
        if (Time.time - spawnTime > maxLifetime)
        {
            DestroyProjectile();
            return;
        }

        // Rotaciona na direção do movimento
        if (rb != null && rb.velocity.magnitude > 0.1f)
        {
            transform.rotation = Quaternion.LookRotation(rb.velocity);
        }

        // Ray entre frames para detectar hits rápidos
        CheckRayBetweenFrames();

        previousPosition = transform.position;
    }

    #region HIT DETECTION

    /// <summary>
    /// Ray entre frames para detectar colisões de alta velocidade
    /// </summary>
    private void CheckRayBetweenFrames()
    {
        if (hasHit) return;

        Vector3 direction = transform.position - previousPosition;
        float distance = direction.magnitude;

        if (distance < 0.01f) return;

        RaycastHit hit;
        if (Physics.Raycast(previousPosition, direction.normalized, out hit, distance, hitMask))
        {
            OnHit(hit);
        }
    }

    /// <summary>
    /// Collision detection via OnCollisionEnter
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;

        // Cria um HitInfo a partir da colisão
        HitInfo hit = new HitInfo
        {
            point = collision.contacts[0].point,
            normal = collision.contacts[0].normal,
            collider = collision.collider
        };

        OnHit(hit);
    }

    /// <summary>
    /// Trigger detection (para hitboxes de jogadores)
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (hasHit && !penetratesPlayers) return;

        // Cria um HitInfo a partir do trigger
        HitInfo hit = new HitInfo
        {
            point = other.ClosestPoint(transform.position),
            normal = (transform.position - other.transform.position).normalized,
            collider = other
        };

        OnHit(hit);
    }

    #endregion

    #region HIT PROCESSING

    /// <summary>
    /// Processa hit (versão sobrecarregada para RaycastHit)
    /// </summary>
    private void OnHit(RaycastHit hit)
    {
        HitInfo hitInfo = new HitInfo
        {
            point = hit.point,
            normal = hit.normal,
            collider = hit.collider
        };
        OnHit(hitInfo);
    }

    /// <summary>
    /// Processa hit
    /// </summary>
    private void OnHit(HitInfo hit)
    {
        if (showDebug)
            Debug.Log($"[Projectile] Hit {hit.collider.name}");

        // Verifica se é jogador
        NetworkPlayer player = hit.collider.GetComponentInParent<NetworkPlayer>();
        if (player != null && player.clientId != ownerId)
        {
            HitPlayer(player, hit);
            
            // Se penetra, continua
            if (penetratesPlayers && penetrationCount < maxPenetrations)
            {
                penetrationCount++;
                return;
            }
        }

        // Verifica se é construção
        BuildingPiece building = hit.collider.GetComponentInParent<BuildingPiece>();
        if (building != null)
        {
            HitBuilding(building, hit);
        }

        // Se é explosivo, explode
        if (isExplosive)
        {
            Explode(hit.point);
        }

        // Stick ou destruir
        if (canStickToSurfaces && !isExplosive)
        {
            StickToSurface(hit);
        }
        else
        {
            ImpactEffect(hit.point, hit.normal);
            DestroyProjectile();
        }

        hasHit = true;
    }

    /// <summary>
    /// Hit em jogador
    /// </summary>
    private void HitPlayer(NetworkPlayer player, HitInfo hit)
    {
        // Aplica dano via servidor
        if (NetworkManager.Instance != null && NetworkManager.Instance.isServer)
        {
            PlayerHealth health = player.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(damage, ownerId, damageType);
            }
        }

        // Efeito de impacto
        ImpactEffect(hit.point, hit.normal);

        // Stick no player se permitido
        if (canStickToPlayers && canStickToSurfaces)
        {
            StickToTarget(hit.collider.transform);
        }

        if (showDebug)
            Debug.Log($"[Projectile] Hit player {player.clientId} for {damage} damage");
    }

    /// <summary>
    /// Hit em construção
    /// </summary>
    private void HitBuilding(BuildingPiece building, HitInfo hit)
    {
        // Aplica dano
        building.TakeDamage(damage, ownerId, damageType);

        // Efeito de impacto
        ImpactEffect(hit.point, hit.normal);

        if (showDebug)
            Debug.Log($"[Projectile] Hit building for {damage} damage");
    }

    #endregion

    #region EXPLOSION

    /// <summary>
    /// Explode e causa dano em área
    /// </summary>
    private void Explode(Vector3 position)
    {
        if (showDebug)
            Debug.Log($"[Projectile] Exploding at {position}");

        // Spawna efeito de explosão
        if (explosionEffectPrefab != null)
        {
            Instantiate(explosionEffectPrefab, position, Quaternion.identity);
        }

        // Dano em área (apenas servidor)
        if (NetworkManager.Instance != null && NetworkManager.Instance.isServer)
        {
            DealExplosionDamage(position);
        }

        // Som de explosão já está no effect prefab
    }

    /// <summary>
    /// Aplica dano de explosão em área
    /// </summary>
    private void DealExplosionDamage(Vector3 center)
    {
        Collider[] hitColliders = Physics.OverlapSphere(center, explosionRadius);

        foreach (Collider col in hitColliders)
        {
            // Calcula distância e falloff de dano
            float distance = Vector3.Distance(center, col.transform.position);
            float damageMultiplier = 1f - (distance / explosionRadius);
            float finalDamage = explosionDamage * damageMultiplier;

            // Jogador
            NetworkPlayer player = col.GetComponentInParent<NetworkPlayer>();
            if (player != null && player.clientId != ownerId)
            {
                PlayerHealth health = player.GetComponent<PlayerHealth>();
                if (health != null)
                {
                    health.TakeDamage(finalDamage, ownerId, DamageType.Explosion);
                }
            }

            // Construção
            BuildingPiece building = col.GetComponentInParent<BuildingPiece>();
            if (building != null)
            {
                building.TakeDamage(finalDamage, ownerId, DamageType.Explosion);
            }
        }

        if (showDebug)
            Debug.Log($"[Projectile] Explosion damaged {hitColliders.Length} objects");
    }

    #endregion

    #region STICK

    /// <summary>
    /// Gruda na superfície
    /// </summary>
    private void StickToSurface(HitInfo hit)
    {
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
        }

        // Posiciona no ponto de hit
        transform.position = hit.point;
        transform.rotation = Quaternion.LookRotation(hit.normal);

        // Parent ao objeto (se possível)
        if (hit.collider.attachedRigidbody != null)
        {
            transform.SetParent(hit.collider.transform);
        }

        if (showDebug)
            Debug.Log($"[Projectile] Stuck to {hit.collider.name}");

        // Destrói após tempo
        Destroy(gameObject, maxLifetime);
    }

    /// <summary>
    /// Gruda em target específico
    /// </summary>
    private void StickToTarget(Transform target)
    {
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
        }

        transform.SetParent(target);

        if (showDebug)
            Debug.Log($"[Projectile] Stuck to target {target.name}");
    }

    #endregion

    #region EFFECTS

    /// <summary>
    /// Spawna efeito de impacto
    /// </summary>
    private void ImpactEffect(Vector3 position, Vector3 normal)
    {
        if (impactEffectPrefab != null)
        {
            Quaternion rotation = Quaternion.LookRotation(normal);
            GameObject effect = Instantiate(impactEffectPrefab, position, rotation);
            Destroy(effect, 2f);
        }

        if (impactSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(impactSound);
        }
    }

    #endregion

    /// <summary>
    /// Destrói o projétil
    /// </summary>
    private void DestroyProjectile()
    {
        if (trailEffect != null)
        {
            trailEffect.transform.SetParent(null);
            Destroy(trailEffect, 2f);
        }

        Destroy(gameObject);
    }

    #region GIZMOS

    private void OnDrawGizmosSelected()
    {
        if (!isExplosive) return;

        // Desenha raio de explosão
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }

    #endregion
}

/// <summary>
/// Struct para armazenar informações de hit
/// (RaycastHit é read-only, então criamos nosso próprio)
/// </summary>
public struct HitInfo
{
    public Vector3 point;
    public Vector3 normal;
    public Collider collider;
}

/// <summary>
/// Tipos de projétil
/// </summary>
public enum ProjectileType
{
    Arrow,      // Flecha
    Bullet,     // Bala
    Rocket,     // Foguete
    Grenade,    // Granada
    Spear       // Lança
}
