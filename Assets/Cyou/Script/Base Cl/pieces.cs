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
        protected float currentMaxHP;
        protected float currentHP;
        protected float currentMaxAP;
        protected float currentAP;
        protected int currentPID; // 現在所属しているプレイヤーID
        protected int originalPID; // 元々所属していたプレイヤーID
        protected PieceState currentState = PieceState.Idle;
        protected int upgradeLevel = 0; // 0:初期、1:升級1、2:升級2、3:升級3



        // ===== イベント =====
        public event Action<Piece> OnPieceDeath;
        public event Action<Piece,Piece> OnCharmed;//(魅惑された駒、魅惑した駒)
        public event Action<float, float> OnHPChanged;
        public event Action<float, float> OnAPChanged;
        public event Action<PieceState, PieceState> OnStateChanged;

        // ===== プロパティ =====
        public PieceDataSO Data => pieceData;
        public int CurrentPID => currentPID;
        public int OriginalPID => originalPID;
        public float CurrentHP => currentHP;
        public float CurrentAP => currentAP;
        public bool IsAlive => currentHP > 0;
        public bool CanMove => currentAP >= pieceData.moveAPCost;
        public PieceState State => currentState;
        public int UpgradeLevel => upgradeLevel;
        
        #region 初期化

        /// <summary>
        /// 駒を初期化（プレイヤーIDで管理）
        /// </summary>
        /// <param name="data">駒のデータ</param>
        /// <param name="playerID">所属するプレイヤーID</param>
        public virtual void Initialize(PieceDataSO data, int playerID)
        {
            pieceData = data;
            originalPID = data.originalPID;
            currentPID = playerID;

            currentMaxHP = data.maxHPByLevel[0];
            currentHP = currentMaxHP;
            currentMaxAP = data.maxAPByLevel[0];
            currentAP = currentMaxAP;

            SetupComponents();
            StartCoroutine(ActionPointRecoveryCoroutine());
        }

        /// <summary>
        /// プレイヤーIDを変更（陣営変更）
        /// </summary>
        /// <param name="newPlayerID">新しいプレイヤーID</param>
        public virtual void ChangePID(int newPlayerID,Piece charmer=null)
        {
            currentPID = newPlayerID;
            OnCharmed?.Invoke(this,charmer);
            Debug.Log($"{pieceData.originalPID}の{pieceData.pieceName} がプレイヤー{newPlayerID}の駒になりました");
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
                if (currentAP < currentMaxAP && currentState != PieceState.InBuilding)
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
            currentAP = Mathf.Clamp(currentAP + amount, 0, currentMaxAP);
            
            if (!Mathf.Approximately(oldValue, currentAP))
            {
                OnAPChanged?.Invoke(currentAP, currentMaxAP);
            }
        }
        
        #endregion
        
        #region ダメージ処理
        
        public virtual void TakeDamage(float damage, Piece attacker = null)
        {
            float oldHP = currentHP;
            currentHP = Mathf.Max(0, currentHP - damage);
            
            OnHPChanged?.Invoke(currentHP, currentMaxHP);
            
            if (currentHP <= 0 && oldHP > 0)
            {
                Die();
            }
        }
        
        public virtual void Heal(float amount)
        {
            float oldHP = currentHP;
            currentHP = Mathf.Min(currentMaxHP, currentHP + amount);
            
            if (!Mathf.Approximately(oldHP, currentHP))
            {
                OnHPChanged?.Invoke(currentHP, currentMaxHP);
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

        #region アップグレード管理

        /// <summary>
        /// 駒をアップグレードする
        /// </summary>
        public virtual bool UpgradePiece()
        {
            if (upgradeLevel >= 3)
            {
                Debug.LogWarning($"{pieceData.pieceName} は既に最大レベルです");
                return false;
            }

            upgradeLevel++;
            ApplyUpgradeEffects();
            Debug.Log($"{pieceData.pieceName} がレベル {upgradeLevel} にアップグレードしました");
            return true;
        }

        /// <summary>
        /// アップグレード効果を適用（派生クラスでオーバーライド）
        /// </summary>
        protected virtual void ApplyUpgradeEffects()
        {
            // 基底クラスでは何もしない
            // 派生クラスで具体的な効果を実装
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