using UnityEngine;
using GameData;
using GamePieces;
using Buildings;

/// <summary>
/// 十字軍が建物を攻撃して崩壊させるテスト用スクリプト
/// Unityのゲームオブジェクトにアタッチして使用
/// </summary>
public class BuildingAttackTest : MonoBehaviour
{
    [Header("Manager参照")]
    [SerializeField] private PieceManager pieceManager;
    [SerializeField] private BuildingManager buildingManager;

    [Header("テスト設定")]
    [SerializeField] private Religion testReligion = Religion.SilkReligion;
    [SerializeField] private Vector3 militarySpawnPosition = new Vector3(0, 0, 0);
    [SerializeField] private Vector3 buildingSpawnPosition = new Vector3(5, 0, 0);
    [SerializeField] private int testPlayerID = 1;
    [SerializeField] private int enemyPlayerID = 2;

    [Header("キー設定")]
    [SerializeField] private KeyCode createMilitaryKey = KeyCode.Alpha1;
    [SerializeField] private KeyCode createBuildingKey = KeyCode.Alpha2;
    [SerializeField] private KeyCode attackKey = KeyCode.Space;
    [SerializeField] private KeyCode autoAttackKey = KeyCode.A;

    [Header("デバッグ情報")]
    [SerializeField] private bool showDebugInfo = true;

    // テスト用の駒と建物のID
    private int militaryPieceID = -1;
    private int targetBuildingID = -1;
    private bool isAutoAttacking = false;
    private float autoAttackInterval = 1.0f; // 自動攻撃の間隔（秒）
    private float lastAutoAttackTime = 0f;

    void Start()
    {
        // Managerの自動取得（Inspectorで設定されていない場合）
        if (pieceManager == null)
        {
            pieceManager = FindObjectOfType<PieceManager>();
        }
        if (buildingManager == null)
        {
            buildingManager = FindObjectOfType<BuildingManager>();
        }

        // Managerの初期化
        if (pieceManager != null)
        {
            pieceManager.SetLocalPlayerID(testPlayerID);
            Debug.Log($"[BuildingAttackTest] PieceManagerを初期化: PlayerID={testPlayerID}");
        }
        else
        {
            Debug.LogError("[BuildingAttackTest] PieceManagerが見つかりません！");
        }

        if (buildingManager != null)
        {
            buildingManager.SetLocalPlayerID(testPlayerID);
            buildingManager.InitializeBuildingData(testReligion, testReligion);
            Debug.Log($"[BuildingAttackTest] BuildingManagerを初期化: Religion={testReligion}");
        }
        else
        {
            Debug.LogError("[BuildingAttackTest] BuildingManagerが見つかりません！");
        }

        PrintInstructions();
    }

    void Update()
    {
        // キー入力処理
        if (Input.GetKeyDown(createMilitaryKey))
        {
            CreateMilitary();
        }

        if (Input.GetKeyDown(createBuildingKey))
        {
            CreateBuilding();
        }

        if (Input.GetKeyDown(attackKey))
        {
            AttackBuilding();
        }

        if (Input.GetKeyDown(autoAttackKey))
        {
            ToggleAutoAttack();
        }

        // 自動攻撃処理
        if (isAutoAttacking && Time.time - lastAutoAttackTime >= autoAttackInterval)
        {
            AttackBuilding();
            lastAutoAttackTime = Time.time;
        }
    }

    void OnGUI()
    {
        if (!showDebugInfo) return;

        GUILayout.BeginArea(new Rect(10, 10, 400, 300));
        GUILayout.BeginVertical("box");

        GUILayout.Label("=== 建物攻撃テスト ===", GUI.skin.label);
        GUILayout.Space(10);

        GUILayout.Label($"[{createMilitaryKey}] 十字軍を生成");
        GUILayout.Label($"[{createBuildingKey}] 建物を生成");
        GUILayout.Label($"[{attackKey}] 建物を攻撃");
        GUILayout.Label($"[{autoAttackKey}] 自動攻撃 ON/OFF");
        GUILayout.Space(10);

        GUILayout.Label("--- 状態 ---");
        if (militaryPieceID >= 0)
        {
            Piece military = pieceManager?.GetPiece(militaryPieceID);
            if (military != null && military.IsAlive)
            {
                GUILayout.Label($"十字軍: ID={militaryPieceID}, HP={military.CurrentHP}, AP={military.CurrentAP}");
            }
            else
            {
                GUILayout.Label("十字軍: 存在しない");
            }
        }
        else
        {
            GUILayout.Label("十字軍: 未生成");
        }

        if (targetBuildingID >= 0)
        {
            Building building = buildingManager?.GetBuilding(targetBuildingID);
            if (building != null && building.IsAlive)
            {
                GUILayout.Label($"建物: ID={building.BuildingID}, HP={building.CurrentHP}/{building.Data.maxHp}");
            }
            else
            {
                GUILayout.Label("建物: 破壊済み");
            }
        }
        else
        {
            GUILayout.Label("建物: 未生成");
        }

        GUILayout.Label($"自動攻撃: {(isAutoAttacking ? "ON" : "OFF")}");

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    /// <summary>
    /// 十字軍を生成
    /// </summary>
    private void CreateMilitary()
    {
        if (pieceManager == null)
        {
            Debug.LogError("[BuildingAttackTest] PieceManagerがありません！");
            return;
        }

        // 既存の十字軍を削除
        if (militaryPieceID >= 0)
        {
            pieceManager.RemovePiece(militaryPieceID);
            Debug.Log($"[BuildingAttackTest] 既存の十字軍を削除しました: ID={militaryPieceID}");
        }

        // 十字軍を生成
        syncPieceData? syncData = pieceManager.CreatePiece(
            PieceType.Military,
            testReligion,
            testPlayerID,
            militarySpawnPosition
        );

        if (syncData.HasValue)
        {
            militaryPieceID = syncData.Value.pieceID;
            Debug.Log($"[BuildingAttackTest] 十字軍を生成しました: ID={militaryPieceID}, HP={syncData.Value.currentHP}, AP={syncData.Value.currentAP}");
        }
        else
        {
            Debug.LogError("[BuildingAttackTest] 十字軍の生成に失敗しました！");
        }
    }

    /// <summary>
    /// 建物を生成
    /// </summary>
    private void CreateBuilding()
    {
        if (buildingManager == null)
        {
            Debug.LogError("[BuildingAttackTest] BuildingManagerがありません！");
            return;
        }

        // 既存の建物を削除
        if (targetBuildingID >= 0)
        {
            buildingManager.RemoveBuilding(targetBuildingID);
            Debug.Log($"[BuildingAttackTest] 既存の建物を削除しました");
        }

        // 建物名を取得（最初の建物を使用）
        var buildableBuildings = buildingManager.GetBuildableBuildingTypes();
        if (buildableBuildings == null || buildableBuildings.Count == 0)
        {
            Debug.LogError("[BuildingAttackTest] 建設可能な建物がありません！");
            return;
        }

        string buildingName = buildableBuildings[0].buildingName;

        // 建物を生成
        syncBuildingData? syncData = buildingManager.CreateBuildingByName(
            buildingName,
            enemyPlayerID, // 敵プレイヤーの建物として生成
            buildingSpawnPosition
        );

        if (syncData.HasValue)
        {
            targetBuildingID = syncData.Value.buildingID;
            Building targetBuilding = buildingManager.GetBuilding(targetBuildingID);

            // 建築を完了させる
            if (targetBuilding != null && targetBuilding.State == BuildingState.UnderConstruction)
            {
                int remainingCost = targetBuilding.RemainingBuildCost;
                targetBuilding.ProgressConstruction(remainingCost);
                Debug.Log($"[BuildingAttackTest] 建物を即座に完成させました");
            }

            Debug.Log($"[BuildingAttackTest] 建物を生成しました: ID={syncData.Value.buildingID}, Name={syncData.Value.buildingName}, HP={syncData.Value.currentHP}");
        }
        else
        {
            Debug.LogError("[BuildingAttackTest] 建物の生成に失敗しました！");
        }
    }

    /// <summary>
    /// 建物を攻撃
    /// </summary>
    private void AttackBuilding()
    {
        if (pieceManager == null)
        {
            Debug.LogError("[BuildingAttackTest] PieceManagerがありません！");
            return;
        }

        if (buildingManager == null)
        {
            Debug.LogError("[BuildingAttackTest] BuildingManagerがありません！");
            return;
        }

        if (militaryPieceID < 0)
        {
            Debug.LogWarning("[BuildingAttackTest] 十字軍が生成されていません！");
            return;
        }

        if (targetBuildingID < 0)
        {
            Debug.LogWarning("[BuildingAttackTest] 建物が生成されていません！");
            return;
        }

        Building targetBuilding = buildingManager.GetBuilding(targetBuildingID);
        if (targetBuilding == null || !targetBuilding.IsAlive)
        {
            Debug.LogWarning("[BuildingAttackTest] 建物が存在しないか、既に破壊されています！");
            isAutoAttacking = false;
            return;
        }

        // 攻撃実行
        int hpBefore = targetBuilding.CurrentHP;
        bool success = pieceManager.AttackBuilding(militaryPieceID, targetBuilding);

        if (success)
        {
            int hpAfter = targetBuilding.CurrentHP;
            int damage = hpBefore - hpAfter;
            Debug.Log($"[BuildingAttackTest] 攻撃成功！ ダメージ={damage}, 建物HP: {hpBefore} → {hpAfter}");

            // 建物が破壊されたかチェック
            if (!targetBuilding.IsAlive)
            {
                Debug.Log($"[BuildingAttackTest] ★ 建物を破壊しました！ ★");
                isAutoAttacking = false; // 自動攻撃を停止
            }
        }
        else
        {
            Debug.LogWarning("[BuildingAttackTest] 攻撃に失敗しました（AP不足など）");

            // 十字軍のAPが足りない場合、APを回復
            Piece military = pieceManager.GetPiece(militaryPieceID);
            if (military != null && military.CurrentAP < 20)
            {
                military.RecoverAP(100); // テスト用に大量回復
                Debug.Log($"[BuildingAttackTest] 十字軍のAPを回復しました: AP={military.CurrentAP}");
            }
        }
    }

    /// <summary>
    /// 自動攻撃のON/OFF切り替え
    /// </summary>
    private void ToggleAutoAttack()
    {
        isAutoAttacking = !isAutoAttacking;
        lastAutoAttackTime = Time.time;
        Debug.Log($"[BuildingAttackTest] 自動攻撃を{(isAutoAttacking ? "開始" : "停止")}しました");

        if (isAutoAttacking && (militaryPieceID < 0 || targetBuildingID < 0))
        {
            Debug.LogWarning("[BuildingAttackTest] 十字軍または建物が存在しません。まず生成してください。");
            isAutoAttacking = false;
        }

        // 建物が破壊されていないかチェック
        if (isAutoAttacking && buildingManager != null)
        {
            Building building = buildingManager.GetBuilding(targetBuildingID);
            if (building == null || !building.IsAlive)
            {
                Debug.LogWarning("[BuildingAttackTest] 建物が既に破壊されています。");
                isAutoAttacking = false;
            }
        }
    }

    /// <summary>
    /// 操作説明を出力
    /// </summary>
    private void PrintInstructions()
    {
        Debug.Log("=== 建物攻撃テスト 操作説明 ===");
        Debug.Log($"[{createMilitaryKey}] キー: 十字軍を生成");
        Debug.Log($"[{createBuildingKey}] キー: 建物を生成");
        Debug.Log($"[{attackKey}] キー: 建物を攻撃");
        Debug.Log($"[{autoAttackKey}] キー: 自動攻撃のON/OFF");
        Debug.Log("==============================");
    }

    /// <summary>
    /// テストをリセット
    /// </summary>
    [ContextMenu("Reset Test")]
    public void ResetTest()
    {
        if (militaryPieceID >= 0 && pieceManager != null)
        {
            pieceManager.RemovePiece(militaryPieceID);
        }

        if (targetBuildingID >= 0 && buildingManager != null)
        {
            buildingManager.RemoveBuilding(targetBuildingID);
        }

        militaryPieceID = -1;
        targetBuildingID = -1;
        isAutoAttacking = false;

        Debug.Log("[BuildingAttackTest] テストをリセットしました");
    }
}
