// ===== 鏡湖教（Mirror Lake Religion）専用データ定義 =====
using UnityEngine;

namespace GameData
{
    /// <summary>
    /// 鏡湖教の信徒データ
    /// </summary>
    [CreateAssetMenu(fileName = "MirrorLakeReligionFarmerData", menuName = "GameData/Religions/MirrorLakeReligion/FarmerData")]
    public class MirrorLakeReligionFarmerDataSO : FarmerDataSO
    {
        // アセット作成時に一度だけ呼ばれる初期化メソッド（Inspectorでリセット時も呼ばれる）
        private void Reset()
        {
            pieceName = "鏡湖教_農民";
            canAttack = false;

            // リソースコストと人口コスト
            resourceCost = 2;
            populationCost = 2;

            // 移動AP
            moveAPCost = 0; // 領地内のみ移動

            // HP・AP（初始, 升級1, 升級2, 升級3）
            maxHPByLevel = new int[4] { 3, 4, 5, 5 };
            maxAPByLevel = new int[4] { 3, 4, 6, 6 };

            // 各項目のアップグレードコスト
            hpUpgradeCost = new int[3] { 3, 4, 0 }; // 血量: 3→4(3資源), 4→5(4資源), 升級3なし
            apUpgradeCost = new int[3] { 5, 4, 0 }; // 行動力: 3→4(5資源), 4→6(4資源), 升級3なし

            // 獻祭設定（鏡湖教はHP回復ではなくAP回復）
            maxSacrificeLevel = new int[3] { 0, 0, 0 }; // 鏡湖教はHP回復なし
            maxAPRecoveryLevel = new int[3] { 1, 2, 3 }; // AP回復量: 1→2→3
            sacrificeUpgradeCost = new int[2] { 0, 0 }; // HP回復アップグレードなし
            apRecoveryUpgradeCost = new int[2] { 5, 6 }; // AP回復: 1→2(5資源), 2→3(6資源)

            devotionAPCost = 2; // AP回復スキル使用に必要なAPコスト
            buildingSpeedModifier = 1.0f;
            productEfficiency = 1.0f;
        }
    }
}
