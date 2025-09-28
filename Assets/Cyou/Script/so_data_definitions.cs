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
        public int startBuildActionCost; // 建築開始に必要な行動力
        public int buildActionCost; // 建築過程に必要な行動力
        public int resourceGenerationInterval; // 資源生成間隔（ターン数）
        
        [Header("特殊建物属性")]
        public bool isSpecialBuilding; // バフ効果を持つ特別な建物か
        public int maxSkillSlots; // 駒を配置可能なスロット数
        
        [Header("生産設定")]
        public ResourceGenerationType generationType;
        public int baseProductionAmount;
        public float productionMultiplier = 1.0f;
        
        [Header("ビジュアル")]
        public Sprite buildingSprite;
        public GameObject buildingPrefab;
        public Color buildingColor = Color.white;
    }
    
    /// <summary>
    /// 駒（ピース）の基本データ定義
    /// </summary>
    [CreateAssetMenu(fileName = "PieceData", menuName = "GameData/Pieces/PieceData")]
    public class PieceDataSO : ScriptableObject
    {
        [Header("基本パラメータ")]
        public string pieceName;
        public float maxHP = 100f;
        public int populationCost = 1;
        public int resourceCost = 10;
        
        [Header("行動力パラメータ")]
        public float maxActionPoints = 100f;
        public float actionPointRecoveryRate = 10f; // 毎秒の回復量
        public float moveActionCost = 10f;
        
        [Header("戦闘パラメータ")]
        public bool canAttack = false;
        public float attackPower = 0f;
        public float attackRange = 0f;
        public float attackCooldown = 1f;
        public float attackActionCost = 20f;
        
        [Header("ビジュアル")]
        public Sprite pieceSprite;
        public GameObject piecePrefab;
        public RuntimeAnimatorController animatorController;
    }
    
    /// <summary>
    /// 農民特有のデータ定義
    /// </summary>
    [CreateAssetMenu(fileName = "FarmerData", menuName = "GameData/Pieces/FarmerData")]
    public class FarmerDataSO : PieceDataSO
    {
        [Header("建築能力")]
        public BuildingDataSO[] buildableBuildings; // 建築可能な建物リスト
        public float buildingSpeedModifier = 1.0f; // 建築速度修正値
        
        [Header("農民特有パラメータ")]
        public float workEfficiency = 1.0f; // 作業効率
        public int carryCapacity = 10; // 運搬能力
        
        [Header("スキルレベル設定")]
        public int maxSkillLevel = 10; // 最大スキルレベル
        public float skillProductionBonus = 0.2f; // スキルレベルごとの生産ボーナス（20%）
        
        // コンストラクタ的な初期化
        private void OnEnable()
        {
            // デフォルト値の設定
            if (string.IsNullOrEmpty(pieceName))
            {
                pieceName = "農民";
                maxHP = 150f;
                maxActionPoints = 100f;
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
        
        private void OnEnable()
        {
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
        public float actionPointCost;
        public float cooldown;
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
}