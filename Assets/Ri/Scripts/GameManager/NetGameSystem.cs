using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections;



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
    public GameManage gameManage;
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
    public void GetGameManage()
    {
        Debug.Log("获取gamemanage");
        gameManage = GameManage.Instance;
        playerDataManager = PlayerDataManager.Instance;
    }


    private void InitializeMessageHandlers()
    {
        messageHandlers = new Dictionary<NetworkMessageType, Action<NetworkMessage>>
        {
                { NetworkMessageType.CONNECT, HandleConnect },
                { NetworkMessageType.CONNECTED, HandleConnected },
                { NetworkMessageType.PLAYER_JOINED, HandlePlayerJoined },
                { NetworkMessageType.GAME_START, HandleGameStart },
                { NetworkMessageType.TURN_START, HandleTurnStart },  
                { NetworkMessageType.TURN_END, HandleTurnEnd },
                { NetworkMessageType.UNIT_MOVE, HandleUnitMove },
                { NetworkMessageType.UNIT_ADD, HandleUnitAdd },
                { NetworkMessageType.UNIT_REMOVE, HandleUnitRemove },
                { NetworkMessageType.PING, HandlePing },
                { NetworkMessageType.PONG, HandlePong }
        };

        Debug.Log($"=== 消息处理器注册完成 ===");
        Debug.Log($"共注册 {messageHandlers.Count} 个处理器");
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
            try
            {
                IPEndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = udpClient.Receive(ref clientEndPoint);
                string jsonData = Encoding.UTF8.GetString(data);

                NetworkMessage message = JsonConvert.DeserializeObject<NetworkMessage>(jsonData);
              
                Debug.Log($"[服务器] 消息类型: {message.MessageType}");
                Debug.Log($"[服务器] 发送者ID: {message.SenderId}");
              
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

                Debug.Log($"=== 客户端收到原始网络数据 ===");
                Debug.Log($"数据长度: {data.Length}");
                Debug.Log($"原始JSON: {jsonData}");

                NetworkMessage message = JsonConvert.DeserializeObject<NetworkMessage>(jsonData);

                // 在主线程处理消息
                MainThreadDispatcher.Enqueue(() =>
                {
                    Debug.Log($"主线程准备处理消息: {message.MessageType}");
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
            Debug.Log($"发送到服务" );

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
        if (!clients.ContainsKey(clientId))
        {
            Debug.LogError($"clients 字典中不存在 clientId: {clientId}");
            return;
        }

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
        Debug.Log($"=== BroadcastToClients 开始 ===");
        Debug.Log($"消息类型: {message.MessageType}");
        Debug.Log($"排除ID: {excludeClientId}");
        Debug.Log($"当前是服务器: {isServer}");
        Debug.Log($"clients 状态: {(clients == null ? "null" : $"Count={clients.Count}")}");


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

        int broadcastCount = 0;
        foreach (var kvp in clients)
        {
            Debug.Log($"[广播] 检查客户端 {kvp.Key}");

            if (excludeClientId == uint.MaxValue || kvp.Key != excludeClientId)
            {
                try
                {
                    Debug.Log($"[广播] 发送给客户端 {kvp.Key}");
                    SendToClient(kvp.Key, message);
                    broadcastCount++;
                    Debug.Log($"[广播]  发送成功");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[广播]  发送失败: {ex.Message}");
                }
            }
            else
            {
                Debug.Log($"[广播] 跳过客户端 {kvp.Key} (被排除)");
            }
        }

        Debug.Log($"=== BroadcastToClients 完成，共发送 {broadcastCount} 条 ===");
    }
    // *************************
    //      消息处理
    // *************************

    private void ProcessMessage(NetworkMessage message)
    {
        Debug.Log($"=== ProcessMessage: 开始处理消息类型 {message.MessageType} ===");

        // 触发事件
        OnMessageReceived?.Invoke(message);
        Debug.Log($"已触发 OnMessageReceived 事件");

        bool handled = false;

        // 主要处理器
        if (messageHandlers.ContainsKey(message.MessageType))
        {
            Debug.Log($"找到主要处理器，准备调用处理函数");
            messageHandlers[message.MessageType]?.Invoke(message);
            handled = true;
            Debug.Log($"主要处理器调用完成");
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

    // 连接消息(客户端不应该收到此消息)
    private void HandleConnect(NetworkMessage message)
    {
        Debug.LogWarning("[客户端] 收到CONNECT消息,这不应该发生");
    }


    // 添加回合开始消息
    private void HandleTurnStart(NetworkMessage message)
    {
        try
        {
            Debug.Log($"=== HandleTurnStart 被调用 ===");
            Debug.Log($"当前是服务器: {isServer}");
            Debug.Log($"消息发送者ID: {message.SenderId}");

            var data = JsonConvert.DeserializeObject<TurnStartMessage>(message.JsonData);
            Debug.Log($"目标玩家: {data.PlayerId}");

            // 多重查找 GameManage
            if (gameManage == null)
            {
                Debug.Log("gameManage 为 null，尝试查找...");
                gameManage = GameManage.Instance;
            }

            if (gameManage == null)
            {
                Debug.Log("尝试通过 GameObject.Find 查找...");
                GameObject gmObj = GameObject.Find("GameManager");
                if (gmObj != null)
                {
                    gameManage = gmObj.GetComponent<GameManage>();
                    Debug.Log($" 通过 GameObject.Find 找到 GameManage");
                }
                else
                {
                    Debug.LogError("GameObject.Find 未找到 'GameManager' 对象");
                }
            }

            if (gameManage != null)
            {
                Debug.Log($" 调用 StartTurn({data.PlayerId})");
                gameManage.StartTurn(data.PlayerId);
                Debug.Log($" StartTurn 调用完成");
            }
            else
            {
                Debug.LogError(" 无法找到 GameManage，延迟重试");

                // 列出场景中所有对象（调试用）
                GameObject[] allObjects = FindObjectsOfType<GameObject>();
                Debug.Log($"场景中共有 {allObjects.Length} 个 GameObject");

                bool foundGameManage = false;
                foreach (var obj in allObjects)
                {
                    if (obj.name.Contains("GameManage") || obj.name.Contains("GameManager"))
                    {
                        Debug.Log($"找到可能的对象: {obj.name}, 激活: {obj.activeInHierarchy}");
                        var gm = obj.GetComponent<GameManage>();
                        if (gm != null)
                        {
                            Debug.Log($" 这个对象有 GameManage 组件!");
                            gameManage = gm;
                            foundGameManage = true;
                            break;
                        }
                    }
                }

                if (!foundGameManage)
                {
                    Debug.LogError("场景中完全找不到 GameManage 组件!");
                    StartCoroutine(RetryHandleTurnStart(message, 0.5f));
                }
                else
                {
                    // 找到了，再次尝试调用
                    Debug.Log($" 通过遍历找到 GameManage，调用 StartTurn({data.PlayerId})");
                    gameManage.StartTurn(data.PlayerId);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($" 处理回合开始消息出错: {ex.Message}\n{ex.StackTrace}");
        }
    }


    // 添加重试协程
    private IEnumerator RetryHandleTurnStart(NetworkMessage message, float delay)
    {
        yield return new WaitForSeconds(delay);

        Debug.Log("=== 重试 HandleTurnStart ===");
        HandleTurnStart(message);
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

    // 回合结束
    private void HandleTurnEnd(NetworkMessage message)
    {
        Debug.Log($"=== HandleTurnEnd 被调用 ===");
        Debug.Log($"当前是服务器: {isServer}");
        Debug.Log($"gameManage 是否为空: {gameManage == null}");


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

        // 解析玩家数据
        if (!string.IsNullOrEmpty(data.PlayerDataJson))
        {
            PlayerData playerData = JsonUtility.FromJson<PlayerData>(data.PlayerDataJson);

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
                gameManage.GetComponent<PlayerOperationManager>()?
                    .UpdateOtherPlayerDisplay(data.PlayerId, playerData);
                Debug.Log($"已通知更新玩家 {data.PlayerId} 的显示");
            }

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
        else
        {
            playerDataManager = PlayerDataManager.Instance;
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
        else
        {

            playerDataManager = PlayerDataManager.Instance;
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
        else
        {
            playerDataManager = PlayerDataManager.Instance;
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