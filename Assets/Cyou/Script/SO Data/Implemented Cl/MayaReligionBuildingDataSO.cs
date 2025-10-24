// ===== 瑪雅外星人文明教（Maya Alien Civilization Religion）専用データ定義 =====
using UnityEngine;

namespace GameData
{
    /// <summary>
    /// 瑪雅外星人文明教の特殊建築データ
    /// </summary>
    [CreateAssetMenu(fileName = "MayaReligionBuildingData", menuName = "GameData/Religions/MayaReligion/BuildingData")]
    public class MayaReligionBuildingDataSO : BuildingDataSO
    {
        private void Reset()
        {
            buildingName = "瑪雅外星人文明教_特殊建築";

            // HP（初始, 升級1, 升級2）
            maxHpByLevel = new int[3] { 25, 30, 45 };

            // 祭壇格子数（初始, 升級1, 升級2）
            maxSlotsByLevel = new int[3] { 2, 3, 4 };

            // 攻撃範囲
            attackRangeByLevel = new int[3] { 0, 1, 2 };

            // 建造所需花費
            buildingAPCostByLevel = new int[3] { 9, 6, 3 };

            // 各項目のアップグレードコスト (Excel行88-91)
            hpUpgradeCost = new int[2] { 7, 9 }; // 血量: 25→30(7資源), 30→45(9資源)
            attackRangeUpgradeCost = new int[2] { 8, 10 }; // 攻撃範囲: 無→範囲1(8資源), 範囲1→範囲2(10資源)
            slotsUpgradeCost = new int[2] { 8, 10 }; // 祭壇格子數: 2→3(8資源), 3→4(10資源)
            buildCostUpgradeCost = new int[2] { 6, 8 }; // 建造花費: 9→6(6資源), 6→3(8資源)

            // 資源生成設定
            buildStartAPCost = 1;
            resourceGenInterval = 1;
            apCostperTurn = 1;

            // 生産量（一般領地: 2, 金鉱領地: 5）
            generationType = ResourceGenerationType.Gold;
            baseProductionAmount = 2;
            goldenProductionAmount = 5;

            isSpecialBuilding = true;
        }
    }
}