using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameSceneUIManager : MonoBehaviour
{
    // ����
    public static GameSceneUIManager Instance { get; private set; }

    // ��ť

    // ��������ʿ
    private Button EndTurn;

    // ����ũ��
    private Button CreateFramer;

    // ����ʿ��
    private Button CreateSoldier;

    // ��������ʿ
    private Button CreateMissionary;

    // ����ʱ��ʾ
    private TextMeshProUGUI CountdownTime;

    // ��Դ��ʾ
    private TextMeshProUGUI Resources;

    // ��Դ��
    private int ResourcesCount=100;

    private float CountdownTimeCount=100;

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
        // ��ť�¼���ʼ��
        EndTurn = GameObject.Find("EndTurn").GetComponent<Button>();
        CreateFramer = GameObject.Find("CreateFramer").GetComponent<Button>();
        CreateSoldier = GameObject.Find("CreateSoldier").GetComponent<Button>();
        CreateMissionary = GameObject.Find("CreateMissionary").GetComponent<Button>();
        CountdownTime = GameObject.Find("Time").GetComponent<TextMeshProUGUI>();
        Resources = GameObject.Find("Resources").GetComponent<TextMeshProUGUI>();


        EndTurn.onClick.AddListener(() => OnEndTurnButtonPressed());
        CreateFramer.onClick.AddListener(() => OnCreateFramerButtonPressed());
        CreateSoldier.onClick.AddListener(() => OnCreateSoldierButtonPressed());
        CreateMissionary.onClick.AddListener(() => OnCreateMissionaryButtonPressed());

        SetResourcesCount(ResourcesCount);
    }
    void Update()
    {
        if(bIsPlayerTurn)
        {
            CountdownTimeCount -= Time.deltaTime;
            int seconds = Mathf.CeilToInt(CountdownTimeCount);
            CountdownTime.text ="Time:"+ seconds.ToString()+" s";
            if (seconds <= 30)
                CountdownTime.color = Color.yellow;
            if(seconds<=10)
                CountdownTime.color = Color.red;
        }
    }

    public void GetCountdownTime(int time)
    {
        CountdownTimeCount = time;
    }
    private void OnEndTurnButtonPressed()
    {

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
    private void SetCountdownCount(int time)
    {
        CountdownTimeCount=time;
    }
    private void ShowNotEnough()
    {
    }
}
