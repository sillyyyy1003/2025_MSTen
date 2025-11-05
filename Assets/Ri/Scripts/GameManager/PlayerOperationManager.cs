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
    // 双击检测
    // 定义双击的最大时间间隔
    public float doubleClickTimeThreshold = 0.3f;
    private float lastClickTime;
    private int clickCount = 0;

    // === Event 定义区域 ===
    public event System.Action<int,CardType> OnUnitChoosed;


    // *************************
    //         公有属性
    // *************************

    // 玩家的预制体,后续更改
    public GameObject FarmerPrefab;
    public GameObject SoldierPrefab;

    // 其他玩家预制体(后续得到)
    public GameObject EnemyFarmerPrefab;
    public GameObject EnemySoldierPrefab;

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
        if (Input.GetKeyDown(KeyCode.G) && bIsChooseMissionary)
        {
            // 传教士占领
            _HexGrid.GetCell(LastSelectingCellID).Walled = true;
            PlayerDataManager.Instance.GetPlayerData(localPlayerId).AddOwnedCell(LastSelectingCellID);
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
            else if (otherPlayersUnits.Count>=1&& otherPlayersUnits[localPlayerId==0?1:0].ContainsKey(clickPos))  
            {
                    PlayerUnitDataInterface.Instance.GetEnemyUnitPosition(clickPos);
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
                if (ownerId != localPlayerId && PlayerDataManager.Instance.nowChooseUnitType==CardType.Solider)
                {
                    // 【新增】攻击前检查AP
                    if (!CheckUnitHasEnoughAP(currentPos, 1))
                    {
                        Debug.Log("[攻击] AP不足，无法攻击");
                        return;
                    }
                    // 攻击敌方单位
                    AttackUnit(targetPos, ClickCellid);
                }
                else
                {
                    // 移动前检查AP
                    if (!CheckUnitHasEnoughAP(currentPos, 1))
                    {
                        Debug.Log("[移动] AP不足，无法移动");
                        return;
                    }

                    Debug.Log("不能移动到自己单位所在的位置");
                }
            }
            else
            {
                // 移动到空地
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

        Debug.Log("你的回合开始!");

        // 显示结束回合按钮
        GameSceneUIManager.Instance.SetEndTurn(true);
        PieceManager.Instance.ProcessTurnStart(localPlayerId);

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
        foreach (var unit in otherPlayersUnits[playerId].Values)
        {
            if (unit != null)
            {
                Destroy(unit);
                Debug.Log("otherPlayers unit is " + unit.name);
            }

        }
        otherPlayersUnits[playerId].Clear();

        // 创建新的单位显示

        for (int i = 0; i < data.PlayerUnits.Count; i++)
        {
            PlayerUnitData unit = data.PlayerUnits[i];

            Debug.LogWarning($"创建敌方单位: {unit.UnitType} at ({unit.Position.x},{unit.Position.y})");

            // 检查并重新加载 PlayerUnitDataSO
            //if (unit.PlayerUnitDataSO.playerID==-1)
            //{
            //    Debug.LogWarning($"PlayerUnitDataSO 为空，尝试重新加载 {unit.UnitType}");

            //    // 从 UnitListTable 加载
            //    //if (UnitListTable.Instance != null)
            //    //{
            //    //    PieceDataSO loadedData = UnitListTable.Instance.GetPieceDataByCardType(unit.UnitType);
            //    //    if (loadedData != null)
            //    //    {
            //    //        // 通过索引修改列表中的元素
            //    //        data.PlayerUnits[i].ChangeUnitDataSO(loadedData);
            //    //        unit.PlayerUnitDataSO = loadedData;
            //    //        Debug.Log($" 从 UnitListTable 成功加载: {loadedData.name}");
            //    //    }
            //    //}

            //    //// 后备方案 - 从 Resources 加载
            //    //if (unit.PlayerUnitDataSO == null)
            //    //{
            //    //    string resourcePath = GetResourcePathForUnitType(unit.UnitType);
            //    //    if (!string.IsNullOrEmpty(resourcePath))
            //    //    {
            //    //        PieceDataSO loadedData = Resources.Load<PieceDataSO>(resourcePath);
            //    //        if (loadedData != null)
            //    //        {
            //    //            data.PlayerUnits[i].ChangeUnitDataSO(loadedData);
            //    //            unit.PlayerUnitDataSO = loadedData;
            //    //            Debug.Log($"✅ 从 Resources 成功加载: {resourcePath}");
            //    //        }
            //    //    }
            //    //}

            //    //if (unit.PlayerUnitDataSO == null)
            //    //{
            //    //    Debug.LogError($"❌ 无法加载 PlayerUnitDataSO for {unit.UnitType}，跳过创建");
            //    //    continue;
            //    //}
            //}
            //else
            //{
            //    Debug.Log("✅ unit data is " + unit.PlayerUnitDataSO.name);
            //}

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
       foreach(var i in pos)
        {
            if(_HexGrid.GetCell(i).enabled)
                _HexGrid.GetCell(i).Walled = true;

                PlayerDataManager.Instance.GetPlayerData(localPlayerId).AddOwnedCell(i);
        }
    }

    // 在指定的格子创建单位实例
    private void CreateUnitAtPosition(CardType unitType, int cellId)
    {
        BoardInfor cellInfo = GameManage.Instance.GetBoardInfor(cellId);
        int2 position = cellInfo.Cells2DPos;
        Vector3 worldPos = cellInfo.Cells3DPos;

       

        // 选择对应的预制体
        //Piece prefab = null;
        PieceType pieceType=PieceType.None;
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




        //switch (pieceType)
        //{
        //    case PieceType.Farmer:
        //        unitData = PieceManager.Instance.;

        //        break;
        //    case PieceType.Military:
        //        unitData = unit.GetComponent<MilitaryUnit>().GetUnitDataSO();
        //        break;
        //    case  PieceType.Missionary:
        //        unitData = unit.GetComponent<Missionary>().GetUnitDataSO();
        //        break;
        //    case PieceType.Pope:
        //        unitData = unit.GetComponent<Pope>().GetUnitDataSO();
        //        Debug.Log("unit pope data is "+unitData.name);
        //        //_HexGrid.GetCell(cellId).Walled = true ;
        //        GetStartWall(cellId);
        //        break;
        //}

        // 添加描边效果
        pieceObj.AddComponent<ChangeMaterial>();

        //if (unit == null)
        //{
        //    Debug.LogError($"预制体为空: {unitType}");
        //    return;
        //}

        //// 更新棋子位置在棋盘上
        //unit.transform.position = new Vector3(
        //    unit.transform.position.x,
        //    unit.transform.position.y + 2.5f,
        //    unit.transform.position.z
        //);

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
        if (unitData.Value.bCanDoAction)
        {
            _HexGrid.FindPath(LastSelectingCellID, targetCellId, PieceManager.Instance.GetPieceAP(unitData.Value.UnitID));
        }
        else
        {
            Debug.Log("该单位AP不足！");
            bCanContinue = true;
            return;
        }
      
        if(_HexGrid.HasPath)
        {
            List<HexCell> listCellPos = _HexGrid.GetPathCells();
            if(listCellPos.Count-1> PieceManager.Instance.GetPieceAP(unitData.Value.UnitID))
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
                   listCellPos[i].Position.y +2.5f,
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
                    bool apConsumed = PieceManager.Instance.ConsumePieceAP(pieceID, listCellPos.Count-1);
                    
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

        // 检查bCanDoAction标志
        if (!unitData.Value.bCanDoAction)
        {
            Debug.Log($"[AP检查] 单位 at ({position.x},{position.y}) 的 bCanDoAction 为 false");
            //ShowAPInsufficientMessage("该单位已无法行动！");
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
    // 攻击单位
    private void AttackUnit(int2 targetPos, int targetCellId)
    {
        if (SelectingUnit == null) return;

        bCanContinue = false;

        // ========== 获取单位数据 ==========
        // 获取攻击者位置
        int2 attackerPos = PlayerBoardInforDict[LastSelectingCellID].Cells2DPos;

        // 获取目标单位的拥有者
        int targetOwnerId = PlayerDataManager.Instance.GetUnitOwner(targetPos);
      
        // 获取攻击者数据
        PlayerUnitData? attackerUnitData = PlayerDataManager.Instance.FindUnit(localPlayerId, attackerPos);
        if (!attackerUnitData.HasValue)
        {
            Debug.LogError("[AttackUnit] 找不到攻击者单位数据");
            bCanContinue = true;
            return;
        }
       
        // 获取目标数据
        PlayerUnitData? targetUnitData = PlayerDataManager.Instance.FindUnit(targetOwnerId, targetPos);
        if (!targetUnitData.HasValue)
        {
            Debug.LogError("[AttackUnit] 找不到目标单位数据");
            bCanContinue = true;
            return;
        }




        // 获取目标单位的GameObject
        GameObject targetUnit = null;
        if (otherPlayersUnits.ContainsKey(targetOwnerId) &&
            otherPlayersUnits[targetOwnerId].ContainsKey(targetPos))
        {
            targetUnit = otherPlayersUnits[targetOwnerId][targetPos];
        }

        // ========== 战斗计算 ==========
        // 获取双方的 PieceID
        int attackerPieceID = attackerUnitData.Value.PlayerUnitDataSO.pieceID;
        int targetPieceID = targetUnitData.Value.PlayerUnitDataSO.pieceID;

        Debug.Log($"[AttackUnit] 战斗开始 - 攻击者ID:{attackerPieceID} 攻击 目标ID:{targetPieceID}");

        // 执行攻击，获取目标的HP同步数据
        syncPieceData? targetSyncData = PieceManager.Instance.AttackEnemy(attackerPieceID, targetPieceID);

        if (!targetSyncData.HasValue)
        {
            Debug.LogError("[AttackUnit] PieceManager.AttackEnemy 调用失败！");
            bCanContinue = true;
            return;
        }

        // 判断目标是否死亡
        bool targetDied = targetSyncData.Value.currentHP <= 0;
        Debug.Log($"[AttackUnit] 攻击完成 - 目标剩余HP: {targetSyncData.Value.currentHP}, 是否死亡: {targetDied}");

        // ========== 计算移动路径 ==========

        // ============= 使用AP限制寻路距离 ============= 
        int currentAP = PieceManager.Instance.GetPieceAP(attackerUnitData.Value.UnitID);
        // 使用 HexGrid 的寻路系统
        _HexGrid.FindPath(LastSelectingCellID, targetCellId, currentAP);

        if (!_HexGrid.HasPath)
        {
            Debug.LogError("[AttackUnit] 无法找到到目标的路径");
            _HexGrid.ClearPath();
            bCanContinue = true;
            return;
        }
        // 获取路径节点列表
        List<HexCell> listCellPos = _HexGrid.GetPathCells();

        int requiredAP = listCellPos.Count - 1;
        if (requiredAP > currentAP)
        {
            Debug.Log($"[攻击] AP不足 - 需要:{requiredAP}, 当前:{currentAP}");
            _HexGrid.ClearPath();
            bCanContinue = true;
            return;
        }

        // ========== 根据目标状态执行移动 ==========
        if (targetDied)
        {
            // Case 1: 目标死亡 - 移动到目标位置（完整路径）
            Debug.Log("[AttackUnit] 目标死亡，攻击者移动到目标位置");

            ExecuteMoveToTargetPosition(
                listCellPos,
                targetCellId,
                attackerPos,
                targetPos,
                targetUnit,
                targetOwnerId
            );
        }
        else
        {
            // Case 2: 目标存活 - 移动到前一格
            Debug.Log("[AttackUnit] 目标存活，攻击者移动到前一格");

            if (listCellPos.Count < 2)
            {
                Debug.LogWarning("[AttackUnit] 路径太短，无法移动到前一格");
                _HexGrid.ClearPath();
                bCanContinue = true;
                return;
            }

            // 计算前一格位置（倒数第二个节点）
            int moveToIndex = listCellPos.Count - 2;
            HexCell finalCell = listCellPos[moveToIndex];
            int finalCellId = finalCell.Index;
            int2 finalPos =new int2(finalCell.Coordinates.X + finalCell.Coordinates.Z / 2, finalCell.Coordinates.Z) ;
          

            // 只使用到前一格的路径
            List<HexCell> movePathCells = listCellPos.GetRange(0, moveToIndex + 1);

            ExecuteMoveToBeforeTarget(
                movePathCells,
                finalCellId,
                attackerPos,
                finalPos,
                targetUnit,
                targetPos,
                targetSyncData.Value
            );
        }
    }

/// <summary>
 /// 目标死亡时，攻击者沿完整路径移动到目标位置
 /// 
 /// 动画效果：
 /// - 攻击者沿弧形路径移动（参考 MoveToSelectCell 的实现）
 /// - 同时播放目标死亡动画（缩放到0 + 旋转360度）
 /// - 完成后销毁目标，更新数据
 /// </summary>
private void ExecuteMoveToTargetPosition(
    List<HexCell> listCellPos,
    int targetCellId,
    int2 attackerPos,
    int2 targetPos,
    GameObject targetUnit,
    int targetOwnerId)
{
    Sequence moveSequence = DOTween.Sequence();
    Vector3 currentPos = SelectingUnit.transform.position;

    // 构建完整的移动路径动画（与 MoveToSelectCell 相同的方式）
    for (int i = 0; i < listCellPos.Count; i++)
    {
        Vector3 waypoint = new Vector3(
            listCellPos[i].Position.x,
            listCellPos[i].Position.y + 2.5f,
            listCellPos[i].Position.z
        );

        // 计算弧形路径的中间点
        Vector3 midPoint = (currentPos + waypoint) / 2f;
        midPoint.y += 5.0f;

        // 创建三点路径：当前位置 -> 中间点 -> 目标点
        Vector3[] path = new Vector3[] { currentPos, midPoint, waypoint };

        moveSequence.Append(SelectingUnit.transform.DOPath(path, MoveSpeed, PathType.CatmullRom)
            .SetEase(Ease.Linear));

        currentPos = waypoint;
    }

    // 在移动过程中播放目标死亡动画
    if (targetUnit != null)
    {
        // 延迟一段时间后开始死亡动画（让攻击者先靠近）
        moveSequence.Join(targetUnit.transform.DOScale(0f, 0.5f)
            .SetDelay(MoveSpeed * 0.7f));

        moveSequence.Join(targetUnit.transform.DORotate(
            new Vector3(0, 360, 0),
            0.5f,
            RotateMode.FastBeyond360
        ).SetDelay(MoveSpeed * 0.7f));
    }

    // 动画完成后的数据更新
    moveSequence.OnComplete(() =>
    {
        // 销毁目标单位
        if (targetUnit != null)
        {
            Destroy(targetUnit);
        }

        // 从数据管理器移除目标
        PlayerDataManager.Instance.RemoveUnit(targetOwnerId, targetPos);
       
        // 移动攻击者数据
        PlayerDataManager.Instance.MoveUnit(localPlayerId, attackerPos, targetPos);

        // 从PieceManager中移除
        PieceManager.Instance.RemovePiece(PlayerDataManager.Instance.GetUnitIDBy2DPos(targetPos));

        // 更新本地单位字典
        localPlayerUnits.Remove(attackerPos);
        localPlayerUnits[targetPos] = SelectingUnit;

        // 从目标玩家的单位字典中移除
        if (otherPlayersUnits.ContainsKey(targetOwnerId))
        {
            otherPlayersUnits[targetOwnerId].Remove(targetPos);
        }

        // 更新 GameManage 的格子对象
        GameManage.Instance.MoveCellObject(attackerPos, targetPos);

        // 更新选中的格子ID
        LastSelectingCellID = targetCellId;

        // 清除路径显示
        _HexGrid.ClearPath();

        // 解锁操作
        bCanContinue = true;

        // ============= 攻击移动消耗AP逻辑 ============= 
        PlayerUnitData? unitData = PlayerDataManager.Instance.FindUnit(localPlayerId, targetPos);
        if (unitData.HasValue)
        {
            int pieceID = unitData.Value.PlayerUnitDataSO.pieceID;

            // 消耗AP（移动的格子数）
            int apCost = listCellPos.Count - 1;
            bool apConsumed = PieceManager.Instance.ConsumePieceAP(pieceID, apCost);

            if (apConsumed)
            {
                Debug.Log($"[攻击移动] 单位 PieceID:{pieceID} 消耗{apCost} AP");

                // 检查AP是否为0
                Piece piece = PieceManager.Instance.GetPiece(pieceID);
                if (piece != null && piece.CurrentAP <= 0)
                {
                    PlayerDataManager.Instance.UpdateUnitCanDoActionByPos(localPlayerId, targetPos, false);
                    Debug.Log($"[攻击移动] 单位 PieceID:{pieceID} AP为0，bCanDoAction设置为false");
                }
            }
            else
            {
                Debug.LogWarning($"[攻击移动] 单位 PieceID:{pieceID} AP消耗失败");
            }
        }
        Debug.Log($"[AttackUnit] 完成 - 攻击者已移动到目标位置: ({targetPos.x},{targetPos.y})");

        // 网络同步
        SyncLocalUnitAttack(attackerPos, targetPos, targetOwnerId, true);
    });
}


/// <summary>
/// 目标存活时，攻击者移动到前一格并播放攻击动画
/// 
/// 动画流程：
/// 1. 移动到前一格（与 MoveToSelectCell 相同的弧形路径）
/// 2. 短距离冲刺到目标方向（攻击动作）
/// 3. 目标播放受击效果（缩放弹跳 + 位置抖动）
/// 4. 返回到前一格位置
/// 5. 更新目标HP显示
/// </summary>
private void ExecuteMoveToBeforeTarget(
    List<HexCell> movePathCells,
    int finalCellId,
    int2 attackerPos,
    int2 finalPos,
    GameObject targetUnit,
    int2 targetPos,
    syncPieceData targetSyncData)
{
    Sequence moveSequence = DOTween.Sequence();
    Vector3 currentPos = SelectingUnit.transform.position;

    // 第一阶段：移动到前一格（与 MoveToSelectCell 相同的方式）
    for (int i = 0; i < movePathCells.Count; i++)
    {
        Vector3 waypoint = new Vector3(
            movePathCells[i].Position.x,
            movePathCells[i].Position.y + 2.5f,
            movePathCells[i].Position.z
        );

        Vector3 midPoint = (currentPos + waypoint) / 2f;
        midPoint.y += 5.0f;

        Vector3[] path = new Vector3[] { currentPos, midPoint, waypoint };

        moveSequence.Append(SelectingUnit.transform.DOPath(path, MoveSpeed, PathType.CatmullRom)
            .SetEase(Ease.Linear));

        currentPos = waypoint;
    }

    // 第二阶段：播放攻击动画
    moveSequence.AppendCallback(() =>
    {
        // 获取目标的世界坐标
        Vector3 targetWorldPos = new Vector3(
            PlayerBoardInforDict[finalCellId].Cells3DPos.x,
            PlayerBoardInforDict[finalCellId].Cells3DPos.y + 2.5f,
            PlayerBoardInforDict[finalCellId].Cells3DPos.z
        );

        // 计算冲刺位置（向目标方向移动30%）
        Vector3 attackPos = Vector3.Lerp(SelectingUnit.transform.position, targetWorldPos, 0.3f);

        // 创建攻击动画序列
        Sequence attackAnim = DOTween.Sequence();

        // 冲刺
        attackAnim.Append(SelectingUnit.transform.DOMove(attackPos, 0.15f));

        // 目标受击效果
        if (targetUnit != null)
        {
            // 缩放弹跳
            attackAnim.Join(targetUnit.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 5, 1));
            // 位置抖动
            attackAnim.Join(targetUnit.transform.DOShakePosition(0.3f, 0.5f, 10, 90, false, true));
        }

        // 返回原位
        attackAnim.Append(SelectingUnit.transform.DOMove(currentPos, 0.15f));

        // 攻击动画完成后的处理
        attackAnim.OnComplete(() =>
        {
            // 移动攻击者数据
            PlayerDataManager.Instance.MoveUnit(localPlayerId, attackerPos, finalPos);

            // 更新本地单位字典
            localPlayerUnits.Remove(attackerPos);
            localPlayerUnits[finalPos] = SelectingUnit;

            // 更新 GameManage 的格子对象
            GameManage.Instance.MoveCellObject(attackerPos, finalPos);

            // 更新选中的格子ID
            LastSelectingCellID = finalCellId;

            // 清除路径显示
            _HexGrid.ClearPath();

            // 解锁操作
            bCanContinue = true;

            // ============= 攻击移动消耗AP逻辑 ============= 
            PlayerUnitData? unitData = PlayerDataManager.Instance.FindUnit(localPlayerId, finalPos);
            if (unitData.HasValue)
            {
                int pieceID = unitData.Value.PlayerUnitDataSO.pieceID;

                // 消耗AP（移动的格子数）
                int apCost = movePathCells.Count - 1;
                bool apConsumed = PieceManager.Instance.ConsumePieceAP(pieceID, apCost);

                if (apConsumed)
                {
                    Debug.Log($"[攻击移动] 单位 PieceID:{pieceID} 消耗{apCost} AP");

                    // 检查AP是否为0
                    Piece piece = PieceManager.Instance.GetPiece(pieceID);
                    if (piece != null && piece.CurrentAP <= 0)
                    {
                        PlayerDataManager.Instance.UpdateUnitCanDoActionByPos(localPlayerId, finalPos, false);
                        Debug.Log($"[攻击移动] 单位 PieceID:{pieceID} AP为0，bCanDoAction设置为false");
                    }
                }
                else
                {
                    Debug.LogWarning($"[攻击移动] 单位 PieceID:{pieceID} AP消耗失败");
                }
            }

            //// 更新目标HP显示
            //UpdateUnitHPDisplay(targetPos, targetSyncData.currentHP);

            Debug.Log($"[AttackUnit] 完成 - 攻击者移动到前一格: ({finalPos.x},{finalPos.y}), " +
                      $"目标剩余HP: {targetSyncData.currentHP}");

            // 网络同步
            SyncLocalUnitAttack(attackerPos, finalPos, PlayerDataManager.Instance.GetUnitOwner(targetPos), false);
        });
    });
}


// *************************
//        事件处理
// *************************


// 处理来自网络的攻击消息（其他玩家的攻击）
public void HandleNetworkAttack(UnitAttackMessage msg)
    {
        if (msg.AttackerPlayerId == localPlayerId)
        {
            Debug.Log("[网络攻击] 这是本地玩家的攻击，已处理");
            return;
        }

        int2 attackerPos = new int2(msg.AttackerPosX, msg.AttackerPosY);
        int2 targetPos = new int2(msg.TargetPosX, msg.TargetPosY);

        Debug.Log($"[网络攻击] 玩家 {msg.AttackerPlayerId} 攻击 ({targetPos.x},{targetPos.y})");

        // 先更新数据
        if (PlayerDataManager.Instance != null)
        {
            // 移除被攻击的单位
            PlayerDataManager.Instance.RemoveUnit(msg.TargetPlayerId, targetPos);
            // 移动攻击者
            PlayerDataManager.Instance.MoveUnit(msg.AttackerPlayerId, attackerPos, targetPos);
            Debug.Log($"[网络攻击] 已更新数据管理器");
        }

        // 获取攻击者和目标的GameObject
        //GameObject attackerUnit = null;
        //GameObject targetUnit = null;
        // 获取攻击者GameObject（用于播放攻击动画）
        GameObject attackerObj = null;
        if (msg.AttackerPlayerId == localPlayerId && localPlayerUnits.ContainsKey(attackerPos))
        {
            attackerObj = localPlayerUnits[attackerPos];
        }
        else if (otherPlayersUnits.ContainsKey(msg.AttackerPlayerId) &&
                 otherPlayersUnits[msg.AttackerPlayerId].ContainsKey(attackerPos))
        {
            attackerObj = otherPlayersUnits[msg.AttackerPlayerId][attackerPos];
        }

        // 播放攻击动画（如果有）
        if (attackerObj != null)
        {
            // TODO: 播放攻击动画
            // attackerObj.GetComponent<Animator>()?.SetTrigger("Attack");
        }

        // 处理目标
        if (msg.TargetDestroyed)
        {
            // 目标被摧毁
            Debug.Log($"[HandleNetworkAttack] 目标被击杀");

            // 移除目标GameObject
            if (msg.TargetPlayerId == localPlayerId && localPlayerUnits.ContainsKey(targetPos))
            {
                GameObject targetObj = localPlayerUnits[targetPos];

                // 播放死亡动画（如果有）
                // TODO: targetObj.GetComponent<Animator>()?.SetTrigger("Death");

                Destroy(targetObj);
                localPlayerUnits.Remove(targetPos);
            }
            else if (otherPlayersUnits.ContainsKey(msg.TargetPlayerId) &&
                     otherPlayersUnits[msg.TargetPlayerId].ContainsKey(targetPos))
            {
                GameObject targetObj = otherPlayersUnits[msg.TargetPlayerId][targetPos];

                // 播放死亡动画（如果有）
                // TODO: targetObj.GetComponent<Animator>()?.SetTrigger("Death");

                Destroy(targetObj);
                otherPlayersUnits[msg.TargetPlayerId].Remove(targetPos);
            }

            GameManage.Instance.SetCellObject(targetPos, null);
        }
        else if (msg.TargetSyncData.HasValue)
        {
            // 目标存活，播放受击动画
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

            // 播放受击动画（如果有）
            if (targetObj != null)
            {
                // TODO: 播放受击动画
                // targetObj.GetComponent<Animator>()?.SetTrigger("Hit");
                Debug.Log($"[HandleNetworkAttack] 目标受击，当前HP: {msg.TargetSyncData.Value.currentHP}");
            }
        }
        //// 查找攻击者单位
        //if (msg.AttackerPlayerId == localPlayerId)
        //{
        //    if (localPlayerUnits.ContainsKey(attackerPos))
        //    {
        //        attackerUnit = localPlayerUnits[attackerPos];
        //    }
        //}
        //else
        //{
        //    if (otherPlayersUnits.ContainsKey(msg.AttackerPlayerId) &&
        //        otherPlayersUnits[msg.AttackerPlayerId].ContainsKey(attackerPos))
        //    {
        //        attackerUnit = otherPlayersUnits[msg.AttackerPlayerId][attackerPos];
        //    }
        //}

        //// 查找目标单位
        //if (msg.TargetPlayerId == localPlayerId)
        //{
        //    if (localPlayerUnits.ContainsKey(targetPos))
        //    {
        //        targetUnit = localPlayerUnits[targetPos];
        //    }
        //}
        //else
        //{
        //    if (otherPlayersUnits.ContainsKey(msg.TargetPlayerId) &&
        //        otherPlayersUnits[msg.TargetPlayerId].ContainsKey(targetPos))
        //    {
        //        targetUnit = otherPlayersUnits[msg.TargetPlayerId][targetPos];
        //    }
        //}

        //// 播放攻击动画
        //if (attackerUnit != null)
        //{
        //    // 找到目标位置的世界坐标
        //    Vector3 targetWorldPos = Vector3.zero;
        //    foreach (var board in PlayerBoardInforDict.Values)
        //    {
        //        if (board.Cells2DPos.Equals(targetPos))
        //        {
        //            targetWorldPos = new Vector3(
        //                board.Cells3DPos.x,
        //                board.Cells3DPos.y + 6.5f,
        //                board.Cells3DPos.z
        //            );
        //            break;
        //        }
        //    }

        //    // 创建攻击动画
        //    Sequence attackSequence = DOTween.Sequence();

        //    // 冲刺效果
        //    Vector3 attackPos = Vector3.Lerp(attackerUnit.transform.position, targetWorldPos, 0.7f);
        //    attackSequence.Append(attackerUnit.transform.DOMove(attackPos, MoveSpeed * 0.3f));

        //    // 目标消失效果
        //    if (targetUnit != null)
        //    {
        //        attackSequence.Join(targetUnit.transform.DOScale(0f, 0.2f));
        //        attackSequence.Join(targetUnit.transform.DORotate(new Vector3(0, 360, 0), 0.2f, RotateMode.FastBeyond360));
        //    }

        //    // 移动到目标位置
        //    attackSequence.Append(attackerUnit.transform.DOMove(targetWorldPos, MoveSpeed * 0.3f));

        //    // 完成后更新引用
        //    attackSequence.OnComplete(() =>
        //    {
        //        // 销毁目标单位
        //        if (targetUnit != null)
        //        {
        //            Destroy(targetUnit);
        //        }

        //        // 更新攻击者单位的引用
        //        if (msg.AttackerPlayerId == localPlayerId)
        //        {
        //            localPlayerUnits.Remove(attackerPos);
        //            localPlayerUnits[targetPos] = attackerUnit;
        //        }
        //        else
        //        {
        //            if (otherPlayersUnits.ContainsKey(msg.AttackerPlayerId))
        //            {
        //                otherPlayersUnits[msg.AttackerPlayerId].Remove(attackerPos);
        //                otherPlayersUnits[msg.AttackerPlayerId][targetPos] = attackerUnit;
        //            }
        //        }

        //        // 移除目标单位的引用
        //        if (msg.TargetPlayerId == localPlayerId)
        //        {
        //            localPlayerUnits.Remove(targetPos);
        //        }
        //        else
        //        {
        //            if (otherPlayersUnits.ContainsKey(msg.TargetPlayerId))
        //            {
        //                otherPlayersUnits[msg.TargetPlayerId].Remove(targetPos);
        //            }
        //        }

        //        // 更新GameManage
        //        GameManage.Instance.MoveCellObject(attackerPos, targetPos);

        //        Debug.Log($"[网络攻击] 动画完成: ({attackerPos.x},{attackerPos.y}) -> ({targetPos.x},{targetPos.y})");
        //    });
        //}
        //else
        //{
        //    Debug.LogWarning($"[网络攻击] 找不到攻击者单位 at ({attackerPos.x},{attackerPos.y})");

        //    // 如果找不到攻击者单位，至少销毁目标
        //    if (targetUnit != null)
        //    {
        //        Destroy(targetUnit);

        //        if (msg.TargetPlayerId == localPlayerId)
        //        {
        //            localPlayerUnits.Remove(targetPos);
        //        }
        //        else if (otherPlayersUnits.ContainsKey(msg.TargetPlayerId))
        //        {
        //            otherPlayersUnits[msg.TargetPlayerId].Remove(targetPos);
        //        }
        //    }
        //}
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

        //// 先更新数据（不触发事件，因为我们自己处理视觉）
        //if (PlayerDataManager.Instance != null)
        //{
        //    PlayerDataManager.Instance.MoveUnit(msg.PlayerId, fromPos, toPos);
        //    Debug.Log($"[网络移动] 已更新数据管理器");
        //}

        //// 获取移动的单位GameObject
        //GameObject movingUnit = null;

        //if (otherPlayersUnits.ContainsKey(msg.PlayerId) &&
        //    otherPlayersUnits[msg.PlayerId].ContainsKey(fromPos))
        //{
        //    movingUnit = otherPlayersUnits[msg.PlayerId][fromPos];
        //    Debug.Log($"[网络移动] 找到移动单位");
        //}
        //else
        //{
        //    Debug.LogWarning($"[网络移动] 找不到移动的单位 at ({fromPos.x},{fromPos.y})");

        //    // 打印当前玩家的所有单位位置
        //    if (otherPlayersUnits.ContainsKey(msg.PlayerId))
        //    {
        //        Debug.Log($"[网络移动] 玩家 {msg.PlayerId} 当前单位位置：");
        //        foreach (var kvp in otherPlayersUnits[msg.PlayerId])
        //        {
        //            Debug.Log($"  - ({kvp.Key.x},{kvp.Key.y})");
        //        }
        //    }
        //    return;
        //}

        //if (movingUnit != null)
        //{
        //    // 找到目标位置的世界坐标
        //    Vector3 targetWorldPos = Vector3.zero;
        //    foreach (var board in PlayerBoardInforDict.Values)
        //    {
        //        if (board.Cells2DPos.Equals(toPos))
        //        {
        //            targetWorldPos = new Vector3(
        //                board.Cells3DPos.x,
        //                board.Cells3DPos.y + 2.5f,
        //                board.Cells3DPos.z
        //            );
        //            break;
        //        }
        //    }

        //    Debug.Log($"[网络移动] 开始更新引用和执行动画");

        //    // 立即更新字典引用（在动画之前）
        //    otherPlayersUnits[msg.PlayerId].Remove(fromPos);
        //    otherPlayersUnits[msg.PlayerId][toPos] = movingUnit;

        //    // 更新GameManage的格子对象
        //    GameManage.Instance.SetCellObject(fromPos, null);
        //    GameManage.Instance.SetCellObject(toPos, movingUnit);

        //    Debug.Log($"[网络移动] 字典已更新，开始动画");

        //    // 执行移动动画
        //    movingUnit.transform.DOMove(targetWorldPos, MoveSpeed).OnComplete(() =>
        //    {
        //        Debug.Log($"[网络移动] 动画完成");
        //    });
        //}
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

        // 发送网络消息
        NetGameSystem.Instance.SendUnitAttackMessage(
            localPlayerId,
            attackerPos,
            targetPlayerId,
            targetPos,
            attackerData.Value.PlayerUnitDataSO,
            targetData?.PlayerUnitDataSO,
            targetDestroyed
        );

        Debug.Log($"[SyncLocalUnitAttack] 已发送攻击同步消息，目标摧毁: {targetDestroyed}");
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