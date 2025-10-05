using UnityEngine;
using UnityEngine.UI;
using Buildings;
using GameData;
using GamePieces;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// デモ用テストUI
/// 農民生成、建物作成、資源生成のテスト機能を提供
/// </summary>
public class DemoTestUI : MonoBehaviour
{
    [Header("テスト用データ")]
    [SerializeField] private FarmerDataSO farmerData;
    
    [Header("UI要素")]
    [SerializeField] private Button createFarmerButton;
    [SerializeField] private Button buildBuildingButton;
    [SerializeField] private Button continueConstructionButton;
    [SerializeField] private Button enterBuildingButton;
    [SerializeField] private Button generateResourcesButton;
    [SerializeField] private Button advanceTurnButton;
    
    [Header("テスト設定")]
    [SerializeField] private Vector3 spawnPosition = Vector3.zero;
    [SerializeField] private Vector3 buildingSpawnPosition = new Vector3(3, 0, 0);
    
    // 実行時参照
    private Farmer currentFarmer;
    private Building currentBuilding;
    private int currentTurn = 0;
    private List<Farmer> allFarmers = new List<Farmer>(); // 生成された全農民のリスト
    
    void Start()
    {
        SetupUICallbacks();
        UpdateUI();
    }
    
    private void SetupUICallbacks()
    {
        if (createFarmerButton != null)
            createFarmerButton.onClick.AddListener(CreateFarmer);
            
        if (buildBuildingButton != null)
            buildBuildingButton.onClick.AddListener(BuildBuilding);
            
        if (continueConstructionButton != null)
            continueConstructionButton.onClick.AddListener(ContinueConstruction);
            
        if (enterBuildingButton != null)
            enterBuildingButton.onClick.AddListener(EnterBuilding);
            
        if (generateResourcesButton != null)
            generateResourcesButton.onClick.AddListener(TestResourceGeneration);
            
        if (advanceTurnButton != null)
            advanceTurnButton.onClick.AddListener(AdvanceTurn);
    }
    
    /// <summary>
    /// 農民を生成
    /// </summary>
    public void CreateFarmer()
    {
        if (farmerData == null)
        {
            Debug.LogError("FarmerDataSOが設定されていません");
            return;
        }
        
        var farmer = PieceFactory.CreatePiece(farmerData, spawnPosition, Team.Player) as Farmer;
        if (farmer != null)
        {
            currentFarmer = farmer;
            allFarmers.Add(farmer); // リストに追加
            Debug.Log($"農民を生成しました: {farmer.name} (総数: {allFarmers.Count})");
            UpdateUI();
        }
        else
        {
            Debug.LogError("農民の生成に失敗しました");
        }
    }
    
    /// <summary>
    /// 建物を建築
    /// </summary>
    public void BuildBuilding()
    {
        if (currentFarmer == null)
        {
            Debug.LogWarning("農民が存在しません。まず農民を生成してください。");
            return;
        }
        
        if (farmerData.Buildings == null || farmerData.Buildings.Length == 0)
        {
            Debug.LogError("農民が建築可能な建物リストが無い、又は中身が空です。");
            return;
        }
        
        // 最初の建物を建築
        var buildingData = farmerData.Buildings[0];
        bool success = currentFarmer.StartConstruction(0, buildingSpawnPosition);
        
        if (success)
        {
            Debug.Log($"建物の建築を開始しました: {buildingData.buildingName}");
            
            // 建物インスタンスを検索して参照を保持
            var buildingObj = GameObject.FindObjectOfType<Building>();
            if (buildingObj != null)
            {
                currentBuilding = buildingObj;
                currentBuilding.OnBuildingCompleted += OnBuildingCompleted;
                currentBuilding.OnResourceGenerated += OnResourceGenerated;
                Debug.Log($"建物の建築に成功: {buildingData.buildingName}");
            }
        }
        else
        {
            Debug.LogWarning("建物の建築に失敗しました（行動力不足の可能性）");
        }
        
        UpdateUI();
    }
    
    /// <summary>
    /// 継続建築を実行
    /// </summary>
    public void ContinueConstruction()
    {
        if (currentBuilding == null)
        {
            Debug.LogWarning("建築中の建物がありません");
            return;
        }
        
        if (currentBuilding.State != BuildingState.UnderConstruction)
        {
            Debug.LogWarning("建物は建築中ではありません");
            return;
        }
        
        // 利用可能な農民を検索（Idle状態で行動力がある農民）
        Farmer availableFarmer = null;
        foreach (var farmer in allFarmers)
        {
            if (farmer != null && farmer.State == PieceState.Idle && farmer.CurrentAP > 0)
            {
                availableFarmer = farmer;
                break;
            }
        }
        
        if (availableFarmer == null)
        {
            Debug.LogWarning("利用可能な農民がいません（Idle状態で行動力がある農民が必要）");
            return;
        }
        
        bool success = availableFarmer.ContinueConstruction(currentBuilding);
        if (success)
        {
            Debug.Log($"農民 {availableFarmer.name} が建築を継続しました");
            Debug.Log($"残り建築コスト: {currentBuilding.RemainingBuildCost}");
        }
        else
        {
            Debug.LogWarning("継続建築に失敗しました");
        }
        
        UpdateUI();
    }
    
    /// <summary>
    /// 農民を建物に入れる
    /// </summary>
    public void EnterBuilding()
    {
        if (currentFarmer == null)
        {
            Debug.LogWarning("農民が存在しません");
            return;
        }
        
        if (currentBuilding == null)
        {
            Debug.LogWarning("建物が存在しません");
            return;
        }
        
        bool success = currentFarmer.EnterBuilding(currentBuilding);
        if (success)
        {
            Debug.Log("農民が建物に入りました");
            
        }
        else
        {
            Debug.LogWarning("農民が建物に入れませんでした（建物が稼働していない可能性）");
        }
        
        UpdateUI();
    }
    
    /// <summary>
    /// 資源生成をテスト
    /// </summary>
    public void TestResourceGeneration()
    {
        if (currentBuilding == null)
        {
            Debug.LogWarning("建物が存在しません");
            return;
        }
        
        currentBuilding.ProcessTurn(currentTurn);
        Debug.Log($"ターン {currentTurn} の処理を実行しました");
    }
    
    /// <summary>
    /// ターンを進める
    /// </summary>
    public void AdvanceTurn()
    {
        currentTurn++;
        Debug.Log($"ターンが進みました: {currentTurn}");
        
        // 建物がある場合はターン処理を実行
        if (currentBuilding != null)
        {
            currentBuilding.ProcessTurn(currentTurn);
        }
        
        UpdateUI();
    }
    
    /// <summary>
    /// UI要素の有効/無効を更新
    /// </summary>
    private void UpdateUI()
    {
        if (createFarmerButton != null)
            createFarmerButton.interactable = farmerData != null;
            
        if (buildBuildingButton != null)
            buildBuildingButton.interactable = currentFarmer != null && farmerData != null && farmerData.Buildings != null && farmerData.Buildings.Length > 0;
            
        if (continueConstructionButton != null)
        {
            bool hasAvailableFarmer = allFarmers.Exists(f => f != null && f.State == PieceState.Idle && f.CurrentAP > 0);
            bool hasBuildingUnderConstruction = currentBuilding != null && currentBuilding.State == BuildingState.UnderConstruction;
            continueConstructionButton.interactable = hasAvailableFarmer && hasBuildingUnderConstruction;
        }
            
        if (enterBuildingButton != null)
            enterBuildingButton.interactable = currentFarmer != null && currentBuilding != null && currentBuilding.IsOperational;
            
        if (generateResourcesButton != null)
        {
            generateResourcesButton.interactable = currentBuilding != null && currentBuilding.FarmerSlots.Count(slot => slot.IsOccupied) > 0;
            if(currentBuilding!=null)
                Debug.Log($"farmerslots?:{currentBuilding.FarmerSlots.Count(slot => slot.IsOccupied)}");
        }
            
            
        if (advanceTurnButton != null)
            advanceTurnButton.interactable = true;

        if (currentFarmer != null)
        {
            Debug.Log($"今農民の行動力は: {currentFarmer.CurrentAP}");
        }

        if (currentBuilding != null) 
        {
            Debug.Log($"今建物の状態は:{currentBuilding.State}");
        }
        
    }
    
    // イベントハンドラー
    private void OnBuildingCompleted(Building building)
    {
        Debug.Log($"建物が完成しました: {building.Data.buildingName}");
        UpdateUI();
    }
    
    private void OnResourceGenerated(int amount)
    {
        Debug.Log($"資源が生成されました: {amount}");
    }
    
    void OnDestroy()
    {
        // イベントの購読解除
        if (currentBuilding != null)
        {
            currentBuilding.OnBuildingCompleted -= OnBuildingCompleted;
            currentBuilding.OnResourceGenerated -= OnResourceGenerated;
        }
    }
}