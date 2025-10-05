using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;
using Unity.VisualScripting.FullSerializer;

// 简化的消息类型
public enum MessageType
{
    Connect,
    Disconnect,
    GameData
}

// 统一的网络消息
[System.Serializable]
public class NetworkMessage
{
    public MessageType Type;
    public uint SenderId;
    public string Data;

    public NetworkMessage()
    {
        SenderId = 0;
    }
}

// 简化的客户端
public class NetGameSystem
{
    private UdpClient udpClient;
    private IPEndPoint serverEndPoint;
    private bool isConnected = false;
    private uint clientId;
    private Thread receiveThread;
    private CancellationTokenSource cancellationToken;

    public event Action<string> OnDataReceived;
    public event Action OnConnected;
    public event Action OnDisconnected;

    public bool IsConnected => isConnected;
    public uint ClientId => clientId;

    public async Task<bool> ConnectToServer(string serverIP, int port)
    {
        try
        {
            serverEndPoint = new IPEndPoint(IPAddress.Parse(serverIP), port);
            udpClient = new UdpClient();
            cancellationToken = new CancellationTokenSource();

            var connectMsg = new NetworkMessage
            {
                Type = MessageType.Connect,
                Data = "Client_Connect"
            };

            await SendMessage(connectMsg);

            receiveThread = new Thread(ReceiveMessages) { IsBackground = true };
            receiveThread.Start();

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Connect Failed: {ex.Message}");
            return false;
        }
    }

    public async Task SendData(object data)
    {
        if (!isConnected) return;

        var message = new NetworkMessage
        {
            Type = MessageType.GameData,
            SenderId = clientId,
            Data = JsonConvert.SerializeObject(data)
        };

        await SendMessage(message);
    }

    private async Task SendMessage(NetworkMessage message)
    {
        if (udpClient == null) return;

        try
        {
            string jsonData = JsonConvert.SerializeObject(message);
            byte[] data = Encoding.UTF8.GetBytes(jsonData);
            await udpClient.SendAsync(data, data.Length, serverEndPoint);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Send Message Failed: {ex.Message}");
        }
    }

    private void ReceiveMessages()
    {
        while (!cancellationToken.Token.IsCancellationRequested && isConnected)
        {
            try
            {
                var result = udpClient.Receive(ref serverEndPoint);
                string jsonData = Encoding.UTF8.GetString(result);
                var message = JsonConvert.DeserializeObject<NetworkMessage>(jsonData);

                if (message.Type == MessageType.Connect && !isConnected)
                {
                    clientId = uint.Parse(message.Data);
                    isConnected = true;
                    MainThreadDispatcher.Enqueue(() => OnConnected?.Invoke());
                }
                else if (message.Type == MessageType.GameData)
                {
                    MainThreadDispatcher.Enqueue(() => OnDataReceived?.Invoke(message.Data));
                }
            }
            catch (Exception ex)
            {
                if (!cancellationToken.Token.IsCancellationRequested)
                    Debug.LogError($"Get Message Failed: {ex.Message}");
            }
        }
    }

    public void Disconnect()
    {
        isConnected = false;
        cancellationToken?.Cancel();
        receiveThread?.Join(1000);
        udpClient?.Close();
        MainThreadDispatcher.Enqueue(() => OnDisconnected?.Invoke());
    }

    public void SetServerIPAndPort(string ip,int port)
    {

    }
    public void SetAsServer()
    {

    }

    public void SetAsClient()
    {

    }
}

// 简化的服务器
public class SimpleNetworkServer
{
    private UdpClient udpServer;
    private Dictionary<uint, IPEndPoint> clients;
    private uint nextClientId = 1;
    private bool isRunning = false;
    private Thread serverThread;
    private CancellationTokenSource cancellationToken;

    public event Action<uint, string> OnDataReceived;
    public event Action<uint> OnClientConnected;
    public event Action<uint> OnClientDisconnected;

    public int ConnectedClients => clients.Count;

    public SimpleNetworkServer()
    {
        clients = new Dictionary<uint, IPEndPoint>();
    }

    public bool StartServer(int port)
    {
        try
        {
            udpServer = new UdpClient(port);
            cancellationToken = new CancellationTokenSource();
            isRunning = true;

            serverThread = new Thread(ServerLoop) { IsBackground = true };
            serverThread.Start();

            Debug.Log($"Server Start!  Port: {port}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Server Start Failed!  {ex.Message}");
            return false;
        }
    }

    private void ServerLoop()
    {
        while (isRunning && !cancellationToken.Token.IsCancellationRequested)
        {
            try
            {
                IPEndPoint clientEndPoint = null;
                var data = udpServer.Receive(ref clientEndPoint);
                string jsonData = Encoding.UTF8.GetString(data);
                var message = JsonConvert.DeserializeObject<NetworkMessage>(jsonData);

                MainThreadDispatcher.Enqueue(() => HandleClientMessage(message, clientEndPoint));
            }
            catch (Exception ex)
            {
                if (isRunning)
                    Debug.LogError($"Server Error : {ex.Message}");
            }
        }
    }

    private void HandleClientMessage(NetworkMessage message, IPEndPoint clientEndPoint)
    {
        if (message.Type == MessageType.Connect)
        {
            uint clientId = nextClientId++;
            clients[clientId] = clientEndPoint;

            var response = new NetworkMessage
            {
                Type = MessageType.Connect,
                Data = clientId.ToString()
            };

            SendMessageToClient(clientId, response);
            OnClientConnected?.Invoke(clientId);
            Debug.Log($"Client {clientId} Has Connected");
        }
        else if (message.Type == MessageType.GameData)
        {
            uint senderId = GetClientId(clientEndPoint);
            if (senderId > 0)
            {
                OnDataReceived?.Invoke(senderId, message.Data);
                // 转发给其他客户端
                BroadcastData(message.Data, senderId);
            }
        }
    }

    public async Task SendDataToClient(uint clientId, object data)
    {
        if (clients.ContainsKey(clientId))
        {
            var message = new NetworkMessage
            {
                Type = MessageType.GameData,
                Data = JsonConvert.SerializeObject(data)
            };

            await SendMessageToClient(clientId, message);
        }
    }

    public async Task BroadcastData(object data, uint excludeClientId = 0)
    {
        var message = new NetworkMessage
        {
            Type = MessageType.GameData,
            Data = JsonConvert.SerializeObject(data)
        };

        var tasks = new List<Task>();
        foreach (var clientId in clients.Keys)
        {
            if (clientId != excludeClientId)
            {
                tasks.Add(SendMessageToClient(clientId, message));
            }
        }
        await Task.WhenAll(tasks);
    }

    private async Task SendMessageToClient(uint clientId, NetworkMessage message)
    {
        if (clients.ContainsKey(clientId))
        {
            try
            {
                string jsonData = JsonConvert.SerializeObject(message);
                byte[] data = Encoding.UTF8.GetBytes(jsonData);
                await udpServer.SendAsync(data, data.Length, clients[clientId]);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Send Message To Client {clientId} Failed: {ex.Message}");
            }
        }
    }

    private uint GetClientId(IPEndPoint endPoint)
    {
        foreach (var kvp in clients)
        {
            if (kvp.Value.Equals(endPoint))
                return kvp.Key;
        }
        return 0;
    }

    public void StopServer()
    {
        isRunning = false;
        cancellationToken?.Cancel();
        serverThread?.Join(1000);
        udpServer?.Close();
        Debug.Log("Server Is Stopped");
    }
}

// 主线程调度器
public class MainThreadDispatcher : MonoBehaviour
{
    private static readonly Queue<Action> _executionQueue = new();

    private void Update()
    {
        lock (_executionQueue)
        {
            while (_executionQueue.Count > 0)
                _executionQueue.Dequeue().Invoke();
        }
    }

    public static void Enqueue(Action action)
    {
        lock (_executionQueue)
        {
            _executionQueue.Enqueue(action);
        }
    }
}