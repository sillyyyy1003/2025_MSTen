// ===== 紅月教（Red Moon Religion）専用データ定義 =====
using UnityEngine;

namespace GameData
{
    /// <summary>
    /// 紅月教の宣教師データ
    /// </summary>
    [CreateAssetMenu(fileName = "RedMoonReligionMissionaryData", menuName = "GameData/Religions/RedMoonReligion/MissionaryData")]
    public class RedMoonReligionMissionaryDataSO : MissionaryDataSO
    {
        private void Reset()
        {
            pieceName = "紅月教_宣教師";
            canAttack = true;

            // リソースコストと人口コスト
            resourceCost = 2;
            populationCost = 3;

            // 移動AP
            moveAPCost = 1f;

            // HP・AP（初始, 升級1, 升級2, 升級3）
            maxHPByLevel = new float[4] { 5f, 7f, 10f, 10f };
            maxAPByLevel = new float[4] { 5f, 7f, 8f, 8f };

            // 各項目のアップグレードコスト (Excel行54-60)
            hpUpgradeCost = new int[3] { 5, 6, 0 }; // 血量: 5→7(5資源), 7→10(6資源), 升級3なし
            apUpgradeCost = new int[3] { 5, 6, 0 }; // 行動力: 5→7(5資源), 7→8(6資源), 升級3なし

            // 占領設定
            occupyAPCost = 2f;
            occupyEmptySuccessRateByLevel = new float[4] { 0.8f, 0.9f, 1.0f, 1.0f };
            occupyEnemySuccessRateByLevel = new float[4] { 0.5f, 0.6f, 0.7f, 0.7f };
            occupyUpgradeCost = new int[3] { 4, 5, 0 }; // 佔領空白: 0.8→0.9(4資源), 0.9→1.0(5資源), 升級3なし


            // 魅惑設定
            convertAPCost = 3;
            conversionTurnDuration = new int[4] { 2, 3, 4, 5 };

            // 魅惑成功率（Excel: 無, 0.55, 0.6, 0.7 / 無, 0.15, 0.25, 0.3 / 無, 0.5, 0.55, 0.6）
            convertMissionaryChanceByLevel = new float[4] { 0.0f, 0.55f, 0.6f, 0.7f };
            convertFarmerChanceByLevel = new float[4] { 0.0f, 0.15f, 0.25f, 0.3f };
            convertMilitaryChanceByLevel = new float[4] { 0.0f, 0.5f, 0.55f, 0.6f };
            convertEnemyUpgradeCost = new int[3] { 5, 6, 7 }; // 魅惑傳教士: 無→0.55(5資源), 0.55→0.6(6資源), 0.6→0.7(7資源)


            // 魅惑耐性（升級1以降）
            hasAntiConversionSkill = new bool[4] { false, true, true, true };

            baseConversionDefenseChance = 0.2f;
        }
    }
}