using UnityEngine;

/// <summary>
/// Sistema de biomas do mundo
/// Define características de cada bioma (temperatura, recursos, vegetação)
/// </summary>
public class BiomeSystem : MonoBehaviour
{
    public static BiomeSystem Instance { get; private set; }

    [Header("Biome Database")]
    [SerializeField] private BiomeData[] biomes;

    [Header("Debug")]
    [SerializeField] private bool showDebug = false;

    private void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Retorna dados de um bioma
    /// </summary>
    public BiomeData GetBiomeData(BiomeType biomeType)
    {
        if (biomes == null) return null;

        foreach (var biome in biomes)
        {
            if (biome.biomeType == biomeType)
                return biome;
        }

        if (showDebug)
            Debug.LogWarning($"[BiomeSystem] Biome {biomeType} not found!");

        return null;
    }

    /// <summary>
    /// Retorna temperatura de um bioma
    /// </summary>
    public float GetBiomeTemperature(BiomeType biomeType)
    {
        BiomeData data = GetBiomeData(biomeType);
        return data != null ? data.temperature : 20f;
    }

    /// <summary>
    /// Retorna se bioma tem radiação
    /// </summary>
    public bool BiomeHasRadiation(BiomeType biomeType)
    {
        BiomeData data = GetBiomeData(biomeType);
        return data != null && data.hasRadiation;
    }

    /// <summary>
    /// Retorna descrição do bioma
    /// </summary>
    public string GetBiomeDescription(BiomeType biomeType)
    {
        BiomeData data = GetBiomeData(biomeType);
        return data != null ? data.description : "Unknown biome";
    }
}

/// <summary>
/// Dados de configuração de um bioma
/// </summary>
[System.Serializable]
public class BiomeData
{
    [Header("Identity")]
    public BiomeType biomeType = BiomeType.Grassland;
    public string biomeName = "Grassland";
    [TextArea(2, 4)]
    public string description = "A grassy plain";
    public Color biomeColor = Color.green;

    [Header("Environment")]
    [Range(-20f, 50f)]
    public float temperature = 20f; // Celsius
    [Range(0f, 1f)]
    public float moisture = 0.5f;
    public bool hasRadiation = false;
    [Range(0f, 100f)]
    public float radiationLevel = 0f;

    [Header("Terrain")]
    public Texture2D[] terrainTextures;
    public float[] textureStrengths;
    public Material terrainMaterial;

    [Header("Vegetation")]
    public GameObject[] treePrefabs;
    [Range(0f, 1f)]
    public float treeDensity = 0.1f;
    public GameObject[] plantPrefabs;
    [Range(0f, 1f)]
    public float plantDensity = 0.3f;
    public GameObject[] rockPrefabs;
    [Range(0f, 1f)]
    public float rockDensity = 0.05f;

    [Header("Resources")]
    public ResourceSpawnData[] resourceSpawns;

    [Header("Wildlife")]
    public AnimalSpawnData[] animalSpawns;

    [Header("Weather")]
    public WeatherType[] possibleWeather;
    [Range(0f, 1f)]
    public float rainProbability = 0.2f;
    [Range(0f, 1f)]
    public float snowProbability = 0f;

    [Header("Audio")]
    public AudioClip ambientSound;
    [Range(0f, 1f)]
    public float ambientVolume = 0.5f;
}

/// <summary>
/// Tipos de bioma
/// </summary>
public enum BiomeType
{
    Ocean,          // Oceano
    Beach,          // Praia
    Grassland,      // Planície com grama
    Forest,         // Floresta
    Desert,         // Deserto
    Snow,           // Neve
    Tundra,         // Tundra
    Swamp,          // Pântano
    Mountain,       // Montanha
    Radioactive     // Zona radioativa
}

/// <summary>
/// Dados de spawn de recursos
/// </summary>
[System.Serializable]
public class ResourceSpawnData
{
    public string resourceName;
    public GameObject resourcePrefab;
    [Range(0f, 1f)]
    public float spawnProbability = 0.1f;
    public int minPerCluster = 1;
    public int maxPerCluster = 5;
}

/// <summary>
/// Dados de spawn de animais
/// </summary>
[System.Serializable]
public class AnimalSpawnData
{
    public string animalName;
    public GameObject animalPrefab;
    public int minPopulation = 1;
    public int maxPopulation = 10;
    [Range(0f, 1f)]
    public float spawnProbability = 0.1f;
}

/// <summary>
/// Tipos de clima
/// </summary>
public enum WeatherType
{
    Clear,      // Limpo
    Cloudy,     // Nublado
    Rain,       // Chuva
    Storm,      // Tempestade
    Snow,       // Neve
    Fog         // Névoa
}