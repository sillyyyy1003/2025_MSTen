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
        
        [Header("ビジュアル")]
        public Sprite buildingSprite;
        
        public Mesh buildingMesh;
        public Material buildingMaterial;
        
        public GameObject buildingPrefab;
        public Color buildingColor = Color.white;
        public RuntimeAnimatorController animatorController;
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
        public int populationCost = 1;//コマ一つの人口消費量
        public int resourceCost = 10;//資源消費量
        
        [Header("行動力パラメータ")]
        public float maxAP = 100f;
        public float aPRecoveryRate = 10f; // 毎turnの行動力回復量
        public float moveAPCost = 10f;
        public float moveSpeed = 1.0f;//移動速度？
        
        [Header("戦闘パラメータ")]
        public bool canAttack = false;
        public float attackPower = 0f;
        public float attackRange = 0f;
        public float attackCooldown = 1f;
        public float attackAPCost = 20f;
        
        [Header("ビジュアル")]
        public Sprite pieceSprite;

        public Mesh pieceMesh;
        public Material pieceMaterial;
        
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
        public BuildingDataSO[] Buildings; // 建築可能な建物リスト
        public float buildingSpeedModifier = 1.0f; // 建築速度修正値
        
        [Header("資源生産効率")]
        public float productEfficiency = 1.0f; // 作業効率
        
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
                maxAP = 100f;
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
        public float aPCost;
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