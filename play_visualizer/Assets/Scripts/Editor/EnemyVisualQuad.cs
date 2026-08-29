using UnityEditor;
using UnityEngine;

namespace PlayVisualizer.EditorTools
{
    /// <summary>
    /// Shared helper: builds an enemy's procedural "Visual" child — a Quad with an additive shader
    /// material — parented to the enemy root. The collider/behaviour stay on the root; the visual is
    /// what music pulses/jitter modulate (never the collider). Used by the Phase 3 / Phase 6 setups.
    /// </summary>
    internal static class EnemyVisualQuad
    {
        public static Renderer Build(Transform root, Material mat, out Transform visualRoot)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = "Visual";
            Object.DestroyImmediate(q.GetComponent<MeshCollider>());
            q.transform.SetParent(root, false);

            var mr = q.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.sortingOrder = 8;

            visualRoot = q.transform;
            return mr;
        }
    }
}
