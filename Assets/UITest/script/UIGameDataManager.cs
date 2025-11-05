using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameData;
using Unity.Mathematics;
using GamePieces;
using Buildings;
using DG.Tweening.Core.Easing;

[System.Serializable]
public struct UIUnitData
{
    // 单位的Id
    public int UnitId;
    // 单位的种类
    public CardType UnitType;
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
    [Header("Data")]
    public bool isInitialize = false;

    public int localPlayerId;              // 本地玩家ID

    public Religion playerReligion;        // 宗教
    public int Resources;                  // 资源
    public int AllUnitCount;               // 当前总人口
    public int AllUnitCountLimit;          // 总人口上限
    public int ActivateMissionaryCount;    // 传教士激活数
    public int ActivateSoliderCount;       // 士兵激活数
    public int ActivateFarmerCount;        // 农民激活数
    public int ActivateBuildingCount;      // 建筑激活数
    public int InactiveUnitCount;            // 行动的激活单位数


    public int DeckMissionaryCount;        // 传教士卡组数量
    public int DeckSoliderCount;           // 士兵卡组数量
    public int DeckFarmerCount;            // 农民卡组数量
    public int DeckBuildingCount;          // 建筑卡组数量


    // 当前使用中的单位轻量数据集
    public List<UIUnitData> MissionaryUnits;
    public List<UIUnitData> SoliderUnits;
    public List<UIUnitData> FarmerUnits;
    public List<UIUnitData> BuildingUnits;
    public UIUnitData PopeUnitData;

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


    // === Event 定义区域 ===
    public event System.Action<CardType,int,int> OnCardDataUpdate;//种类，激活数，牌山数

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
        isInitialize = false;
    }



    // Start is called before the first frame update
    void Start()
    {

        // 订阅网络事件
        if (NetGameSystem.Instance != null)
        {

            NetGameSystem.Instance.OnGameStarted += OnGameStarted;
        }

        if (ButtonMenuManager.Instance != null)
        {
            ButtonMenuManager.Instance.OnCardTypeSelected += HandleCardTypeSelected;
            ButtonMenuManager.Instance.OnCardPurchasedIntoDeck += HandleCardPurchasedIntoDeck;
            ButtonMenuManager.Instance.OnCardPurchasedIntoMap += HandleCardPurchasedIntoMap;
        }


    }





    // Update is called once per frame
    void Update()
    {
        Initialize();


    }

    private void OnDestroy()
    {
        // 取消订阅事件
        if (NetGameSystem.Instance != null)
        {
            NetGameSystem.Instance.OnGameStarted -= OnGameStarted;
        }

        if (ButtonMenuManager.Instance != null)
        {
            ButtonMenuManager.Instance.OnCardTypeSelected -= HandleCardTypeSelected;
            ButtonMenuManager.Instance.OnCardPurchasedIntoDeck -= HandleCardPurchasedIntoDeck;
            ButtonMenuManager.Instance.OnCardPurchasedIntoMap -= HandleCardPurchasedIntoMap;
        }

        if (GameManage.Instance != null)
        {
            // 订阅事件
            GameManage.Instance.OnTurnStarted += HandleTurnStart;
            GameManage.Instance.OnTurnEnded += HandleTurnEnd;
        }

    }






    public List<UIUnitData> GetActivateUnitDataList(CardType type)
    {
        switch (type)
        {
            case CardType.Missionary:
                return MissionaryUnits;
            case CardType.Solider:
                return SoliderUnits;
            case CardType.Farmer:
                return FarmerUnits;
            case CardType.Building:
                return BuildingUnits;
            default:
                return null;
        }
    }

    public int GetActivateUnitCount(CardType type)
    {
        switch (type)
        {
            case CardType.Missionary:
                return ActivateMissionaryCount;
            case CardType.Solider:
                return ActivateSoliderCount;
            case CardType.Farmer:
                return ActivateFarmerCount;
            case CardType.Building:
                return ActivateBuildingCount;
            case CardType.Pope:
                return 1;
            default:
                return 0;

        }

    }

    public int GetUIDeckNum(CardType type)
    {

        switch (type)
        {
            case CardType.Missionary:
                return DeckMissionaryCount;
            case CardType.Solider:
                return DeckSoliderCount;
            case CardType.Farmer:
                return DeckFarmerCount;
            case CardType.Building:
                return DeckBuildingCount;
            default:
                return 0;
        }


    }

    public bool AddDeckNumByType(CardType type)
    {
        //if (Resources <= 0) return false;


        switch (type)
        {
            case CardType.Missionary:
                DeckMissionaryCount++;

                return true;

            case CardType.Solider:
                DeckSoliderCount++;

                return true;
            case CardType.Farmer:
                DeckFarmerCount++;

                return true;
            case CardType.Building:
                DeckBuildingCount++;

                return true;
            default:
                return false;
        }

    }

    public bool ActivateDeckCardByType(CardType type)
    {
        switch (type)
        {
            case CardType.Missionary:
                if (DeckMissionaryCount <= 0) return false;

                DeckMissionaryCount--;
                return true;

            case CardType.Solider:
                if (DeckSoliderCount <= 0) return false;

                DeckSoliderCount--;
                return true;
            case CardType.Farmer:
                if (DeckFarmerCount <= 0) return false;

                DeckFarmerCount--;
                return true;
            case CardType.Building:
                if (DeckBuildingCount <= 0) return false;

                DeckBuildingCount--;
                return true;
            default:

                return false;
        }

    }


    // === 内部更新 ===
    private void Initialize()
    {
        if (isInitialize) return;

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
        InactiveUnitCount = PlayerUnitDataInterface.Instance.GetInactiveUnitCount();


        DeckMissionaryCount = 0;
        DeckSoliderCount = 0;
        DeckFarmerCount = 0;
        DeckBuildingCount = 0;

        UpdateUIUnitDataListFromInterface(CardType.Missionary);
        UpdateUIUnitDataListFromInterface(CardType.Solider);
        UpdateUIUnitDataListFromInterface(CardType.Farmer);
        UpdateUIUnitDataListFromInterface(CardType.Building);
        UpdateUIUnitDataListFromInterface(CardType.Pope);

        isInitialize = true;
    }




    private void UpdateUIUnitDataListFromInterface(CardType type)
    {
        ClearUIUnitDataList(type);

        List<int> UnitIDs = PlayerUnitDataInterface.Instance.GetUnitIDListByType(type);
        List<UIUnitData> uiList = new List<UIUnitData>();

        foreach (int id in UnitIDs)
        {
            //Piece unitData = PlayerUnitDataInterface.Instance.GetUnitData(id);

            uiList.Add(new UIUnitData
            {
                UnitId = id,
                UnitType = type,
                //HP = (int)unitData.CurrentHP,
                //AP = (int)unitData.CurrentAP,
                HP = 0,
                AP = 0,
            });
        }

        SetActivateUnitCount(type, uiList.Count);
        Debug.Log($"[UIGameDataManager] UnitType = {type} UnitIDs.Count = {UnitIDs.Count}");

        switch (type)
        {
            case CardType.Missionary:

                MissionaryUnits = uiList;
                return;
            case CardType.Solider:
                SoliderUnits = uiList;

                return;
            case CardType.Farmer:
                FarmerUnits = uiList;

                return;
            case CardType.Building:

                BuildingUnits = uiList;
                return;
            case CardType.Pope:
                PopeUnitData = uiList[0];
                return;
            default:
                return;

        }

    }

    private void SetActivateUnitCount(CardType type, int count)
    {
        switch (type)
        {
            case CardType.Missionary:

                ActivateMissionaryCount = count;
                return;
            case CardType.Solider:
                ActivateSoliderCount = count;

                return;
            case CardType.Farmer:
                ActivateFarmerCount = count;
                return;
            case CardType.Building:

                ActivateBuildingCount = count;
                return;
            default:
                return;

        }

    }


    private void ClearUIUnitDataList(CardType type)
    {
        switch (type)
        {
            case CardType.Missionary:
                MissionaryUnits.Clear();
                break;
            case CardType.Solider:
                SoliderUnits.Clear();
                break;
            case CardType.Farmer:
                FarmerUnits.Clear();
                break;
            case CardType.Building:
                BuildingUnits.Clear();
                break;
            default:
                break;

        }

    }


    private void UpdateUIPlayerData()
    {
        int currentplayerid = GameManage.Instance.CurrentTurnPlayerID;

        Dictionary<int, PlayerData> datalist = PlayerDataManager.Instance.GetAllPlayersData();
        allPlayersData = new Dictionary<int, UIPlayerData>();

        foreach (var kv in datalist)
        {
            PlayerData src = kv.Value;

            UIPlayerData uiData = new UIPlayerData
            {
                PlayerID = src.PlayerID,
                avatarSpriteId = 0,
                isAlive = src.PlayerUnits.Count!=0,
                isOperating = src.PlayerID == currentplayerid,
                religion = src.PlayerReligion,

            };

        }


    }



    // === 回调函数 ===
    private void OnGameStarted()
    {
        
    }


    private void HandleCardTypeSelected(CardType type)
    {
        Debug.Log($"选中了卡类型: {type}");
        UpdateUIUnitDataListFromInterface(type);

    }

    private void HandleCardPurchasedIntoDeck(CardType type)
    {
        Debug.Log($"购买卡进仓库: {type}");
        Resources = PlayerUnitDataInterface.Instance.GetResourceNum();
    }

    private void HandleCardPurchasedIntoMap(CardType type)
    {
        Debug.Log($"购买卡进场上: {type}");
        UpdateUIUnitDataListFromInterface(type);
        Resources = PlayerUnitDataInterface.Instance.GetResourceNum();
        AllUnitCount = PlayerUnitDataInterface.Instance.GetAllActivatedUnitCount();
        AllUnitCountLimit = 100;
        InactiveUnitCount = PlayerUnitDataInterface.Instance.GetInactiveUnitCount();

    }

    private void HandleTurnStart(int playerID)
    {
        Debug.Log($"▶️ 回合开始：玩家 {playerID}");
        UpdateUIPlayerData();
        Resources = PlayerUnitDataInterface.Instance.GetResourceNum();
        AllUnitCount = PlayerUnitDataInterface.Instance.GetAllActivatedUnitCount();
        AllUnitCountLimit = 100;
        InactiveUnitCount = PlayerUnitDataInterface.Instance.GetInactiveUnitCount();
        UpdateUIUnitDataListFromInterface(CardType.Missionary);
        UpdateUIUnitDataListFromInterface(CardType.Solider);
        UpdateUIUnitDataListFromInterface(CardType.Farmer);
        UpdateUIUnitDataListFromInterface(CardType.Building);
        UpdateUIUnitDataListFromInterface(CardType.Pope);

    }

    private void HandleTurnEnd(int playerID)
    {
        Debug.Log($"⏹ 回合结束：玩家 {playerID}");
        UpdateUIPlayerData();
    }

}







