using UnityEngine;
using System;
using System.Collections.Generic;
using GameData;
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
        private BuildingState currentState;
        private int currentHp;
        private int remainingBuildCost;
        private int currentSkillUses;
        private int lastResourceGenerationTurn;
        
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
        public bool IsOperational => currentState == BuildingState.Active && currentSkillUses > 0;
        public int CurrentHP => currentHp;
        public float BuildProgress => buildingData.buildActionCost > 0 ? 
            1f - (float)remainingBuildCost / buildingData.buildActionCost : 1f;
        
        #region 初期化
        
        public void Initialize(BuildingDataSO data)
        {
            buildingData = data;
            currentHp = data.maxHp;
            remainingBuildCost = data.buildActionCost;
            currentState = BuildingState.UnderConstruction;
            currentSkillUses = data.maxSkillSlots;
            
            // スロット初期化
            if (data.isSpecialBuilding)
            {
                for (int i = 0; i < data.maxSkillSlots; i++)
                {
                    farmerSlots.Add(new FarmerSlot());
                }
            }
        }
        
        #endregion
        
        #region 建築処理
        
        /// <summary>
        /// 建築を進める
        /// </summary>
        public bool ProgressConstruction(int actionPoints)
        {
            if (currentState != BuildingState.UnderConstruction)
                return false;
            
            remainingBuildCost -= actionPoints;
            
            if (remainingBuildCost <= 0)
            {
                CompleteConstruction();
                return true;
            }
            
            return false;
        }
        
        private void CompleteConstruction()
        {
            currentState = BuildingState.Active;
            OnBuildingCompleted?.Invoke(this);
        }
        
        #endregion
        
        #region 農民配置
        
        /// <summary>
        /// 農民を建物に配置
        /// </summary>
        public bool AssignFarmer(Farmer farmer)
        {
            if (!buildingData.isSpecialBuilding || currentState != BuildingState.Active)
                return false;
            
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
            
            if (currentTurn - lastResourceGenerationTurn >= buildingData.resourceGenerationInterval)
            {
                GenerateResources();
                lastResourceGenerationTurn = currentTurn;
                
                // 農民の行動力を消費
                ProcessFarmerActionPoints();
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
                if (slot.IsOccupied && slot.HasActionPoints)
                {
                    // 基本生産量にスキルレベルによる倍率を適用
                    totalProduction += buildingData.baseProductionAmount * slot.ProductionMultiplier;
                }
            }
            
            return Mathf.RoundToInt(totalProduction * buildingData.productionMultiplier);
        }
        
        private void ProcessFarmerActionPoints()
        {
            foreach (var slot in farmerSlots)
            {
                if (slot.IsOccupied)
                {
                    slot.ConsumeActionPoint();
                    if (!slot.HasActionPoints)
                    {
                        // 行動力が尽きた農民を除去
                        slot.RemoveFarmer();
                        currentSkillUses--;
                        
                        if (currentSkillUses <= 0)
                        {
                            ConvertToRuin();
                        }
                    }
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
            currentState = BuildingState.Ruined;
            OnBuildingDestroyed?.Invoke(this);
            
            // すべての農民を解放
            foreach (var slot in farmerSlots)
            {
                slot.RemoveFarmer();
            }
        }
        
        #endregion
        
        /// <summary>
        /// 農民スロット管理クラス
        /// </summary>
        [Serializable]
        private class FarmerSlot
        {
            private Farmer assignedFarmer;
            private int remainingActionPoints;
            
            public bool IsOccupied => assignedFarmer != null;
            public bool HasActionPoints => remainingActionPoints > 0;
            public float ProductionMultiplier => assignedFarmer != null ? assignedFarmer.GetProductionMultiplier() : 1f;
            
            public void AssignFarmer(Farmer farmer)
            {
                assignedFarmer = farmer;
                remainingActionPoints = Mathf.RoundToInt(farmer.CurrentActionPoints);
            }
            
            public void ConsumeActionPoint()
            {
                if (remainingActionPoints > 0)
                    remainingActionPoints--;
            }
            
            public void RemoveFarmer()
            {
                if (assignedFarmer != null)
                {
                    assignedFarmer.OnExitBuilding();
                    assignedFarmer = null;
                    remainingActionPoints = 0;
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
        Disabled,          // 無効化
        Ruined            // 廃墟
    }
    
    /// <summary>
    /// 建物ファクトリー
    /// </summary>
    public static class BuildingFactory
    {
        public static Building CreateBuilding(BuildingDataSO data, Vector3 position)
        {
            if (data == null || data.buildingPrefab == null)
            {
                Debug.LogError("建物データまたはPrefabが設定されていません");
                return null;
            }
            
            GameObject buildingObj = GameObject.Instantiate(data.buildingPrefab, position, Quaternion.identity);
            Building building = buildingObj.GetComponent<Building>();
            
            if (building == null)
            {
                building = buildingObj.AddComponent<Building>();
            }
            
            building.Initialize(data);
            return building;
        }
    }
}