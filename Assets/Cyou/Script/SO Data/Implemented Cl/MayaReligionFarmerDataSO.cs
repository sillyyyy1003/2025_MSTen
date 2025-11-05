// ===== 瑪雅外星人文明教（Maya Alien Civilization Religion）専用データ定義 =====
using UnityEngine;

namespace GameData
{
    /// <summary>
    /// 瑪雅外星人文明教の信徒データ
    /// </summary>
    [CreateAssetMenu(fileName = "MayaReligionFarmerData", menuName = "GameData/Religions/MayaReligion/FarmerData")]
    public class MayaReligionFarmerDataSO : FarmerDataSO
    {
        private void Reset()
        {
            pieceName = "瑪雅外星人文明教_農民";
            canAttack = false;

            // リソースコストと人口コスト
            resourceCost = 2;
            populationCost = 2;

            // 移動AP
            moveAPCost = 0; // 領地内のみ移動

            // HP・AP（初始, 升級1, 升級2, 升級3）
            maxHPByLevel = new int[4] { 3, 4, 5, 5 };
            maxAPByLevel = new int[4] { 3, 5, 5, 5 };

            // 各項目のアップグレードコスト (Excel行82-84)
            hpUpgradeCost = new int[3] { 4, 5, 0 }; // 血量: 3→4(4資源), 4→5(5資源), 升級3なし
            apUpgradeCost = new int[3] { 5, 6, 0 }; // 行動力: 3→5(5資源), 5→空白(6資源), 升級3空白

            // 獻祭回復量（FarmerDataSO独自の配列）（注：瑪雅は初始が2で高い）
            maxSacrificeLevel = new int[3] { 2, 3, 3 };
            sacrificeUpgradeCost = new int[2] { 5, 0 }; // 獻祭: 2→3(5資源), 升級2空白

            devotionAPCost = 1;
            buildingSpeedModifier = 1.0f;
            productEfficiency = 1.0f;
        }
    }
}