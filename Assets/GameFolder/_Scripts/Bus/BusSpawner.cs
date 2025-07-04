using UnityEngine;
using System.Collections.Generic;
using SKC.Bus;
using SKC.Grid;

public class BusSpawner : MonoBehaviour
{
    [Header("Data Source")]
    [SerializeField] private TextAsset busLevelDataJson;

    [Header("Prefabs")]
    [SerializeField] private GameObject busControllerPrefab;
    [SerializeField] private GameObject headPartPrefab;
    [SerializeField] private GameObject bodyPartPrefab;
    [SerializeField] private GameObject tailPartPrefab;

    private LevelGridGenerator gridGenerator;

    public void Initalize(TextAsset busLevelDataJson)
    {
        this.busLevelDataJson = busLevelDataJson;
        gridGenerator = LevelGridGenerator.Instance;

        if (ValidateDependencies())
        {
            SpawnBusesFromData();
        }
    }

    private bool ValidateDependencies()
    {
        if (busLevelDataJson == null)
        {
            return false;
        }
        if (busControllerPrefab == null || headPartPrefab == null || bodyPartPrefab == null || tailPartPrefab == null)
        {
            return false;
        }
        if (gridGenerator == null)
        {
            return false;
        }
        return true;
    }

    private void SpawnBusesFromData()
    {
        BusLevelData levelData = JsonUtility.FromJson<BusLevelData>(busLevelDataJson.text);

        foreach (BusInfo busInfo in levelData.buses)
        {
            if (busInfo.parts == null || busInfo.parts.Count == 0) continue;
            SpawnSingleBus(busInfo);
        }
    }

    private void SpawnSingleBus(BusInfo busInfo)
    {
        GameObject busInstance = Instantiate(busControllerPrefab, Vector3.zero, Quaternion.identity, LevelGridGenerator.Instance.transform);
        busInstance.name = $"Bus_{busInfo.busId}_{busInfo.busColor}";
        BusController busController = busInstance.GetComponent<BusController>();
        
        var spawnedPartsData = new Dictionary<BusPart, GridPosition>();

        foreach (BusPartData partData in busInfo.parts)
        {
            GameObject prefabToSpawn = GetPrefabForPartType(partData.partType);
            if (prefabToSpawn == null) continue;

            Vector3 spawnPosition = gridGenerator.GetWorldPosition(partData.position);
            
            GameObject partInstance = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity, busInstance.transform);
            partInstance.name = $"{busInfo.busColor}_{partData.partType}_{partData.position}";

            BusPart busPartComponent = partInstance.GetComponent<BusPart>();
            if (busPartComponent != null)
            {
                spawnedPartsData.Add(busPartComponent, partData.position);
            }
        }
        
        Color realColor = BusColorConverter.ToUnityColor(busInfo.busColor);
        busController.InitializeFromSpawner(spawnedPartsData, realColor, busInfo);
    }

    private GameObject GetPrefabForPartType(BusPartType partType)
    {
        switch (partType)
        {
            case BusPartType.Head: return headPartPrefab;
            case BusPartType.Body: return bodyPartPrefab;
            case BusPartType.Tail: return tailPartPrefab;
            default:
                return null;
        }
    }
}