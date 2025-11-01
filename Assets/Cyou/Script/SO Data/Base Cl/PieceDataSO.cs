// ===== ScriptableObject データ定義 =====
using UnityEngine;
using System; // 例外処理で使用

namespace GameData
{
    /// <summary>
    /// 駒（ピース）の基本データ定義
    /// </summary>
    [CreateAssetMenu(fileName = "PieceData", menuName = "GameData/BasePieces/PieceData")]
    public class PieceDataSO : ScriptableObject
    {
        [Header("基本パラメータ")]
        public int originalPID;//最初にどのプレイヤーに属すか
        public string pieceName;
        public int populationCost = 1;//コマ一つの人口消費量
        public int resourceCost = 10;//資源消費量
        public int initialLevel = 0;//初期レベル

        [Header("行動力パラメータ")]
        public float aPRecoveryRate = 10f; // 毎turnの行動力回復量
        public float moveAPCost = 10f;
        public float moveSpeed = 1.0f;//移動速度？

        [Header("戦闘パラメータ")]
        public bool canAttack = false;
        public int attackPower = 0;//一応残しておくが、廃止してもいい感じ
        public float attackRange = 0f;
        public float attackCooldown = 1f;
        public int attackAPCost = 20;


        public int[] maxHPByLevel = new int[4]; // レベルごとの最大HP
        public int[] maxAPByLevel = new int[4]; // レベルごとの最大AP
        public int[] attackPowerByLevel = new int[4]; // レベルごとの攻撃力

        [Header("各項目のアップグレードコスト")]
        public int[] hpUpgradeCost = new int[3]; // 血量アップグレード資源コスト（0→1, 1→2, 2→3）。0=アップグレード不可
        public int[] apUpgradeCost = new int[3]; // 行動力アップグレード資源コスト（0→1, 1→2, 2→3）

        [Header("Prefab")]
        public GameObject piecePrefab;

        /// <summary>
        /// レベルに応じたHP取得
        /// </summary>
        public int GetMaxHPByLevel(int level)
        {
            if (maxHPByLevel == null || maxHPByLevel.Length == 0)
            {
                Debug.LogError($"{pieceName}はInspector内でmaxHPByLevelの設定か行われていません！");
                throw new InvalidOperationException("maxHPByLevelの設定か行われていません！");
            }

            level = Mathf.Clamp(level, 0, maxHPByLevel.Length - 1);

            if (maxHPByLevel[level] > 0)
            {
                return maxHPByLevel[level];
            }
            else
            {
                Debug.LogError($"{pieceName}のmaxHPByLevelの値は正の値じゃないといけません！");
                throw new ArgumentException("maxHPByLevelの値が不正です！");
            }
        }

        /// <summary>
        /// レベルに応じたAP取得
        /// </summary>
        public int GetMaxAPByLevel(int level)
        {
            if (maxAPByLevel == null || maxAPByLevel.Length == 0)
            {
                Debug.LogError($"{pieceName}はInspector内でmaxAPByLevelの設定か行われていません！");
                throw new InvalidOperationException("maxAPByLevelの設定か行われていません！");
            }

            level = Mathf.Clamp(level, 0, maxAPByLevel.Length - 1);

            if (maxAPByLevel[level] > 0)
            {
                return maxAPByLevel[level];
            }
            else
            {
                Debug.LogError($"{pieceName}のmaxAPByLevelの値は正の値じゃないといけません！");
                throw new ArgumentException("maxAPByLevelの値が不正です！");
            }
        }

        /// <summary>
        /// レベルに応じた攻撃力取得
        /// </summary>
        public float GetAttackPowerByLevel(int level)
        {
            if (attackPowerByLevel == null || attackPowerByLevel.Length == 0) return attackPower;
            level = Mathf.Clamp(level, 0, attackPowerByLevel.Length - 1);
            return attackPowerByLevel[level] > 0 ? attackPowerByLevel[level] : attackPower;
        }
    }
}