using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Gerador procedural de mundo estilo Rust
/// Cria terreno, biomas, recursos e monumentos
/// Apenas servidor executa geração
/// </summary>
public class WorldGenerator : MonoBehaviour
{
    public static WorldGenerator Instance { get; private set; }

    [Header("World Settings")]
    [SerializeField] private int worldSize = 4000; // Metros (4km x 4km)
    [SerializeField] private int worldSeed = 12345;
    [SerializeField] private bool useRandomSeed = false;

    [Header("Terrain")]
    [SerializeField] private GameObject terrainPrefab;
    [SerializeField] private int terrainResolution = 513;
    [SerializeField] private float terrainHeight = 600f;
    [SerializeField] private AnimationCurve heightCurve;

    [Header("Noise Settings")]
    [SerializeField] private float noiseScale = 0.01f;
    [SerializeField] private int octaves = 4;
    [SerializeField] private float persistence = 0.5f;
    [SerializeField] private float lacunarity = 2f;

    [Header("Biomes")]
    [SerializeField] private BiomeData[] biomes;
    [SerializeField] private float biomeBlendDistance = 50f;

    [Header("Water")]
    [SerializeField] private GameObject waterPrefab;
    [SerializeField] private float waterLevel = 50f;

    [Header("Monuments")]
    [SerializeField] private MonumentSpawnData[] monuments;
    [SerializeField] private int minMonumentDistance = 200;

    [Header("Debug")]
    [SerializeField] private bool showDebug = false;
    [SerializeField] private bool visualizeNoiseInEditor = false;

    // Estado
    private bool isGenerated = false;
    private Terrain terrain;
    private TerrainData terrainData;

    // Dados gerados
    private float[,] heightMap;
    private float[,] moistureMap;
    private float[,] temperatureMap;
    private BiomeType[,] biomeMap;

    // Spawn points
    private List<Vector3> beachSpawnPoints = new List<Vector3>();
    private List<GameObject> spawnedMonuments = new List<GameObject>();

    private void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Gera o mundo completo
    /// </summary>
    public void GenerateWorld()
    {
        if (isGenerated)
        {
            Debug.LogWarning("[WorldGenerator] World already generated!");
            return;
        }

        Debug.Log("========================================");
        Debug.Log("[WorldGenerator] Starting world generation...");
        Debug.Log($"[WorldGenerator] Size: {worldSize}m x {worldSize}m");
        Debug.Log($"[WorldGenerator] Seed: {worldSeed}");
        Debug.Log("========================================");

        // Define seed
        if (useRandomSeed)
        {
            worldSeed = Random.Range(0, 999999);
        }
        Random.InitState(worldSeed);

        // 1. Gera heightmap
        GenerateHeightMap();

        // 2. Gera mapas auxiliares (moisture, temperature)
        GenerateAuxiliaryMaps();

        // 3. Calcula biomas
        CalculateBiomes();

        // 4. Cria terrain
        CreateTerrain();

        // 5. Aplica texturas de bioma
        ApplyBiomeTextures();

        // 6. Spawna água
        SpawnWater();

        // 7. Spawna monumentos
        SpawnMonuments();

        // 8. Calcula spawn points
        CalculateSpawnPoints();

        isGenerated = true;

        Debug.Log("[WorldGenerator] World generation complete!");
    }

    #region HEIGHTMAP

    /// <summary>
    /// Gera heightmap usando Perlin Noise
    /// </summary>
    private void GenerateHeightMap()
    {
        Debug.Log("[WorldGenerator] Generating heightmap...");

        heightMap = new float[terrainResolution, terrainResolution];

        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;

        // Gera noise
        for (int y = 0; y < terrainResolution; y++)
        {
            for (int x = 0; x < terrainResolution; x++)
            {
                float amplitude = 1f;
                float frequency = 1f;
                float noiseHeight = 0f;

                // Multi-octave Perlin noise
                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = (x + worldSeed) * noiseScale * frequency;
                    float sampleY = (y + worldSeed) * noiseScale * frequency;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2f - 1f;
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                if (noiseHeight > maxNoiseHeight)
                    maxNoiseHeight = noiseHeight;
                if (noiseHeight < minNoiseHeight)
                    minNoiseHeight = noiseHeight;

                heightMap[x, y] = noiseHeight;
            }
        }

        // Normaliza entre 0 e 1
        for (int y = 0; y < terrainResolution; y++)
        {
            for (int x = 0; x < terrainResolution; x++)
            {
                heightMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, heightMap[x, y]);
                
                // Aplica curva de altura
                if (heightCurve != null)
                {
                    heightMap[x, y] = heightCurve.Evaluate(heightMap[x, y]);
                }

                // Cria ilhas (opcional) - bordas mais baixas
                float distanceFromCenter = GetDistanceFromCenter(x, y);
                float islandMask = Mathf.Clamp01(1f - (distanceFromCenter / 0.5f));
                heightMap[x, y] *= islandMask;
            }
        }

        Debug.Log("[WorldGenerator] Heightmap generated");
    }

    /// <summary>
    /// Retorna distância do centro normalizada (0-1)
    /// </summary>
    private float GetDistanceFromCenter(int x, int y)
    {
        float centerX = terrainResolution / 2f;
        float centerY = terrainResolution / 2f;
        
        float distX = (x - centerX) / centerX;
        float distY = (y - centerY) / centerY;
        
        return Mathf.Sqrt(distX * distX + distY * distY);
    }

    #endregion

    #region AUXILIARY MAPS

    /// <summary>
    /// Gera mapas auxiliares (umidade, temperatura)
    /// </summary>
    private void GenerateAuxiliaryMaps()
    {
        Debug.Log("[WorldGenerator] Generating auxiliary maps...");

        moistureMap = new float[terrainResolution, terrainResolution];
        temperatureMap = new float[terrainResolution, terrainResolution];

        for (int y = 0; y < terrainResolution; y++)
        {
            for (int x = 0; x < terrainResolution; x++)
            {
                // Umidade - Perlin noise separado
                float moistureSampleX = (x + worldSeed + 1000) * noiseScale * 0.5f;
                float moistureSampleY = (y + worldSeed + 1000) * noiseScale * 0.5f;
                moistureMap[x, y] = Mathf.PerlinNoise(moistureSampleX, moistureSampleY);

                // Aumenta umidade perto da água
                if (heightMap[x, y] < waterLevel / terrainHeight)
                {
                    moistureMap[x, y] = Mathf.Lerp(moistureMap[x, y], 1f, 0.5f);
                }

                // Temperatura - baseada em altitude e latitude
                float altitude = heightMap[x, y];
                float latitude = Mathf.Abs(y - terrainResolution / 2f) / (terrainResolution / 2f);
                
                temperatureMap[x, y] = 1f - altitude * 0.5f; // Altitude diminui temperatura
                temperatureMap[x, y] *= 1f - latitude * 0.3f; // Latitude diminui temperatura
                
                // Adiciona noise para variação
                float tempSampleX = (x + worldSeed + 2000) * noiseScale * 0.3f;
                float tempSampleY = (y + worldSeed + 2000) * noiseScale * 0.3f;
                float tempNoise = Mathf.PerlinNoise(tempSampleX, tempSampleY);
                temperatureMap[x, y] = Mathf.Lerp(temperatureMap[x, y], tempNoise, 0.3f);
            }
        }

        Debug.Log("[WorldGenerator] Auxiliary maps generated");
    }

    #endregion

    #region BIOMES

    /// <summary>
    /// Calcula tipo de bioma para cada ponto
    /// </summary>
    private void CalculateBiomes()
    {
        Debug.Log("[WorldGenerator] Calculating biomes...");

        biomeMap = new BiomeType[terrainResolution, terrainResolution];

        for (int y = 0; y < terrainResolution; y++)
        {
            for (int x = 0; x < terrainResolution; x++)
            {
                float height = heightMap[x, y];
                float moisture = moistureMap[x, y];
                float temperature = temperatureMap[x, y];

                biomeMap[x, y] = DetermineBiome(height, moisture, temperature);
            }
        }

        Debug.Log("[WorldGenerator] Biomes calculated");
    }

    /// <summary>
    /// Determina bioma baseado em altura, umidade e temperatura
    /// </summary>
    private BiomeType DetermineBiome(float height, float moisture, float temperature)
    {
        // Água
        if (height < waterLevel / terrainHeight)
            return BiomeType.Ocean;

        // Praia
        if (height < (waterLevel + 5f) / terrainHeight)
            return BiomeType.Beach;

        // Neve (altitude alta ou temperatura baixa)
        if (height > 0.7f || temperature < 0.2f)
            return BiomeType.Snow;

        // Deserto (baixa umidade, alta temperatura)
        if (moisture < 0.3f && temperature > 0.6f)
            return BiomeType.Desert;

        // Floresta (alta umidade)
        if (moisture > 0.6f)
            return BiomeType.Forest;

        // Tundra (baixa temperatura)
        if (temperature < 0.4f)
            return BiomeType.Tundra;

        // Planície (padrão)
        return BiomeType.Grassland;
    }

    /// <summary>
    /// Retorna dados de um bioma
    /// </summary>
    private BiomeData GetBiomeData(BiomeType biomeType)
    {
        if (biomes == null) return null;

        foreach (var biome in biomes)
        {
            if (biome.biomeType == biomeType)
                return biome;
        }

        return null;
    }

    #endregion

    #region TERRAIN

    /// <summary>
    /// Cria terrain do Unity
    /// </summary>
    private void CreateTerrain()
    {
        Debug.Log("[WorldGenerator] Creating terrain...");

        // Cria TerrainData
        terrainData = new TerrainData();
        terrainData.heightmapResolution = terrainResolution;
        terrainData.size = new Vector3(worldSize, terrainHeight, worldSize);
        terrainData.SetHeights(0, 0, heightMap);

        // Cria GameObject de Terrain
        GameObject terrainObj;
        if (terrainPrefab != null)
        {
            terrainObj = Instantiate(terrainPrefab);
        }
        else
        {
            terrainObj = new GameObject("Terrain");
            terrainObj.AddComponent<Terrain>();
            terrainObj.AddComponent<TerrainCollider>();
        }

        terrain = terrainObj.GetComponent<Terrain>();
        terrain.terrainData = terrainData;

        TerrainCollider terrainCollider = terrainObj.GetComponent<TerrainCollider>();
        if (terrainCollider != null)
        {
            terrainCollider.terrainData = terrainData;
        }

        // Centraliza terrain
        terrainObj.transform.position = new Vector3(-worldSize / 2f, 0, -worldSize / 2f);

        Debug.Log("[WorldGenerator] Terrain created");
    }

    /// <summary>
    /// Aplica texturas de bioma ao terrain
    /// </summary>
    private void ApplyBiomeTextures()
    {
        Debug.Log("[WorldGenerator] Applying biome textures...");

        // TODO: Implementar splatmap com texturas de biomas
        // Por enquanto, deixa com material padrão

        Debug.Log("[WorldGenerator] Biome textures applied");
    }

    #endregion

    #region WATER

    /// <summary>
    /// Spawna água
    /// </summary>
    private void SpawnWater()
    {
        Debug.Log("[WorldGenerator] Spawning water...");

        if (waterPrefab != null)
        {
            GameObject water = Instantiate(waterPrefab);
            water.transform.position = new Vector3(0, waterLevel, 0);
            water.transform.localScale = new Vector3(worldSize, 1, worldSize);
            water.name = "Ocean";
        }

        Debug.Log("[WorldGenerator] Water spawned");
    }

    #endregion

    #region MONUMENTS

    /// <summary>
    /// Spawna monumentos no mapa
    /// </summary>
    private void SpawnMonuments()
    {
        Debug.Log("[WorldGenerator] Spawning monuments...");

        if (monuments == null || monuments.Length == 0)
        {
            Debug.LogWarning("[WorldGenerator] No monuments configured!");
            return;
        }

        foreach (var monumentData in monuments)
        {
            if (monumentData.prefab == null) continue;

            for (int i = 0; i < monumentData.spawnCount; i++)
            {
                Vector3 spawnPos = FindMonumentSpawnPosition(monumentData);
                
                if (spawnPos != Vector3.zero)
                {
                    GameObject monument = Instantiate(monumentData.prefab, spawnPos, Quaternion.identity);
                    monument.name = $"{monumentData.monumentName}_{i}";
                    spawnedMonuments.Add(monument);

                    if (showDebug)
                        Debug.Log($"[WorldGenerator] Spawned {monumentData.monumentName} at {spawnPos}");
                }
            }
        }

        Debug.Log($"[WorldGenerator] Spawned {spawnedMonuments.Count} monuments");
    }

    /// <summary>
    /// Encontra posição válida para spawnar monumento
    /// </summary>
    private Vector3 FindMonumentSpawnPosition(MonumentSpawnData monumentData)
    {
        int maxAttempts = 50;
        
        for (int i = 0; i < maxAttempts; i++)
        {
            // Posição aleatória
            float x = Random.Range(-worldSize / 2f + 100, worldSize / 2f - 100);
            float z = Random.Range(-worldSize / 2f + 100, worldSize / 2f - 100);
            
            // Converte para coordenadas do heightmap
            int hmX = Mathf.RoundToInt(((x + worldSize / 2f) / worldSize) * terrainResolution);
            int hmZ = Mathf.RoundToInt(((z + worldSize / 2f) / worldSize) * terrainResolution);
            
            hmX = Mathf.Clamp(hmX, 0, terrainResolution - 1);
            hmZ = Mathf.Clamp(hmZ, 0, terrainResolution - 1);

            float height = heightMap[hmX, hmZ];
            BiomeType biome = biomeMap[hmX, hmZ];

            // Valida bioma
            if (monumentData.allowedBiomes != null && monumentData.allowedBiomes.Length > 0)
            {
                bool biomeAllowed = false;
                foreach (var allowedBiome in monumentData.allowedBiomes)
                {
                    if (biome == allowedBiome)
                    {
                        biomeAllowed = true;
                        break;
                    }
                }
                if (!biomeAllowed) continue;
            }

            // Valida altura
            if (height < monumentData.minHeight || height > monumentData.maxHeight)
                continue;

            // Valida distância de outros monumentos
            bool tooClose = false;
            foreach (var existingMonument in spawnedMonuments)
            {
                float distance = Vector3.Distance(new Vector3(x, 0, z), existingMonument.transform.position);
                if (distance < minMonumentDistance)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose) continue;

            // Posição válida!
            float y = height * terrainHeight;
            return new Vector3(x, y, z);
        }

        Debug.LogWarning($"[WorldGenerator] Failed to find spawn position for {monumentData.monumentName}");
        return Vector3.zero;
    }

    #endregion

    #region SPAWN POINTS

    /// <summary>
    /// Calcula spawn points de jogadores (praias)
    /// </summary>
    private void CalculateSpawnPoints()
    {
        Debug.Log("[WorldGenerator] Calculating spawn points...");

        beachSpawnPoints.Clear();

        // Procura por praias
        for (int y = 0; y < terrainResolution; y += 10)
        {
            for (int x = 0; x < terrainResolution; x += 10)
            {
                if (biomeMap[x, y] == BiomeType.Beach)
                {
                    // Converte para world space
                    float worldX = (x / (float)terrainResolution) * worldSize - worldSize / 2f;
                    float worldZ = (y / (float)terrainResolution) * worldSize - worldSize / 2f;
                    float worldY = heightMap[x, y] * terrainHeight;

                    beachSpawnPoints.Add(new Vector3(worldX, worldY, worldZ));
                }
            }
        }

        Debug.Log($"[WorldGenerator] Found {beachSpawnPoints.Count} beach spawn points");
    }

    /// <summary>
    /// Retorna spawn point aleatório de praia
    /// </summary>
    public Vector3 GetRandomBeachSpawn()
    {
        if (beachSpawnPoints.Count == 0)
        {
            Debug.LogWarning("[WorldGenerator] No beach spawn points found! Using center.");
            return new Vector3(0, 100, 0);
        }

        return beachSpawnPoints[Random.Range(0, beachSpawnPoints.Count)];
    }

    #endregion

    #region PUBLIC METHODS

    /// <summary>
    /// Retorna altura do terreno em world position
    /// </summary>
    public float GetTerrainHeight(Vector3 worldPosition)
    {
        if (terrain == null) return 0f;

        return terrain.SampleHeight(worldPosition);
    }

    /// <summary>
    /// Retorna bioma em world position
    /// </summary>
    public BiomeType GetBiomeAt(Vector3 worldPosition)
    {
        if (biomeMap == null) return BiomeType.Grassland;

        // Converte para coordenadas do biomeMap
        int x = Mathf.RoundToInt(((worldPosition.x + worldSize / 2f) / worldSize) * terrainResolution);
        int z = Mathf.RoundToInt(((worldPosition.z + worldSize / 2f) / worldSize) * terrainResolution);

        x = Mathf.Clamp(x, 0, terrainResolution - 1);
        z = Mathf.Clamp(z, 0, terrainResolution - 1);

        return biomeMap[x, z];
    }

    /// <summary>
    /// Retorna se mundo está gerado
    /// </summary>
    public bool IsGenerated() => isGenerated;

    /// <summary>
    /// Retorna tamanho do mundo
    /// </summary>
    public int GetWorldSize() => worldSize;

    /// <summary>
    /// Retorna seed do mundo
    /// </summary>
    public int GetWorldSeed() => worldSeed;

    #endregion
}

/// <summary>
/// Dados de spawn de monumento
/// </summary>
[System.Serializable]
public class MonumentSpawnData
{
    public string monumentName;
    public GameObject prefab;
    public int spawnCount = 1;
    public BiomeType[] allowedBiomes;
    [Range(0f, 1f)]
    public float minHeight = 0.2f;
    [Range(0f, 1f)]
    public float maxHeight = 0.8f;
}