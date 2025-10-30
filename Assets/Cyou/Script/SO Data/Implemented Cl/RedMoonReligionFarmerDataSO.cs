// ===== 紅月教（Red Moon Religion）専用データ定義 =====
using UnityEngine;

namespace GameData
{
    /// <summary>
    /// 紅月教の信徒データ
    /// </summary>
    [CreateAssetMenu(fileName = "RedMoonReligionFarmerData", menuName = "GameData/Religions/RedMoonReligion/FarmerData")]
    public class RedMoonReligionFarmerDataSO : FarmerDataSO
    {
        private void Reset()
        {
            pieceName = "紅月教_農民";
            canAttack = false;

            // リソースコストと人口コスト
            resourceCost = 1;
            populationCost = 2;

            // 移動AP
            moveAPCost = 0f; // 領地内のみ移動

            // HP・AP（初始, 升級1, 升級2, 升級3）
            maxHPByLevel = new float[4] { 2f, 3f, 4f, 4f };
            maxAPByLevel = new float[4] { 3f, 5f, 5f, 5f };

            // 各項目のアップグレードコスト (Excel行61-63)
            hpUpgradeCost = new int[3] { 3, 4, 0 }; // 血量: 2→3(3資源), 3→4(4資源), 升級3なし
            apUpgradeCost = new int[3] { 4, 0, 0 }; // 行動力: 3→5(4資源), 升級2空白, 升級3空白

            // 獻祭回復量（FarmerDataSO独自の配列）
            maxSacrificeLevel = new int[3] { 1, 2, 2 };
            sacrificeUpgradeCost = new int[2] { 5, 0 }; // 獻祭: 1→2(5資源), 升級2空白

            devotionAPCost = 1;
            buildingSpeedModifier = 1.0f;
            productEfficiency = 1.0f;
        }
    }
}