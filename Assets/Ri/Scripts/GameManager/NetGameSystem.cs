using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections;
using UnityEngine.SceneManagement;
using GameData;
using Unity.Mathematics;
using UnityEngine.Rendering.Universal;



// *************************
//      消息类型枚举
// *************************
public enum NetworkMessageType
{
    // 连接相关
    CONNECT,
    CONNECTED,
    PLAYER_JOINED,
    PLAYER_LEFT,

    // 游戏流程
    GAME_START,
    GAME_OVER,


    // 房间管理 
    PLAYER_READY,      // 玩家准备
    PLAYER_NOT_READY,  // 玩家取消准备
    ROOM_STATUS_UPDATE, // 房间状态更新

    // 回合管理
    TURN_START,
    TURN_END,

    // 玩家操作
    UNIT_MOVE,
    UNIT_ADD,
    UNIT_REMOVE,
    UNIT_ATTACK,
    BUILDING_ATTACK,     // 建筑攻击
    UNIT_CHARM,          // 单位魅惑
    CHARM_EXPIRE,        // 魅惑过期（归还控制权）

    // 同步
    SYNC_DATA,
    PING,
    PONG
}

// *************************
//      网络消息结构
// *************************
[Serializable]
public class NetworkMessage
{
    public NetworkMessageType MessageType;
    public uint SenderId;
    public string JsonData;
    public long Timestamp;

    public NetworkMessage()
    {
        Timestamp = DateTime.Now.Ticks;
    }
}

// *************************
//      具体消息数据
// *************************
[Serializable]
public class ConnectMessage
{
    public string PlayerName;
    public string PlayerIP;
}

[Serializable]
public class ConnectedMessage
{
    public uint AssignedClientId;
    public List<uint> ExistingPlayerIds;
}

// 房间内玩家信息
[Serializable]
public class PlayerInfo
{
    public uint PlayerId;
    public string PlayerName;
    public string PlayerIP;
    public bool IsReady;
}

// 玩家加入消息
[Serializable]
public class PlayerJoinedMessage
{
    public uint PlayerId;
    public string PlayerName;
    public string PlayerIP;
}

// 玩家准备消息
[Serializable]
public class PlayerReadyMessage
{
    public uint PlayerId;
    public bool IsReady;
}

// 房间状态更新消息
[Serializable]
public class RoomStatusUpdateMessage
{
    public List<PlayerInfo> Players;
}

[Serializable]
public class UnitMoveMessage
{
    public int PlayerId;
    public int FromX;
    public int FromY;
    public int ToX;
    public int ToY;
    public syncPieceData MovedUnitSyncData; // 移动后的单位同步数据
}

[Serializable]
public class UnitAddMessage
{
    public int PlayerId;
    public int UnitType; // PlayerUnitType as int
    public int PosX;
    public int PosY;
    public syncPieceData NewUnitSyncData; // PieceDataSO序列化为JSON字符串
    public syncBuildingData? BuildingData;  // 新增
    public bool IsUsed; // 单位是否已使用
}

[Serializable]
public class UnitRemoveMessage
{
    public int PlayerId;
    public int PosX;
    public int PosY;
    public int UnitID; // 被移除单位的ID
}

// 攻击消息
[Serializable]
public class UnitAttackMessage
{
    public int AttackerPlayerId;
    public int AttackerPosX;
    public int AttackerPosY;

    // 原始位置字段
    public int AttackerOriginalPosX;    // 攻击者原始位置（移动前的位置）
    public int AttackerOriginalPosY;
    public bool HasMoved;               // 标记攻击者是否进行了移动

    public int TargetPlayerId;
    public int TargetPosX;
    public int TargetPosY;
    public syncPieceData AttackerSyncData; // 攻击者的同步数据（HP可能变化）
    public syncPieceData? TargetSyncData;  // 目标的同步数据（如果存活），null表示被击杀
    public bool TargetDestroyed;           // 目标是否被摧毁
}

// 建筑攻击消息
[Serializable]
public class BuildingAttackMessage
{
    public int AttackerPlayerId;
    public int AttackerPosX;
    public int AttackerPosY;

    public int BuildingOwnerId;
    public int BuildingPosX;
    public int BuildingPosY;
    public int BuildingID;             // 被攻击的建筑ID

    public syncPieceData AttackerSyncData; // 攻击者的同步数据
    public int BuildingRemainingHP;    // 建筑剩余HP
    public bool BuildingDestroyed;     // 建筑是否被摧毁
}

// 魅惑消息
[Serializable]
public class UnitCharmMessage
{
    public int MissionaryPlayerId;      // 传教士所属玩家ID
    public int MissionaryID;            // 传教士ID
    public int MissionaryPosX;          // 传教士位置
    public int MissionaryPosY;

    public int TargetPlayerId;          // 目标单位原始所有者ID
    public int TargetID;                // 目标单位ID
    public int TargetPosX;              // 目标单位位置
    public int TargetPosY;

    public syncPieceData NewUnitSyncData;  // 新创建的被魅惑单位的同步数据
    public int CharmedTurns;            // 魅惑持续回合数
}

// 魅惑过期消息（归还控制权）
[Serializable]
public class CharmExpireMessage
{
    public int CurrentOwnerId;          // 当前控制者ID（魅惑者）
    public int OriginalOwnerId;         // 原始所有者ID
    public int UnitID;                  // 单位ID
    public int PosX;                    // 单位位置
    public int PosY;
    public syncPieceData UnitSyncData;  // 单位同步数据
}

[Serializable]
public class TurnEndMessage
{
    public int PlayerId;
    public SerializablePlayerData PlayerDataJson; // PlayerData序列化
}

// 辅助消息类
[Serializable]
public class TurnStartMessage
{
    public int PlayerId;
}

[Serializable]
public struct SerializablePlayerUnitData
{
    public int UnitID;
    public int UnitType;
    public int PositionX;
    public int PositionY;
    public bool bUnitIsActivated;
    public syncPieceData SyncData;

    public static SerializablePlayerUnitData FromPlayerUnitData(PlayerUnitData data)
    {
        return new SerializablePlayerUnitData
        {
            UnitID = data.UnitID,
            UnitType = (int)data.UnitType,
            PositionX = data.Position.x,
            PositionY = data.Position.y,
            bUnitIsActivated = data.bUnitIsActivated,
            SyncData = data.PlayerUnitDataSO

        };
    }

    public PlayerUnitData ToPlayerUnitData()
    {
        return new PlayerUnitData(
            UnitID,
            (CardType)UnitType,
            new Unity.Mathematics.int2(PositionX, PositionY),
            SyncData,
            bUnitIsActivated
        );
    }
}

[Serializable]
public struct SerializablePlayerData
{
    public int PlayerID;
    public List<SerializablePlayerUnitData> PlayerUnits;
    public int Resources;
    public int PlayerReligion;
    public List<int> PlayerOwnedCells;

    public static SerializablePlayerData FromPlayerData(PlayerData data)
    {
        SerializablePlayerData serializableData = new SerializablePlayerData
        {
            PlayerID = data.PlayerID,
            Resources = data.Resources,
            PlayerReligion = (int)data.PlayerReligion,
            PlayerOwnedCells = new List<int>(data.PlayerOwnedCells),
            PlayerUnits = new List<SerializablePlayerUnitData>()
        };

        foreach (var unit in data.PlayerUnits)
        {
            serializableData.PlayerUnits.Add(
                SerializablePlayerUnitData.FromPlayerUnitData(unit)
            );
        }

        return serializableData;
    }

    public PlayerData ToPlayerData()
    {
        PlayerData playerData = new PlayerData(PlayerID);
        playerData.Resources = Resources;
        playerData.PlayerReligion = (Religion)PlayerReligion;
        playerData.PlayerOwnedCells = new List<int>(PlayerOwnedCells);
        playerData.PlayerUnits = new List<PlayerUnitData>();

        foreach (var unit in PlayerUnits)
        {
            playerData.PlayerUnits.Add(unit.ToPlayerUnitData());
        }

        return playerData;
    }
}

// *************************
//      主要网络系统
// *************************
public class NetGameSystem : MonoBehaviour
{
    // 单例
    public static NetGameSystem Instance { get; private set; }


    [Header("网络配置")]
    [SerializeField] private bool isServer = false;
    [SerializeField] private string serverIP = "127.0.0.1";
    [SerializeField] private int port = 8888;
    [SerializeField] private int maxPlayers = 2;
    [SerializeField] private string playerName = "Player";

    // 网络组件
    private UdpClient udpClient;
    private IPEndPoint serverEndPoint;
    private Dictionary<uint, IPEndPoint> clients; // 服务器: 客户端列表
    private Dictionary<uint, string> clientNames; // 服务器: 客户端名称
    private uint localClientId = 0;
    private uint nextClientId = 1;
    private bool isRunning = false;
    private Thread networkThread;

    // 房间相关
    // 玩家IP
    private string playerIP = "";
    // 游戏状态
    private bool isGameStarted = false;
    private List<uint> connectedPlayers = new List<uint>();

    // 客户端准备状态和IP
    private Dictionary<uint, bool> clientReadyStatus; // 服务器: 客户端准备状态
    private Dictionary<uint, string> clientIPs; // 服务器: 客户端IP地址


    // 本地准备状态
    private bool isLocalReady = false;

    // 所有玩家信息列表
    private List<PlayerInfo> roomPlayers = new List<PlayerInfo>();

    // 消息处理器
    private Dictionary<NetworkMessageType, Action<NetworkMessage>> messageHandlers;

    // 事件
    public event Action<NetworkMessage> OnMessageReceived;
    public event Action<uint> OnClientConnected;  // 服务器端
    //public event Action<uint> OnClientDisconnected;

    public event Action OnConnectedToServer;      // 客户端
    public event Action OnDisconnected;
    public event Action OnGameStarted;

    // 房间状态更新事件
    public event Action<List<PlayerInfo>> OnRoomStatusUpdated;
    public event Action<bool> OnAllPlayersReady; // 所有玩家准备完毕

    // 属性
    public bool bIsConnected => isRunning;
    public bool bIsServer => isServer;
    public uint bLocalClientId => localClientId;
    public List<uint> ConnectedPlayers => new List<uint>(connectedPlayers);
    public int PlayerCount => connectedPlayers.Count;


    // 获取房间玩家信息
    public List<PlayerInfo> RoomPlayers => new List<PlayerInfo>(roomPlayers);
    public bool IsLocalReady => isLocalReady;

    // 引用
    private GameManage gameManage;
    private PlayerDataManager playerDataManager;


    // 2025.11.17
    private bool hasStartedLoading = false;     // 是否开始Loading
    GameLoadProgressUI gameLoadProgressUI;      // 获得本地加载UI



    // *************************
    //      Unity生命周期
    // *************************

    private void Awake()
    {
        // 单例设置
        if (Instance == null)
        {
            Instance = this;
            // 2025.11.17 Guoning 避免跨场景存在
            //DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 确保MainThreadDispatcher存在
        if (FindObjectOfType<MainThreadDispatcher>() == null)
        {
            GameObject dispatcherObj = new GameObject("MainThreadDispatcher");
            dispatcherObj.AddComponent<MainThreadDispatcher>();
        }

        // 初始化消息处理器
        InitializeMessageHandlers();

        // 初始化房间相关字典 
        clientReadyStatus = new Dictionary<uint, bool>();
        clientIPs = new Dictionary<uint, string>();

    }

    private void Start()
    {
        // 从SceneStateManager
        if (SceneStateManager.Instance != null)
        {
            isServer = SceneStateManager.Instance.GetIsServer();
            playerName = SceneStateManager.Instance.PlayerName;
            playerIP = SceneStateManager.Instance.PlayerIP; // 获取本地IP

            if (SceneStateManager.Instance.bIsDirectConnect)
            {
                // 互联测试中，这里可以从PlayerPrefs获取默认服务器IP
                if (!isServer)
                {
                    //互联测试中，这里可以从PlayerPrefs获取默认服务器IP
                    serverIP = PlayerPrefs.GetString("ServerIP", "192.168.1.100");
                }

            }


            // 延迟启动网络,确保所有单例初始化完成
            StartCoroutine(DelayedNetworkStart());
        }

    }

    private IEnumerator DelayedNetworkStart()
    {
        // 等待一帧,确保所有 Awake 执行完成
        yield return 0.1f;

        // 获取 GameManage 引用
        GetGameManage();

        if (SceneStateManager.Instance.bIsSingle)
        {
            gameManage.StartGameFromRoomUI();
        }
        else
        {
            // 自动启动网络
            if (isServer)
            {
                StartServer();
            }
            else
            {
                ConnectToServer();
            }
        }
    }
    private void OnDestroy()
    {
        Shutdown();

        // 2025.11.17 清理
        if (Instance == this)
        {
            Instance = null;
            Debug.Log("NetGameManager已销毁");
        }
    }

    // *************************
    //         初始化
    // *************************
    public void GetGameManage()
    {

        // 多重尝试获取
        gameManage = GameManage.Instance;

        if (gameManage == null)
        {
            gameManage = GameObject.Find("GameManager").GetComponent<GameManage>();
        }

        //if (gameManage == null)
        //{
        //    Debug.LogError("无法找到 GameManage!");
        //}
        //else
        //{
        //    Debug.Log($"成功获取 GameManage");
        //}

        playerDataManager = PlayerDataManager.Instance;

        if (playerDataManager == null)
        {
            Debug.LogError("无法找到 PlayerDataManager!");
        }
    }


    private void InitializeMessageHandlers()
    {
        messageHandlers = new Dictionary<NetworkMessageType, Action<NetworkMessage>>
        {
                // 网络状态相关
                { NetworkMessageType.CONNECT, HandleConnect },
                { NetworkMessageType.CONNECTED, HandleConnected },
                { NetworkMessageType.PLAYER_JOINED, HandlePlayerJoined },
                
                // 房间状态相关
                { NetworkMessageType.PLAYER_LEFT, HandlePlayerLeft },
                { NetworkMessageType.PLAYER_READY, HandlePlayerReady },
                { NetworkMessageType.PLAYER_NOT_READY, HandlePlayerNotReady },
                { NetworkMessageType.ROOM_STATUS_UPDATE, HandleRoomStatusUpdate }, 
               
                // 游戏流程相关
                { NetworkMessageType.GAME_START, HandleGameStart },
                { NetworkMessageType.TURN_START, HandleTurnStart },
                { NetworkMessageType.TURN_END, HandleTurnEnd },
                 // 单位相关
                { NetworkMessageType.UNIT_MOVE, HandleUnitMove },
                { NetworkMessageType.UNIT_ADD, HandleUnitAdd },
                { NetworkMessageType.UNIT_REMOVE, HandleUnitRemove },
                { NetworkMessageType.UNIT_ATTACK, HandleUnitAttack },
                { NetworkMessageType.BUILDING_ATTACK, HandleBuildingAttack },
                { NetworkMessageType.UNIT_CHARM, HandleUnitCharm },
                { NetworkMessageType.CHARM_EXPIRE, HandleCharmExpire },

                { NetworkMessageType.PING, HandlePing },
                { NetworkMessageType.PONG, HandlePong }
        };

        //Debug.Log($"=== 消息处理器注册完成 ===");
        //Debug.Log($"共注册 {messageHandlers.Count} 个处理器");
    }

    // *************************
    //      服务器功能
    // *************************

    public void StartServer()
    {
        if (isRunning)
        {
            Debug.LogWarning("服务器已经在运行!");
            return;
        }

        try
        {
            clients = new Dictionary<uint, IPEndPoint>();
            clientNames = new Dictionary<uint, string>();

            clientReadyStatus = new Dictionary<uint, bool>();
            clientIPs = new Dictionary<uint, string>();

            //connectedPlayers.Clear();

            udpClient = new UdpClient(port);
            isRunning = true;

            // 服务器自己算作第一个玩家
            localClientId = 0;
            connectedPlayers.Add(0);

            //添加服务器自己到房间玩家列表
            roomPlayers.Add(new PlayerInfo
            {
                PlayerId = 0,
                PlayerName = playerName,
                PlayerIP = playerIP,
                IsReady = true
            });

            // 启动接收线程
            networkThread = new Thread(ServerLoop) { IsBackground = true };
            networkThread.Start();

            Debug.Log($"[服务器] 启动成功 - 端口: {port}");

            // 通知UI更新房间状态 
            MainThreadDispatcher.Enqueue(() =>
            {
                OnRoomStatusUpdated?.Invoke(roomPlayers);

            });

        }
        catch (Exception ex)
        {
            Debug.LogError($"[服务器] 启动失败: {ex.Message}");
            isRunning = false;
        }
    }

    // 服务器接收循环
    private void ServerLoop()
    {
        while (isRunning)
        {
            try
            {
                IPEndPoint clientEP = null;
                byte[] data = udpClient.Receive(ref clientEP);
                string json = Encoding.UTF8.GetString(data);

                if (json == "PingCheck")
                {
                    // 告知客户端“服务器在线”
                    byte[] response = Encoding.UTF8.GetBytes("ServerAlive");
                    udpClient.Send(response, response.Length, clientEP);
                    continue; // 跳过本次循环，不继续解析
                }

                NetworkMessage message = JsonConvert.DeserializeObject<NetworkMessage>(json);

                // 处理消息
                MainThreadDispatcher.Enqueue(() =>
                {
                    if (messageHandlers.ContainsKey(message.MessageType))
                    {
                        messageHandlers[message.MessageType](message);
                    }

                    // 如果是新连接请求，记录客户端
                    if (message.MessageType == NetworkMessageType.CONNECT)
                    {
                        ConnectMessage connectData = JsonConvert.DeserializeObject<ConnectMessage>(message.JsonData);
                        uint clientId = nextClientId++;
                        clients[clientId] = clientEP;
                        clientNames[clientId] = connectData.PlayerName;
                        clientReadyStatus[clientId] = false; // 初始化为未准备
                        clientIPs[clientId] = connectData.PlayerIP; // 保存IP
                        connectedPlayers.Add(clientId);

                        // 发送确认
                        ConnectedMessage connectedMsg = new ConnectedMessage
                        {
                            AssignedClientId = clientId,
                            ExistingPlayerIds = connectedPlayers
                        };

                        NetworkMessage response = new NetworkMessage
                        {
                            MessageType = NetworkMessageType.CONNECTED,
                            SenderId = 0,
                            JsonData = JsonConvert.SerializeObject(connectedMsg)
                        };

                        SendToClient(clientId, response);

                        // 通知其他客户端
                        PlayerJoinedMessage joinedMsg = new PlayerJoinedMessage
                        {
                            PlayerId = clientId,
                            PlayerName = connectData.PlayerName,
                            PlayerIP = connectData.PlayerIP
                        };

                        NetworkMessage joinedMessage = new NetworkMessage
                        {
                            MessageType = NetworkMessageType.PLAYER_JOINED,
                            SenderId = 0,
                            JsonData = JsonConvert.SerializeObject(joinedMsg)
                        };

                        BroadcastToClients(joinedMessage, clientId);

                        // 更新房间玩家列表并发送房间状态
                        UpdateRoomPlayersList();
                        SendRoomStatusToAll();

                        OnClientConnected?.Invoke(clientId);
                    }

                    // 转发其他消息
                    else if (message.MessageType != NetworkMessageType.PING &&
                             message.MessageType != NetworkMessageType.PONG &&
                             message.MessageType != NetworkMessageType.PLAYER_READY &&  // 不广播准备消息
                             message.MessageType != NetworkMessageType.PLAYER_NOT_READY)  // 不广播取消准备消息
                    {
                        BroadcastToClients(message, message.SenderId);
                    }
                });
            }
            catch (Exception e)
            {
                if (isRunning)
                    Debug.LogError($"服务器接收错误: {e.Message}");
            }
        }
    }

    // 更新房间玩家列表
    private void UpdateRoomPlayersList()
    {
        roomPlayers.Clear();

        // 添加服务器自己
        roomPlayers.Add(new PlayerInfo
        {
            PlayerId = 0,
            PlayerName = playerName,
            PlayerIP = playerIP,
            IsReady = true
        });

        // 添加所有客户端
        foreach (var clientId in connectedPlayers)
        {
            if (clientId != 0 && clientNames.ContainsKey(clientId))
            {
                bool clientReady = false;
                if (clientReadyStatus.ContainsKey(clientId))
                {
                    clientReady = clientReadyStatus[clientId];
                }

                roomPlayers.Add(new PlayerInfo
                {
                    PlayerId = clientId,
                    PlayerName = clientNames[clientId],
                    PlayerIP = clientIPs.ContainsKey(clientId) ? clientIPs[clientId] : "Unknown",
                    IsReady = clientReady
                });
                // 调试输出
                //Debug.Log($"[UpdateRoomPlayersList] 玩家 {clientId} - 准备状态: {clientReadyStatus[clientId]}");
            }
        }
    }



    // 发送房间状态给所有玩家
    private void SendRoomStatusToAll()
    {
        RoomStatusUpdateMessage statusMsg = new RoomStatusUpdateMessage
        {
            Players = roomPlayers
        };

        NetworkMessage message = new NetworkMessage
        {
            MessageType = NetworkMessageType.ROOM_STATUS_UPDATE,
            SenderId = 0,
            JsonData = JsonConvert.SerializeObject(statusMsg)
        };

        // 广播给所有客户端
        BroadcastToClients(message, uint.MaxValue);

        // 服务器自己也更新
        MainThreadDispatcher.Enqueue(() => {
            OnRoomStatusUpdated?.Invoke(roomPlayers);

            //Debug.Log("SendRoomStatusToAll");
            CheckAllPlayersReady();
        });
    }

    // 检查所有玩家是否准备完毕
    private void CheckAllPlayersReady()
    {
        //Debug.Log("CheckAllPlayersReady");
        if (roomPlayers.Count < 2) // 至少需要2个玩家
        {
            OnAllPlayersReady?.Invoke(false);
            return;
        }

        bool allReady = true;
        foreach (var player in roomPlayers)
        {
            //Debug.Log("Player "+player.PlayerId+" Ready? "+player.IsReady);
            if (!player.IsReady)
            {
                allReady = false;
                break;
            }
        }

        OnAllPlayersReady?.Invoke(allReady);
    }


    private void HandleNewClientConnection(IPEndPoint clientEndPoint, NetworkMessage message)
    {
        // 检查是否已满
        if (connectedPlayers.Count >= maxPlayers)
        {
            Debug.Log("[服务器] 服务器已满,拒绝连接");
            return;
        }

        uint newClientId = nextClientId++;
        clients[newClientId] = clientEndPoint;
        connectedPlayers.Add(newClientId);

        // 解析客户端名称
        ConnectMessage connectMsg = JsonConvert.DeserializeObject<ConnectMessage>(message.JsonData);
        string clientName = connectMsg?.PlayerName ?? $"Player{newClientId}";
        clientNames[newClientId] = clientName;

        // 发送连接确认
        ConnectedMessage connectedMsg = new ConnectedMessage
        {
            AssignedClientId = newClientId,
            ExistingPlayerIds = new List<uint>(connectedPlayers)
        };

        NetworkMessage response = new NetworkMessage
        {
            MessageType = NetworkMessageType.CONNECTED,
            SenderId = 0,
            JsonData = JsonConvert.SerializeObject(connectedMsg)
        };

        SendToClient(newClientId, response);

        // 通知所有其他客户端
        PlayerJoinedMessage joinedMsg = new PlayerJoinedMessage
        {
            PlayerId = newClientId,
            PlayerName = clientName
        };

        NetworkMessage joinedMessage = new NetworkMessage
        {
            MessageType = NetworkMessageType.PLAYER_JOINED,
            SenderId = 0,
            JsonData = JsonConvert.SerializeObject(joinedMsg)
        };

        BroadcastToClients(joinedMessage, newClientId);

        MainThreadDispatcher.Enqueue(() =>
        {
            OnClientConnected?.Invoke(newClientId);
            Debug.Log($"[服务器] 客户端 {newClientId} ({clientName}) 已连接 - 当前玩家数: {connectedPlayers.Count}/{maxPlayers}");

            // 如果人数够了,自动开始游戏
            if (connectedPlayers.Count >= maxPlayers && !isGameStarted)
            {
                Invoke(nameof(StartGame), 1f); // 延迟1秒开始
            }
        });
    }

    public void StartGame()
    {
        if (!isServer)
        {
            Debug.LogWarning("只有服务器可以启动游戏!");
            return;
        }

        if (isGameStarted)
        {
            Debug.LogWarning("游戏已经开始!");
            return;
        }

        if (connectedPlayers.Count < maxPlayers)
        {
            Debug.LogWarning("玩家不足,无法开始游戏!");
            return;
        }

        // 创建游戏开始数据

        int[] playerIds = new int[connectedPlayers.Count];
        for (int i = 0; i < connectedPlayers.Count; i++)
        {
            playerIds[i] = (int)connectedPlayers[i];
        }

        GameStartData gameData = new GameStartData
        {
            PlayerIds = playerIds,
            StartPositions = AssignStartPositions(),
            FirstTurnPlayerId = (int)connectedPlayers[0],
            PlayerReligion = SceneStateManager.Instance.PlayerReligion
        };

        NetworkMessage message = new NetworkMessage
        {
            MessageType = NetworkMessageType.GAME_START,
            SenderId = 0,
            JsonData = JsonConvert.SerializeObject(gameData)
        };

        // 广播给所有客户端
        BroadcastToClients(message, 0);

        // 服务器自己也处理
        MainThreadDispatcher.Enqueue(() =>
        {
            Debug.Log("[服务器] 游戏开始!");
            OnGameStarted?.Invoke();
            HandleGameStart(message);
        });

        // 2025.11.14 Guoning 开始播放音乐
        SoundManager.Instance.StopBGM();
        SoundManager.Instance.PlayBGM(SoundSystem.TYPE_BGM.REDMOON_THEME);
    }

    private int[] AssignStartPositions()
    {
        // 根据玩家数量分配起始位置，后续起始位置实装后不需要
        if (gameManage != null && gameManage.GetBoardCount() > 0)
        {
            int boardCount = gameManage.GetBoardCount();
            int[] positions = new int[connectedPlayers.Count];


            // 简单分配: 第一个玩家在0, 最后一个玩家在最后一个格子
            for (int i = 0; i < positions.Length; i++)
            {
                positions[i] = gameManage.GetStartPosForNetGame(i);
                // 更改为保存的起始位置
                //if (i == 0)
                //    positions[i] = 0;
                //else if (i == positions.Length - 1)
                //    positions[i] = boardCount - 1;
                //else
                //    positions[i] = (boardCount / positions.Length) * i;

                //if (i == 0)
                //    positions[i] = 0;
                //else if (i == positions.Length - 1)
                //    positions[i] = boardCount - 1;
                //else
                //    positions[i] = (boardCount / positions.Length) * i;
            }

            return positions;
        }

        // 默认位置
        return new int[] { 0, 99 };
    }

    // *************************
    //      客户端功能
    // *************************

    public void ConnectToServer()
    {
        // 添加服务器检测
        bool serverExists = false;
        using (UdpClient testClient = new UdpClient())
        {
            try
            {
                testClient.Connect(serverIP, port);
                //Debug.Log("Server IP is "+serverIP+" server port is "+port);
                // 发送一个测试Ping包
                byte[] testData = Encoding.UTF8.GetBytes("PingCheck");
                testClient.Send(testData, testData.Length);

                // 设置超时
                testClient.Client.ReceiveTimeout = 500;
                IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);

                // 等待服务器响应
                DateTime startTime = DateTime.Now;
                while ((DateTime.Now - startTime).TotalMilliseconds < 500)
                {
                    if (testClient.Available > 0)
                    {
                        byte[] recv = testClient.Receive(ref remote);
                        string reply = Encoding.UTF8.GetString(recv);
                        if (reply.Contains("ServerAlive"))
                        {
                            serverExists = true;
                            break;
                        }
                    }
                    Thread.Sleep(10); // 短暂等待避免占满CPU
                }
            }
            catch (SocketException)
            {
                serverExists = false;
            }
        }

        if (!serverExists)
        {
            Debug.LogWarning("[客户端] 未检测到服务器，连接失败。");
            SceneController.Instance?.SwitchScene("SelectScene", null);
            return;
        }


        try
        {
            serverEndPoint = new IPEndPoint(IPAddress.Parse(serverIP), port);
            udpClient = new UdpClient();
            isRunning = true;

            // 发送连接请求
            ConnectMessage connectMsg = new ConnectMessage
            {
                PlayerName = playerName,
                PlayerIP = playerIP
            };

            NetworkMessage message = new NetworkMessage
            {
                MessageType = NetworkMessageType.CONNECT,
                SenderId = 0,
                JsonData = JsonConvert.SerializeObject(connectMsg)
            };

            SendToServer(message);

            // 启动接收线程
            networkThread = new Thread(ClientLoop) { IsBackground = true };
            networkThread.Start();

            Debug.Log($"[客户端] 正在连接到 {serverIP}:{port}...");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[客户端] 连接失败: {ex.Message}");
            isRunning = false;
        }
    }

    private void ClientLoop()
    {
        while (isRunning)
        {
            try
            {
                IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = udpClient.Receive(ref remoteEndPoint);
                string jsonData = Encoding.UTF8.GetString(data);

                NetworkMessage message = JsonConvert.DeserializeObject<NetworkMessage>(jsonData);

                // 在主线程处理消息
                MainThreadDispatcher.Enqueue(() =>
                {
                    if (messageHandlers.ContainsKey(message.MessageType))
                    {
                        messageHandlers[message.MessageType](message);
                    }
                    OnMessageReceived?.Invoke(message);
                });
            }
            catch (SocketException)
            {
                if (isRunning)
                {
                    Thread.Sleep(10);
                }
            }
            catch (Exception ex)
            {
                if (isRunning)
                    Debug.LogError($"[客户端] 错误: {ex.Message}");
            }
        }
    }

    // *************************
    //      发送消息
    // *************************

    public void SendMessage(NetworkMessageType type, object data)
    {
        NetworkMessage message = new NetworkMessage
        {
            MessageType = type,
            SenderId = localClientId,
            JsonData = JsonConvert.SerializeObject(data)
        };

        if (isServer)
        {
            BroadcastToClients(message, localClientId);
        }
        else
        {
            SendToServer(message);
        }
    }

    /// <summary>
    /// 发送单位添加消息
    /// </summary>
    public void SendUnitAddMessage(int playerId, CardType unitType, int2 pos,
        syncPieceData newUnitData, bool isUsed = false,
        syncBuildingData? buildingData = null)
    {
        UnitAddMessage addData = new UnitAddMessage
        {
            PlayerId = playerId,
            UnitType = (int)unitType,
            PosX = pos.x,
            PosY = pos.y,
            NewUnitSyncData = newUnitData,
            BuildingData = buildingData  // 添加建筑数据
        };

        NetworkMessage msg = new NetworkMessage
        {
            MessageType = NetworkMessageType.UNIT_ADD,
            SenderId = localClientId,
            JsonData = JsonConvert.SerializeObject(addData)
        };

        if (isServer)
        {
            BroadcastToClients(msg, localClientId);
            Debug.Log($"[网络-服务器] 广播 UNIT_ADD 消息给所有客户端");
        }
        else
        {
            SendToServer(msg);
            Debug.Log($"[网络-客户端] 发送 UNIT_ADD 消息到服务器");
        }
    }

    /// <summary>
    /// 发送单位移动消息
    /// </summary>
    public void SendUnitMoveMessage(int playerId, int2 fromPos, int2 toPos, syncPieceData movedUnitData)
    {
        UnitMoveMessage moveData = new UnitMoveMessage
        {
            PlayerId = playerId,
            FromX = fromPos.x,
            FromY = fromPos.y,
            ToX = toPos.x,
            ToY = toPos.y,
            MovedUnitSyncData = movedUnitData
        };

        NetworkMessage msg = new NetworkMessage
        {
            MessageType = NetworkMessageType.UNIT_MOVE,
            SenderId = localClientId,
            JsonData = JsonConvert.SerializeObject(moveData)
        };

        if (isServer)
        {
            // 服务器广播给所有客户端（排除自己）
            BroadcastToClients(msg, localClientId);
            Debug.Log($"[网络-服务器] 广播 UNIT_MOVE 消息给所有客户端");
        }
        else
        {
            // 客户端发送给服务器
            SendToServer(msg);
            Debug.Log($"[网络-客户端] 发送 UNIT_MOVE 消息到服务器");
        }
    }

    /// <summary>
    /// 发送单位移除消息
    /// </summary>
    public void SendUnitRemoveMessage(int playerId, int2 pos, int unitId)
    {
        UnitRemoveMessage removeData = new UnitRemoveMessage
        {
            PlayerId = playerId,
            PosX = pos.x,
            PosY = pos.y,
            UnitID = unitId
        };

        NetworkMessage msg = new NetworkMessage
        {
            MessageType = NetworkMessageType.UNIT_REMOVE,
            SenderId = localClientId,
            JsonData = JsonConvert.SerializeObject(removeData)
        };

        if (isServer)
        {
            BroadcastToClients(msg, localClientId);
            Debug.Log($"[网络-服务器] 广播 UNIT_REMOVE 消息给所有客户端");
        }
        else
        {
            SendToServer(msg);
            Debug.Log($"[网络-客户端] 发送 UNIT_REMOVE 消息到服务器");
        }
    }

    /// <summary>
    /// 发送单位攻击消息
    /// </summary>
    public void SendUnitAttackMessage(
        int attackerPlayerId,
        int2 attackerPos,
        int targetPlayerId,
        int2 targetPos,
        syncPieceData attackerData,
        syncPieceData? targetData,
        bool targetDestroyed,
        int2? attackerOriginalPos = null,    // 【新增】原始位置（可选）
        bool hasMoved = false)               // 【新增】是否移动标记
    {
        // 如果没有提供原始位置，使用当前位置作为原始位置
        int2 originalPos = attackerOriginalPos ?? attackerPos;

        UnitAttackMessage attackData = new UnitAttackMessage
        {
            AttackerPlayerId = attackerPlayerId,
            AttackerPosX = attackerPos.x,
            AttackerPosY = attackerPos.y,

            // 【新增】设置原始位置
            AttackerOriginalPosX = originalPos.x,
            AttackerOriginalPosY = originalPos.y,
            HasMoved = hasMoved,

            TargetPlayerId = targetPlayerId,
            TargetPosX = targetPos.x,
            TargetPosY = targetPos.y,
            AttackerSyncData = attackerData,
            TargetSyncData = targetData,
            TargetDestroyed = targetDestroyed
        };

        NetworkMessage msg = new NetworkMessage
        {
            MessageType = NetworkMessageType.UNIT_ATTACK,
            SenderId = localClientId,
            JsonData = JsonConvert.SerializeObject(attackData)
        };

        if (isServer)
        {
            BroadcastToClients(msg, localClientId);
            Debug.Log($"[网络-服务器] 广播 UNIT_ATTACK 消息给所有客户端");
        }
        else
        {
            SendToServer(msg);
            Debug.Log($"[网络-客户端] 发送 UNIT_ATTACK 消息到服务器");
        }
    }

    /// <summary>
    /// 发送建筑攻击消息
    /// </summary>
    public void SendBuildingAttackMessage(
        int attackerPlayerId,
        int2 attackerPos,
        int buildingOwnerId,
        int2 buildingPos,
        int buildingID,
        syncPieceData attackerData,
        int buildingRemainingHP,
        bool buildingDestroyed)
    {
        BuildingAttackMessage attackData = new BuildingAttackMessage
        {
            AttackerPlayerId = attackerPlayerId,
            AttackerPosX = attackerPos.x,
            AttackerPosY = attackerPos.y,

            BuildingOwnerId = buildingOwnerId,
            BuildingPosX = buildingPos.x,
            BuildingPosY = buildingPos.y,
            BuildingID = buildingID,

            AttackerSyncData = attackerData,
            BuildingRemainingHP = buildingRemainingHP,
            BuildingDestroyed = buildingDestroyed
        };

        NetworkMessage msg = new NetworkMessage
        {
            MessageType = NetworkMessageType.BUILDING_ATTACK,
            SenderId = localClientId,
            JsonData = JsonConvert.SerializeObject(attackData)
        };

        if (isServer)
        {
            BroadcastToClients(msg, localClientId);
            Debug.Log($"[网络-服务器] 广播 BUILDING_ATTACK 消息给所有客户端");
        }
        else
        {
            SendToServer(msg);
            Debug.Log($"[网络-客户端] 发送 BUILDING_ATTACK 消息到服务器");
        }
    }



    // 发送单位魅惑消息
    public void SendUnitCharmMessage(
        int missionaryPlayerId,
        int missionaryID,
        int2 missionaryPos,
        int targetPlayerId,
        int targetID,
        int2 targetPos,
        syncPieceData newUnitSyncData,
        int charmedTurns = 3)
    {
        UnitCharmMessage charmData = new UnitCharmMessage
        {
            MissionaryPlayerId = missionaryPlayerId,
            MissionaryID = missionaryID,
            MissionaryPosX = missionaryPos.x,
            MissionaryPosY = missionaryPos.y,

            TargetPlayerId = targetPlayerId,
            TargetID = targetID,
            TargetPosX = targetPos.x,
            TargetPosY = targetPos.y,

            NewUnitSyncData = newUnitSyncData,
            CharmedTurns = charmedTurns
        };

        NetworkMessage msg = new NetworkMessage
        {
            MessageType = NetworkMessageType.UNIT_CHARM,
            SenderId = localClientId,
            JsonData = JsonConvert.SerializeObject(charmData)
        };

        if (isServer)
        {
            BroadcastToClients(msg, localClientId);
            Debug.Log($"[网络-服务器] 广播 UNIT_CHARM 消息给所有客户端");
        }
        else
        {
            SendToServer(msg);
            Debug.Log($"[网络-客户端] 发送 UNIT_CHARM 消息到服务器");
        }
    }

    // 发送魅惑过期消息（归还控制权）
    public void SendCharmExpireMessage(
        int currentOwnerId,
        int originalOwnerId,
        int unitID,
        int2 pos,
        syncPieceData unitSyncData)
    {
        CharmExpireMessage expireData = new CharmExpireMessage
        {
            CurrentOwnerId = currentOwnerId,
            OriginalOwnerId = originalOwnerId,
            UnitID = unitID,
            PosX = pos.x,
            PosY = pos.y,
            UnitSyncData = unitSyncData
        };

        NetworkMessage msg = new NetworkMessage
        {
            MessageType = NetworkMessageType.CHARM_EXPIRE,
            SenderId = localClientId,
            JsonData = JsonConvert.SerializeObject(expireData)
        };

        if (isServer)
        {
            BroadcastToClients(msg, localClientId);
            Debug.Log($"[网络-服务器] 广播 CHARM_EXPIRE 消息给所有客户端");
        }
        else
        {
            SendToServer(msg);
            Debug.Log($"[网络-客户端] 发送 CHARM_EXPIRE 消息到服务器");
        }
    }


    // 发送消息到服务器
    private void SendToServer(NetworkMessage message)
    {
        if (!isRunning || isServer) return;

        message.SenderId = localClientId;
        byte[] data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));
        udpClient.Send(data, data.Length, serverEndPoint);
    }

    private void SendToClient(uint clientId, NetworkMessage message)
    {
        if (!clients.ContainsKey(clientId))
        {
            Debug.LogError($"clients 字典中不存在 clientId: {clientId}");
            return;
        }

        try
        {
            byte[] data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));
            udpClient.Send(data, data.Length, clients[clientId]);
        }
        catch (Exception ex)
        {
            Debug.LogError($"发送到客户端 {clientId} 失败: {ex.Message}");
        }
    }

    // 设置准备状态
    public void SetReadyStatus(bool ready)
    {
        Debug.Log("Ready? = " + ready);
        isLocalReady = ready;

        if (isServer)
        {
            // 服务器更新自己的准备状态
            UpdateRoomPlayersList();
            SendRoomStatusToAll();
        }
        else
        {
            // 客户端发送准备状态给服务器
            PlayerReadyMessage readyMsg = new PlayerReadyMessage
            {
                PlayerId = localClientId,
                IsReady = ready
            };

            NetworkMessage message = new NetworkMessage
            {
                MessageType = ready ? NetworkMessageType.PLAYER_READY : NetworkMessageType.PLAYER_NOT_READY,
                SenderId = localClientId,
                JsonData = JsonConvert.SerializeObject(readyMsg)
            };

            SendToServer(message);
        }
    }


    private void BroadcastToClients(NetworkMessage message, uint excludeClientId)
    {
        //Debug.Log($"=== BroadcastToClients 开始 ===");
        //Debug.Log($"消息类型: {message.MessageType}");
        //Debug.Log($"排除ID: {excludeClientId}");
        //Debug.Log($"当前是服务器: {isServer}");
        //Debug.Log($"clients 状态: {(clients == null ? "null" : $"Count={clients.Count}")}");


        if (!isServer)
        {
            Debug.LogWarning("不是服务器，无法广播");
            return;
        }

        if (clients == null)
        {
            Debug.LogError("clients 字典为 null!");
            return;
        }

        if (clients.Count == 0)
        {
            Debug.LogError("clients 字典为空，没有客户端!");
            return;
        }

        byte[] data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));

        foreach (var client in clients)
        {
            if (client.Key != excludeClientId)
            {
                try
                {
                    udpClient.Send(data, data.Length, client.Value);
                }
                catch (Exception e)
                {
                    Debug.LogError($"发送到客户端 {client.Key} 失败: {e.Message}");
                }
            }
        }

        //Debug.Log($"=== BroadcastToClients 完成，共发送 {broadcastCount} 条 ===");
    }


    // *************************
    //      消息处理
    // *************************

    private void ProcessMessage(NetworkMessage message)
    {
        Debug.Log($"=== ProcessMessage: 开始处理消息类型 {message.MessageType} ===");

        // 触发事件
        OnMessageReceived?.Invoke(message);
        //Debug.Log($"已触发 OnMessageReceived 事件");

        bool handled = false;

        // 主要处理器
        if (messageHandlers.ContainsKey(message.MessageType))
        {
            messageHandlers[message.MessageType]?.Invoke(message);
            handled = true;
        }
        else
        {
            Debug.LogWarning($"未找到消息类型 {message.MessageType} 的主要处理器");
        }

        // 如果没有被处理，使用备用处理器
        if (!handled)
        {
            Debug.Log($"消息 {message.MessageType} 未被主要处理器处理，使用备用处理器");

        }

        Debug.Log($"消息处理完成: {message.MessageType}");
    }

    // 连接消息
    private void HandleConnect(NetworkMessage message)
    {
        //Debug.LogWarning("[客户端] 收到CONNECT消息,这不应该发生");
    }

    private void HandleConnected(NetworkMessage message)
    {
        // 客户端收到连接确认
        ConnectedMessage data = JsonConvert.DeserializeObject<ConnectedMessage>(message.JsonData);
        localClientId = data.AssignedClientId;
        connectedPlayers = data.ExistingPlayerIds;

        Debug.Log($"成功连接到服务器，分配ID: {localClientId}");
        OnConnectedToServer?.Invoke();
    }
    private void HandlePlayerJoined(NetworkMessage message)
    {
        PlayerJoinedMessage data = JsonConvert.DeserializeObject<PlayerJoinedMessage>(message.JsonData);

        if (!connectedPlayers.Contains(data.PlayerId))
        {
            connectedPlayers.Add(data.PlayerId);
        }

        Debug.Log($"玩家 {data.PlayerName} (ID: {data.PlayerId}, IP: {data.PlayerIP}) 加入游戏");
    }

    // 处理玩家离开
    private void HandlePlayerLeft(NetworkMessage message)
    {
        // TODO: 玩家离开逻辑
    }

    // 处理玩家准备
    private void HandlePlayerReady(NetworkMessage message)
    {
        if (isServer)
        {
            PlayerReadyMessage data = JsonConvert.DeserializeObject<PlayerReadyMessage>(message.JsonData);

            Debug.Log($"[服务器] 收到玩家 {data.PlayerId} 的准备请求");
            Debug.Log($"[服务器] clientReadyStatus包含该ID? {clientReadyStatus.ContainsKey(data.PlayerId)}");
            Debug.Log($"[服务器] 当前房间人数: {roomPlayers.Count}");

            if (clientReadyStatus.ContainsKey(data.PlayerId) && roomPlayers.Count >= 2)
            {
                clientReadyStatus[data.PlayerId] = true;
                UpdateRoomPlayersList();
                SendRoomStatusToAll();

                Debug.Log($"玩家 {data.PlayerId} 准备完毕");
            }
            else
            {
                Debug.LogError($"[服务器] 找不到玩家 {data.PlayerId} 的准备状态记录");
            }
        }
    }

    // 处理玩家取消准备
    private void HandlePlayerNotReady(NetworkMessage message)
    {
        if (isServer)
        {
            PlayerReadyMessage data = JsonConvert.DeserializeObject<PlayerReadyMessage>(message.JsonData);
            if (clientReadyStatus.ContainsKey(data.PlayerId))
            {
                clientReadyStatus[data.PlayerId] = false;
                UpdateRoomPlayersList();
                SendRoomStatusToAll();
                Debug.Log($"玩家 {data.PlayerId} 取消准备");
            }
        }
    }

    // 处理房间状态更新
    private void HandleRoomStatusUpdate(NetworkMessage message)
    {
        RoomStatusUpdateMessage data = JsonConvert.DeserializeObject<RoomStatusUpdateMessage>(message.JsonData);
        roomPlayers = data.Players;

        foreach (var player in roomPlayers)
        {
            Debug.Log($"  - 玩家 {player.PlayerId}: {player.PlayerName}, 准备: {player.IsReady}");
        }
        OnRoomStatusUpdated?.Invoke(roomPlayers);
        CheckAllPlayersReady();
    }



    // 添加重试协程
    private IEnumerator RetryHandleTurnStart(NetworkMessage message, float delay)
    {
        yield return new WaitForSeconds(delay);

        Debug.Log("=== 重试 HandleTurnStart ===");
        HandleTurnStart(message);
    }

    //// 连接确认
    //private void HandleConnected(NetworkMessage message)
    //{
    //    ConnectedMessage data = JsonConvert.DeserializeObject<ConnectedMessage>(message.JsonData);
    //    localClientId = data.AssignedClientId;
    //    connectedPlayers = data.ExistingPlayerIds;

    //    Debug.Log($"[客户端] 已连接到服务器! 分配ID: {localClientId}");
    //    OnConnectedToServer?.Invoke();
    //}

    //// 玩家加入
    //private void HandlePlayerJoined(NetworkMessage message)
    //{
    //    PlayerJoinedMessage data = JsonConvert.DeserializeObject<PlayerJoinedMessage>(message.JsonData);

    //    if (!connectedPlayers.Contains(data.PlayerId))
    //    {
    //        connectedPlayers.Add(data.PlayerId);
    //        Debug.Log($"玩家 {data.PlayerId} ({data.PlayerName}) 加入游戏 - 当前玩家数: {connectedPlayers.Count}");
    //    }
    //}

    // 游戏开始
    private void HandleGameStart(NetworkMessage message)
    {
        //isGameStarted = true;
        //Debug.Log("游戏开始!");
        //OnGameStarted?.Invoke();
        GameStartData data = JsonConvert.DeserializeObject<GameStartData>(message.JsonData);

        Debug.Log($"游戏开始! 玩家数: {data.PlayerIds.Length}");

        isGameStarted = true;
        OnGameStarted?.Invoke();

        // 通知GameManage初始化游戏
        if (gameManage != null)
        {
            gameManage.InitGameWithNetworkData(data);
        }
        else
        {
            gameManage = GameManage.Instance;
            if (gameManage != null)
            {
                gameManage.InitGameWithNetworkData(data);
            }
            else
            {
                Debug.LogError("无法找到 GameManage 来初始化游戏!");
            }
        }
    }
    // 添加回合开始消息
    private void HandleTurnStart(NetworkMessage message)
    {
        try
        {
            //Debug.Log($"当前是服务器: {isServer}");
            //Debug.Log($"消息发送者ID: {message.SenderId}");

            var data = JsonConvert.DeserializeObject<TurnStartMessage>(message.JsonData);
            Debug.Log($"目标玩家: {data.PlayerId}");

            //// 多重查找 GameManage
            //if (gameManage == null)
            //{
            //    Debug.Log("gameManage 为 null，尝试查找...");
            //    gameManage = GameManage.Instance;
            //}

            //if (gameManage == null)
            //{
            //    Debug.Log("尝试通过 GameObject.Find 查找...");
            //    GameObject gmObj = GameObject.Find("GameManager");
            //    if (gmObj != null)
            //    {
            //        gameManage = gmObj.GetComponent<GameManage>();
            //        Debug.Log($" 通过 GameObject.Find 找到 GameManage");
            //    }
            //    else
            //    {
            //        Debug.LogError("GameObject.Find 未找到 'GameManager' 对象");
            //    }
            //}

            if (gameManage != null)
            {
                Debug.Log($" 调用 StartTurn({data.PlayerId})");
                gameManage.StartTurn(data.PlayerId);
                //Debug.Log($" StartTurn 调用完成");
            }
            else
            {
                Debug.LogError(" 无法找到 GameManage，延迟重试");

                //// 列出场景中所有对象（调试用）
                //GameObject[] allObjects = FindObjectsOfType<GameObject>();
                //Debug.Log($"场景中共有 {allObjects.Length} 个 GameObject");

                //bool foundGameManage = false;
                //foreach (var obj in allObjects)
                //{
                //    if (obj.name.Contains("GameManage") || obj.name.Contains("GameManager"))
                //    {
                //        Debug.Log($"找到可能的对象: {obj.name}, 激活: {obj.activeInHierarchy}");
                //        var gm = obj.GetComponent<GameManage>();
                //        if (gm != null)
                //        {
                //            Debug.Log($" 这个对象有 GameManage 组件!");
                //            gameManage = gm;
                //            foundGameManage = true;
                //            break;
                //        }
                //    }
                //}

                //if (!foundGameManage)
                //{
                //    Debug.LogError("场景中完全找不到 GameManage 组件!");
                //    StartCoroutine(RetryHandleTurnStart(message, 0.5f));
                //}
                //else
                //{
                //    Debug.Log($" 通过遍历找到 GameManage，调用 StartTurn({data.PlayerId})");
                //    gameManage.StartTurn(data.PlayerId);
                //}
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($" 处理回合开始消息出错: {ex.Message}\n{ex.StackTrace}");
        }
    }

    // 回合结束
    private void HandleTurnEnd(NetworkMessage message)
    {
        Debug.Log($"=== HandleTurnEnd 被调用 ===");
        //Debug.Log($"当前是服务器: {isServer}");
        //Debug.Log($"gameManage 是否为空: {gameManage == null}");


        if (gameManage == null)
        {
            gameManage = GameManage.Instance;
        }
        if (playerDataManager == null)
        {
            playerDataManager = PlayerDataManager.Instance;
        }

        TurnEndMessage data = JsonConvert.DeserializeObject<TurnEndMessage>(message.JsonData);

        Debug.Log($"收到玩家 {data.PlayerId} 的回合结束消息");

        //  转换为 PlayerData
        PlayerData playerData = data.PlayerDataJson.ToPlayerData();

        Debug.Log($"解析玩家数据成功，单位数: {playerData.GetUnitCount()}");

        // 重新加载所有单位的 PieceDataSO
        //for (int i = 0; i < playerData.PlayerUnits.Count; i++)
        //{
        //    PlayerUnitData unit = playerData.PlayerUnits[i];

        //    if (unit.PlayerUnitDataSO.pieceID!=-1)
        //    {
        //        //PieceDataSO pieceData = LoadPieceDataSO(unit.UnitType);

        //        //if (pieceData != null)
        //        //{
        //        //    unit.PlayerUnitDataSO = pieceData;
        //        //    playerData.PlayerUnits[i] = unit;
        //        //    Debug.Log($" 重新加载 {unit.UnitType} 的 PieceDataSO: {pieceData.name}");
        //        //}
        //        //else
        //        //{
        //        //    Debug.LogError($"❌ 无法加载 {unit.UnitType} 的 PieceDataSO");
        //        //}
        //    }
        //}
        // 更新数据
        if (playerDataManager != null)
        {
            playerDataManager.UpdatePlayerData(data.PlayerId, playerData);
        }


        // 通知 GameManage 更新显示
        if (gameManage != null)
        {
            // 调用 PlayerOperationManager 更新其他玩家显示
            gameManage.UpdateOtherPlayerShow(data.PlayerId, playerData);
            Debug.Log($"已通知更新玩家 {data.PlayerId} 的显示");
        }



        // 如果是服务器,切换到下一个回合
        if (isServer)
        {
            Debug.Log("[服务器] 处理回合切换...");

            if (gameManage == null)
            {
                Debug.LogError("[服务器] gameManage 为 null!");
                gameManage = GameManage.Instance;
                if (gameManage == null)
                {
                    Debug.LogError("[服务器] 无法获取 GameManage.Instance!");
                    return;
                }
            }

            // 找到下一个玩家 - 正确的类型转换
            int currentPlayerId = data.PlayerId;
            Debug.Log($"[服务器] 当前结束回合的玩家: {currentPlayerId}");


            // 在 connectedPlayers 中找到当前玩家的索引
            int currentIndex = -1;
            for (int i = 0; i < connectedPlayers.Count; i++)
            {
                Debug.Log($"[服务器] 检查 connectedPlayers[{i}] = {connectedPlayers[i]}");
                if ((int)connectedPlayers[i] == currentPlayerId)
                {
                    currentIndex = i;
                    Debug.Log($"[服务器] 找到当前玩家索引: {currentIndex}");
                    break;
                }
            }

            if (currentIndex == -1)
            {
                Debug.LogError($"找不到玩家 {currentPlayerId} 在connectedPlayers中");
                Debug.LogError($"connectedPlayers: {string.Join(", ", connectedPlayers)}");
                return;
            }

            int nextIndex = (currentIndex + 1) % connectedPlayers.Count;
            int nextPlayerId = (int)connectedPlayers[nextIndex];

            Debug.Log($"[服务器] 下一个玩家索引: {nextIndex}");
            Debug.Log($"[服务器] 下一个玩家ID: {nextPlayerId}");
            Debug.Log($"[服务器] 切换回合: 玩家 {currentPlayerId} -> 玩家 {nextPlayerId}");

            // 创建回合开始消息
            TurnStartMessage turnStartData = new TurnStartMessage
            {
                PlayerId = nextPlayerId
            };

            NetworkMessage turnStartMsg = new NetworkMessage
            {
                MessageType = NetworkMessageType.TURN_START,
                SenderId = 0,
                JsonData = JsonConvert.SerializeObject(turnStartData)
            };

            Debug.Log($"[服务器] 已创建 TURN_START 消息");
            Debug.Log($"[服务器] 消息内容: {turnStartMsg.JsonData}");
            Debug.Log($"[服务器] clients 字典状态: {(clients == null ? "null" : $"Count={clients.Count}")}");

            // 广播给所有客户端
            if (clients != null && clients.Count > 0)
            {
                Debug.Log($"[服务器] 准备广播消息给 {clients.Count} 个客户端");

                // 列出所有客户端
                foreach (var client in clients)
                {
                    Debug.Log($"[服务器] 客户端 {client.Key}: {client.Value}");
                }

                // 广播（不排除任何客户端）
                BroadcastToClients(turnStartMsg, uint.MaxValue);

                Debug.Log($"[服务器]  BroadcastToClients 调用完成");
            }
            else
            {
                Debug.LogError($"[服务器]  clients 为空或没有客户端! clients={(clients == null ? "null" : $"Count={clients.Count}")}");
            }

            // 服务器自己也开始回合
            Debug.Log($"[服务器] 服务器自己开始回合: {nextPlayerId}");
            gameManage.StartTurn(nextPlayerId);

            Debug.Log($"[服务器]  回合切换完成");
        }
        else
        {
            Debug.Log("[客户端] 收到 TURN_END 消息，等待服务器发送 TURN_START");
        }
    }

    /// <summary>
    /// 统一的 PieceDataSO 加载方法
    /// </summary>
    //private PieceDataSO LoadPieceDataSO(CardType cardType)
    //{
    //    PieceDataSO pieceData = null;

    //    // 方法1: 从 UnitListTable 加载
    //    if (UnitListTable.Instance != null)
    //    {
    //        pieceData = UnitListTable.Instance.GetPieceDataByCardType(cardType);
    //        if (pieceData != null)
    //        {
    //            return pieceData;
    //        }
    //    }

    //    // 方法2: 从 Resources 加载
    //    string resourcePath = GetResourcePathForCardType(cardType);
    //    if (!string.IsNullOrEmpty(resourcePath))
    //    {
    //        pieceData = Resources.Load<PieceDataSO>(resourcePath);
    //        if (pieceData != null)
    //        {
    //            return pieceData;
    //        }
    //    }

    //    Debug.LogError($"无法加载 PieceDataSO for CardType: {cardType}");
    //    return null;
    //}

    //private string GetResourcePathForCardType(CardType cardType)
    //{
    //    switch (cardType)
    //    {
    //        case CardType.Farmer: return "Cyou/Prefab/farmer";
    //        case CardType.Solider: return "Cyou/Prefab/military";
    //        case CardType.Missionary: return "Cyou/Prefab/Missionary";
    //        case CardType.Pope: return "Cyou/Prefab/pope";
    //        default: return null;
    //    }
    //}



    // 单位移动
    private void HandleUnitMove(NetworkMessage message)
    {
        UnitMoveMessage data = JsonConvert.DeserializeObject<UnitMoveMessage>(message.JsonData);

        int2 fromPos = new int2(data.FromX, data.FromY);
        int2 toPos = new int2(data.ToX, data.ToY);

        Debug.Log($"[网络] 玩家 {data.PlayerId} 移动单位: ({fromPos.x},{fromPos.y}) -> ({toPos.x},{toPos.y})");

        // 确保管理器存在
        if (gameManage == null)
        {
            gameManage = GameManage.Instance;
        }

        if (playerDataManager == null)
        {
            playerDataManager = PlayerDataManager.Instance;
        }

        // ===== 修复：不再使用MoveUnit，直接更新数据 =====
        if (playerDataManager != null)
        {
            // 方法1：尝试使用MoveUnit
            bool moveSuccess = playerDataManager.MoveUnit(data.PlayerId, fromPos, toPos);

            if (moveSuccess)
            {
                // 移动成功，更新同步数据
                playerDataManager.UpdateUnitSyncDataByPos(data.PlayerId, toPos, data.MovedUnitSyncData);
                Debug.Log($"[网络] PlayerDataManager 已更新单位移动数据");
            }
            else
            {
                // ===== 关键修复：MoveUnit失败时，直接修改单位数据 =====
                Debug.LogWarning($"[网络] MoveUnit失败，尝试直接更新数据（可能是交换操作）");

                // 获取PlayerData
                PlayerData playerData = playerDataManager.GetPlayerData(data.PlayerId);

                // 查找目标位置是否已经有单位（从消息中的syncData判断）
                bool found = false;

                for (int i = 0; i < playerData.PlayerUnits.Count; i++)
                {
                    PlayerUnitData unit = playerData.PlayerUnits[i];

                    // 通过UnitID匹配（syncData中的pieceID）
                    if (unit.PlayerUnitDataSO.pieceID == data.MovedUnitSyncData.pieceID)
                    {
                        Debug.Log($"[网络] 通过UnitID找到单位: {unit.PlayerUnitDataSO.pieceID}，当前位置({unit.Position.x},{unit.Position.y})");

                        // 创建更新后的单位数据
                        PlayerUnitData updatedUnit = new PlayerUnitData(
                            unit.UnitID,
                            unit.UnitType,
                            toPos,  // 新位置
                            data.MovedUnitSyncData,  // 新的同步数据
                            unit.bUnitIsActivated,
                            unit.bCanDoAction,
                            unit.bIsCharmed,
                            unit.charmedRemainingTurns,
                            unit.originalOwnerID,
                            unit.BuildingData
                        );

                        // 直接更新
                        playerData.PlayerUnits[i] = updatedUnit;
                        found = true;

                        Debug.Log($"[网络] 成功通过直接更新方式移动单位到({toPos.x},{toPos.y}) {updatedUnit.UnitID}");
                        break;
                    }
                }

                if (!found)
                {
                    Debug.LogError($"[网络] 无法找到要移动的单位: pieceID={data.MovedUnitSyncData.pieceID}");
                }
            }
        }

        // ===== 关键！！！必须调用这个方法来更新视觉效果 =====
        // 通知 PlayerOperationManager 处理视觉效果
        if (gameManage != null && gameManage._PlayerOperation != null)
        {
            gameManage._PlayerOperation.HandleNetworkMove(data);
        }
    }



    // 单位添加
    private void HandleUnitAdd(NetworkMessage message)
    {
        UnitAddMessage data = JsonConvert.DeserializeObject<UnitAddMessage>(message.JsonData);

        int2 pos = new int2(data.PosX, data.PosY);
        CardType unitType = (CardType)data.UnitType;

        Debug.Log($"[网络] 玩家 {data.PlayerId} 添加单位: {unitType} at ({pos.x},{pos.y})");

        if (gameManage == null)
        {
            gameManage = GameManage.Instance;
        }

        // 添加到 PlayerDataManager
        if (playerDataManager != null)
        {
            // 使用 syncPieceData 添加单位
            playerDataManager.AddUnit(data.PlayerId, unitType, pos, data.NewUnitSyncData, null);

            Debug.Log($"[网络] PlayerDataManager 已添加单位数据");
        }

        // 通知 PlayerOperationManager 创建视觉对象
        if (gameManage != null && gameManage._PlayerOperation != null)
        {
            gameManage._PlayerOperation.HandleNetworkAddUnit(data);
        }


    }

    // 单位移除
    private void HandleUnitRemove(NetworkMessage message)
    {
        UnitRemoveMessage data = JsonConvert.DeserializeObject<UnitRemoveMessage>(message.JsonData);

        int2 pos = new int2(data.PosX, data.PosY);

        Debug.Log($"玩家 {data.PlayerId} 移除单位 at ({pos.x},{pos.y})");

        if (playerDataManager != null)
        {
            playerDataManager.RemoveUnit(data.PlayerId, pos);
        }

        // 先通知 PlayerOperationManager 处理 GameObject
        if (gameManage != null && gameManage._PlayerOperation != null)
        {
            gameManage._PlayerOperation.HandleNetworkRemove(data);
        }

        // 从 PlayerDataManager 移除
        if (playerDataManager != null)
        {
            bool removeSuccess = playerDataManager.RemoveUnit(data.PlayerId, pos);

            if (removeSuccess)
            {
                Debug.Log($"[网络] PlayerDataManager 已移除单位数据");
            }
        }
    }

    // 单位攻击
    private void HandleUnitAttack(NetworkMessage message)
    {
        UnitAttackMessage data = JsonConvert.DeserializeObject<UnitAttackMessage>(message.JsonData);

        int2 attackerPos = new int2(data.AttackerPosX, data.AttackerPosY);
        int2 targetPos = new int2(data.TargetPosX, data.TargetPosY);

        Debug.Log($"[网络] 玩家 {data.AttackerPlayerId} 攻击 ({targetPos.x},{targetPos.y}) ");

        // 确保管理器存在
        if (gameManage == null)
        {
            gameManage = GameManage.Instance;
        }

        // 更新 PlayerDataManager 中的数据
        if (playerDataManager != null)
        {
            // 更新攻击者的同步数据
            bool attackerUpdated = playerDataManager.UpdateUnitSyncDataByPos(
                data.AttackerPlayerId, attackerPos, data.AttackerSyncData);

            if (attackerUpdated)
            {
                Debug.Log($"[网络] 攻击者数据已更新");
            }

            // 先通知 PlayerOperationManager 处理
            // 这样 HandleNetworkAttack 可以找到 UnitID 并从 PieceManager 移除
            if (gameManage != null && gameManage._PlayerOperation != null)
            {
                gameManage._PlayerOperation.HandleNetworkAttack(data);
            }

            // 然后再处理 PlayerDataManager 的数据移除/更新
            if (data.TargetDestroyed)
            {
                // 目标被摧毁，从 PlayerDataManager 移除
                bool targetRemoved = playerDataManager.RemoveUnit(data.TargetPlayerId, targetPos);

                if (targetRemoved)
                {
                    Debug.Log($"[网络] 目标单位已从PlayerDataManager移除");
                }
                else
                {
                    Debug.LogWarning($"[网络] ✗ 从PlayerDataManager移除目标失败");
                }
            }
            else if (data.TargetSyncData.HasValue)
            {
                // 目标存活，更新同步数据
                bool targetUpdated = playerDataManager.UpdateUnitSyncDataByPos(
                    data.TargetPlayerId, targetPos, data.TargetSyncData.Value);

                if (targetUpdated)
                {
                    Debug.Log($"[网络] 目标数据已更新");
                }
            }
        }
    }

    // 建筑攻击
    private void HandleBuildingAttack(NetworkMessage message)
    {
        BuildingAttackMessage data = JsonConvert.DeserializeObject<BuildingAttackMessage>(message.JsonData);

        int2 attackerPos = new int2(data.AttackerPosX, data.AttackerPosY);
        int2 buildingPos = new int2(data.BuildingPosX, data.BuildingPosY);

        Debug.Log($"[网络] 玩家 {data.AttackerPlayerId} 攻击建筑 ID={data.BuildingID} at ({buildingPos.x},{buildingPos.y})");

        // 确保管理器存在
        if (gameManage == null)
        {
            gameManage = GameManage.Instance;
        }

        // 更新 PlayerDataManager 中的攻击者数据
        if (playerDataManager != null)
        {
            // 更新攻击者的同步数据
            bool attackerUpdated = playerDataManager.UpdateUnitSyncDataByPos(
                data.AttackerPlayerId, attackerPos, data.AttackerSyncData);

            if (attackerUpdated)
            {
                Debug.Log($"[网络] 攻击者数据已更新");
            }

            // 通知 PlayerOperationManager 处理建筑攻击
            if (gameManage != null && gameManage._PlayerOperation != null)
            {
                gameManage._PlayerOperation.HandleNetworkBuildingAttack(data);
            }

            // 如果建筑被摧毁，从 PlayerDataManager 和 BuildingManager 移除
            if (data.BuildingDestroyed)
            {
                // 从 PlayerDataManager 移除建筑数据
                bool buildingRemoved = playerDataManager.RemoveUnit(data.BuildingOwnerId, buildingPos);

                if (buildingRemoved)
                {
                    Debug.Log($"[网络] 建筑已从PlayerDataManager移除");
                }
                else
                {
                    Debug.LogWarning($"[网络] ✗ 从PlayerDataManager移除建筑失败");
                }

                // 从 BuildingManager 移除建筑
                if (GameManage.Instance._BuildingManager != null)
                {
                    GameManage.Instance._BuildingManager.RemoveBuilding(data.BuildingID);
                    Debug.Log($"[网络] 建筑ID={data.BuildingID}已从BuildingManager移除");
                }
            }
            else
            {
                // 建筑存活，更新建筑HP
                if (GameManage.Instance._BuildingManager != null)
                {
                    Buildings.Building building = GameManage.Instance._BuildingManager.GetBuilding(data.BuildingID);
                    if (building != null)
                    {
                        building.SetHP(data.BuildingRemainingHP);
                        Debug.Log($"[网络] 建筑HP已更新为 {data.BuildingRemainingHP}");
                    }
                }
            }
        }
    }

    // 单位魅惑
    private void HandleUnitCharm(NetworkMessage message)
    {
        UnitCharmMessage data = JsonConvert.DeserializeObject<UnitCharmMessage>(message.JsonData);

        int2 missionaryPos = new int2(data.MissionaryPosX, data.MissionaryPosY);
        int2 targetPos = new int2(data.TargetPosX, data.TargetPosY);

        Debug.Log($"[网络] 玩家 {data.MissionaryPlayerId} 的传教士魅惑单位 at ({targetPos.x},{targetPos.y})");

        // 确保管理器存在
        if (gameManage == null)
        {
            gameManage = GameManage.Instance;
        }

        // ===== 新增：确保目标单位数据存在 =====
        if (playerDataManager != null)
        {
            // 检查目标单位是否存在于PlayerDataManager中
            PlayerUnitData? targetUnit = playerDataManager.FindUnit(data.TargetPlayerId, targetPos);

            if (!targetUnit.HasValue)
            {
                Debug.LogWarning($"[网络魅惑] 目标单位不存在于PlayerDataManager，可能需要先创建");

                // 如果目标单位不存在，可能需要先从NewUnitSyncData创建
                // 注意：这种情况通常不应该发生，说明同步顺序有问题
                // 但为了健壮性，我们可以尝试添加单位
                CardType unitType = CardType.None;
                // 根据syncPieceData推断单位类型
                switch (data.NewUnitSyncData.piecetype)
                {
                    case PieceType.Pope:
                        unitType = CardType.Pope;
                        break;
                    case PieceType.Missionary:
                        unitType = CardType.Missionary;
                        break;
                    case PieceType.Military:
                        unitType = CardType.Solider;
                        break;
                    case PieceType.Farmer:
                        unitType = CardType.Farmer;
                        break;

                }
                playerDataManager.AddUnit(
                    data.TargetPlayerId,
                    unitType,
                    targetPos,
                    data.NewUnitSyncData,
                    null
                );

                Debug.Log($"[网络魅惑] 已添加缺失的目标单位数据");
            }
        }


        if (playerDataManager != null && gameManage != null && gameManage._PlayerOperation != null)
        {
            // 通知 PlayerOperationManager 处理魅惑
            gameManage._PlayerOperation.HandleNetworkCharm(data);
        }
    }

    // 魅惑过期（归还控制权）
    private void HandleCharmExpire(NetworkMessage message)
    {
        CharmExpireMessage data = JsonConvert.DeserializeObject<CharmExpireMessage>(message.JsonData);

        int2 pos = new int2(data.PosX, data.PosY);

        Debug.Log($"[网络] 单位 {data.UnitID} at ({pos.x},{pos.y}) 魅惑过期，归还给玩家 {data.OriginalOwnerId}");

        // 确保管理器存在
        if (gameManage == null)
        {
            gameManage = GameManage.Instance;
        }

        if (playerDataManager != null && gameManage != null && gameManage._PlayerOperation != null)
        {
            // 通知 PlayerOperationManager 处理魅惑过期
            gameManage._PlayerOperation.HandleNetworkCharmExpire(data);
        }
    }

    // Ping/Pong(心跳检测)
    private void HandlePing(NetworkMessage message)
    {
        NetworkMessage pong = new NetworkMessage
        {
            MessageType = NetworkMessageType.PONG,
            SenderId = localClientId
        };

        if (isServer)
        {
            SendToClient(message.SenderId, pong);
        }
        else
        {
            SendToServer(pong);
        }
    }

    private void HandlePong(NetworkMessage message)
    {
        // 可以用来计算延迟
        long latency = DateTime.Now.Ticks - message.Timestamp;
        Debug.Log($"延迟: {latency / 10000}ms");
    }

    // *************************
    //      辅助函数
    // *************************

    private uint GetClientId(IPEndPoint endPoint)
    {
        foreach (var kvp in clients)
        {
            if (kvp.Value.Equals(endPoint))
                return kvp.Key;
        }
        return 0;
    }

    public void Shutdown()
    {
        Debug.Log("Server Over!");

        isRunning = false;

        if (networkThread != null && networkThread.IsAlive)
        {
            networkThread.Join(1000);
        }

        if (udpClient != null)
        {
            udpClient.Close();
            udpClient = null;
        }

        Debug.Log("网络系统已关闭");
        OnDisconnected?.Invoke();
    }

    // *************************
    //      运行时配置
    // *************************

    //public void SetConfig(bool asServer, string ip, int networkPort, int maxPlayerCount = 2)
    //{
    //    if (isRunning)
    //    {
    //        Debug.LogWarning("请在启动前设置配置!");
    //        return;
    //    }

    //    isServer = asServer;
    //    serverIP = ip;
    //    port = networkPort;
    //    maxPlayers = maxPlayerCount;
    //}

    //public void SetPlayerName(string name)
    //{
    //    playerName = name;
    //}
}

// *************************
//   主线程调度器 
// *************************
public class MainThreadDispatcher : MonoBehaviour
{
    private static MainThreadDispatcher instance;
    private static readonly Queue<Action> executionQueue = new Queue<Action>();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            // 2025.11.17 Guoning
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        lock (executionQueue)
        {
            while (executionQueue.Count > 0)
            {
                executionQueue.Dequeue().Invoke();
            }
        }
    }


    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
            Debug.Log("MainThreadDispatcher已销毁");
        }
    }
    public static void Enqueue(Action action)
    {
        if (action == null) return;

        lock (executionQueue)
        {
            executionQueue.Enqueue(action);
        }
    }
}