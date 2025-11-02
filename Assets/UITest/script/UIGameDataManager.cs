using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameData;
using Unity.Mathematics;
using GamePieces;

[System.Serializable]
public struct UIUnitData
{
    // 单位的Id
    public int UnitId;
    // 单位的种类
    public CardType UnitType;
    // 单位的2维坐标
    public int2 Position;
    // HP
    public int HP;
    // AP
    public int AP;

}

[System.Serializable]
public struct UIPlayerData
{
    // 玩家ID
    public int PlayerID;
    // 玩家头像（未知存储类型）
    public int avatarSpriteId;
    // 玩家是否存活
    public bool isAlive;
    // 玩家是否正在操作
    public bool isOperating;
    // 玩家宗教
    public Religion religion;

}

public class UIGameDataManager : MonoBehaviour
{
    public int localPlayerId;              // 本地玩家ID

    public Religion playerReligion;        // 宗教
    public int Resources;                  // 资源
    public int AllUnitCount;               // 当前总人口
    public int AllUnitCountLimit;          // 总人口上限
    public int ActivateMissionaryCount;    // 传教士激活数
    public int ActivateSoliderCount;       // 士兵激活数
    public int ActivateFarmerCount;        // 农民激活数
    public int ActivateBuildingCount;      // 建筑激活数
    public int UnusedUnitCount;            // 未使用的单位数

    public int DeckMissionaryCount;        // 传教士卡组数量
    public int DeckSoliderCount;           // 士兵卡组数量
    public int DeckFarmerCount;            // 农民卡组数量
    public int DeckBuildingCount;          // 建筑卡组数量


    // 当前使用中的单位轻量数据集
    public List<UIUnitData> MissionaryUnits;
    public List<UIUnitData> SoliderUnits;
    public List<UIUnitData> FarmerUnits;
    public List<UIUnitData> BuildingUnits;

    // 全部玩家轻量数据
    private Dictionary<int, UIPlayerData> allPlayersData;


    [Header("UI Elements")]
    public Image religionIcon;                 // 宗教图标
    public TextMeshProUGUI resourcesValue;     // 资源值
    public TextMeshProUGUI activateMissionaryValue; // 传教士激活数
    public TextMeshProUGUI activateSoliderValue;    // 士兵激活数
    public TextMeshProUGUI activateFarmerValue;     // 农民激活数
    public TextMeshProUGUI allUnitValue;       // 当前人口 / 人口上限
    public TextMeshProUGUI unusedUnitValue;    // 未使用单位数

    public Image player01_Icon;
    public Image player02_Icon;
    public TextMeshProUGUI player01_State;
    public TextMeshProUGUI player02_State;
    public Image miniMap;

    public Button EndTurn;
    public TextMeshProUGUI CountdownTime;



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


    private void Initialize()
    {

        localPlayerId = GameManage.Instance.LocalPlayerID;

        Dictionary<int, PlayerData> datalist = PlayerDataManager.Instance.GetAllPlayersData();
        allPlayersData = new Dictionary<int, UIPlayerData>();

        foreach (var kv in datalist)
        {
            PlayerData src = kv.Value;

            UIPlayerData uiData = new UIPlayerData
            {
                PlayerID = src.PlayerID,
                avatarSpriteId = 0,
                isAlive = true,
                isOperating = false,
                religion = src.PlayerReligion,

            };


            if (src.PlayerID == localPlayerId)
            {
                playerReligion = src.PlayerReligion;
            }

        }

        
        Resources = PlayerUnitDataInterface.Instance.GetResourceNum();
        AllUnitCount = PlayerUnitDataInterface.Instance.GetAllActivatedUnitCount();
        AllUnitCountLimit = 100;
        ActivateMissionaryCount = PlayerUnitDataInterface.Instance.GetUnitCountByType(CardType.Missionary);
        ActivateSoliderCount = PlayerUnitDataInterface.Instance.GetUnitCountByType(CardType.Solider);
        ActivateFarmerCount = PlayerUnitDataInterface.Instance.GetUnitCountByType(CardType.Farmer);
        ActivateBuildingCount= PlayerUnitDataInterface.Instance.GetUnitCountByType(CardType.Building);
        UnusedUnitCount = PlayerUnitDataInterface.Instance.GetInactiveUnitCount();

        DeckMissionaryCount = PlayerUnitDataInterface.Instance.GetDeckNumByType(CardType.Missionary);
        DeckSoliderCount = PlayerUnitDataInterface.Instance.GetDeckNumByType(CardType.Solider);
        DeckFarmerCount = PlayerUnitDataInterface.Instance.GetDeckNumByType(CardType.Farmer);
        DeckBuildingCount = PlayerUnitDataInterface.Instance.GetDeckNumByType(CardType.Building);


        MissionaryUnits = GetUIUnitDataList(CardType.Missionary);
        SoliderUnits = GetUIUnitDataList(CardType.Solider);
        FarmerUnits = GetUIUnitDataList(CardType.Farmer);
        BuildingUnits = GetUIUnitDataList(CardType.Building);



    }


    public static List<UIUnitData> GetUIUnitDataList(CardType type)
    {
        List<int> UnitIDs = PlayerUnitDataInterface.Instance.GetUnitIDListByType(type);
        List<UIUnitData> uiList = new List<UIUnitData>();

        foreach (int id in UnitIDs)
        {
            Piece unitData = PlayerUnitDataInterface.Instance.GetUnitData(id);

            uiList.Add(new UIUnitData
            {
                UnitId = id,
                UnitType = type,
                HP = (int)unitData.CurrentHP,
                AP = (int)unitData.CurrentAP,
            }) ; 
        }

        return uiList;

    }








}







