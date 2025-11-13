using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static UnityEditor.Progress;

public class GameSceneUIManager : MonoBehaviour
{
    // 单例
    public static GameSceneUIManager Instance { get; private set; }

    public PlayerOperationManager _PlayerOpManager;

    public GameObject GameUIObject;
    public GameObject NetRoomUIObject;

    // 房间UI组件
    [Header("房间UI组件")]
    //public Transform PlayerListContainer; // 玩家列表容器
    public GameObject PlayerItemPrefab; // 玩家列表项预制体
    public Button Button_ReadyAndStartGame; // 准备按钮
    //public Button Button_StartGame; // 开始游戏按钮
    //public TextMeshProUGUI Text_RoomInfo; // 房间信息文本
    //public TextMeshProUGUI Text_LocalPlayerInfo; // 本地玩家信息
                                              
    //public Toggle Toggle_Ready; // 准备状态Toggle

    // 玩家列表项字典
    private Dictionary<uint, GameObject> playerListItems = new Dictionary<uint, GameObject>();

    private List<Vector2> PlayerInforListPos=new List<Vector2> { 
        new Vector2(0,50), 
        new Vector2(0, 0), 
        new Vector2(0, -50) };
    //private int PlayerCount = 0;


    private bool bIsPlayerTurn=true; 
    


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

    }


    // Start is called before the first frame update
    void Start()
    {



        // 初始化房间UI
        InitializeRoomUI();
        // 初始化游戏界面UI
        InitializeGameUI();

        if (SceneStateManager.Instance.bIsSingle)
        {
            GameUIObject.SetActive(true);
            NetRoomUIObject.SetActive(false);
        }
        else
        {
            GameUIObject.SetActive(false);
            NetRoomUIObject.SetActive(true);
        }




    }
    void Update()
    {
        if(bIsPlayerTurn)
        {
            GameUIManager.Instance.UpdateTimer();
        }
        else
        {


        }


    }

    // 初始化房间UI
    private void InitializeRoomUI()
    {
        // 如果使用按钮
        if (Button_ReadyAndStartGame != null)
        {
            Button_ReadyAndStartGame.onClick.AddListener(OnReadyButtonClicked);
        }

        // 订阅网络事件
        if (NetGameSystem.Instance != null)
        {
            NetGameSystem.Instance.OnRoomStatusUpdated += UpdateRoomDisplay;
            NetGameSystem.Instance.OnAllPlayersReady += OnAllPlayersReadyChanged;
            NetGameSystem.Instance.OnGameStarted += OnGameStarted;
        }
    }

    private void InitializeGameUI()
    {

        // 订阅UI事件
        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.OnEndTurnButtonPressed += OnEndTurnButtonPressed;
            GameUIManager.Instance.TimeIsOut += TimeIsOut;
        }

    }

    // 显示本地玩家信息
    private void DisplayLocalPlayerInfo()
    {
        if (SceneStateManager.Instance != null)
        {
            string playerName = SceneStateManager.Instance.PlayerName;
            string playerIP = playerIP = SceneStateManager.Instance.PlayerIP;
            bool isServer = SceneStateManager.Instance.GetIsServer();

            //Text_LocalPlayerInfo.text = $"玩家名: {playerName}\nIP: {playerIP}\n身份: {(isServer ? "房主" : "成员")}";
        }
    }

    // 更新房间显示 
    private void UpdateRoomDisplay(List<PlayerInfo> players)
    {
        // 清空现有列表
        foreach (var item in playerListItems.Values)
        {
            Destroy(item);
        }
        playerListItems.Clear();

        // 创建新的玩家列表项
        foreach (var player in players)
        {
            CreatePlayerListItem(player);
        }

        // 更新按钮状态（客户端）
        if (!NetGameSystem.Instance.bIsServer)
        {
            UpdateClientButtonState();
        }
    }

    // 创建玩家列表项 
    private void CreatePlayerListItem(PlayerInfo player)
    {
        // 使用预制体创建玩家列表项
        GameObject item = Instantiate(PlayerItemPrefab, NetRoomUIObject.transform);
        item.GetComponent<RectTransform>().anchoredPosition = PlayerInforListPos[(int)player.PlayerId];
       
        playerListItems[player.PlayerId] = item;
       
        item.transform.Find("Name").GetComponent<TextMeshProUGUI>().text = player.PlayerName;
        item.transform.Find("IP").GetComponent<TextMeshProUGUI>().text = player.PlayerIP;

        // 使用player.IsReady来设置Toggle状态
        Toggle toggle = item.transform.Find("Toggle").GetComponent<Toggle>();
        if (toggle != null)
        {
            toggle.isOn = player.IsReady;  // 使用实际的准备状态
            toggle.interactable = false;    // Toggle只用于显示
        }

        if (player.PlayerId==0)
        {
            Button_ReadyAndStartGame.GetComponentInChildren<TextMeshProUGUI>().text = "WaitForPlayer";
            Button_ReadyAndStartGame.interactable = false;
        }
        else
        {
            Button_ReadyAndStartGame.GetComponentInChildren<TextMeshProUGUI>().text = "Ready";
            Button_ReadyAndStartGame.interactable =true;
        }

        // 更新显示信息
        TextMeshProUGUI nameText = item.GetComponentInChildren<TextMeshProUGUI>();
    }

    // 更新客户端按钮状态
    private void UpdateClientButtonState()
    {
        if (NetGameSystem.Instance != null && !NetGameSystem.Instance.bIsServer)
        {
            bool isReady = NetGameSystem.Instance.IsLocalReady;
            Button_ReadyAndStartGame.GetComponentInChildren<TextMeshProUGUI>().text = isReady ? "Cancel" : "Ready";
            Button_ReadyAndStartGame.interactable = true;

            Debug.Log($"[UI] 客户端按钮状态更新 - 准备: {isReady}, 按钮文字: {(isReady ? "Cancel" : "Ready")}");
        }
    }

    // 准备按钮点击事件
    private void OnReadyButtonClicked()
    {
        if (NetGameSystem.Instance != null)
        {
            // 如果是服务器并且所有玩家都准备了，则开始游戏
            if (NetGameSystem.Instance.bIsServer && Button_ReadyAndStartGame.GetComponentInChildren<TextMeshProUGUI>().text == "StartGame")
            {
                OnStartGameButtonClicked();
                return;
            }

            // 获取当前准备状态
            bool currentStatus = NetGameSystem.Instance.IsLocalReady;
            bool newStatus = !currentStatus;

            Debug.Log($"[UI] 准备按钮点击 - 当前状态: {currentStatus}, 新状态: {newStatus}");

            // 发送新的准备状态
            NetGameSystem.Instance.SetReadyStatus(newStatus);

            // 立即更新按钮文本（不等待网络回调）
            if (Button_ReadyAndStartGame != null)
            {
                TextMeshProUGUI buttonText = Button_ReadyAndStartGame.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = newStatus ? "Cancel" : "Ready";
                }
            }
        }
    }

    //// 准备Toggle改变事件
    //private void OnReadyToggleChanged(bool isReady)
    //{
    //    if (NetGameSystem.Instance != null)
    //    {
    //        NetGameSystem.Instance.SetReadyStatus(isReady);
    //    }
    //}

    // 所有玩家准备完毕回调 
    private void OnAllPlayersReadyChanged(bool allReady)
    {
        // 只有服务器玩家才能看到开始游戏按钮
        if (NetGameSystem.Instance != null && NetGameSystem.Instance.bIsServer)
        {
            //Debug.Log("All Player Ready ? " + allReady);
            if (Button_ReadyAndStartGame != null)
            {
                // 可以添加按钮可交互性控制
                Button_ReadyAndStartGame.interactable = allReady;

                // 更新按钮文本提示
                TextMeshProUGUI buttonText = Button_ReadyAndStartGame.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = allReady ? "StartGame" : "WaitForPlayer";
                }
            }
        }
    }

    // 开始游戏按钮点击事件
    private void OnStartGameButtonClicked()
    {
        if (NetGameSystem.Instance != null && NetGameSystem.Instance.bIsServer)
        {
            NetGameSystem.Instance.StartGame();
        }
    }

    // 游戏开始回调 
    private void OnGameStarted()
    {
        // 切换到游戏UI
        if (NetRoomUIObject != null)
        {
            NetRoomUIObject.SetActive(false);
        }

        if (GameUIObject != null)
        {
            GameUIObject.SetActive(true);
        }

        Debug.Log("游戏开始，切换到游戏界面");


    }

    // 清理事件订阅
    private void OnDestroy()
    {
        // 取消订阅事件
        if (NetGameSystem.Instance != null)
        {
            NetGameSystem.Instance.OnRoomStatusUpdated -= UpdateRoomDisplay;
            NetGameSystem.Instance.OnAllPlayersReady -= OnAllPlayersReadyChanged;
            NetGameSystem.Instance.OnGameStarted -= OnGameStarted;
        }
    }
    // *************************
    //         公有函数
    // *************************

    public void StartMyTurn(bool tf)
    {
        bIsPlayerTurn = tf;
        if (bIsPlayerTurn)
        {
            GameUIManager.Instance.TurnStart();
        }
        else
        {
            GameUIManager.Instance.UpdatePlayerIconsData();
        }
    }
    public void EndTurn()
    {
        bIsPlayerTurn = false;
        GameUIManager.Instance.TurnEnd();
    }



    // *************************;
    //         私有函数
    // *************************

    private void TimeIsOut()
    {
        OnEndTurnButtonPressed();
    }
    private void OnEndTurnButtonPressed()
    {
        EndTurn();
        _PlayerOpManager.TurnEnd();
    }


}
