// ===== 紅月教（Red Moon Religion）専用データ定義 =====
using UnityEngine;

namespace GameData
{
    /// <summary>
    /// 紅月教の特殊建築データ
    /// </summary>
    [CreateAssetMenu(fileName = "RedMoonReligionBuildingData", menuName = "GameData/Religions/RedMoonReligion/BuildingData")]
    public class RedMoonReligionBuildingDataSO : BuildingDataSO
    {
        private void Reset()
        {
            religion = Religion.RedMoonReligion;
            buildingName = "紅月教_特殊建築";

            // 建築に必要な資源
            buildingResourceCost = 12;

            // HP（初始, 升級1, 升級2）
            maxHpByLevel = new int[3] { 4, 5, 6 };

            // 祭壇格子数（初始, 升級1, 升級2）
            maxSlotsByLevel = new int[3] { 4, 5, 6 };

            // 各項目のアップグレードコスト (Excel行67-70)
            hpUpgradeCost = new int[2] { 6, 9 }; // 血量: 4→5(6資源), 5→6(9資源)
            slotsUpgradeCost = new int[2] { 8, 10 }; // 祭壇格子數: 4→5(8資源), 5→6(10資源)

            // 資源生成設定
            resourceGenInterval = 1;

            // 生産量（一般領地: 2, 金鉱領地: 5）
            generationType = ResourceGenerationType.Gold;
            baseProductionAmount = 2;
            goldenProductionAmount = 5;

            isSpecialBuilding = true;
        }
    }
}