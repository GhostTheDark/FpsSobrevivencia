using UnityEngine;

/// <summary>
/// Controlador principal do jogador - Recebe inputs e coordena outros componentes
/// Apenas o jogador local processa inputs
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerMotor))]
[RequireComponent(typeof(PlayerCamera))]
public class PlayerController : MonoBehaviour
{
    [Header("Identity")]
    private int clientId = -1;
    private bool isLocalPlayer = false;
    private bool isInitialized = false;

    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 7f;
    [SerializeField] private float crouchSpeed = 2.5f;
    [SerializeField] private float jumpForce = 5f;

    [Header("Crouch Settings")]
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float normalHeight = 2f;
    [SerializeField] private float crouchTransitionSpeed = 10f;

    [Header("Input Settings")]
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode crouchKey = KeyCode.LeftControl;
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;

    [Header("Debug")]
    [SerializeField] private bool showDebug = false;

    // Estado atual
    private bool isSprinting = false;
    private bool isCrouching = false;
    private bool isGrounded = true;

    // Componentes
    private CharacterController characterController;
    private PlayerMotor playerMotor;
    private PlayerCamera playerCamera;
    private PlayerStamina playerStamina;
    private PlayerStats playerStats;

    // Input
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool jumpInput;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        playerMotor = GetComponent<PlayerMotor>();
        playerCamera = GetComponent<PlayerCamera>();
        playerStamina = GetComponent<PlayerStamina>();
        playerStats = GetComponent<PlayerStats>();
    }

    /// <summary>
    /// Inicializa o controlador
    /// </summary>
    public void Initialize(int id, bool isLocal)
    {
        clientId = id;
        isLocalPlayer = isLocal;
        isInitialized = true;

        // Configura componentes
        if (playerMotor != null)
            playerMotor.Initialize(isLocal);

        if (playerCamera != null)
            playerCamera.Initialize(isLocal);

        // Desabilita input se não for jogador local
        if (!isLocalPlayer)
        {
            enabled = false;
        }

        Debug.Log($"[PlayerController] Initialized (ID: {clientId}, Local: {isLocalPlayer})");
    }

    private void Update()
    {
        if (!isInitialized || !isLocalPlayer) return;

        // Lê inputs
        ReadInputs();

        // Processa movimento
        ProcessMovement();

        // Processa ações
        ProcessActions();

        // Debug
        if (showDebug)
            DebugInfo();
    }

    #region INPUT

    /// <summary>
    /// Lê todos os inputs do jogador
    /// </summary>
    private void ReadInputs()
    {
        // Movimento WASD
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        moveInput = new Vector2(horizontal, vertical).normalized;

        // Mouse Look
        lookInput = new Vector2(
            Input.GetAxis("Mouse X"),
            Input.GetAxis("Mouse Y")
        );

        // Sprint
        if (Input.GetKeyDown(sprintKey))
            isSprinting = true;
        if (Input.GetKeyUp(sprintKey))
            isSprinting = false;

        // Crouch (toggle)
        if (Input.GetKeyDown(crouchKey))
            ToggleCrouch();

        // Jump
        jumpInput = Input.GetKeyDown(jumpKey);
    }

    #endregion

    #region MOVEMENT

    /// <summary>
    /// Processa movimento do jogador
    /// </summary>
    private void ProcessMovement()
    {
        if (playerMotor == null) return;

        // Verifica se está no chão
        isGrounded = characterController.isGrounded;

        // Não pode correr agachado
        if (isCrouching && isSprinting)
            isSprinting = false;

        // Não pode correr sem stamina
        if (isSprinting && playerStamina != null && !playerStamina.HasStamina())
            isSprinting = false;

        // Calcula velocidade baseada no estado
        float currentSpeed = GetCurrentSpeed();

        // Envia para o motor
        playerMotor.SetMoveInput(moveInput, currentSpeed);

        // Rotação da câmera
        if (playerCamera != null)
            playerCamera.SetLookInput(lookInput);

        // Jump
        if (jumpInput && isGrounded && !isCrouching)
        {
            if (playerStamina == null || playerStamina.UseStamina(10f))
            {
                playerMotor.Jump(jumpForce);
            }
        }

        // Stamina ao correr
        if (isSprinting && moveInput.magnitude > 0.1f)
        {
            if (playerStamina != null)
                playerStamina.UseStamina(5f * Time.deltaTime);
        }
    }

    /// <summary>
    /// Retorna velocidade atual baseada no estado
    /// </summary>
    private float GetCurrentSpeed()
    {
        if (isCrouching)
            return crouchSpeed;

        if (isSprinting)
            return sprintSpeed;

        return walkSpeed;
    }

    /// <summary>
    /// Alterna estado de agachamento
    /// </summary>
    private void ToggleCrouch()
    {
        isCrouching = !isCrouching;

        // Ajusta altura do CharacterController
        float targetHeight = isCrouching ? crouchHeight : normalHeight;
        StartCoroutine(SmoothCrouchTransition(targetHeight));

        if (showDebug)
            Debug.Log($"[PlayerController] Crouch: {isCrouching}");
    }

    /// <summary>
    /// Transição suave de agachamento
    /// </summary>
    private System.Collections.IEnumerator SmoothCrouchTransition(float targetHeight)
    {
        float startHeight = characterController.height;
        float elapsedTime = 0f;
        float duration = 0.2f;

        Vector3 startCenter = characterController.center;
        Vector3 targetCenter = new Vector3(0, targetHeight / 2f, 0);

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            characterController.height = Mathf.Lerp(startHeight, targetHeight, t);
            characterController.center = Vector3.Lerp(startCenter, targetCenter, t);

            yield return null;
        }

        characterController.height = targetHeight;
        characterController.center = targetCenter;
    }

    #endregion

    #region ACTIONS

    /// <summary>
    /// Processa ações do jogador (atacar, usar item, etc)
    /// </summary>
    private void ProcessActions()
    {
        // Ataque primário (botão esquerdo do mouse)
        if (Input.GetMouseButtonDown(0))
        {
            OnPrimaryAttack();
        }

        // Ataque secundário (botão direito do mouse)
        if (Input.GetMouseButtonDown(1))
        {
            OnSecondaryAttack();
        }

        // Recarregar (R)
        if (Input.GetKeyDown(KeyCode.R))
        {
            OnReload();
        }

        // Usar item (E)
        if (Input.GetKeyDown(KeyCode.E))
        {
            OnInteract();
        }

        // Abrir inventário (Tab)
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            OnToggleInventory();
        }

        // Hot bar (1-6)
        for (int i = 0; i < 6; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                OnHotbarSelect(i);
            }
        }
    }

    private void OnPrimaryAttack()
    {
        // Será implementado no sistema de combate
        if (showDebug)
            Debug.Log("[PlayerController] Primary Attack");
    }

    private void OnSecondaryAttack()
    {
        // Será implementado no sistema de combate
        if (showDebug)
            Debug.Log("[PlayerController] Secondary Attack");
    }

    private void OnReload()
    {
        // Será implementado no sistema de armas
        if (showDebug)
            Debug.Log("[PlayerController] Reload");
    }

    private void OnInteract()
    {
        // Será implementado no sistema de interação
        if (showDebug)
            Debug.Log("[PlayerController] Interact");
    }

    private void OnToggleInventory()
    {
        // Será implementado no sistema de UI
        if (showDebug)
            Debug.Log("[PlayerController] Toggle Inventory");
    }

    private void OnHotbarSelect(int slot)
    {
        // Será implementado no sistema de inventário
        if (showDebug)
            Debug.Log($"[PlayerController] Hotbar slot {slot}");
    }

    #endregion

    #region PUBLIC METHODS

    /// <summary>
    /// Retorna se está correndo
    /// </summary>
    public bool IsSprinting() => isSprinting;

    /// <summary>
    /// Retorna se está agachado
    /// </summary>
    public bool IsCrouching() => isCrouching;

    /// <summary>
    /// Retorna se está no chão
    /// </summary>
    public bool IsGrounded() => isGrounded;

    /// <summary>
    /// Força sprint (pode ser usado por buffs/efeitos)
    /// </summary>
    public void SetSprinting(bool sprint)
    {
        isSprinting = sprint;
    }

    /// <summary>
    /// Trava/destrava controles (para UI, cutscenes, etc)
    /// </summary>
    public void SetControlsEnabled(bool enabled)
    {
        this.enabled = enabled;

        if (playerCamera != null)
            playerCamera.enabled = enabled;

        // Trava/destrava cursor
        Cursor.lockState = enabled ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !enabled;
    }

    #endregion

    #region DEBUG

    private void DebugInfo()
    {
        Debug.Log($"[PlayerController] Speed: {GetCurrentSpeed():F1} | Sprint: {isSprinting} | Crouch: {isCrouching} | Grounded: {isGrounded}");
    }

    private void OnGUI()
    {
        if (!showDebug || !isLocalPlayer) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Box("=== PLAYER CONTROLLER ===");
        GUILayout.Label($"Speed: {GetCurrentSpeed():F1} m/s");
        GUILayout.Label($"Sprint: {isSprinting}");
        GUILayout.Label($"Crouch: {isCrouching}");
        GUILayout.Label($"Grounded: {isGrounded}");
        GUILayout.Label($"Move Input: {moveInput}");
        GUILayout.EndArea();
    }

    #endregion
}