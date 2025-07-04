using UnityEngine;
using SKC.Grid;
using SKC.Inputs;
using SKC.Passenger;

namespace SKC.Bus
{
    public enum BusPartType
    {
        Head,
        Body,
        Tail
    }

    public class BusPart : MonoBehaviour
    {
        public BusPartType partType;
        [HideInInspector] public BusController busController;
        
        [HideInInspector] public GridPosition gridPosition;
        
        public int passengerCapacity = 2;
        public Transform[] passengerSeats;

        private Vector3 _startWorldPosition;
        private Vector3 _targetWorldPosition;
        private Quaternion _startRotation;
        private Quaternion _targetRotation;
        private float _animationProgress; 

        private BusPartVisual busPartVisual;
        private PlayerInputController controller = null;
        private string _interactTag = "Finish";
        public string InteractTag {get {return _interactTag;} set {_interactTag = value;}}
        
        
        public void SetPassengerSeats()
        {
            //if(busController != null) busController.SetPassengerSizeAndSeats(passengerCapacity, passengerSeats);
            controller = PlayerInputController.Instance;
        }
        
        public void StartAnimation(Vector3 startPos, Vector3 targetPos, Quaternion startRot, Quaternion targetRot)
        {
            _startWorldPosition = startPos;
            _targetWorldPosition = targetPos;
            _startRotation = startRot;
            _targetRotation = targetRot;
            _animationProgress = 0f;
            
            if (busPartVisual != null)
            {
                busPartVisual.SetMoving(true);
            }
        }
        
        public void Animate(float deltaTime, float duration)
        {
            _animationProgress += deltaTime / duration;
            float t = Mathf.Clamp01(_animationProgress);

            transform.position = Vector3.Lerp(_startWorldPosition, _targetWorldPosition, t);
            //transform.rotation = Quaternion.Slerp(_startRotation, _targetRotation, t);

            if (_animationProgress >= 1f)
            {
                transform.position = _targetWorldPosition;
                transform.rotation = _targetRotation;
                if (busPartVisual != null)
                {
                    busPartVisual.SetMoving(false);
                }
            }
        }
        public bool IsAnimationFinished()
        {
            return _animationProgress >= 1f;
        }
        
        public void SetWorldPositionImmediately(Vector3 worldPos)
        {
            transform.position = worldPos;
        }

        private void OnTriggerStay(Collider other)
        {
            if (other.gameObject.CompareTag(_interactTag) && !controller.IsDragging)
            {
                DestinationQueue queue = other.GetComponent<DestinationQueue>();
                busController.StartBoardingProcess(queue);
            }
        }
    }
}