using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Unity.Mathematics;
using Newtonsoft.Json.Bson;
using UnityEngine.EventSystems;
using System.Dynamic;

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

    // 本地玩家ID
    private int localPlayerId = -1;

    // 本地玩家的所有单位GameObject (key: 位置, value: GameObject)
    private Dictionary<int2, GameObject> localPlayerUnits = new Dictionary<int2, GameObject>();

    // 其他玩家的单位GameObject
    private Dictionary<int, Dictionary<int2, GameObject>> otherPlayersUnits = new Dictionary<int, Dictionary<int2, GameObject>>();

    // 玩家数据管理器引用
    private PlayerDataManager playerDataManager;

    private int selectCellID;

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
        playerDataManager = PlayerDataManager.Instance;

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
            HandleLeftClick();
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
    private void HandleLeftClick()
    {
        Ray ray = GameCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // 射线检测
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, RayTestLayerMask))
        {
            ClickCellid = hit.collider.gameObject.GetComponent<HexCell>().id;
            int2 clickPos = GameManage.Instance.FindCell(ClickCellid).Cells2DPos;

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

                Debug.Log($"选择了单位: {clickPos}");
            }
            else
            {
                // 点击了空地或其他玩家单位
                ReturnToDefault();
                SelectingUnit = null;

                // 检查是否是空格子
                if (!playerDataManager.IsPositionOccupied(clickPos)&& _HexGrid.IsValidDestination(_HexGrid.GetCell(ClickCellid)))
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
            ClickCellid = hit.collider.gameObject.GetComponent<HexCell>().id;
            int2 targetPos = GameManage.Instance.FindCell(ClickCellid).Cells2DPos;

            // 检查目标位置
            if (playerDataManager.IsPositionOccupied(targetPos))
            {
                int ownerId = playerDataManager.GetUnitOwner(targetPos);
                if (ownerId != localPlayerId)
                {
                    // 攻击敌方单位
                    //Debug.Log("攻击功能尚未实现");
                    // TODO: 实现攻击逻辑
                    AttackUnit(targetPos, ClickCellid);
                }
                else
                {
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


    // *************************
    //         公有函数
    // *************************

    /// <summary>
    /// 尝试在当前选中的空格子创建单位
    /// </summary>
    /// <param name="unitType">要创建的单位类型</param>
    /// <returns>是否成功创建</returns>
    public bool TryCreateUnit(PlayerUnitType unitType)
    {
        // 检查是否选中了空格子
        if (SelectedEmptyCellID == -1)
        {
            Debug.LogWarning("未选中任何空格子");
            return false;
        }

        // 获取选中格子的信息
        BoardInfor cellInfo = GameManage.Instance.GetBoardInfor(SelectedEmptyCellID);
        int2 cellPos = cellInfo.Cells2DPos;

        // 再次确认该位置是空的
        if (playerDataManager.IsPositionOccupied(cellPos))
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
        localPlayerData = playerDataManager.GetPlayerData(playerId);

        Debug.Log($"PlayerOperationManager: 初始化玩家 {playerId}");

        // 创建玩家拥有的单位
        //CreatePlayerUnits(startBoardID);

        // 添加数据变化事件
        if (playerDataManager != null)
        {
            playerDataManager.OnUnitAdded += OnUnitAddedHandler;
            playerDataManager.OnUnitRemoved += OnUnitRemovedHandler;
            playerDataManager.OnUnitMoved += OnUnitMovedHandler;
        }
    }

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
        localPlayerData = playerDataManager.GetPlayerData(localPlayerId);

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

        

        // 清除旧的单位显示
        foreach (var unit in otherPlayersUnits[playerId].Values)
        {
            if (unit != null)
            {
                Destroy(unit);
                Debug.Log("unit is " + unit.name);
            }

        }
        otherPlayersUnits[playerId].Clear();

        // 创建新的单位显示
        foreach (var unit in data.PlayerUnits)
        {
            Debug.LogWarning($"创建敌方单位: {unit.UnitType} at ({unit.Position.x},{unit.Position.y})");
            CreateEnemyUnit(playerId, unit);
        }
    }

    // *************************
    //        私有函数
    // *************************


    // 在指定位置创建单位
    private void CreateUnitAtPosition(PlayerUnitType unitType, int cellId)
    {
        BoardInfor cellInfo = GameManage.Instance.GetBoardInfor(cellId);
        int2 position = cellInfo.Cells2DPos;
        Vector3 worldPos = cellInfo.Cells3DPos;

        // 选择对应的预制体
        GameObject prefab = null;
        switch (unitType)
        {
            case PlayerUnitType.Farmer:
                prefab = FarmerPrefab;
                break;
            case PlayerUnitType.Soldier:
                prefab = SoldierPrefab;
                break;
            //case PlayerUnitType.Missionary:
            //    prefab = MissionaryPrefab;
            //    break;
            default:
                Debug.LogError($"未知的单位类型: {unitType}");
                return;
        }

        if (prefab == null)
        {
            Debug.LogError($"预制体为空: {unitType}");
            return;
        }

        // 创建GameObject
        GameObject unit = Instantiate(prefab, worldPos, prefab.transform.rotation);
        unit.transform.position = new Vector3(
            unit.transform.position.x,
            unit.transform.position.y + 2.5f,
            unit.transform.position.z
        );

        // 添加到数据管理器
        playerDataManager.AddUnit(localPlayerId, unitType, position);

        // 保存本地引用
        localPlayerUnits[position] = unit;
        GameManage.Instance.SetCellObject(position, unit);

        Debug.Log($"在 ({position.x},{position.y}) 创建了 {unitType}");

        // 发送网络消息
        if (GameManage.Instance._NetGameSystem != null)
        {
            UnitAddMessage msg = new UnitAddMessage
            {
                PlayerId = localPlayerId,
                UnitType = (int)unitType,
                PosX = position.x,
                PosY = position.y
            };
            GameManage.Instance._NetGameSystem.SendMessage(NetworkMessageType.UNIT_ADD, msg);
        }
    }


    // 创建玩家单位
    private void CreatePlayerUnits(int startBoardID)
    {
        // 清空现有单位
        foreach (var a in localPlayerUnits.Values)
        {
            if (a != null)
                Destroy(a);
        }
        localPlayerUnits.Clear();

        // 在起始位置创建一个初始单位
        int2 startPos = GameManage.Instance.GetBoardInfor(startBoardID).Cells2DPos;
        Vector3 worldPos = GameManage.Instance.GetBoardInfor(startBoardID).Cells3DPos;

        // 添加到数据
        playerDataManager.AddUnit(localPlayerId, PlayerUnitType.Farmer, startPos);

        // 创建GameObject
        GameObject prefab = FarmerPrefab != null ? FarmerPrefab : GameObject.CreatePrimitive(PrimitiveType.Cube);
        GameObject unit = Instantiate(prefab, worldPos, prefab.transform.rotation);

        unit.transform.position = new Vector3(
            unit.transform.position.x,
            unit.transform.position.y + 6.5f,
            unit.transform.position.z
        );

        // 保存引用
        localPlayerUnits[startPos] = unit;
        GameManage.Instance.SetCellObject(startPos, unit);

        Debug.Log($"在 ({startPos.x},{startPos.y}) 创建了初始单位");
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
        GameObject prefab = unitData.UnitType == PlayerUnitType.Farmer ?
            (EnemyFarmerPrefab != null ? EnemyFarmerPrefab : FarmerPrefab) :
            (EnemySoldierPrefab != null ? EnemySoldierPrefab : SoldierPrefab);

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

        _HexGrid.FindPath(LastSelectingCellID, targetCellId,10);
        if(_HexGrid.HasPath)
        {
            List<HexCell> listCellPos = _HexGrid.GetPath();

            Sequence moveSequence = DOTween.Sequence();
            for (int i = 0; i < listCellPos.Count; i++)
            {
                // 根据路径坐标找到对应的格子信息
                Vector3 waypoint = new Vector3(
                   listCellPos[i].Position.x,
                   listCellPos[i].Position.y +2.5f,
                    listCellPos[i].Position.z
                    );
                moveSequence.Append(SelectingUnit.transform.DOMove(waypoint, MoveSpeed)
                  .SetEase(Ease.Linear));
                Debug.Log("2Dpos is " + PlayerBoardInforDict[i].Cells2DPos+
                    "3Dpos is "+ PlayerBoardInforDict[i].Cells3DPos);
            }
            moveSequence.OnComplete(() =>
            {
                // 动画完成后更新数据
                bCanContinue = true;

                     // 更新本地数据
                     playerDataManager.MoveUnit(localPlayerId, fromPos, toPos);

                     // 更新本地引用
                     localPlayerUnits.Remove(fromPos);
                     localPlayerUnits[toPos] = SelectingUnit;

                     // 更新GameManage的格子对象
                     GameManage.Instance.MoveCellObject(fromPos, toPos);

                     LastSelectingCellID = targetCellId;

                    _HexGrid.ClearPath();

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

    // 攻击单位(示例实现)
    private void AttackUnit(int2 targetPos, int targetCellId)
    {
        if (SelectingUnit == null) return;

        bCanContinue = false;

        // 获取攻击者位置
        int2 attackerPos = PlayerBoardInforDict[LastSelectingCellID].Cells2DPos;

        // 获取目标单位的拥有者
        int targetOwnerId = playerDataManager.GetUnitOwner(targetPos);

        if (targetOwnerId == -1)
        {
            Debug.LogWarning("目标位置没有单位");
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

        Debug.Log($"攻击敌方单位: 玩家{targetOwnerId} at ({targetPos.x},{targetPos.y})");

        // 获取目标世界坐标
        Vector3 targetWorldPos = new Vector3(
            PlayerBoardInforDict[targetCellId].Cells3DPos.x,
            PlayerBoardInforDict[targetCellId].Cells3DPos.y + 6.5f,
            PlayerBoardInforDict[targetCellId].Cells3DPos.z
        );

        // 创建攻击动画序列
        Sequence attackSequence = DOTween.Sequence();

        // 1. 向前冲刺
        Vector3 attackPos = Vector3.Lerp(SelectingUnit.transform.position, targetWorldPos, 0.7f);
        attackSequence.Append(SelectingUnit.transform.DOMove(attackPos, MoveSpeed * 0.3f));

        // 2. 目标单位消失效果
        if (targetUnit != null)
        {
            attackSequence.Join(targetUnit.transform.DOScale(0f, 0.2f));
            attackSequence.Join(targetUnit.transform.DORotate(new Vector3(0, 360, 0), 0.2f, RotateMode.FastBeyond360));
        }

        // 3. 移动到目标位置
        attackSequence.Append(SelectingUnit.transform.DOMove(targetWorldPos, MoveSpeed * 0.3f));

        // 4. 完成后的处理
        attackSequence.OnComplete(() =>
        {
            // 移除敌方单位数据
            playerDataManager.RemoveUnit(targetOwnerId, targetPos);

            // 移动攻击者数据
            playerDataManager.MoveUnit(localPlayerId, attackerPos, targetPos);

            // 更新本地引用
            localPlayerUnits.Remove(attackerPos);
            localPlayerUnits[targetPos] = SelectingUnit;

            // 更新GameManage的格子对象
            GameManage.Instance.MoveCellObject(attackerPos, targetPos);

            LastSelectingCellID = targetCellId;

            bCanContinue = true;

            Debug.Log($"攻击并移动完成: ({attackerPos.x},{attackerPos.y}) -> ({targetPos.x},{targetPos.y})");
        });

        // 发送网络消息
        if (GameManage.Instance._NetGameSystem != null)
        {
            UnitAttackMessage attackMsg = new UnitAttackMessage
            {
                AttackerPlayerId = localPlayerId,
                AttackerPosX = attackerPos.x,
                AttackerPosY = attackerPos.y,
                TargetPlayerId = targetOwnerId,
                TargetPosX = targetPos.x,
                TargetPosY = targetPos.y
            };
            GameManage.Instance._NetGameSystem.SendMessage(NetworkMessageType.UNIT_ATTACK, attackMsg);
            Debug.Log($"[本地] 已发送攻击消息到网络");
        }
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
        if (playerDataManager != null)
        {
            // 移除被攻击的单位
            playerDataManager.RemoveUnit(msg.TargetPlayerId, targetPos);
            // 移动攻击者
            playerDataManager.MoveUnit(msg.AttackerPlayerId, attackerPos, targetPos);
            Debug.Log($"[网络攻击] 已更新数据管理器");
        }

        // 获取攻击者和目标的GameObject
        GameObject attackerUnit = null;
        GameObject targetUnit = null;

        // 查找攻击者单位
        if (msg.AttackerPlayerId == localPlayerId)
        {
            if (localPlayerUnits.ContainsKey(attackerPos))
            {
                attackerUnit = localPlayerUnits[attackerPos];
            }
        }
        else
        {
            if (otherPlayersUnits.ContainsKey(msg.AttackerPlayerId) &&
                otherPlayersUnits[msg.AttackerPlayerId].ContainsKey(attackerPos))
            {
                attackerUnit = otherPlayersUnits[msg.AttackerPlayerId][attackerPos];
            }
        }

        // 查找目标单位
        if (msg.TargetPlayerId == localPlayerId)
        {
            if (localPlayerUnits.ContainsKey(targetPos))
            {
                targetUnit = localPlayerUnits[targetPos];
            }
        }
        else
        {
            if (otherPlayersUnits.ContainsKey(msg.TargetPlayerId) &&
                otherPlayersUnits[msg.TargetPlayerId].ContainsKey(targetPos))
            {
                targetUnit = otherPlayersUnits[msg.TargetPlayerId][targetPos];
            }
        }

        // 播放攻击动画
        if (attackerUnit != null)
        {
            // 找到目标位置的世界坐标
            Vector3 targetWorldPos = Vector3.zero;
            foreach (var board in PlayerBoardInforDict.Values)
            {
                if (board.Cells2DPos.Equals(targetPos))
                {
                    targetWorldPos = new Vector3(
                        board.Cells3DPos.x,
                        board.Cells3DPos.y + 6.5f,
                        board.Cells3DPos.z
                    );
                    break;
                }
            }

            // 创建攻击动画
            Sequence attackSequence = DOTween.Sequence();

            // 冲刺效果
            Vector3 attackPos = Vector3.Lerp(attackerUnit.transform.position, targetWorldPos, 0.7f);
            attackSequence.Append(attackerUnit.transform.DOMove(attackPos, MoveSpeed * 0.3f));

            // 目标消失效果
            if (targetUnit != null)
            {
                attackSequence.Join(targetUnit.transform.DOScale(0f, 0.2f));
                attackSequence.Join(targetUnit.transform.DORotate(new Vector3(0, 360, 0), 0.2f, RotateMode.FastBeyond360));
            }

            // 移动到目标位置
            attackSequence.Append(attackerUnit.transform.DOMove(targetWorldPos, MoveSpeed * 0.3f));

            // 完成后更新引用
            attackSequence.OnComplete(() =>
            {
                // 销毁目标单位
                if (targetUnit != null)
                {
                    Destroy(targetUnit);
                }

                // 更新攻击者单位的引用
                if (msg.AttackerPlayerId == localPlayerId)
                {
                    localPlayerUnits.Remove(attackerPos);
                    localPlayerUnits[targetPos] = attackerUnit;
                }
                else
                {
                    if (otherPlayersUnits.ContainsKey(msg.AttackerPlayerId))
                    {
                        otherPlayersUnits[msg.AttackerPlayerId].Remove(attackerPos);
                        otherPlayersUnits[msg.AttackerPlayerId][targetPos] = attackerUnit;
                    }
                }

                // 移除目标单位的引用
                if (msg.TargetPlayerId == localPlayerId)
                {
                    localPlayerUnits.Remove(targetPos);
                }
                else
                {
                    if (otherPlayersUnits.ContainsKey(msg.TargetPlayerId))
                    {
                        otherPlayersUnits[msg.TargetPlayerId].Remove(targetPos);
                    }
                }

                // 更新GameManage
                GameManage.Instance.MoveCellObject(attackerPos, targetPos);

                Debug.Log($"[网络攻击] 动画完成: ({attackerPos.x},{attackerPos.y}) -> ({targetPos.x},{targetPos.y})");
            });
        }
        else
        {
            Debug.LogWarning($"[网络攻击] 找不到攻击者单位 at ({attackerPos.x},{attackerPos.y})");

            // 如果找不到攻击者单位，至少销毁目标
            if (targetUnit != null)
            {
                Destroy(targetUnit);

                if (msg.TargetPlayerId == localPlayerId)
                {
                    localPlayerUnits.Remove(targetPos);
                }
                else if (otherPlayersUnits.ContainsKey(msg.TargetPlayerId))
                {
                    otherPlayersUnits[msg.TargetPlayerId].Remove(targetPos);
                }
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

        // 先更新数据（不触发事件，因为我们自己处理视觉）
        if (playerDataManager != null)
        {
            playerDataManager.MoveUnit(msg.PlayerId, fromPos, toPos);
            Debug.Log($"[网络移动] 已更新数据管理器");
        }

        // 获取移动的单位GameObject
        GameObject movingUnit = null;

        if (otherPlayersUnits.ContainsKey(msg.PlayerId) &&
            otherPlayersUnits[msg.PlayerId].ContainsKey(fromPos))
        {
            movingUnit = otherPlayersUnits[msg.PlayerId][fromPos];
            Debug.Log($"[网络移动] 找到移动单位");
        }
        else
        {
            Debug.LogWarning($"[网络移动] 找不到移动的单位 at ({fromPos.x},{fromPos.y})");

            // 打印当前玩家的所有单位位置
            if (otherPlayersUnits.ContainsKey(msg.PlayerId))
            {
                Debug.Log($"[网络移动] 玩家 {msg.PlayerId} 当前单位位置：");
                foreach (var kvp in otherPlayersUnits[msg.PlayerId])
                {
                    Debug.Log($"  - ({kvp.Key.x},{kvp.Key.y})");
                }
            }
            return;
        }

        if (movingUnit != null)
        {
            // 找到目标位置的世界坐标
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

            Debug.Log($"[网络移动] 开始更新引用和执行动画");

            // 立即更新字典引用（在动画之前）
            otherPlayersUnits[msg.PlayerId].Remove(fromPos);
            otherPlayersUnits[msg.PlayerId][toPos] = movingUnit;

            // 更新GameManage的格子对象
            GameManage.Instance.SetCellObject(fromPos, null);
            GameManage.Instance.SetCellObject(toPos, movingUnit);

            Debug.Log($"[网络移动] 字典已更新，开始动画");

            // 执行移动动画
            movingUnit.transform.DOMove(targetWorldPos, MoveSpeed).OnComplete(() =>
            {
                Debug.Log($"[网络移动] 动画完成");
            });
        }
    }

    // 处理来自网络的创建单位消息
    public void HandleNetworkAddUnit(UnitAddMessage msg)
    {
        if (msg.PlayerId == localPlayerId)
        {
            Debug.Log("[网络创建] 这是本地玩家的创建，已处理");
            return;
        }

        int2 pos = new int2(msg.PosX, msg.PosY);
        PlayerUnitType unitType = (PlayerUnitType)msg.UnitType;

        Debug.Log($"[网络创建] 玩家 {msg.PlayerId} 创建单位: {unitType} at ({pos.x},{pos.y})");

        // 先更新数据
        if (playerDataManager != null)
        {
            playerDataManager.AddUnit(msg.PlayerId, unitType, pos);
            Debug.Log($"[网络创建] 已更新数据管理器");
        }

        // 创建单位数据
        PlayerUnitData unitData = new PlayerUnitData(unitType, pos);

        // 创建视觉对象
        CreateEnemyUnit(msg.PlayerId, unitData);

        Debug.Log($"[网络创建] 完成");
    }

    private void OnUnitAddedHandler(int playerId, PlayerUnitData unitData)
    {
        //if (playerId == localPlayerId)
        //{
        //    // 本地玩家添加单位
        //    Debug.Log($"本地玩家添加单位: {unitData.UnitType} at ({unitData.Position.x},{unitData.Position.y})");
        //}
        //else
        //{
        //    // 其他玩家添加单位
        //    CreateEnemyUnit(playerId, unitData);
        //}
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
        //Debug.Log($"[事件] OnUnitMovedHandler: 玩家 {playerId}");

      
        //if (playerId == localPlayerId)
        //{
        //    Debug.Log("[事件] 本地玩家移动已在 MoveToSelectCell 中处理");
        //    // 本地玩家的移动已经在MoveToSelectCell中处理，这里不需要再处理
        //    return;
        //}
        //else
        //{
        //    Debug.Log("[事件] 其他玩家移动由 HandleNetworkMove 处理，跳过");
        //    // 其他玩家的移动由 HandleNetworkMove 处理，这里不处理
        //    return;
        //}
    }

    // *************************
    //        清理
    // *************************

    private void OnDestroy()
    {
        // 取消订阅事件
        if (playerDataManager != null)
        {
            playerDataManager.OnUnitAdded -= OnUnitAddedHandler;
            playerDataManager.OnUnitRemoved -= OnUnitRemovedHandler;
            playerDataManager.OnUnitMoved -= OnUnitMovedHandler;
        }
    }
}