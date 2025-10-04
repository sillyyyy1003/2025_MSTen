using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class NetworkConfig
{
    [Header("网络角色")]
    public bool isServer = false;

    [Header("IP地址设置")]
    public string serverIP = "192.168.1.100";    // 服务器电脑的IP
    public string clientIP = "192.168.1.101";    // 客户端电脑的IP

    [Header("端口设置")]
    public int gamePort = 8888;

    [Header("连接设置")]
    public bool autoConnect = false;             // 是否自动连接
    public float connectionTimeout = 10f;        // 连接超时时间
}

public class NetworkConfigManager : MonoBehaviour
{
    [Header("网络配置")]
    [SerializeField] private NetworkConfig config = new NetworkConfig();

    [Header("预设配置")]
    [SerializeField] private NetworkConfig[] presetConfigs;

    [Header("UI引用")]
    [SerializeField] private InputField serverIPInput;
    [SerializeField] private InputField clientIPInput;
    [SerializeField] private InputField portInput;
    [SerializeField] private Toggle serverToggle;
    [SerializeField] private Button applyConfigButton;
    [SerializeField] private Dropdown configPresetDropdown;
    [SerializeField] private Text currentConfigText;

    private NetGameSystem netSystem;

    private void Start()
    {
        netSystem = GameObject.Find("NetManager").GetComponent<NetGameSystem>();

        // 初始化预设配置
        InitializePresetConfigs();

        // 绑定UI事件
        SetupUI();

        // 应用当前配置
        ApplyConfig();

        // 更新UI显示
        UpdateUI();
    }

    private void InitializePresetConfigs()
    {
        if (presetConfigs == null || presetConfigs.Length == 0)
        {
            presetConfigs = new NetworkConfig[]
            {
                new NetworkConfig
                {
                    serverIP = "192.168.1.100",
                    clientIP = "192.168.1.101",
                    gamePort = 8888,
                    isServer = true
                },
                new NetworkConfig
                {
                    serverIP = "192.168.1.100",
                    clientIP = "192.168.1.101",
                    gamePort = 8888,
                    isServer = false
                },
                new NetworkConfig
                {
                    serverIP = "10.0.0.100",
                    clientIP = "10.0.0.101",
                    gamePort = 8888,
                    isServer = true
                },
                new NetworkConfig
                {
                    serverIP = "192.168.0.100",
                    clientIP = "192.168.0.101",
                    gamePort = 8888,
                    isServer = false
                }
            };
        }
    }

    private void SetupUI()
    {
        if (applyConfigButton != null)
            applyConfigButton.onClick.AddListener(ApplyUIConfig);

        if (configPresetDropdown != null)
        {
            configPresetDropdown.options.Clear();
            configPresetDropdown.options.Add(new Dropdown.OptionData("自定义配置"));

            for (int i = 0; i < presetConfigs.Length; i++)
            {
                string optionText = $"{(presetConfigs[i].isServer ? "服务器" : "客户端")} - {presetConfigs[i].serverIP}:{presetConfigs[i].gamePort}";
                configPresetDropdown.options.Add(new Dropdown.OptionData(optionText));
            }

            configPresetDropdown.onValueChanged.AddListener(OnPresetChanged);
        }
    }

    private void OnPresetChanged(int index)
    {
        if (index > 0 && index - 1 < presetConfigs.Length)
        {
            config = presetConfigs[index - 1];
            UpdateUI();
            ApplyConfig();
        }
    }

    private void UpdateUI()
    {
        if (serverIPInput != null)
            serverIPInput.text = config.serverIP;

        if (clientIPInput != null)
            clientIPInput.text = config.clientIP;

        if (portInput != null)
            portInput.text = config.gamePort.ToString();

        if (serverToggle != null)
            serverToggle.isOn = config.isServer;

        UpdateCurrentConfigDisplay();
    }

    private void ApplyUIConfig()
    {
        // 从UI获取配置
        if (serverIPInput != null)
            config.serverIP = serverIPInput.text;

        if (clientIPInput != null)
            config.clientIP = clientIPInput.text;

        if (portInput != null && int.TryParse(portInput.text, out int port))
            config.gamePort = port;

        if (serverToggle != null)
            config.isServer = serverToggle.isOn;

        ApplyConfig();
    }

    public void ApplyConfig()
    {
        if (netSystem != null)
        {
            netSystem.SetServerIPAndPort(config.serverIP, config.gamePort);

            // 根据配置设置角色
            if (config.isServer)
                netSystem.SetAsServer();
            else
                netSystem.SetAsClient();
        }

        UpdateCurrentConfigDisplay();
        Debug.Log($"已应用网络配置: {(config.isServer ? "服务器" : "客户端")} - {config.serverIP}:{config.gamePort}");
    }

    private void UpdateCurrentConfigDisplay()
    {
        if (currentConfigText != null)
        {
            string roleText = config.isServer ? "服务器" : "客户端";
            string configText = $"当前配置:\n角色: {roleText}\n服务器IP: {config.serverIP}\n端口: {config.gamePort}";

            if (!config.isServer)
                configText += $"\n本机IP: {config.clientIP}";

            currentConfigText.text = configText;
        }
    }

    // 便捷设置方法
    [ContextMenu("设置为服务器配置")]
    public void SetServerConfig()
    {
        config.isServer = true;
        ApplyConfig();
        UpdateUI();
    }

    [ContextMenu("设置为客户端配置")]
    public void SetClientConfig()
    {
        config.isServer = false;
        ApplyConfig();
        UpdateUI();
    }

    // 网线直连的典型配置
    [ContextMenu("应用网线直连配置")]
    public void ApplyDirectConnectionConfig()
    {
        // 典型的网线直连配置
        config.serverIP = "192.168.1.100";
        config.clientIP = "192.168.1.101";
        config.gamePort = 8888;

        ApplyConfig();
        UpdateUI();

        Debug.Log("已应用网线直连配置");
        Debug.Log("请确保:");
        Debug.Log("1. 服务器电脑IP设置为: 192.168.1.100");
        Debug.Log("2. 客户端电脑IP设置为: 192.168.1.101");
        Debug.Log("3. 两台电脑子网掩码都是: 255.255.255.0");
    }

    // 保存和加载配置
    public void SaveConfig()
    {
        string configJson = JsonUtility.ToJson(config, true);
        PlayerPrefs.SetString("NetworkConfig", configJson);
        Debug.Log("网络配置已保存");
    }

    public void LoadConfig()
    {
        if (PlayerPrefs.HasKey("NetworkConfig"))
        {
            string configJson = PlayerPrefs.GetString("NetworkConfig");
            config = JsonUtility.FromJson<NetworkConfig>(configJson);
            ApplyConfig();
            UpdateUI();
            Debug.Log("网络配置已加载");
        }
    }

    // 网络诊断工具
    [ContextMenu("测试网络连通性")]
    public void TestNetworkConnectivity()
    {
        Debug.Log("=== 网络连通性测试 ===");
        Debug.Log($"当前配置: {(config.isServer ? "服务器" : "客户端")}");
        Debug.Log($"目标IP: {config.serverIP}");
        Debug.Log($"端口: {config.gamePort}");
        Debug.Log("请检查:");
        Debug.Log("1. 两台电脑是否正确连接网线");
        Debug.Log("2. IP地址是否正确设置");
        Debug.Log("3. 防火墙是否允许该端口通信");
    }
}