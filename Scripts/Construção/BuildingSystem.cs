using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Sistema de construção completo estilo Rust
/// Building Plan -> Ghost -> Place -> Upgrade
/// Client-side preview, Server Authoritative placement
/// </summary>
public class BuildingSystem : MonoBehaviour
{
    [Header("Building Mode")]
    [SerializeField] private bool isBuildingMode = false;
    [SerializeField] private BuildingPiece currentBuildingPrefab;
    [SerializeField] private BuildingGhost currentGhost;

    [Header("Settings")]
    [SerializeField] private float maxBuildDistance = 5f;
    [SerializeField] private float rotationStep = 90f;
    [SerializeField] private LayerMask buildableSurfaces;
    [SerializeField] private LayerMask obstructionMask;

    [Header("Input")]
    [SerializeField] private KeyCode buildModeKey = KeyCode.Q;
    [SerializeField] private KeyCode rotateKey = KeyCode.R;
    [SerializeField] private KeyCode upgradeModeKey = KeyCode.None;

    [Header("References")]
    [SerializeField] private Transform ghostParent;
    [SerializeField] private Material validPlacementMaterial;
    [SerializeField] private Material invalidPlacementMaterial;

    [Header("Debug")]
    [SerializeField] private bool showDebug = false;

    // Estado
    private int clientId = -1;
    private bool isLocalPlayer = false;
    private bool isInitialized = false;

    // Ghost atual
    private GameObject currentGhostObject;
    private bool isValidPlacement = false;
    private Vector3 lastPlacementPosition;
    private Quaternion lastPlacementRotation;
    private float currentRotation = 0f;

    // Building pieces registry (apenas servidor)
    private static Dictionary<int, BuildingPiece> allBuildingPieces = new Dictionary<int, BuildingPiece>();
    private static int nextBuildingId = 1000;

    // Componentes
    private PlayerController playerController;
    private Camera playerCamera;
    private InventorySystem inventorySystem;
    private NetworkBuilding networkBuilding;

    // Upgrade mode
    private bool isUpgradeMode = false;
    private BuildingPiece targetedPiece = null;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        inventorySystem = GetComponent<InventorySystem>();
        networkBuilding = GetComponent<NetworkBuilding>();

        // Cria parent para ghosts
        if (ghostParent == null)
        {
            GameObject parent = new GameObject("BuildingGhostParent");
            parent.transform.SetParent(transform);
            ghostParent = parent.transform;
        }
    }

    /// <summary>
    /// Inicializa o sistema
    /// </summary>
    public void Initialize(int id, bool isLocal)
    {
        clientId = id;
        isLocalPlayer = isLocal;
        isInitialized = true;

        if (isLocal)
        {
            playerCamera = Camera.main;
        }

        Debug.Log($"[BuildingSystem] Initialized (ID: {clientId}, Local: {isLocal})");
    }

    private void Update()
    {
        if (!isInitialized || !isLocalPlayer) return;

        // Input para ativar building mode
        HandleBuildingModeInput();

        // Se está em building mode, processa
        if (isBuildingMode)
        {
            UpdateGhostPlacement();
            HandleRotationInput();
            HandlePlacementInput();
        }

        // Upgrade mode
        if (isUpgradeMode)
        {
            UpdateUpgradeMode();
        }
    }

    #region BUILDING MODE

    /// <summary>
    /// Input para ativar/desativar building mode
    /// </summary>
    private void HandleBuildingModeInput()
    {
        if (Input.GetKeyDown(buildModeKey))
        {
            if (isBuildingMode)
            {
                ExitBuildingMode();
            }
            else
            {
                // Pega building plan da hotbar
                InventorySlot equipped = inventorySystem?.GetEquippedItem();
                if (equipped != null && equipped.HasItem())
                {
                    ItemData item = ItemDatabase.Instance.GetItem(equipped.itemId);
                    if (item != null && item.isBuildingPiece && item.buildingPrefab != null)
                    {
                        EnterBuildingMode(item.buildingPrefab);
                    }
                }
            }
        }

        // Upgrade mode
        if (Input.GetKeyDown(upgradeModeKey))
        {
            isUpgradeMode = !isUpgradeMode;
            
            if (isUpgradeMode && isBuildingMode)
            {
                ExitBuildingMode();
            }

            if (showDebug)
                Debug.Log($"[BuildingSystem] Upgrade mode: {isUpgradeMode}");
        }
    }

    /// <summary>
    /// Entra em building mode
    /// </summary>
    public void EnterBuildingMode(GameObject buildingPrefab)
    {
        if (buildingPrefab == null) return;

        isBuildingMode = true;
        currentRotation = 0f;

        // Cria ghost
        CreateGhost(buildingPrefab);

        // Trava controles de tiro
        if (playerController != null)
        {
            // TODO: Desabilitar ações de combate
        }

        if (showDebug)
            Debug.Log($"[BuildingSystem] Entered building mode: {buildingPrefab.name}");
    }

    /// <summary>
    /// Sai do building mode
    /// </summary>
    public void ExitBuildingMode()
    {
        if (!isBuildingMode) return;

        isBuildingMode = false;

        // Destrói ghost
        DestroyGhost();

        if (showDebug)
            Debug.Log("[BuildingSystem] Exited building mode");
    }

    #endregion

    #region GHOST

    /// <summary>
    /// Cria ghost de preview
    /// </summary>
    private void CreateGhost(GameObject prefab)
    {
        if (currentGhostObject != null)
        {
            Destroy(currentGhostObject);
        }

        // Instancia ghost
        currentGhostObject = Instantiate(prefab, ghostParent);
        currentGhost = currentGhostObject.GetComponent<BuildingGhost>();

        // Se não tem componente BuildingGhost, adiciona
        if (currentGhost == null)
        {
            currentGhost = currentGhostObject.AddComponent<BuildingGhost>();
        }

        // Configura ghost
        currentGhost.Initialize(validPlacementMaterial, invalidPlacementMaterial);

        // Remove colliders do ghost
        Collider[] colliders = currentGhostObject.GetComponentsInChildren<Collider>();
        foreach (var col in colliders)
        {
            col.enabled = false;
        }

        if (showDebug)
            Debug.Log($"[BuildingSystem] Created ghost: {prefab.name}");
    }

    /// <summary>
    /// Destrói ghost
    /// </summary>
    private void DestroyGhost()
    {
        if (currentGhostObject != null)
        {
            Destroy(currentGhostObject);
            currentGhostObject = null;
            currentGhost = null;
        }
    }

    /// <summary>
    /// Atualiza posição e validade do ghost
    /// </summary>
    private void UpdateGhostPlacement()
    {
        if (currentGhostObject == null || playerCamera == null)
            return;

        // Raycast do centro da tela
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxBuildDistance, buildableSurfaces))
        {
            // Calcula posição de placement
            Vector3 placementPosition = CalculatePlacementPosition(hit);
            Quaternion placementRotation = Quaternion.Euler(0, currentRotation, 0);

            // Atualiza ghost
            currentGhostObject.transform.position = placementPosition;
            currentGhostObject.transform.rotation = placementRotation;

            // Valida placement
            isValidPlacement = ValidatePlacement(placementPosition, placementRotation);

            // Atualiza visual do ghost
            if (currentGhost != null)
            {
                currentGhost.SetValid(isValidPlacement);
            }

            lastPlacementPosition = placementPosition;
            lastPlacementRotation = placementRotation;

            // Debug
            if (showDebug)
            {
                Debug.DrawLine(ray.origin, hit.point, Color.yellow);
                Debug.DrawRay(hit.point, hit.normal, isValidPlacement ? Color.green : Color.red);
            }
        }
        else
        {
            // Sem hit, coloca longe
            currentGhostObject.transform.position = ray.origin + ray.direction * maxBuildDistance;
            isValidPlacement = false;

            if (currentGhost != null)
            {
                currentGhost.SetValid(false);
            }
        }
    }

    /// <summary>
    /// Calcula posição exata de placement baseada no socket/snap
    /// </summary>
    private Vector3 CalculatePlacementPosition(RaycastHit hit)
    {
        // Verifica se acertou uma building piece (para snap)
        BuildingPiece hitPiece = hit.collider.GetComponentInParent<BuildingPiece>();
        
        if (hitPiece != null)
        {
            // TODO: Sistema de socket/snap
            // Por enquanto, usa posição do hit
            return hit.point;
        }

        // Snap para grid
        Vector3 pos = hit.point;
        pos.x = Mathf.Round(pos.x / 0.5f) * 0.5f; // Grid de 0.5m
        pos.z = Mathf.Round(pos.z / 0.5f) * 0.5f;

        return pos;
    }

    /// <summary>
    /// Valida se pode construir nessa posição
    /// </summary>
    private bool ValidatePlacement(Vector3 position, Quaternion rotation)
    {
        if (currentGhostObject == null)
            return false;

        // 1. Verifica overlap com outras construções
        Collider[] overlaps = Physics.OverlapBox(
            position,
            currentGhostObject.transform.localScale / 2f,
            rotation,
            obstructionMask
        );

        if (overlaps.Length > 0)
        {
            if (showDebug)
                Debug.LogWarning($"[BuildingSystem] Obstruction detected: {overlaps.Length} overlaps");
            return false;
        }

        // 2. Verifica fundação (certas peças precisam de fundação)
        // TODO: Implementar verificação de fundação

        // 3. Verifica Tool Cupboard range
        // TODO: Implementar verificação de TC

        // 4. Verifica se tem recursos
        if (inventorySystem != null)
        {
            InventorySlot equipped = inventorySystem.GetEquippedItem();
            if (equipped != null && equipped.HasItem())
            {
                ItemData item = ItemDatabase.Instance.GetItem(equipped.itemId);
                if (item != null && item.recipe != null)
                {
                    if (!item.recipe.CanCraft(inventorySystem))
                    {
                        if (showDebug)
                            Debug.LogWarning("[BuildingSystem] Not enough resources");
                        return false;
                    }
                }
            }
        }

        return true;
    }

    #endregion

    #region INPUT

    /// <summary>
    /// Input de rotação
    /// </summary>
    private void HandleRotationInput()
    {
        if (Input.GetKeyDown(rotateKey))
        {
            currentRotation += rotationStep;
            if (currentRotation >= 360f)
                currentRotation -= 360f;

            if (showDebug)
                Debug.Log($"[BuildingSystem] Rotation: {currentRotation}°");
        }
    }

    /// <summary>
    /// Input de placement
    /// </summary>
    private void HandlePlacementInput()
    {
        // Click esquerdo para colocar
        if (Input.GetMouseButtonDown(0))
        {
            if (isValidPlacement)
            {
                PlaceBuilding();
            }
            else
            {
                if (showDebug)
                    Debug.LogWarning("[BuildingSystem] Cannot place here!");
            }
        }

        // Click direito ou ESC para cancelar
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            ExitBuildingMode();
        }
    }

    /// <summary>
    /// Coloca a construção (envia para servidor)
    /// </summary>
    private void PlaceBuilding()
    {
        if (!isValidPlacement || networkBuilding == null)
            return;

        // Pega item equipado
        InventorySlot equipped = inventorySystem?.GetEquippedItem();
        if (equipped == null || !equipped.HasItem())
            return;

        ItemData item = ItemDatabase.Instance.GetItem(equipped.itemId);
        if (item == null || !item.isBuildingPiece)
            return;

        // Envia requisição para servidor via NetworkBuilding
        networkBuilding.RequestPlaceBuilding(
            item.itemId,
            lastPlacementPosition,
            lastPlacementRotation
        );

        if (showDebug)
            Debug.Log($"[BuildingSystem] Requested place building at {lastPlacementPosition}");

        // Efeitos visuais/sonoros
        // TODO: Som de construção
        // TODO: Partículas
    }

    #endregion

    #region UPGRADE MODE

    /// <summary>
    /// Atualiza modo de upgrade
    /// </summary>
    private void UpdateUpgradeMode()
    {
        if (playerCamera == null)
            return;

        // Raycast para detectar building piece
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxBuildDistance))
        {
            BuildingPiece piece = hit.collider.GetComponentInParent<BuildingPiece>();
            
            if (piece != null && piece != targetedPiece)
            {
                // Destaca nova peça
                if (targetedPiece != null)
                    targetedPiece.SetHighlight(false);

                targetedPiece = piece;
                targetedPiece.SetHighlight(true);
            }
        }
        else
        {
            // Sem hit, remove destaque
            if (targetedPiece != null)
            {
                targetedPiece.SetHighlight(false);
                targetedPiece = null;
            }
        }

        // Input para upgrade
        if (Input.GetMouseButtonDown(0) && targetedPiece != null)
        {
            TryUpgradeBuilding(targetedPiece);
        }

        // Cancela upgrade mode
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            isUpgradeMode = false;
            if (targetedPiece != null)
            {
                targetedPiece.SetHighlight(false);
                targetedPiece = null;
            }
        }
    }

    /// <summary>
    /// Tenta fazer upgrade de uma peça
    /// </summary>
    private void TryUpgradeBuilding(BuildingPiece piece)
    {
        if (piece == null || networkBuilding == null)
            return;

        // Verifica se pode fazer upgrade
        BuildingGrade nextGrade = GetNextGrade(piece.GetCurrentGrade());
        
        if (nextGrade == piece.GetCurrentGrade())
        {
            if (showDebug)
                Debug.Log("[BuildingSystem] Already max grade!");
            return;
        }

        // Verifica se tem recursos
        // TODO: Verificar custo de upgrade no inventário

        // Envia requisição para servidor
        networkBuilding.RequestUpgradeBuilding(
            piece.GetBuildingId(),
            (int)nextGrade
        );

        if (showDebug)
            Debug.Log($"[BuildingSystem] Requested upgrade to {nextGrade}");
    }

    /// <summary>
    /// Retorna próximo grade de construção
    /// </summary>
    private BuildingGrade GetNextGrade(BuildingGrade current)
    {
        switch (current)
        {
            case BuildingGrade.Twig: return BuildingGrade.Wood;
            case BuildingGrade.Wood: return BuildingGrade.Stone;
            case BuildingGrade.Stone: return BuildingGrade.Metal;
            case BuildingGrade.Metal: return BuildingGrade.Armored;
            case BuildingGrade.Armored: return BuildingGrade.Armored; // Max
            default: return current;
        }
    }

    #endregion

    #region SERVER SIDE (Static Registry)

    /// <summary>
    /// (SERVIDOR) Registra building piece
    /// </summary>
    public static int RegisterBuildingPiece(BuildingPiece piece)
    {
        int id = nextBuildingId++;
        allBuildingPieces[id] = piece;
        return id;
    }

    /// <summary>
    /// (SERVIDOR) Remove building piece
    /// </summary>
    public static void UnregisterBuildingPiece(int id)
    {
        allBuildingPieces.Remove(id);
    }

    /// <summary>
    /// (SERVIDOR) Busca building piece por ID
    /// </summary>
    public static BuildingPiece GetBuildingPiece(int id)
    {
        return allBuildingPieces.TryGetValue(id, out BuildingPiece piece) ? piece : null;
    }

    #endregion

    #region PUBLIC METHODS

    /// <summary>
    /// Define building prefab para construir
    /// </summary>
    public void SetBuildingPrefab(GameObject prefab)
    {
        if (isBuildingMode)
        {
            DestroyGhost();
            CreateGhost(prefab);
        }
    }

    /// <summary>
    /// Retorna se está em building mode
    /// </summary>
    public bool IsInBuildingMode() => isBuildingMode;

    /// <summary>
    /// Retorna se está em upgrade mode
    /// </summary>
    public bool IsInUpgradeMode() => isUpgradeMode;

    #endregion

    #region DEBUG

    private void OnGUI()
    {
        if (!showDebug || !isLocalPlayer) return;

        if (isBuildingMode)
        {
            float width = 250f;
            float height = 100f;
            float x = (Screen.width - width) / 2f;
            float y = Screen.height - height - 100f;

            GUI.color = Color.black;
            GUI.Box(new Rect(x - 2, y - 2, width + 4, height + 4), "");

            GUI.color = Color.white;
            GUILayout.BeginArea(new Rect(x, y, width, height));
            GUILayout.Label("=== BUILDING MODE ===", new GUIStyle { alignment = TextAnchor.MiddleCenter });
            GUILayout.Label($"Valid: {isValidPlacement}");
            GUILayout.Label($"Rotation: {currentRotation}°");
            GUILayout.Label($"Position: {lastPlacementPosition}");
            GUILayout.EndArea();
        }

        if (isUpgradeMode)
        {
            GUI.color = Color.yellow;
            GUI.Label(new Rect(Screen.width / 2 - 100, Screen.height / 2 + 50, 200, 30),
                "UPGRADE MODE", new GUIStyle { alignment = TextAnchor.MiddleCenter, fontSize = 14 });
        }
    }

    #endregion
}