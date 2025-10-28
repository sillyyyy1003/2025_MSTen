using GameData;
using UnityEngine;

///<summary>
///全ての建築可能建物を持たせるレジストリ
///<summary>
[CreateAssetMenu(fileName = "BuildingRegistry", menuName = "GameData/BuildingRegistry")]
public class BuildingRegistry : ScriptableObject
{
    [Header("全ての建築可能な建物リスト")]
    public BuildingDataSO[] buildingDataSOList;
}