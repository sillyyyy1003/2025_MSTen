// ===== ScriptableObject データ定義 =====
using UnityEngine;

namespace GameData
{
    /// <summary>
    /// 建物の基本データ定義
    /// C++のPOD構造体のような純粋なデータコンテナ
    /// </summary>
    [CreateAssetMenu(fileName = "BuildingData", menuName = "GameData/BaseBuilding/BuildingData")]
    public class BuildingDataSO : ScriptableObject
    {
        [Header("Prefab Path")]
        public string piecePrefabResourcePath;
        [Header("宗教")]
        public Religion religion = Religion.None; // 所属する宗教
        [Header("基本属性")]
        public string buildingName;
        public int maxHp;
        public int buildingResourceCost; // 建築に必要な資源
        public int resourceGenInterval; // 資源生成間隔（ターン数）
        public Terrain cellType;//金鉱がある土地か否か

        [Header("特殊建物属性")]
        public bool isSpecialBuilding; // バフ効果を持つ特別な建物か
        public int maxUnitCapacity; // 駒を配置可能なスロット数

        [Header("生産設定")]
        public ResourceGenerationType generationType;
        public int baseProductionAmount;
        public int goldenProductionAmount;
        public float productionMultiplier = 1.0f;

        [Header("アップグレードレベルごとのデータ（升級1,2）")]
        public int[] maxHpByLevel = new int[3]; // 血量（Excel基礎数値）
        public int[] attackRangeByLevel = new int[3] { 0, 1, 2 }; // 攻撃範囲（無、有攻撃範囲1、攻撃範囲2）
        public int[] maxSlotsByLevel = new int[3] { 3, 5, 5 }; // 投入信徒数量（Excel基礎数値: 3, 5, 升級2未記載なので5を踏襲）

        [Header("反撃機能（鏡湖教用）")]
        public bool[] hasCounterAttack = new bool[3] { false, false, false }; // 反撃可能か（無、受到攻擊反擊、受到攻擊反擊）
        public int[] counterAttackDamage = new int[3] { 0, 0, 0 }; // 反撃ダメージ（0、基礎ダメージ、基礎ダメージ+1）

        [Header("各項目のアップグレードコスト")]
        public int[] hpUpgradeCost = new int[2]; // 血量アップグレード資源コスト（0→1, 1→2）。0=アップグレード不可
        public int[] attackRangeUpgradeCost = new int[2]; // 攻撃範囲アップグレード資源コスト（0→1, 1→2）
        public int[] slotsUpgradeCost = new int[2]; // 祭壇格子数アップグレード資源コスト（0→1, 1→2）

        [Header("Prefab")]
        public GameObject buildingPrefab;

        /// <summary>
        /// リセット関数
        /// </summary>
        private void Reset()
        {
            buildingResourceCost = 12;
        }

        /// <summary>
        /// レベルに応じた最大HPを取得
        /// </summary>
        public int GetMaxHpByLevel(int level)
        {
            level = Mathf.Clamp(level, 0, maxHpByLevel.Length - 1);
            return maxHpByLevel[level];
        }

        /// <summary>
        /// レベルに応じた攻撃範囲を取得
        /// </summary>
        public int GetAttackRangeByLevel(int level)
        {
            level = Mathf.Clamp(level, 0, attackRangeByLevel.Length - 1);
            return attackRangeByLevel[level];
        }

        /// <summary>
        /// レベルに応じた最大スロット数を取得
        /// </summary>
        public int GetMaxSlotsByLevel(int level)
        {
            level = Mathf.Clamp(level, 0, maxSlotsByLevel.Length - 1);
            return maxSlotsByLevel[level];
        }

        public int GetBuildingResourceProduction()
        {
            return cellType switch
            {
                Terrain.Normal => baseProductionAmount,
                Terrain.Gold => goldenProductionAmount,
                _ => 0
            };
        }
    }
}