using UnityEngine;
using System;
using System.Collections;

namespace GamePieces
{
    /// <summary>
    /// すべての駒の基底クラス
    /// </summary>
    public abstract class BasePiece : MonoBehaviour
    {
        #region 基本属性
        
        [Header("基本パラメータ")]
        [SerializeField] protected float maxHP = 100f;
        [SerializeField] protected float currentHP;
        [SerializeField] protected int populationCost = 1;
        [SerializeField] protected int resourceCost = 10;
        
        [Header("戦闘パラメータ")]
        [SerializeField] protected float attackPower = 10f;
        [SerializeField] protected bool canAttack = true;
        [SerializeField] protected float attackRange = 1f;
        [SerializeField] protected float attackCoolDown = 1f;
        protected float lastAttackTime;
        
        [Header("行動力システム")]
        [SerializeField] protected float maxActionPoints = 100f;
        [SerializeField] protected float currentActionPoints;
        [SerializeField] protected float actionPointRecoveryRate = 10f; // 毎秒の回復量
        [SerializeField] protected float moveActionCost = 10f;
        [SerializeField] protected float attackActionCost = 20f;
        
        [Header("移動パラメータ")]
        [SerializeField] protected float moveSpeed = 1f; // 1マス移動にかかる時間（秒）
        [SerializeField] protected int moveRangeLimit = -1; // -1は無制限、正の値は移動範囲制限
        
        #endregion
        
        #region プロパティ
        
        public float CurrentHP => currentHP;
        public float MaxHP => maxHP;
        public float CurrentActionPoints => currentActionPoints;
        public float MaxActionPoints => maxActionPoints;
        public bool IsAlive => currentHP > 0;
        public bool CanMove => currentActionPoints >= moveActionCost;
        public bool CanPerformAttack => canAttack && currentActionPoints >= attackActionCost;
        public Vector2Int CurrentPosition { get; protected set; }
        public Team OwnerTeam { get; protected set; }
        
        #endregion
        
        #region イベント
        
        public event Action<BasePiece> OnPieceDeath;
        public event Action<float, float> OnHPChanged;
        public event Action<float, float> OnActionPointsChanged;
        public event Action<Vector2Int, Vector2Int> OnPositionChanged;
        
        #endregion
        
        #region Unity Lifecycle
        
        protected virtual void Awake()
        {
            currentHP = maxHP;
            currentActionPoints = maxActionPoints;
        }
        
        protected virtual void Start()
        {
            StartCoroutine(ActionPointRecoveryCoroutine());
        }
        
        protected virtual void Update()
        {
            // 派生クラスで必要に応じてオーバーライド
        }
        
        #endregion
        
        #region 行動力管理
        
        /// <summary>
        /// 行動力の自動回復コルーチン
        /// </summary>
        protected IEnumerator ActionPointRecoveryCoroutine()
        {
            while (IsAlive)
            {
                if (currentActionPoints < maxActionPoints)
                {
                    ModifyActionPoints(actionPointRecoveryRate * Time.deltaTime);
                }
                yield return null;
            }
        }
        
        /// <summary>
        /// 行動力を消費する
        /// </summary>
        protected bool ConsumeActionPoints(float amount)
        {
            if (currentActionPoints >= amount)
            {
                ModifyActionPoints(-amount);
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// 行動力を変更する
        /// </summary>
        protected void ModifyActionPoints(float amount)
        {
            float oldValue = currentActionPoints;
            currentActionPoints = Mathf.Clamp(currentActionPoints + amount, 0, maxActionPoints);
            
            if (oldValue != currentActionPoints)
            {
                OnActionPointsChanged?.Invoke(currentActionPoints, maxActionPoints);
            }
        }
        
        #endregion
        
        #region 移動システム
        
        /// <summary>
        /// 指定位置への移動を試みる
        /// </summary>
        public virtual bool TryMove(Vector2Int targetPosition)
        {
            if (!CanMove)
                return false;
            
            if (!IsValidMove(targetPosition))
                return false;
            
            // 移動先に敵がいるか確認
            BasePiece targetPiece = GameBoard.Instance.GetPieceAt(targetPosition);
            if (targetPiece != null && targetPiece.OwnerTeam != OwnerTeam)
            {
                // 自動攻撃
                return TryAttack(targetPiece);
            }
            
            // 移動実行
            if (ConsumeActionPoints(moveActionCost))
            {
                StartCoroutine(MoveCoroutine(targetPosition));
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 移動が有効かチェック
        /// </summary>
        protected virtual bool IsValidMove(Vector2Int targetPosition)
        {
            // 移動範囲制限チェック
            if (moveRangeLimit > 0)
            {
                int distance = Mathf.Abs(targetPosition.x - CurrentPosition.x) + 
                              Mathf.Abs(targetPosition.y - CurrentPosition.y);
                if (distance > moveRangeLimit)
                    return false;
            }
            
            // マスの有効性チェック
            if (!GameBoard.Instance.IsValidPosition(targetPosition))
                return false;
            
            // 駒の重複チェック（1マスに1体のみ）
            if (GameBoard.Instance.GetPieceAt(targetPosition) != null)
                return false;
            
            // 経路上の障害物チェック（駒を飛び越えられない）
            if (!GameBoard.Instance.IsPathClear(CurrentPosition, targetPosition))
                return false;
            
            return true;
        }
        
        /// <summary>
        /// 移動アニメーション
        /// </summary>
        protected virtual IEnumerator MoveCoroutine(Vector2Int targetPosition)
        {
            Vector2Int oldPosition = CurrentPosition;
            Vector3 startPos = transform.position;
            Vector3 endPos = GameBoard.Instance.GetWorldPosition(targetPosition);
            
            float elapsedTime = 0;
            while (elapsedTime < moveSpeed)
            {
                transform.position = Vector3.Lerp(startPos, endPos, elapsedTime / moveSpeed);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            transform.position = endPos;
            CurrentPosition = targetPosition;
            OnPositionChanged?.Invoke(oldPosition, targetPosition);
        }
        
        #endregion
        
        #region 戦闘システム
        
        /// <summary>
        /// 攻撃を試みる
        /// </summary>
        public virtual bool TryAttack(BasePiece target)
        {
            if (!CanPerformAttack)
                return false;
            
            if (Time.time - lastAttackTime < attackCoolDown)
                return false;
            
            float distance = Vector2Int.Distance(CurrentPosition, target.CurrentPosition);
            if (distance > attackRange)
                return false;
            
            if (ConsumeActionPoints(attackActionCost))
            {
                PerformAttack(target);
                lastAttackTime = Time.time;
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 攻撃の実行（派生クラスでオーバーライド可能）
        /// </summary>
        protected virtual void PerformAttack(BasePiece target)
        {
            target.TakeDamage(attackPower, this);
        }
        
        /// <summary>
        /// ダメージを受ける
        /// </summary>
        public virtual void TakeDamage(float damage, BasePiece attacker)
        {
            float oldHP = currentHP;
            currentHP = Mathf.Max(0, currentHP - damage);
            
            OnHPChanged?.Invoke(currentHP, maxHP);
            
            if (currentHP <= 0 && oldHP > 0)
            {
                Die();
            }
        }
        
        #endregion
        
        #region 死亡処理
        
        /// <summary>
        /// 死亡処理
        /// </summary>
        protected virtual void Die()
        {
            OnPieceDeath?.Invoke(this);
            
            // 死亡アニメーション
            StartCoroutine(DeathAnimation());
        }
        
        protected virtual IEnumerator DeathAnimation()
        {
            // 簡単なフェードアウト
            float fadeTime = 1f;
            float elapsedTime = 0;
            
            SpriteRenderer renderer = GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                Color originalColor = renderer.color;
                while (elapsedTime < fadeTime)
                {
                    float alpha = Mathf.Lerp(1, 0, elapsedTime / fadeTime);
                    renderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }
            }
            
            Destroy(gameObject);
        }
        
        #endregion
        
        #region 静止行動
        
        /// <summary>
        /// 静止して行動力を回復する
        /// </summary>
        public virtual void Rest()
        {
            // 回復速度を一時的に上昇させるなどの処理
            // 基本的な回復は自動回復コルーチンで行われる
        }
        
        #endregion
    }
    
    /// <summary>
    /// チーム定義
    /// </summary>
    public enum Team
    {
        Player,
        Enemy,
        Neutral
    }
}