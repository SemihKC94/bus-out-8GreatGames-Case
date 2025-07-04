using UnityEngine;
using SKC.GameLogic;

namespace SKC.Grid
{

    public class VisualGridCell : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MeshRenderer meshRenderer;

        [Header("Cell Type Colors")]
        [SerializeField] private Color emptyColor = Color.white;
        [SerializeField] private Color blockedColor = Color.black;
        [SerializeField] private Color playerStartColor = Color.blue;
        [SerializeField] private Color targetColor = Color.green;
        [SerializeField] private Color nothingColor = Color.gray;

        [Header("Highlight Colors")]
        [SerializeField] private Color highlightColor = Color.yellow; 
        [SerializeField] private Color occupiedHighlightColor = Color.red; 

        // Internal state
        public GridPosition gridPosition;
        public GridContentType currentType;
        private Color originalColor; 
        private bool isHighlighted = false; 

        // Cached instances
        private LevelGridGenerator gridGenerator;
        private GridOccupancyManager occupancyManager;

        private void Start()
        {
            if (meshRenderer == null)
            {
                meshRenderer = GetComponentInParent<MeshRenderer>();
            }

            gridGenerator = LevelGridGenerator.Instance;
            occupancyManager = GridOccupancyManager.Instance;
            
        }
        
        public void Initialize(GridPosition pos)
        {
            gridPosition = pos;
            UpdateCellVisual();
        }
        
        public void UpdateCellVisual()
        {
            if (gridGenerator == null || meshRenderer == null) return;

            currentType = gridGenerator.GetCellType(gridPosition);
            SetBaseColor(currentType); 
            originalColor = meshRenderer.material.color; 
        }
        
        private void SetBaseColor(GridContentType type)
        {
            switch (type)
            {
                case GridContentType.Empty:
                    meshRenderer.material.color = emptyColor;
                    break;
                case GridContentType.Blocked:
                    meshRenderer.material.color = blockedColor;
                    break;
                case GridContentType.PlayerStart:
                    meshRenderer.material.color = playerStartColor;
                    break;
                case GridContentType.Target:
                    meshRenderer.material.color = targetColor;
                    break;
                case GridContentType.Nothing:
                    meshRenderer.material.color = nothingColor;
                    break;
                default:
                    meshRenderer.material.color = emptyColor;
                    break;
            }
        }
        
        public void HighlightCell()
        {
            if (isHighlighted) return; 
            
            if (occupancyManager != null && occupancyManager.IsCellOccupied(gridPosition))
            {
                meshRenderer.material.color = occupiedHighlightColor;
            }
            else
            {
                meshRenderer.material.color = highlightColor;
            }
            isHighlighted = true;
        }
        
        public void NormalizeCell()
        {
            if (!isHighlighted) return; 

            meshRenderer.material.color = originalColor;
            isHighlighted = false;
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