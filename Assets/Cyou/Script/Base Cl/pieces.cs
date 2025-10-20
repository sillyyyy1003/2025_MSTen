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
        public DemoUITest GM;

        // ===== 実行時の状態 =====
        protected float currentMaxHP;
        protected float currentHP;
        protected float currentMaxAP;
        protected float currentAP;
        protected int currentPID; // 現在所属しているプレイヤーID
        protected int originalPID; // 元々所属していたプレイヤーID
        protected PieceState currentState = PieceState.Idle;
        protected int upgradeLevel = 0; // 0:初期、1:升級1、2:升級2、3:升級3


        /// <summary>
        /// 変換した駒の情報を保持する構造体
        /// </summary>
        public struct ConvertedPieceInfo
        {
            public Piece convertedPiece;
            public int originalPlayerID;
            public float convertedTurn;

        }



        // ===== イベント =====

        //GameManagerとの連絡用
        public static event Action<Piece, Piece> OnAnyCharmed;    // グローバルイベント
        public static event Action<Piece> OnAnyUncharmed;         // グローバルイベント

        public event Action<Piece> OnPieceDeath;
        public event Action<Piece,Piece> OnCharmed;//(魅惑された駒、魅惑した駒)
        public event Action<Piece> OnUncharmed;//魅惑状態が解除して元の陣営に戻った駒
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
            if (CurrentHP <= 0)
                Debug.LogError($"{data.pieceName}のHP初期値定義が0以下です！");
            currentMaxAP = data.maxAPByLevel[0];
            currentAP = currentMaxAP;
            if (CurrentAP <= 0)
                Debug.LogError($"{data.pieceName}のAP初期値定義が0以下です！");


            SetupComponents();
            //StartCoroutine(ActionPointRecoveryCoroutine());
        }

        /// <summary>
        /// プレイヤーIDを変更（陣営変更）
        /// 変更なので勿論元戻りも含まれている
        /// </summary>
        /// <param name="newPlayerID">新しいプレイヤーID</param>
        public virtual void ChangePID(int newPlayerID, int charmTurns=0,Piece charmer=null)
        {
            currentPID = newPlayerID;
            if (newPlayerID != OriginalPID)
            {
                OnCharmed?.Invoke(this, charmer);
                Debug.Log($"{pieceData.originalPID}の{pieceData.pieceName} がプレイヤー{newPlayerID}の駒になりました");
            }
            else if(newPlayerID==OriginalPID)
            {
                OnUncharmed?.Invoke(this);
                Debug.Log($"{pieceData.originalPID}の{pieceData.pieceName} がプレイヤー{newPlayerID}に復帰しました。");
            }

            if(charmTurns>0)
                StartCoroutine(RevertCharmAfterTurns(charmTurns,OriginalPID));


        }

        private IEnumerator RevertCharmAfterTurns(int charmTurns,int originalPID)
        {
            int remainingTurns=charmTurns;
            int endTurn = remainingTurns + DemoUITest.GetTurn();//魅惑が解除されるターンの数字

            while (remainingTurns > 0)
            {
                yield return new WaitUntil(() => DemoUITest.GetTurn() >= endTurn);//GameManagerからターンの終了宣告を貰う
                    remainingTurns--;
            }
            
            currentPID = originalPID;
            OnUncharmed?.Invoke(this);
            Debug.Log($"{OriginalPID}の駒{this.pieceData.pieceName}の魅惑が解けました");
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
                    ModifyAP(pieceData.aPRecoveryRate * Time.deltaTime);//ターン数へ移行
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
            Destroy(gameObject);
            //StartCoroutine(DeathAnimation());//死亡アニメーションも一応
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