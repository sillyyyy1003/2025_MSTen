using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using GameData;
using GamePieces;
using Buildings;

/// <summary>
/// 建物管理クラス
/// GameManagerが具体的な建物の型を見ずに、IDベースでアクセスできるようにする
/// </summary>
public class BuildingManager : MonoBehaviour
{
    // ===== 建物の管理 =====
    private Dictionary<int, Building> buildings = new Dictionary<int, Building>();
    private int nextBuildingID = 1;

    // ===== 依存関係 =====
    [SerializeField] private List<BuildingDataSO> availableBuildingTypes; // 利用可能な建物タイプのリスト

    // ===== イベント（内部使用・GameManagerには通知しない） =====
    public event Action<int> OnBuildingCreated;       // 建物ID
    public event Action<int> OnBuildingCompleted;     // 建物ID（建築完了時）
    public event Action<int> OnBuildingDestroyed;     // 建物ID
    public event Action<int, int> OnResourceGenerated; // (建物ID, 資源量)

    #region 建物の生成

    /// <summary>
    /// 建物を生成（GameManagerから呼び出し）
    /// </summary>
    /// <param name="buildingData">建物データSO</param>
    /// <param name="playerID">プレイヤーID</param>
    /// <param name="position">生成位置</param>
    /// <returns>生成された建物のID（失敗時は-1）</returns>
    public int CreateBuilding(BuildingDataSO buildingData, int playerID, Vector3 position)
    {
        if (buildingData == null)
        {
            Debug.LogError("建物データがnullです");
            return -1;
        }

        // Prefabから建物を生成
        GameObject buildingObj = Instantiate(buildingData.buildingPrefab, position, Quaternion.identity);
        Building building = buildingObj.GetComponent<Building>();

        if (building == null)
        {
            Debug.LogError($"Buildingコンポーネントがありません: {buildingData.buildingName}");
            Destroy(buildingObj);
            return -1;
        }

        // 建物を初期化
        building.Initialize(buildingData);

        // IDを割り当てて登録
        int buildingID = nextBuildingID++;
        building.SetBuildingID(buildingID);
        buildings[buildingID] = building;

        // イベントを購読
        building.OnBuildingCompleted += (completedBuilding) => HandleBuildingCompleted(completedBuilding.BuildingID);
        building.OnBuildingDestroyed += (destroyedBuilding) => HandleBuildingDestroyed(destroyedBuilding.BuildingID);
        building.OnResourceGenerated += (amount) => OnResourceGenerated?.Invoke(buildingID, amount);

        Debug.Log($"建物を生成しました: ID={buildingID}, Name={buildingData.buildingName}, PlayerID={playerID}");
        OnBuildingCreated?.Invoke(buildingID);

        return buildingID;
    }

    /// <summary>
    /// 建物名から建物を生成（便利メソッド）
    /// </summary>
    /// <param name="buildingName">建物名</param>
    /// <param name="playerID">プレイヤーID</param>
    /// <param name="position">生成位置</param>
    /// <returns>生成された建物のID（失敗時は-1）</returns>
    public int CreateBuildingByName(string buildingName, int playerID, Vector3 position)
    {
        BuildingDataSO buildingData = availableBuildingTypes?.Find(b => b.buildingName == buildingName);

        if (buildingData == null)
        {
            Debug.LogError($"建物データが見つかりません: {buildingName}");
            return -1;
        }

        return CreateBuilding(buildingData, playerID, position);
    }

    #endregion

    #region 建築処理

    /// <summary>
    /// 建物の建築を進める（農民を投入）
    /// </summary>
    /// <param name="buildingID">建物ID</param>
    /// <param name="farmerID">投入する農民の駒ID</param>
    /// <param name="pieceManager">PieceManagerの参照</param>
    /// <returns>建築進行成功したらtrue</returns>
    public bool AddFarmerToConstruction(int buildingID, int farmerID, PieceManager pieceManager)
    {
        if (!buildings.TryGetValue(buildingID, out Building building))
        {
            Debug.LogError($"建物が見つかりません: ID={buildingID}");
            return false;
        }

        if (building.State != BuildingState.UnderConstruction)
        {
            Debug.LogError($"建物ID={buildingID}は建築中ではありません（状態: {building.State}）");
            return false;
        }

        // PieceManagerから農民を取得
        if (!pieceManager.DoesPieceExist(farmerID))
        {
            Debug.LogError($"農民が見つかりません: ID={farmerID}");
            return false;
        }

        // 農民のAPを取得
        float farmerAP = pieceManager.GetPieceAP(farmerID);
        if (farmerAP <= 0)
        {
            Debug.LogError($"農民ID={farmerID}のAPが不足しています");
            return false;
        }

        // 建築を進める
        int apToConsume = Mathf.Min(Mathf.FloorToInt(farmerAP), building.RemainingBuildCost);

        // 農民のAPを消費（PieceManagerを通じて）
        if (!pieceManager.ConsumePieceAP(farmerID, apToConsume))
        {
            Debug.LogError($"農民ID={farmerID}のAP消費に失敗しました");
            return false;
        }

        // 建築を進める
        bool isCompleted = building.ProgressConstruction(apToConsume);

        if (isCompleted)
        {
            Debug.Log($"建物ID={buildingID}の建築が完了しました！");
        }
        else
        {
            Debug.Log($"建物ID={buildingID}の建築が進みました（残りコスト: {building.RemainingBuildCost}）");
        }

        return true;
    }

    /// <summary>
    /// 建築をキャンセル
    /// </summary>
    /// <param name="buildingID">建物ID</param>
    /// <returns>キャンセル成功したらtrue</returns>
    public bool CancelConstruction(int buildingID)
    {
        if (!buildings.TryGetValue(buildingID, out Building building))
        {
            Debug.LogError($"建物が見つかりません: ID={buildingID}");
            return false;
        }

        return building.CancelConstruction();
    }

    #endregion

    #region 農民配置

    /// <summary>
    /// 農民を建物に配置
    /// </summary>
    /// <param name="buildingID">建物ID</param>
    /// <param name="farmerID">農民の駒ID</param>
    /// <param name="pieceManager">PieceManagerの参照</param>
    /// <returns>配置成功したらtrue</returns>
    public bool EnterBuilding(int buildingID, int farmerID, PieceManager pieceManager)
    {
        if (!buildings.TryGetValue(buildingID, out Building building))
        {
            Debug.LogError($"建物が見つかりません: ID={buildingID}");
            return false;
        }

        if (!building.Data.isSpecialBuilding)
        {
            Debug.LogError($"建物ID={buildingID}は農民を配置できる建物ではありません");
            return false;
        }

        // PieceManagerから農民を取得
        if (!pieceManager.DoesPieceExist(farmerID))
        {
            Debug.LogError($"農民が見つかりません: ID={farmerID}");
            return false;
        }

        // 農民の型チェック（PieceTypeで確認）
        if (pieceManager.GetPieceType(farmerID) != PieceType.Farmer)
        {
            Debug.LogError($"駒ID={farmerID}は農民ではありません");
            return false;
        }

        // PieceManagerから農民インスタンスを取得
        Farmer farmer = pieceManager.GetFarmer(farmerID);
        if (farmer == null)
        {
            Debug.LogError($"農民の取得に失敗しました: ID={farmerID}");
            return false;
        }

        // 建物に農民を配置
        bool success = building.AssignFarmer(farmer);
        if (success)
        {
            Debug.Log($"農民ID={farmerID}を建物ID={buildingID}に配置しました");
        }
        else
        {
            Debug.LogError($"農民ID={farmerID}を建物ID={buildingID}に配置できませんでした（スロットが満員または建物が未完成）");
        }

        return success;
    }

    #endregion

    #region アップグレード関連

    /// <summary>
    /// 建物の項目をアップグレード
    /// </summary>
    /// <param name="buildingID">建物ID</param>
    /// <param name="upgradeType">アップグレード項目</param>
    /// <returns>成功したらtrue</returns>
    public bool UpgradeBuilding(int buildingID, BuildingUpgradeType upgradeType)
    {
        if (!buildings.TryGetValue(buildingID, out Building building))
        {
            Debug.LogError($"建物が見つかりません: ID={buildingID}");
            return false;
        }

        switch (upgradeType)
        {
            case BuildingUpgradeType.HP:
                return building.UpgradeHP();
            case BuildingUpgradeType.AttackRange:
                return building.UpgradeAttackRange();
            case BuildingUpgradeType.Slots:
                return building.UpgradeSlots();
            case BuildingUpgradeType.BuildCost:
                return building.UpgradeBuildCost();
            default:
                Debug.LogError($"不明なアップグレードタイプ: {upgradeType}");
                return false;
        }
    }

    /// <summary>
    /// アップグレードコストを取得
    /// </summary>
    /// <param name="buildingID">建物ID</param>
    /// <param name="upgradeType">アップグレード項目</param>
    /// <returns>コスト（取得失敗時は-1）</returns>
    public int GetUpgradeCost(int buildingID, BuildingUpgradeType upgradeType)
    {
        if (!buildings.TryGetValue(buildingID, out Building building))
        {
            Debug.LogError($"建物が見つかりません: ID={buildingID}");
            return -1;
        }

        return building.GetUpgradeCost(upgradeType);
    }

    /// <summary>
    /// アップグレード可能かチェック
    /// </summary>
    /// <param name="buildingID">建物ID</param>
    /// <param name="upgradeType">アップグレード項目</param>
    /// <returns>アップグレード可能ならtrue</returns>
    public bool CanUpgrade(int buildingID, BuildingUpgradeType upgradeType)
    {
        if (!buildings.TryGetValue(buildingID, out Building building))
        {
            return false;
        }

        return building.CanUpgrade(upgradeType);
    }

    #endregion

    #region 建物の情報取得

    /// <summary>
    /// 建物の現在HPを取得
    /// </summary>
    public int GetBuildingHP(int buildingID)
    {
        if (!buildings.TryGetValue(buildingID, out Building building))
        {
            Debug.LogError($"建物が見つかりません: ID={buildingID}");
            return -1;
        }
        return building.CurrentHP;
    }

    /// <summary>
    /// 建物の状態を取得
    /// </summary>
    public BuildingState GetBuildingState(int buildingID)
    {
        if (!buildings.TryGetValue(buildingID, out Building building))
        {
            Debug.LogError($"建物が見つかりません: ID={buildingID}");
            return BuildingState.Ruined;
        }
        return building.State;
    }

    /// <summary>
    /// 建築進捗を取得（0.0～1.0）
    /// </summary>
    public float GetBuildProgress(int buildingID)
    {
        if (!buildings.TryGetValue(buildingID, out Building building))
        {
            Debug.LogError($"建物が見つかりません: ID={buildingID}");
            return -1;
        }
        return building.BuildProgress;
    }

    /// <summary>
    /// 建物が存在するかチェック
    /// </summary>
    public bool DoesBuildingExist(int buildingID)
    {
        return buildings.ContainsKey(buildingID);
    }

    /// <summary>
    /// 指定プレイヤーのすべての建物IDを取得
    /// Note: Buildingクラスは現在playerIDを持っていないため、
    /// 別途管理が必要（将来の拡張として）
    /// </summary>
    public List<int> GetAllBuildingIDs()
    {
        return buildings.Keys.ToList();
    }

    /// <summary>
    /// 稼働中の建物IDリストを取得
    /// </summary>
    public List<int> GetOperationalBuildings()
    {
        return buildings
            .Where(kvp => kvp.Value.IsOperational)
            .Select(kvp => kvp.Key)
            .ToList();
    }

    /// <summary>
    /// 建築中の建物IDリストを取得
    /// </summary>
    public List<int> GetBuildingsUnderConstruction()
    {
        return buildings
            .Where(kvp => kvp.Value.State == BuildingState.UnderConstruction)
            .Select(kvp => kvp.Key)
            .ToList();
    }

    #endregion

    #region 建物の削除

    /// <summary>
    /// 建物完成時の内部処理
    /// </summary>
    private void HandleBuildingCompleted(int buildingID)
    {
        Debug.Log($"建物が完成しました: ID={buildingID}");
        OnBuildingCompleted?.Invoke(buildingID);
    }

    /// <summary>
    /// 建物破壊時の内部処理
    /// </summary>
    private void HandleBuildingDestroyed(int buildingID)
    {
        if (buildings.TryGetValue(buildingID, out Building building))
        {
            Debug.Log($"建物が破壊されました: ID={buildingID}");
            buildings.Remove(buildingID);
            OnBuildingDestroyed?.Invoke(buildingID);

            if (building != null && building.gameObject != null)
            {
                Destroy(building.gameObject);
            }
        }
    }

    /// <summary>
    /// 建物を強制削除（GameManager側から呼び出し可能）
    /// </summary>
    public bool RemoveBuilding(int buildingID)
    {
        if (!buildings.TryGetValue(buildingID, out Building building))
        {
            Debug.LogError($"建物が見つかりません: ID={buildingID}");
            return false;
        }

        buildings.Remove(buildingID);
        if (building != null && building.gameObject != null)
        {
            Destroy(building.gameObject);
        }

        Debug.Log($"建物を削除しました: ID={buildingID}");
        return true;
    }

    #endregion

    #region ダメージ処理

    /// <summary>
    /// 建物にダメージを与える
    /// </summary>
    /// <param name="buildingID">建物ID</param>
    /// <param name="damage">ダメージ量</param>
    public bool DamageBuilding(int buildingID, int damage)
    {
        if (!buildings.TryGetValue(buildingID, out Building building))
        {
            Debug.LogError($"建物が見つかりません: ID={buildingID}");
            return false;
        }

        building.TakeDamage(damage);
        return true;
    }

    #endregion

    #region ターン処理

    /// <summary>
    /// すべての建物のターン処理（資源生成など）
    /// </summary>
    /// <param name="currentTurn">現在のターン数</param>
    public void ProcessTurnStart(int currentTurn)
    {
        var operationalBuildings = GetOperationalBuildings();

        foreach (int buildingID in operationalBuildings)
        {
            if (buildings.TryGetValue(buildingID, out Building building))
            {
                building.ProcessTurn(currentTurn);
            }
        }

        Debug.Log($"ターン{currentTurn}: {operationalBuildings.Count}個の建物が処理されました");
    }

    #endregion
}
