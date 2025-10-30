using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

using UnityEngine.UIElements;
using static UnityEngine.Rendering.DebugUI.Table;

// 棋盘每个格子信息的结构体
public struct BoardInfor
{

    // 每个格子的二维坐标
    public int2 Cells2DPos;

    // 每个格子的世界坐标
    public Vector3 Cells3DPos;

    // 每个格子的id
    public int id;

    // 每个格子的地块信息 是否可通过
    public TerrainType type;
    
    // 是否是起始位置
    public bool bIsStartPos;



};

// 游戏开始数据
[Serializable]
public class GameStartData
{
    public int[] PlayerIds;
    public int[] StartPositions;
    public int FirstTurnPlayerId;
}

// 回合结束数据
[Serializable]
public class TurnEndData
{
    public int PlayerId;
    public PlayerData UpdatedPlayerData;
}



[Serializable]
public class PlayerDataSyncMessage
{
    public int PlayerId;
    public PlayerData PlayerData;
}


public class GameManage : MonoBehaviour
{
    // 单例
    public static GameManage Instance { get; private set; }

    // *************************
    //          私有属性
    // *************************

    // 是否在游戏中
    private bool bIsInGaming;

    // 当前回合玩家ID
    private int _CurrentTurnPlayerID = -1;

    // 本地玩家ID
    private int _LocalPlayerID = -1;

    // 游戏中的所有玩家ID列表
    private List<int> AllPlayerIds = new List<int>();

    // 玩家起始位置ID列表
    private List<int> PlayerStartPositions = new List<int>();

    // 棋盘信息List与Dictionary
    private List<BoardInfor> GameBoardInfor = new List<BoardInfor>();
    private Dictionary<int, BoardInfor> GameBoardInforDict = new Dictionary<int, BoardInfor>();
    private Dictionary<int2, BoardInfor> GameBoardInforDict2D = new Dictionary<int2, BoardInfor>();


    // 每个格子上的GameObject (所有玩家)
    private Dictionary<int2, GameObject> CellObjects = new Dictionary<int2, GameObject>();

    private bool bIsStartSingleGame = false;
    // *************************
    //         公有属性
    // *************************


    // 网络系统引用 (在Inspector中赋值或通过代码获取)
    public NetGameSystem _NetGameSystem;

    // 玩家相机引用
    public GameCamera _GameCamera;
    // 玩家操作管理器
    public PlayerOperationManager _PlayerOperation;

    // 单位创建引用
    public Instantiater _Instantiater;
    // 玩家数据管理器引用
    private PlayerDataManager _PlayerDataManager;




    // 事件: 回合开始
    public event Action<int> OnTurnStarted;

    // 事件: 回合结束
    public event Action<int> OnTurnEnded;

    // 事件: 游戏开始
    public event Action OnGameStarted;

    // 事件: 游戏结束
    public event Action<int> OnGameEnded;


    // *************************
    //        属性访问器
    // *************************

    public int LocalPlayerID => _LocalPlayerID;
    public int CurrentTurnPlayerID => _CurrentTurnPlayerID;
    public bool IsMyTurn => _CurrentTurnPlayerID == _LocalPlayerID;
    public Dictionary<int, BoardInfor> GetPlayerBoardInfor() { return GameBoardInforDict; }

    // 设置当前是否在游戏中
    public void SetIsGamingOrNot(bool isGaming) { bIsInGaming = isGaming; }

    // 得到游戏是否在运行
    public bool GetIsGamingOrNot() { return bIsInGaming; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            //Debug.Log($" GameManage.Instance 已设置 (GameObject: {gameObject.name})");
        }
        else if (Instance != this)
        {
            Debug.LogWarning($"GameManage 单例已存在，销毁重复对象: {gameObject.name}");
            Destroy(gameObject);
            return;
        }

        // 获取PlayerDataManager引用
        _PlayerDataManager = PlayerDataManager.Instance;
        if (_PlayerDataManager == null)
        {
            GameObject pdm = new GameObject("PlayerDataManager");
            _PlayerDataManager = pdm.AddComponent<PlayerDataManager>();
        }
        
    }



    // Update is called once per frame
   

    // *************************
    //        游戏流程函数
    // *************************

    // 房间状态待机管理
    public void CheckIsServer(bool server)
    {
        _GameCamera.SetCanUseCamera(false);
        if(server)
        {

        }
        else
        {

        }
    }

    // 从房间UI处获取游戏开始事件
    // 在此处设置单机或联机启动
    public void StartGameFromRoomUI()
    {
        _GameCamera.SetCanUseCamera(true);

       
        // 单机测试
        GameInit();
    }

    // 游戏初始化 (由网络系统调用,传入游戏开始数据)
    public bool InitGameWithNetworkData(GameStartData data)
    {
        Debug.Log("GameManage: 开始初始化游戏...");
        Debug.Log($"接收到玩家数: {data.PlayerIds.Length}");
        Debug.Log($"起始位置数: {data.StartPositions.Length}");
        
        // 清空之前的数据
        AllPlayerIds.Clear();
        PlayerStartPositions.Clear();
        CellObjects.Clear();

        // 设置游戏状态
        SetIsGamingOrNot(true);
        Debug.Log($"游戏状态已设置: {bIsInGaming}");

        // 保存玩家信息
        foreach (var playerId in data.PlayerIds)
        {
            AllPlayerIds.Add(playerId);
            _PlayerDataManager.CreatePlayer(playerId);
            Debug.Log($"创建玩家数据: {playerId}");
        }

        // 保存起始位置，后续更改
        foreach (var pos in data.StartPositions)
        {
            PlayerStartPositions.Add(pos);
            Debug.Log($"添加起始位置: {pos}");
        }

        // 确定本地玩家ID (如果是客户端,从网络系统获取)
        if (_NetGameSystem != null && !_NetGameSystem.bIsServer)
        {
            _LocalPlayerID = (int)_NetGameSystem.bLocalClientId;
            SceneStateManager.Instance.PlayerID = _LocalPlayerID;
            // 这里需要NetGameSystem提供本地客户端ID
            // localPlayerID = netGameSystem.GetLocalClientId();
            // 临时方案: 假设第一个玩家是本地玩家
            //_LocalPlayerID = data.PlayerIds[0];
        }
        else
        {
            _LocalPlayerID = 0; // 服务器默认是玩家0
            SceneStateManager.Instance.PlayerID = _LocalPlayerID;
        }

        Debug.Log($"本地玩家ID: {LocalPlayerID}");

        // 初始化棋盘数据 (如果还没有初始化)
        if (GameBoardInforDict.Count > 0)
        {
            Debug.Log("开始初始化本地玩家...");
            // 找到本地玩家的起始位置
            int localPlayerIndex = AllPlayerIds.IndexOf(LocalPlayerID);
            Debug.Log($"本地玩家索引: {localPlayerIndex}");

            if (localPlayerIndex >= 0 && localPlayerIndex < PlayerStartPositions.Count)
            {
                int startPos = PlayerStartPositions[localPlayerIndex];
                Debug.Log($"本地玩家起始位置: {startPos}");

                // 使用起始位置初始化
                _PlayerOperation.InitPlayer(_LocalPlayerID, startPos);
            }
            else
            {
                Debug.LogError($"无法找到本地玩家的起始位置! Index: {localPlayerIndex}");
            }
        }

        _NetGameSystem.GetGameManage();

        _GameCamera.SetCanUseCamera(true);
        // 触发游戏开始事件
        OnGameStarted?.Invoke();

        // 开始第一个玩家的回合
        StartTurn(data.FirstTurnPlayerId);

        return true;
    }

    // 游戏初始化 (测试用)
    public bool GameInit()
    {
        Debug.Log("GameManage: 本地测试模式初始化...");

        SetIsGamingOrNot(true);
     
        // 使用协程开始游戏，避免脚本Start执行顺序问题
           if (!bIsStartSingleGame)
                StartCoroutine(TrueStartGame());
        
      
        return true;
    }
    private IEnumerator TrueStartGame()
    {
        yield return 0.1f;
        if (GameBoardInforDict.Count > 0)
        {
            // 添加默认玩家
            _LocalPlayerID = 0;
            AllPlayerIds.Add(0);


            _PlayerDataManager.CreatePlayer(0);

            // 添加玩家初始格子位置的id
            PlayerStartPositions.Add(0);
            PlayerStartPositions.Add(GameBoardInforDict.Count - 1);

            _PlayerOperation.InitPlayer(_LocalPlayerID, PlayerStartPositions[0]);
            // 初始化本机玩家
            // 起始位置方法，待地图起始位置功能实装后使用
            //_PlayerOperation.InitPlayer(_LocalPlayerID, PlayerStartPositions[0]);
            //_GameCamera.GetPlayerPosition(GameBoardInforDict[PlayerStartPositions[0]].Cells3DPos);


            Debug.Log("本地玩家初始化完毕");
            // 开始第一个回合
            StartTurn(0);
            bIsStartSingleGame = true;
        }

    }
    // 游戏结束
    public bool GameOver(int winnerPlayerId = -1)
    {
        Debug.Log($"游戏结束! 胜者: {(winnerPlayerId == -1 ? "平局" : $"玩家{winnerPlayerId}")}");

        // 清空数据
        AllPlayerIds.Clear();
        PlayerStartPositions.Clear();
        CellObjects.Clear();

        // 清空棋盘信息
        GameBoardInfor.Clear();
        GameBoardInforDict.Clear();

        // 清空玩家数据
        _PlayerDataManager.ClearAllPlayers();

        SetIsGamingOrNot(false);
        _CurrentTurnPlayerID = -1;

        // 触发游戏结束事件
        OnGameEnded?.Invoke(winnerPlayerId);

        return true;
    }


    // *************************
    //        回合管理函数
    // *************************

    // 开始回合
    public void StartTurn(int playerId)
    {
        _CurrentTurnPlayerID = playerId;

        Debug.Log($"回合开始: 玩家 {playerId}" + (IsMyTurn ? " (你的回合)" : " (等待中)"));

        // 触发回合开始事件
        OnTurnStarted?.Invoke(playerId);

        // 确保 PlayerOperationManager 引用有效
        if (_PlayerOperation == null)
        {
            Debug.LogError("PlayerOperationManager 引用为 null!");
            // 尝试重新获取引用
            _PlayerOperation = FindObjectOfType<PlayerOperationManager>();
            if (_PlayerOperation == null)
            {
                Debug.LogError("无法找到 PlayerOperationManager!");
                return;
            }
        }
        if (IsMyTurn)
        {
            // 本地玩家回合
            _PlayerOperation.TurnStart();

            // 更新UI
            if (GameSceneUIManager.Instance != null)
            {
                GameSceneUIManager.Instance.SetTurnText(true);
                GameSceneUIManager.Instance.SetEndTurn(true);
                GameSceneUIManager.Instance.StartTurn();
            }
        }
        else
        {
            // 其他玩家回合,禁用输入
            _PlayerOperation.DisableInput();

            // 更新UI
            if (GameSceneUIManager.Instance != null)
            {
                GameSceneUIManager.Instance.SetTurnText(false);
                GameSceneUIManager.Instance.SetEndTurn(false);
            }
        }
    }

    // 结束回合 (由本地玩家操作管理器调用)
    public void EndTurn()
    {
        if (!IsMyTurn)
        {
            Debug.LogWarning("不是你的回合,无法结束!");
            return;
        }

        Debug.Log($"玩家 {LocalPlayerID} 结束回合");

        // 获取本地玩家数据
        PlayerData localData = _PlayerDataManager.GetPlayerData(LocalPlayerID);

        // 创建回合结束消息
        TurnEndMessage turnEndMsg = new TurnEndMessage
        {
            PlayerId = LocalPlayerID,
            
            PlayerDataJson = SerializablePlayerData.FromPlayerData(localData)
        };

        // 发送到网络
        if (_NetGameSystem != null)
        {
            _NetGameSystem.SendMessage(NetworkMessageType.TURN_END, turnEndMsg);
            Debug.Log($" 已发送回合结束消息");



            NextTurn();
        }

        // 触发回合结束事件
        OnTurnEnded?.Invoke(LocalPlayerID);


    }

    /// <summary>
    /// 切换到下一个玩家回合
    /// </summary>
    private void NextTurn()
    {
        int currentIndex = AllPlayerIds.IndexOf(CurrentTurnPlayerID);
        int nextIndex = (currentIndex + 1) % AllPlayerIds.Count;
        int nextPlayerId = AllPlayerIds[nextIndex];

        Debug.Log($"[服务器] 切换到玩家 {nextPlayerId}");

        // 如果是服务器，广播 TURN_START
        if (_NetGameSystem != null && _NetGameSystem.bIsServer)
        {
            TurnStartMessage turnStartData = new TurnStartMessage
            {
                PlayerId = nextPlayerId
            };

            _NetGameSystem.SendMessage(NetworkMessageType.TURN_START, turnStartData);
            Debug.Log($"[服务器] 已广播 TURN_START 消息");
        }

        StartTurn(nextPlayerId);
    }

    // *************************
    //        网络消息处理
    // *************************

    private void OnReceiveGameStart(GameStartData data)
    {
        InitGameWithNetworkData(data);
    }

    private void OnReceiveTurnStart(int playerId)
    {
        StartTurn(playerId);
    }

    private void OnReceiveTurnEnd(TurnEndData data)
    {
        Debug.Log($"接收到玩家 {data.PlayerId} 的回合结束数据");

        // 更新该玩家的数据
        _PlayerDataManager.UpdatePlayerData(data.PlayerId, data.UpdatedPlayerData);

        // 如果不是本地玩家,需要更新显示
        if (data.PlayerId != LocalPlayerID)
        {
            _PlayerOperation.UpdateOtherPlayerDisplay(data.PlayerId, data.UpdatedPlayerData);
        }
    }

    private void OnReceivePlayerDataSync(PlayerDataSyncMessage data)
    {
        // 同步所有玩家数据
        _PlayerDataManager.UpdatePlayerData(data.PlayerId, data.PlayerData);

        if (data.PlayerId != LocalPlayerID)
        {
            _PlayerOperation.UpdateOtherPlayerDisplay(data.PlayerId, data.PlayerData);
        }
    }

    public void HandleNetworkTurnStart(NetworkMessage message)
    {
        try
        {
            // 使用 Newtonsoft.Json 反序列化
            TurnStartMessage data = JsonConvert.DeserializeObject<TurnStartMessage>(message.JsonData);
            Debug.Log($"GameManage: 接收到网络回合开始消息，玩家 {data.PlayerId}");
            StartTurn(data.PlayerId);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"处理回合开始消息失败: {ex.Message}");
        }
    }


    // 同步玩家数据的方法
    public void SyncPlayerData(int playerId, PlayerData data)
    {
        Debug.Log($"同步玩家 {playerId} 的数据，单位数: {data.GetUnitCount()}");

        // 更新数据管理器
        _PlayerDataManager.UpdatePlayerData(playerId, data);

        // 更新显示
        if (playerId != LocalPlayerID)
        {
            _PlayerOperation.UpdateOtherPlayerDisplay(playerId, data);
            Debug.Log($"已更新玩家 {playerId} 的显示");
        }
        else
        {
            Debug.Log($"这是本地玩家的数据，不需要更新显示");
        }
    }
    public void UpdateOtherPlayerShow(int playerId, PlayerData data)
    {
        _PlayerOperation.UpdateOtherPlayerDisplay(playerId,data);
    }


    // *************************
    //        数据查询函数
    // *************************

    public BoardInfor GetBoardInfor(int id)
    {
        if (GameBoardInforDict.ContainsKey(id))
            return GameBoardInforDict[id];

        Debug.LogWarning($"找不到格子ID: {id}");
        return default;
    }

    // 根据格子id返回其周围所有可创建单位的格子id
    public List<int> GetBoardNineSquareGrid(int id)
    {
        Debug.Log("pos is "+GetBoardInfor(id).Cells2DPos);
        List<int> startPos = new List<int>();
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
              
                    int2 pos = new int2(GameBoardInforDict[id].Cells2DPos.x + dx, GameBoardInforDict[id].Cells2DPos.y + dy);
                    if(GameBoardInforDict2D.ContainsKey(pos))
                    {
                        startPos.Add(GameBoardInforDict2D[pos].id);
                        //Debug.Log("pos is " + GetBoardInfor(GameBoardInforDict2D[pos].id).Cells2DPos);
                    }
            }
        }
        return startPos;
    }

    public int GetBoardCount()
    {
        return GameBoardInforDict.Count;
    }

    public BoardInfor FindCell(int id)
    {
        return GetBoardInfor(id);
    }

   
    // 查找某个格子上是否有单位
    public bool FindUnitOnCell(int2 pos)
    {
        return CellObjects.ContainsKey(pos) && CellObjects[pos] != null;
    }

    // 查找某个格子上是否有指定玩家的单位
    public bool FindPlayerUnitOnCell(int playerId, int2 pos)
    {
        PlayerData data = _PlayerDataManager.GetPlayerData(playerId);
        return data.FindUnitAt(pos) != null;
    }

    // 获取格子上的GameObject
    public GameObject GetCellObject(int2 pos)
    {
        if (CellObjects.ContainsKey(pos))
            return CellObjects[pos];
        return null;
    }

    // 设置格子上的GameObject
    public void SetCellObject(int2 pos, GameObject obj)
    {
        if (obj == null)
            CellObjects.Remove(pos);
        else
            CellObjects[pos] = obj;
    }

    // 移动格子上的对象
    public void MoveCellObject(int2 fromPos, int2 toPos)
    {
        if (CellObjects.ContainsKey(fromPos))
        {
            GameObject obj = CellObjects[fromPos];
            CellObjects.Remove(fromPos);
            CellObjects[toPos] = obj;
        }
    }
    public int GetStartPosForNetGame(int i)
    {
        return PlayerStartPositions[i];
    }
    // 设置棋盘结构体信息
    public void SetGameBoardInfor(BoardInfor infor)
    {
        // 如果已经储存数据 清除当前数据
        if(GameBoardInfor.Contains(infor))
        {
            GameBoardInfor.Clear();
            GameBoardInforDict.Clear();
            GameBoardInforDict2D.Clear();
        }

        GameBoardInfor.Add(infor);
        GameBoardInforDict.Add(infor.id, infor);
        GameBoardInforDict2D.Add(infor.Cells2DPos, infor);

        // 正确的添加起始位置方式  
        if (infor.bIsStartPos)
        {
            Debug.Log("Add start pos id :" + infor.id);
            PlayerStartPositions.Add(infor.id);
        }
        // 测试用
        //if (infor.id==24|| infor.id ==277)
        //{
        //    Debug.Log("Add start pos id :" + infor.id);
        //    PlayerStartPositions.Add(infor.id);
        //}
    }

  
    // *************************
    //        网络消息处理
    // *************************

  
    // 获取所有玩家ID
    public List<int> GetAllPlayerIds()
    {
        return new List<int>(AllPlayerIds);
    }

    // 获取玩家数据
    public PlayerData GetPlayerData(int playerId)
    {
        return _PlayerDataManager.GetPlayerData(playerId);
    }

    private void OnDestroy()
    {
        // 取消订阅网络事件
        //if (_NetGameSystem != null)
        //{
        //    _NetGameSystem.OnDataReceived -= HandleNetworkData;
        //    _NetGameSystem.OnConnected -= OnConnectedToServer;
        //}
    }
}