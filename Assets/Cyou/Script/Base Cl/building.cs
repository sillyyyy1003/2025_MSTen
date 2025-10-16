using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using GameData;
using GamePieces;

namespace Buildings
{
    /// <summary>
    /// 建物の実行時インスタンス
    /// ScriptableObjectのデータを参照し、状態管理に専念
    /// </summary>
    public class Building : VisualGameObject
    {
        [SerializeField] private BuildingDataSO buildingData;

        // ===== 実行時の状態管理 =====
        private BuildingState currentState;
        private int currentHp;
        private int remainingBuildCost;
        private int currentSkillUses;//現時点まだ使用可能のスロットの数
        private int lastResourceGenTurn;
        private int apCostperTurn;
        private int upgradeLevel = 0; // 0:初期、1:升級1、2:升級2

        // 配置された農民のリスト
        private List<FarmerSlot> farmerSlots = new List<FarmerSlot>();

        // ===== イベント =====
        public event Action<Building> OnBuildingCompleted;
        public event Action<Building> OnBuildingDestroyed;
        public event Action<int> OnResourceGenerated;

        // ===== プロパティ =====
        public BuildingDataSO Data => buildingData;
        public BuildingState State => currentState;
        public bool IsAlive => currentState != BuildingState.Ruined;
        public bool IsOperational => (currentState == BuildingState.Disabled || currentState == BuildingState.Active) && currentSkillUses > 0;
        public int CurrentHP => currentHp;
        public float BuildProgress => buildingData.buildingAPCost > 0 ?
            1f - (float)remainingBuildCost / buildingData.buildingAPCost : 1f;
        public int RemainingBuildCost => remainingBuildCost;
        public List<FarmerSlot> FarmerSlots => farmerSlots;
        public int APCostPerTurn => apCostperTurn;
        public int UpgradeLevel => upgradeLevel;

        #region 初期化

        public void Initialize(BuildingDataSO data)
        {
            buildingData = data;
            currentHp = data.maxHp;
            remainingBuildCost = data.buildingAPCost;
            apCostperTurn = data.apCostperTurn;
            currentSkillUses = data.GetMaxSlotsByLevel(0);

            ChangeState(BuildingState.UnderConstruction);

            // スロット初期化
            if (data.isSpecialBuilding)
            {
                int initialSlots = data.GetMaxSlotsByLevel(0);
                for (int i = 0; i < initialSlots; i++)
                {
                    farmerSlots.Add(new FarmerSlot());
                }
            }

            SetupComponents();
        }

        protected virtual void SetupComponents()
        {
            SetupVisualComponents();
            ApplySprite(buildingData.buildingSprite, Color.white);
            ApplyMesh(buildingData.buildingMesh, buildingData.buildingMaterial);
        }

        #endregion

        #region 建築処理

        /// <summary>
        /// 建築を進める
        /// </summary>
        public bool ProgressConstruction(int AP)
        {
            if (currentState != BuildingState.UnderConstruction)
                return false;

            remainingBuildCost -= AP;

            if (remainingBuildCost <= 0)
            {
                CompleteConstruction();
                return true;
            }

            return false;
        }

        private void CompleteConstruction()
        {
            ChangeState(BuildingState.Disabled);//未稼働状態へ移行
            OnBuildingCompleted?.Invoke(this);
        }

        #endregion

        #region 状態管理
        public void ChangeState(BuildingState state)
        {
            currentState = state;
        }

        #endregion

        #region 農民配置

        /// <summary>
        /// 農民を建物に配置
        /// </summary>
        public bool AssignFarmer(Farmer farmer)
        {
            if (!buildingData.isSpecialBuilding || (currentState != BuildingState.Disabled&&currentState!=BuildingState.Active))
            {
                Debug.Log("建物が未完成又は廃墟になってます。");
                return false;
            }
                
            var emptySlot = farmerSlots.Find(s => !s.IsOccupied);
            if (emptySlot == null)
                return false;

            emptySlot.AssignFarmer(farmer);
            return true;
        }

        #endregion

        #region 資源生成

        /// <summary>
        /// ターン処理で資源を生成
        /// </summary>
        public void ProcessTurn(int currentTurn)
        {
            if (!IsOperational)
                return;

            // APがある農民がいるかチェック
            bool hasActiveFarmer = farmerSlots.Any(slot => slot.IsOccupied && slot.HasAP);
            if (!hasActiveFarmer)
                return;

            // APがある農民がいる場合のみActiveに変更
            if (currentState == BuildingState.Disabled)
                ChangeState(BuildingState.Active);

            if (currentTurn - lastResourceGenTurn >= buildingData.resourceGenInterval)
            {
                GenerateResources();
                lastResourceGenTurn = currentTurn;

                // 農民の行動力を消費
                ProcessFarmerAP();
            }
        }

        private void GenerateResources()
        {
            int totalProduction = CalculateProduction();
            OnResourceGenerated?.Invoke(totalProduction);
        }

        private int CalculateProduction()
        {
            float totalProduction = 0f;
            foreach (var slot in farmerSlots)
            {
                if (slot.IsOccupied && slot.HasAP)
                {
                    // 基本生産量にスキルレベルによる倍率を適用
                    totalProduction += buildingData.baseProductionAmount;
                }
            }

            return Mathf.RoundToInt(totalProduction * buildingData.productionMultiplier);
        }

        private void ProcessFarmerAP()
        {
            foreach (var slot in farmerSlots)
            {
                if (slot.IsOccupied)
                {
                    slot.ConsumeActionPoint(apCostperTurn);//毎ターン在籍中農民の行動力消費量
                    if (!slot.HasAP)
                    {
                        // 行動力が尽きた農民を除去
                        slot.RemoveFarmer();
                        currentSkillUses--;

                        if (currentSkillUses <= 0)
                        {
                            ConvertToRuin();
                            return; // 廃墟になった場合は処理終了
                        }
                    }
                }
            }

            // AP消費後、稼働中の農民がいるかチェック
            CheckAndUpdateBuildingState();
        }

        /// <summary>
        /// 建物の状態を確認し、必要に応じて更新
        /// </summary>
        private void CheckAndUpdateBuildingState()
        {
            if (currentState == BuildingState.Active)
            {
                // Active状態で稼働中農民がいない場合はDisabledに戻す
                bool hasActiveFarmer = farmerSlots.Any(slot => slot.IsOccupied && slot.HasAP);
                
                if (!hasActiveFarmer)
                {
                    ChangeState(BuildingState.Disabled);
                    Debug.Log($"建物 {buildingData.buildingName} の農民のAPが尽きたため、Disabledに状態変更しました");
                }
            }
        }

        #endregion

        #region ダメージ処理

        public void TakeDamage(int damage)
        {
            currentHp -= damage;
            if (currentHp <= 0)
            {
                ConvertToRuin();
            }
        }

        private void ConvertToRuin()
        {
            ChangeState(BuildingState.Ruined);
            OnBuildingDestroyed?.Invoke(this);

            // すべての農民を解放
            foreach (var slot in farmerSlots)
            {
                slot.RemoveFarmer();
            }
        }

        #endregion

        #region アップグレード管理

        /// <summary>
        /// 建物をアップグレードする
        /// </summary>
        public bool UpgradeBuilding()
        {
            if (upgradeLevel >= 2)
            {
                Debug.LogWarning($"{buildingData.buildingName} は既に最大レベルです");
                return false;
            }

            if (currentState != BuildingState.Disabled && currentState != BuildingState.Active)
            {
                Debug.LogWarning("建物が未完成または廃墟です。アップグレードできません");
                return false;
            }

            upgradeLevel++;
            ApplyUpgradeEffects();
            Debug.Log($"{buildingData.buildingName} がレベル {upgradeLevel} にアップグレードしました");
            return true;
        }

        /// <summary>
        /// アップグレード効果を適用
        /// </summary>
        private void ApplyUpgradeEffects()
        {
            // レベルに応じた最大HPを更新
            int newMaxHp = buildingData.GetMaxHpByLevel(upgradeLevel);
            float hpRatio = (float)currentHp / buildingData.maxHp;
            currentHp = Mathf.RoundToInt(newMaxHp * hpRatio);

            // レベルに応じた最大スロット数を更新
            int newMaxSlots = buildingData.GetMaxSlotsByLevel(upgradeLevel);
            if (newMaxSlots > farmerSlots.Count)
            {
                // スロット数を増やす
                int slotsToAdd = newMaxSlots - farmerSlots.Count;
                for (int i = 0; i < slotsToAdd; i++)
                {
                    farmerSlots.Add(new FarmerSlot());
                }
                currentSkillUses = newMaxSlots;
            }

            Debug.Log($"建物のアップグレード効果適用: レベル{upgradeLevel} HP={newMaxHp}, スロット数={newMaxSlots}");

            // 新しい機能のログ
            if (upgradeLevel == 1)
            {
                int attackRange = buildingData.GetAttackRangeByLevel(upgradeLevel);
                if (attackRange > 0)
                {
                    Debug.Log($"攻撃機能獲得: 攻撃範囲={attackRange}");
                }
            }
            else if (upgradeLevel == 2)
            {
                int attackRange = buildingData.GetAttackRangeByLevel(upgradeLevel);
                Debug.Log($"強化攻撃機能: 攻撃範囲={attackRange}");
            }
        }

        /// <summary>
        /// レベルに応じた攻撃範囲を取得
        /// </summary>
        public int GetAttackRange()
        {
            return buildingData.GetAttackRangeByLevel(upgradeLevel);
        }

        /// <summary>
        /// 攻撃可能かどうか
        /// </summary>
        public bool CanAttack()
        {
            return GetAttackRange() > 0 && IsOperational;
        }

        #endregion

        /// <summary>
        /// 農民スロット管理クラス
        /// </summary>
        [Serializable]
        public class FarmerSlot
        {
            private Farmer assignedFarmer;

            public bool IsOccupied => assignedFarmer != null;
            public bool HasAP => assignedFarmer != null && assignedFarmer.CurrentAP > 0;

            public void AssignFarmer(Farmer farmer)
            {
                assignedFarmer = farmer;
            }

            public void ConsumeActionPoint(int cost)
            {
                if (assignedFarmer != null && assignedFarmer.CurrentAP > 0)
                {
                    assignedFarmer.ConsumeAP(cost);
                }
            }

            public void RemoveFarmer()
            {
                if (assignedFarmer != null)
                {
                    assignedFarmer.OnExitBuilding();
                    assignedFarmer = null;
                }
            }
        }
    }

    /// <summary>
    /// 建物の状態
    /// </summary>
    public enum BuildingState
    {
        UnderConstruction,  // 建築中
        Active,            // 稼働中
        Disabled,          // 未稼働（効果を発揮してない）
        Ruined            // 廃墟
    }

}
   