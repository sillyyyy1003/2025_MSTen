// ===== 瘋狂科學家教（Mad Scientist Religion）専用データ定義 =====
using UnityEngine;

namespace GameData
{
    /// <summary>
    /// 瘋狂科學家教の特殊建築データ
    /// </summary>
    [CreateAssetMenu(fileName = "MadScientistReligionBuildingData", menuName = "GameData/Religions/MadScientistReligion/BuildingData")]
    public class MadScientistReligionBuildingDataSO : BuildingDataSO
    {
        private void Reset()
        {
            buildingName = "瘋狂科學家教_特殊建築";

            // HP（初始, 升級1, 升級2）
            maxHpByLevel = new int[3] { 25, 30, 45 };

            // 祭壇格子数（初始, 升級1, 升級2）
            maxSlotsByLevel = new int[3] { 3, 4, 5 };

            // 攻撃範囲
            attackRangeByLevel = new int[3] { 0, 1, 2 };

            // 各項目のアップグレードコスト (基礎数値と同じ推測)
            hpUpgradeCost = new int[2] { 6, 8 }; // 血量: 25→30, 30→45
            attackRangeUpgradeCost = new int[2] { 8, 10 }; // 攻撃範囲: 無→範囲1, 範囲1→範囲2
            slotsUpgradeCost = new int[2] { 8, 10 }; // 祭壇格子數: 3→4, 4→5

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