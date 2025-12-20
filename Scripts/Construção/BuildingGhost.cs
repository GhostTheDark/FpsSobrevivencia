using UnityEngine;

/// <summary>
/// Preview visual de construção (ghost)
/// Mostra se placement é válido ou não
/// </summary>
public class BuildingGhost : MonoBehaviour
{
    [Header("Materials")]
    private Material validMaterial;
    private Material invalidMaterial;

    [Header("State")]
    private bool isValid = false;
    private bool isInitialized = false;

    [Header("Visual")]
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseIntensity = 0.2f;
    [SerializeField] private Color validColor = new Color(0, 1, 0, 0.5f);
    [SerializeField] private Color invalidColor = new Color(1, 0, 0, 0.5f);

    // Componentes
    private MeshRenderer[] meshRenderers;
    private Material[] originalMaterials;
    private Material ghostMaterial;

    /// <summary>
    /// Inicializa o ghost
    /// </summary>
    public void Initialize(Material validMat, Material invalidMat)
    {
        validMaterial = validMat;
        invalidMaterial = invalidMat;

        // Pega todos os renderers
        meshRenderers = GetComponentsInChildren<MeshRenderer>();
        originalMaterials = new Material[meshRenderers.Length];

        // Salva materiais originais e aplica ghost
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            originalMaterials[i] = meshRenderers[i].material;
            
            // Cria material ghost
            if (ghostMaterial == null)
            {
                ghostMaterial = CreateGhostMaterial(originalMaterials[i]);
            }
            
            meshRenderers[i].material = ghostMaterial;
        }

        isInitialized = true;

        // Inicia como inválido
        SetValid(false);
    }

    private void Update()
    {
        if (!isInitialized) return;

        // Efeito de pulse
        ApplyPulseEffect();
    }

    /// <summary>
    /// Define se placement é válido
    /// </summary>
    public void SetValid(bool valid)
    {
        if (!isInitialized) return;

        isValid = valid;

        // Atualiza cor do material
        Color targetColor = valid ? validColor : invalidColor;
        
        if (ghostMaterial != null)
        {
            ghostMaterial.color = targetColor;
        }
    }

    /// <summary>
    /// Cria material transparente para ghost
    /// </summary>
    private Material CreateGhostMaterial(Material baseMaterial)
    {
        Material mat = new Material(Shader.Find("Standard"));
        
        // Configura como transparente
        mat.SetFloat("_Mode", 3); // Transparent mode
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;

        // Copia textura se tiver
        if (baseMaterial.HasProperty("_MainTex"))
        {
            mat.mainTexture = baseMaterial.mainTexture;
        }

        // Define cor inicial
        mat.color = invalidColor;

        return mat;
    }

    /// <summary>
    /// Aplica efeito de pulse ao ghost
    /// </summary>
    private void ApplyPulseEffect()
    {
        if (ghostMaterial == null) return;

        // Calcula pulse
        float pulse = Mathf.Sin(Time.time * pulseSpeed) * pulseIntensity;
        
        // Aplica ao alpha
        Color currentColor = isValid ? validColor : invalidColor;
        currentColor.a += pulse;
        currentColor.a = Mathf.Clamp01(currentColor.a);

        ghostMaterial.color = currentColor;
    }

    /// <summary>
    /// Retorna se é válido
    /// </summary>
    public bool IsValid() => isValid;

    private void OnDestroy()
    {
        // Cleanup material
        if (ghostMaterial != null)
        {
            Destroy(ghostMaterial);
        }
    }
}