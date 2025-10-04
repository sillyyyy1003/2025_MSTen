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
    public abstract class Piece : VisualGameObject
    {
        [SerializeField] protected PieceDataSO pieceData;
        
        // ===== 実行時の状態 =====
        protected float currentHP;
        protected float currentAP;
        protected Team ownerTeam;
        protected PieceState currentState = PieceState.Idle;
        

        
        // ===== イベント =====
        public event Action<Piece> OnPieceDeath;
        public event Action<float, float> OnHPChanged;
        public event Action<float, float> OnAPChanged;
        public event Action<PieceState, PieceState> OnStateChanged;
        
        // ===== プロパティ =====
        public PieceDataSO Data => pieceData;
        public float CurrentHP => currentHP;
        public float CurrentAP => currentAP;
        public bool IsAlive => currentHP > 0;
        public bool CanMove => currentAP >= pieceData.moveAPCost;
        public Team OwnerTeam => ownerTeam;
        public PieceState State => currentState;
        
        #region 初期化
        
        public virtual void Initialize(PieceDataSO data, Team team)
        {
            pieceData = data;
            ownerTeam = team;
            
            currentHP = data.maxHP;
            currentAP = data.maxAP;
            
            SetupComponents();
            StartCoroutine(ActionPointRecoveryCoroutine());
        }

        protected virtual void SetupComponents()
        {
            SetupVisualComponents();
            ApplySprite(pieceData.pieceSprite, Color.white);
            ApplyMesh(pieceData.pieceMesh, pieceData.pieceMaterial);
        }

        #endregion

        #region 行動力管理

        protected IEnumerator ActionPointRecoveryCoroutine()
        {
            while (IsAlive)
            {
                if (currentAP < pieceData.maxAP && currentState != PieceState.InBuilding)
                {
                    ModifyAP(pieceData.aPRecoveryRate * Time.deltaTime);
                }
                yield return null;
            }
        }
        
        public bool ConsumeAP(float amount)
        {
            if (currentAP >= amount)
            {
                ModifyAP(-amount);
                return true;
            }
            return false;
        }
        
        protected void ModifyAP(float amount)
        {
            float oldValue = currentAP;
            currentAP = Mathf.Clamp(currentAP + amount, 0, pieceData.maxAP);
            
            if (!Mathf.Approximately(oldValue, currentAP))
            {
                OnAPChanged?.Invoke(currentAP, pieceData.maxAP);
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
    

}