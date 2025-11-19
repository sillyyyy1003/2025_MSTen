// ===== ScriptableObject データ定義 =====
using UnityEngine;

namespace GameData
{
    /// <summary>
    /// 教皇のデータ定義
    /// </summary>
    [CreateAssetMenu(fileName = "PopeData", menuName = "GameData/BasePieces/PopeData")]
    public class PopeDataSO : PieceDataSO
    {
        [Header("位置交換能力")]
        public int[] swapCooldown = new int[4] { 5, 3, 3, 3 }; // 位置交換のクールタイム（ターン数）

        [Header("バフ効果一覧")]
        public int[] hpBuff = new int[4] { 1, 2, 3, 3 };//周囲駒への体力バフ（Excel基礎数値: +1, +2, +3）
        public int[] atkBuff = new int[4] { 1, 1, 1, 1 };//周囲駒への攻撃力バフ
        public float[] convertBuff = new float[4] { 0.03f, 0.05f, 0.08f, 0.08f };//周囲宣教師への魅惑スキル成功率バフ（Excel基礎数値: +3%, +5%, +8%）
        public float[] defenseBuff = new float[4] { 0.0f, 0.0f, 0.0f, 0.0f };//周囲駒への防御力バフ（鏡湖教用: +10%, +15%, +20%, +25%）

        [Header("教皇専用アップグレードコスト")]
        public int[] swapCooldownUpgradeCost = new int[2]; // 位置交換CDアップグレード資源コスト（0→1, 1→2）
        public int[] buffUpgradeCost = new int[3]; // バフ効果アップグレード資源コスト（0→1, 1→2, 2→3）

        private void Reset()
        {
            if (string.IsNullOrEmpty(pieceName))
            {
                pieceName = "教皇";
                canAttack = false;
            }
        }
    }
}