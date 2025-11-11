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
    // ===== ネットワーク同期用 =====
    private int localPlayerID = -1; // このBuildingManagerが管理するプレイヤーID

    // ===== 建物の管理 =====
    private Dictionary<int, Building> buildings = new Dictionary<int, Building>(); // 己方の建物
    private Dictionary<int, Building> enemyBuildings = new Dictionary<int, Building>(); // 敵方の建物
    private int nextBuildingID = 0;

    // ===== 破壊データのキャッシュ =====
    private syncBuildingData? lastDestroyedBuildingData = null;



    // ===== 建物データ =====
    [SerializeField] private BuildingRegistry buildingRegistry; // 全建物のレジストリ
    private List<BuildingDataSO> allBuildingTypes = new List<BuildingDataSO>(); // 全建物（自分+相手、検索用）
    private List<BuildingDataSO> buildableBuildingTypes = new List<BuildingDataSO>(); // 自分が建設可能な建物のみ


    //25.11.11 RI add GameObject
    private GameObject buildingObject;

    // ===== イベント（内部使用・GameManagerには通知しない） =====
    public event Action<int> OnBuildingCreated;       // 建物ID
    public event Action<int> OnBuildingCompleted;     // 建物ID（建築完了時）
    public event Action<int> OnBuildingDestroyed;     // 建物ID（己方の建物が破壊された時のみ）
    public event Action<int, int> OnResourceGenerated; // (建物ID, 資源量)

    #region 初期化

    /// <summary>
    /// ローカルプレイヤーIDを設定（己方/敵方を区別するため）
    /// </summary>
    public void SetLocalPlayerID(int playerID)
    {
        localPlayerID = playerID;
        Debug.Log($"BuildingManagerのローカルプレイヤーIDを設定しました: {playerID}");
    }

    /// <summary>
    /// ローカルプレイヤーIDを取得
    /// </summary>
    public int GetLocalPlayerID()
    {
        return localPlayerID;
    }

    /// <summary>
    /// 宗教に基づいて建物データを初期化（GameManagerから呼び出し）
    /// BuildingRegistryから自動的に該当宗教の建物を取得
    /// </summary>
    /// <param name="playerReligion">自陣営の宗教</param>
    /// <param name="enemyReligion">対戦相手の宗教</param>
    public void InitializeBuildingData(Religion playerReligion, Religion enemyReligion)
    {
        if (buildingRegistry == null)
        {
            Debug.LogError("BuildingRegistry が設定されていません。Inspectorで設定してください。");
            return;
        }

        // 各宗教の建物を取得
        List<BuildingDataSO> playerBuildings = buildingRegistry.GetBuildingsByReligion(playerReligion);
        List<BuildingDataSO> enemyBuildings = buildingRegistry.GetBuildingsByReligion(enemyReligion);

        // 全建物リストを初期化（検索用：自分 + 相手）
        allBuildingTypes = new List<BuildingDataSO>();
        if (playerBuildings != null)
        {
            allBuildingTypes.AddRange(playerBuildings);
        }
        if (enemyBuildings != null)
        {
            allBuildingTypes.AddRange(enemyBuildings);
        }

        // 建設可能な建物リストを初期化（自分のみ）
        buildableBuildingTypes = new List<BuildingDataSO>();
        if (playerBuildings != null)
        {
            buildableBuildingTypes.AddRange(playerBuildings);
        }

        Debug.Log($"宗教に基づいて建物データを初期化しました: 自陣営={playerReligion}({buildableBuildingTypes.Count}個), 敵陣営={enemyReligion}({enemyBuildings?.Count ?? 0}個), 全建物数={allBuildingTypes.Count}");
    }

    #endregion

    #region 建物の生成


    // 25.11.1 RI add return piece gameObject
    public GameObject GetBuildingGameObject()
    {
        if (buildingObject != null)
            return buildingObject;
        return null;
    }



    /// <summary>
    /// 建物を生成（GameManagerから呼び出し）
    /// </summary>
    /// <param name="buildingData">建物データSO</param>
    /// <param name="playerID">プレイヤーID</param>
    /// <param name="position">生成位置</param>
    /// <returns>生成された建物の同期データ（失敗時はnull）</returns>

    public syncBuildingData? CreateBuilding(BuildingDataSO buildingData, int playerID,int pieceID, Vector3 position)
    {                                                                    //25.11.11 RI add piece ID
        if (buildingData == null)
        {
            Debug.LogError("建物データがnullです");
            return null;
        }

        // Prefabから建物を生成
        GameObject buildingObj = Instantiate(buildingData.buildingPrefab, position, Quaternion.identity);

        // 25.11.11 RI init building gameObject
        buildingObject = buildingObj;

        //25.11.10 RI Fix GetComponent Bug
        //Building building = buildingObj.GetComponent<Building>();
        Building building = buildingObj.AddComponent<Building>();

        if (building == null)
        {
            Debug.LogError($"Buildingコンポーネントがありません: {buildingData.buildingName}");
            Destroy(buildingObj);
            return null;
        }

        // 建物を初期化
        building.Initialize(buildingData, playerID);

        // IDを割り当てて登録
        //25.11.11 RI change ID
        ////int buildingID = nextBuildingID;
        //building.SetBuildingID(buildingID);
        //buildings[buildingID] = building;
        //nextBuildingID++;

        building.SetBuildingID(pieceID);
        buildings[pieceID] = building;


        // イベントを購読
        building.OnBuildingCompleted += (completedBuilding) => HandleBuildingCompleted(completedBuilding.BuildingID);
        building.OnBuildingDestroyed += (destroyedBuilding) => HandleBuildingDestroyed(destroyedBuilding.BuildingID);
        building.OnResourceGenerated += (amount) => OnResourceGenerated?.Invoke(pieceID, amount);

        Debug.Log($"建物を生成しました: ID={pieceID}, Name={buildingData.buildingName}, PlayerID={playerID}");
        OnBuildingCreated?.Invoke(pieceID);

        // 同期データを作成して返す
        return CreateCompleteSyncData(pieceID);
    }

    /// <summary>
    /// 建物名から建物を生成（便利メソッド）
    /// </summary>
    /// <param name="buildingName">建物名</param>
    /// <param name="playerID">プレイヤーID</param>
    /// <param name="position">生成位置</param>
    /// <returns>生成された建物の同期データ（失敗時はnull）</returns>
    public syncBuildingData? CreateBuildingByName(string buildingName, int playerID,int pieceID, Vector3 position)
    {                                                                                //25.11.11 RI add piece ID
        BuildingDataSO buildingData = buildableBuildingTypes?.Find(b => b.buildingName == buildingName);

        if (buildingData == null)
        {
            Debug.LogError($"建設可能な建物データが見つかりません: {buildingName}");
            return null;
        }

        return CreateBuilding(buildingData, playerID, pieceID, position);
    }

    /// <summary>
    /// 敵方の建物を同期データから生成（ネットワーク同期用）
    /// </summary>
    /// <param name="sbd">建物同期データ</param>
    /// <returns>生成成功したらtrue</returns>
    public bool CreateEnemyBuilding(syncBuildingData sbd)
    {
        // 建物データを検索（全建物から：自分 + 相手）
        BuildingDataSO buildingData = buildableBuildingTypes?.Find(b => b.buildingName == sbd.buildingName);

        if (buildingData == null)
        {
            Debug.LogError($"建物データが見つかりません: {sbd.buildingName}");
            return false;
        }

        // Prefabから建物を生成
        GameObject buildingObj = Instantiate(buildingData.buildingPrefab, sbd.position, Quaternion.identity);
        Building building = buildingObj.GetComponent<Building>();

        if (building == null)
        {
            Debug.LogError($"Buildingコンポーネントがありません: {sbd.buildingName}");
            Destroy(buildingObj);
            return false;
        }

        // 建物を初期化
        building.Initialize(buildingData, sbd.playerID);
        building.SetBuildingID(sbd.buildingID);

        // 同期データから状態を設定
        if (sbd.currentHP > 0)
        {
            building.SetHP(sbd.currentHP);
        }

        if (sbd.hpLevel > 0)
        {
            building.SetHPLevel(sbd.hpLevel);
        }

        if (sbd.attackRangeLevel > 0)
        {
            building.SetAttackRangeLevel(sbd.attackRangeLevel);
        }

        if (sbd.slotsLevel > 0)
        {
            building.SetSlotsLevel(sbd.slotsLevel);
        }

        if (sbd.buildCostLevel > 0)
        {
            building.SetBuildCostLevel(sbd.buildCostLevel);
        }

        // 建築進捗を設定
        if (sbd.state == BuildingState.UnderConstruction && sbd.remainingBuildCost > 0)
        {
            building.SetRemainingBuildCost(sbd.remainingBuildCost);
        }

        // 状態を設定
        if (sbd.state != BuildingState.UnderConstruction)
        {
            building.SetState(sbd.state);
        }

        // 敵方の建物として登録
        enemyBuildings[sbd.buildingID] = building;

        Debug.Log($"敵方の建物を生成しました: ID={sbd.buildingID}, Name={sbd.buildingName}, PlayerID={sbd.playerID}");
        return true;
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

    #region ネットワーク同期

    /// <summary>
    /// 敵方の建物の状態を同期（ネットワーク同期用）
    /// </summary>
    /// <param name="sbd">建物同期データ</param>
    /// <returns>同期成功したらtrue</returns>
    public bool SyncEnemyBuildingState(syncBuildingData sbd)
    {
        // 建物を検索（己方・敵方両方から）
        Building building = null;
        bool found = buildings.TryGetValue(sbd.buildingID, out building) ||
                     enemyBuildings.TryGetValue(sbd.buildingID, out building);

        if (!found || building == null)
        {
            Debug.LogWarning($"建物が見つかりません: ID={sbd.buildingID}");
            return false;
        }

        // 状態を同期
        if (sbd.currentHP > 0)
        {
            building.SetHP(sbd.currentHP);
        }

        if (sbd.hpLevel > 0)
        {
            building.SetHPLevel(sbd.hpLevel);
        }

        if (sbd.attackRangeLevel > 0)
        {
            building.SetAttackRangeLevel(sbd.attackRangeLevel);
        }

        if (sbd.slotsLevel > 0)
        {
            building.SetSlotsLevel(sbd.slotsLevel);
        }

        if (sbd.buildCostLevel > 0)
        {
            building.SetBuildCostLevel(sbd.buildCostLevel);
        }

        // 建築進捗を同期
        if (sbd.state == BuildingState.UnderConstruction && sbd.remainingBuildCost >= 0)
        {
            building.SetRemainingBuildCost(sbd.remainingBuildCost);
        }

        // 状態を同期
        if (sbd.state != building.State)
        {
            building.SetState(sbd.state);
        }

        // 位置を同期
        if (building.transform.position != sbd.position)
        {
            building.transform.position = sbd.position;
        }

        Debug.Log($"建物の状態を同期しました: ID={sbd.buildingID}");
        return true;
    }

    /// <summary>
    /// 建物の完全な同期データを作成（送信用）
    /// </summary>
    /// <param name="buildingID">建物ID</param>
    /// <returns>同期データ（失敗時はnull）</returns>
    public syncBuildingData? CreateCompleteSyncData(int buildingID)
    {
        // 建物を検索（己方・敵方両方から）
        Building building = null;
        bool found = buildings.TryGetValue(buildingID, out building) ||
                     enemyBuildings.TryGetValue(buildingID, out building);

        if (!found || building == null)
        {
            Debug.LogError($"建物が見つかりません: ID={buildingID}");
            return null;
        }

        // 同期データを作成
        syncBuildingData sbd = new syncBuildingData
        {
            buildingID = buildingID,
            buildingName = building.Data.buildingName,
            playerID = building.PlayerID,
            position = building.transform.position,
            currentHP = building.CurrentHP,
            state = building.State,
            remainingBuildCost = building.RemainingBuildCost,

            // アップグレードレベル
            hpLevel = building.HPLevel,
            attackRangeLevel = building.AttackRangeLevel,
            slotsLevel = building.SlotsLevel,
            buildCostLevel = building.BuildCostLevel
        };

        return sbd;
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
    /// </summary>
    public List<int> GetPlayerBuildings(int playerID)
    {
        return buildings
            .Where(kvp => kvp.Value.PlayerID == playerID)
            .Select(kvp => kvp.Key)
            .ToList();
    }

    /// <summary>
    /// すべての建物IDを取得
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
    /// 建物破壊時の内部処理（己方の建物が破壊された時）
    /// </summary>
    private void HandleBuildingDestroyed(int buildingID)
    {
        Building building = null;
        bool found = buildings.TryGetValue(buildingID, out building) ||
                     enemyBuildings.TryGetValue(buildingID, out building);

        if (!found || building == null)
        {
            Debug.LogWarning($"破壊された建物が見つかりません: ID={buildingID}");
            return;
        }

        // 所有者IDを取得
        int ownerID = building.PlayerID;

        // 破壊データをキャッシュ（己方の建物の場合のみ）
        if (localPlayerID != -1 && ownerID == localPlayerID)
        {
            lastDestroyedBuildingData = new syncBuildingData
            {
                buildingID = buildingID,
                buildingName = building.Data.buildingName,
                playerID = ownerID,
                position = building.transform.position,
                currentHP = 0, // 破壊されたのでHP=0
                state = BuildingState.Ruined
            };
        }

        // 建物を削除
        RemoveBuildingInternal(buildingID, building);

        // イベント発火（己方の建物の場合のみ）
        if (localPlayerID != -1 && ownerID == localPlayerID)
        {
            OnBuildingDestroyed?.Invoke(buildingID);
        }
    }

    /// <summary>
    /// 敵方の建物破壊通知を受信（ネットワーク同期用）
    /// </summary>
    /// <param name="sbd">破壊された建物の同期データ</param>
    /// <returns>削除成功したらtrue</returns>
    public bool HandleEnemyBuildingDestruction(syncBuildingData sbd)
    {
        // 建物を検索
        Building building = null;
        bool found = buildings.TryGetValue(sbd.buildingID, out building) ||
                     enemyBuildings.TryGetValue(sbd.buildingID, out building);

        if (!found || building == null)
        {
            Debug.LogWarning($"破壊通知を受けましたが、建物が見つかりません: ID={sbd.buildingID}");
            return false;
        }

        // 建物を削除（イベントは発火しない）
        RemoveBuildingInternal(sbd.buildingID, building);
        Debug.Log($"敵方の建物破壊通知を受信し、削除しました: ID={sbd.buildingID}");
        return true;
    }

    /// <summary>
    /// 最後に破壊された建物の同期データを取得（送信用）
    /// </summary>
    /// <returns>破壊データ（キャッシュがない場合はnull）</returns>
    public syncBuildingData? GetLastDestroyedBuildingData()
    {
        syncBuildingData? data = lastDestroyedBuildingData;
        lastDestroyedBuildingData = null; // 取得後はクリア
        return data;
    }

    /// <summary>
    /// 建物を内部的に削除する共通処理
    /// </summary>
    private void RemoveBuildingInternal(int buildingID, Building building)
    {
        // 辞書から削除（己方/敵方の判定）
        if (localPlayerID != -1 && building.PlayerID == localPlayerID)
        {
            if (buildings.ContainsKey(buildingID))
            {
                buildings.Remove(buildingID);
            }
        }
        else
        {
            if (enemyBuildings.ContainsKey(buildingID))
            {
                enemyBuildings.Remove(buildingID);
            }
        }

        // GameObjectを破棄
        if (building != null && building.gameObject != null)
        {
            Destroy(building.gameObject);
        }

        Debug.Log($"建物を削除しました: ID={buildingID}");
    }

    /// <summary>
    /// 建物を強制削除（GameManager側から呼び出し可能）
    /// </summary>
    public bool RemoveBuilding(int buildingID)
    {
        Building building = null;
        bool found = buildings.TryGetValue(buildingID, out building) ||
                     enemyBuildings.TryGetValue(buildingID, out building);

        if (!found || building == null)
        {
            Debug.LogError($"建物が見つかりません: ID={buildingID}");
            return false;
        }

        RemoveBuildingInternal(buildingID, building);
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

/// <summary>
/// 建物の同期データ構造体（ネットワーク同期用）
/// </summary>
[System.Serializable]
public struct syncBuildingData
{
    // 基本情報
    public int buildingID;
    public string buildingName;
    public int playerID;
    //25.11.10 RI 修改为序列化Vector3
    public SerializableVector3 position;

    // 状態情報
    public int currentHP;
    public BuildingState state;
    public int remainingBuildCost; // 残り建築コスト

    // アップグレードレベル
    public int hpLevel;            // HP等級 (0-3)
    public int attackRangeLevel;   // 攻撃範囲等級 (0-3)
    public int slotsLevel;         // スロット数等級 (0-3)
    public int buildCostLevel;     // 建造コスト等級 (0-3)
}
