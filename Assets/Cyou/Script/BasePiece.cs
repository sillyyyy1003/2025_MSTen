using UnityEngine;
using System;
using System.Collections;

namespace GamePieces
{
    /// <summary>
    /// すべての駒の基底クラス（GameManager対応版）
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
        [SerializeField] protected float actionPointRecoveryRate = 10f;
        [SerializeField] protected float moveActionCost = 10f;
        [SerializeField] protected float attackActionCost = 20f;

        [Header("移動パラメータ")]
        [SerializeField] protected float moveSpeed = 1f;
        [SerializeField] protected int moveRangeLimit = -1;

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

        #region 初期化

        /// <summary>
        /// GameManagerから呼ばれる初期化メソッド
        /// </summary>
        public virtual void Initialize(Team team, Vector2Int position)
        {
            OwnerTeam = team;
            CurrentPosition = position;
            currentHP = maxHP;
            currentActionPoints = maxActionPoints;
        }

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

        #region アクセサ

        /// <summary>
        /// リソースコストを取得（GameManager用）
        /// </summary>
        public int GetResourceCost()
        {
            return resourceCost;
        }

        /// <summary>
        /// ポピュレーションコストを取得（GameManager用）
        /// </summary>
        public int GetPopulationCost()
        {
            return populationCost;
        }

        #endregion

        #region 行動力管理

        /// <summary>
        /// 行動力を完全回復（ターン開始時にGameManagerから呼ばれる）
        /// </summary>
        public void RestoreActionPoints()
        {
            ModifyActionPoints(maxActionPoints - currentActionPoints);
        }

        /// <summary>
        /// 行動力の自動回復コルーチン
        /// </summary>
        protected IEnumerator ActionPointRecoveryCoroutine()
        {
            while (IsAlive)
            {
                // GameManagerの状態を確認
                if (GameManager.Instance != null &&
                    GameManager.Instance.CurrentState == GameManager.GameState.WaitingForInput &&
                    GameManager.Instance.CurrentTurn == OwnerTeam)
                {
                    if (currentActionPoints < maxActionPoints)
                    {
                        ModifyActionPoints(actionPointRecoveryRate * Time.deltaTime);
                    }
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

            // GameManagerに移動可能か確認
            if (GameManager.Instance != null &&
                !GameManager.Instance.CanMovePieceTo(this, targetPosition))
                return false;

            if (!IsValidMove(targetPosition))
                return false;

            // 移動先に敵がいるか確認
            BasePiece targetPiece = GetPieceAt(targetPosition);
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

            // GameBoard経由でマスの有効性チェック
            if (!IsValidPosition(targetPosition))
                return false;

            // 駒の重複チェック
            if (GetPieceAt(targetPosition) != null)
                return false;

            // 経路上の障害物チェック
            if (!IsPathClear(CurrentPosition, targetPosition))
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
            Vector3 endPos = GetWorldPosition(targetPosition);

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
        /// 攻撃の実行
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

        #region GameBoard連携用ヘルパーメソッド

        /// <summary>
        /// 指定位置の駒を取得（GameBoard経由）
        /// </summary>
        protected BasePiece GetPieceAt(Vector2Int position)
        {
            return GameBoard.Instance?.GetPieceAt(position);
        }

        /// <summary>
        /// 位置が有効かチェック（GameBoard経由）
        /// </summary>
        protected bool IsValidPosition(Vector2Int position)
        {
            return GameBoard.Instance?.IsValidPosition(position) ?? false;
        }

        /// <summary>
        /// パスが通行可能かチェック（GameBoard経由）
        /// </summary>
        protected bool IsPathClear(Vector2Int start, Vector2Int end)
        {
            return GameBoard.Instance?.IsPathClear(start, end) ?? false;
        }

        /// <summary>
        /// ワールド座標を取得（GameBoard経由）
        /// </summary>
        protected Vector3 GetWorldPosition(Vector2Int gridPosition)
        {
            return GameBoard.Instance?.GetWorldPosition(gridPosition) ?? Vector3.zero;
        }

        #endregion

        #region 休憩行動

        /// <summary>
        /// 休憩して行動力を回復する
        /// </summary>
        public virtual void Rest()
        {
            // 通常の2倍速で回復
            ModifyActionPoints(actionPointRecoveryRate * 2 * Time.deltaTime);
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