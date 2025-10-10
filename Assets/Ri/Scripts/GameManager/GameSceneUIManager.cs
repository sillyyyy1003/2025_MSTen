using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameSceneUIManager : MonoBehaviour
{
    // 单例
    public static GameSceneUIManager Instance { get; private set; }

    public PlayerOperationManager _PlayerOpManager;
    // 按钮

    // 创建传教士
    private Button EndTurn;

    // 创建农民
    private Button CreateFramer;

    // 创建士兵
    private Button CreateSoldier;

    // 创建传教士
    private Button CreateMissionary;

    // 倒计时显示
    private TextMeshProUGUI CountdownTime;

    // 资源显示
    private TextMeshProUGUI Resources;

    // 回合显示
    private TextMeshProUGUI TurnText;
    public string sPlayerTurn = "Your Turn";
    public string sEnemyTurn = "Enemy's Turn";

    // 资源数
    private int ResourcesCount=100;

    private float CountdownTimeCount=100;
    private float CountdownTimePoolCount = 100;

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
        Debug.Log("ui");
        // 按钮事件初始化
        EndTurn = GameObject.Find("EndTurn").GetComponent<Button>();
        CreateFramer = GameObject.Find("CreateFramer").GetComponent<Button>();
        CreateSoldier = GameObject.Find("CreateSoldier").GetComponent<Button>();
        CreateMissionary = GameObject.Find("CreateMissionary").GetComponent<Button>();
        CountdownTime = GameObject.Find("Time").GetComponent<TextMeshProUGUI>();
        Resources = GameObject.Find("Resources").GetComponent<TextMeshProUGUI>();
        TurnText= GameObject.Find("ShowWhosTurn").GetComponent<TextMeshProUGUI>();


        EndTurn.onClick.AddListener(() => OnEndTurnButtonPressed());
        CreateFramer.onClick.AddListener(() => OnCreateFramerButtonPressed());
        CreateSoldier.onClick.AddListener(() => OnCreateSoldierButtonPressed());
        CreateMissionary.onClick.AddListener(() => OnCreateMissionaryButtonPressed());

        SetResourcesCount(ResourcesCount);
        // test
        SetTurnText(true);
    }
    void Update()
    {
        if(bIsPlayerTurn)
        {
            this.GetComponent<Timer>().SetTime();


            CountdownTime.text = "Time:" + CountdownTimeCount.ToString() + " s"
                + "  TimePool:" + CountdownTimePoolCount.ToString() + " s";
            ////if (seconds <= 30)
            ////    CountdownTime.color = Color.yellow;
            ////if(seconds<=10)
            ////    CountdownTime.color = Color.red;
        }
    }
    // *************************
    //         公有函数
    // *************************

    // Timer相关
    public void StartTurn()
    {
        GetComponent<Timer>().StartTurn();
    }

    // 设置回合结束时间
    public void SetCountdownTime(int time)
    {
        CountdownTimeCount = time;
    }
    public void SetCountdownTimePool(int time)
    {
        CountdownTimePoolCount = time;
    }
    // 设置回合文本
    public void SetTurnText(bool playerTurn)
    {
        if(playerTurn)
        {
            TurnText.text = sPlayerTurn;
        }
        else
        {

            TurnText.text = sEnemyTurn;
        }
    }

    // 设置结束回合按钮是否可用(自己回合外不可用)
    public void SetEndTurn(bool canUse)
    {
            EndTurn.interactable=canUse;
    }    
    
    // 设置玩家资源
    public void SetPlayerResource(int res)
    {
        ResourcesCount = res;
    }


    // *************************;
    //         私有函数
    // *************************


    private void OnEndTurnButtonPressed()
    {
        _PlayerOpManager.TurnEnd();
    }


    private void OnCreateFramerButtonPressed()
    {
        if(ResourcesCount-10<=0)
        {
            ShowNotEnough();
        }
        else
        {
            ResourcesCount -= 10;
            SetResourcesCount(ResourcesCount);
        }
    }
    private void OnCreateSoldierButtonPressed()
    {
        if (ResourcesCount - 20 <= 0)
        {
            ShowNotEnough();
        }
        else
        {
            ResourcesCount -= 20;
            SetResourcesCount(ResourcesCount);
        }
    }
    private void OnCreateMissionaryButtonPressed()
    {
        if (ResourcesCount - 30 <= 0)
        {
            ShowNotEnough();
        }
        else
        {
            ResourcesCount -= 30;
            SetResourcesCount(ResourcesCount);
        }
    }
    private void SetResourcesCount(int count)
    {
        Resources.text = "Resources: " + count.ToString();
    }
 
    private void ShowNotEnough()
    {
    }
}
