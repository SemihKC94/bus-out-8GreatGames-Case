using UnityEngine;
using SKC.Grid; 
using SKC.Bus;
using SKC.GameLogic;

namespace SKC.Inputs
{
    public class PlayerInputController : MonoBehaviour
    {
        // Singleton pattern 
        public static PlayerInputController Instance { get; private set; }

        [Header("References")]
        [SerializeField] private LayerMask busPartLayer; 
        [SerializeField] private Camera mainCamera; 

        private LevelGridGenerator gridGenerator;
        private GridOccupancyManager occupancyManager;

        private BusController selectedBus; 
        private BusPart clickedBusPart; 
        private GridPosition initialClickGridPos; 
        private GridPosition lastMouseGridPos; 

        private bool isDragging = false; 
        public bool IsDragging
        {
            get { return isDragging; }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    enabled = false;
                    return;
                }
            }
            
            gridGenerator = LevelGridGenerator.Instance;
            occupancyManager = GridOccupancyManager.Instance;
        }

        private void Update()
        {
            // Mouse Down (Click)
            if (Input.GetMouseButtonDown(0)) 
            {
                HandleMouseDown();
            }

            // Mouse Drag
            if (isDragging && Input.GetMouseButton(0)) 
            {
                HandleMouseDrag();
            }

            // Mouse Up (Release)
            if (Input.GetMouseButtonUp(0)) 
            {
                HandleMouseUp();
            }
        }

        private void HandleMouseDown()
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, busPartLayer))
            {
                clickedBusPart = hit.collider.GetComponentInParent<BusPart>();
                if (clickedBusPart != null)
                {
                    selectedBus = clickedBusPart.busController;
                    if (selectedBus != null)
                    {
                        isDragging = true;
                        initialClickGridPos = clickedBusPart.gridPosition; 
                        lastMouseGridPos = initialClickGridPos; 
                    }
                }
            }
            else
            {
                HandleMouseUp(); 
            }
        }

        private void HandleMouseDrag()
        {
            if (selectedBus == null || clickedBusPart == null) return;

            GridPosition currentMouseGridPos = GetMouseGridPosition();
            
            if (!currentMouseGridPos.Equals(lastMouseGridPos))
            {
                int dx = currentMouseGridPos.x - lastMouseGridPos.x;
                int dy = currentMouseGridPos.y - lastMouseGridPos.y;

                BusDirection direction = BusDirection.None;
                
                if (Mathf.Abs(dx) == 1 && dy == 0)
                {
                    direction = dx > 0 ? BusDirection.Right : BusDirection.Left;
                }
                else if (Mathf.Abs(dy) == 1 && dx == 0)
                {
                    direction = dy > 0 ? BusDirection.Forward : BusDirection.Backward;
                }
                
                if (direction != BusDirection.None)
                {
                    selectedBus.AttemptMove(clickedBusPart, direction);
                }
                
                lastMouseGridPos = currentMouseGridPos;
            }
        }

        private void HandleMouseUp()
        {
            if (isDragging)
            {
                Debug.Log($"<color=lime>Bus {selectedBus?.name} released.</color>");
                selectedBus = null;
                clickedBusPart = null;
                isDragging = false;
            }
        }
        
        private GridPosition GetMouseGridPosition()
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            
            if (gridGenerator == null)
            {
                return lastMouseGridPos;
            }
            
            Plane groundPlane = new Plane(Vector3.up, new Vector3(0, gridGenerator.transform.position.y, 0)); 

            float distance;
            if (groundPlane.Raycast(ray, out distance))
            {
                Vector3 worldPoint = ray.GetPoint(distance);
                return gridGenerator.WorldToGrid(worldPoint);
            }
            return lastMouseGridPos; 
        }
        
        private BusDirection GetDirectionFromGridMovement(GridPosition from, GridPosition to)
        {
            int dx = to.x - from.x;
            int dy = to.y - from.y;
            
            if (Mathf.Abs(dx) > 1 || Mathf.Abs(dy) > 1 || (Mathf.Abs(dx) == 1 && Mathf.Abs(dy) == 1))
            {
                return BusDirection.None;
            }

            if (dx == 0 && dy == 1) return BusDirection.Forward; // Moved up
            if (dx == 0 && dy == -1) return BusDirection.Backward; // Moved down
            if (dx == -1 && dy == 0) return BusDirection.Left; // Moved left
            if (dx == 1 && dy == 0) return BusDirection.Right; // Moved right

            return BusDirection.None;
        }
        
        private BusDirection DetermineActualMoveDirection(BusDirection desiredDirection)
        {
            if (selectedBus == null || clickedBusPart == null) return BusDirection.None;
            
            if (clickedBusPart.partType == BusPartType.Head || clickedBusPart.partType == BusPartType.Body)
            {
                return desiredDirection;
            }
            
            else if (clickedBusPart.partType == BusPartType.Tail)
            {
                switch (desiredDirection)
                {
                    case BusDirection.Forward: return BusDirection.Backward;
                    case BusDirection.Backward: return BusDirection.Forward;
                    case BusDirection.Left: return BusDirection.Right;
                    case BusDirection.Right: return BusDirection.Left;
                    default: return BusDirection.None;
                }
            }
            return BusDirection.None;
        }
    }
}