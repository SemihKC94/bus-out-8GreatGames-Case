using UnityEngine;
using SKC.Bus;

namespace SKC.Passenger
{
    public class PassengerController : MonoBehaviour
    {
        public Animator animator;
        public Renderer passengerRenderer;
        public Material[]  passengerMaterials;
        public float moveSpeed = 5f;
        private Vector3 targetPosition;
        private Transform targetSeat;
        private System.Action onArrival; // Hedefe ulaştığında çağrılacak fonksiyon

        private enum State { Idle, MovingToPosition, MovingToSeat }
        private State currentState = State.Idle;
        private int WalkHash = Animator.StringToHash("Running");
        private int IdleHash = Animator.StringToHash("Idle");
        private int SitHash = Animator.StringToHash("Sit");

        private void Update()
        {
            if (currentState == State.MovingToPosition)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
                if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
                {
                    currentState = State.Idle;
                    onArrival?.Invoke();
                    animator.Play(IdleHash);
                }
            }
            else if (currentState == State.MovingToSeat)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetSeat.position, moveSpeed * Time.deltaTime);
                if (Vector3.Distance(transform.position, targetSeat.position) < 0.01f)
                {
                    currentState = State.Idle;
                    transform.SetParent(targetSeat); // Koltuğun child'ı ol
                    transform.localPosition = Vector3.zero;
                    onArrival?.Invoke();
                    animator.Play(SitHash);
                }
            }
            
        }

        public void MoveTo(Vector3 position, System.Action onArrivalCallback = null)
        {
            targetPosition = position;
            currentState = State.MovingToPosition;
            onArrival = onArrivalCallback;
            animator.Play(WalkHash);
        }

        public void BoardBus(Transform seat, System.Action onBoardedCallback = null)
        {
            targetSeat = seat;
            currentState = State.MovingToSeat;
            onArrival = onBoardedCallback;
            animator.Play(WalkHash);
        }

        public void SetPassengerInfo(BusColor busColor)
        {
            passengerRenderer.sharedMaterial  = passengerMaterials[(int)busColor];
        }
    }
}
