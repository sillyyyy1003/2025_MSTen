using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using Newtonsoft.Json;

// 网络消息
[System.Serializable]
public class NetworkMessage
{
    public uint SenderId;
    public string Data;
}

// 简化的网络系统 - 支持服务器和客户端
public class NetGameSystem: MonoBehaviour
{
    [Header("网络配置")]
    [SerializeField] private bool isServer = false;
    [SerializeField] private string serverIP = "192.168.1.100";
    [SerializeField] private int port = 8888;

    private UdpClient udpClient;
    private IPEndPoint serverEndPoint;
    private Dictionary<uint, IPEndPoint> clients; // 仅服务器使用
    private uint clientId;
    private uint nextClientId = 1;
    private bool isRunning = false;
    private Thread networkThread;

    // 事件
    public event Action<string> OnDataReceived;
    public event Action<uint> OnClientConnected;  // 服务器端事件
    public event Action OnConnected;              // 客户端事件

    public bool IsConnected => isRunning;
    public bool IsServer => isServer;

    private void Start()
    { 
        // 确保 MainThreadDispatcher 存在
        if (FindObjectOfType<MainThreadDispatcher>() == null)
        {
            GameObject dispatcherObj = new GameObject("MainThreadDispatcher");
            dispatcherObj.AddComponent<MainThreadDispatcher>();
        }
        if (isServer)
            StartServer();
        else
            ConnectToServer();
    }

    // 启动服务器
    private void StartServer()
    {
        try
        {
            clients = new Dictionary<uint, IPEndPoint>();
            udpClient = new UdpClient(port);
            isRunning = true;

            networkThread = new Thread(ServerLoop) { IsBackground = true };
            networkThread.Start();

            Debug.Log($"服务器已启动 - 端口: {port}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"服务器启动失败: {ex.Message}");
        }
    }

    // 连接到服务器
    private void ConnectToServer()
    {
        try
        {
            serverEndPoint = new IPEndPoint(IPAddress.Parse(serverIP), port);
            udpClient = new UdpClient();
            isRunning = true;

            // 发送连接请求
            SendToServer(new NetworkMessage { Data = "CONNECT" });

            networkThread = new Thread(ClientLoop) { IsBackground = true };
            networkThread.Start();

            Debug.Log($"正在连接服务器: {serverIP}:{port}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"连接失败: {ex.Message}");
        }
    }

    // 服务器循环
    private void ServerLoop()
    {
        while (isRunning)
        {
            try
            {
                IPEndPoint clientEndPoint = null;
                var data = udpClient.Receive(ref clientEndPoint);
                string jsonData = Encoding.UTF8.GetString(data);
                var message = JsonConvert.DeserializeObject<NetworkMessage>(jsonData);

                // 处理连接请求
                if (message.Data == "CONNECT")
                {
                    uint newClientId = nextClientId++;
                    clients[newClientId] = clientEndPoint;

                    // 发送客户端ID
                    var response = new NetworkMessage { SenderId = newClientId, Data = "CONNECTED" };
                    SendToClient(newClientId, response);

                    MainThreadDispatcher.Enqueue(() =>
                    {
                        OnClientConnected?.Invoke(newClientId);
                        Debug.Log($"客户端 {newClientId} 已连接");
                    });
                }
                else
                {
                    // 接收并转发数据
                    uint senderId = GetClientId(clientEndPoint);
                    MainThreadDispatcher.Enqueue(() => OnDataReceived?.Invoke(message.Data));

                    // 广播给其他客户端
                    BroadcastToClients(message, senderId);
                }
            }
            catch (Exception ex)
            {
                if (isRunning)
                    Debug.LogError($"服务器错误: {ex.Message}");
            }
        }
    }

    // 客户端循环
    private void ClientLoop()
    {
        while (isRunning)
        {
            try
            {
                var result = udpClient.Receive(ref serverEndPoint);
                string jsonData = Encoding.UTF8.GetString(result);
                var message = JsonConvert.DeserializeObject<NetworkMessage>(jsonData);

                if (message.Data == "CONNECTED")
                {
                    clientId = message.SenderId;
                    MainThreadDispatcher.Enqueue(() =>
                    {
                        OnConnected?.Invoke();
                        Debug.Log($"已连接到服务器，客户端ID: {clientId}");
                    });
                }
                else
                {
                    MainThreadDispatcher.Enqueue(() => OnDataReceived?.Invoke(message.Data));
                }
            }
            catch (Exception ex)
            {
                if (isRunning)
                    Debug.LogError($"接收数据失败: {ex.Message}");
            }
        }
    }

    // 发送数据 (通用接口)
    public void SendData(object data)
    {
        string jsonData = JsonConvert.SerializeObject(data);
        var message = new NetworkMessage
        {
            SenderId = isServer ? 0 : clientId,
            Data = jsonData
        };

        if (isServer)
            BroadcastToClients(message, 0);
        else
            SendToServer(message);
    }

    // 发送到服务器
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
            Debug.LogError($"发送失败: {ex.Message}");
        }
    }

    // 发送到指定客户端
    private void SendToClient(uint clientId, NetworkMessage message)
    {
        if (clients.ContainsKey(clientId))
        {
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
    }

    // 广播到所有客户端
    private void BroadcastToClients(NetworkMessage message, uint excludeClientId)
    {
        foreach (var kvp in clients)
        {
            if (kvp.Key != excludeClientId)
                SendToClient(kvp.Key, message);
        }
    }

    // 获取客户端ID
    private uint GetClientId(IPEndPoint endPoint)
    {
        foreach (var kvp in clients)
        {
            if (kvp.Value.Equals(endPoint))
                return kvp.Key;
        }
        return 0;
    }

    private void OnDestroy()
    {
        isRunning = false;
        networkThread?.Join(1000);
        udpClient?.Close();
        Debug.Log("网络已断开");
    }

    // 运行时修改配置
    public void SetConfig(bool asServer, string ip, int networkPort)
    {
        isServer = asServer;
        serverIP = ip;
        port = networkPort;
    }
}

// 主线程调度器
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
    }

    private void Update()
    {
        lock (executionQueue)
        {
            while (executionQueue.Count > 0)
                executionQueue.Dequeue().Invoke();
        }
    }

    public static void Enqueue(Action action)
    {
        lock (executionQueue)
        {
            executionQueue.Enqueue(action);
        }
    }
}