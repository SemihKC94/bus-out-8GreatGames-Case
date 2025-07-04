using UnityEngine;
using System.Collections.Generic;
using SKC.Grid;

namespace SKC.GameLogic
{
    public class GridOccupancyManager : MonoBehaviour
    {
        public static GridOccupancyManager Instance { get; private set; }

        private Dictionary<GridPosition, GameObject> occupiedCellsByEntities = new Dictionary<GridPosition, GameObject>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public bool RegisterOccupiedCell(GridPosition pos, GameObject entity)
        {
            if (occupiedCellsByEntities.ContainsKey(pos))
            {
                if (occupiedCellsByEntities[pos] == entity)
                {
                    return true; 
                }
                return false;
            }

            occupiedCellsByEntities.Add(pos, entity);
            return true;
        }
        public bool UnregisterOccupiedCell(GridPosition pos, GameObject entity)
        {
            if (occupiedCellsByEntities.ContainsKey(pos))
            {
                if (occupiedCellsByEntities[pos] == entity)
                {
                    occupiedCellsByEntities.Remove(pos);
                    return true;
                }
                return false; 
            }

            return false;
        }
        
        public bool IsCellOccupied(GridPosition pos)
        {
            return occupiedCellsByEntities.ContainsKey(pos);
        }
        
        public bool IsCellOccupiedBySpecificEntity(GridPosition pos, GameObject entity)
        {
            if (occupiedCellsByEntities.TryGetValue(pos, out GameObject occupyingEntity))
            {
                return occupyingEntity == entity;
            }
            return false;
        }
        
        public GameObject GetEntityAtCell(GridPosition pos)
        {
            if (occupiedCellsByEntities.TryGetValue(pos, out GameObject occupyingEntity))
            {
                return occupyingEntity;
            }
            return null;
        }

        public void ClearOccupancy()
        {
            occupiedCellsByEntities.Clear();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}