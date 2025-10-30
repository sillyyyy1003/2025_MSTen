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
        private int upgradeLevel = 0; // 0:初期、1:升級1、2:升級2（全体レベル・互換性のため残す）

        // ===== 各項目の個別レベル =====
        private int hpLevel = 0;            // HP レベル (0-2)
        private int attackRangeLevel = 0;   // 攻撃範囲レベル (0-2)
        private int slotsLevel = 0;         // スロット数レベル (0-2)
        private int buildCostLevel = 0;     // 建築コストレベル (0-2)

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
        public bool IsOperational => (currentState == BuildingState.Inactive || currentState == BuildingState.Active) && currentSkillUses > 0;
        public int CurrentHP => currentHp;
        public float BuildProgress => buildingData.buildingAPCost > 0 ?
            1f - (float)remainingBuildCost / buildingData.buildingAPCost : 1f;
        public int RemainingBuildCost => remainingBuildCost;
        public List<FarmerSlot> FarmerSlots => farmerSlots;
        public int APCostPerTurn => apCostperTurn;
        public int UpgradeLevel => upgradeLevel;
        public int HPLevel => hpLevel;
        public int AttackRangeLevel => attackRangeLevel;
        public int SlotsLevel => slotsLevel;
        public int BuildCostLevel => buildCostLevel;

        #region 初期化

        public void Initialize(BuildingDataSO data)
        {
            buildingData = data;
            currentHp = data.maxHp;
            remainingBuildCost = data.buildingAPCost;
            apCostperTurn = data.apCostperTurn;
            currentSkillUses = data.GetMaxSlotsByLevel(0);
            //地面に金鉱があるか否かを判断すべき


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
            // SetupVisualComponents(); // Prefabの外見をそのまま使用するため不要
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
            ChangeState(BuildingState.Inactive);//未稼働状態へ移行
            OnBuildingCompleted?.Invoke(this);
        }

        /// <summary>
        /// 建築をキャンセルする
        /// 注意: 消耗された農民と行動力は返されない
        /// </summary>
        public bool CancelConstruction()
        {
            if (currentState != BuildingState.UnderConstruction)
            {
                Debug.LogWarning("建築中でない建物はキャンセルできません");
                return false;
            }

            Debug.Log($"建物 {buildingData.buildingName} の建築がキャンセルされました。消耗された農民と行動力は返されません。");

            // 建物を廃墟状態に変更
            ChangeState(BuildingState.Ruined);

            // 破壊イベントを発火（DemoUITestのリストから自動削除される）
            OnBuildingDestroyed?.Invoke(this);

            // 建物オブジェクトを破棄
            Destroy(gameObject);

            return true;
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
            if (!buildingData.isSpecialBuilding || (currentState != BuildingState.Inactive&&currentState!=BuildingState.Active))
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
            {
                Debug.Log($"建物 {buildingData.buildingName} は稼働可能な状態ではありません (状態: {currentState}, スロット: {currentSkillUses})");
                return;
            }

            // APがある農民がいるかチェック
            bool hasActiveFarmer = farmerSlots.Any(slot => slot.IsOccupied && slot.HasAP);
            if (!hasActiveFarmer)
            {
                Debug.Log($"建物 {buildingData.buildingName} にAPを持つ農民がいません");
                return;
            }

            // APがある農民がいる場合のみActiveに変更
            if (currentState == BuildingState.Inactive)
            {
                ChangeState(BuildingState.Active);
                Debug.Log($"建物 {buildingData.buildingName} がActiveに変更されました");
            }

            // 資源生成間隔チェック
            int turnsSinceLastGen = currentTurn - lastResourceGenTurn;
            if (turnsSinceLastGen >= buildingData.resourceGenInterval)
            {
                GenerateResources();
                lastResourceGenTurn = currentTurn;

                // 農民の行動力を消費
                ProcessFarmerAP();
            }
            else
            {
                Debug.Log($"建物 {buildingData.buildingName} は資源生成間隔待ち中 ({turnsSinceLastGen}/{buildingData.resourceGenInterval}ターン)");
            }
        }

        private void GenerateResources()
        {
            int totalProduction = CalculateProduction();
            Debug.Log($"建物 {buildingData.buildingName} が資源 {totalProduction} を生成しました！");
            OnResourceGenerated?.Invoke(totalProduction);
        }

        private int CalculateProduction()
        {
            float totalProduction = 0f;
            foreach (var slot in farmerSlots)
            {
                if (slot.IsOccupied && slot.HasAP)
                {
                    int baseProduction = Data.GetBuildingResourceProduction();
                    // 基本生産量にスキルレベルによる倍率を適用
                    totalProduction += baseProduction;
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
                // Active状態で稼働中農民がいない場合はInactiveに戻す
                bool hasActiveFarmer = farmerSlots.Any(slot => slot.IsOccupied && slot.HasAP);
                
                if (!hasActiveFarmer)
                {
                    ChangeState(BuildingState.Inactive);
                    Debug.Log($"建物 {buildingData.buildingName} の農民のAPが尽きたため、Inactiveに状態変更しました");
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
        /// 建物をアップグレードする（旧システム・互換性のため残す）
        /// </summary>
        public bool UpgradeBuilding()
        {
            if (upgradeLevel >= 2)
            {
                Debug.LogWarning($"{buildingData.buildingName} は既に最大レベルです");
                return false;
            }

            if (currentState != BuildingState.Inactive && currentState != BuildingState.Active)
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
        /// HPをアップグレードする
        /// </summary>
        /// <returns>アップグレード成功したらtrue</returns>
        public bool UpgradeHP()
        {
            // 建物状態チェック
            if (currentState != BuildingState.Inactive && currentState != BuildingState.Active)
            {
                Debug.LogWarning("建物が未完成または廃墟です。アップグレードできません");
                return false;
            }

            // 最大レベルチェック
            if (hpLevel >= 2)
            {
                Debug.LogWarning($"{buildingData.buildingName} のHPは既に最大レベル(2)です");
                return false;
            }

            // アップグレードコスト配列の境界チェック
            if (buildingData.hpUpgradeCost == null || hpLevel >= buildingData.hpUpgradeCost.Length)
            {
                Debug.LogError($"{buildingData.buildingName} のhpUpgradeCostが正しく設定されていません");
                return false;
            }

            int cost = buildingData.hpUpgradeCost[hpLevel];

            // コストが0の場合はアップグレード不可
            if (cost <= 0)
            {
                Debug.LogWarning($"{buildingData.buildingName} のHPレベル{hpLevel}→{hpLevel + 1}へのアップグレードは設定されていません（コスト0）");
                return false;
            }

            // レベルアップ実行
            hpLevel++;
            int newMaxHp = buildingData.GetMaxHpByLevel(hpLevel);
            float hpRatio = (float)currentHp / buildingData.GetMaxHpByLevel(hpLevel - 1);
            currentHp = Mathf.RoundToInt(newMaxHp * hpRatio);

            Debug.Log($"{buildingData.buildingName} のHPがレベル{hpLevel}にアップグレードしました（最大HP: {newMaxHp}）");
            return true;
        }

        /// <summary>
        /// 攻撃範囲をアップグレードする
        /// </summary>
        /// <returns>アップグレード成功したらtrue</returns>
        public bool UpgradeAttackRange()
        {
            // 建物状態チェック
            if (currentState != BuildingState.Inactive && currentState != BuildingState.Active)
            {
                Debug.LogWarning("建物が未完成または廃墟です。アップグレードできません");
                return false;
            }

            // 最大レベルチェック
            if (attackRangeLevel >= 2)
            {
                Debug.LogWarning($"{buildingData.buildingName} の攻撃範囲は既に最大レベル(2)です");
                return false;
            }

            // アップグレードコスト配列の境界チェック
            if (buildingData.attackRangeUpgradeCost == null || attackRangeLevel >= buildingData.attackRangeUpgradeCost.Length)
            {
                Debug.LogError($"{buildingData.buildingName} のattackRangeUpgradeCostが正しく設定されていません");
                return false;
            }

            int cost = buildingData.attackRangeUpgradeCost[attackRangeLevel];

            // コストが0の場合はアップグレード不可
            if (cost <= 0)
            {
                Debug.LogWarning($"{buildingData.buildingName} の攻撃範囲レベル{attackRangeLevel}→{attackRangeLevel + 1}へのアップグレードは設定されていません（コスト0）");
                return false;
            }

            // レベルアップ実行
            attackRangeLevel++;
            int newAttackRange = buildingData.GetAttackRangeByLevel(attackRangeLevel);

            Debug.Log($"{buildingData.buildingName} の攻撃範囲がレベル{attackRangeLevel}にアップグレードしました（攻撃範囲: {newAttackRange}）");
            return true;
        }

        /// <summary>
        /// スロット数をアップグレードする
        /// </summary>
        /// <returns>アップグレード成功したらtrue</returns>
        public bool UpgradeSlots()
        {
            // 建物状態チェック
            if (currentState != BuildingState.Inactive && currentState != BuildingState.Active)
            {
                Debug.LogWarning("建物が未完成または廃墟です。アップグレードできません");
                return false;
            }

            // 最大レベルチェック
            if (slotsLevel >= 2)
            {
                Debug.LogWarning($"{buildingData.buildingName} のスロット数は既に最大レベル(2)です");
                return false;
            }

            // アップグレードコスト配列の境界チェック
            if (buildingData.slotsUpgradeCost == null || slotsLevel >= buildingData.slotsUpgradeCost.Length)
            {
                Debug.LogError($"{buildingData.buildingName} のslotsUpgradeCostが正しく設定されていません");
                return false;
            }

            int cost = buildingData.slotsUpgradeCost[slotsLevel];

            // コストが0の場合はアップグレード不可
            if (cost <= 0)
            {
                Debug.LogWarning($"{buildingData.buildingName} のスロット数レベル{slotsLevel}→{slotsLevel + 1}へのアップグレードは設定されていません（コスト0）");
                return false;
            }

            // レベルアップ実行
            slotsLevel++;
            int newMaxSlots = buildingData.GetMaxSlotsByLevel(slotsLevel);

            // スロット数を増やす
            if (newMaxSlots > farmerSlots.Count)
            {
                int slotsToAdd = newMaxSlots - farmerSlots.Count;
                for (int i = 0; i < slotsToAdd; i++)
                {
                    farmerSlots.Add(new FarmerSlot());
                }
                currentSkillUses += slotsToAdd;
            }

            Debug.Log($"{buildingData.buildingName} のスロット数がレベル{slotsLevel}にアップグレードしました（スロット数: {newMaxSlots}）");
            return true;
        }

        /// <summary>
        /// 建築コストをアップグレードする（建築に必要なAPを減少させる）
        /// </summary>
        /// <returns>アップグレード成功したらtrue</returns>
        public bool UpgradeBuildCost()
        {
            // 建物状態チェック
            if (currentState != BuildingState.Inactive && currentState != BuildingState.Active)
            {
                Debug.LogWarning("建物が未完成または廃墟です。アップグレードできません");
                return false;
            }

            // 最大レベルチェック
            if (buildCostLevel >= 2)
            {
                Debug.LogWarning($"{buildingData.buildingName} の建築コストは既に最大レベル(2)です");
                return false;
            }

            // アップグレードコスト配列の境界チェック
            if (buildingData.buildCostUpgradeCost == null || buildCostLevel >= buildingData.buildCostUpgradeCost.Length)
            {
                Debug.LogError($"{buildingData.buildingName} のbuildCostUpgradeCostが正しく設定されていません");
                return false;
            }

            int cost = buildingData.buildCostUpgradeCost[buildCostLevel];

            // コストが0の場合はアップグレード不可
            if (cost <= 0)
            {
                Debug.LogWarning($"{buildingData.buildingName} の建築コストレベル{buildCostLevel}→{buildCostLevel + 1}へのアップグレードは設定されていません（コスト0）");
                return false;
            }

            // レベルアップ実行
            buildCostLevel++;
            int newBuildCost = buildingData.GetBuildingAPCostByLevel(buildCostLevel);

            Debug.Log($"{buildingData.buildingName} の建築コストがレベル{buildCostLevel}にアップグレードしました（建築AP: {newBuildCost}）");
            return true;
        }

        /// <summary>
        /// 指定項目のアップグレードコストを取得
        /// </summary>
        public int GetUpgradeCost(BuildingUpgradeType type)
        {
            switch (type)
            {
                case BuildingUpgradeType.HP:
                    if (hpLevel >= 2 || buildingData.hpUpgradeCost == null || hpLevel >= buildingData.hpUpgradeCost.Length)
                        return -1;
                    return buildingData.hpUpgradeCost[hpLevel];

                case BuildingUpgradeType.AttackRange:
                    if (attackRangeLevel >= 2 || buildingData.attackRangeUpgradeCost == null || attackRangeLevel >= buildingData.attackRangeUpgradeCost.Length)
                        return -1;
                    return buildingData.attackRangeUpgradeCost[attackRangeLevel];

                case BuildingUpgradeType.Slots:
                    if (slotsLevel >= 2 || buildingData.slotsUpgradeCost == null || slotsLevel >= buildingData.slotsUpgradeCost.Length)
                        return -1;
                    return buildingData.slotsUpgradeCost[slotsLevel];

                case BuildingUpgradeType.BuildCost:
                    if (buildCostLevel >= 2 || buildingData.buildCostUpgradeCost == null || buildCostLevel >= buildingData.buildCostUpgradeCost.Length)
                        return -1;
                    return buildingData.buildCostUpgradeCost[buildCostLevel];

                default:
                    return -1;
            }
        }

        /// <summary>
        /// 指定項目がアップグレード可能かチェック
        /// </summary>
        public bool CanUpgrade(BuildingUpgradeType type)
        {
            if (currentState != BuildingState.Inactive && currentState != BuildingState.Active)
                return false;

            int cost = GetUpgradeCost(type);
            return cost > 0;
        }

        /// <summary>
        /// レベルに応じた攻撃範囲を取得
        /// </summary>
        public int GetAttackRange()
        {
            return buildingData.GetAttackRangeByLevel(attackRangeLevel);
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
        Inactive,          // 未稼働（効果を発揮してない）
        Ruined            // 廃墟
    }

    /// <summary>
    /// 建物のアップグレード項目タイプ
    /// </summary>
    public enum BuildingUpgradeType
    {
        HP,             // 最大HP
        AttackRange,    // 攻撃範囲
        Slots,          // スロット数
        BuildCost       // 建築コスト
    }

}
   