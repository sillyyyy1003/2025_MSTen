using Buildings;
using GameData;

using UnityEngine;

/// <summary>
/// 建物ファクトリー
/// </summary>
public static class BuildingFactory
{
    public static Building CreateBuilding(BuildingDataSO data, int playerID, Vector3 position)
    {
        if (data == null || data.buildingPrefab == null)
        {
            Debug.LogError("建物データまたはPrefabが設定されていません");
            return null;
        }

        GameObject buildingObj = GameObject.Instantiate(data.buildingPrefab, position, Quaternion.identity);
        Building building = buildingObj.GetComponent<Building>();

        if (building == null)
        {
            building = buildingObj.AddComponent<Building>();
        }

        building.Initialize(data, playerID);
        return building;
    }
}