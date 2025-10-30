using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using GameData;
using GamePieces;
using Buildings;

/// <summary>
/// テスト用GameManagerクラス
/// 各機能をボタンでテストできるようにする
/// </summary>
public class DemoUITest : MonoBehaviour
{
    public static DemoUITest Instance;

    #region ターン管理
    private static int nowTurn = 0;
    public static int GetTurn() => nowTurn;
    #endregion

    #region テストデータの参照
    [Header("ScriptableObject Data")]
    [SerializeField] private UnitListTable unitListTable;
    [SerializeField] private BuildingRegistry buildingRegistry;//

    [Header("Default Test Settings")]
    [SerializeField] private Religion defaultReligion = Religion.E;
    [SerializeField] private int defaultPlayerID = 1;
    #endregion

    #region テスト用オブジェクト管理
    private List<Piece> testPieces = new List<Piece>();
    private List<Building> testBuildings = new List<Building>();

    // テスト用の選択されたオブジェクト
    private Piece selectedPiece;
    private Piece targetPiece;
    private Building selectedBuilding;
    private int selectedNum=0;
    #endregion

    #region UI要素
    [Header("UI Elements")]
    [SerializeField] private Canvas uiCanvas;
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private Text infoText;
    [SerializeField] private Text turnText;
    #endregion

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // UIが設定されていない場合は自動生成
        if (uiCanvas == null)
        {
            SetupUI();
        }

        CreateTestButtons();
        UpdateTurnDisplay();
        LogInfo("テストGameManagerが起動しました");
    }

    #region UI セットアップ
    private void SetupUI()
    {
        // Canvas生成
        GameObject canvasObj = new GameObject("TestUICanvas");
        uiCanvas = canvasObj.AddComponent<Canvas>();
        uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        // ボタンコンテナ
        GameObject containerObj = new GameObject("ButtonContainer");
        containerObj.transform.SetParent(uiCanvas.transform);
        buttonContainer = containerObj.AddComponent<RectTransform>();

        var vlg = containerObj.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 10;
        vlg.padding = new RectOffset(20, 20, 20, 20);
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        RectTransform rect = buttonContainer.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(0.3f, 1);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        // 情報表示テキスト
        GameObject infoObj = new GameObject("InfoText");
        infoObj.transform.SetParent(uiCanvas.transform);
        infoText = infoObj.AddComponent<Text>();
        infoText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        infoText.fontSize = 14;
        infoText.color = Color.white;
        infoText.alignment = TextAnchor.UpperLeft;

        RectTransform infoRect = infoObj.GetComponent<RectTransform>();
        infoRect.anchorMin = new Vector2(0.3f, 0.5f);
        infoRect.anchorMax = new Vector2(1, 1);
        infoRect.offsetMin = new Vector2(10, 10);
        infoRect.offsetMax = new Vector2(-10, -10);

        // ターン表示テキスト
        GameObject turnObj = new GameObject("TurnText");
        turnObj.transform.SetParent(uiCanvas.transform);
        turnText = turnObj.AddComponent<Text>();
        turnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        turnText.fontSize = 18;
        turnText.fontStyle = FontStyle.Bold;
        turnText.color = Color.yellow;
        turnText.alignment = TextAnchor.UpperLeft;

        RectTransform turnRect = turnObj.GetComponent<RectTransform>();
        turnRect.anchorMin = new Vector2(0.3f, 0);
        turnRect.anchorMax = new Vector2(1, 0.5f);
        turnRect.offsetMin = new Vector2(10, 10);
        turnRect.offsetMax = new Vector2(-10, -10);
    }

    private void CreateTestButtons()
    {
        // カテゴリ1: 駒の生成
        CreateCategoryLabel("=== 駒の生成 ===");
        CreateButton("農民を生成", CreateFarmer);
        CreateButton("宣教師を生成", CreateMissionary);
        CreateButton("十字軍を生成", CreateMilitary);
        CreateButton("敵農民を生成 (PID:2)", () => CreatePiece(PieceType.Farmer, defaultReligion, 2));
        CreateButton("異宗教農民を生成 (B)", () => CreatePiece(PieceType.Farmer, Religion.F, defaultPlayerID));
        CreateButton("一式セット生成 (3種×2)", CreateTestSet);
        CreateButton("すべての駒を削除", ClearAllPieces);

        // カテゴリ2: 駒の基本操作
        CreateCategoryLabel("=== 駒の基本操作 ===");
        CreateButton("選択した駒にダメージ(3)", () => DamageSelectedPiece(3f));
        CreateButton("選択した駒を回復(5)", () => HealSelectedPiece(5f));
        CreateButton("選択した駒をアップグレード", UpgradeSelectedPiece);
        CreateButton("選択した駒のAP消費(2)", () => ConsumeAPSelectedPiece(2f));

        // カテゴリ3: 建物関連
        CreateCategoryLabel("=== 建物関連 ===");
        CreateButton("建物を直接生成", DirectCreateBuilding);
        CreateButton("農民で建物を建築", FarmerBuildBuilding);
        CreateButton("最高AP農民で建築完成", CompleteConstructionWithBestFarmer);
        CreateButton("農民を建物に配置", AssignFarmerToBuilding);
        CreateButton("建物をアップグレード", UpgradeSelectedBuilding);
        CreateButton("建築をキャンセル", CancelConstruction);
        CreateButton("すべての建物を削除", ClearAllBuildings);

        // カテゴリ4: 特殊スキル
        CreateCategoryLabel("=== 特殊スキル ===");
        CreateButton("宣教師で魅惑攻撃", MissionaryCharm);
        CreateButton("宣教師で占領", MissionaryOccupy);
        CreateButton("十字軍で攻撃", MilitaryAttack);
        CreateButton("農民で犠牲スキル", FarmerSacrifice);

        // カテゴリ5: ターン管理
        CreateCategoryLabel("=== ターン管理 ===");
        CreateButton("次のターンへ", NextTurn);
        CreateButton("建物のターン処理実行", ProcessBuildingTurn);

        // カテゴリ6: 情報表示
        CreateCategoryLabel("=== 情報表示 ===");
        CreateButton("次のオブジェクトを選択",ChangeSelectedPiece);
        CreateButton("すべての駒の情報", ShowAllPiecesInfo);
        CreateButton("すべての建物の情報", ShowAllBuildingsInfo);
        CreateButton("選択中オブジェクトの詳細", ShowSelectedInfo);

        // カテゴリ7: 設定変更
        CreateCategoryLabel("=== 設定変更 ===");
        CreateButton("宗教を切替 (A→B→C...)", CycleReligion);
        CreateButton("プレイヤーIDを切替 (1⇔2)", TogglePlayerID);
        CreateButton("現在の設定を表示", ShowCurrentSettings);
    }

    private void CreateButton(string label, Action onClick)
    {
        GameObject buttonObj = new GameObject(label);
        buttonObj.transform.SetParent(buttonContainer);

        Button button = buttonObj.AddComponent<Button>();
        Image img = buttonObj.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform);
        Text text = textObj.AddComponent<Text>();
        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 12;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(200, 30);

        button.onClick.AddListener(() => onClick?.Invoke());
    }

    private void CreateCategoryLabel(string label)
    {
        GameObject labelObj = new GameObject(label);
        labelObj.transform.SetParent(buttonContainer);

        Text text = labelObj.AddComponent<Text>();
        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 14;
        text.fontStyle = FontStyle.Bold;
        text.color = Color.cyan;
        text.alignment = TextAnchor.MiddleLeft;

        RectTransform rect = labelObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(200, 25);
    }
    #endregion

    #region 駒の生成
    private void CreateFarmer()
    {
        CreatePiece(PieceType.Farmer, defaultReligion, defaultPlayerID);
    }

    private void CreateMissionary()
    {
        CreatePiece(PieceType.Missionary, defaultReligion, defaultPlayerID);
    }

    private void CreateMilitary()
    {
        CreatePiece(PieceType.Military, defaultReligion, defaultPlayerID);
    }

    /// <summary>
    /// 汎用的な駒生成メソッド（PieceFactoryを活用）
    /// </summary>
    private void CreatePiece(PieceType pieceType, Religion religion, int playerID)
    {
        if (unitListTable == null)
        {
            LogInfo("エラー: UnitListTableが設定されていません");
            return;
        }

        // UnitListTableから対応するPieceDataSOを取得
        var pieceDetail = new UnitListTable.PieceDetail(pieceType, religion);
        PieceDataSO pieceData = unitListTable.GetPieceDataSO(pieceDetail);

        if (pieceData == null)
        {
            LogInfo($"エラー: {pieceType} ({religion}) のデータが見つかりません");
            return;
        }

        Vector3 position = new Vector3(testPieces.Count * 2f, 0, 0);

        // PieceFactoryを使って駒を生成
        Piece piece = PieceFactory.CreatePiece(pieceData, position, playerID);

        if (piece != null)
        {
            testPieces.Add(piece);
            selectedPiece = piece;

            // 死亡イベントを購読
            piece.OnPieceDeath += OnPieceDeath;

            LogInfo($"{pieceType} ({religion}) を生成しました (位置: {position}, PID: {playerID})");
        }
        else
        {
            LogInfo($"エラー: {pieceType} ({religion}) の生成に失敗しました");
        }
    }

    /// <summary>
    /// Factoryを使って複数の駒を一度に生成（テスト用セット）
    /// </summary>
    private void CreateTestSet()
    {
        LogInfo("=== テストセットを生成中 ===");

        // プレイヤー1の駒セット
        CreatePiece(PieceType.Farmer, defaultReligion, 1);
        CreatePiece(PieceType.Missionary, defaultReligion, 1);
        CreatePiece(PieceType.Military, defaultReligion, 1);

        // プレイヤー2の駒セット
        CreatePiece(PieceType.Farmer, defaultReligion, 2);
        CreatePiece(PieceType.Missionary, defaultReligion, 2);
        CreatePiece(PieceType.Military, defaultReligion, 2);

        LogInfo($"テストセットの生成完了: 計{6}体の駒を生成しました");
    }

    private void ClearAllPieces()
    {
        foreach (var piece in testPieces)
        {
            if (piece != null)
            {
                piece.OnPieceDeath -= OnPieceDeath; // イベント購読解除
                Destroy(piece.gameObject);
            }
        }
        testPieces.Clear();
        selectedPiece = null;
        targetPiece = null;
        LogInfo("すべての駒を削除しました");
    }
    #endregion

    #region 駒の基本操作
    private void DamageSelectedPiece(float damage)
    {
        if (selectedPiece == null)
        {
            LogInfo("エラー: 駒が選択されていません");
            return;
        }

        selectedPiece.TakeDamage(damage);
        LogInfo($"{selectedPiece.Data.pieceName}に{damage}ダメージを与えました (HP: {selectedPiece.CurrentHP}/{selectedPiece.Data.maxHPByLevel[selectedPiece.UpgradeLevel]})");
    }

    private void HealSelectedPiece(float amount)
    {
        if (selectedPiece == null)
        {
            LogInfo("エラー: 駒が選択されていません");
            return;
        }

        selectedPiece.Heal(amount);
        LogInfo($"{selectedPiece.Data.pieceName}を{amount}回復しました (HP: {selectedPiece.CurrentHP}/{selectedPiece.Data.maxHPByLevel[selectedPiece.UpgradeLevel]})");
    }

    private void UpgradeSelectedPiece()
    {
        if (selectedPiece == null)
        {
            LogInfo("エラー: 駒が選択されていません");
            return;
        }

        bool success = selectedPiece.UpgradePiece();
        if (success)
        {
            LogInfo($"{selectedPiece.Data.pieceName}をレベル{selectedPiece.UpgradeLevel}にアップグレードしました");
        }
        else
        {
            LogInfo("アップグレードに失敗しました（最大レベル）");
        }
    }

    private void ConsumeAPSelectedPiece(float amount)
    {
        if (selectedPiece == null)
        {
            LogInfo("エラー: 駒が選択されていません");
            return;
        }

        bool success = selectedPiece.ConsumeAP(amount);
        if (success)
        {
            LogInfo($"{selectedPiece.Data.pieceName}の行動力を{amount}消費しました (AP: {selectedPiece.CurrentAP}/{selectedPiece.Data.maxAPByLevel[selectedPiece.UpgradeLevel]})");
        }
        else
        {
            LogInfo("行動力が不足しています");
        }
    }
    #endregion

    #region 建物関連
    /// <summary>
    /// BuildingFactoryを使って建物を直接生成
    /// </summary>
    private void DirectCreateBuilding()
    {
        if (buildingRegistry == null || buildingRegistry.buildingDataSOList.Length == 0)
        {
            LogInfo("エラー: BuildingDataSOが設定されていません");
            return;
        }

        // 最初の建物データを使用
        BuildingDataSO buildingData = buildingRegistry.buildingDataSOList[0];
        Vector3 buildPos = new Vector3(testBuildings.Count * 3f, 0, 0);

        // BuildingFactoryを使って建物を生成
        Building building = BuildingFactory.CreateBuilding(buildingData, buildPos);

        if (building != null)
        {
            testBuildings.Add(building);
            selectedBuilding = building;

            // イベントを購読
            building.OnBuildingDestroyed += OnBuildingDestroyed;
            building.OnResourceGenerated += OnResourceGenerated;

            // テスト用に建築を完了させる
            building.ProgressConstruction(buildingData.buildingAPCost);

            LogInfo($"建物 {buildingData.buildingName} を直接生成しました (位置: {buildPos}, 状態: {building.State})");
        }
        else
        {
            LogInfo("建物の生成に失敗しました");
        }
    }

    private void FarmerBuildBuilding()
    {
        if (selectedPiece == null || !(selectedPiece is Farmer))
        {
            LogInfo("エラー: 農民が選択されていません");
            return;
        }

        Farmer farmer = selectedPiece as Farmer;
        Vector3 buildPos = new Vector3(testBuildings.Count * 3f, 0, 0);

        bool success = farmer.StartConstruction(buildingRegistry.buildingDataSOList[0], buildPos);
        if (success)
        {
            LogInfo($"農民が建物の建築を開始しました (位置: {buildPos})");
            // 建築された建物を探してリストに追加
            var building = FindObjectsOfType<Building>().LastOrDefault();
            if (building != null && !testBuildings.Contains(building))
            {
                testBuildings.Add(building);
                selectedBuilding = building;

                // イベントを購読
                building.OnBuildingDestroyed += OnBuildingDestroyed;
                building.OnResourceGenerated += OnResourceGenerated;
            }
        }
        else
        {
            LogInfo("建築に失敗しました");
        }
    }

    /// <summary>
    /// 建築中の建物に最も行動力を持つ農民を投入して完成させる
    /// </summary>
    private void CompleteConstructionWithBestFarmer()
    {
        // 建築中の建物を取得
        var underConstructionBuilding = testBuildings.FirstOrDefault(b =>
            b != null && b.State == Buildings.BuildingState.UnderConstruction);

        if (underConstructionBuilding == null)
        {
            LogInfo("エラー: 建築中の建物がありません");
            return;
        }

        // 最も行動力が高い農民を検索
        Farmer bestFarmer = null;
        float maxAP = 0;

        foreach (var piece in testPieces)
        {
            if (piece != null && piece is Farmer && piece.IsAlive && piece.State == PieceState.Idle)
            {
                Farmer farmer = piece as Farmer;
                if (farmer.CurrentAP > maxAP)
                {
                    maxAP = farmer.CurrentAP;
                    bestFarmer = farmer;
                }
            }
        }

        if (bestFarmer == null)
        {
            LogInfo("エラー: 投入可能な農民がいません");
            return;
        }

        LogInfo($"最高AP農民({bestFarmer.Data.pieceName}, AP:{bestFarmer.CurrentAP})を建築中の建物({underConstructionBuilding.Data.buildingName}, 残りコスト:{underConstructionBuilding.RemainingBuildCost})に投入します");

        bool success = bestFarmer.ContinueConstruction(underConstructionBuilding);
        if (success)
        {
            LogInfo($"建築進行完了: 建物状態={underConstructionBuilding.State}, 農民AP={bestFarmer.CurrentAP}, 農民状態={bestFarmer.State}");
            selectedBuilding = underConstructionBuilding;
        }
        else
        {
            LogInfo("建築の継続に失敗しました");
        }
    }

    private void AssignFarmerToBuilding()
    {
        if (selectedPiece.State == PieceState.InBuilding)
        {
            LogInfo("選択している駒は既に建物の中に入っています");
            return;
        }



        if (selectedPiece == null || !(selectedPiece is Farmer))
        {
            LogInfo("エラー: 農民が選択されていません");
            return;
        }

        if (selectedBuilding == null)
        {
            LogInfo("エラー: 建物が選択されていません");
            return;
        }


        Farmer farmer = selectedPiece as Farmer;
        bool success = farmer.EnterBuilding(selectedBuilding);
        if (success)
        {
            LogInfo($"農民を建物に配置しました");
        }
        else
        {
            LogInfo("建物への配置に失敗しました");
        }
    }

    /// <summary>
    /// 建築中の建物をキャンセルする（消耗された農民と行動力は返されない）
    /// </summary>
    private void CancelConstruction()
    {
        if (selectedBuilding == null)
        {
            LogInfo("エラー: 建物が選択されていません");
            return;
        }

        if (selectedBuilding.State != Buildings.BuildingState.UnderConstruction)
        {
            LogInfo("エラー: 選択された建物は建築中ではありません");
            return;
        }

        LogInfo($"建物 {selectedBuilding.Data.buildingName} の建築をキャンセルします (進捗: {selectedBuilding.BuildProgress:P0})");
        LogInfo("注意: 消耗された農民と行動力は返されません");

        // CancelConstruction()内でOnBuildingDestroyedイベントが発火され、
        // OnBuildingDestroyedハンドラーで自動的にリストから削除される
        bool success = selectedBuilding.CancelConstruction();
        if (success)
        {
            LogInfo("建築がキャンセルされました");
        }
        else
        {
            LogInfo("建築のキャンセルに失敗しました");
        }
    }

    private void UpgradeSelectedBuilding()
    {
        if (selectedBuilding == null)
        {
            LogInfo("エラー: 建物が選択されていません");
            return;
        }

        bool success = selectedBuilding.UpgradeBuilding();
        if (success)
        {
            LogInfo($"{selectedBuilding.Data.buildingName}をレベル{selectedBuilding.UpgradeLevel}にアップグレードしました");
        }
        else
        {
            LogInfo("建物のアップグレードに失敗しました");
        }
    }

    private void ClearAllBuildings()
    {
        foreach (var building in testBuildings)
        {
            if (building != null)
            {
                building.OnBuildingDestroyed -= OnBuildingDestroyed; // イベント購読解除
                building.OnResourceGenerated -= OnResourceGenerated; // イベント購読解除
                Destroy(building.gameObject);
            }
        }
        testBuildings.Clear();
        selectedBuilding = null;
        LogInfo("すべての建物を削除しました");
    }

    /// <summary>
    /// 資源が生成された時のイベントハンドラー
    /// </summary>
    private void OnResourceGenerated(int amount)
    {
        LogInfo($"【資源生成】 資源 {amount} が生成されました！");
    }

    /// <summary>
    /// 駒が死亡した時のイベントハンドラー
    /// </summary>
    private void OnPieceDeath(Piece piece)
    {
        if (testPieces.Contains(piece))
        {
            testPieces.Remove(piece);
            LogInfo($"{piece.Data.pieceName} が死亡してリストから削除されました（残り: {testPieces.Count}体）");

            // 選択中の駒だった場合はクリア
            if (selectedPiece == piece)
                selectedPiece = testPieces[0];
            if (targetPiece == piece)
                targetPiece = null;

            // イベント購読解除
            piece.OnPieceDeath -= OnPieceDeath;
        }
    }

    /// <summary>
    /// 建物が破壊された時のイベントハンドラー
    /// </summary>
    private void OnBuildingDestroyed(Building building)
    {
        if (testBuildings.Contains(building))
        {
            testBuildings.Remove(building);
            LogInfo($"{building.Data.buildingName} が崩壊してリストから削除されました（残り: {testBuildings.Count}個）");

            // 選択中の建物だった場合はクリア
            if (selectedBuilding == building)
                selectedBuilding = null;

            // イベント購読解除
            building.OnBuildingDestroyed -= OnBuildingDestroyed;
            building.OnResourceGenerated -= OnResourceGenerated;
        }
    }
    #endregion

    #region 特殊スキル
    private void MissionaryCharm()
    {
        if (selectedPiece == null || !(selectedPiece is Missionary))
        {
            LogInfo("エラー: 宣教師が選択されていません");
            return;
        }

        // ターゲットとして最初の駒を選択（デモ用）
        targetPiece = testPieces.FirstOrDefault(p => p != selectedPiece && p != null && p.IsAlive);
        if (targetPiece == null)
        {
            LogInfo("エラー: 攻撃対象がいません（他の駒を生成してください）");
            return;
        }

        Missionary missionary = selectedPiece as Missionary;
        bool success = missionary.ConversionAttack(targetPiece);
        if (success)
        {
            LogInfo($"宣教師が{targetPiece.Data.pieceName}に魅惑攻撃を実行しました");
        }
        else
        {
            LogInfo("魅惑攻撃に失敗しました");
        }
    }

    private void MissionaryOccupy()
    {
        if (selectedPiece == null || !(selectedPiece is Missionary))
        {
            LogInfo("エラー: 宣教師が選択されていません");
            return;
        }

        Missionary missionary = selectedPiece as Missionary;
        Vector3 targetPos = missionary.transform.position + Vector3.forward * 2;
        bool success = missionary.StartOccupy(targetPos);
        if (success)
        {
            LogInfo($"宣教師が占領を試みました (位置: {targetPos})");
        }
        else
        {
            LogInfo("占領に失敗しました");
        }
    }

    private void MilitaryAttack()
    {
        if (selectedPiece == null || !(selectedPiece is MilitaryUnit))
        {
            LogInfo("エラー: 十字軍が選択されていません");
            return;
        }

        // ターゲットとして最初の駒を選択（デモ用）
        targetPiece = testPieces.FirstOrDefault(p => p != selectedPiece && p != null && p.IsAlive);
        if (targetPiece == null)
        {
            LogInfo("エラー: 攻撃対象がいません（他の駒を生成してください）");
            return;
        }

        MilitaryUnit military = selectedPiece as MilitaryUnit;
        string name = targetPiece.Data.pieceName; 
        bool success = military.Attack(targetPiece);
        
        if (success)
        {
            //LogInfo($"十字軍が{targetPiece.Data.pieceName}を攻撃しました");
            //↑こう書くと相手がすでに死んでいる場合nullreferenceexceptionになる
            LogInfo($"十字軍が{name}を攻撃しました");
        }
        else
        {
            LogInfo("攻撃に失敗しました");
        }
    }

    private void FarmerSacrifice()
    {
        if (selectedPiece == null || !(selectedPiece is Farmer))
        {
            LogInfo("エラー: 農民が選択されていません");
            return;
        }

        // ターゲットとして最初の駒を選択（デモ用）
        targetPiece = testPieces.FirstOrDefault(p => p != selectedPiece && p != null && p.IsAlive);
        if (targetPiece == null)
        {
            LogInfo("エラー: 対象がいません（他の駒を生成してください）");
            return;
        }

        Farmer farmer = selectedPiece as Farmer;
        float oldTargetHP = targetPiece.CurrentHP;
        bool success = farmer.Sacrifice(targetPiece);

        if (success)
        {
            LogInfo($"農民が{targetPiece.Data.pieceName}に犠牲スキルを使用しました (HP: {oldTargetHP:F1} → {targetPiece.CurrentHP:F1}, 農民AP: {farmer.CurrentAP:F1})");
        }
        else
        {
            LogInfo("犠牲スキルの使用に失敗しました");
        }
    }
    #endregion

    #region ターン管理
    public void NextTurn()
    {
        nowTurn++;
        UpdateTurnDisplay();
        LogInfo($"ターン {nowTurn} に進みました");
    }

    private void ProcessBuildingTurn()
    {
        foreach (var building in testBuildings)
        {
            if (building != null && building.IsAlive)
            {
                building.ProcessTurn(nowTurn);
            }
        }
        LogInfo($"すべての建物のターン処理を実行しました (ターン: {nowTurn})");
    }

    private void UpdateTurnDisplay()
    {
        if (turnText != null)
        {
            turnText.text = $"現在のターン: {nowTurn}";
        }
    }
    #endregion

    #region 情報表示
    private void ShowAllPiecesInfo()
    {
        if (testPieces.Count == 0)
        {
            LogInfo("駒が存在しません");
            return;
        }

        string info = $"=== 駒の一覧 ({testPieces.Count}体) ===\n";
        for (int i = 0; i < testPieces.Count; i++)
        {
            var piece = testPieces[i];
            if (piece != null && piece.IsAlive)
            {
                info += $"[{i}] {piece.Data.pieceName} - HP:{piece.CurrentHP:F1}/{piece.Data.maxHPByLevel[piece.UpgradeLevel]:F1} AP:{piece.CurrentAP:F1}/{piece.Data.maxAPByLevel[piece.UpgradeLevel]:F1} Lv:{piece.UpgradeLevel} PID:{piece.CurrentPID}\n";
            }
        }
        LogInfo(info);
    }

    private void ShowAllBuildingsInfo()
    {
        if (testBuildings.Count == 0)
        {
            LogInfo("建物が存在しません");
            return;
        }

        string info = $"=== 建物の一覧 ({testBuildings.Count}個) ===\n";
        for (int i = 0; i < testBuildings.Count; i++)
        {
            var building = testBuildings[i];
            if (building != null && building.IsAlive)
            {
                info += $"[{i}] {building.Data.buildingName} - HP:{building.CurrentHP} 状態:{building.State} Lv:{building.UpgradeLevel} 進捗:{building.BuildProgress:P0}\n";
            }
        }
        LogInfo(info);
    }


    private void ChangeSelectedPiece()
    {
        if ((selectedNum + 1) % (testPieces.Count) != 0)
            selectedNum++;
        else
            selectedNum = 0;

        selectedPiece = testPieces[selectedNum];
        string info = "===選択中の駒の番号===\n";

        info += $"配列内で要素数{selectedNum}番の駒が選ばれています。\n";
        LogInfo(info);

    }

    private void ShowSelectedInfo()
    {
        string info = "=== 選択中のオブジェクト ===\n";

        if (selectedPiece != null)
        {
            info += $"選択駒: {selectedPiece.Data.pieceName}\n";
            info += $"  HP: {selectedPiece.CurrentHP:F1}/{selectedPiece.Data.maxHPByLevel[selectedPiece.UpgradeLevel]:F1}\n";
            info += $"  AP: {selectedPiece.CurrentAP:F1}/{selectedPiece.Data.maxAPByLevel[selectedPiece.UpgradeLevel]:F1}\n";
            info += $"  レベル: {selectedPiece.UpgradeLevel}\n";
            info += $"  プレイヤーID: {selectedPiece.CurrentPID} (元: {selectedPiece.OriginalPID})\n";
            info += $"  状態: {selectedPiece.State}\n";
        }
        else
        {
            info += "選択駒: なし\n";
        }

        if (selectedBuilding != null)
        {
            info += $"\n選択建物: {selectedBuilding.Data.buildingName}\n";
            info += $"  HP: {selectedBuilding.CurrentHP}\n";
            info += $"  状態: {selectedBuilding.State}\n";
            info += $"  レベル: {selectedBuilding.UpgradeLevel}\n";
            info += $"  建築進捗: {selectedBuilding.BuildProgress:P0}\n";
        }
        else
        {
            info += "\n選択建物: なし\n";
        }

        LogInfo(info);
    }

    private void LogInfo(string message)
    {
        Debug.Log(message);
        if (infoText != null)
        {
            infoText.text = $"{System.DateTime.Now:HH:mm:ss} - {message}\n\n{infoText.text}";

            // テキストが長くなりすぎたら古い部分を削除
            string[] lines = infoText.text.Split('\n');
            if (lines.Length > 50)
            {
                infoText.text = string.Join("\n", lines.Take(50).ToArray());
            }
        }
    }
    #endregion

    #region 設定変更
    private void CycleReligion()
    {
        int currentIndex = (int)defaultReligion;
        currentIndex++;

        // Noneを飛ばしてA~Hをサイクル
        if (currentIndex > (int)Religion.H)
        {
            currentIndex = (int)Religion.G;
        }

        defaultReligion = (Religion)currentIndex;
        LogInfo($"宗教を {defaultReligion} に変更しました");
    }

    private void TogglePlayerID()
    {
        defaultPlayerID = (defaultPlayerID == 1) ? 2 : 1;
        LogInfo($"プレイヤーIDを {defaultPlayerID} に変更しました");
    }

    private void ShowCurrentSettings()
    {
        string info = "=== 現在の設定 ===\n";
        info += $"デフォルト宗教: {defaultReligion}\n";
        info += $"デフォルトプレイヤーID: {defaultPlayerID}\n";
        info += $"現在のターン: {nowTurn}\n";
        LogInfo(info);
    }
    #endregion

    #region デバッグ用キーボードショートカット
    private void Update()
    {
        // 数字キーで駒を選択
        if (Input.GetKeyDown(KeyCode.Alpha1) && testPieces.Count > 0)
        {
            selectedPiece = testPieces[0];
            LogInfo($"駒0を選択: {selectedPiece.Data.pieceName}");
        }
        if (Input.GetKeyDown(KeyCode.Alpha2) && testPieces.Count > 1)
        {
            selectedPiece = testPieces[1];
            LogInfo($"駒1を選択: {selectedPiece.Data.pieceName}");
        }
        if (Input.GetKeyDown(KeyCode.Alpha3) && testPieces.Count > 2)
        {
            selectedPiece = testPieces[2];
            LogInfo($"駒2を選択: {selectedPiece.Data.pieceName}");
        }

        // Bキーで建物を選択
        if (Input.GetKeyDown(KeyCode.B) && testBuildings.Count > 0)
        {
            selectedBuilding = testBuildings[0];
            LogInfo($"建物0を選択: {selectedBuilding.Data.buildingName}");
        }

        // Spaceキーでターン進行
        if (Input.GetKeyDown(KeyCode.Space))
        {
            NextTurn();
        }

        // Iキーで情報表示
        if (Input.GetKeyDown(KeyCode.I))
        {
            ShowSelectedInfo();
        }

        // Rキーで宗教切替
        if (Input.GetKeyDown(KeyCode.R))
        {
            CycleReligion();
        }

        // Pキーでプレイヤー切替
        if (Input.GetKeyDown(KeyCode.P))
        {
            TogglePlayerID();
        }
    }
    #endregion
}
