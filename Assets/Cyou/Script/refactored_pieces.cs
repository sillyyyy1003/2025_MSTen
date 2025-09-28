using UnityEngine;
using System;
using System.Collections;
using GameData;
using Buildings;

namespace GamePieces
{
    /// <summary>
    /// 駒の基底クラス - ScriptableObjectからデータを参照
    /// </summary>
    public abstract class Piece : MonoBehaviour
    {
        [SerializeField] protected PieceDataSO pieceData;
        
        // ===== 実行時の状態 =====
        protected float currentHP;
        protected float currentActionPoints;
        protected Team ownerTeam;
        protected PieceState currentState = PieceState.Idle;
        
        // ===== コンポーネントキャッシュ =====
        protected SpriteRenderer spriteRenderer;
        protected Animator animator;
        
        // ===== イベント =====
        public event Action<Piece> OnPieceDeath;
        public event Action<float, float> OnHPChanged;
        public event Action<float, float> OnActionPointsChanged;
        public event Action<PieceState, PieceState> OnStateChanged;
        
        // ===== プロパティ =====
        public PieceDataSO Data => pieceData;
        public float CurrentHP => currentHP;
        public float CurrentActionPoints => currentActionPoints;
        public bool IsAlive => currentHP > 0;
        public bool CanMove => currentActionPoints >= pieceData.moveActionCost;
        public Team OwnerTeam => ownerTeam;
        public PieceState State => currentState;
        
        #region 初期化
        
        public virtual void Initialize(PieceDataSO data, Team team)
        {
            pieceData = data;
            ownerTeam = team;
            
            currentHP = data.maxHP;
            currentActionPoints = data.maxActionPoints;
            
            SetupComponents();
            StartCoroutine(ActionPointRecoveryCoroutine());
        }
        
        protected virtual void SetupComponents()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer && pieceData.pieceSprite)
            {
                spriteRenderer.sprite = pieceData.pieceSprite;
            }
            
            animator = GetComponent<Animator>();
            if (!animator && pieceData.animatorController)
            {
                animator = gameObject.AddComponent<Animator>();
                animator.runtimeAnimatorController = pieceData.animatorController;
            }
        }
        
        #endregion
        
        #region 行動力管理
        
        protected IEnumerator ActionPointRecoveryCoroutine()
        {
            while (IsAlive)
            {
                if (currentActionPoints < pieceData.maxActionPoints && currentState != PieceState.InBuilding)
                {
                    ModifyActionPoints(pieceData.actionPointRecoveryRate * Time.deltaTime);
                }
                yield return null;
            }
        }
        
        public bool ConsumeActionPoints(float amount)
        {
            if (currentActionPoints >= amount)
            {
                ModifyActionPoints(-amount);
                return true;
            }
            return false;
        }
        
        protected void ModifyActionPoints(float amount)
        {
            float oldValue = currentActionPoints;
            currentActionPoints = Mathf.Clamp(currentActionPoints + amount, 0, pieceData.maxActionPoints);
            
            if (!Mathf.Approximately(oldValue, currentActionPoints))
            {
                OnActionPointsChanged?.Invoke(currentActionPoints, pieceData.maxActionPoints);
            }
        }
        
        #endregion
        
        #region ダメージ処理
        
        public virtual void TakeDamage(float damage, Piece attacker = null)
        {
            float oldHP = currentHP;
            currentHP = Mathf.Max(0, currentHP - damage);
            
            OnHPChanged?.Invoke(currentHP, pieceData.maxHP);
            
            if (currentHP <= 0 && oldHP > 0)
            {
                Die();
            }
        }
        
        public virtual void Heal(float amount)
        {
            float oldHP = currentHP;
            currentHP = Mathf.Min(pieceData.maxHP, currentHP + amount);
            
            if (!Mathf.Approximately(oldHP, currentHP))
            {
                OnHPChanged?.Invoke(currentHP, pieceData.maxHP);
            }
        }
        
        #endregion
        
        #region 状態管理
        
        protected void ChangeState(PieceState newState)
        {
            if (currentState != newState)
            {
                var oldState = currentState;
                currentState = newState;
                OnStateChanged?.Invoke(oldState, newState);
            }
        }
        
        #endregion
        
        #region 死亡処理
        
        protected virtual void Die()
        {
            ChangeState(PieceState.Dead);
            OnPieceDeath?.Invoke(this);
            StartCoroutine(DeathAnimation());
        }
        
        protected virtual IEnumerator DeathAnimation()
        {
            float fadeTime = 1f;
            float elapsedTime = 0;
            
            if (spriteRenderer != null)
            {
                Color originalColor = spriteRenderer.color;
                while (elapsedTime < fadeTime)
                {
                    float alpha = Mathf.Lerp(1, 0, elapsedTime / fadeTime);
                    spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }
            }
            
            Destroy(gameObject);
        }
        
        #endregion
    }
    
    /// <summary>
    /// 農民クラス
    /// </summary>
    public class Farmer : Piece
    {
        private FarmerDataSO farmerData;
        private Building currentBuilding;
        private int currentSkillLevel = 1; // 現在のスキルレベル
        
        // プロパティ
        public int SkillLevel => currentSkillLevel;
        
        public override void Initialize(PieceDataSO data, Team team)
        {
            farmerData = data as FarmerDataSO;
            if (farmerData == null)
            {
                Debug.LogError("農民にはFarmerDataSOが必要です");
                return;
            }
            
            base.Initialize(data, team);
        }
        
        /// <summary>
        /// スキルレベルを設定
        /// </summary>
        public void SetSkillLevel(int level)
        {
            currentSkillLevel = Mathf.Clamp(level, 1, farmerData.maxSkillLevel);
        }
        
        /// <summary>
        /// スキルレベルをレベルアップ
        /// </summary>
        public void LevelUp()
        {
            if (currentSkillLevel < farmerData.maxSkillLevel)
            {
                currentSkillLevel++;
            }
        }
        
        /// <summary>
        /// 生産倍率を取得（スキルレベルに基づく）
        /// </summary>
        public float GetProductionMultiplier()
        {
            return 1f + (currentSkillLevel - 1) * farmerData.skillProductionBonus;
        }
        
        /// <summary>
        /// 建物を建築
        /// </summary>
        public bool StartConstruction(int buildingIndex, Vector3 position)
        {
            if (farmerData == null || currentState != PieceState.Idle)
                return false;
            
            if (buildingIndex < 0 || buildingIndex >= farmerData.buildableBuildings.Length)
            {
                Debug.LogError($"無効な建物インデックス: {buildingIndex}");
                return false;
            }
            
            var buildingData = farmerData.buildableBuildings[buildingIndex];
            
            // 行動力チェック
            if (currentActionPoints < buildingData.startBuildActionCost)
                return false;
            
            // 行動力消費
            ConsumeActionPoints(buildingData.startBuildActionCost);
            
            // 建物生成
            var building = BuildingFactory.CreateBuilding(buildingData, position);
            if (building != null)
            {
                ChangeState(PieceState.Building);
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 建物に入る
        /// </summary>
        public bool EnterBuilding(Building building)
        {
            if (building == null || !building.IsOperational)
                return false;
            
            if (building.AssignFarmer(this))
            {
                currentBuilding = building;
                ChangeState(PieceState.InBuilding);
                // 視覚的に非表示に
                gameObject.SetActive(false);
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 建物から出る際の処理
        /// </summary>
        public void OnExitBuilding()
        {
            currentBuilding = null;
            
            // 行動力が0の場合は死亡
            if (currentActionPoints <= 0)
            {
                Die();
            }
            else
            {
                ChangeState(PieceState.Idle);
                gameObject.SetActive(true);
            }
        }
    }
    
    /// <summary>
    /// 軍隊ユニットクラス
    /// </summary>
    public class MilitaryUnit : Piece
    {
        private MilitaryDataSO militaryData;
        private float lastAttackTime;
        
        public override void Initialize(PieceDataSO data, Team team)
        {
            militaryData = data as MilitaryDataSO;
            if (militaryData == null)
            {
                Debug.LogError("軍隊ユニットにはMilitaryDataSOが必要です");
                return;
            }
            
            base.Initialize(data, team);
        }
        
        /// <summary>
        /// 攻撃実行
        /// </summary>
        public bool Attack(Piece target)
        {
            if (!militaryData.canAttack || target == null || !target.IsAlive)
                return false;
            
            if (Time.time - lastAttackTime < militaryData.attackCooldown)
                return false;
            
            if (!ConsumeActionPoints(militaryData.attackActionCost))
                return false;
            
            PerformAttack(target);
            lastAttackTime = Time.time;
            return true;
        }
        
        protected virtual void PerformAttack(Piece target)
        {
            float finalDamage = CalculateDamage();
            target.TakeDamage(finalDamage, this);
            
            // クリティカル判定
            if (UnityEngine.Random.value < militaryData.criticalChance)
            {
                finalDamage *= 2f;
                // クリティカルエフェクト表示
            }
        }
        
        private float CalculateDamage()
        {
            return militaryData.attackPower;
        }
        
        /// <summary>
        /// ダメージを受ける（アーマー考慮）
        /// </summary>
        public override void TakeDamage(float damage, Piece attacker = null)
        {
            float reducedDamage = damage - militaryData.armorValue;
            reducedDamage = Mathf.Max(1f, reducedDamage); // 最小1ダメージ
            
            base.TakeDamage(reducedDamage, attacker);
        }
    }
    
    /// <summary>
    /// 駒の状態
    /// </summary>
    public enum PieceState
    {
        Idle,       // 待機
        Moving,     // 移動中
        Attacking,  // 攻撃中
        Building,   // 建築中
        InBuilding, // 建物内
        Dead        // 死亡
    }
    
    /// <summary>
    /// 駒ファクトリー
    /// </summary>
    public static class PieceFactory
    {
        public static Piece CreatePiece(PieceDataSO data, Vector3 position, Team team)
        {
            if (data == null || data.piecePrefab == null)
            {
                Debug.LogError("駒データまたはPrefabが設定されていません");
                return null;
            }
            
            GameObject pieceObj = GameObject.Instantiate(data.piecePrefab, position, Quaternion.identity);
            Piece piece = null;
            
            // データ型に応じて適切なコンポーネントを追加
            if (data is FarmerDataSO)
            {
                piece = pieceObj.GetComponent<Farmer>() ?? pieceObj.AddComponent<Farmer>();
            }
            else if (data is MilitaryDataSO)
            {
                piece = pieceObj.GetComponent<MilitaryUnit>() ?? pieceObj.AddComponent<MilitaryUnit>();
            }
            else
            {
                Debug.LogError("未知の駒データタイプ");
                GameObject.Destroy(pieceObj);
                return null;
            }
            
            piece.Initialize(data, team);
            return piece;
        }
    }
}