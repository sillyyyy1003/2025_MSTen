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
using System.Threading;
using Unity.VisualScripting;
using GameData.UI;

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

public class GameUIManager : MonoBehaviour
{
    //Data
    private bool isInitialize = false;

    private int localPlayerId;              // 本地玩家ID

    private Religion playerReligion;        // 宗教
    private int Resources;                  // 资源
    private int AllUnitCount;               // 当前总人口
    private int AllUnitCountLimit;          // 总人口上限
    private int ActivateMissionaryCount;    // 传教士激活数
    private int ActivateSoliderCount;       // 士兵激活数
    private int ActivateFarmerCount;        // 农民激活数
    private int ActivateBuildingCount;      // 建筑激活数
    private int InactiveUnitCount;            // 行动的激活单位数


    private int DeckMissionaryCount;        // 传教士卡组数量
    private int DeckSoliderCount;           // 士兵卡组数量
    private int DeckFarmerCount;            // 农民卡组数量
    private int DeckBuildingCount;          // 建筑卡组数量


    // 当前使用中的单位轻量数据集
    private List<UIUnitData> MissionaryUnits;
    private List<UIUnitData> SoliderUnits;
    private List<UIUnitData> FarmerUnits;
    private List<UIUnitData> BuildingUnits;
    private UIUnitData PopeUnitData;

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

    public GameObject playerIconPrefab;
    public RectTransform playerIconParent;
    public Image miniMap;

    public Button EndTurn;
    public Button InactiveUnit;
    public TextMeshProUGUI CountdownTime;

    [Header("Script")]
    //时间
    public Timer timer;


    // === Event 定义区域 ===
    public event System.Action<CardType,int,int> OnCardDataUpdate;//种类，激活数，牌山数
    public event System.Action TimeIsOut;//时间结束
    public event System.Action OnEndTurnButtonPressed;//回合结束按钮按下



    public static GameUIManager Instance { get; private set; }

    private void Awake()
    {

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        //DontDestroyOnLoad(gameObject);
        isInitialize = false;
    }



    // Start is called before the first frame update
    void Start()
    {
        timer = GetComponent<Timer>();

        if (ButtonMenuManager.Instance != null)
        {
            ButtonMenuManager.Instance.OnCardTypeSelected += HandleCardTypeSelected;
            ButtonMenuManager.Instance.OnCardPurchasedIntoDeck += HandleCardPurchasedIntoDeck;
            ButtonMenuManager.Instance.OnCardPurchasedIntoMap += HandleCardPurchasedIntoMap;
            ButtonMenuManager.Instance.OnCardSkillUsed += HandleCardSkillUsed;
            ButtonMenuManager.Instance.OnTechUpdated += HandleTechUpdated;
        }

        if (UnitCardManager.Instance != null)
        {
            UnitCardManager.Instance.OnCardDragCreated += HandleCardDragCreated;
        }

        if (timer != null)
        {
            timer.OnTimeOut += HandleTimeIsOut;
            timer.OnTimePoolStarted += () => Debug.Log("开始使用倒计时池");
        }

        EndTurn.onClick.AddListener(() => HandleEndTurnButtonPressed());

        InactiveUnit.onClick.AddListener(() => HandleInactiveUnitButtonPressed());


        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnPlayerDataChanged += HandlePlayerDataChanged;
        }

        MissionaryUnits = new List<UIUnitData>();
        SoliderUnits = new List<UIUnitData>();
        FarmerUnits = new List<UIUnitData>();
        BuildingUnits = new List<UIUnitData>();

        // Pope 单位默认值
        PopeUnitData = new UIUnitData
        {
            UnitId = -1,
            UnitType = CardType.None,
            HP = 0,
            AP = 0
        };

    }





    // Update is called once per frame
    void Update()
    {





    }

    private void OnDestroy()
    {
        // 取消订阅事件
        if (ButtonMenuManager.Instance != null)
        {
            ButtonMenuManager.Instance.OnCardTypeSelected -= HandleCardTypeSelected;
            ButtonMenuManager.Instance.OnCardPurchasedIntoDeck -= HandleCardPurchasedIntoDeck;
            ButtonMenuManager.Instance.OnCardPurchasedIntoMap -= HandleCardPurchasedIntoMap;
            ButtonMenuManager.Instance.OnCardSkillUsed -= HandleCardSkillUsed;
            ButtonMenuManager.Instance.OnTechUpdated -= HandleTechUpdated;
        }
        if (UnitCardManager.Instance != null)
        {
            UnitCardManager.Instance.OnCardDragCreated -= HandleCardDragCreated;
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

    public UIUnitData GetPopeUnitData()
    {

        return PopeUnitData;

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

    public void TurnStart()
    {
        Initialize();

        Debug.Log($" 回合开始：玩家 {localPlayerId}");

        StartTimer();

        DisableInput(false);

        UpdatePlayerIconsData();
        UpdateResourcesData();
        UpdatePopulationData(); // 更新人口显示
		//UpdateAllUnitCountData();

		UpdateUIUnitDataListFromInterface(CardType.Missionary);
        UpdateUIUnitDataListFromInterface(CardType.Solider);
        UpdateUIUnitDataListFromInterface(CardType.Farmer);
        UpdateUIUnitDataListFromInterface(CardType.Building);
        UpdateUIUnitDataListFromInterface(CardType.Pope);

    }

    public void TurnEnd()
    {
        Debug.Log($" 回合结束：玩家 {localPlayerId}");

        StopTimer();

        DisableInput(true);

        UpdatePlayerIconsData();

    }

    public void UpdatePlayerIconsData()
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
                isAlive = src.PlayerUnits.Count != 0,
                isOperating = src.PlayerID == currentplayerid,
                religion = src.PlayerReligion,

            };

            allPlayersData.Add(src.PlayerID, uiData);

        }

        RefreshPlayerIcons();

    }


    public void UpdateTimer()
    {
        float turnTime = timer.GetTurnTime();
        float poolTime = timer.GetTimePool();
        bool usingPool = timer.IsUsingTimePool();

        // 更新显示
        string turnTimeStr = FormatTime(turnTime);
        string poolTimeStr = FormatTime(poolTime);

        if (usingPool)
        {
            CountdownTime.text = $"<color=orange>0:00</color>+{poolTimeStr}";
        }
        else
        {
            CountdownTime.text = $"{turnTimeStr}+{poolTimeStr}";
        }


    }
    public void SetCountdownTime(int time)
    {
        timer.SetTurnTimeLimit(time);
    }
    public void SetCountdownTimePool(int time)
    {
        timer.SetTimePoolInitial(time);
    }

    public void DisableInput(bool tf)
    {
        if (tf)
        {
            EndTurn.interactable = false;


        }
        else
        {
            EndTurn.interactable = true;


        }



    }

    public Religion GetPlayerReligion()
    {

        return playerReligion;

    }



    // === 内部更新 ===
    private void Initialize()
    {
        if (isInitialize) return;

        MissionaryUnits = new List<UIUnitData>();
        SoliderUnits = new List<UIUnitData>();
        FarmerUnits = new List<UIUnitData>();
        BuildingUnits = new List<UIUnitData>();
        allPlayersData = new Dictionary<int, UIPlayerData>();


        PlayerDataManager.Instance.SetPlayerResourses(32);

        localPlayerId = GameManage.Instance.LocalPlayerID;
        playerReligion = PlayerDataManager.Instance.GetPlayerData(localPlayerId).PlayerReligion;

        SetPlayerReligionIcon(playerReligion);
        UpdateAllUnitCountData();
        UpdatePopulationData(); // 更新人口显示
		UpdateResourcesData();

        UpdatePlayerIconsData();

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
        //25.11.11 RI change for  building bug
        if (type != CardType.Building)
        {
            ClearUIUnitDataList(type);

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
                });
            }

            UpdateActivateUnitCount(type, uiList.Count);
            //Debug.Log($"[GameUIManager] UnitType = {type} UnitIDs.Count = {UnitIDs.Count}");

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
    }

    private void UpdateActivateUnitCount(CardType type, int count)
    {

        switch (type)
        {
            case CardType.Missionary:

                ActivateMissionaryCount = count;
                activateMissionaryValue.text = ActivateMissionaryCount.ToString();
                return;
            case CardType.Solider:
                ActivateSoliderCount = count;
                activateSoliderValue.text = ActivateSoliderCount.ToString();
                return;
            case CardType.Farmer:
                ActivateFarmerCount = count;
                activateFarmerValue.text = ActivateFarmerCount.ToString();
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

    private void SetPlayerReligionIcon(Religion religion)
    {
        religionIcon.sprite = UISpriteHelper.Instance.GetIconByReligion(religion);
    }

    private void RefreshPlayerIcons()
    {
        foreach (Transform child in playerIconParent)
        {
            Destroy(child.gameObject);
        }

        foreach (var kv in allPlayersData)
        {
            GameObject iconObj = Instantiate(playerIconPrefab, playerIconParent);
            iconObj.GetComponent<PlayerIcon>().Setup(kv.Value);
        }


    }


    private void UpdateResourcesData()
    {
        Resources = PlayerDataManager.Instance.GetPlayerResource();
        resourcesValue.text = Resources.ToString();
		Debug.Log("[GameUIManager] 更新资源数据" + Resources + "/" + resourcesValue.text);
	}


    private void UpdateAllUnitCountData()
    {
        AllUnitCount = PlayerUnitDataInterface.Instance.GetAllActivatedUnitCount();
        AllUnitCountLimit = PlayerUnitDataInterface.Instance.GetUnitCountLimit();
        InactiveUnitCount = PlayerUnitDataInterface.Instance.GetInactiveUnitCount();

        allUnitValue.text = $"{AllUnitCount}/{AllUnitCountLimit}";
        unusedUnitValue.text = InactiveUnitCount.ToString();
    }

    private void UpdatePopulationData()
    {
        int nowPopulation = PlayerDataManager.Instance.NowPopulation;
        int maxPopulation = PlayerDataManager.Instance.PopulationCost;

        allUnitValue.text = $"{nowPopulation}/{maxPopulation}";
	}




    private string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60);  // 计算分钟数
        int seconds = Mathf.FloorToInt(timeInSeconds % 60);  // 计算秒数
        return $"{minutes}:{seconds:D2}";  // 返回格式化字符串
    }
    private void StartTimer()
    {
        timer.StartTurn();
        CountdownTime.gameObject.SetActive(true);
    }
    private void StopTimer()
    {
        timer.StopTimer();
        CountdownTime.gameObject.SetActive(false);
    }

    // === 回调函数 ===

    private void HandleCardTypeSelected(CardType type)
    {
        Debug.Log($"选中了卡类型: {type}");
        UpdateUIUnitDataListFromInterface(type);

    }

    private void HandleCardPurchasedIntoDeck(CardType type)
    {
        Debug.Log($"购买卡进仓库: {type}");
        UpdateResourcesData();
		
        // 2025.11.14 Guoning 音声再生
		SoundManager.Instance.PlaySE(SoundSystem.TYPE_SE.BUYCARD);
	}

    private void HandleCardPurchasedIntoMap(CardType type)
    {
        Debug.Log($"购买卡进场上: {type}");
        UpdateUIUnitDataListFromInterface(type);
        UpdateResourcesData();
        UpdateAllUnitCountData();

		// 2025.11.14 Guoning 音声再生
		SoundManager.Instance.PlaySE(SoundSystem.TYPE_SE.SPAWNUNIT);

    }

    private void HandleCardDragCreated(CardType type)
    {
        UpdateUIUnitDataListFromInterface(type);
        UpdateResourcesData();
        UpdateAllUnitCountData();

		// 2025.11.14 Guoning 音声再生
		SoundManager.Instance.PlaySE(SoundSystem.TYPE_SE.SPAWNUNIT);
	}


    private void HandleTimeIsOut()
    {
        TimeIsOut?.Invoke();
    }

    private void HandleEndTurnButtonPressed()
    {

        OnEndTurnButtonPressed?.Invoke();

    }

    private void HandleCardSkillUsed(CardType card,CardSkill cardSkill)
    {

        UpdateUIUnitDataListFromInterface(card);

    }

    private void HandleTechUpdated(CardType card,TechTree tech)
    {

        UpdateUIUnitDataListFromInterface(card);

		// 2025.11.14 Guoning
		SoundManager.Instance.PlaySE(SoundSystem.TYPE_SE.UPGRADE);

	}

    private void HandleInactiveUnitButtonPressed()
    {
        if (InactiveUnitCount == 0) return;

    }

    private void HandlePlayerDataChanged(int id,PlayerData player)
    {
        Debug.Log($"玩家数据更新: 玩家 {id}");
		UpdatePlayerIconsData();
        UpdateResourcesData();  // 更新资源显示
        UpdatePopulationData(); // 更新人口显示
		//UpdateAllUnitCountData();

		// 更新单位数据列表
		UpdateUIUnitDataListFromInterface(CardType.Missionary);
        UpdateUIUnitDataListFromInterface(CardType.Solider);
        UpdateUIUnitDataListFromInterface(CardType.Farmer);
        UpdateUIUnitDataListFromInterface(CardType.Building);
        UpdateUIUnitDataListFromInterface(CardType.Pope);

    }

}







