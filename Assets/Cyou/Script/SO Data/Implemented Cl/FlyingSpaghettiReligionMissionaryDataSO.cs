// ===== 飛天擔擔麵教（Flying Spaghetti Religion）専用データ定義 =====
using UnityEngine;

namespace GameData
{
    /// <summary>
    /// 飛天擔擔麵教の宣教師データ
    /// </summary>
    [CreateAssetMenu(fileName = "FlyingSpaghettiReligionMissionaryData", menuName = "GameData/Religions/FlyingSpaghettiReligion/MissionaryData")]
    public class FlyingSpaghettiReligionMissionaryDataSO : MissionaryDataSO
    {
        private void Reset()
        {
            pieceName = "飛天擔擔麵教_宣教師";
            canAttack = true;

            // リソースコストと人口コスト
            resourceCost = 3;
            populationCost = 3;

            // 移動AP
            moveAPCost = 1;

            // HP・AP（初始, 升級1, 升級2, 升級3）
            maxHPByLevel = new int[4] { 7, 8, 10, 10 };
            maxAPByLevel = new int[4] { 5, 6, 7, 7 };

            // 各項目のアップグレードコスト (Excel行112-118)
            hpUpgradeCost = new int[3] { 4, 6, 0 }; // 血量: 7→8(4資源), 8→10(6資源), 升級3なし
            apUpgradeCost = new int[3] { 6, 8, 0 }; // 行動力: 5→6(6資源), 6→7(8資源), 升級3なし

            // 占領設定
            occupyAPCost = 2;
            occupyEmptySuccessRateByLevel = new float[4] { 0.7f, 0.75f, 0.85f, 0.85f }; // 70%→75%→85%
            occupyEnemySuccessRateByLevel = new float[4] { 0.5f, 0.55f, 0.65f, 0.65f }; // 50%→55%→65%
            occupyUpgradeCost = new int[3] { 5, 7, 0 }; // 佔領空白: 70%→75%(5資源), 75%→85%(7資源), 升級3なし

            // 魅惑設定
            convertAPCost = 3;
            conversionTurnDuration = new int[4] { 2, 3, 4, 5 };

            // 魅惑成功率（Excel: 3%, 6%, 11% / 6%, 12%, 20% / 1%, 2%, 5%）
            convertMissionaryChanceByLevel = new float[4] { 0.03f, 0.06f, 0.11f, 0.11f }; // 傳教士: 3%→6%→11%
            convertFarmerChanceByLevel = new float[4] { 0.06f, 0.12f, 0.2f, 0.2f }; // 信徒: 6%→12%→20%
            convertMilitaryChanceByLevel = new float[4] { 0.01f, 0.02f, 0.05f, 0.05f }; // 十字軍: 1%→2%→5%
            convertEnemyUpgradeCost = new int[3] { 6, 8, 0 }; // 魅惑傳教士: 3%→6%(6資源), 6%→11%(8資源), 升級3なし

            // 魅惑耐性（升級1以降）
            hasAntiConversionSkill = new bool[4] { false, true, true, true };

            baseConversionDefenseChance = 0.2f;
        }
    }
}
