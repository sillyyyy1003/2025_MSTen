using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameData;
using Unity.Mathematics;

public class UIGameDataManager : MonoBehaviour
{

    [System.Serializable]
    public struct UIPlayerData
    {
        public int playerId;    // 玩家Id
        public int avatarSpriteId;  // 玩家头像(未知存储类型)


        public Religion religion;//宗教
        public int Resources;             //资源
        public int AllUnitCount;       // 当前总人口
        public int AllUnitCountLimit;   // 总人口上限
        public int ActivateMissionaryCount;//传教士激活数
        public int ActivateSoliderCount;//士兵激活数
        public int ActivateFarmerCount;//农民激活数
        //public int ActivateBuildingCount;//建筑激活数
        public int UnusedUnitCount;//未使用的个体数


        public int DeckMissionaryCount;//传教士牌山数
        public int DeckSoliderCount;//士兵牌山数
        public int DeckFarmerCount;//农民牌山数
        public int DeckBuildingCount;//建筑牌山数



        




    }

    [System.Serializable]
    public struct UIUnitData
    {

        // 单位的Id
        public int UnitId;
        // 单位的种类
        public PlayerUnitType UnitType;
        // 单位的2维坐标
        public int2 Position;

    }



    [Header("UI Elements")]
    public Image religionIcon;//宗教图标
    public TextMeshProUGUI resourcesValue;             //资源数
    public TextMeshProUGUI activateMissionaryValue;//传教士激活数
    public TextMeshProUGUI activateSoliderValue;//士兵激活数
    public TextMeshProUGUI activateFarmerValue;//农民激活数
    public TextMeshProUGUI allUnitValue;       // 当前总人口/人口上限
    public TextMeshProUGUI unusedUnitValue;//未使用的个体数

    public Image player01_Icon;
    public Image player02_Icon;
    public TextMeshProUGUI player01_State;
    public TextMeshProUGUI player02_State;
    public Image miniMap;

    public Button EndTurn;
    public TextMeshProUGUI CountdownTime;


    private Dictionary<int, UIPlayerData> uiPlayerDataDict = new Dictionary<int, UIPlayerData>();


    public static UIGameDataManager Instance { get; private set; }

    private void Awake()
    {

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

    }



    // Start is called before the first frame update
    void Start()
    {
        







    }





    // Update is called once per frame
    void Update()
    {
        




    }







}
