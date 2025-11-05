// ===== 絲織教（Silk Religion）専用データ定義 =====
using UnityEngine;

namespace GameData
{
    /// <summary>
    /// 絲織教の信徒データ
    /// </summary>
    [CreateAssetMenu(fileName = "SilkReligionFarmerData", menuName = "GameData/Religions/SilkReligion/FarmerData")]
    public class SilkReligionFarmerDataSO : FarmerDataSO
    {
        // アセット作成時に一度だけ呼ばれる初期化メソッド（Inspectorでリセット時も呼ばれる）
        private void Reset()
        {
            pieceName = "絲織教_農民";
            canAttack = false;

            // リソースコストと人口コスト
            resourceCost = 2;
            populationCost = 2;

            // 移動AP
            moveAPCost = 0; // 領地内のみ移動

            // HP・AP（初始, 升級1, 升級2, 升級3）
            maxHPByLevel = new int[4] { 3, 4, 5, 5 };
            maxAPByLevel = new int[4] { 3, 5, 5, 5 };

            // 各項目のアップグレードコスト
            hpUpgradeCost = new int[3] { 3, 4, 0 }; // 血量: 3→4(3資源), 4→5(4資源), 升級3なし
            apUpgradeCost = new int[3] { 5, 0, 0 }; // 行動力: 3→5(5資源), 升級2空白, 升級3空白

            // 獻祭回復量（FarmerDataSO独自の配列）
            maxSacrificeLevel = new int[3] { 1, 2, 2 };
            sacrificeUpgradeCost = new int[2] { 6, 0 }; // 獻祭: 1→2(6資源), 升級2空白

            devotionAPCost = 1;
            buildingSpeedModifier = 1.0f;
            productEfficiency = 1.0f;
        }
    }
}