using UnityEngine;
using System;
using System.Collections;

namespace GamePieces
{
    /// <summary>
    /// ���ׂĂ̋�̊��N���X�iGameManager�Ή��Łj
    /// </summary>
    public abstract class BasePiece : MonoBehaviour
    {
        #region ��{����

        [Header("��{�p�����[�^")]
        [SerializeField] protected float maxHP = 100f;
        [SerializeField] protected float currentHP;
        [SerializeField] protected int populationCost = 1;
        [SerializeField] protected int resourceCost = 10;

        [Header("�퓬�p�����[�^")]
        [SerializeField] protected float attackPower = 10f;
        [SerializeField] protected bool canAttack = true;
        [SerializeField] protected float attackRange = 1f;
        [SerializeField] protected float attackCoolDown = 1f;
        protected float lastAttackTime;

        [Header("�s���̓V�X�e��")]
        [SerializeField] protected float maxActionPoints = 100f;
        [SerializeField] protected float currentActionPoints;
        [SerializeField] protected float actionPointRecoveryRate = 10f;
        [SerializeField] protected float moveActionCost = 10f;
        [SerializeField] protected float attackActionCost = 20f;

        [Header("�ړ��p�����[�^")]
        [SerializeField] protected float moveSpeed = 1f;
        [SerializeField] protected int moveRangeLimit = -1;

        #endregion

        #region �v���p�e�B

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

        #region �C�x���g

        public event Action<BasePiece> OnPieceDeath;
        public event Action<float, float> OnHPChanged;
        public event Action<float, float> OnActionPointsChanged;
        public event Action<Vector2Int, Vector2Int> OnPositionChanged;

        #endregion

        #region ������

        /// <summary>
        /// GameManager����Ă΂�鏉�������\�b�h
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
            // �h���N���X�ŕK�v�ɉ����ăI�[�o�[���C�h
        }

        #endregion

        #region �A�N�Z�T

        /// <summary>
        /// ���\�[�X�R�X�g���擾�iGameManager�p�j
        /// </summary>
        public int GetResourceCost()
        {
            return resourceCost;
        }

        /// <summary>
        /// �|�s�����[�V�����R�X�g���擾�iGameManager�p�j
        /// </summary>
        public int GetPopulationCost()
        {
            return populationCost;
        }

        #endregion

        #region �s���͊Ǘ�

        /// <summary>
        /// �s���͂����S�񕜁i�^�[���J�n����GameManager����Ă΂��j
        /// </summary>
        public void RestoreActionPoints()
        {
            ModifyActionPoints(maxActionPoints - currentActionPoints);
        }

        /// <summary>
        /// �s���͂̎����񕜃R���[�`��
        /// </summary>
        protected IEnumerator ActionPointRecoveryCoroutine()
        {
            while (IsAlive)
            {
                // GameManager�̏�Ԃ��m�F
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
        /// �s���͂������
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
        /// �s���͂�ύX����
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

        #region �ړ��V�X�e��

        /// <summary>
        /// �w��ʒu�ւ̈ړ������݂�
        /// </summary>
        public virtual bool TryMove(Vector2Int targetPosition)
        {
            if (!CanMove)
                return false;

            // GameManager�Ɉړ��\���m�F
            if (GameManager.Instance != null &&
                !GameManager.Instance.CanMovePieceTo(this, targetPosition))
                return false;

            if (!IsValidMove(targetPosition))
                return false;

            // �ړ���ɓG�����邩�m�F
            BasePiece targetPiece = GetPieceAt(targetPosition);
            if (targetPiece != null && targetPiece.OwnerTeam != OwnerTeam)
            {
                // �����U��
                return TryAttack(targetPiece);
            }

            // �ړ����s
            if (ConsumeActionPoints(moveActionCost))
            {
                StartCoroutine(MoveCoroutine(targetPosition));
                return true;
            }

            return false;
        }

        /// <summary>
        /// �ړ����L�����`�F�b�N
        /// </summary>
        protected virtual bool IsValidMove(Vector2Int targetPosition)
        {
            // �ړ��͈͐����`�F�b�N
            if (moveRangeLimit > 0)
            {
                int distance = Mathf.Abs(targetPosition.x - CurrentPosition.x) +
                              Mathf.Abs(targetPosition.y - CurrentPosition.y);
                if (distance > moveRangeLimit)
                    return false;
            }

            // GameBoard�o�R�Ń}�X�̗L�����`�F�b�N
            if (!IsValidPosition(targetPosition))
                return false;

            // ��̏d���`�F�b�N
            if (GetPieceAt(targetPosition) != null)
                return false;

            // �o�H��̏�Q���`�F�b�N
            if (!IsPathClear(CurrentPosition, targetPosition))
                return false;

            return true;
        }

        /// <summary>
        /// �ړ��A�j���[�V����
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

        #region �퓬�V�X�e��

        /// <summary>
        /// �U�������݂�
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
        /// �U���̎��s
        /// </summary>
        protected virtual void PerformAttack(BasePiece target)
        {
            target.TakeDamage(attackPower, this);
        }

        /// <summary>
        /// �_���[�W���󂯂�
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

        #region ���S����

        /// <summary>
        /// ���S����
        /// </summary>
        protected virtual void Die()
        {
            OnPieceDeath?.Invoke(this);

            // ���S�A�j���[�V����
            StartCoroutine(DeathAnimation());
        }

        protected virtual IEnumerator DeathAnimation()
        {
            // �ȒP�ȃt�F�[�h�A�E�g
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

        #region GameBoard�A�g�p�w���p�[���\�b�h

        /// <summary>
        /// �w��ʒu�̋���擾�iGameBoard�o�R�j
        /// </summary>
        protected BasePiece GetPieceAt(Vector2Int position)
        {
            return GameBoard.Instance?.GetPieceAt(position);
        }

        /// <summary>
        /// �ʒu���L�����`�F�b�N�iGameBoard�o�R�j
        /// </summary>
        protected bool IsValidPosition(Vector2Int position)
        {
            return GameBoard.Instance?.IsValidPosition(position) ?? false;
        }

        /// <summary>
        /// �p�X���ʍs�\���`�F�b�N�iGameBoard�o�R�j
        /// </summary>
        protected bool IsPathClear(Vector2Int start, Vector2Int end)
        {
            return GameBoard.Instance?.IsPathClear(start, end) ?? false;
        }

        /// <summary>
        /// ���[���h���W���擾�iGameBoard�o�R�j
        /// </summary>
        protected Vector3 GetWorldPosition(Vector2Int gridPosition)
        {
            return GameBoard.Instance?.GetWorldPosition(gridPosition) ?? Vector3.zero;
        }

        #endregion

        #region �x�e�s��

        /// <summary>
        /// �x�e���čs���͂��񕜂���
        /// </summary>
        public virtual void Rest()
        {
            // �ʏ��2�{���ŉ�
            ModifyActionPoints(actionPointRecoveryRate * 2 * Time.deltaTime);
        }

        #endregion
    }

    /// <summary>
    /// �`�[����`
    /// </summary>
    public enum Team
    {
        Player,
        Enemy,
        Neutral
    }
}