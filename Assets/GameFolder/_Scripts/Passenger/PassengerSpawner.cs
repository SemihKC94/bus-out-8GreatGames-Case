using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using SKC.Bus;
using SKC.Grid;

namespace SKC.Passenger
{
    public class PassengerSpawner : MonoBehaviour
{
    [Header("Data & Prefabs")]
    [SerializeField] private TextAsset busDataJson;
    [SerializeField] private GameObject passengerPrefab;

    private List<BusInfo> allBusesData;
    
    public void Initalize(TextAsset busDataJson)
    {
        this.busDataJson = busDataJson;
        
        BusLevelData busLevelData = JsonUtility.FromJson<BusLevelData>(busDataJson.text);
        allBusesData = busLevelData?.buses ?? new List<BusInfo>();
    }

    private void OnEnable()
    {
        LevelGridGenerator.OnDestinationSpawned += HandleDestinationSpawned;
    }

    private void OnDisable()
    {
        LevelGridGenerator.OnDestinationSpawned -= HandleDestinationSpawned;
    }
    
    private void HandleDestinationSpawned(DestinationQueue spawnedQueue)
    {
        if (allBusesData == null || passengerPrefab == null) return;
        
        BusInfo targetBus = allBusesData.FirstOrDefault(bus => bus.busColor == spawnedQueue.queueColor);

        if (targetBus != null)
        {
            int passengersToSpawn = 0;
            foreach (BusPartData part in targetBus.parts)
            {
                switch (part.partType)
                {
                    case BusPartType.Head: passengersToSpawn += 2; break;
                    case BusPartType.Body: passengersToSpawn += 4; break;
                    case BusPartType.Tail: passengersToSpawn += 2; break;
                }
            }
            
            GameObject parent = new GameObject($"Passengers_For_{spawnedQueue.queueColor}");
            parent.transform.SetParent(LevelGridGenerator.Instance.transform);
            for (int i = 0; i < passengersToSpawn; i++)
            {
                GameObject passengerInstance = Instantiate(passengerPrefab, spawnedQueue.transform.position, Quaternion.identity, parent.transform);
                passengerInstance.name = $"Passenger_{targetBus.busColor}_{i + 1}";

                PassengerController passengerController = passengerInstance.GetComponent<PassengerController>();
                if (passengerController != null)
                {
                    passengerController.SetPassengerInfo(targetBus.busColor);
                    spawnedQueue.AddPassengerToQueue(passengerController);
                }
            }
            
        }
        else
        {
            Debug.LogWarning($"No bus found with color {spawnedQueue.queueColor}. No passengers will be spawned for this destination.");
        }
    }
}
}
