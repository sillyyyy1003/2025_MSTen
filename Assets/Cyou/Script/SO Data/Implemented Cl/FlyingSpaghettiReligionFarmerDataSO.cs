// ===== 飛天擔擔麵教（Flying Spaghetti Religion）専用データ定義 =====
using UnityEngine;

namespace GameData
{
    /// <summary>
    /// 飛天擔擔麵教の信徒データ
    /// </summary>
    [CreateAssetMenu(fileName = "FlyingSpaghettiReligionFarmerData", menuName = "GameData/Religions/FlyingSpaghettiReligion/FarmerData")]
    public class FlyingSpaghettiReligionFarmerDataSO : FarmerDataSO
    {
        private void Reset()
        {
            pieceName = "飛天擔擔麵教_農民";
            canAttack = false;

            // リソースコストと人口コスト
            resourceCost = 1;
            populationCost = 2;

            // 移動AP
            moveAPCost = 0; // 領地内のみ移動

            // HP・AP（初始, 升級1, 升級2, 升級3）
            maxHPByLevel = new int[4] { 3, 4, 5, 5 };
            maxAPByLevel = new int[4] { 3, 4, 5, 5 };

            // 各項目のアップグレードコスト (Excel行119-121)
            hpUpgradeCost = new int[3] { 3, 4, 0 }; // 血量: 3→4(3資源), 4→5(4資源), 升級3なし
            apUpgradeCost = new int[3] { 4, 6, 0 }; // 行動力: 3→4(4資源), 4→5(6資源), 升級3なし

            // 獻祭（注：飛天擔擔麵教は資源+1→資源+2の効果）
            // maxSacrificeLevel値をゲームロジック側で資源生成量として使用
            maxSacrificeLevel = new int[3] { 1, 2, 2 }; // 資源+1, 資源+2, 資源+2
            sacrificeUpgradeCost = new int[2] { 6, 0 }; // 獻祭: 資源+1→資源+2(6資源), 升級2なし

            devotionAPCost = 2; // 消耗2點行動力
            buildingSpeedModifier = 1.0f;
            productEfficiency = 1.0f;
        }
    }
}
