using UnityEngine;

namespace SKC.Bus
{
    public class BusPartVisual : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MeshRenderer meshRenderer;
        
        private bool _isCurrentlyMoving = false;
        private void Awake()
        {
            if (meshRenderer == null)
            {
                meshRenderer = GetComponent<MeshRenderer>();
            }
        }

        private void ApplyColor(Color colorToApply)
        {
            if (meshRenderer != null && meshRenderer.material != null)
            {
                meshRenderer.material.color = colorToApply;
            }
        }
        
        public void SetMoving(bool moving)
        {
            _isCurrentlyMoving = moving;
        }

        public void SetBaseColor(Color baseColor)
        {
            ApplyColor(baseColor);
        }
 
        private void OnDestroy()
        {
            if (meshRenderer != null && meshRenderer.material != null)
            {
                Destroy(meshRenderer.material);
            }
        }
    }
}