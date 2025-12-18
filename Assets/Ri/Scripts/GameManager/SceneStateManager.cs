using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using UnityEngine;
using GameData;
using System.Net.NetworkInformation;


// 场景状态管理，需要切换场景保存参数时使用
public class SceneStateManager : MonoBehaviour
{
    // 单例
    public static SceneStateManager Instance;

    // 玩家名
    public string PlayerName;
    // 玩家ip
    public string PlayerIP;
    // 玩家id
    public int PlayerID;

    // 玩家单位上限，根据宗教不同来制定
    public int PlayerUnitLimit = 20;

    // 玩家选择的宗教
    public Religion PlayerReligion;

    // 是否为主机
    private bool bIsServer;

    // 是否为单机模式
    public bool bIsSingle = false;

    // 是否为单机模式
    public bool bIsDirectConnect = false;

    // 随机生成的地图编号
    public int mapSerialNumber = -1;  // 如果不是服务器则地图编码为-1

	// 设置本地保存玩家名数据
	private const string PLAYER_NAME_KEY = "PlayerName";
    private const string DEFAULT_NAME = "玩家1";
    private const string SERVER_IP_KEY = "ServerIP";
    private const string CLIENT_IP_KEY = "ClientIP";

    // 设置本地保存服务器与客户端网络ip，需提前在电脑设置
    private const string SERVER_IP = "192.168.1.100";
    private const string CLIENT_IP = "192.168.1.101";


    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 场景切换时不销毁
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 设置是否为服务器启动
    public void SetAsServer(bool isServer)
    {
        if (isServer)
        {
            // 暂时将设置玩家名放在这
            SavePlayerName(PlayerName);
            PlayerIP = GetLocalIPv4();

            // 2025.11.17 Guoning 如果是服务器 则生成地图序列编号
            ChooseRandomMap();

        }
        else
        {
            SavePlayerName("MisumiUika");
            PlayerIP = GetLocalIPv4();
        }

        bIsServer = isServer;


        // 2025.11.17 Guoning 随机选择宗教
        // 2025.11.17 RI 现有宗教单位数不足以随机，暂时注释
        ChooseRandomReligion();
        //if (isServer)
        //{
        //    PlayerReligion = Religion.MadScientistReligion;
        //}
    }

	public bool GetIsServer()
    {
        return bIsServer;
    }

    // 加载玩家名
    public void LoadPlayerName()
    {
        if (PlayerPrefs.HasKey(PLAYER_NAME_KEY))
        {
            PlayerName = PlayerPrefs.GetString(PLAYER_NAME_KEY);
            Debug.Log("Load Player Name: " + PlayerName);
        }
        else
        {
            PlayerName = DEFAULT_NAME;
            Debug.Log("First time use default name");
        }
    }

    // 保存玩家名
    public void SavePlayerName(string name)
    {
        PlayerName = name;
        PlayerPrefs.SetString(PLAYER_NAME_KEY, name);
        //Debug.Log("Save PlayerName: " + name);


        PlayerPrefs.SetString(SERVER_IP_KEY,SERVER_IP);
        PlayerPrefs.SetString(CLIENT_IP_KEY, CLIENT_IP);

        // 保存ip设置
        PlayerPrefs.Save(); // 立即保存到磁盘

    }

    string GetLocalIPv4()
    {
        string localIP = "";

        // 获取所有网络接口
        NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();

        foreach (NetworkInterface ni in interfaces)
        {
            // 跳过非活动、回环和虚拟网卡
            if (ni.OperationalStatus != OperationalStatus.Up)
                continue;
            if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                continue;
            // 跳过VMware等虚拟网卡
            if (ni.Description.Contains("VMware") ||
                ni.Description.Contains("VirtualBox") ||
                ni.Description.Contains("Hyper-V") ||
                ni.Name.Contains("VMnet"))
                continue;

            // 获取该网卡的IP信息
            IPInterfaceProperties ipProps = ni.GetIPProperties();
            foreach (UnicastIPAddressInformation ip in ipProps.UnicastAddresses)
            {
                // 只要IPv4地址
                if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = ip.Address.ToString();
                    Debug.Log($"找到网卡: {ni.Name} ({ni.Description}) - IP: {localIP}");
                    return localIP; // 找到第一个物理网卡就返回
                }
            }
        }
        Debug.Log("ip is "+ string.IsNullOrEmpty(localIP));
        return string.IsNullOrEmpty(localIP) ? "未找到局域网 IPv4 地址" : localIP;
    }

    /// <summary>
    /// 随机选择宗教
    /// </summary>
    private void ChooseRandomReligion()
    {
        // 生成一个随机值（目前为1~4 之后扩展到1~8）
        int religion = Random.Range(1, 5);
        PlayerReligion = (Religion)religion;

    }

    /// <summary>
    /// 随机选择地图
    /// </summary>
    private void ChooseRandomMap()
    {
		// map 1001~1010
		mapSerialNumber = Random.Range(1001, 1011);
        //mapSerialNumber  = 1009;
    }

    public void StartSingleGameWithRandomMapAndReligion()
    {
        ChooseRandomReligion();
        ChooseRandomTutorialMap();
	}

    private void ChooseRandomTutorialMap()
    {
        // tutorial map 2001~2003
        mapSerialNumber = Random.Range(2001, 2003);
	}

}
