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
        protected int pieceID = -1; // 駒の一意なID（PieceManagerが設定）
        protected int currentMaxHP;
        protected int currentHP;
        protected int currentMaxAP;
        protected int currentAP;
        protected int currentPID; // 現在所属しているプレイヤーID
        protected int originalPID; // 元々所属していたプレイヤーID
        protected PieceState currentState = PieceState.Idle;
        protected int upgradeLevel = 0; // 0:初期、1:升級1、2:升級2、3:升級3（全体レベル・互換性のため残す）

        // ===== 各項目の個別レベル =====
        protected int hpLevel = 0; // HP レベル (0-3)
        protected int apLevel = 0; // AP レベル (0-3)


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

        public event Action<PieceState, PieceState> OnStateChanged;

        // ===== プロパティ =====
        public int PieceID => pieceID;
        public PieceDataSO Data => pieceData;
        public int CurrentPID => currentPID;
        public int OriginalPID => originalPID;
        public int CurrentHP => currentHP;
        public int CurrentMaxHP => currentMaxHP;
        public int CurrentAP => currentAP;
        public int CurrentMaxAP => currentMaxAP;
        public bool IsAlive => currentHP > 0;
        public bool CanMove => currentAP >= pieceData.moveAPCost;
        public PieceState State => currentState;
        public int UpgradeLevel => upgradeLevel;
        public int HPLevel => hpLevel;
        public int APLevel => apLevel;

        /// <summary>
        /// 駒IDを設定（PieceManagerのみが呼び出し）
        /// </summary>
        public void SetPieceID(int id)
        {
            pieceID = id;
        }
        
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

            //if(charmTurns>0)
            //    StartCoroutine(RevertCharmAfterTurns(charmTurns,OriginalPID));


        }


        private IEnumerator RevertCharmAfterTurns(int charmTurns,int originalPID)
        {
            int remainingTurns=charmTurns;
            //int endTurn = remainingTurns + DemoUITest.GetTurn();//魅惑が解除されるターンの数字

            while (remainingTurns > 0)
            {
                yield return new WaitUntil(() =>1 >= 0);//GameManagerからターンの終了宣告を貰う
                    remainingTurns--;
            }
            
            currentPID = originalPID;
            OnUncharmed?.Invoke(this);
            Debug.Log($"{OriginalPID}の駒{this.pieceData.pieceName}の魅惑が解けました");
        }

        protected virtual void SetupComponents()
        {
            // SetupVisualComponents(); // Prefabの外見をそのまま使用するため不要
            // Prefabの外見をそのまま使用するため、動的な適用は不要
        }

        #endregion

        #region 行動力管理

        protected IEnumerator ActionPointRecoveryCoroutine()
        {
            while (IsAlive)
            {
                if (currentAP < currentMaxAP && currentState != PieceState.InBuilding)
                {
                    //ModifyAP(pieceData.aPRecoveryRate * );//ターン数へ移行
                }
                yield return null;
            }
        }
        
        public bool ConsumeAP(int amount)
        {
            if (currentAP >= amount)
            {
                ModifyAP(-amount);
                return true;
            }
            return false;
        }
        
        protected void ModifyAP(int amount)
        {
            float oldValue = currentAP;
            currentAP = Math.Clamp(currentAP + amount, 0, currentMaxAP);

            if (!Mathf.Approximately(oldValue, currentAP))
            {
            }
        }

        /// <summary>
        /// APを回復（PieceManagerから呼び出し可能）
        /// </summary>
        public void RecoverAP(int amount)
        {
            ModifyAP(amount);
        }

        #endregion
        
        #region ダメージ処理
        
        public virtual void TakeDamage(int damage, Piece attacker = null)
        {
            int oldHP = currentHP;
            currentHP = currentHP - damage;
            
            
            if (currentHP <= 0 && oldHP > 0)
            {
                Die();
            }
        }
        
        public virtual void Heal(int amount)
        {
            float oldHP = currentHP;
            currentHP = Mathf.Min(currentMaxHP, currentHP + amount);
            
            if (!Mathf.Approximately(oldHP, currentHP))
            {
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
        /// 駒をアップグレードする（旧システム・互換性のため残す）
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

        /// <summary>
        /// HPをアップグレードする
        /// </summary>
        /// <returns>アップグレード成功したらtrue</returns>
        public bool UpgradeHP()
        {
            // 最大レベルチェック
            if (hpLevel >= 3)
            {
                Debug.LogWarning($"{pieceData.pieceName} のHPは既に最大レベル(3)です");
                return false;
            }

            // アップグレードコスト配列の境界チェック
            if (pieceData.hpUpgradeCost == null || hpLevel >= pieceData.hpUpgradeCost.Length)
            {
                Debug.LogError($"{pieceData.pieceName} のhpUpgradeCostが正しく設定されていません");
                return false;
            }

            int cost = pieceData.hpUpgradeCost[hpLevel];

            // コストが0の場合はアップグレード不可
            if (cost <= 0)
            {
                Debug.LogWarning($"{pieceData.pieceName} のHPレベル{hpLevel}→{hpLevel + 1}へのアップグレードは設定されていません（コスト0）");
                return false;
            }

            // レベルアップ実行
            hpLevel++;
            int newMaxHP = pieceData.GetMaxHPByLevel(hpLevel);
            int hpRatio = currentHP / currentMaxHP; // 現在のHP割合を保持
            currentMaxHP = newMaxHP;
            currentHP = newMaxHP * hpRatio; // 割合を維持してHPを再計算



            Debug.Log($"{pieceData.pieceName} のHPがレベル{hpLevel}にアップグレードしました（最大HP: {currentMaxHP}）");
            return true;
        }

        /// <summary>
        /// APをアップグレードする
        /// </summary>
        /// <returns>アップグレード成功したらtrue</returns>
        public bool UpgradeAP()
        {
            // 最大レベルチェック
            if (apLevel >= 3)
            {
                Debug.LogWarning($"{pieceData.pieceName} のAPは既に最大レベル(3)です");
                return false;
            }

            // アップグレードコスト配列の境界チェック
            if (pieceData.apUpgradeCost == null || apLevel >= pieceData.apUpgradeCost.Length)
            {
                Debug.LogError($"{pieceData.pieceName} のapUpgradeCostが正しく設定されていません");
                return false;
            }

            int cost = pieceData.apUpgradeCost[apLevel];

            // コストが0の場合はアップグレード不可
            if (cost <= 0)
            {
                Debug.LogWarning($"{pieceData.pieceName} のAPレベル{apLevel}→{apLevel + 1}へのアップグレードは設定されていません（コスト0）");
                return false;
            }

            // レベルアップ実行
            apLevel++;
            int newMaxAP = pieceData.GetMaxAPByLevel(apLevel);
            int apRatio = currentAP / currentMaxAP; // 現在のAP割合を保持
            currentMaxAP = newMaxAP;
            currentAP = newMaxAP * apRatio; // 割合を維持してAPを再計算

            Debug.Log($"{pieceData.pieceName} のAPがレベル{apLevel}にアップグレードしました（最大AP: {currentMaxAP}）");
            return true;
        }

        /// <summary>
        /// 指定項目のアップグレードコストを取得
        /// </summary>
        public int GetUpgradeCost(PieceUpgradeType type)
        {
            switch (type)
            {
                case PieceUpgradeType.HP:
                    if (hpLevel >= 3 || pieceData.hpUpgradeCost == null || hpLevel >= pieceData.hpUpgradeCost.Length)
                        return -1; // アップグレード不可
                    return pieceData.hpUpgradeCost[hpLevel];

                case PieceUpgradeType.AP:
                    if (apLevel >= 3 || pieceData.apUpgradeCost == null || apLevel >= pieceData.apUpgradeCost.Length)
                        return -1; // アップグレード不可
                    return pieceData.apUpgradeCost[apLevel];

                default:
                    return -1;
            }
        }

        /// <summary>
        /// 指定項目がアップグレード可能かチェック
        /// </summary>
        public bool CanUpgrade(PieceUpgradeType type)
        {
            int cost = GetUpgradeCost(type);
            return cost > 0;
        }

        #endregion

        #region セッター（同期用）

        /// <summary>
        /// HPを直接設定（ネットワーク同期用）
        /// </summary>
        public void SetHP(int hp)
        {
            currentHP = Mathf.Clamp(hp, 0, currentMaxHP);
        }

        /// <summary>
        /// HPレベルを直接設定（ネットワーク同期用）
        /// </summary>
        public void SetHPLevel(int level)
        {
            hpLevel = Mathf.Clamp(level, 0, 3);
            currentMaxHP = pieceData.GetMaxHPByLevel(hpLevel);
        }

        /// <summary>
        /// APレベルを直接設定（ネットワーク同期用）
        /// </summary>
        public void SetAPLevel(int level)
        {
            apLevel = Mathf.Clamp(level, 0, 3);
            currentMaxAP = pieceData.GetMaxAPByLevel(apLevel);
        }

        /// <summary>
        /// プレイヤーIDを直接設定（ネットワーク同期用）
        /// </summary>
        public void SetPlayerID(int playerID)
        {
            currentPID = playerID;
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

    /// <summary>
    /// アップグレード項目タイプ
    /// </summary>


}