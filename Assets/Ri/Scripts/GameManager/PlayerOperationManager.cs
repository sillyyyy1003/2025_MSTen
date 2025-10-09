using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Unity.Mathematics;
using Newtonsoft.Json.Bson;

/// <summary>
/// 玩家操作管理，负责处理因玩家操作导致的数据变动
/// </summary>
public class PlayerOperationManager : MonoBehaviour
{
    // HexGrid的引用
    public GameObject HexGrid;


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

    // 其他玩家预制体(可以是不同颜色)
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
        // 左键点击 - 选择单位
        if (Input.GetMouseButtonDown(0) && bCanContinue)
        {
            HexGrid.GetComponent<HexGrid>().GetCell(selectCellID).DisableHighlight();
            HandleLeftClick();
        }

        // 右键点击 - 移动/攻击
        if (Input.GetMouseButtonDown(1) && bCanContinue)
        {
            HexGrid.GetComponent<HexGrid>().GetCell(selectCellID).DisableHighlight();
            HandleRightClick();
        }
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
                ChooseEmptyCell(ClickCellid);
                selectCellID = ClickCellid;
                SelectingUnit = null;
            }
        }
        else
        {
            ReturnToDefault();
            SelectingUnit = null;
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
                    Debug.Log("攻击功能尚未实现");
                    // TODO: 实现攻击逻辑
                    // AttackUnit(targetPos);
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
        CreatePlayerUnits(startBoardID);

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
        HexGrid.GetComponent<HexGrid>().GetCell(selectCellID).DisableHighlight();
        ReturnToDefault();
        SelectingUnit = null;



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

        Debug.Log($"更新玩家 {playerId} 的显示");

        // 如果没有这个玩家的字典,创建一个
        if (!otherPlayersUnits.ContainsKey(playerId))
        {
            otherPlayersUnits[playerId] = new Dictionary<int2, GameObject>();
        }

        // 清除旧的单位显示
        foreach (var unit in otherPlayersUnits[playerId].Values)
        {
            if (unit != null)
                Destroy(unit);
        }
        otherPlayersUnits[playerId].Clear();

        // 创建新的单位显示
        foreach (var unit in data.PlayerUnits)
        {
            Debug.Log($"创建敌方单位: {unit.UnitType} at ({unit.Position.x},{unit.Position.y})");
            CreateEnemyUnit(playerId, unit);
        }
    }

    // *************************
    //        私有函数
    // *************************

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
            unit.transform.position.y + 2.5f,
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
            unit.transform.position.y + 2.5f,
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

        Vector3 targetWorldPos = new Vector3(
            PlayerBoardInforDict[targetCellId].Cells3DPos.x,
            PlayerBoardInforDict[targetCellId].Cells3DPos.y + 2.5f,
            PlayerBoardInforDict[targetCellId].Cells3DPos.z
        );

        // 执行移动动画
        SelectingUnit.transform.DOMove(targetWorldPos, MoveSpeed).OnComplete(() =>
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

            //Debug.Log($"移动完成: ({fromPos.x},{fromPos.y}) -> ({toPos.x},{toPos.y})");
        });
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
        HexGrid.GetComponent<HexGrid>().GetCell(cell).EnableHighlight(Color.red);
    }

    //// 攻击单位(示例实现)
    //private void AttackUnit(int2 targetPos)
    //{
    //    // 1. 获取目标单位
    //    int targetPlayerId = playerDataManager.GetUnitOwner(targetPos);
    //    PlayerUnitData? targetUnit = playerDataManager.FindUnit(targetPlayerId, targetPos);

    //    if (!targetUnit.HasValue) return;

    //    // 2. 获取攻击者单位
    //    int2 attackerPos = PlayerBoardInforDict[LastSelectingCellID].Cells2DPos;
    //    PlayerUnitData? attacker = playerDataManager.FindUnit(localPlayerId, attackerPos);

    //    if (!attacker.HasValue) return;

    //    // 3. 计算伤害
    //    int damage = attacker.Value.Attack;
    //    int newHealth = targetUnit.Value.Health - damage;

    //    // 4. 更新目标血量
    //    playerDataManager.UpdateUnitHealth(targetPlayerId, targetPos, newHealth);

    //    // 5. 播放攻击动画
    //    // TODO: 实现攻击动画

    //    Debug.Log($"攻击! 造成 {damage} 伤害, 目标剩余血量: {newHealth}");
    //}

    // *************************
    //        事件处理
    // *************************

    private void OnUnitAddedHandler(int playerId, PlayerUnitData unitData)
    {
        if (playerId == localPlayerId)
        {
            // 本地玩家添加单位
            Debug.Log($"本地玩家添加单位: {unitData.UnitType} at ({unitData.Position.x},{unitData.Position.y})");
        }
        else
        {
            // 其他玩家添加单位
            CreateEnemyUnit(playerId, unitData);
        }
    }

    private void OnUnitRemovedHandler(int playerId, int2 position)
    {
        if (playerId == localPlayerId)
        {
            // 移除本地单位GameObject
            if (localPlayerUnits.ContainsKey(position))
            {
                Destroy(localPlayerUnits[position]);
                localPlayerUnits.Remove(position);
            }
        }
        else
        {
            // 移除其他玩家单位GameObject
            if (otherPlayersUnits.ContainsKey(playerId) &&
                otherPlayersUnits[playerId].ContainsKey(position))
            {
                Destroy(otherPlayersUnits[playerId][position]);
                otherPlayersUnits[playerId].Remove(position);
            }
        }

        GameManage.Instance.SetCellObject(position, null);
    }

    private void OnUnitMovedHandler(int playerId, int2 fromPos, int2 toPos)
    {
        // 本地玩家的移动已经在MoveToSelectCell中处理
        if (playerId != localPlayerId)
        {
            // 其他玩家的单位移动
            if (otherPlayersUnits.ContainsKey(playerId) &&
                otherPlayersUnits[playerId].ContainsKey(fromPos))
            {
                GameObject unit = otherPlayersUnits[playerId][fromPos];

                // 找到目标位置的世界坐标
                Vector3 targetPos = Vector3.zero;
                foreach (var board in PlayerBoardInforDict.Values)
                {
                    if (board.Cells2DPos.Equals(toPos))
                    {
                        targetPos = new Vector3(
                            board.Cells3DPos.x,
                            board.Cells3DPos.y + 2.5f,
                            board.Cells3DPos.z
                        );
                        break;
                    }
                }

                // 执行移动动画
                unit.transform.DOMove(targetPos, MoveSpeed);

                // 更新字典
                otherPlayersUnits[playerId].Remove(fromPos);
                otherPlayersUnits[playerId][toPos] = unit;

                // 更新GameManage
                GameManage.Instance.MoveCellObject(fromPos, toPos);
            }
        }
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