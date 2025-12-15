// ===== 鏡湖教（Mirror Lake Religion）専用データ定義 =====
using UnityEngine;

namespace GameData
{
    /// <summary>
    /// 鏡湖教の特殊建築データ
    /// 特徴：通常攻撃ではなく反撃機能を持つ
    /// </summary>
    [CreateAssetMenu(fileName = "MirrorLakeReligionBuildingData", menuName = "GameData/Religions/MirrorLakeReligion/BuildingData")]
    public class MirrorLakeReligionBuildingDataSO : BuildingDataSO
    {
        private void Reset()
        {
            religion = Religion.MirrorLakeReligion;
            buildingName = "鏡湖教_特殊建築";

            // 建築に必要な資源
            buildingResourceCost = 12;

            // HP（初始, 升級1, 升級2, 升級3）
            maxHpByLevel = new int[4] { 6, 7, 8, 10 };

            // 祭壇格子数（初始, 升級1, 升級2, 升級3）
            maxSlotsByLevel = new int[4] { 3, 4, 5, 6 };

            // 反撃機能（無→受到攻擊反擊→受到攻擊反擊傷害+1→受到攻擊反擊傷害+2）
            hasCounterAttack = new bool[4] { false, true, true, true };
            counterAttackDamage = new int[4] { 0, 1, 2, 3 }; // 反撃ダメージ（0、基礎1、基礎+1で2、基礎+2で3）

            // 各項目のアップグレードコスト
            hpUpgradeCost = new int[3] { 6, 8, 10 }; // 血量: 6→7(6資源), 7→8(8資源), 8→10(10資源)
            slotsUpgradeCost = new int[2] { 8, 10 }; // 祭壇格子數: 3→4(8資源), 4→5(10資源)

            // 資源生成設定
            resourceGenInterval = 1; // 毎回合

            // 生産量（一般領地: 2, 金鉱領地: 4）
            generationType = ResourceGenerationType.Gold;
            baseProductionAmount = 2;
            goldenProductionAmount = 4;

            isSpecialBuilding = true;
        }
    }
}
