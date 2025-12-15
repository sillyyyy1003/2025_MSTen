// ===== 絲織教（Silk Religion）専用データ定義 =====
using UnityEngine;

namespace GameData
{
    /// <summary>
    /// 絲織教の特殊建築データ
    /// </summary>
    [CreateAssetMenu(fileName = "SilkReligionBuildingData", menuName = "GameData/Religions/SilkReligion/BuildingData")]
    public class SilkReligionBuildingDataSO : BuildingDataSO
    {
        private void Reset()
        {
            religion = Religion.SilkReligion;
            buildingName = "絲織教_特殊建築";

            // 建築に必要な資源
            buildingResourceCost = 12;

            // HP（初始, 升級1, 升級2）
            maxHpByLevel = new int[3] { 5, 6, 7 };

            // 祭壇格子数（初始, 升級1, 升級2）
            maxSlotsByLevel = new int[3] { 3, 4, 5 };

            // 各項目のアップグレードコスト
            hpUpgradeCost = new int[2] { 6, 7 }; // 血量: 5→6(6資源), 6→7(7資源)
            slotsUpgradeCost = new int[2] { 8, 10 }; // 祭壇格子數: 3→4(8資源), 4→5(10資源)

            // 資源生成設定
            resourceGenInterval = 1; // 毎回合

            // 生産量（一般領地: 2, 金鉱領地: 4）
            generationType = ResourceGenerationType.Gold;
            baseProductionAmount = 2;
            goldenProductionAmount = 4; // Excelでは金鉱領地は4資源と記載

            isSpecialBuilding = true;
        }
    }
}