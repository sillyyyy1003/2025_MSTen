using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using Newtonsoft.Json;



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

    // 回合管理
    TURN_START,
    TURN_END,

    // 玩家操作
    UNIT_MOVE,
    UNIT_ADD,
    UNIT_REMOVE,
    UNIT_ATTACK,

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
}

[Serializable]
public class ConnectedMessage
{
    public uint AssignedClientId;
    public List<uint> ExistingPlayerIds;
}

[Serializable]
public class PlayerJoinedMessage
{
    public uint PlayerId;
    public string PlayerName;
}

[Serializable]
public class UnitMoveMessage
{
    public int PlayerId;
    public int FromX;
    public int FromY;
    public int ToX;
    public int ToY;
}

[Serializable]
public class UnitAddMessage
{
    public int PlayerId;
    public int UnitType; // PlayerUnitType as int
    public int PosX;
    public int PosY;
}

[Serializable]
public class UnitRemoveMessage
{
    public int PlayerId;
    public int PosX;
    public int PosY;
}

[Serializable]
public class TurnEndMessage
{
    public int PlayerId;
    public string PlayerDataJson; // PlayerData序列化
}


// *************************
//      主要网络系统
// *************************
public class NetGameSystem : MonoBehaviour
{
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

    // 游戏状态
    private bool isGameStarted = false;
    private List<uint> connectedPlayers = new List<uint>();

    // 消息处理器
    private Dictionary<NetworkMessageType, Action<NetworkMessage>> messageHandlers;

    // 事件
    public event Action<NetworkMessage> OnMessageReceived;
    public event Action<uint> OnClientConnected;  // 服务器端
    public event Action<uint> OnClientDisconnected;
    public event Action OnConnectedToServer;      // 客户端端
    public event Action OnDisconnected;
    public event Action OnGameStarted;

    // 属性
    public bool IsConnected => isRunning;
    public bool IsServer => isServer;
    public uint LocalClientId => localClientId;
    public List<uint> ConnectedPlayers => new List<uint>(connectedPlayers);
    public int PlayerCount => connectedPlayers.Count;

    // 引用
    private GameManage gameManage;
    private PlayerDataManager playerDataManager;

    // *************************
    //      Unity生命周期
    // *************************

    private void Awake()
    {
        // 确保MainThreadDispatcher存在
        if (FindObjectOfType<MainThreadDispatcher>() == null)
        {
            GameObject dispatcherObj = new GameObject("MainThreadDispatcher");
            dispatcherObj.AddComponent<MainThreadDispatcher>();
        }

        // 初始化消息处理器
        InitializeMessageHandlers();
    }

    private void Start()
    {
        // 获取管理器引用
        gameManage = GameManage.Instance;
        playerDataManager = PlayerDataManager.Instance;

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

    private void OnDestroy()
    {
        Shutdown();
    }

    // *************************
    //         初始化
    // *************************

    private void InitializeMessageHandlers()
    {
        messageHandlers = new Dictionary<NetworkMessageType, Action<NetworkMessage>>
        {
            { NetworkMessageType.CONNECT, HandleConnect },
            { NetworkMessageType.CONNECTED, HandleConnected },
            { NetworkMessageType.PLAYER_JOINED, HandlePlayerJoined },
            { NetworkMessageType.GAME_START, HandleGameStart },
            { NetworkMessageType.TURN_END, HandleTurnEnd },
            { NetworkMessageType.UNIT_MOVE, HandleUnitMove },
            { NetworkMessageType.UNIT_ADD, HandleUnitAdd },
            { NetworkMessageType.UNIT_REMOVE, HandleUnitRemove },
            { NetworkMessageType.PING, HandlePing },
            { NetworkMessageType.PONG, HandlePong }
        };
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
            connectedPlayers.Clear();

            udpClient = new UdpClient(port);
            isRunning = true;
            localClientId = 0; // 服务器ID为0

            networkThread = new Thread(ServerLoop) { IsBackground = true };
            networkThread.Start();

            Debug.Log($"[服务器] 启动成功 - 端口: {port}");

            // 服务器自己也算一个玩家
            connectedPlayers.Add(0);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[服务器] 启动失败: {ex.Message}");
        }
    }

    private void ServerLoop()
    {
        while (isRunning)
        {
            Debug.Log("Server is Running");
            try
            {
                IPEndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = udpClient.Receive(ref clientEndPoint);
                string jsonData = Encoding.UTF8.GetString(data);

                NetworkMessage message = JsonConvert.DeserializeObject<NetworkMessage>(jsonData);

                if (message.MessageType == NetworkMessageType.CONNECT)
                {
                    // 处理新客户端连接
                    HandleNewClientConnection(clientEndPoint, message);
                }
                else
                {
                    // 处理其他消息
                    uint senderId = GetClientId(clientEndPoint);
                    message.SenderId = senderId;

                    // 在主线程处理
                    MainThreadDispatcher.Enqueue(() =>
                    {
                        ProcessMessage(message);
                    });

                    // 广播给其他客户端(除了发送者)
                    BroadcastToClients(message, senderId);
                }
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
                    Debug.LogError($"[服务器] 错误: {ex.Message}");
            }
        }
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

        if (connectedPlayers.Count < 2)
        {
            Debug.LogWarning("玩家不足,无法开始游戏!");
            return;
        }

        isGameStarted = true;

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
            FirstTurnPlayerId = (int)connectedPlayers[0]
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
        // 根据玩家数量分配起始位置
        if (gameManage != null && gameManage.GetBoardCount() > 0)
        {
            int boardCount = gameManage.GetBoardCount();
            int[] positions = new int[connectedPlayers.Count];

            // 简单分配: 第一个玩家在0, 最后一个玩家在最后一个格子
            for (int i = 0; i < positions.Length; i++)
            {
                if (i == 0)
                    positions[i] = 0;
                else if (i == positions.Length - 1)
                    positions[i] = boardCount - 1;
                else
                    positions[i] = (boardCount / positions.Length) * i;
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
        if (isRunning)
        {
            Debug.LogWarning("已经连接!");
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
                PlayerName = playerName
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
                    ProcessMessage(message);
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

    private void SendToServer(NetworkMessage message)
    {
        try
        {
            string jsonData = JsonConvert.SerializeObject(message);
            byte[] data = Encoding.UTF8.GetBytes(jsonData);
            udpClient.Send(data, data.Length, serverEndPoint);
        }
        catch (Exception ex)
        {
            Debug.LogError($"发送到服务器失败: {ex.Message}");
        }
    }

    private void SendToClient(uint clientId, NetworkMessage message)
    {
        if (!clients.ContainsKey(clientId)) return;

        try
        {
            string jsonData = JsonConvert.SerializeObject(message);
            byte[] data = Encoding.UTF8.GetBytes(jsonData);
            udpClient.Send(data, data.Length, clients[clientId]);
        }
        catch (Exception ex)
        {
            Debug.LogError($"发送到客户端 {clientId} 失败: {ex.Message}");
        }
    }

    private void BroadcastToClients(NetworkMessage message, uint excludeClientId)
    {
        if (!isServer) return;

        foreach (var kvp in clients)
        {
            if (kvp.Key != excludeClientId)
            {
                SendToClient(kvp.Key, message);
            }
        }
    }

    // *************************
    //      消息处理
    // *************************

    private void ProcessMessage(NetworkMessage message)
    {
        OnMessageReceived?.Invoke(message);

        if (messageHandlers.ContainsKey(message.MessageType))
        {
            messageHandlers[message.MessageType]?.Invoke(message);
        }
        else
        {
            Debug.LogWarning($"未处理的消息类型: {message.MessageType}");
        }
    }

    // 连接消息(客户端不应该收到此消息)
    private void HandleConnect(NetworkMessage message)
    {
        Debug.LogWarning("[客户端] 收到CONNECT消息,这不应该发生");
    }

    // 连接确认
    private void HandleConnected(NetworkMessage message)
    {
        ConnectedMessage data = JsonConvert.DeserializeObject<ConnectedMessage>(message.JsonData);
        localClientId = data.AssignedClientId;
        connectedPlayers = data.ExistingPlayerIds;

        Debug.Log($"[客户端] 已连接到服务器! 分配ID: {localClientId}");
        OnConnectedToServer?.Invoke();
    }

    // 玩家加入
    private void HandlePlayerJoined(NetworkMessage message)
    {
        PlayerJoinedMessage data = JsonConvert.DeserializeObject<PlayerJoinedMessage>(message.JsonData);

        if (!connectedPlayers.Contains(data.PlayerId))
        {
            connectedPlayers.Add(data.PlayerId);
            Debug.Log($"玩家 {data.PlayerId} ({data.PlayerName}) 加入游戏 - 当前玩家数: {connectedPlayers.Count}");
        }
    }

    // 游戏开始
    private void HandleGameStart(NetworkMessage message)
    {
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
            gameManage.InitGameWithNetworkData(data);
        }
    }

    // 回合结束
    private void HandleTurnEnd(NetworkMessage message)
    {
        TurnEndMessage data = JsonConvert.DeserializeObject<TurnEndMessage>(message.JsonData);

        Debug.Log($"收到玩家 {data.PlayerId} 的回合结束消息");

        // 解析玩家数据
        if (!string.IsNullOrEmpty(data.PlayerDataJson))
        {
            PlayerData playerData = JsonUtility.FromJson<PlayerData>(data.PlayerDataJson);

            // 更新数据
            if (playerDataManager != null)
            {
                playerDataManager.UpdatePlayerData(data.PlayerId, playerData);
            }
        }

        // 如果是服务器,切换到下一个回合
        if (isServer && gameManage != null)
        {
            // 找到下一个玩家
            int currentIndex = connectedPlayers.IndexOf((uint)data.PlayerId);
            int nextIndex = (currentIndex + 1) % connectedPlayers.Count;
            uint nextPlayerId = connectedPlayers[nextIndex];

            // 通知所有人开始下一个回合
            NetworkMessage turnStartMsg = new NetworkMessage
            {
                MessageType = NetworkMessageType.TURN_START,
                SenderId = 0,
                JsonData = JsonConvert.SerializeObject(new { PlayerId = (int)nextPlayerId })
            };

            BroadcastToClients(turnStartMsg, 0);

            // 服务器自己也处理
            gameManage.StartTurn((int)nextPlayerId);
        }
    }

    // 单位移动
    private void HandleUnitMove(NetworkMessage message)
    {
        UnitMoveMessage data = JsonConvert.DeserializeObject<UnitMoveMessage>(message.JsonData);

        Unity.Mathematics.int2 fromPos = new Unity.Mathematics.int2(data.FromX, data.FromY);
        Unity.Mathematics.int2 toPos = new Unity.Mathematics.int2(data.ToX, data.ToY);

        Debug.Log($"玩家 {data.PlayerId} 移动单位: ({fromPos.x},{fromPos.y}) -> ({toPos.x},{toPos.y})");

        if (playerDataManager != null)
        {
            playerDataManager.MoveUnit(data.PlayerId, fromPos, toPos);
        }
    }

    // 单位添加
    private void HandleUnitAdd(NetworkMessage message)
    {
        UnitAddMessage data = JsonConvert.DeserializeObject<UnitAddMessage>(message.JsonData);

        Unity.Mathematics.int2 pos = new Unity.Mathematics.int2(data.PosX, data.PosY);
        PlayerUnitType unitType = (PlayerUnitType)data.UnitType;

        Debug.Log($"玩家 {data.PlayerId} 添加单位: {unitType} at ({pos.x},{pos.y})");

        if (playerDataManager != null)
        {
            playerDataManager.AddUnit(data.PlayerId, unitType, pos);
        }
    }

    // 单位移除
    private void HandleUnitRemove(NetworkMessage message)
    {
        UnitRemoveMessage data = JsonConvert.DeserializeObject<UnitRemoveMessage>(message.JsonData);

        Unity.Mathematics.int2 pos = new Unity.Mathematics.int2(data.PosX, data.PosY);

        Debug.Log($"玩家 {data.PlayerId} 移除单位 at ({pos.x},{pos.y})");

        if (playerDataManager != null)
        {
            playerDataManager.RemoveUnit(data.PlayerId, pos);
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

    public void SetConfig(bool asServer, string ip, int networkPort, int maxPlayerCount = 2)
    {
        if (isRunning)
        {
            Debug.LogWarning("请在启动前设置配置!");
            return;
        }

        isServer = asServer;
        serverIP = ip;
        port = networkPort;
        maxPlayers = maxPlayerCount;
    }

    public void SetPlayerName(string name)
    {
        playerName = name;
    }
}

// *************************
//   主线程调度器 (已有但为了完整性再写一次)
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
            DontDestroyOnLoad(gameObject);
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

    public static void Enqueue(Action action)
    {
        if (action == null) return;

        lock (executionQueue)
        {
            executionQueue.Enqueue(action);
        }
    }
}