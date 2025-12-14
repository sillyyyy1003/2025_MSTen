using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using GameData;
using GameData.UI;
using GamePieces;

namespace Buildings
{
    /// <summary>
    /// 建物の実行時インスタンス
    /// ScriptableObjectのデータを参照し、状態管理に専念
    /// </summary>
    public class Building : MonoBehaviour
    {
        [SerializeField] private BuildingDataSO buildingData;

        // ===== 実行時の状態管理 =====
        private int buildingID = -1; // 建物の一意なID（BuildingManagerが設定）
        private int playerID = -1; // 建物の所有者プレイヤーID
        private BuildingState currentState;
        private int currentHp;
        private int remainingBuildCost;
        private int currentSkillUses;//現時点まだ使用可能のスロットの数
        private int lastResourceGenTurn;

        // 25.11.26 RI add now slot count and cant use slot
        // ===== 各項目の個別レベル =====
        private int hpLevel = 0;            // HP レベル (0-2)
        private int slotsLevel = 0;         // スロット数レベル (0-2)

        // 配置された農民のリスト
        private List<FarmerSlot> farmerSlots = new List<FarmerSlot>();
        private List<NewFarmerSlot> newFarmerSlots = new List<NewFarmerSlot>();

        // 25.11.26 RI add slots
        private int slots =0;
        private bool isGoldMine;

        // ===== イベント =====
        public event Action<Building> OnBuildingCompleted;
        public event Action<Building> OnBuildingDestroyed;
        public event Action<int> OnResourceGenerated;

        // ===== プロパティ =====
        public int BuildingID => buildingID;
        public int PlayerID => playerID;
        public BuildingDataSO Data => buildingData;
        public BuildingState State => currentState;
        public bool IsAlive => currentState != BuildingState.Ruined;
        public bool IsOperational => (currentState == BuildingState.Inactive || currentState == BuildingState.Active) && currentSkillUses > 0;
        public int CurrentHP => currentHp;
        public float BuildProgress => buildingData.buildingResourceCost > 0 ?
            1f - (float)remainingBuildCost / buildingData.buildingResourceCost : 1f;
        public int RemainingBuildCost => remainingBuildCost;
        public List<FarmerSlot> FarmerSlots => farmerSlots;

        //25.11.28 Ri add new FarmerSlots
        public List<NewFarmerSlot> NewFarmerSlots => newFarmerSlots;

        public int HPLevel => hpLevel;
        public int SlotsLevel => slotsLevel;

        /// <summary>
        /// 建物IDを設定（BuildingManagerのみが呼び出し）
        /// </summary>
        public void SetBuildingID(int id)
        {
            buildingID = id;
        }

        #region 初期化

        public virtual void Initialize(BuildingDataSO data, int ownerPlayerID)
        {
            buildingData = data;
            playerID = ownerPlayerID;

            // ローカルプレイヤーの建物の場合、SkillTreeUIManagerからレベルを取得
            if (ownerPlayerID == PieceManager.Instance.GetLocalPlayerID())
            {
                hpLevel = SkillTreeUIManager.Instance.GetCurrentLevel(PieceType.Building, TechTree.HP);
                slotsLevel = SkillTreeUIManager.Instance.GetCurrentLevel(PieceType.Building, TechTree.AltarCount);
            }
            else
            {
                // 敵プレイヤーの建物はデフォルトレベル0
                hpLevel = 0;
                slotsLevel = 0;
            }

            currentHp = data.maxHpByLevel[hpLevel];
            remainingBuildCost = data.buildingResourceCost;
            currentSkillUses = data.GetMaxSlotsByLevel(slotsLevel);
            slots = data.GetMaxSlotsByLevel(slotsLevel);
            //地面に金鉱があるか否かを判断すべき

            // 25.11.10 RI 直接生成完了に変更
            ChangeState(BuildingState.Active);

            // スロット初期化
            if (data.isSpecialBuilding)
            {
                int initialSlots = data.GetMaxSlotsByLevel(slotsLevel);
                for (int i = 0; i < initialSlots; i++)
                {
                    farmerSlots.Add(new FarmerSlot());
                    // RT 25.11.28 AddNewSlot
                    newFarmerSlots.Add(new NewFarmerSlot());
                }
            }

            SetupComponents();
        }

        protected virtual void SetupComponents()
        {
            // Prefabの外見をそのまま使用するため、動的な適用は不要
        }

        #endregion

        #region 建築処理

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
        public int ProcessTurn()
        {
            //25.11.26 RI change logic

            if (!IsOperational)
            {
                Debug.Log($"建物 {buildingData.buildingName} は稼働可能な状態ではありません (状態: {currentState}, スロット: {currentSkillUses})");
                return 0;
            }

            // APがある農民がいるかチェック
            bool hasActiveFarmer = farmerSlots.Any(slot => slot.IsOccupied && slot.HasAP);
            if (!hasActiveFarmer)
            {
                Debug.Log($"建物 {buildingData.buildingName} にAPを持つ農民がいません");
                return 0;
            }

            // APがある農民がいる場合のみActiveに変更
            if (currentState == BuildingState.Inactive)
            {
                ChangeState(BuildingState.Active);
                Debug.Log($"建物 {buildingData.buildingName} がActiveに変更されました");
            }

            // 資源生成間隔チェック(廃止済み)
            //int turnsSinceLastGen = currentTurn - lastResourceGenTurn;
            //if (turnsSinceLastGen >= buildingData.resourceGenInterval)


            int generatedResources = GenerateResources();
                
                // 農民の行動力を消費
                ProcessFarmerAP();
            
                Debug.Log($"建物 {buildingData.buildingName} はターン)");

            return generatedResources;
        }
        public void SetIsOnGoldmine(bool isOn)
        {
            isGoldMine = isOn;
        }
        private int GenerateResources()
        {
            int totalProduction = CalculateProduction();
            Debug.Log($"建物 {buildingData.buildingName} が資源 {totalProduction} を生成しました！");
            OnResourceGenerated?.Invoke(totalProduction);
            return totalProduction;
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
                    // 行動力チェックは他のシステムで管理されるため削除
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


        // 25.11.26 RI Add Get Building All HP
        public int  GetAllHP()
        {
            return buildingData.GetMaxHpByLevel(hpLevel);
        }

        // 25.11.26 RI Add Get Building Slots
        public int GetSlots()
        {
            return NewFarmerSlots.Count;
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

        //25.11.26 RI add new FarmerEnter
        public bool FarmerEnter(int id,int ap)
        {
            Debug.Log("empty slot count is "+newFarmerSlots.Count+" farmer ap is "+ap);
            bool hasEmptySlot = false; ;
            for (int i=0;i<newFarmerSlots.Count;i++)
            {
                if (newFarmerSlots[i].canInSlot)
                {
                    hasEmptySlot = true;
                    newFarmerSlots[i].farmerAP = ap;
                    newFarmerSlots[i].canInSlot = false;
                    Debug.Log("this slot is " + i + " this slot ap is " + newFarmerSlots[i].farmerAP + " can In is " + newFarmerSlots[i].canInSlot);

                    // 25.12.9 RI スロットのUI効果を追加
                    UnitStatusUIManager.Instance.ActivateSlotByID(id,i);
                    break;
                }
            }
          
            return hasEmptySlot;
        }
        //25.11.26 RI add New Get Resources
        public int NewGetResource(int id)
        {
            int res=0;
            for (int i = 0; i < newFarmerSlots.Count; i++)
            {
                if (!newFarmerSlots[i].canInSlot&& newFarmerSlots[i].isActived)
                {
                    if(isGoldMine)
                        res += 10;
                    else
                        res += 5;
                    newFarmerSlots[i].farmerAP -= 1;
                    if (newFarmerSlots[i].farmerAP == 0)
                    {
                        newFarmerSlots[i].isActived = false;
                        // 25.12.9 RI スロットのUI効果を追加
                        UnitStatusUIManager.Instance.RemoveSlotFromUnit(id,i);
                        Debug.Log("this slot is unActived! " + i);
                    }
                }
            }
         

            return res;
        }
        public bool GetBuildingActived()
        {
            bool actived = false;
            foreach (var a in newFarmerSlots)
            {
                if (a.isActived)
                {
                    actived = true;
                    break;
                }
            }

            return actived;
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

            // 25.11.26 RI add slots
            if (newMaxSlots > newFarmerSlots.Count)
            {
                int slotsToAdd = newMaxSlots - newFarmerSlots.Count;
                for (int i = 0; i < slotsToAdd; i++)
                {
                    newFarmerSlots.Add(new NewFarmerSlot());
                }
                currentSkillUses += slotsToAdd;
            }

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
        /// 指定項目のアップグレードコストを取得
        /// </summary>
        public int GetUpgradeCost(int level, BuildingUpgradeType type)
        {
            switch (type)
            {
                case BuildingUpgradeType.BuildingHP:
                    if (level >= 2 || buildingData.hpUpgradeCost == null || level >= buildingData.hpUpgradeCost.Length)
                        return -1;
                    return buildingData.hpUpgradeCost[level];

                case BuildingUpgradeType.slotsLevel:
                    if (level >= 2 || buildingData.slotsUpgradeCost == null || level >= buildingData.slotsUpgradeCost.Length)
                        return -1;
                    return buildingData.slotsUpgradeCost[level];

                default:
                    return -1;
            }
        }

        /// <summary>
        /// 指定項目がアップグレード可能かチェック
        /// </summary>
        public bool CanUpgrade(int level, BuildingUpgradeType type)
        {
            if (currentState != BuildingState.Inactive && currentState != BuildingState.Active)
                return false;

            int cost = GetUpgradeCost(level, type);
            return cost > 0;
        }

        #endregion

        #region ネットワーク同期用セッター

        /// <summary>
        /// HPを直接設定（ネットワーク同期用）
        /// </summary>
        public void SetHP(int hp)
        {
            currentHp = Mathf.Clamp(hp, 0, buildingData.GetMaxHpByLevel(hpLevel));
        }

        /// <summary>
        /// HPレベルを直接設定（ネットワーク同期用）
        /// </summary>
        public void SetHPLevel(int level)
        {
            hpLevel = Mathf.Clamp(level, 0, 2);
            // HPレベルに応じて最大HPを更新（現在HPは変更しない）
        }

        /// <summary>
        /// スロット数レベルを直接設定（ネットワーク同期用）
        /// </summary>
        public void SetSlotsLevel(int level)
        {
            slotsLevel = Mathf.Clamp(level, 0, 2);

            // スロット数を更新
            int newMaxSlots = buildingData.GetMaxSlotsByLevel(slotsLevel);
            if (newMaxSlots > farmerSlots.Count)
            {
                int slotsToAdd = newMaxSlots - farmerSlots.Count;
                for (int i = 0; i < slotsToAdd; i++)
                {
                    farmerSlots.Add(new FarmerSlot());
                }
            }
            //25.11.28 RI add new FarmerSlots
            if (newMaxSlots > newFarmerSlots.Count)
            {
                int slotsToAdd = newMaxSlots - newFarmerSlots.Count;
                for (int i = 0; i < slotsToAdd; i++)
                {
                    newFarmerSlots.Add(new NewFarmerSlot());
                }
            }
        }


        /// <summary>
        /// 残り建築コストを直接設定（ネットワーク同期用）
        /// </summary>
        public void SetRemainingBuildCost(int cost)
        {
            remainingBuildCost = Mathf.Max(0, cost);
        }

        /// <summary>
        /// 建物の状態を直接設定（ネットワーク同期用）
        /// </summary>
        public void SetState(BuildingState state)
        {
            currentState = state;
        }

        #endregion

        /// <summary>
        /// 25.11.28 RI add new FarmerSlot
        /// 農民スロット管理クラス
        /// </summary>
        [Serializable]
        public class NewFarmerSlot
        {
            // excess AP
            public int farmerAP;

            // Farmer can in
            public bool canInSlot=true;
            // is actived
            public bool isActived=true;


        }

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


}
   