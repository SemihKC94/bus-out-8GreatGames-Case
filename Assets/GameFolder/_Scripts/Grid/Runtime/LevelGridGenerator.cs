using System;
using UnityEngine;
using System.Collections.Generic;
using SKC.GameLogic;
using SKC.Passenger;

namespace SKC.Grid
{
    public class LevelGridGenerator : MonoBehaviour
    {
        public TextAsset gridDataJsonFile; 

        [Header("Border Visuals")]
        [SerializeField ] private  GameObject borderPrefab;
        [SerializeField ] private  GameObject emptyCellPrefab;
        [SerializeField ] private  GameObject blockedCellPrefab;
        [SerializeField ] private  GameObject playerStartCellPrefab;
        [SerializeField ] private  GameObject targetCellPrefab;
        [SerializeField ] private  GameObject nothingCellPrefab; // Optional, can be null
        
        [Header("Destination Visuals")]
        [SerializeField ] private  TextAsset destinationDataJsonFile;
        [SerializeField ] private  GameObject[] destinationPrefabs;

        public static event Action<DestinationQueue> OnDestinationSpawned;
        
        private Dictionary<GridPosition, GameObject> instantiatedCellVisuals =
            new Dictionary<GridPosition, GameObject>();

        public int GridSizeX { get; private set; }
        public int GridSizeY { get; private set; }
        public float CellSize { get; private set; }
        private GridContentType[,] runtimeGridTypes;
        
        public static LevelGridGenerator Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void Initialize(TextAsset gridDataJsonFile, TextAsset destinationDataJsonFile)
        {
            this.gridDataJsonFile = gridDataJsonFile;
            this.destinationDataJsonFile = destinationDataJsonFile;
            
            GridLevelData loadedGridData = null;
            try
            {
                string jsonString = gridDataJsonFile.text;
                loadedGridData = JsonUtility.FromJson<GridLevelData>(jsonString);
                if (loadedGridData == null)
                {
                    enabled = false;
                    return;
                }
            }
            catch (System.Exception e)
            {
                enabled = false;
                return;
            }
            
            GridSizeX = loadedGridData.gridSizeX;
            GridSizeY = loadedGridData.gridSizeY;
            CellSize = loadedGridData.cellSize;

            if (emptyCellPrefab == null && blockedCellPrefab == null && playerStartCellPrefab == null &&
                targetCellPrefab == null)
            {
                enabled = false;
                return;
            }

            CheckPrefabForVisualGridCell(emptyCellPrefab, "EmptyCellPrefab");
            CheckPrefabForVisualGridCell(blockedCellPrefab, "BlockedCellPrefab");
            CheckPrefabForVisualGridCell(playerStartCellPrefab, "PlayerStartCellPrefab");
            CheckPrefabForVisualGridCell(targetCellPrefab, "TargetCellPrefab");
            CheckPrefabForVisualGridCell(nothingCellPrefab, "NothingCellPrefab (Optional)");
            
            runtimeGridTypes = new GridContentType[GridSizeX, GridSizeY];
            int nothingCountInParsedData = 0;
            int nonNothingCountInParsedData = 0;
            int processedCellCountFromParsedData = 0;

            if (loadedGridData.cellTypes != null &&
                loadedGridData.cellTypes.Length == GridSizeX * GridSizeY) 
            {

                for (int j = 0; j < GridSizeY; j++)
                {
                    for (int i = 0; i < GridSizeX; i++)
                    {
                        int index = j * GridSizeX + i; // Calculate 1D index from 2D (y, x)
                        if (index < loadedGridData.cellTypes.Length) // Safety check
                        {
                            GridContentType cellType = loadedGridData.cellTypes[index];
                            runtimeGridTypes[i, j] = cellType; // Store in 2D array (x, y)
                            processedCellCountFromParsedData++;

                            if (cellType == GridContentType.Nothing)
                            {
                                nothingCountInParsedData++;
                            }
                            else
                            {
                                nonNothingCountInParsedData++;
                            }
                        }
                        else
                        {
                            runtimeGridTypes[i, j] = GridContentType.Nothing; // Default to Nothing if out of bounds
                            nothingCountInParsedData++;
                        }
                    }
                }
            }
            else
            {
                enabled = false;
                return;
            }
            
            GenerateGridVisuals();
            SpawnDestinationVisuals();
            SpawnBorders();
        }
        
        private void CheckPrefabForVisualGridCell(GameObject prefab, string prefabName)
        {
            if (prefab != null)
            {
                if (prefab.GetComponent<VisualGridCell>() == null)
                {
                    Debug.LogError(
                        $"SKC.Grid.LevelGridGenerator: {prefabName} prefab is assigned but DOES NOT have a VisualGridCell component.",
                        prefab);
                }
            }
        }

        private void GenerateGridVisuals()
        {
            ClearGridVisuals();

            int instantiatedCount = 0;

            for (int y = 0; y < GridSizeY; y++)
            {
                for (int x = 0; x < GridSizeX; x++)
                {
                    GridPosition currentGridPos = new GridPosition(x, y);
                    GridContentType
                        cellType = runtimeGridTypes[x, y]; // Accessing the local, copied runtimeGridTypes array

                    GameObject prefabToInstantiate = GetPrefabForCellType(cellType);

                    if (prefabToInstantiate != null) // Only instantiate if a prefab is assigned for this cell type
                    {
                        GameObject cellVisual = Instantiate(prefabToInstantiate, GetWorldPosition(currentGridPos),
                            Quaternion.identity, transform);
                        cellVisual.name = $"Cell_{cellType}_{currentGridPos.x}_{currentGridPos.y}";

                        VisualGridCell
                            visualCellComp = cellVisual.GetComponent<VisualGridCell>(); // Get existing component
                        if (visualCellComp == null)
                        {

                            Destroy(cellVisual); // Destroy the misconfigured object
                            continue; 
                        }

                        visualCellComp.gridPosition = currentGridPos;
                        visualCellComp.currentType = cellType;
                        
                        instantiatedCellVisuals.Add(currentGridPos,
                            cellVisual); // Store reference to the instantiated object
                        instantiatedCount++;
                    }
                    else if (cellType != GridContentType.Nothing) // Warn if a non-Nothing cell has no assigned prefab
                    {
                        Debug.LogWarning(
                            $"SKC.Grid.LevelGridGenerator: No prefab assigned for cell type {cellType} at {currentGridPos}.");
                    }
                }
            }
        }
        
    private void SpawnDestinationVisuals()
    {
        if (destinationDataJsonFile == null || destinationPrefabs == null || destinationPrefabs.Length == 0) return;

        DestinationLevelData levelData = JsonUtility.FromJson<DestinationLevelData>(destinationDataJsonFile.text);
        if (levelData == null || levelData.destinations == null) return;

        GameObject destinationParent = new GameObject("SpawnedDestinationVisuals");
        destinationParent.transform.SetParent(this.transform);

        foreach (DestinationData destination in levelData.destinations)
        {
            int prefabIndex = (int)destination.targetColor;
            if (prefabIndex < 0 || prefabIndex >= destinationPrefabs.Length || destinationPrefabs[prefabIndex] == null)
            {
                continue;
            }

            if (!IsValidGridPosition(destination.position))
            {
                continue;
            }
            
            Quaternion targetRotation = Quaternion.identity;
            GridPosition pos = destination.position;
            
            if (IsCellPlayable(new GridPosition(pos.x, pos.y - 1)))
            {
                targetRotation = Quaternion.Euler(0, 0, 0);
            }
            else if (IsCellPlayable(new GridPosition(pos.x, pos.y + 1)))
            {
                targetRotation = Quaternion.Euler(0, 180, 0);
            }
            else if (IsCellPlayable(new GridPosition(pos.x + 1, pos.y)))
            {
                targetRotation = Quaternion.Euler(0, -90, 0);
            }
            else if (IsCellPlayable(new GridPosition(pos.x - 1, pos.y)))
            {
                targetRotation = Quaternion.Euler(0, 90, 0);
            }

            GameObject prefabToSpawn = destinationPrefabs[prefabIndex];
            Vector3 worldPosition = GetWorldPosition(destination.position);
            
            GameObject spawnedObj = Instantiate(prefabToSpawn, worldPosition, targetRotation, destinationParent.transform);
            spawnedObj.name = $"Destination_{destination.targetColor}_{pos.x}_{pos.y}";
            
            DestinationQueue queue = spawnedObj.GetComponent<DestinationQueue>();
            queue.Initialize(destination.targetColor);

            OnDestinationSpawned?.Invoke(queue);
        }
    }
    
        private GameObject GetPrefabForCellType(GridContentType type)
        {
            switch (type)
            {
                case GridContentType.Empty: return emptyCellPrefab;
                case GridContentType.Blocked: return blockedCellPrefab;
                case GridContentType.PlayerStart: return playerStartCellPrefab;
                case GridContentType.Target: return targetCellPrefab;
                case GridContentType.Nothing: return nothingCellPrefab;
                default: return null; 
            }
        }
        
        private void SpawnBorders()
        {
            GameObject borderParent = new GameObject("Borders");
            borderParent.transform.SetParent(this.transform);
            
            for (int x = 0; x < GridSizeX; x++)
            {
                for (int y = 0; y <= GridSizeY; y++)
                {
                    var upperCellPos = new GridPosition(x, y);
                    var lowerCellPos = new GridPosition(x, y - 1);

                    if (IsCellPlayable(upperCellPos) != IsCellPlayable(lowerCellPos))
                    {
                        float worldX = GetWorldPosition(new GridPosition(x, 0)).x;
                        float worldZ = transform.position.z + y * CellSize - (GridSizeY * CellSize) / 2f;
                        Vector3 borderPosition = new Vector3(worldX, transform.position.y, worldZ);
                        
                        Quaternion borderRotation = Quaternion.Euler(0, 90, 0);
                        Instantiate(borderPrefab, borderPosition, borderRotation, borderParent.transform);
                    }
                }
            }
            
            for (int y = 0; y < GridSizeY; y++)
            {
                for (int x = 0; x <= GridSizeX; x++)
                {
                    var rightCellPos = new GridPosition(x, y);
                    var leftCellPos = new GridPosition(x - 1, y);

                    if (IsCellPlayable(rightCellPos) != IsCellPlayable(leftCellPos))
                    {
                        float worldX = transform.position.x + x * CellSize - (GridSizeX * CellSize) / 2f;
                        float worldZ = GetWorldPosition(new GridPosition(0, y)).z;
                        Vector3 borderPosition = new Vector3(worldX, transform.position.y, worldZ);
                        
                        Instantiate(borderPrefab, borderPosition, Quaternion.identity, borderParent.transform);
                    }
                }
            }
        }

        private bool IsCellPlayable(GridPosition pos)
        {
            if (!IsValidGridPosition(pos))
            {
                return false; 
            }

            if (GetCellType(pos) == GridContentType.Nothing)
            {
                return false; 
            }

            return true; 
        }
        
        private void ClearGridVisuals()
        {
            foreach (var kvp in instantiatedCellVisuals)
            {
                if (kvp.Value != null)
                {
                    Destroy(kvp.Value);
                }
            }

            instantiatedCellVisuals.Clear();

            while (transform.childCount > 0)
            {
                Destroy(transform.GetChild(0).gameObject);
            }
        }
        
        public GridPosition WorldToGrid(Vector3 worldPos)
        {
            float startX = transform.position.x;
            float startZ = transform.position.z;

            startX -= (GridSizeX * CellSize) / 2f;
            startZ -= (GridSizeY * CellSize) / 2f;

            int x = Mathf.FloorToInt((worldPos.x - startX) / CellSize);
            int y = Mathf.FloorToInt((worldPos.z - startZ) / CellSize); // Maps world Z to grid Y

            return new GridPosition(x, y);
        }
        
        public Vector3 GetWorldPosition(GridPosition gridPos)
        {
            float worldX = transform.position.x + gridPos.x * CellSize + CellSize / 2f;
            float worldZ = transform.position.z + gridPos.y * CellSize + CellSize / 2f; // Maps grid Y to world Z
            float worldY = transform.position.y; // Fixed Y-height for the floor (ground level)

            worldX -= (GridSizeX * CellSize) / 2f;
            worldZ -= (GridSizeY * CellSize) / 2f;

            return new Vector3(worldX, worldY, worldZ);
        }
        
        public bool IsValidGridPosition(GridPosition pos)
        {
            return pos.x >= 0 && pos.x < GridSizeX &&
                   pos.y >= 0 && pos.y < GridSizeY;
        }
        
        private bool IsValidGridPosition(GridPosition pos, int sizeX, int sizeY)
        {
            return pos.x >= 0 && pos.x < sizeX &&
                   pos.y >= 0 && pos.y < sizeY;
        }
        
        public GridContentType GetCellType(GridPosition pos)
        {
            if (IsValidGridPosition(pos))
            {
                return runtimeGridTypes[pos.x, pos.y];
            }

            return GridContentType.Nothing;
        }
    }
}