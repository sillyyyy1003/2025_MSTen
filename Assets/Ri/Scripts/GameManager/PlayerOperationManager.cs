using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Unity.Mathematics;
using Newtonsoft.Json.Bson;
using UnityEngine.EventSystems;
using System.Dynamic;
using GameData;
using GamePieces;
using UnityEngine.UI;
using static UnityEditor.PlayerSettings;

/// <summary>
/// 玩家操作管理，负责处理因玩家操作导致的数据变动
/// </summary>
public class PlayerOperationManager : MonoBehaviour
{
    // HexGrid的引用
    public HexGrid _HexGrid;


    private Camera GameCamera;

    // 是否可进行操作
    private bool bCanContinue = true;

    // 是否是当前玩家的回合
    private bool isMyTurn = false;

    // 点击到的格子的id
    private int ClickCellid;

    // 射线检测指定为cell层级
    private int RayTestLayerMask = 1 << 6;

    // 当前选择的单位
    private GameObject SelectingUnit;

    // 上一次选择到的格子的id
    private int LastSelectingCellID;

    // 当前选择的空格子ID（用于创建单位）
    private int SelectedEmptyCellID = -1;

    // 本机玩家保存的棋盘信息
    private Dictionary<int, BoardInfor> PlayerBoardInforDict = new Dictionary<int, BoardInfor>();

    // 本地玩家数据
    private PlayerData localPlayerData;


    // 本地玩家的所有单位GameObject (key: 位置, value: GameObject)
    private Dictionary<int2, GameObject> localPlayerUnits = new Dictionary<int2, GameObject>();

    // 其他玩家的单位GameObject
    private Dictionary<int, Dictionary<int2, GameObject>> otherPlayersUnits = new Dictionary<int, Dictionary<int2, GameObject>>();

    // 玩家数据管理器引用
    //private PlayerDataManager playerDataManager;

    // 本地玩家ID
    private int localPlayerId = -1;

    private int selectCellID;

    // 是否选中了农民
    private bool bIsChooseFarmer;
    // 是否选中了传教士
    private bool bIsChooseMissionary;

    // 保存攻击前的原始位置（用于"移动+攻击"场景）
    private int2? attackerOriginalPosition = null;

    // 双击检测
    // 定义双击的最大时间间隔
    public float doubleClickTimeThreshold = 0.3f;
    private float lastClickTime;
    private int clickCount = 0;

    // === Event 定义区域 ===
    public event System.Action<int, CardType> OnUnitChoosed;


    // *************************
    //         公有属性
    // *************************

    // 移动速度
    public float MoveSpeed = 1.0f;



    // UI相关(需要在Inspector中赋值)
    //public GameObject EndTurnButton;

    // 玩家的id,由GameManage统一分配
    public int PlayerID
    {
        get { return localPlayerId; }
        private set { localPlayerId = value; }
    }


    // Start is called before the first frame update
    void Start()
    {
        GameCamera = GameObject.Find("GameCamera").GetComponent<Camera>();

        // 获取PlayerDataManager引用
        //playerDataManager = PlayerDataManager.Instance;

        // 隐藏结束回合按钮
        //GameSceneUIManager.Instance.SetEndTurn(false);
    }



    // Update is called once per frame
    void Update()
    {
        if (GameManage.Instance.GetIsGamingOrNot() && isMyTurn)
        {
            HandleMouseInput();

        }
        if (Input.GetKeyDown(KeyCode.F) && bIsChooseFarmer)
        {
            // 农民生成建筑
        }
        if (Input.GetKeyDown(KeyCode.H) && bIsChooseFarmer)
        {
            // 传教士魅惑
        }
        if (Input.GetKeyDown(KeyCode.G) && bIsChooseMissionary)
        {
            // 传教士占领
            // 通过PieceManager判断
            if (!PlayerDataManager.Instance.GetPlayerData(localPlayerId).PlayerOwnedCells.Contains(LastSelectingCellID)
                && PieceManager.Instance.OccupyTerritory(PlayerDataManager.Instance.nowChooseUnitID, PlayerBoardInforDict[selectCellID].Cells3DPos))
            {
                _HexGrid.GetCell(LastSelectingCellID).Walled = true;
                PlayerDataManager.Instance.GetPlayerData(localPlayerId).AddOwnedCell(LastSelectingCellID);
            }
            else
            {
                Debug.Log("传教士 ID: " + PlayerDataManager.Instance.nowChooseUnitID + " 占领失败！");
            }
        }
    }


    // *************************
    //        输入处理
    // *************************

    private void HandleMouseInput()
    {
        if (IsPointerOverUIElement())
        {
            return;
        }
        // 左键点击 - 选择单位
        if (Input.GetMouseButtonDown(0) && bCanContinue)
        {
            _HexGrid.GetCell(selectCellID).DisableHighlight();

            // 检测鼠标左键点击
            if (Input.GetMouseButtonDown(0))
            {
                float timeSinceLastClick = Time.time - lastClickTime;

                if (timeSinceLastClick <= doubleClickTimeThreshold)
                {
                    // 第二次点击在阈值时间内，是双击
                    clickCount++;
                    if (clickCount == 2)
                    {


                        HandleLeftClick(true);

                        // 重置计数器
                        clickCount = 0;
                    }
                }
                else
                {
                    // 第一次点击或两次点击间隔太长
                    clickCount = 1;

                    HandleLeftClick(false);
                }

                lastClickTime = Time.time;
            }

        }

        // 右键点击 - 移动/攻击
        if (Input.GetMouseButtonDown(1) && bCanContinue)
        {
            _HexGrid.GetCell(selectCellID).DisableHighlight();
            HandleRightClick();
        }
    }

    // 检测鼠标指针是否在可交互的UI元素上（按钮、输入框等）
    private bool IsPointerOverUIElement()
    {
        if (EventSystem.current == null)
            return false;

        // 创建指针事件数据
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;

        // 射线检测所有UI元素
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        // 只检查可交互的UI组件（按钮、输入框等）
        foreach (var result in results)
        {
            if (result.gameObject.activeInHierarchy)
            {
                // 检查是否是按钮或其他可交互组件
                if (result.gameObject.GetComponent<UnityEngine.UI.Button>() != null ||
                    result.gameObject.GetComponent<UnityEngine.UI.Toggle>() != null ||
                    result.gameObject.GetComponent<UnityEngine.UI.Slider>() != null ||
                    result.gameObject.GetComponent<UnityEngine.UI.InputField>() != null ||
                    result.gameObject.GetComponent<UnityEngine.UI.Dropdown>() != null ||
                    result.gameObject.GetComponent<UnityEngine.UI.Scrollbar>() != null ||
                    result.gameObject.GetComponent<TMPro.TMP_InputField>() != null ||
                    result.gameObject.GetComponent<TMPro.TMP_Dropdown>() != null)
                {
                    return true;
                }
            }
        }

        return false;
    }
    private void HandleLeftClick(bool isDoubleClick)
    {
        Ray ray = GameCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // 射线检测
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, RayTestLayerMask))
        {
            ClickCellid = hit.collider.gameObject.GetComponent<HexCell>().Index;
            int2 clickPos = GameManage.Instance.FindCell(ClickCellid).Cells2DPos;

            // 双击后聚焦
            if (isDoubleClick)
            {
                GameManage.Instance._GameCamera.GetPlayerPosition(GameManage.Instance.FindCell(ClickCellid).Cells3DPos);
            }

            // 检查是否点击了自己的单位
            if (localPlayerUnits.ContainsKey(clickPos))
            {
                // 取消之前的选择
                ReturnToDefault();
                SelectedEmptyCellID = -1;


                // 选择新单位
                SelectingUnit = localPlayerUnits[clickPos];
                SelectingUnit.GetComponent<ChangeMaterial>().Outline();
                LastSelectingCellID = ClickCellid;


                PlayerDataManager.Instance.nowChooseUnitID = PlayerDataManager.Instance.GetUnitIDBy2DPos(clickPos);
                PlayerDataManager.Instance.nowChooseUnitType = PlayerDataManager.Instance.GetUnitTypeIDBy2DPos(clickPos);


                if (PlayerDataManager.Instance.nowChooseUnitType == CardType.Farmer)
                    bIsChooseFarmer = true;
                if (PlayerDataManager.Instance.nowChooseUnitType == CardType.Missionary)
                    bIsChooseMissionary = true;


                OnUnitChoosed?.Invoke(PlayerDataManager.Instance.nowChooseUnitID, PlayerDataManager.Instance.nowChooseUnitType);
                Debug.Log($"选择了单位 ID: {PlayerDataManager.Instance.nowChooseUnitID},{PlayerDataManager.Instance.nowChooseUnitType}");

            }
            else if (otherPlayersUnits.Count >= 1 && otherPlayersUnits[localPlayerId == 0 ? 1 : 0].ContainsKey(clickPos))
            {
                Debug.Log("Get Enemy Unit " + clickPos);
                PlayerUnitDataInterface.Instance.SetEnemyUnitPosition(clickPos);
            }
            else
            {

                // 点击了空地或其他玩家单位
                ReturnToDefault();
                SelectingUnit = null;

                if (!bIsChooseFarmer)
                {
                    PlayerDataManager.Instance.nowChooseUnitID = -1;
                    PlayerDataManager.Instance.nowChooseUnitType = CardType.None;
                }

                // 检查是否是空格子
                if (!PlayerDataManager.Instance.IsPositionOccupied(clickPos) && _HexGrid.IsValidDestination(_HexGrid.GetCell(ClickCellid)))
                {
                    ChooseEmptyCell(ClickCellid);
                    selectCellID = ClickCellid;
                    SelectedEmptyCellID = ClickCellid; // 保存选中的空格子
                                                       //Debug.Log($"选择了空格子: {clickPos}，可以在此创建单位");


                }
                else
                {

                    SelectedEmptyCellID = -1; // 清除选择
                    Debug.Log("该格子无法创建单位");
                }
            }
        }
        else
        {
            ReturnToDefault();
            SelectingUnit = null;
            SelectedEmptyCellID = -1;
        }
    }
    private void HandleRightClick()
    {
        if (SelectingUnit == null) return;

        Ray ray = GameCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // 射线检测
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, RayTestLayerMask))
        {
            ClickCellid = hit.collider.gameObject.GetComponent<HexCell>().Index;
            int2 targetPos = GameManage.Instance.FindCell(ClickCellid).Cells2DPos;

            // 获取当前选中单位的位置
            int2 currentPos = PlayerBoardInforDict[LastSelectingCellID].Cells2DPos;

            // 检查目标位置
            if (PlayerDataManager.Instance.IsPositionOccupied(targetPos))
            {
                int ownerId = PlayerDataManager.Instance.GetUnitOwner(targetPos);

                // 传教士魅惑敌方单位
                if (ownerId != localPlayerId && bIsChooseMissionary && IsAdjacentPosition(currentPos, targetPos))
                {
                    Debug.Log("[魅惑] 传教士尝试魅惑敌方单位");

                    // 检查AP（魅惑需要消耗AP）
                    if (!CheckUnitHasEnoughAP(currentPos, 1))
                    {
                        Debug.Log("[魅惑] AP不足，无法魅惑");
                        return;
                    }

                    ExecuteCharm(targetPos, ownerId);
                    return;
                }

                // 军事单位攻击敌方单位
                if (ownerId != localPlayerId && PlayerDataManager.Instance.nowChooseUnitType == CardType.Solider)
                {
                    //  新逻辑：检查是否在攻击范围（相邻格）
                    if (IsAdjacentPosition(currentPos, targetPos))
                    {
                        // 在攻击范围内，直接攻击
                        Debug.Log("[攻击] 目标在攻击范围内，执行攻击");

                        // 检查AP（攻击需要消耗1点AP）
                        if (!CheckUnitHasEnoughAP(currentPos, 1))
                        {
                            Debug.Log("[攻击] AP不足，无法攻击");
                            return;
                        }

                        ExecuteAttack(targetPos, ClickCellid);
                    }
                    else
                    {
                        // 不在攻击范围内，无法攻击也无法移动到敌方单位位置
                        Debug.Log("[攻击] 目标不在相邻格，无法攻击。请先移动到敌方单位旁边再攻击。");
                        return;
                    }


                    //// 【新增】攻击前检查AP
                    //if (!CheckUnitHasEnoughAP(currentPos, 1))
                    //{
                    //    Debug.Log("[攻击] AP不足，无法攻击");
                    //    return;
                    //}
                    //// 攻击敌方单位
                    //AttackUnit(targetPos, ClickCellid);
                }
                else
                {
                    Debug.Log("不能移动到已占用的位置");
                }
            }
            else
            {
                // 移动到空地
                if (bIsChooseMissionary)
                {
                    List<HexCell> list = new List<HexCell>();
                    for (int i = 0; i < PlayerDataManager.Instance.GetPlayerData(localPlayerId).PlayerOwnedCells.Count; i++)
                    {
                        list.Add(_HexGrid.GetCell(PlayerDataManager.Instance.GetPlayerData(localPlayerId).PlayerOwnedCells[i]));
                    }
                    if (_HexGrid.SearchCellRange(list, _HexGrid.GetCell(targetPos.x, targetPos.y), 3))
                    {
                        MoveToSelectCell(ClickCellid);
                    }
                    else
                    {
                        Debug.LogWarning("Missionary  Cant Move To That Cell!");
                    }

                }
                else if (bIsChooseFarmer)
                {
                    if (PlayerDataManager.Instance.GetPlayerData(localPlayerId).PlayerOwnedCells.Contains(ClickCellid))
                    {
                        MoveToSelectCell(ClickCellid);
                    }
                    else
                    {
                        Debug.LogWarning("Farmer  Cant Move To That Cell!");
                    }

                }
                else
                    MoveToSelectCell(ClickCellid);
            }
        }
    }

    // 返回当前摄像机聚焦的单位id
    public int GetFocusedUnitID()
    {
        return 0;
    }

    // *************************
    //         公有函数
    // *************************



    /// <summary>
    /// 尝试在当前选中的空格子创建单位
    /// </summary>
    /// <param name="unitType">要创建的单位类型</param>
    /// <returns>是否成功创建</returns>
    public bool TryCreateUnit(CardType unitType)
    {
        // 检查是否选中了空格子或领土内
        if (SelectedEmptyCellID == -1 || !_HexGrid.GetCell(SelectedEmptyCellID).Walled)
        {
            Debug.LogWarning("未选中任何空格子");
            return false;
        }

        // 获取选中格子的信息
        BoardInfor cellInfo = GameManage.Instance.GetBoardInfor(SelectedEmptyCellID);
        int2 cellPos = cellInfo.Cells2DPos;

        // 再次确认该位置是空的
        if (PlayerDataManager.Instance.IsPositionOccupied(cellPos))
        {
            Debug.LogWarning("该格子已有单位");
            SelectedEmptyCellID = -1;
            return false;
        }

        // 创建单位

        CreateUnitAtPosition(unitType, SelectedEmptyCellID);

        // 清除选择
        _HexGrid.GetCell(SelectedEmptyCellID).DisableHighlight();
        SelectedEmptyCellID = -1;

        return true;
    }

    // 在外部指定的格子创建单位
    public bool TryCreateUnit(CardType unitType, int cellID)
    {
        // 检查是否选中了空格子
        if (SelectedEmptyCellID == -1 || !_HexGrid.GetCell(SelectedEmptyCellID).Walled)
        {
            Debug.LogWarning("未选中任何空格子");
            return false;
        }

        // 获取选中格子的信息
        BoardInfor cellInfo = GameManage.Instance.GetBoardInfor(SelectedEmptyCellID);
        int2 cellPos = cellInfo.Cells2DPos;

        // 再次确认该位置是空的
        if (PlayerDataManager.Instance.IsPositionOccupied(cellPos))
        {
            Debug.LogWarning("该格子已有单位");
            SelectedEmptyCellID = -1;
            return false;
        }

        // 在领土内创建单位

        CreateUnitAtPosition(unitType, SelectedEmptyCellID);

        // 清除选择
        _HexGrid.GetCell(SelectedEmptyCellID).DisableHighlight();
        SelectedEmptyCellID = -1;

        return true;
    }



    /// <summary>
    /// 初始化玩家
    /// </summary>
    /// <param name="playerId">玩家ID</param>
    /// <param name="startBoardID">玩家初始位置格子id</param>
    public void InitPlayer(int playerId, int startBoardID)
    {
        localPlayerId = playerId;
        PlayerBoardInforDict = GameManage.Instance.GetPlayerBoardInfor();

        // 从PlayerDataManager获取数据
        localPlayerData = PlayerDataManager.Instance.GetPlayerData(playerId);

        Debug.Log($"PlayerOperationManager: 初始化玩家 {playerId}");



        // 创建玩家拥有的单位
        CreatePlayerPope(startBoardID);

        // 添加数据变化事件
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnUnitAdded += OnUnitAddedHandler;
            PlayerDataManager.Instance.OnUnitRemoved += OnUnitRemovedHandler;
            PlayerDataManager.Instance.OnUnitMoved += OnUnitMovedHandler;
        }
    }

    // *************************
    //        回合相关
    // *************************

    // 回合开始
    public void TurnStart()
    {
        // 开始倒计时
        GameSceneUIManager.Instance.StartTurn();

        isMyTurn = true;
        bCanContinue = true;


        // 显示结束回合按钮
        GameSceneUIManager.Instance.SetEndTurn(true);
        PieceManager.Instance.ProcessTurnStart(localPlayerId);
        foreach (var unit in PlayerDataManager.Instance.GetPlayerData(localPlayerId).PlayerUnits)
        {
            unit.SetCanDoAction(true);
            Debug.Log("你的回合开始!重置行动！" + "unit name is " + unit.UnitID + " canDo is " + unit.bCanDoAction);
        }

    }

    // 回合结束
    public void TurnEnd()
    {
        if (!isMyTurn)
        {
            Debug.LogWarning("不是你的回合!");
            return;
        }

        isMyTurn = false;
        bCanContinue = false;

        // 取消当前选择
        _HexGrid.GetCell(selectCellID).DisableHighlight();
        ReturnToDefault();
        SelectingUnit = null;
        SelectedEmptyCellID = -1;



        // 隐藏结束回合按钮
        GameSceneUIManager.Instance.SetEndTurn(false);

        Debug.Log("你的回合结束!");

        // 更新本地玩家数据到PlayerDataManager
        localPlayerData = PlayerDataManager.Instance.GetPlayerData(localPlayerId);

        // 通知GameManage结束回合
        GameManage.Instance.EndTurn();
    }

    // 其他玩家回合禁用输入
    public void DisableInput()
    {
        isMyTurn = false;
        bCanContinue = false;

        // 取消选择
        ReturnToDefault();
        SelectingUnit = null;
        SelectedEmptyCellID = -1;

        // 隐藏结束回合按钮
        GameSceneUIManager.Instance.SetEndTurn(false);

        Debug.Log("等待其他玩家...");
    }

    // 更新其他玩家的显示
    public void UpdateOtherPlayerDisplay(int playerId, PlayerData data)
    {
        if (playerId == localPlayerId)
        {
            Debug.Log($"playerId 为自己");
            return;
        };

        Debug.LogWarning($"更新玩家 {playerId} 的显示");

        // 如果没有这个玩家的字典,创建一个
        if (!otherPlayersUnits.ContainsKey(playerId))
        {
            otherPlayersUnits[playerId] = new Dictionary<int2, GameObject>();
        }

        if (data.PlayerOwnedCells != null && data.PlayerOwnedCells.Count > 0)
        {
            Debug.Log($"[显示更新] 玩家 {playerId} 拥有 {data.PlayerOwnedCells.Count} 个格子");
            foreach (int cellId in data.PlayerOwnedCells)
            {
                if (_HexGrid.GetCell(cellId) != null)
                {
                    _HexGrid.GetCell(cellId).Walled = true; // 设置墙壁/领土效果
                }
            }

        }



        // 清除旧的单位显示
        //foreach (var unit in otherPlayersUnits[playerId].Values)
        //{
        //    if (unit != null)
        //    {
        //        Destroy(unit);
        //        Debug.Log("otherPlayers unit is " + unit.name);
        //    }

        //}
        //otherPlayersUnits[playerId].Clear();

        // 创建新的单位显示

        for (int i = 0; i < data.PlayerUnits.Count; i++)
        {
            PlayerUnitData unit = data.PlayerUnits[i];

            Debug.LogWarning($"创建敌方单位: {unit.UnitType} at ({unit.Position.x},{unit.Position.y}" +
                $" player ID:{unit.PlayerUnitDataSO.playerID}) unit ID:{unit.PlayerUnitDataSO.pieceID}");

            if (otherPlayersUnits[playerId].ContainsKey(unit.Position))
            {
                Debug.Log($"单位已存在，跳过创建");
                continue;
            }


            CreateEnemyUnit(playerId, unit);
        }
    }

    // *************************
    //        私有函数
    // *************************

    // 获得初始领地
    private void GetStartWall(int cellID)
    {
        List<int> pos = GameManage.Instance.GetBoardNineSquareGrid(cellID);
        foreach (var i in pos)
        {
            if (_HexGrid.GetCell(i).enabled)
                _HexGrid.GetCell(i).Walled = true;

            PlayerDataManager.Instance.GetPlayerData(localPlayerId).AddOwnedCell(i);
        }
    }

    // 在指定的格子创建单位实例
    private void CreateUnitAtPosition(CardType unitType, int cellId)
    {
        BoardInfor cellInfo = GameManage.Instance.GetBoardInfor(cellId);
        int2 position = cellInfo.Cells2DPos;
        Vector3 worldPos = new Vector3(
            cellInfo.Cells3DPos.x,
           cellInfo.Cells3DPos.y + 2.5f,
           cellInfo.Cells3DPos.z
        );


        // 选择对应的预制体
        //Piece prefab = null;
        PieceType pieceType = PieceType.None;
        switch (unitType)
        {
            case CardType.Farmer:
                pieceType = PieceType.Farmer;

                break;
            case CardType.Solider:
                pieceType = PieceType.Military;
                break;
            case CardType.Missionary:
                pieceType = PieceType.Missionary;
                break;
            case CardType.Pope:
                pieceType = PieceType.Pope;
                GetStartWall(cellId);
                break;
            default:
                Debug.LogError($"未知的单位类型: {unitType}");
                return;
        }

        syncPieceData unitData = (syncPieceData)PieceManager.Instance.CreatePiece(pieceType,
        SceneStateManager.Instance.PlayerReligion,
        GameManage.Instance.LocalPlayerID,
        worldPos);


        GameObject pieceObj = PieceManager.Instance.GetPieceGameObject();

        // 添加描边效果
        pieceObj.AddComponent<ChangeMaterial>();


        // 添加到数据管理器
        PlayerDataManager.Instance.AddUnit(localPlayerId, unitType, position, unitData);

        // 保存本地引用
        localPlayerUnits[position] = pieceObj;
        GameManage.Instance.SetCellObject(position, pieceObj);

        Debug.Log($"在ID:  ({cellId}) 创建了 {unitType}");


        // 发送网络消息
        if (GameManage.Instance._NetGameSystem != null)
        {
            GameManage.Instance._NetGameSystem.SendUnitAddMessage(
                localPlayerId,
                unitType,
                position,
                unitData,
                false
            );
        }
        //PlayerDataManager.Instance.GetUnitPos(unitData.pieceID);
    }

    // *************************
    //         单位相关
    // *************************

    // 创建玩家教皇
    private void CreatePlayerPope(int startBoardID)
    {
        // 清空现有单位
        foreach (var a in localPlayerUnits.Values)
        {
            if (a != null)
                Destroy(a);
        }
        localPlayerUnits.Clear();

        CreateUnitAtPosition(CardType.Pope, startBoardID);


        GameManage.Instance._GameCamera.GetPlayerPosition(GameManage.Instance.FindCell(startBoardID).Cells3DPos);

    }

    // 单位使用技能
    public void UnitUseSkill(CardType type)
    {
        switch (type)
        {
            case CardType.Farmer:

                break;

            case CardType.Missionary:

                break;


        }

    }
    // 创建敌方单位
    private void CreateEnemyUnit(int playerId, PlayerUnitData unitData)
    {
        if (!PlayerBoardInforDict.ContainsKey(0))
        {
            Debug.LogWarning("棋盘信息未初始化");
            return;
        }

        // 找到对应位置的世界坐标
        Vector3 worldPos = Vector3.zero;
        foreach (var board in PlayerBoardInforDict.Values)
        {
            if (board.Cells2DPos.Equals(unitData.Position))
            {
                worldPos = board.Cells3DPos;
                break;
            }
        }

        // 选择预制体
        PieceManager.Instance.CreateEnemyPiece(unitData.PlayerUnitDataSO);

        GameObject prefab = PieceManager.Instance.GetPieceGameObject();
        if (prefab == null)
            prefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);

        GameObject unit = Instantiate(prefab, worldPos, prefab.transform.rotation);
        unit.transform.position = new Vector3(
            unit.transform.position.x,
            unit.transform.position.y + 6.5f,
            unit.transform.position.z
        );



        // 保存引用
        if (!otherPlayersUnits.ContainsKey(playerId))
        {
            otherPlayersUnits[playerId] = new Dictionary<int2, GameObject>();
        }
        otherPlayersUnits[playerId][unitData.Position] = unit;
        GameManage.Instance.SetCellObject(unitData.Position, unit);

        //Debug.Log("开始查询敌方单位位置");
        //PlayerDataManager.Instance.GetUnitPos(unitData.UnitID);

    }

    // 移动到选择的棋盘
    private void MoveToSelectCell(int targetCellId)
    {
        if (SelectingUnit == null) return;

        bCanContinue = false;

        // 获取起始和目标位置
        int2 fromPos = PlayerBoardInforDict[LastSelectingCellID].Cells2DPos;
        int2 toPos = PlayerBoardInforDict[targetCellId].Cells2DPos;

        PlayerUnitData? unitData = PlayerDataManager.Instance.FindUnit(localPlayerId, fromPos);
        Debug.Log("now unit AP is " + PieceManager.Instance.GetPieceAP(unitData.Value.UnitID));
        if (PieceManager.Instance.GetPieceAP(unitData.Value.UnitID) > 0)
        {
            _HexGrid.FindPath(LastSelectingCellID, targetCellId, PieceManager.Instance.GetPieceAP(unitData.Value.UnitID));
        }
        else
        {
            Debug.Log("该单位AP不足！");
            bCanContinue = true;
            return;
        }

        if (_HexGrid.HasPath)
        {
            List<HexCell> listCellPos = _HexGrid.GetPathCells();
            if (listCellPos.Count - 1 > PieceManager.Instance.GetPieceAP(unitData.Value.UnitID))
            {
                Debug.Log("AP不足");
                _HexGrid.ClearPath();
                bCanContinue = true;
                return;
            }
            Sequence moveSequence = DOTween.Sequence();
            Vector3 currentPos = SelectingUnit.transform.position;
            for (int i = 0; i < listCellPos.Count; i++)
            {
                // 根据路径坐标找到对应的格子信息
                Vector3 waypoint = new Vector3(
                   listCellPos[i].Position.x,
                   listCellPos[i].Position.y + 2.5f,
                    listCellPos[i].Position.z
                    );

                // 计算弧形路径的中间点
                Vector3 midPoint = (currentPos + waypoint) / 2f;
                midPoint.y += 5.0f;

                // 创建从当前位置到目标位置的弧形路径
                Vector3[] path = new Vector3[] { currentPos, midPoint, waypoint };

                moveSequence.Append(SelectingUnit.transform.DOPath(path, MoveSpeed, PathType.CatmullRom)
                  .SetEase(Ease.Linear));
                currentPos = waypoint;
                //Debug.Log("2Dpos is " + PlayerBoardInforDict[i].Cells2DPos+
                //    "3Dpos is "+ PlayerBoardInforDict[i].Cells3DPos);
            }
            moveSequence.OnComplete(() =>
            {
                // 动画完成后更新数据
                bCanContinue = true;

                // 更新本地数据
                PlayerDataManager.Instance.MoveUnit(localPlayerId, fromPos, toPos);

                // 更新本地引用
                localPlayerUnits.Remove(fromPos);
                localPlayerUnits[toPos] = SelectingUnit;

                // 更新格子上是否有单位


                // 更新GameManage的格子对象
                GameManage.Instance.MoveCellObject(fromPos, toPos);

                LastSelectingCellID = targetCellId;

                _HexGrid.ClearPath();

                // ============= 移动消耗AP逻辑 ============
                PlayerUnitData? unitData = PlayerDataManager.Instance.FindUnit(localPlayerId, toPos);
                if (unitData.HasValue)
                {
                    int pieceID = unitData.Value.PlayerUnitDataSO.pieceID;

                    // 消耗AP
                    bool apConsumed = PieceManager.Instance.ConsumePieceAP(pieceID, listCellPos.Count - 1);

                    if (apConsumed)
                    {
                        Debug.Log($"[移动] 单位 PieceID:{pieceID} 消耗{listCellPos.Count - 1} AP");

                        // 检查AP是否为0
                        Piece piece = PieceManager.Instance.GetPiece(pieceID);
                        if (piece != null && piece.CurrentAP <= 0)
                        {
                            PlayerDataManager.Instance.UpdateUnitCanDoActionByPos(localPlayerId, toPos, false);
                            Debug.Log($"[移动] 单位 PieceID:{pieceID} AP为0，bCanDoAction设置为false");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[移动] 单位 PieceID:{pieceID} AP消耗失败");
                    }
                }


                // 发送网络消息 - 移动
                if (GameManage.Instance._NetGameSystem != null)
                {
                    UnitMoveMessage moveMsg = new UnitMoveMessage
                    {
                        PlayerId = localPlayerId,
                        FromX = fromPos.x,
                        FromY = fromPos.y,
                        ToX = toPos.x,
                        ToY = toPos.y
                    };
                    GameManage.Instance._NetGameSystem.SendMessage(NetworkMessageType.UNIT_MOVE, moveMsg);
                    Debug.Log($"[本地] 已发送移动消息到网络: ({fromPos.x},{fromPos.y}) -> ({toPos.x},{toPos.y})");
                }
            });
        }
        else
        {
            _HexGrid.ClearPath();
            bCanContinue = true;
        }

    }

    // 取消选择单位的描边
    private void ReturnToDefault()
    {
        if (SelectingUnit != null)
        {
            var changeMaterial = SelectingUnit.GetComponent<ChangeMaterial>();
            if (changeMaterial != null)
            {
                changeMaterial.Default();
            }
            Debug.Log("unit name: " + SelectingUnit.name);
        }
    }
    private void ChooseEmptyCell(int cell)
    {
        _HexGrid.GetCell(cell).EnableHighlight(Color.red);
    }

    // *************************
    //      单位动作相关
    // *************************

    // 生成建筑
    private void CreateBuilding()
    {

    }

    // 检查指定位置的单位是否有足够的AP执行动作
    private bool CheckUnitHasEnoughAP(int2 position, int requiredAP = 1)
    {
        // 获取单位数据
        PlayerUnitData? unitData = PlayerDataManager.Instance.FindUnit(localPlayerId, position);

        if (!unitData.HasValue)
        {
            Debug.LogWarning($"[AP检查] 找不到位置 ({position.x},{position.y}) 的单位");
            return false;
        }

        // 获取pieceID并检查AP
        int pieceID = unitData.Value.PlayerUnitDataSO.pieceID;
        int currentAP = PieceManager.Instance.GetPieceAP(pieceID);

        if (currentAP < requiredAP)
        {
            Debug.Log($"[AP检查] 单位 PieceID:{pieceID} AP不足 (当前:{currentAP}, 需要:{requiredAP})");
            //ShowAPInsufficientMessage($"AP不足！当前AP: {currentAP}，需要: {requiredAP}");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 检查两个位置是否相邻（六边形格子的六个方向）
    /// </summary>
    /// <param name="pos1">位置1</param>
    /// <param name="pos2">位置2</param>
    /// <returns>如果相邻返回true</returns>
    private bool IsAdjacentPosition(int2 a, int2 b)
    {
        // 计算差值
        int d = _HexGrid.GetCell(a.x, a.y).Coordinates.DistanceTo(_HexGrid.GetCell(b.x, b.y).Coordinates);

        //Debug.Log("攻击者位置 " + a + " 被攻击者位置" + b+" 距离: "+d);
        if (d <= 1)
            return true;
        else
            return false;


    }

    // ============================================
    // 新增方法4：ExecuteAttack
    // 在当前位置执行攻击（必须已经在攻击范围内）
    // ============================================

    /// <summary>
    /// 在当前位置执行攻击，目标必须在相邻格
    /// </summary>
    /// <param name="targetPos">目标位置</param>
    /// <param name="targetCellId">目标格子ID</param>
    private void ExecuteAttack(int2 targetPos, int targetCellId)
    {
        if (SelectingUnit == null) return;

        // 获取攻击者位置
        int2 attackerPos = PlayerBoardInforDict[LastSelectingCellID].Cells2DPos;

        //  关键检查：必须在相邻格才能攻击
        if (!IsAdjacentPosition(attackerPos, targetPos))
        {
            Debug.LogError("[ExecuteAttack] 错误：目标不在攻击范围内！");
            bCanContinue = true;
            return;
        }

        // 获取目标拥有者
        int targetOwnerId = PlayerDataManager.Instance.GetUnitOwner(targetPos);

        // 获取攻击者数据
        PlayerUnitData? attackerData = PlayerDataManager.Instance.FindUnit(localPlayerId, attackerPos);
        if (!attackerData.HasValue)
        {
            Debug.LogError("[ExecuteAttack] 找不到攻击者数据");
            bCanContinue = true;
            return;
        }

        // 获取目标数据
        PlayerUnitData? targetData = PlayerDataManager.Instance.FindUnit(targetOwnerId, targetPos);
        if (!targetData.HasValue)
        {
            Debug.LogError("[ExecuteAttack] 找不到目标数据");
            bCanContinue = true;
            return;
        }



        // 获取目标GameObject
        GameObject targetUnit = null;
        if (otherPlayersUnits.ContainsKey(targetOwnerId) &&
            otherPlayersUnits[targetOwnerId].ContainsKey(targetPos))
        {
            targetUnit = otherPlayersUnits[targetOwnerId][targetPos];
        }

        // 获取双方的 PieceID
        int attackerPieceID = attackerData.Value.PlayerUnitDataSO.pieceID;
        int targetPieceID = targetData.Value.PlayerUnitDataSO.pieceID;

        Debug.Log($"[ExecuteAttack] 战斗开始 - 攻击者ID:{attackerPieceID} 攻击 目标ID:{targetPieceID}");

        //  在移动完成后才计算战斗结果
        syncPieceData? targetSyncData = PieceManager.Instance.AttackEnemy(attackerPieceID, targetPieceID);

        if (!targetSyncData.HasValue)
        {
            Debug.LogError("[ExecuteAttack] PieceManager.AttackEnemy 调用失败！");
            bCanContinue = true;
            return;
        }

        bool updateSuccess = PlayerDataManager.Instance.UpdateUnitSyncDataByPos(
                targetOwnerId,
                targetPos,
                targetSyncData.Value);

        if (updateSuccess)
        {
            Debug.Log($"[ExecuteAttack] ✓ 已同步目标HP到PlayerDataManager: {targetSyncData.Value.currentHP}");
        }
        else
        {
            Debug.LogError($"[ExecuteAttack] ✗ 同步目标HP失败！");
        }


        // 消耗攻击者的AP（攻击消耗1 AP）
        //PieceManager.Instance.ConsumePieceAP(attackerPieceID, 1);

        // 判断目标是否死亡
        bool targetDied = targetSyncData.Value.currentHP <= 0;
        Debug.Log($"[ExecuteAttack] 攻击完成 - 目标剩余HP: {targetSyncData.Value.currentHP}, 是否死亡: {targetDied} ,单位剩余行动力: {PieceManager.Instance.GetPieceAP(attackerPieceID)}");

        if (targetDied)
        {
            // 目标死亡，攻击者前进到目标位置
            Debug.Log("[ExecuteAttack] 目标死亡，攻击者前进到目标位置");
            ExecuteMoveToDeadTargetPosition(attackerPos, targetPos, targetCellId, targetUnit, targetOwnerId);
        }
        else
        {
            // 目标存活，停在当前位置
            Debug.Log("[ExecuteAttack] 目标存活，攻击者停留在当前位置");

            // 播放受击动画
            if (targetUnit != null)
            {
                // 震动效果
                targetUnit.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 5);

                // 可选：显示伤害数字
                // ShowDamageNumber(targetUnit.transform.position, damage);
            }

            // 网络同步攻击
            SyncLocalUnitAttack(attackerPos, targetPos, targetOwnerId, false);

            bCanContinue = true;
        }
    }

    // ============================================
    // ExecuteCharm - 传教士魅惑敌方单位
    // ============================================
    private void ExecuteCharm(int2 targetPos, int targetOwnerId)
    {
        if (SelectingUnit == null) return;

        // 获取传教士位置
        int2 missionaryPos = PlayerBoardInforDict[LastSelectingCellID].Cells2DPos;

        // 必须在相邻格才能魅惑
        if (!IsAdjacentPosition(missionaryPos, targetPos))
        {
            Debug.LogError("[ExecuteCharm] 错误：目标不在魅惑范围内！");
            bCanContinue = true;
            return;
        }

        // 获取传教士数据
        PlayerUnitData? missionaryData = PlayerDataManager.Instance.FindUnit(localPlayerId, missionaryPos);
        if (!missionaryData.HasValue)
        {
            Debug.LogError("[ExecuteCharm] 找不到传教士数据");
            bCanContinue = true;
            return;
        }

        // 获取目标数据
        PlayerUnitData? targetData = PlayerDataManager.Instance.FindUnit(targetOwnerId, targetPos);
        if (!targetData.HasValue)
        {
            Debug.LogError("[ExecuteCharm] 找不到目标数据");
            bCanContinue = true;
            return;
        }

        // 获取双方的 PieceID
        int missionaryPieceID = missionaryData.Value.PlayerUnitDataSO.pieceID;
        int targetPieceID = targetData.Value.PlayerUnitDataSO.pieceID;

        Debug.Log($"[ExecuteCharm] 魅惑尝试 - 传教士ID:{missionaryPieceID} 魅惑 目标ID:{targetPieceID}");

        // 调用PieceManager的ConvertEnemy方法
        syncPieceData? convertResult = PieceManager.Instance.ConvertEnemy(missionaryPieceID, targetPieceID);

        if (!convertResult.HasValue)
        {
            Debug.Log("[ExecuteCharm] 魅惑失败！");
            bCanContinue = true;
            return;
        }

        Debug.Log("[ExecuteCharm] 魅惑成功！创建新的我方单位: "+ convertResult.Value.piecetype);

        // 获取目标GameObject
        GameObject targetUnit = null;
        if (otherPlayersUnits.ContainsKey(targetOwnerId) &&
            otherPlayersUnits[targetOwnerId].ContainsKey(targetPos))
        {
            targetUnit = otherPlayersUnits[targetOwnerId][targetPos];
        }

        // 1. 消灭敌方单位（从敌方玩家移除）
        PlayerDataManager.Instance.RemoveUnit(targetOwnerId, targetPos);

        // 销毁敌方单位GameObject
        if (targetUnit != null)
        {
            // 播放消失动画
            targetUnit.transform.DOScale(0f, 0.5f).OnComplete(() =>
            {
                if (otherPlayersUnits.ContainsKey(targetOwnerId))
                {
                    otherPlayersUnits[targetOwnerId].Remove(targetPos);
                }
                Destroy(targetUnit);
            });
        }

        // 2. 在该位置创建新的我方单位（使用返回的syncPieceData）
        syncPieceData newUnitData = convertResult.Value;
        newUnitData.playerID = localPlayerId; // 设置为本地玩家

        // 使用PieceManager创建新的己方棋子
        bool createSuccess = PieceManager.Instance.CreateEnemyPiece(newUnitData);

        if (createSuccess)
        {
            // 获取创建的GameObject
            GameObject newUnitObj = PieceManager.Instance.GetPieceGameObject();

            if (newUnitObj != null)
            {
                // 添加到本地玩家单位字典
                localPlayerUnits[targetPos] = newUnitObj;

                // 更新GameManage的格子对象
                GameManage.Instance.SetCellObject(targetPos, newUnitObj);

                // 3. 将新单位标记为被魅惑状态，并添加到PlayerDataManager
                CardType unitType = ConvertPieceTypeToCardType(newUnitData.piecetype);

                // 添加到PlayerDataManager（先使用标准方法添加）
                int newUnitID = PlayerDataManager.Instance.AddUnit(
                    localPlayerId,
                    unitType,
                    targetPos,
                    newUnitData,
                    null,  // GameObject由PieceManager管理
                    false  // isUsed
                );

                // 然后设置魅惑状态
                if (newUnitID >= 0)
                {
                    PlayerDataManager.Instance.SetUnitCharmed(localPlayerId, targetPos, targetOwnerId, 3);
                    Debug.Log($"[ExecuteCharm] 创建被魅惑单位成功 - 新单位ID:{newUnitID}, 原所有者:{targetOwnerId}, 剩余回合:3");
                }

                // 4. 网络同步魅惑操作
                SyncLocalUnitCharm(missionaryPieceID, missionaryPos, targetPieceID, targetOwnerId, targetPos, newUnitData);

                // 播放魅惑特效（可选）
                if (newUnitObj != null)
                {
                    newUnitObj.transform.DOPunchScale(Vector3.one * 0.3f, 0.5f, 5);
                }
            }
            else
            {
                Debug.LogError("[ExecuteCharm] 无法获取创建的GameObject");
            }
        }
        else
        {
            Debug.LogError("[ExecuteCharm] PieceManager.CreateEnemyPiece 失败");
        }

        bCanContinue = true;
    }

    // ============================================
    // 新增方法5：ExecuteMoveToDeadTargetPosition
    // 目标死亡后，攻击者前进到目标位置
    // ============================================

    /// <summary>
    /// 目标死亡后，攻击者前进一格到目标位置
    /// </summary>
    private void ExecuteMoveToDeadTargetPosition(
        int2 fromPos,
        int2 toPos,
        int targetCellId,
        GameObject targetUnit,
        int targetOwnerId)
    {
        // 计算移动路径（只有一格的距离）
        Vector3 startPos = SelectingUnit.transform.position;
        Vector3 targetWorldPos = GameManage.Instance.FindCell(targetCellId).Cells3DPos;
        targetWorldPos.y += 2.5f;

        // 创建移动动画
        Sequence moveSequence = DOTween.Sequence();

        // 弧形路径
        Vector3 midPoint = (startPos + targetWorldPos) / 2f;
        midPoint.y += 5.0f;
        Vector3[] path = new Vector3[] { startPos, midPoint, targetWorldPos };

        moveSequence.Append(SelectingUnit.transform.DOPath(path, MoveSpeed, PathType.CatmullRom)
            .SetEase(Ease.Linear));

        // 同时播放目标死亡动画
        if (targetUnit != null)
        {
            // 缩放到0
            moveSequence.Join(targetUnit.transform.DOScale(0f, 0.5f));

            // 旋转消失
            moveSequence.Join(targetUnit.transform.DORotate(
                new Vector3(0, 360, 0),
                0.5f,
                RotateMode.FastBeyond360
            ));
        }

        // 动画完成后的处理
        moveSequence.OnComplete(() =>
        {
            // 1发送攻击消息（通知目标被击杀）
            SyncLocalUnitAttack(fromPos, toPos, targetOwnerId, true);
            Debug.Log($"[ExecuteMoveToDeadTargetPosition] 已发送攻击消息：击杀目标 at ({toPos.x},{toPos.y})");

            //// 发送移动消息（通知攻击者移动到目标位置）
            //SyncLocalUnitMove(fromPos, toPos);
            //Debug.Log($"[ExecuteMoveToDeadTargetPosition] 已发送移动消息：攻击者 ({fromPos.x},{fromPos.y}) → ({toPos.x},{toPos.y})");



            // 销毁目标GameObject
            if (targetUnit != null)
            {
                Destroy(targetUnit);
            }

            // 先获取目标数据
            PlayerUnitData? targetData = PlayerDataManager.Instance.FindUnit(targetOwnerId, toPos);

            // 从PieceManager移除目标
            if (PieceManager.Instance.DoesPieceExist(targetData.Value.UnitID))
            {
                PieceManager.Instance.RemovePiece(targetData.Value.UnitID);
                Debug.Log($"[ExecuteMoveToDeadTargetPosition] 已从PieceManager移除目标 ID:{targetData.Value.UnitID}");
            }
            else
            {
                Debug.LogWarning($"[ExecuteMoveToDeadTargetPosition] 找不到目标单位数据 at ({toPos.x},{toPos.y})");
            }
            // 3. 从PlayerDataManager移除目标数据
            PlayerDataManager.Instance.RemoveUnit(targetOwnerId, toPos);


            // 移动攻击者数据
            PlayerDataManager.Instance.MoveUnit(localPlayerId, fromPos, toPos);

            // 更新本地单位字典
            localPlayerUnits.Remove(fromPos);
            localPlayerUnits[toPos] = SelectingUnit;

            // 从目标玩家的单位字典中移除
            if (otherPlayersUnits.ContainsKey(targetOwnerId))
            {
                otherPlayersUnits[targetOwnerId].Remove(toPos);
            }

            // 更新GameManage的格子对象
            GameManage.Instance.MoveCellObject(fromPos, toPos);

            // 更新选中的格子ID
            LastSelectingCellID = targetCellId;



            Debug.Log($"[ExecuteAttack] 击杀目标，移动到目标位置: ({toPos.x},{toPos.y})");

            bCanContinue = true;
        });
    }


    public void HandleNetworkAttack(UnitAttackMessage msg)
    {
        if (msg.AttackerPlayerId == localPlayerId)
        {
            Debug.Log("[网络攻击] 这是本地玩家的攻击，已处理");
            return;
        }

        int2 attackerPos = new int2(msg.AttackerPosX, msg.AttackerPosY);
        int2 targetPos = new int2(msg.TargetPosX, msg.TargetPosY);

        Debug.Log($"[网络攻击] 玩家 {msg.AttackerPlayerId} 攻击 玩家 {msg.TargetPlayerId}");
        Debug.Log($"[网络攻击] 攻击者位置: ({attackerPos.x},{attackerPos.y}), 目标位置: ({targetPos.x},{targetPos.y})");

        // 获取攻击者GameObject
        GameObject attackerObj = null;
        int2 attackerOriginalPos = attackerPos; // 默认原始位置就是当前位置

        // 【修复】首先尝试从原始位置查找攻击者（可能还没移动）
        // 检查消息中是否包含原始位置信息
        if (msg.AttackerOriginalPosX != msg.AttackerPosX || msg.AttackerOriginalPosY != msg.AttackerPosY)
        {
            // 这是一个"移动+攻击"场景
            attackerOriginalPos = new int2(msg.AttackerOriginalPosX, msg.AttackerOriginalPosY);
            Debug.Log($"[网络攻击] 检测到移动+攻击，原始位置: ({attackerOriginalPos.x},{attackerOriginalPos.y})");

            // 从原始位置获取攻击者对象
            if (otherPlayersUnits.ContainsKey(msg.AttackerPlayerId) &&
                otherPlayersUnits[msg.AttackerPlayerId].ContainsKey(attackerOriginalPos))
            {
                attackerObj = otherPlayersUnits[msg.AttackerPlayerId][attackerOriginalPos];
            }
        }
        else
        {
            // 这是一个"直接攻击"场景（攻击者已经在攻击范围内）
            Debug.Log($"[网络攻击] 直接攻击，无需移动");

            if (otherPlayersUnits.ContainsKey(msg.AttackerPlayerId) &&
                otherPlayersUnits[msg.AttackerPlayerId].ContainsKey(attackerPos))
            {
                attackerObj = otherPlayersUnits[msg.AttackerPlayerId][attackerPos];
            }
        }

        if (attackerObj == null)
        {
            Debug.LogWarning($"[网络攻击] 找不到攻击者GameObject");
            return;
        }

        // 获取目标GameObject
        GameObject targetObj = null;
        if (msg.TargetPlayerId == localPlayerId && localPlayerUnits.ContainsKey(targetPos))
        {
            targetObj = localPlayerUnits[targetPos];
        }
        else if (otherPlayersUnits.ContainsKey(msg.TargetPlayerId) &&
                 otherPlayersUnits[msg.TargetPlayerId].ContainsKey(targetPos))
        {
            targetObj = otherPlayersUnits[msg.TargetPlayerId][targetPos];
        }

        // ========================================
        // 【修复】根据场景执行不同的动画流程
        // ========================================

        if (attackerOriginalPos.x != attackerPos.x || attackerOriginalPos.y != attackerPos.y)
        {
            // ===== 场景1：移动+攻击 =====
            Debug.Log($"[网络攻击] 执行移动+攻击动画");

            // 先播放移动动画
            PlayMoveAnimationForAttack(
                attackerObj,
                msg.AttackerPlayerId,
                attackerOriginalPos,
                attackerPos,
                () => {
                    // 移动完成后播放攻击效果
                    Debug.Log($"[网络攻击] 移动完成，播放攻击效果");

                    if (msg.TargetDestroyed)
                    {
                        // 目标死亡，攻击者继续前进到目标位置
                        HandleTargetDestroyedAfterAttack(
                            attackerObj,
                            msg.AttackerPlayerId,
                            attackerPos,
                            targetPos,
                            targetObj,
                            msg.TargetPlayerId
                        );
                    }
                    else
                    {
                        // 目标存活，只播放受击动画
                        HandleTargetSurvivedAfterAttack(targetObj, msg);
                    }
                }
            );
        }
        else
        {
            // ===== 场景2：直接攻击（不需要移动） =====
            Debug.Log($"[网络攻击] 执行直接攻击");

            // 播放攻击动画（可选）
            // PlayAttackAnimation(attackerObj);

            if (msg.TargetDestroyed)
            {
                // 目标死亡，攻击者前进到目标位置
                HandleTargetDestroyedAfterAttack(
                    attackerObj,
                    msg.AttackerPlayerId,
                    attackerPos,
                    targetPos,
                    targetObj,
                    msg.TargetPlayerId
                );
            }
            else
            {
                // 目标存活，只播放受击动画
                HandleTargetSurvivedAfterAttack(targetObj, msg);
            }
        }

        Debug.Log($"[网络攻击] 攻击处理完成");
    }

    // ========================================
    // 辅助方法1：播放移动到攻击位置的动画
    // ========================================
    private void PlayMoveAnimationForAttack(
        GameObject attackerObj,
        int attackerPlayerId,
        int2 fromPos,
        int2 toPos,
        System.Action onComplete)
    {
        // 获取目标世界坐标
        Vector3 targetWorldPos = GameManage.Instance.GetCell2D(toPos).Cells3DPos;
        targetWorldPos.y += 2.5f;

        // 更新字典：从原位置移除，添加到新位置
        if (attackerPlayerId == localPlayerId)
        {
            localPlayerUnits.Remove(fromPos);
            localPlayerUnits[toPos] = attackerObj;
        }
        else if (otherPlayersUnits.ContainsKey(attackerPlayerId))
        {
            otherPlayersUnits[attackerPlayerId].Remove(fromPos);
            otherPlayersUnits[attackerPlayerId][toPos] = attackerObj;
        }

        // 更新GameManage的格子对象
        GameManage.Instance.SetCellObject(fromPos, null);
        GameManage.Instance.SetCellObject(toPos, attackerObj);

        // 播放移动动画（可以使用弧形路径）
        Vector3 startPos = attackerObj.transform.position;
        Vector3 midPoint = (startPos + targetWorldPos) / 2f;
        midPoint.y += 5.0f;

        Sequence moveSeq = DOTween.Sequence();
        moveSeq.Append(attackerObj.transform.DOPath(
            new Vector3[] { startPos, midPoint, targetWorldPos },
            MoveSpeed,
            PathType.CatmullRom
        ).SetEase(Ease.Linear));

        moveSeq.OnComplete(() =>
        {
            Debug.Log($"[PlayMoveAnimationForAttack] 移动动画完成");
            onComplete?.Invoke();
        });
    }

    // ========================================
    // 辅助方法2：处理目标死亡的情况
    // ========================================
    private void HandleTargetDestroyedAfterAttack(
        GameObject attackerObj,
        int attackerPlayerId,
        int2 attackerCurrentPos,
        int2 targetPos,
        GameObject targetObj,
        int targetPlayerId)
    {
        Debug.Log($"[HandleTargetDestroyedAfterAttack] 目标被击杀，攻击者前进到目标位置");

        // 移除目标GameObject
        if (targetObj != null)
        {
            // 播放死亡动画（可选）
            targetObj.transform.DOScale(0f, 0.3f).OnComplete(() =>
            {
                if (targetPlayerId == localPlayerId && localPlayerUnits.ContainsKey(targetPos))
                {
                    localPlayerUnits.Remove(targetPos);
                }
                else if (otherPlayersUnits.ContainsKey(targetPlayerId) &&
                         otherPlayersUnits[targetPlayerId].ContainsKey(targetPos))
                {
                    otherPlayersUnits[targetPlayerId].Remove(targetPos);
                }

                Destroy(targetObj);
            });
        }

        // 攻击者前进到目标位置
        Vector3 targetWorldPos = GameManage.Instance.GetCell2D(targetPos).Cells3DPos;
        targetWorldPos.y += 2.5f;

        // 更新字典
        if (attackerPlayerId == localPlayerId)
        {
            localPlayerUnits.Remove(attackerCurrentPos);
            localPlayerUnits[targetPos] = attackerObj;
        }
        else if (otherPlayersUnits.ContainsKey(attackerPlayerId))
        {
            otherPlayersUnits[attackerPlayerId].Remove(attackerCurrentPos);
            otherPlayersUnits[attackerPlayerId][targetPos] = attackerObj;
        }

        // 更新GameManage
        GameManage.Instance.SetCellObject(attackerCurrentPos, null);
        GameManage.Instance.SetCellObject(targetPos, attackerObj);

        // 播放前进动画
        attackerObj.transform.DOMove(targetWorldPos, MoveSpeed * 0.5f).OnComplete(() =>
        {
            Debug.Log($"[HandleTargetDestroyedAfterAttack] 攻击者前进动画完成");
        });

        // 从PieceManager中移除被击杀的目标
        PlayerUnitData? deadTargetData = PlayerDataManager.Instance.FindUnit(targetPlayerId, targetPos);
        if (deadTargetData.HasValue && PieceManager.Instance != null)
        {
            PieceManager.Instance.RemovePiece(deadTargetData.Value.UnitID);
            Debug.Log($"[HandleTargetDestroyedAfterAttack] 已从PieceManager移除被击杀单位 ID:{deadTargetData.Value.UnitID}");
        }
    }

    // ========================================
    // 辅助方法3：处理目标存活的情况
    // ========================================
    private void HandleTargetSurvivedAfterAttack(GameObject targetObj, UnitAttackMessage msg)
    {
        Debug.Log($"[HandleTargetSurvivedAfterAttack] 目标存活，当前HP: {msg.TargetSyncData?.currentHP ?? 0}");

        // 更新目标HP数据
        if (msg.TargetSyncData.HasValue)
        {
            int2 targetPos = new int2(msg.TargetPosX, msg.TargetPosY);
            bool updateSuccess = PlayerDataManager.Instance.UpdateUnitSyncDataByPos(
                msg.TargetPlayerId,
                targetPos,
                msg.TargetSyncData.Value
            );

            if (updateSuccess)
            {
                Debug.Log($"[HandleTargetSurvivedAfterAttack] ✓ 已更新目标HP: {msg.TargetSyncData.Value.currentHP}");
            }
        }

        // 播放受击动画
        if (targetObj != null)
        {
            // 震动效果
            targetObj.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 5);

            // 如果有血条UI，更新血条显示
            // UpdateUnitHPDisplay(targetPos, msg.TargetSyncData.Value.currentHP);
        }
    }


    // 处理来自网络的魅惑消息
    public void HandleNetworkCharm(UnitCharmMessage msg)
    {
        if (msg.MissionaryPlayerId == localPlayerId)
        {
            Debug.Log("[网络魅惑] 这是本地玩家的魅惑，已处理");
            return;
        }

        int2 missionaryPos = new int2(msg.MissionaryPosX, msg.MissionaryPosY);
        int2 targetPos = new int2(msg.TargetPosX, msg.TargetPosY);

        Debug.Log($"[网络魅惑] 玩家 {msg.MissionaryPlayerId} 魅惑单位 at ({targetPos.x},{targetPos.y})");

        // 1. 获取并销毁目标单位GameObject
        GameObject targetUnit = null;

        if (msg.TargetPlayerId == localPlayerId && localPlayerUnits.ContainsKey(targetPos))
        {
            // 我的单位被魅惑了
            targetUnit = localPlayerUnits[targetPos];
            localPlayerUnits.Remove(targetPos);
        }
        else if (otherPlayersUnits.ContainsKey(msg.TargetPlayerId) &&
                 otherPlayersUnits[msg.TargetPlayerId].ContainsKey(targetPos))
        {
            // 其他玩家的单位被魅惑
            targetUnit = otherPlayersUnits[msg.TargetPlayerId][targetPos];
            otherPlayersUnits[msg.TargetPlayerId].Remove(targetPos);
        }

        // 销毁原单位GameObject
        if (targetUnit != null)
        {
            targetUnit.transform.DOScale(0f, 0.5f).OnComplete(() => Destroy(targetUnit));
        }

        // 2. 从原所有者的PlayerDataManager移除
        PlayerDataManager.Instance.RemoveUnit(msg.TargetPlayerId, targetPos);

        // 3. 创建新的被魅惑单位（属于魅惑者）
        bool createSuccess = PieceManager.Instance.CreateEnemyPiece(msg.NewUnitSyncData);

        if (createSuccess)
        {
            GameObject newUnitObj = PieceManager.Instance.GetPieceGameObject();

            if (newUnitObj != null)
            {
                // 添加到魅惑者的单位字典
                if (msg.MissionaryPlayerId == localPlayerId)
                {
                    localPlayerUnits[targetPos] = newUnitObj;
                }
                else
                {
                    if (!otherPlayersUnits.ContainsKey(msg.MissionaryPlayerId))
                    {
                        otherPlayersUnits[msg.MissionaryPlayerId] = new Dictionary<int2, GameObject>();
                    }
                    otherPlayersUnits[msg.MissionaryPlayerId][targetPos] = newUnitObj;
                }

                // 更新GameManage的格子对象
                GameManage.Instance.SetCellObject(targetPos, newUnitObj);

                // 添加到PlayerDataManager（魅惑者的单位列表）
                CardType unitType = ConvertPieceTypeToCardType(msg.NewUnitSyncData.piecetype);

                int newUnitID = PlayerDataManager.Instance.AddUnit(
                    msg.MissionaryPlayerId,
                    unitType,
                    targetPos,
                    msg.NewUnitSyncData,
                    null,
                    false
                );

                // 设置魅惑状态
                if (newUnitID >= 0)
                {
                    PlayerDataManager.Instance.SetUnitCharmed(
                        msg.MissionaryPlayerId,
                        targetPos,
                        msg.TargetPlayerId,
                        msg.CharmedTurns
                    );
                }

                Debug.Log($"[网络魅惑] 创建被魅惑单位成功 - 新所有者:{msg.MissionaryPlayerId}, 原所有者:{msg.TargetPlayerId}");

                // 播放魅惑特效
                if (newUnitObj != null)
                {
                    newUnitObj.transform.DOPunchScale(Vector3.one * 0.3f, 0.5f, 5);
                }
            }
        }
    }

    // 处理来自网络的魅惑过期消息
    public void HandleNetworkCharmExpire(CharmExpireMessage msg)
    {
        int2 pos = new int2(msg.PosX, msg.PosY);

        Debug.Log($"[网络魅惑过期] 单位 {msg.UnitID} 归还给玩家 {msg.OriginalOwnerId}");

        // 1. 从当前控制者移除单位
        GameObject unitObj = null;

        if (msg.CurrentOwnerId == localPlayerId && localPlayerUnits.ContainsKey(pos))
        {
            unitObj = localPlayerUnits[pos];
            localPlayerUnits.Remove(pos);
        }
        else if (otherPlayersUnits.ContainsKey(msg.CurrentOwnerId) &&
                 otherPlayersUnits[msg.CurrentOwnerId].ContainsKey(pos))
        {
            unitObj = otherPlayersUnits[msg.CurrentOwnerId][pos];
            otherPlayersUnits[msg.CurrentOwnerId].Remove(pos);
        }

        // 从PlayerDataManager移除
        PlayerDataManager.Instance.RemoveUnit(msg.CurrentOwnerId, pos);

        // 如果有GameObject，暂时销毁它，因为我们会通过PieceManager重新创建
        if (unitObj != null)
        {
            Destroy(unitObj);
        }

        // 2. 重新创建单位（更新playerID为原所有者）
        syncPieceData returnedData = msg.UnitSyncData;
        returnedData.playerID = msg.OriginalOwnerId;

        bool createSuccess = PieceManager.Instance.CreateEnemyPiece(returnedData);

        if (createSuccess)
        {
            GameObject newUnitObj = PieceManager.Instance.GetPieceGameObject();

            if (newUnitObj != null)
            {
                // 添加到原所有者的单位字典
                if (msg.OriginalOwnerId == localPlayerId)
                {
                    localPlayerUnits[pos] = newUnitObj;
                }
                else
                {
                    if (!otherPlayersUnits.ContainsKey(msg.OriginalOwnerId))
                    {
                        otherPlayersUnits[msg.OriginalOwnerId] = new Dictionary<int2, GameObject>();
                    }
                    otherPlayersUnits[msg.OriginalOwnerId][pos] = newUnitObj;
                }

                // 更新GameManage的格子对象
                GameManage.Instance.SetCellObject(pos, newUnitObj);

                // 添加到PlayerDataManager（归还给原所有者，不再是魅惑状态）
                CardType unitType = ConvertPieceTypeToCardType(returnedData.piecetype);

                PlayerDataManager.Instance.AddUnit(
                    msg.OriginalOwnerId,
                    unitType,
                    pos,
                    returnedData,
                    null,
                    false
                );
                // 魅惑状态已经在ReturnCharmedUnit中重置，这里不需要再设置

                Debug.Log($"[网络魅惑过期] 单位归还成功 - 原所有者:{msg.OriginalOwnerId}");
            }
        }
    }

    // 处理本地魅惑过期（不通过网络，直接在本地处理）
    public void HandleCharmExpireLocal(CharmExpireInfo expireInfo)
    {
        int2 pos = expireInfo.Position;

        Debug.Log($"[本地魅惑过期] 单位 {expireInfo.UnitID} 归还给玩家 {expireInfo.OriginalOwnerID}");

        // 1. 从当前控制者（本地玩家）移除单位GameObject
        GameObject unitObj = null;

        if (localPlayerUnits.ContainsKey(pos))
        {
            unitObj = localPlayerUnits[pos];
            localPlayerUnits.Remove(pos);
        }

        // 如果有GameObject，暂时销毁它
        if (unitObj != null)
        {
            Destroy(unitObj);
        }

        // 2. 重新创建单位（更新playerID为原所有者）
        syncPieceData returnedData = expireInfo.UnitData.PlayerUnitDataSO;
        returnedData.playerID = expireInfo.OriginalOwnerID;

        bool createSuccess = PieceManager.Instance.CreateEnemyPiece(returnedData);

        if (createSuccess)
        {
            GameObject newUnitObj = PieceManager.Instance.GetPieceGameObject();

            if (newUnitObj != null)
            {
                // 添加到原所有者的单位字典
                if (expireInfo.OriginalOwnerID == localPlayerId)
                {
                    localPlayerUnits[pos] = newUnitObj;
                }
                else
                {
                    if (!otherPlayersUnits.ContainsKey(expireInfo.OriginalOwnerID))
                    {
                        otherPlayersUnits[expireInfo.OriginalOwnerID] = new Dictionary<int2, GameObject>();
                    }
                    otherPlayersUnits[expireInfo.OriginalOwnerID][pos] = newUnitObj;
                }

                // 更新GameManage的格子对象
                GameManage.Instance.SetCellObject(pos, newUnitObj);

                Debug.Log($"[本地魅惑过期] 单位归还成功 - 原所有者:{expireInfo.OriginalOwnerID}");
            }
        }
    }



    // 处理来自网络的移动消息
    public void HandleNetworkMove(UnitMoveMessage msg)
    {
        // 如果是自己的移动，已经在本地处理过了
        if (msg.PlayerId == localPlayerId)
        {
            Debug.Log("[网络移动] 这是本地玩家的移动，已处理");
            return;
        }

        int2 fromPos = new int2(msg.FromX, msg.FromY);
        int2 toPos = new int2(msg.ToX, msg.ToY);

        Debug.Log($"[网络移动] 玩家 {msg.PlayerId} 移动: ({fromPos.x},{fromPos.y}) -> ({toPos.x},{toPos.y})");

        if (otherPlayersUnits.ContainsKey(msg.PlayerId) &&
      otherPlayersUnits[msg.PlayerId].ContainsKey(fromPos))
        {
            GameObject movingUnit = otherPlayersUnits[msg.PlayerId][fromPos];

            // 更新字典
            otherPlayersUnits[msg.PlayerId].Remove(fromPos);
            otherPlayersUnits[msg.PlayerId][toPos] = movingUnit;


            // 获取目标世界坐标
            Vector3 targetWorldPos = Vector3.zero;
            foreach (var board in PlayerBoardInforDict.Values)
            {
                if (board.Cells2DPos.Equals(toPos))
                {
                    targetWorldPos = new Vector3(
                        board.Cells3DPos.x,
                        board.Cells3DPos.y + 2.5f,
                        board.Cells3DPos.z
                    );
                    break;
                }
            }


            // 执行移动动画
            movingUnit.transform.DOMove(targetWorldPos, MoveSpeed).OnComplete(() =>
            {
                Debug.Log($"[HandleNetworkMove] 移动动画完成");
            });

            // 更新 GameManage
            GameManage.Instance.SetCellObject(fromPos, null);
            GameManage.Instance.SetCellObject(toPos, movingUnit);

            Debug.Log($"[HandleNetworkMove] 视觉移动完成: ({fromPos.x},{fromPos.y}) -> ({toPos.x},{toPos.y})");
        }
        else
        {
            Debug.LogWarning($"[HandleNetworkMove] 找不到要移动的单位: 玩家{msg.PlayerId} at ({fromPos.x},{fromPos.y})");
        }


    }

    // 处理来自网络的创建单位消息
    public void HandleNetworkAddUnit(UnitAddMessage msg)
    {
        int2 pos = new int2(msg.PosX, msg.PosY);
        CardType unitType = (CardType)msg.UnitType;

        Debug.Log($"[网络创建] 玩家 {msg.PlayerId} 创建单位: {unitType} at ({pos.x},{pos.y})");



        // 使用 PieceManager 创建敌方棋子
        if (PieceManager.Instance != null)
        {
            bool success = PieceManager.Instance.CreateEnemyPiece(msg.NewUnitSyncData);

            if (success)
            {
                // 获取创建的GameObject
                GameObject unitObj = PieceManager.Instance.GetPieceGameObject();

                if (unitObj != null)
                {
                    // 确保字典存在
                    if (!otherPlayersUnits.ContainsKey(msg.PlayerId))
                    {
                        otherPlayersUnits[msg.PlayerId] = new Dictionary<int2, GameObject>();
                    }

                    // 保存到其他玩家单位字典
                    otherPlayersUnits[msg.PlayerId][pos] = unitObj;

                    // 更新 GameManage 的格子对象
                    GameManage.Instance.SetCellObject(pos, unitObj);

                    Debug.Log($"[HandleNetworkAddUnit] 成功创建敌方单位 ID:{msg.NewUnitSyncData.pieceID}");

                    //Debug.Log("开始查询敌方单位位置");
                    //PlayerDataManager.Instance.GetUnitPos(msg.NewUnitSyncData.pieceID);

                }
                else
                {
                    Debug.LogError($"[HandleNetworkAddUnit] 无法获取创建的GameObject");
                }
            }
            else
            {
                Debug.LogError($"[HandleNetworkAddUnit] PieceManager.CreateEnemyPiece 失败");
            }
        }
        else
        {
            Debug.LogError($"[HandleNetworkAddUnit] PieceManager.Instance 为 null");
        }
        //// 更新数据
        //if (PlayerDataManager.Instance != null)
        //{
        //    PlayerDataManager.Instance.AddUnit(msg.PlayerId, unitType, pos,
        //        msg.NewUnitSyncData);
        //}

        //// 创建单位
        //int unitId = PlayerDataManager.Instance.GetUnitIDBy2DPos(pos);
        //PlayerUnitData unitData = new PlayerUnitData(unitId, unitType, pos,
        //   msg.NewUnitSyncData, msg.IsUsed);

        //CreateEnemyUnit(msg.PlayerId, unitData);

        //Debug.Log($"[网络创建] 完成");
    }

    // 操作同步管理
    /// <summary>
    /// 本地玩家移动单位后调用此方法进行网络同步
    /// 在 HandleRightClick 中移动完成后调用
    /// </summary>
    private void SyncLocalUnitMove(int2 fromPos, int2 toPos)
    {
        // 检查网络连接
        if (NetGameSystem.Instance == null || !NetGameSystem.Instance.bIsConnected)
        {
            return; // 单机模式或未连接，不发送
        }

        // 获取移动后的单位数据
        PlayerUnitData? unitData = PlayerDataManager.Instance.FindUnit(localPlayerId, toPos);

        if (unitData.HasValue)
        {
            // 发送网络消息
            NetGameSystem.Instance.SendUnitMoveMessage(
                localPlayerId,
                fromPos,
                toPos,
                unitData.Value.PlayerUnitDataSO
            );

            Debug.Log($"[SyncLocalUnitMove] 已发送移动同步消息");
        }
        else
        {
            Debug.LogWarning($"[SyncLocalUnitMove] 找不到移动后的单位数据 at ({toPos.x},{toPos.y})");
        }
    }

    /// <summary>
    /// 本地玩家创建单位后调用此方法进行网络同步
    /// 在 TryCreateUnit 成功后调用
    /// </summary>
    private void SyncLocalUnitAdd(CardType unitType, int2 pos)
    {
        // 检查网络连接
        if (NetGameSystem.Instance == null || !NetGameSystem.Instance.bIsConnected)
        {
            return; // 单机模式或未连接，不发送
        }

        // 获取新添加的单位数据
        PlayerUnitData? unitData = PlayerDataManager.Instance.FindUnit(localPlayerId, pos);

        if (unitData.HasValue)
        {
            // 发送网络消息
            NetGameSystem.Instance.SendUnitAddMessage(
                localPlayerId,
                unitType,
                pos,
                unitData.Value.PlayerUnitDataSO
            );

            Debug.Log($"[SyncLocalUnitAdd] 已发送创建单位同步消息");
        }
        else
        {
            Debug.LogWarning($"[SyncLocalUnitAdd] 找不到新创建的单位数据 at ({pos.x},{pos.y})");
        }
    }

    /// <summary>
    /// 本地玩家攻击后调用此方法进行网络同步
    /// 在攻击完成后调用
    /// 注意:在新的逻辑中,攻击者必须已在相邻格,不会发生移动
    /// </summary>
    private void SyncLocalUnitAttack(int2 attackerPos, int2 targetPos, int targetPlayerId, bool targetDestroyed)
    {
        // 检查网络连接
        if (NetGameSystem.Instance == null || !NetGameSystem.Instance.bIsConnected)
        {
            return; // 单机模式或未连接，不发送
        }

        // 获取攻击者数据
        PlayerUnitData? attackerData = PlayerDataManager.Instance.FindUnit(localPlayerId, attackerPos);

        if (!attackerData.HasValue)
        {
            Debug.LogWarning($"[SyncLocalUnitAttack] 找不到攻击者数据 at ({attackerPos.x},{attackerPos.y})");
            return;
        }

        // 获取目标数据（如果存活）
        PlayerUnitData? targetData = null;
        if (!targetDestroyed)
        {
            targetData = PlayerDataManager.Instance.FindUnit(targetPlayerId, targetPos);
        }

        // 发送网络消息 - 攻击者位置不变,所以不传递移动参数
        NetGameSystem.Instance.SendUnitAttackMessage(
            localPlayerId,
            attackerPos,
            targetPlayerId,
            targetPos,
            attackerData.Value.PlayerUnitDataSO,
            targetData?.PlayerUnitDataSO,
            targetDestroyed
        // 不传递 attackerOriginalPos 和 hasMoved 参数,使用默认值(无移动)
        );

        Debug.Log($"[SyncLocalUnitAttack] 已发送攻击同步消息，攻击者位置: ({attackerPos.x},{attackerPos.y}), 目标摧毁: {targetDestroyed}");
    }

    /// <summary>
    /// 本地玩家魅惑单位后调用此方法进行网络同步
    /// </summary>
    private void SyncLocalUnitCharm(int missionaryID, int2 missionaryPos, int targetID, int targetOwnerId, int2 targetPos, syncPieceData newUnitSyncData)
    {
        // 检查网络连接
        if (NetGameSystem.Instance == null || !NetGameSystem.Instance.bIsConnected)
        {
            return; // 单机模式或未连接，不发送
        }

        // 发送网络消息
        NetGameSystem.Instance.SendUnitCharmMessage(
            localPlayerId,
            missionaryID,
            missionaryPos,
            targetOwnerId,
            targetID,
            targetPos,
            newUnitSyncData,
            3  // 魅惑持续3回合
        );

        Debug.Log($"[SyncLocalUnitCharm] 已发送魅惑同步消息");
    }

    /// <summary>
    /// 本地玩家魅惑过期后调用此方法进行网络同步
    /// </summary>
    private void SyncCharmExpire(int unitID, int2 pos, int originalOwnerId, syncPieceData unitSyncData)
    {
        // 检查网络连接
        if (NetGameSystem.Instance == null || !NetGameSystem.Instance.bIsConnected)
        {
            return;
        }

        // 发送网络消息
        NetGameSystem.Instance.SendCharmExpireMessage(
            localPlayerId,
            originalOwnerId,
            unitID,
            pos,
            unitSyncData
        );

        Debug.Log($"[SyncCharmExpire] 已发送魅惑过期同步消息");
    }



    // 将 CardType 转换为 PieceType
    private PieceType ConvertCardTypeToPieceType(CardType cardType)
    {
        switch (cardType)
        {
            case CardType.Farmer: return PieceType.Farmer;
            case CardType.Solider: return PieceType.Military;
            case CardType.Missionary: return PieceType.Missionary;
            case CardType.Pope: return PieceType.Pope;
            default:
                Debug.LogError($"未知的 CardType: {cardType}");
                return PieceType.None;
        }
    }

    // 将 PieceType 转换为 CardType
    private CardType ConvertPieceTypeToCardType(PieceType pieceType)
    {
        switch (pieceType)
        {
            case PieceType.Farmer: return CardType.Farmer;
            case PieceType.Military: return CardType.Solider;
            case PieceType.Missionary: return CardType.Missionary;
            case PieceType.Pope: return CardType.Pope;
            default:
                Debug.LogError($"未知的 PieceType: {pieceType}");
                return CardType.Farmer; // 默认返回Farmer
        }
    }

    // 获取资源路径（根据实际项目调整路径）
    private string GetResourcePathForUnitType(CardType unitType)
    {
        switch (unitType)
        {
            case CardType.Farmer: return "Cyou/Prefab/farmer";
            case CardType.Solider: return "Cyou/Prefab/military";
            case CardType.Missionary: return "Cyou/Prefab/Missionary";
            case CardType.Pope: return "Cyou/Prefab/pope";
            default: return null;
        }
    }

    private void OnUnitAddedHandler(int playerId, PlayerUnitData unitData)
    {

    }

    private void OnUnitRemovedHandler(int playerId, int2 position)
    {
        Debug.Log($"[事件] OnUnitRemovedHandler: 玩家 {playerId} at ({position.x},{position.y})");

        if (playerId == localPlayerId)
        {
            Debug.Log("[事件] 本地玩家移除单位");
            // 本地玩家移除单位（发生在被攻击时）
            if (localPlayerUnits.ContainsKey(position))
            {
                Destroy(localPlayerUnits[position]);
                localPlayerUnits.Remove(position);
            }
            GameManage.Instance.SetCellObject(position, null);
        }
        else
        {
            Debug.Log("[事件] 其他玩家移除单位由 HandleNetworkAttack 处理，跳过");

            return;
        }
    }

    private void OnUnitMovedHandler(int playerId, int2 fromPos, int2 toPos)
    {

    }



    // *************************
    //        清理
    // *************************

    private void OnDestroy()
    {
        // 取消订阅事件
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnUnitAdded -= OnUnitAddedHandler;
            PlayerDataManager.Instance.OnUnitRemoved -= OnUnitRemovedHandler;
            PlayerDataManager.Instance.OnUnitMoved -= OnUnitMovedHandler;
        }
    }
}