using System.Collections.Generic;
using UnityEngine;

// Adicione isto como um ScriptableObject
[CreateAssetMenu(fileName = "UITheme", menuName = "Rustlike/UI Theme")]
public class UITheme : ScriptableObject
{
    [Header("🎨 Cores Principais")]
    public Color backgroundColor = new Color(0.08f, 0.08f, 0.08f, 0.95f);      // #141414F2
    public Color panelColor = new Color(0.12f, 0.12f, 0.12f, 0.98f);           // #1F1F1FFA
    public Color slotColor = new Color(0.16f, 0.16f, 0.16f, 1f);               // #292929
    public Color slotHoverColor = new Color(0.22f, 0.22f, 0.22f, 1f);          // #383838
    public Color slotSelectedColor = new Color(0.4f, 0.32f, 0.15f, 1f);        // #664D26
    
    [Header("💚 Stats Colors")]
    public Color healthColor = new Color(0.8f, 0.2f, 0.2f, 1f);                // Vermelho
    public Color healthLowColor = new Color(1f, 0.1f, 0.1f, 1f);               // Vermelho brilhante
    public Color hungerColor = new Color(1f, 0.6f, 0.2f, 1f);                  // Laranja
    public Color thirstColor = new Color(0.3f, 0.7f, 1f, 1f);                  // Azul claro
    public Color temperatureColor = new Color(0.5f, 0.5f, 0.5f, 1f);           // Cinza
    
    [Header("✨ Accent Colors")]
    public Color accentGold = new Color(1f, 0.8f, 0.3f, 1f);                   // #FFD14D
    public Color accentGreen = new Color(0.4f, 0.8f, 0.4f, 1f);                // Verde sucesso
    public Color accentRed = new Color(0.9f, 0.3f, 0.3f, 1f);                  // Vermelho erro
    
    [Header("📝 Text Colors")]
    public Color textPrimary = new Color(0.95f, 0.95f, 0.95f, 1f);             // Branco suave
    public Color textSecondary = new Color(0.7f, 0.7f, 0.7f, 1f);              // Cinza claro
    public Color textDisabled = new Color(0.4f, 0.4f, 0.4f, 1f);               // Cinza escuro
}