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
            buildingResourceCost = 18;

            // HP（初始, 升級1, 升級2）
            maxHpByLevel = new int[3] { 25, 30, 35 };

            // 祭壇格子数（初始, 升級1, 升級2）
            maxSlotsByLevel = new int[3] { 3, 4, 5 };

            // 攻撃範囲（鏡湖教は通常攻撃なし、反撃のみ）
            attackRangeByLevel = new int[3] { 0, 0, 0 };

            // 反撃機能（無→受到攻擊反擊→受到攻擊反擊傷害+1）
            hasCounterAttack = new bool[3] { false, true, true };
            counterAttackDamage = new int[3] { 0, 1, 2 }; // 反撃ダメージ（0、基礎1、基礎+1で2）

            // 各項目のアップグレードコスト
            hpUpgradeCost = new int[2] { 6, 8 }; // 血量: 25→30(6資源), 30→35(8資源)
            attackRangeUpgradeCost = new int[2] { 8, 10 }; // 反撃機能: 無→反撃(8資源), 反撃→反撃+1(10資源)
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
