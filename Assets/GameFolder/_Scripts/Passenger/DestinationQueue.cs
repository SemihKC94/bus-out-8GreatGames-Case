using UnityEngine;
using System.Collections.Generic;
using SKC.Bus;

namespace SKC.Passenger
{
    public class DestinationQueue : MonoBehaviour
    {
        [Header("Queue Configuration")]
        public BusColor queueColor;
        public Transform firstQueuePosition;
        [SerializeField] private float spacing = 1.0f;
        
        private List<Vector3> queueSlots = new List<Vector3>();
        private Queue<PassengerController> passengerQueue = new Queue<PassengerController>();

        private void Awake()
        {
            if (firstQueuePosition == null)
            {
                enabled = false;
            }
        }
        
        public void Initialize(BusColor color)
        {
            queueColor = color;
        }
        
        public void AddPassengerToQueue(PassengerController passenger)
        {
            Vector3 newSlotPosition = firstQueuePosition.position - (firstQueuePosition.forward * passengerQueue.Count * spacing);
            
            passengerQueue.Enqueue(passenger);
            queueSlots.Add(newSlotPosition);

            passenger.transform.SetParent(this.transform);
            passenger.MoveTo(newSlotPosition);
            
            passenger.transform.localEulerAngles = new Vector3(0.0f, 180.0f, 0.0f);
        }
        
        public PassengerController GetNextPassenger()
        {
            if (passengerQueue.Count > 0)
            {
                queueSlots.RemoveAt(0);
                PassengerController passenger = passengerQueue.Dequeue();
                UpdateQueuePositions();
                return passenger;
            }
            return null;
        }
        
        private void UpdateQueuePositions()
        {
            int i = 0;
            foreach (var passenger in passengerQueue)
            {
                Vector3 targetSlotPosition = firstQueuePosition.position - (firstQueuePosition.forward * i * spacing);
                passenger.MoveTo(targetSlotPosition);
                i++;
            }
        }

        public bool IsEmpty()
        {
            return passengerQueue.Count == 0;
        }
        
        #if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (firstQueuePosition == null) return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(firstQueuePosition.position, 0.2f);
            Gizmos.DrawLine(firstQueuePosition.position, firstQueuePosition.position + firstQueuePosition.forward * 1.0f);
            
            for (int i = 1; i < 5; i++) 
            {
                Vector3 slotPosition = firstQueuePosition.position - (firstQueuePosition.forward * i * spacing);
                Gizmos.color = new Color(1, 1, 0, 0.5f);
                Gizmos.DrawSphere(slotPosition, 0.15f);
            }
        }
        #endif
    }
}
