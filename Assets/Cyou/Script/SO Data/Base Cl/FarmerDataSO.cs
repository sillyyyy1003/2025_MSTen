// ===== ScriptableObject データ定義 =====
using UnityEngine;

namespace GameData
{
    /// <summary>
    /// 農民特有のデータ定義
    /// </summary>
    [CreateAssetMenu(fileName = "FarmerData", menuName = "GameData/BasePieces/FarmerData")]
    public class FarmerDataSO : PieceDataSO
    {
        [Header("建築能力")]
        public float buildingSpeedModifier = 1.0f; // 建築速度修正値

        [Header("資源生産効率")]
        public float productEfficiency = 1.0f; // 作業効率

        [Header("献身の治癒")]
        public int devotionAPCost = 1;//行動力を消費して他駒を回復するスキル

        [Header("アップグレードレベルごとのデータ（升級1,2）")]
        public int[] maxSacrificeLevel = new int[3] { 1, 2, 2 }; // 自分を生贄にして他駒のHPを回復するスキル
        public int[] maxAPRecoveryLevel = new int[3] { 0, 0, 0 }; // 他駒のAPを回復するスキル（鏡湖教用: 1→2→3）

        [Header("農民専用アップグレードコスト")]
        public int[] sacrificeUpgradeCost = new int[2]; // 獻祭アップグレード資源コスト（0→1, 1→2）
        public int[] apRecoveryUpgradeCost = new int[2]; // AP回復アップグレード資源コスト（0→1, 1→2）（鏡湖教用）

        // コンストラクタ的な初期化
        private void Reset()
        {
            // デフォルト値の設定
            if (string.IsNullOrEmpty(pieceName))
            {
                pieceName = "農民";
                canAttack = false;
            }
        }
    }
}