using System;
using UnityEngine;

namespace SKC.Grid
{
    [Serializable]
    public class GridLevelData
    {
        public int gridSizeX;
        public int gridSizeY;
        public float cellSize;
        
        public GridContentType[] cellTypes;

        public GridLevelData()
        {
            gridSizeX = 0;
            gridSizeY = 0;
            cellSize = 1f;
            cellTypes = new GridContentType[0]; // Empty array by default
        }

        public void Initialize(int x, int y, float cellS)
        {
            gridSizeX = x;
            gridSizeY = y;
            cellSize = cellS;
            cellTypes = new GridContentType[gridSizeX * gridSizeY]; // Initialize array size
            for (int i = 0; i < cellTypes.Length; i++)
            {
                cellTypes[i] = GridContentType.Nothing; // All Nothing initially
            }
        }
        
        private int GetIndex(GridPosition pos)
        {
            return pos.y * gridSizeX + pos.x;
        }
        
        public void UpdateCellType(GridPosition pos, GridContentType newType)
        {
            if (pos.x >= 0 && pos.x < gridSizeX && pos.y >= 0 && pos.y < gridSizeY)
            {
                int index = GetIndex(pos);
                if (cellTypes != null && index >= 0 && index < cellTypes.Length)
                {
                    cellTypes[index] = newType;
                }
                else
                {
                    Debug.LogWarning($"SKC.Grid.GridLevelData: Array index out of bounds for position {pos}.");
                }
            }
            else
            {
                Debug.LogWarning($"SKC.Grid.GridLevelData: Position {pos} is out of grid bounds ({gridSizeX}x{gridSizeY}).");
            }
        }
        
        public GridContentType GetCellType(GridPosition pos)
        {
            if (pos.x >= 0 && pos.x < gridSizeX && pos.y >= 0 && pos.y < gridSizeY)
            {
                int index = GetIndex(pos);
                if (cellTypes != null && index >= 0 && index < cellTypes.Length)
                {
                    return cellTypes[index];
                }
            }
            return GridContentType.Nothing; // Default for invalid positions
        }
        
        public void RevalidateSize(int newX, int newY, float newCellS)
        {
            if (gridSizeX == newX && gridSizeY == newY && cellSize == newCellS && cellTypes != null && cellTypes.Length == newX * newY)
            {
                return;
            }

            GridContentType[] tempNewCellTypes = new GridContentType[newX * newY];
            
            for (int j = 0; j < Mathf.Min(newY, gridSizeY); j++)
            {
                for (int i = 0; i < Mathf.Min(newX, gridSizeX); i++)
                {
                    int oldIndex = j * gridSizeX + i;
                    int newIndex = j * newX + i;
                    if (cellTypes != null && oldIndex < cellTypes.Length)
                    {
                        tempNewCellTypes[newIndex] = cellTypes[oldIndex];
                    }
                    else
                    {
                        tempNewCellTypes[newIndex] = GridContentType.Nothing; // Default for old invalid cells
                    }
                }
            }
            
            for (int j = 0; j < newY; j++)
            {
                for (int i = 0; i < newX; i++)
                {
                    int newIndex = j * newX + i;
                    if (tempNewCellTypes[newIndex] == GridContentType.Nothing && (j >= gridSizeY || i >= gridSizeX))
                    {
                         tempNewCellTypes[newIndex] = GridContentType.Nothing; 
                    }
                }
            }

            gridSizeX = newX;
            gridSizeY = newY;
            cellSize = newCellS;
            cellTypes = tempNewCellTypes; 
        }
    }
}