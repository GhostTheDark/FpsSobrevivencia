using UnityEngine;

/// <summary>
/// Controla câmera em primeira pessoa do jogador
/// Mouse look suave com limite vertical
/// </summary>
public class PlayerCamera : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private Transform cameraHolder;
    [SerializeField] private Camera fpCamera;
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float minVerticalAngle = -89f;
    [SerializeField] private float maxVerticalAngle = 89f;

    [Header("Smoothing")]
    [SerializeField] private bool useSmoothLook = true;
    [SerializeField] private float smoothSpeed = 10f;

    [Header("Head Bob")]
    [SerializeField] private bool useHeadBob = true;
    [SerializeField] private float bobSpeed = 10f;
    [SerializeField] private float bobAmount = 0.05f;
    [SerializeField] private float bobSmoothSpeed = 8f;

    [Header("FOV Effects")]
    [SerializeField] private float normalFOV = 75f;
    [SerializeField] private float sprintFOV = 85f;
    [SerializeField] private float fovTransitionSpeed = 8f;

    [Header("Camera Shake")]
    [SerializeField] private float shakeDecay = 5f;

    [Header("Debug")]
    [SerializeField] private bool showDebug = false;

    // Estado
    private bool isLocalPlayer = false;
    private float currentXRotation = 0f;
    private float currentYRotation = 0f;
    private float targetXRotation = 0f;
    private float targetYRotation = 0f;

    // Head bob
    private float bobTimer = 0f;
    private Vector3 originalCameraPosition;
    private Vector3 targetCameraPosition;

    // Camera shake
    private float shakeIntensity = 0f;
    private Vector3 shakeOffset;

    // FOV
    private float currentFOV;
    private float targetFOV;

    // Componentes
    private PlayerController playerController;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();

        // Cria câmera se não existir
        if (fpCamera == null)
        {
            GameObject camObj = new GameObject("FPCamera");
            camObj.transform.SetParent(transform);
            camObj.transform.localPosition = new Vector3(0, 1.6f, 0);
            fpCamera = camObj.AddComponent<Camera>();
            cameraHolder = camObj.transform;
        }

        originalCameraPosition = cameraHolder.localPosition;
        targetCameraPosition = originalCameraPosition;

        currentFOV = normalFOV;
        targetFOV = normalFOV;
        if (fpCamera != null)
            fpCamera.fieldOfView = currentFOV;
    }

    /// <summary>
    /// Inicializa a câmera
    /// </summary>
    public void Initialize(bool isLocal)
    {
        isLocalPlayer = isLocal;

        if (fpCamera != null)
        {
            fpCamera.enabled = isLocal;

            // Configura layer para não renderizar próprias armas em FP
            if (isLocal)
            {
                // A câmera FPS não renderiza layer "FirstPerson"
                // Isso evita ver o próprio modelo
                fpCamera.cullingMask &= ~(1 << LayerMask.NameToLayer("FirstPerson"));
            }
        }

        // Trava cursor para jogador local
        if (isLocal)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        Debug.Log($"[PlayerCamera] Initialized (Local: {isLocal})");
    }

    private void LateUpdate()
    {
        if (!isLocalPlayer) return;

        // Aplica rotação da câmera
        ApplyRotation();

        // Head bob
        if (useHeadBob)
            ApplyHeadBob();

        // FOV dinâmico
        ApplyDynamicFOV();

        // Camera shake
        ApplyCameraShake();
    }

    #region ROTATION

    /// <summary>
    /// Define input de look do PlayerController
    /// </summary>
    public void SetLookInput(Vector2 lookInput)
    {
        // Multiplica por sensibilidade
        float mouseX = lookInput.x * mouseSensitivity;
        float mouseY = lookInput.y * mouseSensitivity;

        // Rotação horizontal (Y) - corpo do jogador
        targetYRotation += mouseX;

        // Rotação vertical (X) - apenas câmera
        targetXRotation -= mouseY;
        targetXRotation = Mathf.Clamp(targetXRotation, minVerticalAngle, maxVerticalAngle);
    }

    /// <summary>
    /// Aplica rotação suave
    /// </summary>
    private void ApplyRotation()
    {
        if (useSmoothLook)
        {
            // Smooth
            currentYRotation = Mathf.Lerp(currentYRotation, targetYRotation, smoothSpeed * Time.deltaTime);
            currentXRotation = Mathf.Lerp(currentXRotation, targetXRotation, smoothSpeed * Time.deltaTime);
        }
        else
        {
            // Direto
            currentYRotation = targetYRotation;
            currentXRotation = targetXRotation;
        }

        // Aplica rotação
        transform.rotation = Quaternion.Euler(0, currentYRotation, 0);
        
        if (cameraHolder != null)
            cameraHolder.localRotation = Quaternion.Euler(currentXRotation, 0, 0);
    }

    #endregion

    #region HEAD BOB

    /// <summary>
    /// Aplica head bob ao andar
    /// </summary>
    private void ApplyHeadBob()
    {
        if (playerController == null) return;

        // Só aplica se estiver andando no chão
        bool isMoving = Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0;
        bool isGrounded = playerController.IsGrounded();

        if (isMoving && isGrounded)
        {
            // Aumenta timer
            bobTimer += Time.deltaTime * bobSpeed;

            // Calcula offset
            float bobX = Mathf.Cos(bobTimer) * bobAmount;
            float bobY = Mathf.Sin(bobTimer * 2) * bobAmount;

            targetCameraPosition = originalCameraPosition + new Vector3(bobX, bobY, 0);
        }
        else
        {
            // Volta para posição original
            bobTimer = 0f;
            targetCameraPosition = originalCameraPosition;
        }

        // Interpola suavemente
        if (cameraHolder != null)
        {
            cameraHolder.localPosition = Vector3.Lerp(
                cameraHolder.localPosition,
                targetCameraPosition + shakeOffset,
                bobSmoothSpeed * Time.deltaTime
            );
        }
    }

    #endregion

    #region FOV

    /// <summary>
    /// Aplica FOV dinâmico (aumenta ao correr)
    /// </summary>
    private void ApplyDynamicFOV()
    {
        if (fpCamera == null || playerController == null) return;

        // Define FOV alvo
        targetFOV = playerController.IsSprinting() ? sprintFOV : normalFOV;

        // Interpola
        currentFOV = Mathf.Lerp(currentFOV, targetFOV, fovTransitionSpeed * Time.deltaTime);
        fpCamera.fieldOfView = currentFOV;
    }

    #endregion

    #region CAMERA SHAKE

    /// <summary>
    /// Adiciona shake na câmera (tiro, explosão, etc)
    /// </summary>
    public void AddShake(float intensity)
    {
        shakeIntensity += intensity;
    }

    /// <summary>
    /// Aplica camera shake
    /// </summary>
    private void ApplyCameraShake()
    {
        if (shakeIntensity > 0)
        {
            // Gera offset aleatório
            shakeOffset = Random.insideUnitSphere * shakeIntensity;

            // Decay
            shakeIntensity -= shakeDecay * Time.deltaTime;
            if (shakeIntensity < 0)
                shakeIntensity = 0;
        }
        else
        {
            shakeOffset = Vector3.zero;
        }
    }

    #endregion

    #region PUBLIC METHODS

    /// <summary>
    /// Retorna transform da câmera
    /// </summary>
    public Transform GetCameraTransform() => cameraHolder;

    /// <summary>
    /// Retorna componente Camera
    /// </summary>
    public Camera GetCamera() => fpCamera;

    /// <summary>
    /// Define sensibilidade do mouse
    /// </summary>
    public void SetMouseSensitivity(float sensitivity)
    {
        mouseSensitivity = Mathf.Clamp(sensitivity, 0.1f, 10f);
    }

    /// <summary>
    /// Retorna direção que a câmera está apontando
    /// </summary>
    public Vector3 GetLookDirection()
    {
        if (cameraHolder != null)
            return cameraHolder.forward;
        return transform.forward;
    }

    /// <summary>
    /// Reseta rotação (útil para cutscenes)
    /// </summary>
    public void ResetRotation()
    {
        currentXRotation = 0f;
        targetXRotation = 0f;
        currentYRotation = transform.eulerAngles.y;
        targetYRotation = currentYRotation;
    }

    #endregion

    #region DEBUG

    private void OnGUI()
    {
        if (!showDebug || !isLocalPlayer) return;

        GUILayout.BeginArea(new Rect(10, 380, 300, 120));
        GUILayout.Box("=== PLAYER CAMERA ===");
        GUILayout.Label($"Rotation X: {currentXRotation:F1}°");
        GUILayout.Label($"Rotation Y: {currentYRotation:F1}°");
        GUILayout.Label($"FOV: {currentFOV:F1}");
        GUILayout.Label($"Shake: {shakeIntensity:F2}");
        GUILayout.EndArea();
    }

    #endregion
}