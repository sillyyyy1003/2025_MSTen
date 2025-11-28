// ===== 鏡湖教（Mirror Lake Religion）専用データ定義 =====
using UnityEngine;

namespace GameData
{
    /// <summary>
    /// 鏡湖教の宣教師データ
    /// </summary>
    [CreateAssetMenu(fileName = "MirrorLakeReligionMissionaryData", menuName = "GameData/Religions/MirrorLakeReligion/MissionaryData")]
    public class MirrorLakeReligionMissionaryDataSO : MissionaryDataSO
    {
        private void Reset()
        {
            pieceName = "鏡湖教_宣教師";
            canAttack = true;

            // リソースコストと人口コスト
            resourceCost = 3;
            populationCost = 3;

            // 移動AP
            moveAPCost = 1;

            // HP・AP（初始, 升級1, 升級2, 升級3）
            maxHPByLevel = new int[4] { 7, 8, 9, 9 };
            maxAPByLevel = new int[4] { 5, 7, 8, 8 };

            // 各項目のアップグレードコスト
            hpUpgradeCost = new int[3] { 4, 5, 0 }; // 血量: 7→8(4資源), 8→9(5資源), 升級3なし
            apUpgradeCost = new int[3] { 6, 8, 0 }; // 行動力: 5→7(6資源), 7→8(8資源), 升級3なし

            // 占領設定
            occupyAPCost = 2;
            occupyEmptySuccessRateByLevel = new float[4] { 0.6f, 0.7f, 0.8f, 0.8f };
            occupyEnemySuccessRateByLevel = new float[4] { 0.4f, 0.5f, 0.6f, 0.6f };
            occupyUpgradeCost = new int[3] { 5, 6, 0 }; // 佔領空白: 60%→70%(5資源), 70%→80%(6資源), 升級3なし

            // 魅惑設定
            convertAPCost = 3;
            conversionTurnDuration = new int[4] { 2, 3, 4, 5 };

            // 魅惑成功率（Excel: 30%, 40%, 50%, 70% / 40%, 50%, 60%, - / 20%, 25%, 30%, -）
            convertMissionaryChanceByLevel = new float[4] { 0.30f, 0.40f, 0.50f, 0.70f };
            convertFarmerChanceByLevel = new float[4] { 0.40f, 0.50f, 0.60f, 0.60f };
            convertMilitaryChanceByLevel = new float[4] { 0.20f, 0.25f, 0.30f, 0.30f };
            convertEnemyUpgradeCost = new int[3] { 6, 7, 8 }; // 魅惑傳教士: 30%→40%(6資源), 40%→50%(7資源), 50%→70%(8資源)

            // 魅惑耐性（升級1以降）
            hasAntiConversionSkill = new bool[4] { false, true, true, true };

            baseConversionDefenseChance = 0.2f;
        }
    }
}
