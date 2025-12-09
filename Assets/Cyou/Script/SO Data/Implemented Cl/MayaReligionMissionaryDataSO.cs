// ===== 瑪雅外星人文明教（Maya Alien Civilization Religion）専用データ定義 =====
using UnityEngine;

namespace GameData
{
    /// <summary>
    /// 瑪雅外星人文明教の宣教師データ
    /// </summary>
    [CreateAssetMenu(fileName = "MayaReligionMissionaryData", menuName = "GameData/Religions/MayaReligion/MissionaryData")]
    public class MayaReligionMissionaryDataSO : MissionaryDataSO
    {
        private void Reset()
        {
            pieceName = "瑪雅外星人文明教_宣教師";
            canAttack = true;

            // リソースコストと人口コスト
            resourceCost = 3;
            populationCost = 3;

            // 移動AP
            moveAPCost = 1;

            // HP・AP（初始, 升級1, 升級2, 升級3）
            maxHPByLevel = new int[4] { 7, 9, 12, 12 };
            maxAPByLevel = new int[4] { 5, 7, 8, 8 };

            // 各項目のアップグレードコスト (Excel行75-81)
            hpUpgradeCost = new int[3] { 5, 6, 0 }; // 血量: 7→9(5資源), 9→12(6資源), 升級3なし
            apUpgradeCost = new int[3] { 7, 0, 0 }; // 行動力: 5→7(7資源), 7→8(空白), 升級3なし

            // 占領設定
            occupyAPCost = 2;
            occupyEmptySuccessRateByLevel = new float[4] { 0.8f, 0.9f, 1.0f, 1.0f };
            occupyEnemySuccessRateByLevel = new float[4] { 0.5f, 0.6f, 0.7f, 0.7f };
            occupyUpgradeCost = new int[3] { 5, 6, 0 }; // 佔領空白: 0.8→0.9(5資源), 0.9→1.0(6資源), 升級3なし


            // 魅惑設定
            convertAPCost = 3;
            conversionTurnDuration = new int[4] { 2, 3, 4, 5 };

            // 魅惑成功率（Excel: 0.5, 0.55, 0.65, 0.7 / 0.1, 0.2, 0.3, 0.35 / 0.45, 0.5, 0.6, 0.65）
            convertMissionaryChanceByLevel = new float[4] { 0.5f, 0.55f, 0.65f, 0.7f };
            convertFarmerChanceByLevel = new float[4] { 0.1f, 0.2f, 0.3f, 0.35f };
            convertMilitaryChanceByLevel = new float[4] { 0.45f, 0.5f, 0.6f, 0.65f };
            convertEnemyUpgradeCost = new int[3] { 5, 6, 7 }; // 魅惑傳教士: 無→0.5(5資源), 

            // 魅惑耐性（升級1以降）
            hasAntiConversionSkill = new bool[4] { false, true, true, true };

            baseConversionDefenseChance = 0.2f;
        }
    }
}