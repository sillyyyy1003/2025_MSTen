// ===== ScriptableObject データ定義 =====
using UnityEngine;

namespace GameData
{
    /// <summary>
    /// 宣教師のデータ定義
    /// </summary>
    [CreateAssetMenu(fileName = "MissionaryData", menuName = "GameData/BasePieces/MissionaryData")]
    public class MissionaryDataSO : PieceDataSO
    {
        //[Header("移動設定")]（）廃止
        //public int maxMoveRangeOutsideTerritory = 1; // 領地外での最大移動マス数

        [Header("占領設定")]
        public int occupyAPCost = 30; // 占領試行のAP消費量
        //public float occupyDuration = 5f; // 占領判定までの時間(秒)（廃止）

        [Header("特殊攻撃設定")]
        public int convertAPCost = 3; // 基礎攻撃時変換確率
        public int[] conversionTurnDuration = new int[4] { 2, 2, 2, 2 }; // 変換した駒が敵に戻るまでのターン数

        [Header("特殊防御設定")]
        public float baseConversionDefenseChance = 0.2f; // 基礎防御時変換確率

        [Header("スキルレベル設定")]

        public float[] occupyEmptySuccessRateByLevel = new float[4] { 0.8f, 0.9f, 1.0f, 1.0f }; // 占領成功率（Excel基礎数値）
        public float[] occupyEnemySuccessRateByLevel = new float[4] { 0.5f, 0.6f, 0.7f, 0.7f }; // 占領成功率（Excel基礎数値）
        public float[] convertMissionaryChanceByLevel = new float[4] { 0.0f, 0.5f, 0.55f, 0.65f }; // 魅惑傳教士（Excel基礎数値: 無, 0.5, 0.55, 0.65）
        public float[] convertFarmerChanceByLevel = new float[4] { 0.0f, 0.1f, 0.2f, 0.3f }; // 魅惑信徒（Excel基礎数値: 無, 0.1, 0.2, 0.3）
        public float[] convertMilitaryChanceByLevel = new float[4] { 0.0f, 0.45f, 0.5f, 0.6f }; // 魅惑十字軍（Excel基礎数値: 無, 0.45, 0.5, 0.6）
        public bool[] hasAntiConversionSkill = new bool[4] { false, true, true, true }; // 魅惑敵性（升級1以降）

        [Header("宣教師専用アップグレードコスト")]
        public int[] occupyUpgradeCost = new int[3]; // 佔領空白領土アップグレード資源コスト（0→1, 1→2, 2→3）
        public int[] convertEnemyUpgradeCost = new int[3]; // 魅惑傳教士アップグレード資源コスト（0→1, 1→2, 2→3）

        /// <summary>
        /// レベルに応じた空白領地占領成功率を取得
        /// </summary>
        public float GetOccupyEmptySuccessRate(int level)
        {
            level = Mathf.Clamp(level, 0, occupyEmptySuccessRateByLevel.Length - 1);
            return occupyEmptySuccessRateByLevel[level];
        }

        /// <summary>
        /// レベルに応じた敵領地占領成功率を取得
        /// </summary>
        public float GetOccupyEnemySuccessRate(int level)
        {
            level = Mathf.Clamp(level, 0, occupyEnemySuccessRateByLevel.Length - 1);
            return occupyEnemySuccessRateByLevel[level];
        }

        /// <summary>
        /// レベルに応じた宣教師への魅惑成功率を取得
        /// </summary>
        public float GetConvertMissionaryChance(int level)
        {
            level = Mathf.Clamp(level, 0, convertMissionaryChanceByLevel.Length - 1);
            return convertMissionaryChanceByLevel[level];
        }

        /// <summary>
        /// レベルに応じた信徒への魅惑成功率を取得
        /// </summary>
        public float GetConvertFarmerChance(int level)
        {
            level = Mathf.Clamp(level, 0, convertFarmerChanceByLevel.Length - 1);
            return convertFarmerChanceByLevel[level];
        }

        /// <summary>
        /// レベルに応じた十字軍への魅惑成功率を取得
        /// </summary>
        public float GetConvertMilitaryChance(int level)
        {
            level = Mathf.Clamp(level, 0, convertMilitaryChanceByLevel.Length - 1);
            return convertMilitaryChanceByLevel[level];
        }

        /// <summary>
        /// 魅惑耐性を持っているか
        /// </summary>
        public bool HasAntiConversionSkill(int level)
        {
            level = Mathf.Clamp(level, 0, hasAntiConversionSkill.Length - 1);
            return hasAntiConversionSkill[level];
        }

        private void Reset()
        {
            if (string.IsNullOrEmpty(pieceName))
            {
                pieceName = "宣教師";
                canAttack = true;//魅惑スキルは軍の「攻撃」と違うが一応こう書いとく
                attackAPCost = 25;
            }
        }
    }
}