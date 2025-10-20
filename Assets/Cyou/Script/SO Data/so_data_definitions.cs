// ===== ScriptableObject データ定義 =====
using UnityEngine;

namespace GameData
{
    /// <summary>
    /// 建物の基本データ定義
    /// C++のPOD構造体のような純粋なデータコンテナ
    /// </summary>
    [CreateAssetMenu(fileName = "BuildingData", menuName = "GameData/Buildings/BuildingData")]
    public class BuildingDataSO : ScriptableObject
    {
        [Header("基本属性")]
        public string buildingName;
        public int maxHp = 100;
        public int buildStartAPCost; // 建築開始に必要な行動力
        public int buildingAPCost; // 建築過程に必要な行動力
        public int resourceGenInterval; // 資源生成間隔（ターン数）

        [Header("特殊建物属性")]
        public bool isSpecialBuilding; // バフ効果を持つ特別な建物か
        public int maxUnitCapacity; // 駒を配置可能なスロット数

        [Header("生産設定")]
        public ResourceGenerationType generationType;
        public int baseProductionAmount;
        public int apCostperTurn;//資源生成する際毎ターン消耗する農民のAP
        public float productionMultiplier = 1.0f;

        [Header("アップグレードレベルごとのデータ（升級1,2）")]
        public int[] maxHpByLevel = new int[3] { 25, 30, 45 }; // 血量
        public int[] attackRangeByLevel = new int[3] { 0, 1, 2 }; // 攻撃範囲（無、有攻撃範囲1、攻撃範囲2）
        public int[] maxSlotsByLevel = new int[3] { 3, 3, 5 }; // 投入信徒数量
        public int[] buildingAPCostByLevel = new int[3] { 28, 25, 20 }; // 建造所需花費

        [Header("ビジュアル")]
        public Sprite buildingSprite;
        public Mesh buildingMesh;
        public Material buildingMaterial;
        public GameObject buildingPrefab;
        public Color buildingColor = Color.white;
        public RuntimeAnimatorController animatorController;

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

        /// <summary>
        /// レベルに応じた建築APコストを取得
        /// </summary>
        public int GetBuildingAPCostByLevel(int level)
        {
            level = Mathf.Clamp(level, 0, buildingAPCostByLevel.Length - 1);
            return buildingAPCostByLevel[level];
        }

    }
    
    /// <summary>
    /// 駒（ピース）の基本データ定義
    /// </summary>
    [CreateAssetMenu(fileName = "PieceData", menuName = "GameData/Pieces/PieceData")]
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
        public float attackPower = 0f;//一応残しておくが、廃止してもいい感じ
        public float attackRange = 0f;
        public float attackCooldown = 1f;
        public float attackAPCost = 20f;

 
        public float[] maxHPByLevel = new float[4]; // レベルごとの最大HP
        public float[] maxAPByLevel = new float[4]; // レベルごとの最大AP
        public float[] attackPowerByLevel = new float[4]; // レベルごとの攻撃力

        [Header("ビジュアル")]
        public Sprite pieceSprite;
        public Mesh pieceMesh;
        public Material pieceMaterial;
        public GameObject piecePrefab;
        public RuntimeAnimatorController animatorController;

        /// <summary>
        /// レベルに応じたHP取得
        /// </summary>
        public float GetMaxHPByLevel(int level)
        {
            if (maxHPByLevel == null || maxHPByLevel.Length == 0)
            {
                Debug.LogError($"{pieceName}はInspector内でmaxHPByLevelの設定か行われていません！");
                throw new System.InvalidOperationException("maxHPByLevelの設定か行われていません！");
            }
            
            level = Mathf.Clamp(level, 0, maxHPByLevel.Length - 1);
            
            if(maxHPByLevel[level] > 0)
            {
                return maxHPByLevel[level];
            }
            else
            {
                Debug.LogError($"{pieceName}のmaxHPByLevelの値は正の値じゃないといけません！");
                throw new System.ArgumentException("maxHPByLevelの値が不正です！");
            }
        }

        /// <summary>
        /// レベルに応じたAP取得
        /// </summary>
        public float GetMaxAPByLevel(int level)
        {
            if (maxAPByLevel == null || maxAPByLevel.Length == 0)
            {
                Debug.LogError($"{pieceName}はInspector内でmaxAPByLevelの設定か行われていません！");
                throw new System.InvalidOperationException("maxAPByLevelの設定か行われていません！");
            }

            level = Mathf.Clamp(level, 0, maxAPByLevel.Length - 1);

            if (maxAPByLevel[level] > 0)
            {
                return maxAPByLevel[level];
            }
            else
            {
                Debug.LogError($"{pieceName}のmaxAPByLevelの値は正の値じゃないといけません！");
                throw new System.ArgumentException("maxAPByLevelの値が不正です！");
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
    
    /// <summary>
    /// 農民特有のデータ定義
    /// </summary>
    [CreateAssetMenu(fileName = "FarmerData", menuName = "GameData/Pieces/FarmerData")]
    public class FarmerDataSO : PieceDataSO
    {
        [Header("建築能力")]
        public BuildingDataSO[] Buildings; // 建築可能な建物リスト
        public float buildingSpeedModifier = 1.0f; // 建築速度修正値
        
        [Header("資源生産効率")]
        public float productEfficiency = 1.0f; // 作業効率
        

        [Header("アップグレードレベルごとのデータ（升級1,2）")]
        public int[] maxHpByLevel = new int[3] { 3, 4, 5 }; // 血量
        public int[] maxApByLevel = new int[3] { 3, 5, 5 }; // 
        public int[] maxSacrificeLevel = new int[3] { 1, 2, 2 }; // 自分を生贄にして他駒を回復するスキル

        // コンストラクタ的な初期化
        private void OnEnable()
        {
            // デフォルト値の設定
            if (string.IsNullOrEmpty(pieceName))
            {
                pieceName = "農民";
                canAttack = false;
            }
        }
    }
    
    /// <summary>
    /// 軍隊ユニットのデータ定義
    /// </summary>
    [CreateAssetMenu(fileName = "MilitaryData", menuName = "GameData/Pieces/MilitaryData")]
    public class MilitaryDataSO : PieceDataSO
    {
        [Header("軍隊特有パラメータ")]
        public float armorValue = 10f;
        public float criticalChance = 0.1f;
        public DamageType damageType = DamageType.Physical;

        [Header("スキル")]
        public SkillDataSO[] availableSkills;

        [Header("アップグレードレベルごとのスキル効果（升級1,2,3）")]
        public bool[] hasAntiConversionSkill = new bool[4] { false, true, true, true }; // 魅惑敵性（升級3）

        /// <summary>
        /// レベルに応じた攻撃力係数を取得
        /// </summary>
        public float GetAttackRangeByLevel(int level)
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

        private void OnEnable()
        {
            pieceName = "十字軍";
            canAttack = true; // 軍隊は必ず攻撃可能
            if (attackPower <= 0)
            {
                attackPower = 25f;
                attackRange = 2f;
            }
        }
    }
    
    /// <summary>
    /// スキルデータ定義
    /// </summary>
    [CreateAssetMenu(fileName = "SkillData", menuName = "GameData/Skills/SkillData")]
    public class SkillDataSO : ScriptableObject
    {
        public string skillName;
        public string description;
        public float aPCost;
        public float coolDown;
        public float effectValue;
        public SkillType skillType;
        public Sprite skillIcon;
    }
    
    // ===== 列挙型定義 =====
    
    public enum ResourceGenerationType
    {
        None,
        Food,
        Wood,
        Stone,
        Iron,
        Gold
    }
    
    public enum DamageType
    {
        Physical,
        Magic,
        True
    }
    
    public enum SkillType
    {
        Attack,
        Buff,
        Debuff,
        Heal,
        Summon
    }

    
    public enum Team
    {
        Player,
        Enemy,
        Neutral
    }

    public enum PieceType
    {
        None,Military,Farmer,Missionary,Pope
    }

    public enum Religion
    {
        None,A,B,C,D,E,F,G,H
    }

    /// <summary>
    /// 教皇のデータ定義
    /// </summary>
    [CreateAssetMenu(fileName = "PopeData", menuName = "GameData/Pieces/PopeData")]
    public class PopeDataSO : PieceDataSO
    {
        [Header("位置交換能力")]
        public int[] swapCooldown =new int[4] {5,3,3,3}; // 位置交換のクールタイム（ターン数）

        [Header("バフ効果一覧")]
        public int[] hpBuff = new int[4] { 1, 2, 2, 2 };//周囲駒への体力バフ
        public int[] atkBuff = new int[4] { 1, 1, 1, 1 };//周囲駒への攻撃力バフ
        public float[] convertBuff = new float[4] { 0.05f, 0.08f, 0.08f, 0.08f };//周囲宣教師への魅惑スキル成功率バフ


        private void OnEnable()
        {
            if (string.IsNullOrEmpty(pieceName))
            {
                pieceName = "教皇";
                canAttack = false;
            }
        }
    }

    /// <summary>
    /// 宣教師のデータ定義
    /// </summary>
    [CreateAssetMenu(fileName = "MissionaryData", menuName = "GameData/Pieces/MissionaryData")]
    public class MissionaryDataSO : PieceDataSO
    {
        //[Header("移動設定")]（）廃止
        //public int maxMoveRangeOutsideTerritory = 1; // 領地外での最大移動マス数

        [Header("占領設定")]
        public float occupyAPCost = 30f; // 占領試行のAP消費量
        //public float occupyDuration = 5f; // 占領判定までの時間(秒)（廃止）

        [Header("特殊攻撃設定")]
        public float baseConversionAttackChance = 0.3f; // 基礎攻撃時変換確率
        public int[] conversionTurnDuration = new int[4] { 2, 3, 4, 5 }; // 変換した駒が敵に戻るまでのターン数

        [Header("特殊防御設定")]
        public float baseConversionDefenseChance = 0.2f; // 基礎防御時変換確率

        [Header("スキルレベル設定")]
        //public int maxSkillLevel = 10; // 最大スキルレベル
        //public float skillSuccessRateBonus = 0.05f; // スキルレベルごとの成功率ボーナス（5%）

        public float[] occupyEmptySuccessRateByLevel = new float[4] { 0.8f, 0.9f, 1.0f, 1.0f }; // 占領成功率
        public float[] occupyEnemySuccessRateByLevel = new float[4] { 0.5f, 0.6f, 0.7f, 0.7f }; // 占領成功率
        public float[] convertMissionaryChanceByLevel = new float[4] { 0.0f, 0.5f, 0.6f, 0.7f }; // 攻撃時転換確率
        public float[] convertFarmerChanceByLevel = new float[4] { 0.0f, 0.1f, 0.2f, 0.3f }; // 攻撃時転換確率（初期50%、升級1:60%、升級2:70%、升級3:70%）
        public float[] convertMilitaryChanceByLevel = new float[4] { 0.0f, 0.7f, 0.7f, 0.9f }; // 攻撃時転換確率
        public bool[] hasAntiConversionSkill = new bool[4] { false, true, true, true }; // 魅惑敵性（升級1以降）

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

        private void OnEnable()
        {
            if (string.IsNullOrEmpty(pieceName))
            {
                pieceName = "宣教師";
                canAttack = true;//魅惑スキルは軍の「攻撃」と違うが一応こう書いとく
                attackAPCost = 25f;
            }
        }
    }

   
}