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
            buildingName = "絲織教_特殊建築";

            // HP（初始, 升級1, 升級2）
            maxHpByLevel = new int[3] { 25, 30, 45 };

            // 祭壇格子数（初始, 升級1, 升級2）
            maxSlotsByLevel = new int[3] { 3, 4, 5 };

            // 攻撃範囲
            attackRangeByLevel = new int[3] { 0, 1, 2 };

            // 建造所需花費（村民の建築AP消費量）
            buildingAPCostByLevel = new int[3] { 9, 6, 3 };

            // 各項目のアップグレードコスト
            hpUpgradeCost = new int[2] { 6, 8 }; // 血量: 25→30(6資源), 30→45(8資源)
            attackRangeUpgradeCost = new int[2] { 8, 10 }; // 攻撃範囲: 無→範囲1(8資源), 範囲1→範囲2(10資源)
            slotsUpgradeCost = new int[2] { 8, 10 }; // 祭壇格子數: 3→4(8資源), 4→5(10資源)
            buildCostUpgradeCost = new int[2] { 6, 8 }; // 建造花費: 9→6(6資源), 6→3(8資源)

            // 資源生成設定
            buildStartAPCost = 1;
            resourceGenInterval = 1; // 毎回合
            apCostperTurn = 1; // 1信徒につき1APコスト

            // 生産量（一般領地: 2, 金鉱領地: 4）
            generationType = ResourceGenerationType.Gold;
            baseProductionAmount = 2;
            goldenProductionAmount = 4; // Excelでは金鉱領地は4資源と記載

            isSpecialBuilding = true;
        }
    }
}