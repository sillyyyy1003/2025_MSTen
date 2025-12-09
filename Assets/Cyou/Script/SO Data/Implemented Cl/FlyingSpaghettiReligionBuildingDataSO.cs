// ===== 飛天擔擔麵教（Flying Spaghetti Religion）専用データ定義 =====
using UnityEngine;

namespace GameData
{
    /// <summary>
    /// 飛天擔擔麵教の特殊建築データ
    /// </summary>
    [CreateAssetMenu(fileName = "FlyingSpaghettiReligionBuildingData", menuName = "GameData/Religions/FlyingSpaghettiReligion/BuildingData")]
    public class FlyingSpaghettiReligionBuildingDataSO : BuildingDataSO
    {
        private void Reset()
        {
            religion = Religion.FlyingSpaghettiReligion;
            buildingName = "飛天擔擔麵教_特殊建築";

            // 建築に必要な資源
            buildingResourceCost = 12;

            // HP（初始, 升級1, 升級2, 升級3）
            maxHpByLevel = new int[4] { 5, 6, 7, 9 };

            // 祭壇格子数（初始, 升級1, 升級2, 升級3）
            maxSlotsByLevel = new int[4] { 3, 4, 5, 6 };

            // 攻撃範囲（注：飛天擔擔麵教は初期状態から攻撃範囲1を持つ）
            attackRangeByLevel = new int[4] { 1, 2, 3, 4 }; // 範囲1→範囲2→範囲3→範囲4

            // 各項目のアップグレードコスト (Excel行125-127)
            hpUpgradeCost = new int[3] { 6, 8, 8 }; // 血量: 5→6(6資源), 6→7(8資源), 7→9(8資源)
            attackRangeUpgradeCost = new int[2] { 10, 14 }; // 攻撃範囲: 範囲1→範囲2(10資源), 範囲2→範囲3(14資源)
            slotsUpgradeCost = new int[2] { 10, 12 }; // 祭壇格子數: 3→4(10資源), 4→5(12資源)

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
