using UnityEngine;

/// <summary>
/// Motor de movimento do jogador - Executa o movimento físico
/// Aplica gravidade, velocidade e físicas do CharacterController
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerMotor : MonoBehaviour
{
    [Header("Physics")]
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private LayerMask groundMask;

    [Header("Air Control")]
    [SerializeField] private float airControl = 0.5f; // Controle no ar (50%)

    [Header("Slope Handling")]
    [SerializeField] private float maxSlopeAngle = 45f;
    [SerializeField] private float slopeForce = 5f;

    [Header("Debug")]
    [SerializeField] private bool showDebug = false;

    // Estado
    private bool isLocalPlayer = false;
    private Vector3 velocity;
    private Vector3 moveDirection;
    private bool isGrounded;

    // Input do controller
    private Vector2 currentMoveInput;
    private float currentSpeed;

    // Componentes
    private CharacterController characterController;
    private Transform cameraTransform;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    /// <summary>
    /// Inicializa o motor
    /// </summary>
    public void Initialize(bool isLocal)
    {
        isLocalPlayer = isLocal;

        // Pega referência da câmera
        if (isLocal)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
                cameraTransform = mainCam.transform;
        }

        Debug.Log($"[PlayerMotor] Initialized (Local: {isLocalPlayer})");
    }

    private void Update()
    {
        if (!isLocalPlayer) return;

        // Verifica chão
        CheckGround();

        // Aplica movimento
        ApplyMovement();

        // Aplica gravidade
        ApplyGravity();

        // Move o CharacterController
        characterController.Move(velocity * Time.deltaTime);
    }

    #region MOVEMENT

    /// <summary>
    /// Define input de movimento do PlayerController
    /// </summary>
    public void SetMoveInput(Vector2 input, float speed)
    {
        currentMoveInput = input;
        currentSpeed = speed;
    }

    /// <summary>
    /// Aplica movimento baseado no input
    /// </summary>
    private void ApplyMovement()
    {
        if (currentMoveInput.magnitude < 0.01f)
        {
            // Sem input, desacelera
            moveDirection = Vector3.Lerp(moveDirection, Vector3.zero, 10f * Time.deltaTime);
            return;
        }

        // Calcula direção baseada na câmera
        Vector3 forward = cameraTransform != null ? cameraTransform.forward : transform.forward;
        Vector3 right = cameraTransform != null ? cameraTransform.right : transform.right;

        // Remove componente Y (não queremos voar)
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        // Calcula direção desejada
        Vector3 desiredDirection = (forward * currentMoveInput.y + right * currentMoveInput.x).normalized;

        // Controle no ar é reduzido
        float control = isGrounded ? 1f : airControl;

        // Interpola direção suavemente
        moveDirection = Vector3.Lerp(
            moveDirection,
            desiredDirection * currentSpeed,
            10f * control * Time.deltaTime
        );

        // Lida com slopes
        if (isGrounded && IsOnSlope())
        {
            moveDirection = Vector3.ProjectOnPlane(moveDirection, GetSlopeNormal());
        }

        // Aplica movimento horizontal
        velocity.x = moveDirection.x;
        velocity.z = moveDirection.z;
    }

    /// <summary>
    /// Executa pulo
    /// </summary>
    public void Jump(float force)
    {
        if (!isGrounded) return;

        velocity.y = Mathf.Sqrt(force * -2f * gravity);

        if (showDebug)
            Debug.Log($"[PlayerMotor] Jump! Velocity: {velocity.y:F2}");
    }

    #endregion

    #region GRAVITY & GROUND

    /// <summary>
    /// Aplica gravidade
    /// </summary>
    private void ApplyGravity()
    {
        if (isGrounded && velocity.y < 0)
        {
            // Pequena força para manter no chão
            velocity.y = -2f;
        }
        else
        {
            // Aplica gravidade
            velocity.y += gravity * Time.deltaTime;
        }

        // Força adicional em slopes para não flutuar
        if (isGrounded && IsOnSlope() && velocity.y < 0)
        {
            velocity.y -= slopeForce * Time.deltaTime;
        }
    }

    /// <summary>
    /// Verifica se está no chão
    /// </summary>
    private void CheckGround()
    {
        // CharacterController já faz isso, mas vamos validar melhor
        isGrounded = characterController.isGrounded;

        // Raycast adicional para mais precisão
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        if (Physics.Raycast(origin, Vector3.down, groundCheckDistance + 0.1f, groundMask))
        {
            isGrounded = true;
        }

        if (showDebug)
            Debug.DrawRay(origin, Vector3.down * (groundCheckDistance + 0.1f), isGrounded ? Color.green : Color.red);
    }

    /// <summary>
    /// Verifica se está em slope
    /// </summary>
    private bool IsOnSlope()
    {
        if (!isGrounded) return false;

        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out hit, 0.5f, groundMask))
        {
            float angle = Vector3.Angle(Vector3.up, hit.normal);
            return angle > 0.1f && angle <= maxSlopeAngle;
        }

        return false;
    }

    /// <summary>
    /// Retorna normal do slope
    /// </summary>
    private Vector3 GetSlopeNormal()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out hit, 0.5f, groundMask))
        {
            return hit.normal;
        }
        return Vector3.up;
    }

    #endregion

    #region PUBLIC METHODS

    /// <summary>
    /// Retorna velocidade atual
    /// </summary>
    public Vector3 GetVelocity() => velocity;

    /// <summary>
    /// Retorna se está no chão
    /// </summary>
    public bool IsGrounded() => isGrounded;

    /// <summary>
    /// Define velocidade manualmente (para knockback, dash, etc)
    /// </summary>
    public void SetVelocity(Vector3 newVelocity)
    {
        velocity = newVelocity;
    }

    /// <summary>
    /// Adiciona força (para explosões, knockback)
    /// </summary>
    public void AddForce(Vector3 force)
    {
        velocity += force;
    }

    /// <summary>
    /// Teleporta o jogador
    /// </summary>
    public void Teleport(Vector3 position)
    {
        characterController.enabled = false;
        transform.position = position;
        velocity = Vector3.zero;
        characterController.enabled = true;

        if (showDebug)
            Debug.Log($"[PlayerMotor] Teleported to {position}");
    }

    #endregion

    #region DEBUG

    private void OnDrawGizmos()
    {
        if (!showDebug) return;

        // Desenha velocidade
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + velocity.normalized * 2f);

        // Desenha direção de movimento
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + moveDirection.normalized * 1.5f);
    }

    private void OnGUI()
    {
        if (!showDebug || !isLocalPlayer) return;

        GUILayout.BeginArea(new Rect(10, 220, 300, 150));
        GUILayout.Box("=== PLAYER MOTOR ===");
        GUILayout.Label($"Velocity: {velocity.magnitude:F2} m/s");
        GUILayout.Label($"Velocity Y: {velocity.y:F2}");
        GUILayout.Label($"Grounded: {isGrounded}");
        GUILayout.Label($"On Slope: {IsOnSlope()}");
        GUILayout.EndArea();
    }

    #endregion
}