using GameData;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

///<summary>
///全ての建築可能建物を持たせるレジストリ
///<summary>
[CreateAssetMenu(fileName = "BuildingRegistry", menuName = "GameData/BuildingRegistry")]
public class BuildingRegistry : ScriptableObject
{
    [Header("全ての建築可能な建物リスト")]
    public BuildingDataSO[] buildingDataSOList;

    /// <summary>
    /// 指定された宗教の建物のみを取得
    /// </summary>
    /// <param name="religion">宗教タイプ</param>
    /// <returns>その宗教の建物リスト</returns>
    public List<BuildingDataSO> GetBuildingsByReligion(Religion religion)
    {
        if (buildingDataSOList == null)
        {
            Debug.LogWarning("BuildingRegistry: buildingDataSOListがnullです");
            return new List<BuildingDataSO>();
        }

        return buildingDataSOList
            .Where(building => building != null && building.religion == religion)
            .ToList();
    }

    /// <summary>
    /// 複数の宗教の建物を取得
    /// </summary>
    /// <param name="religions">宗教タイプの配列</param>
    /// <returns>指定された宗教の建物リスト</returns>
    public List<BuildingDataSO> GetBuildingsByReligions(params Religion[] religions)
    {
        if (buildingDataSOList == null)
        {
            Debug.LogWarning("BuildingRegistry: buildingDataSOListがnullです");
            return new List<BuildingDataSO>();
        }

        return buildingDataSOList
            .Where(building => building != null && religions.Contains(building.religion))
            .ToList();
    }
}