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

    BUILDING_DESTRUCTION, // 建筑摧毁 (新增)
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
#region ===具体网络消息====
// *************************
//      具体消息数据
// *************************
[Serializable]
public class ConnectMessage
{
    public string PlayerName;
    public string PlayerIP; 
    public int PlayerReligion; // Religion as int for serialization
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
    public int PlayerReligion; // Religion as int
}

// 玩家加入消息
[Serializable]
public class PlayerJoinedMessage
{
    public uint PlayerId;
    public string PlayerName;
    public string PlayerIP;
    public int PlayerReligion; // Religion as int
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

// 建筑摧毁消息
[Serializable]
public class BuildingDestructionMessage
{
    public int BuildingOwnerId;    // 建筑所有者ID
    public int BuildingPosX;       // 建筑位置X
    public int BuildingPosY;       // 建筑位置Y
    public int BuildingID;         // 被摧毁的建筑ID
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


// 游戏结束消息
[Serializable]
public class GameOverMessage
{
    public int WinnerPlayerId;     // 获胜的玩家ID
    public int LoserPlayerId;      // 失败的玩家ID
    public string Reason;          // 结束原因: "surrender"(投降), "building_destroyed"(建筑摧毁), "disconnect"(断线) 等
    public ResultData ResultData;   // 结局数据
}

#endregion

#region 序列化玩家数据
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

#endregion


// 服务器发现信息
[Serializable]
public class ServerInfo
{
    public string ServerIP;
    public string ServerName;
    public int Port;
    public int CurrentPlayers;
    public int MaxPlayers;
    public long LastSeen; // Timestamp
    public int MapSerialNumber;

    public ServerInfo(string ip, string name, int port, int current, int max,int map)
    {
        ServerIP = ip;
        ServerName = name;
        Port = port;
        CurrentPlayers = current;
        MaxPlayers = max;
        LastSeen = DateTime.Now.Ticks;
        MapSerialNumber = map;
    }
}

// 服务器广播消息
[Serializable]
public class ServerBroadcastMessage
{
    public string ServerName;
    public string ServerIP;
    public int Port;
    public int CurrentPlayers;
    public int MaxPlayers;
    public int MapSerialNumber;
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
    [SerializeField] private int broadcastPort = 8889; // 用于服务器发现的广播端口
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

    // 服务器发现相关
    private UdpClient broadcastClient;
    private Thread broadcastThread;
    private bool isBroadcasting = false;
    private List<ServerInfo> discoveredServers = new List<ServerInfo>();

    // 房间相关
    // 玩家IP
    private string playerIP = "";
    // 游戏状态
    private bool isGameStarted = false;
    private List<uint> connectedPlayers = new List<uint>();
    private List<Religion> PlayerReligions = new List<Religion>();

    // 客户端准备状态和IP
    private Dictionary<uint, bool> clientReadyStatus; // 服务器: 客户端准备状态
    private Dictionary<uint, string> clientIPs; // 服务器: 客户端IP地址
    private Dictionary<uint, Religion> clientReligions; // 服务器: 客户端宗教


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
    public event Action<List<ServerInfo>> OnServersDiscovered; // 发现服务器列表更新

    // 属性
    public bool bIsConnected => isRunning;
    public bool bIsServer => isServer;
    public uint bLocalClientId => localClientId;
    public List<uint> ConnectedPlayers => new List<uint>(connectedPlayers);
    public int PlayerCount => connectedPlayers.Count;


    // 获取房间玩家信息
    public List<PlayerInfo> RoomPlayers => new List<PlayerInfo>(roomPlayers);
    public bool IsLocalReady => isLocalReady;
    public List<ServerInfo> DiscoveredServers => new List<ServerInfo>(discoveredServers);

    // 引用
    private GameManage gameManage;
    private PlayerDataManager playerDataManager;



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
        clientReligions = new Dictionary<uint, Religion>();

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
        yield return new WaitForSeconds(0.1f); 

        // 获取 GameManage 引用
        GetGameManage();

        if (SceneStateManager.Instance.bIsSingle)
        {
            // 加载地图
            HexMapManager.Instance.InitHexMapManager();
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
                { NetworkMessageType.GAME_OVER, HandleGameOver },  // 游戏结束 (包括投降)  ← 统一处理
                { NetworkMessageType.TURN_START, HandleTurnStart },
                { NetworkMessageType.TURN_END, HandleTurnEnd },
                 // 单位相关
                { NetworkMessageType.UNIT_MOVE, HandleUnitMove },
                { NetworkMessageType.UNIT_ADD, HandleUnitAdd },
                { NetworkMessageType.UNIT_REMOVE, HandleUnitRemove },
                { NetworkMessageType.UNIT_ATTACK, HandleUnitAttack },
                { NetworkMessageType.BUILDING_ATTACK, HandleBuildingAttack },
                          { NetworkMessageType.BUILDING_DESTRUCTION, HandleBuildingDestruction }, // 新增建筑摧毁
                { NetworkMessageType.UNIT_CHARM, HandleUnitCharm },
                { NetworkMessageType.CHARM_EXPIRE, HandleCharmExpire },

                { NetworkMessageType.PING, HandlePing },
                { NetworkMessageType.PONG, HandlePong }
        };

        //Debug.Log($"=== 消息处理器注册完成 ===");
        //Debug.Log($"共注册 {messageHandlers.Count} 个处理器");
    }
    #region 服务器功能
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
            clientReligions = new Dictionary<uint, Religion>();

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
                IsReady = true,
                PlayerReligion = (int)SceneStateManager.Instance.PlayerReligion
            });

            // 启动接收线程
            networkThread = new Thread(ServerLoop) { IsBackground = true };
            networkThread.Start();

            Debug.Log($"[服务器] 启动成功 - 端口: {port}");

            // 启动服务器广播
            StartServerBroadcast();

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
    // 服务器端: 启动广播
    private void StartServerBroadcast()
    {
        if (isBroadcasting)
            return;

        try
        {
            isBroadcasting = true;
            broadcastThread = new Thread(ServerBroadcastLoop) { IsBackground = true };
            broadcastThread.Start();
            Debug.Log($"[服务器] 开始广播服务器信息 (端口: {broadcastPort})");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[服务器] 启动广播失败: {ex.Message}");
            isBroadcasting = false;
        }
    }

    // 服务器广播循环
    private void ServerBroadcastLoop()
    {
        using (UdpClient broadcastSender = new UdpClient())
        {
            broadcastSender.EnableBroadcast = true;
            IPEndPoint broadcastEP = new IPEndPoint(IPAddress.Broadcast, broadcastPort);

            while (isBroadcasting && isRunning)
            {
                try
                {
                    ServerBroadcastMessage broadcastMsg = new ServerBroadcastMessage
                    {
                        ServerName = playerName,
                        ServerIP = playerIP,
                        Port = port,
                        CurrentPlayers = connectedPlayers.Count,
                        MaxPlayers = maxPlayers,
                        MapSerialNumber=SceneStateManager.Instance.mapSerialNumber,
                    };

                    string json = JsonConvert.SerializeObject(broadcastMsg);
                    byte[] data = Encoding.UTF8.GetBytes(json);
                    broadcastSender.Send(data, data.Length, broadcastEP);

                    //Debug.Log($"[服务器] 广播: {playerName} ({playerIP}:{port}) - {connectedPlayers.Count}/{maxPlayers}");
                }
                catch (Exception ex)
                {
                    if (isBroadcasting)
                        Debug.LogError($"[服务器] 广播错误: {ex.Message}");
                }

                Thread.Sleep(2000); // 每2秒广播一次
            }
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
                        clientReligions[clientId] = (Religion)connectData.PlayerReligion; // 保存宗教
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
                            PlayerIP = connectData.PlayerIP,
                            PlayerReligion = connectData.PlayerReligion
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
    #endregion

    #region 客户端功能
    // *************************
    //      客户端功能
    // *************************

    public void ConnectToServer()
    {
        StartServerDiscovery();
        //// 如果serverIP为空或默认值，启动自动发现
        //if (string.IsNullOrEmpty(serverIP) || serverIP == "127.0.0.1")
        //{
        //    Debug.Log("[客户端] 开始自动搜索服务器...");
        //    StartServerDiscovery();
        //    return;
        //}
        //// 使用指定IP连接
        //ConnectToSpecificServer(serverIP, port);

    }
    // 连接到特定服务器
    private void ConnectToSpecificServer(string targetIP, int targetPort)
    {
        Debug.Log($"[客户端] 尝试连接到服务器: {targetIP}:{targetPort}");

        // 添加服务器检测
        bool serverExists = false;
        using (UdpClient testClient = new UdpClient())
        {
            try
            {
                testClient.Connect(targetIP, targetPort);
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
                Debug.Log("socket error");
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
            serverEndPoint = new IPEndPoint(IPAddress.Parse(targetIP), targetPort);
            udpClient = new UdpClient();
            isRunning = true;

            // 发送连接请求
            ConnectMessage connectMsg = new ConnectMessage
            {
                PlayerName = playerName,
                PlayerIP = playerIP,
                PlayerReligion = (int)SceneStateManager.Instance.PlayerReligion
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
            Debug.Log($"[客户端] 正在连接到 {targetIP}:{targetPort}...");
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

    // 客户端: 启动服务器发现
    private void StartServerDiscovery()
    {
        discoveredServers.Clear();

        try
        {
            broadcastClient = new UdpClient(broadcastPort);
            broadcastClient.EnableBroadcast = true;

            // 启动监听线程
            broadcastThread = new Thread(ClientDiscoveryLoop) { IsBackground = true };
            broadcastThread.Start();

            Debug.Log($"[客户端] 开始搜索局域网服务器... (端口: {broadcastPort})");

            // 5秒后如果没发现服务器，停止搜索
            StartCoroutine(StopDiscoveryAfterTimeout(5.0f));
        }
        catch (Exception ex)
        {
            Debug.LogError($"[客户端] 启动服务器发现失败: {ex.Message}");
        }
    }

    // 客户端发现循环
    private void ClientDiscoveryLoop()
    {
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, broadcastPort);
        DateTime startTime = DateTime.Now;

        while ((DateTime.Now - startTime).TotalSeconds < 5)
        {
            try
            {
                // ===== 添加 null 检查 =====
                if (broadcastClient == null)
                {
                    Debug.LogWarning("[客户端] broadcastClient 已被释放，停止搜索");
                    break;
                }

                if (discoveredServers == null)
                {
                    Debug.LogError("[客户端] discoveredServers 为 null");
                    break;
                }
                // ===== 检查结束 =====

                if (broadcastClient.Available > 0)
                {
                    byte[] data = broadcastClient.Receive(ref remoteEP);
                    string json = Encoding.UTF8.GetString(data);

                    ServerBroadcastMessage broadcastMsg = JsonConvert.DeserializeObject<ServerBroadcastMessage>(json);

                    if (broadcastMsg != null)
                    {
                        // 再次检查 discoveredServers
                        if (discoveredServers == null)
                        {
                            Debug.LogError("[客户端] discoveredServers 在操作过程中变为 null");
                            break;
                        }

                        // 检查是否已存在
                        ServerInfo existingServer = discoveredServers.Find(s =>
                            s.ServerIP == broadcastMsg.ServerIP && s.Port == broadcastMsg.Port);

                        if (existingServer != null)
                        {
                            // 更新现有服务器信息
                            existingServer.CurrentPlayers = broadcastMsg.CurrentPlayers;
                            existingServer.MaxPlayers = broadcastMsg.MaxPlayers;
                            existingServer.LastSeen = DateTime.Now.Ticks;
                        }
                        else
                        {
                            // 添加新服务器
                            ServerInfo newServer = new ServerInfo(
                                broadcastMsg.ServerIP,
                                broadcastMsg.ServerName,
                                broadcastMsg.Port,
                                broadcastMsg.CurrentPlayers,
                                broadcastMsg.MaxPlayers,
                                broadcastMsg.MapSerialNumber
                            );
                            discoveredServers.Add(newServer);

                            Debug.Log($"[客户端] 发现服务器: {newServer.ServerName} ({newServer.ServerIP}:{newServer.Port}) - {newServer.CurrentPlayers}/{newServer.MaxPlayers}");

                            // 通知UI更新
                            MainThreadDispatcher.Enqueue(() =>
                            {
                                if (OnServersDiscovered != null)  // 添加检查
                                {
                                    OnServersDiscovered.Invoke(discoveredServers);
                                }
                            });
                        }
                    }
                }

                Thread.Sleep(100);
            }
            catch (Exception ex)
            {
                // ===== 改进错误日志 =====
                Debug.LogError($"[客户端] 发现服务器错误: {ex.Message}\n堆栈跟踪: {ex.StackTrace}");
                break;
            }
        }

        // 搜索完成
        MainThreadDispatcher.Enqueue(() =>
        {
            OnDiscoveryComplete();
        });
    }

    // 发现完成
    private void OnDiscoveryComplete()
    {
        if (broadcastClient != null)
        {
            broadcastClient.Close();
            broadcastClient = null;
        }

        Debug.Log($"[客户端] 服务器搜索完成，找到 {discoveredServers.Count} 个服务器");

        if (discoveredServers.Count > 0)
        {
            // 自动连接到第一个可用服务器
            ServerInfo firstServer = discoveredServers[0];
            Debug.Log($"[客户端] 自动连接到: {firstServer.ServerName} ({firstServer.ServerIP}:{firstServer.Port})");

            serverIP = firstServer.ServerIP;
            port = firstServer.Port;
            ConnectToSpecificServer(serverIP, port);
            SceneStateManager.Instance.mapSerialNumber = discoveredServers[0].MapSerialNumber;
            HexMapManager.Instance.InitHexMapManager();
        }
        else
        {
            Debug.LogWarning("[客户端] 未发现任何服务器");
            SceneController.Instance?.SwitchScene("SelectScene", null);
        }
    }

    // 超时后停止发现
    private IEnumerator StopDiscoveryAfterTimeout(float timeout)
    {
        yield return new WaitForSeconds(timeout);

        if (broadcastThread != null && broadcastThread.IsAlive)
        {
            if (broadcastClient != null)
            {
                broadcastClient.Close();
            }
        }
    }

    // 手动连接到已发现的服务器
    public void ConnectToDiscoveredServer(ServerInfo server)
    {
        if (server == null)
        {
            Debug.LogError("[客户端] 服务器信息为空");
            return;
        }

        Debug.Log($"[客户端] 连接到选定服务器: {server.ServerName} ({server.ServerIP}:{server.Port})");
        serverIP = server.ServerIP;
        port = server.Port;
        ConnectToSpecificServer(serverIP, port);
    }


    #endregion

    #region   (旧)房间功能
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
            IsReady = true,
            PlayerReligion = (int)SceneStateManager.Instance.PlayerReligion
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
                int religionValue = 0;
                if (clientReligions.ContainsKey(clientId))
                {
                    religionValue = (int)clientReligions[clientId];
                }
                roomPlayers.Add(new PlayerInfo
                {
                    PlayerId = clientId,
                    PlayerName = clientNames[clientId],
                    PlayerIP = clientIPs.ContainsKey(clientId) ? clientIPs[clientId] : "Unknown",
                    IsReady = clientReady,
                    PlayerReligion = religionValue
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
        Debug.Log("CheckAllPlayersReady");
        if (roomPlayers.Count < 2) // 至少需要2个玩家
        {
            OnAllPlayersReady?.Invoke(false);
            return;
        }

        bool allReady = true;
        foreach (var player in roomPlayers)
        {
            Debug.Log("Player "+player.PlayerId+" Ready? "+player.IsReady);
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

    #endregion
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
        int[] playerReligions = new int[connectedPlayers.Count];
        for (int i = 0; i < connectedPlayers.Count; i++)
        {
            playerIds[i] = (int)connectedPlayers[i];
            // 获取每个玩家的宗教
            if (connectedPlayers[i] == 0)
            {
                // 服务器玩家 (Player 0)
                playerReligions[i] = (int)SceneStateManager.Instance.PlayerReligion;
            }
            else
            {
                // 客户端玩家
                if (clientReligions.ContainsKey(connectedPlayers[i]))
                {
                    playerReligions[i] = (int)clientReligions[connectedPlayers[i]];
                }
                else
                {
                    playerReligions[i] = (int)Religion.None;
                    Debug.LogWarning($"玩家 {connectedPlayers[i]} 的宗教信息缺失，使用默认值");
                }
            }

            Debug.Log($"玩家 {playerIds[i]} 的宗教: {(Religion)playerReligions[i]}");
        }

        // 服务器加载地图
        HexMapManager.Instance.InitHexMapManager();

        GameStartData gameData = new GameStartData
        {
            PlayerIds = playerIds,
            StartPositions = AssignStartPositions(),
            FirstTurnPlayerId = (int)connectedPlayers[0],
            PlayerReligions = playerReligions
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
            
            }

            return positions;
        }

        // 默认位置
        return new int[] { 0, 99 };
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

    #region 发送具体单位消息
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

    /// <summary>
    /// 发送建筑摧毁消息
    /// </summary>
    public void SendBuildingDestructionMessage(
        int buildingOwnerId,
        int2 buildingPos,
        int buildingID)
    {
        BuildingDestructionMessage destructionData = new BuildingDestructionMessage
        {
            BuildingOwnerId = buildingOwnerId,
            BuildingPosX = buildingPos.x,
            BuildingPosY = buildingPos.y,
            BuildingID = buildingID
        };

        NetworkMessage msg = new NetworkMessage
        {
            MessageType = NetworkMessageType.BUILDING_DESTRUCTION,
            SenderId = localClientId,
            JsonData = JsonConvert.SerializeObject(destructionData)
        };

        if (isServer)
        {
            BroadcastToClients(msg, localClientId);
            Debug.Log($"[网络-服务器] 广播 BUILDING_DESTRUCTION 消息给所有客户端");
        }
        else
        {
            SendToServer(msg);
            Debug.Log($"[网络-客户端] 发送 BUILDING_DESTRUCTION 消息到服务器");
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
    /// <summary>
    /// 发送游戏结束消息
    /// </summary>
    /// <param name="winnerPlayerId">获胜玩家ID</param>
    /// <param name="loserPlayerId">失败玩家ID</param>
    /// <param name="reason">结束原因: "surrender"(投降), "building_destroyed"(建筑摧毁), "disconnect"(断线) 等</param>
    public void SendGameOverMessage(int winnerPlayerId, int loserPlayerId,  ResultData data,string reason = "surrender")
    {
        GameOverMessage gameOverData = new GameOverMessage
        {
            WinnerPlayerId = winnerPlayerId,
            LoserPlayerId = loserPlayerId,
            Reason = reason,
            ResultData= data
        };

        NetworkMessage msg = new NetworkMessage
        {
            MessageType = NetworkMessageType.GAME_OVER,
            SenderId = localClientId,
            JsonData = JsonConvert.SerializeObject(gameOverData)
        };

        Debug.Log($"[网络] 发送游戏结束消息: 获胜者={winnerPlayerId}, 失败者={loserPlayerId}, 原因={reason}");
     
        if (isServer)
        {
            // 服务器先处理自己的游戏结束
            HandleGameOver(msg);
            Debug.Log($"[网络-服务器] 处理游戏结束消息并广播给所有客户端");
        }
        else
        {
            SendToServer(msg);
            Debug.Log($"[网络-客户端] 发送游戏结束消息到服务器");
        }
    }
    #endregion
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


            if (gameManage != null)
            {
                Debug.Log($" 调用 StartTurn({data.PlayerId})");
                gameManage.StartTurn(data.PlayerId);
                //Debug.Log($" StartTurn 调用完成");
            }
            else
            {
                Debug.LogError(" 无法找到 GameManage，延迟重试");

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
    // 处理游戏结束消息
    private void HandleGameOver(NetworkMessage message)
    {
        Debug.Log($"=== HandleGameOver 被调用 ===");

        if (gameManage == null)
        {
            gameManage = GameManage.Instance;
        }

        GameOverMessage data = JsonConvert.DeserializeObject<GameOverMessage>(message.JsonData);

        Debug.Log($"游戏结束! 获胜者: 玩家 {data.WinnerPlayerId}, 失败者: 玩家 {data.LoserPlayerId}, 原因: {data.Reason}");

        // 触发游戏结束事件
        MainThreadDispatcher.Enqueue(() =>
        {
            if (gameManage != null)
            {
                Debug.Log($"触发游戏结束事件，获胜者: {data.WinnerPlayerId}");
                gameManage.TriggerGameEnded(data.WinnerPlayerId,data.ResultData);
            }
        });

        // 如果是服务器，广播游戏结束消息给所有客户端
        if (isServer && clients != null && clients.Count > 0)
        {
            Debug.Log($"[服务器] 广播游戏结束消息给所有客户端");
            BroadcastToClients(message, uint.MaxValue);
        }
    }
    #region 处理具体单位操作消息
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
    /// <summary>
    /// 处理建筑摧毁消息
    /// </summary>
    private void HandleBuildingDestruction(NetworkMessage message)
    {
        BuildingDestructionMessage data = JsonConvert.DeserializeObject<BuildingDestructionMessage>(message.JsonData);

        int2 buildingPos = new int2(data.BuildingPosX, data.BuildingPosY);

        Debug.Log($"[网络] 玩家 {data.BuildingOwnerId} 的建筑 ID={data.BuildingID} at ({buildingPos.x},{buildingPos.y}) 被摧毁");

        // 确保管理器存在
        if (gameManage == null)
        {
            gameManage = GameManage.Instance;
        }

        // 从 PlayerDataManager 移除建筑数据
        if (playerDataManager != null)
        {
            bool buildingRemoved = playerDataManager.RemoveUnit(data.BuildingOwnerId, buildingPos);

            if (buildingRemoved)
            {
                Debug.Log($"[网络] 建筑已从PlayerDataManager移除");
            }
        }

        // 从 BuildingManager 移除建筑
        if (gameManage != null && gameManage._BuildingManager != null)
        {
            gameManage._BuildingManager.RemoveBuilding(data.BuildingID);
            Debug.Log($"[网络] 建筑已从BuildingManager移除");
        }

        // 通知 PlayerOperationManager 处理建筑摧毁的视觉效果
        if (gameManage != null && gameManage._PlayerOperation != null)
        {
            gameManage._PlayerOperation.HandleNetworkBuildingDestruction(data);
        }

        Debug.Log($"[网络] 建筑摧毁消息处理完成");
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
                        unitType = CardType.Soldier;
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
    #endregion
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
        isBroadcasting = false;

        if (networkThread != null && networkThread.IsAlive)
        {
            networkThread.Join(1000);
        }

        if (broadcastThread != null && broadcastThread.IsAlive)
        {
            broadcastThread.Join(1000);
        }

        if (udpClient != null)
        {
            udpClient.Close();
            udpClient = null;
        }

        if (broadcastClient != null)
        {
            broadcastClient.Close();
            broadcastClient = null;
        }

        Debug.Log("网络系统已关闭");
        OnDisconnected?.Invoke();
    }
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