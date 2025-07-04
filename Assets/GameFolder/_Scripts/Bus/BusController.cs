using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using SKC.Grid;
using SKC.GameLogic;
using System.Linq;
using SKC.Events;
using SKC.Passenger;

namespace SKC.Bus
{
    public enum BusDirection
    {
        None,
        Forward,
        Backward,
        Left,
        Right
    }
    
    public class BusController : MonoBehaviour
    {
        [Header("Bus Configuration")]
        [SerializeField ] private  List<BusPart> busParts = new List<BusPart>();
        [SerializeField ] private  float moveDuration = 0.2f; 
        
        [Space(1f), Header("Passenger Settings")]
        [SerializeField ] private  GameObject passengerPrefab;
        
        [SerializeField ] private  BusDirection currentDirection = BusDirection.None;
        [SerializeField ] private  bool isMoving = false;
        
        // privates
        private GridPosition targetHeadGridPosition;
        private LevelGridGenerator gridGenerator; 
        private GridOccupancyManager occupancyManager; 
        private Queue<Transform> availableSeats = new Queue<Transform>();
        private bool isBoarding = false;
        private const float BOARDING_INTERVAL = 0.20f;
        private int _passengerSize = 0;
        private int _currentPassengerSize = 0;
        private List<Transform> passengerSeats = new List<Transform>();
        private string _myTag = "Finish/";
        private bool _isReversed = false;
        private bool isInitializedBySpawner = false;
        
        
        public void InitializeFromSpawner(Dictionary<BusPart, GridPosition> partData, Color busColor, BusInfo busInfo)
        {
            gridGenerator = LevelGridGenerator.Instance;
            occupancyManager = GridOccupancyManager.Instance;
            
            busParts.Clear();
            _myTag = "Finish/" + busInfo.busColor.ToString();
            
            foreach (var kvp in partData)
            {
                BusPart part = kvp.Key;
                GridPosition gridPos = kvp.Value;

                part.busController = this;
                part.gridPosition = gridPos;
                _passengerSize += part.passengerCapacity;
                part.InteractTag = _myTag;
                Debug.Log(_myTag);
                
                BusPartVisual visual = part.GetComponent<BusPartVisual>();
                if (visual != null)
                {
                    visual.SetBaseColor(busColor);
                }

                busParts.Add(part);
                occupancyManager.RegisterOccupiedCell(gridPos, part.gameObject);
            }
            

            UpdateSeats();
            isInitializedBySpawner = true;
        }
        
        private void PrepareSeats()
        {
            availableSeats.Clear();
            foreach (var part in busParts)
            {
                foreach (var seat in part.passengerSeats)
                {
                    availableSeats.Enqueue(seat);
                }
            }
        }

        private void Update()
        {
            if (isMoving)
            {
                bool allPartsFinished = true;
                foreach (BusPart part in busParts)
                {
                    if (part != null)
                    {
                        part.Animate(Time.deltaTime, moveDuration);
                        if (!part.IsAnimationFinished())
                        {
                            allPartsFinished = false;
                        }
                    }
                }

                if (allPartsFinished)
                {
                    isMoving = false;
                    ApplyFinalBusPartPositions();
                }
            }
        }
        
        public bool AttemptMove(BusPart leadingPart, BusDirection direction)
        {
            if (isMoving || leadingPart == null) return false;
            
            if (leadingPart.partType == BusPartType.Head)
            {
                return TryMoveForward(direction);
            }
            else if (leadingPart.partType == BusPartType.Tail)
            {
                return TryMoveBackward(direction);
            }
            
            return false;
        }

        private bool TryMoveForward(BusDirection direction)
        {
            BusPart head = busParts[0];
            GridPosition targetPos = GetNextGridPosition(head.gridPosition, direction);
            if (!IsMoveValid(targetPos)) return false;
            PrepareForMovementAnimation(targetPos, direction, isReversed: false);
            return true;
        }

        private bool TryMoveBackward(BusDirection direction)
        {
            BusPart tail = busParts.Last();
            GridPosition targetPos = GetNextGridPosition(tail.gridPosition, direction);
            if (!IsMoveValid(targetPos)) return false;
            PrepareForMovementAnimation(targetPos, direction, isReversed: true);
            return true;
        }

        private bool IsMoveValid(GridPosition targetPos)
        {
            if (!gridGenerator.IsValidGridPosition(targetPos)) return false;
            GridContentType targetCellType = gridGenerator.GetCellType(targetPos);
            if (targetCellType == GridContentType.Blocked || targetCellType == GridContentType.Nothing) return false;
            if (occupancyManager.IsCellOccupied(targetPos)) return false;
            return true;
        }

        private void PrepareForMovementAnimation(GridPosition leadPartTargetPos, BusDirection direction, bool isReversed)
        {
            _isReversed = isReversed;
            List<GridPosition> newLogicalGridPositions = new List<GridPosition>();
            
            if (!isReversed)
            {
                newLogicalGridPositions.Add(leadPartTargetPos);
                for (int i = 0; i < busParts.Count - 1; i++)
                {
                    newLogicalGridPositions.Add(busParts[i].gridPosition);
                }
            }
            else
            {
                for (int i = 0; i < busParts.Count - 1; i++)
                {
                    newLogicalGridPositions.Add(busParts[i + 1].gridPosition); 
                }
                newLogicalGridPositions.Add(leadPartTargetPos);
            }

            UpdateOccupancyForNextMove(newLogicalGridPositions); 
            
            for (int i = 0; i < busParts.Count; i++)
            {
                Vector3 partTargetPos = gridGenerator.GetWorldPosition(newLogicalGridPositions[i]);
                Quaternion partTargetRot = GetTargetRotation(direction);
                busParts[i].StartAnimation(busParts[i].transform.position, partTargetPos, busParts[i].transform.rotation, partTargetRot);
            }
            
            isMoving = true;
        }
        private void UpdateOccupancyForNextMove(List<GridPosition> newLogicalGridPositions)
        {
            foreach (BusPart part in busParts)
            {
                if (part != null)
                {
                    occupancyManager.UnregisterOccupiedCell(part.gridPosition, part.gameObject);
                }
            }

            for (int i = 0; i < busParts.Count; i++)
            {
                BusPart currentPart = busParts[i];
                if (currentPart == null) continue;

                currentPart.gridPosition = newLogicalGridPositions[i];
                occupancyManager.RegisterOccupiedCell(currentPart.gridPosition, currentPart.gameObject);
            }
        }
        
        private void ApplyFinalBusPartPositions()
        {
            foreach (BusPart part in busParts)
            {
                if (part != null)
                {
                    Vector3 finalWorldPos = gridGenerator.GetWorldPosition(part.gridPosition);
                    part.SetWorldPositionImmediately(finalWorldPos);
                }
            }
        }
        
        private GridPosition GetNextGridPosition(GridPosition currentPos, BusDirection direction)
        {
            switch (direction)
            {
                case BusDirection.Forward:  return new GridPosition(currentPos.x, currentPos.y + 1);
                case BusDirection.Backward: return new GridPosition(currentPos.x, currentPos.y - 1);
                case BusDirection.Left:     return new GridPosition(currentPos.x - 1, currentPos.y);
                case BusDirection.Right:    return new GridPosition(currentPos.x + 1, currentPos.y);
                default:                    return currentPos;
            }
        }
        
        private Quaternion GetTargetRotation(BusDirection direction)
        {
            switch (direction)
            {
                case BusDirection.Forward:  return _isReversed ? Quaternion.Euler(0, 180, 0) : Quaternion.Euler(0, 0, 0);
                case BusDirection.Backward: return _isReversed ? Quaternion.Euler(0, 0, 0) : Quaternion.Euler(0, 180, 0);
                case BusDirection.Left:     return _isReversed ? Quaternion.Euler(0, 90, 0) : Quaternion.Euler(0, -90, 0);
                case BusDirection.Right:    return _isReversed ? Quaternion.Euler(0, -90, 0) : Quaternion.Euler(0, 90, 0);
                default:                    return transform.rotation;
            }
        }
        
        private void UpdateSeats()
        {
            foreach (BusPart part in busParts)
            {
                part.SetPassengerSeats();
            }
            
            PrepareSeats();
        }

        public void StartBoardingProcess(DestinationQueue queue)
        {
            if (isBoarding || queue.IsEmpty() || availableSeats.Count == 0)
            {
                return;
            }
        
            StartCoroutine(BoardingCoroutine(queue));
        }

        private IEnumerator BoardingCoroutine(DestinationQueue queue)
        {
            isBoarding = true;
            
            while (availableSeats.Count > 0 && !queue.IsEmpty())
            {
                PassengerController passenger = queue.GetNextPassenger();
                if (passenger == null) break;

                Transform seat = availableSeats.Dequeue();
                
                passenger.MoveTo(queue.firstQueuePosition.position, () => 
                {
                    passenger.BoardBus(seat);
                });

                _currentPassengerSize++;
                yield return new WaitForSeconds(BOARDING_INTERVAL);
            }
        
            isBoarding = false;

            if (_currentPassengerSize == _passengerSize)
            {
                // TO DO : Use Tween
                EventBroker.OnBusFinish();
                Destroy(this.gameObject,0.50f);
            }
        }

        private void OnDestroy()
        {
            if (occupancyManager != null)
            {
                foreach (BusPart part in busParts)
                {
                    if (part != null)
                    {
                        occupancyManager.UnregisterOccupiedCell(part.gridPosition, part.gameObject);
                    }
                }
            }
        }
    }
}