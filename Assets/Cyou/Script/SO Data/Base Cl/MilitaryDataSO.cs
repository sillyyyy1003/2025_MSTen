// ===== ScriptableObject データ定義 =====
using UnityEngine;

namespace GameData
{
    /// <summary>
    /// 軍隊ユニットのデータ定義
    /// </summary>
    [CreateAssetMenu(fileName = "MilitaryData", menuName = "GameData/BasePieces/MilitaryData")]
    public class MilitaryDataSO : PieceDataSO
    {
        [Header("軍隊特有パラメータ")]
        public float criticalChance = 0.1f;
        public DamageType damageType = DamageType.Physical;

        [Header("スキル")]
        public SkillDataSO[] availableSkills;
        public int skillAPCost;

        [Header("アップグレードレベルごとのスキル効果（升級1,2,3）")]
        public bool[] hasAntiConversionSkill = new bool[4] { false, true, true, true }; // 魅惑敵性（升級3）

        [Header("軍隊専用アップグレードコスト")]
        public int[] attackPowerUpgradeCost = new int[3]; // 攻撃力アップグレード資源コスト（0→1, 1→2, 2→3）

        /// <summary>
        /// レベルに応じた攻撃力係数を取得
        /// </summary>
        public int GetAttackRangeByLevel(int level)
        {
            level = Mathf.Clamp(level, 0, attackPowerByLevel.Length - 1);
            return attackPowerByLevel[level];
        }

        /// <summary>
        /// 魅惑敵性スキルを持っているか
        /// </summary>
        public bool HasAntiConversionSkill(int level)
        {
            level = Mathf.Clamp(level, 0, hasAntiConversionSkill.Length - 1);
            return hasAntiConversionSkill[level];
        }

        private void Reset()
        {
            pieceName = "十字軍";
            canAttack = true; // 軍隊は必ず攻撃可能
            if (attackPower <= 0)
            {
                attackPower = 25;
                attackRange = 2f;
            }
        }
    }
}