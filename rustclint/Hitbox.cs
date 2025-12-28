using UnityEngine;

namespace RustlikeGame.Combat
{
    /// <summary>
    /// Componente que define uma hitbox no modelo do player
    /// Usado para detectar headshots, bodyshots, etc
    /// </summary>
    public class Hitbox : MonoBehaviour
    {
        [Header("Hitbox Type")]
        [SerializeField] private HitboxType type = HitboxType.Body;

        [Header("Visual Feedback")]
        [SerializeField] private bool showDebugGizmo = true;
        [SerializeField] private Color gizmoColor = Color.red;

        public HitboxType Type => type;

        private void OnDrawGizmos()
        {
            if (!showDebugGizmo) return;

            Gizmos.color = gizmoColor;
            
            var collider = GetComponent<Collider>();
            if (collider != null)
            {
                if (collider is BoxCollider box)
                {
                    Gizmos.matrix = transform.localToWorldMatrix;
                    Gizmos.DrawWireCube(box.center, box.size);
                }
                else if (collider is SphereCollider sphere)
                {
                    Gizmos.DrawWireSphere(transform.position + sphere.center, sphere.radius);
                }
                else if (collider is CapsuleCollider capsule)
                {
                    // Simplified capsule gizmo
                    Gizmos.DrawWireSphere(transform.position + capsule.center, capsule.radius);
                }
            }
        }
    }
}
