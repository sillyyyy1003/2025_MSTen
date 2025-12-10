using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameData;
using GamePieces;
using GameData.UI;
using System;

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


    [Header("StatusBar Elements")]
    public Button ReligionIcon;                      // 宗教图标
    public TextMeshProUGUI resourcesValue;          // 资源值
    public TextMeshProUGUI activateMissionaryValue; // 传教士激活数
    public TextMeshProUGUI activateSoliderValue;    // 士兵激活数
    public TextMeshProUGUI activateFarmerValue;     // 农民激活数
    public TextMeshProUGUI allUnitValue;       // 当前人口 / 人口上限
	public Button EndTurn;         // 结束Button
    public GameObject TurnMessageObj;//回合开始提示件
    public TextMeshProUGUI TurnMessageText;//回合开始提示内容
    public RectTransform ReligionInfoPanel;         // 宗教信息和科技树

    [Header("ReligionInfo Elements")]
    public Image ReligionInfoIcon;
    public Image ReligionColor;
    public TextMeshProUGUI ReligionName;
    public TextMeshProUGUI ReligionType;
    public TextMeshProUGUI Population;       // 当前人口 / 人口上限
    public TextMeshProUGUI FarmerValue;     // 农民激活数
    public TextMeshProUGUI SoliderValue;    // 士兵激活数
    public TextMeshProUGUI MissionaryValue; // 传教士激活数
    public TextMeshProUGUI ReligionDescribe01;
    public TextMeshProUGUI ReligionDescribe02;
    public TextMeshProUGUI ReligionDescribe03;

    [Header("SimplePanel Elements")]
    public Image PopeIcon;
    public Image MissionaryIcon;
    public Image SoliderIcon;
    public Image FarmerIcon;
    public Image BuildingIcon;





    [Header("Timer")]
	public Image TimeImage;     // TimeImage
	public TextMeshProUGUI timeText; // 可选数字显示

	//public TextMeshProUGUI unusedUnitValue;    // 未使用单位数
	
	//public RectTransform playerIconParent;
	//public Image miniMap;
	//public Button InactiveUnit;
	//public TextMeshProUGUI CountdownTime;
	
    
   

    [Header("Script")]
    //时间
    public Timer timer;


    // === Event 定义区域 ===
    //public event System.Action<CardType,int,int> OnCardDataUpdate;//种类，激活数，牌山数
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

        ReligionIcon.onClick.AddListener(() => HandleReligionIconClick());
        EndTurn.onClick.AddListener(() => HandleEndTurnButtonPressed());
        EndTurn.interactable = false;

        ReligionInfoPanel.gameObject.SetActive(false);  // 初始默认常隐

        //InactiveUnit.onClick.AddListener(() => HandleInactiveUnitButtonPressed());


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
            case CardType.Soldier:
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
            case CardType.Soldier:
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
            case CardType.Soldier:
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

            case CardType.Soldier:
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

            case CardType.Soldier:
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
        //2025.11.29 UI的更新在OnGameStarted里进行
        //Initialize();

        Debug.Log($" 回合开始：玩家 {localPlayerId}");

        StartTimer();

        DisableInput(false);

        StartCoroutine(ShowTurnMessageForOneSecond(false));

        UpdatePlayerIconsData();
        UpdateResourcesData();
        UpdatePopulationData(); // 更新人口显示


		UpdateUIUnitDataListFromInterface(CardType.Missionary);
        UpdateUIUnitDataListFromInterface(CardType.Soldier);
        UpdateUIUnitDataListFromInterface(CardType.Farmer);
        UpdateUIUnitDataListFromInterface(CardType.Building);
        UpdateUIUnitDataListFromInterface(CardType.Pope);

    }

    public void TurnEnd()
    {
        Debug.Log($" 回合结束：玩家 {localPlayerId}");

        StopTimer();

        DisableInput(true);

        StartCoroutine(ShowTurnMessageForOneSecond(true));

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

        //RefreshPlayerIcons();

    }


    public void UpdateTimer()
    {
		//float turnTime = timer.GetTurnTime();
		//float poolTime = timer.GetTimePool();
		//bool usingPool = timer.IsUsingTimePool();

		//// 更新显示
		//string turnTimeStr = FormatTime(turnTime);
		//string poolTimeStr = FormatTime(poolTime);

		//if (usingPool)
		//{
		//    CountdownTime.text = $"<color=orange>0:00</color>+{poolTimeStr}";
		//}
		//else
		//{
		//    CountdownTime.text = $"{turnTimeStr}+{poolTimeStr}";
		//}


		float turnTime = timer.GetTurnTime();
		float poolTime = timer.GetTimePool();
		bool usingPool = timer.IsUsingTimePool();

		// 1. 总时间进度占比
		float totalTime = turnTime + poolTime;
		float maxTotalTime = timer.turnTimeLimit + timer.timePoolInitial;
		float fill = Mathf.Clamp01(totalTime / maxTotalTime);

		// 填充
		TimeImage.fillAmount = fill;

		// 2. 按当前使用 turn/pool 切换颜色
		if (!usingPool)
			TimeImage.color = Color.white;
		else
			TimeImage.color = Color.red;

		// 显示数字（可选）
		if (timeText != null)
		{
			string turnStr = FormatTime(turnTime);
			string poolStr = FormatTime(poolTime);

			if (usingPool)
				timeText.text = $"<color=orange>0:00</color> + {poolStr}";
			else
				timeText.text = $"{turnStr} + {poolStr}";
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
    public void Initialize()
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
        SetReligionInfo(playerReligion);
        UpdatePopulationData(); // 更新人口显示
		UpdateResourcesData();

        UpdatePlayerIconsData();

        DeckMissionaryCount = 0;
        DeckSoliderCount = 0;
        DeckFarmerCount = 0;
        DeckBuildingCount = 0;

        UpdateUIUnitDataListFromInterface(CardType.Missionary);
        UpdateUIUnitDataListFromInterface(CardType.Soldier);
        UpdateUIUnitDataListFromInterface(CardType.Farmer);
        UpdateUIUnitDataListFromInterface(CardType.Building);
        UpdateUIUnitDataListFromInterface(CardType.Pope);

        // 时间条显示为1
        TimeImage.fillAmount = 1f;

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
            // 25.12.8 ri add error test 
            try
            {
                switch (type)
                {
                    case CardType.Missionary:

                        MissionaryUnits = uiList;
                        return;
                    case CardType.Soldier:
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
            catch(Exception e)
            {
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
                MissionaryValue.text = ActivateMissionaryCount.ToString();
                return;
            case CardType.Soldier:
                ActivateSoliderCount = count;
                activateSoliderValue.text = ActivateSoliderCount.ToString();
                SoliderValue.text = ActivateSoliderCount.ToString();
                return;
            case CardType.Farmer:
                ActivateFarmerCount = count;
                activateFarmerValue.text = ActivateFarmerCount.ToString();
                FarmerValue.text = ActivateFarmerCount.ToString();
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
            case CardType.Soldier:
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
        ReligionIcon.image.sprite = UISpriteHelper.Instance.GetIconByReligion(religion);
        
    }

    public void SetReligionInfo(Religion religion)
    {
        ReligionInfoIcon.sprite = UISpriteHelper.Instance.GetIconByReligion(religion);

        switch (religion)
        {
            case Religion.SilkReligion://丝织教
                ReligionColor.color = new Color32(128, 0, 128, 255);
                ReligionName.text = "？？教";
                ReligionType.text = "？？？？型部族";
                ReligionDescribe01.text = "・？？？が多い\n" +
                    "・？？？の？？成功率が高い\n" +
                    "・？？？の？？？が低い\n" +
                    "・全体HP強化のコストが？？\n\n\n" +
                    "・教皇周囲バフ：啓蒙者の？？？アップ\n" +
                    "・？？の？？：周囲6マスの味方を回復\n";
                ReligionDescribe02.text = "被動：\n" +
                     "？？？が12名に達すると、味方全員が残HPの20%を即時回復。";
                ReligionDescribe03.text = "現在累積：0/??";
                break;
            case Religion.RedMoonReligion://红月教
                ReligionColor.color = new Color32(128, 0, 128, 255);
                ReligionName.text = "紅月教";
                ReligionType.text = "人海戦術型部族";
                ReligionDescribe01.text = "・総人口が多い\n" +
                    "・啓蒙者の洗脳成功率が高い\n" +
                    "・守護者の攻撃力が低い\n" +
                    "・全体HP強化のコストが安い\n\n\n" +
                    "・教皇周囲バフ：啓蒙者の魅了率アップ\n" +
                    "・信徒の献祭：周囲6マスの味方を回復\n";
                ReligionDescribe02.text = "被動：\n" +
                     "戦死者が12名に達すると、味方全員が残HPの20%を即時回復";
                ReligionDescribe03.text = "現在累積：0/12";
                break;

            case Religion.MayaReligion://星界教团
                ReligionColor.color = new Color32(0, 255, 55, 255);
                ReligionName.text = "NCG_1300 星界教団";
                ReligionType.text = "戦闘特化型部族";
                ReligionDescribe01.text = "・洗脳成功率が大幅に低下\n" +
                    "・守衛者の攻撃力が高く、行動力も多い\n" +
                    "・守衛者の維持費が最高\n" +
                    "・行動力関連の強化コストが安い\n\n\n" +
                    "・教皇周囲バフ：味方のHP上昇量アップ\n" +
                    "・信徒の献祭回復：周囲6マスの味方を回復\n";
                ReligionDescribe02.text = "被動：\n" +
                     "味方駒が魅了される確率が大幅に低下";
                ReligionDescribe03.text = "";
                break;
            case Religion.MadScientistReligion://真理研究所
                ReligionColor.color = new Color32(31, 80, 255, 255);
                ReligionName.text = "真理研究所";
                ReligionType.text = "知識こそ力だと信じる部族";
                ReligionDescribe01.text = "・撃破時の獲得ポイントが多い\n" +
                    "・敵啓蒙者の魅了成功率が低い\n" +
                    "・守衛者の維持費が高い\n" +
                    "・行動力関連の強化コストが安い\n\n\n" +
                    "・教皇周囲バフ：なし\n" +
                    "・信徒の献祭回復：なし\n";
                ReligionDescribe02.text = "被動：\n" +
                     "自陣の魔煙中に啓蒙が発動すると、ランダムで1体が復活し、10ポイント獲得する";
                ReligionDescribe03.text = "冷卻時間：8／10回合";
                break;
            default://默认
                ReligionColor.color = new Color32(128, 0, 128, 255);
                ReligionName.text = "？？教";
                ReligionType.text = "？？？？型部族";
                ReligionDescribe01.text = "・？？？が多い\n" +
                    "・？？？の？？成功率が高い\n" +
                    "・？？？の？？？が低い\n" +
                    "・全体HP強化のコストが？？\n\n\n" +
                    "・教皇周囲バフ：啓蒙者の？？？アップ\n" +
                    "・？？の？？：周囲6マスの味方を回復\n";
                ReligionDescribe02.text = "被動：\n" +
                     "？？？が12名に達すると、味方全員が残HPの20%を即時回復。";
                ReligionDescribe03.text = "現在累積：0/??";
                break;
        }

        PopeIcon.sprite = UISpriteHelper.Instance.GetReligionPieceIcon(PieceType.Pope, religion);
        MissionaryIcon.sprite = UISpriteHelper.Instance.GetReligionPieceIcon(PieceType.Missionary, religion);
        SoliderIcon.sprite = UISpriteHelper.Instance.GetReligionPieceIcon(PieceType.Military, religion);
        FarmerIcon.sprite = UISpriteHelper.Instance.GetReligionPieceIcon(PieceType.Farmer, religion);
        BuildingIcon.sprite = UISpriteHelper.Instance.GetReligionPieceIcon(PieceType.Building, religion);

    }

    /*
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
    */

    public void UpdateResourcesData()
    {
        Resources = PlayerDataManager.Instance.GetPlayerResource();
        resourcesValue.text = Resources.ToString();
		Debug.Log("[GameUIManager] 更新资源数据" + Resources + "/" + resourcesValue.text);
	}



    private void UpdatePopulationData()
    {
        int nowPopulation = PlayerDataManager.Instance.NowPopulation;
        int maxPopulation = PlayerDataManager.Instance.PopulationCost;

        AllUnitCount = nowPopulation;
        AllUnitCountLimit = maxPopulation;
        InactiveUnitCount = maxPopulation- nowPopulation;



        allUnitValue.text = $"{nowPopulation}/{maxPopulation}";
        Population.text = $"{maxPopulation}";
    }




    private string FormatTime(float timeInSeconds)
    {
		//int minutes = Mathf.FloorToInt(timeInSeconds / 60);  // 计算分钟数
		//int seconds = Mathf.FloorToInt(timeInSeconds % 60);  // 计算秒数
		//return $"{minutes}:{seconds:D2}";  // 返回格式化字符串
	
		int seconds = Mathf.FloorToInt(timeInSeconds % 60);  // 计算秒数
		return $"{seconds:D2}";
	}
    private void StartTimer()
    {
        timer.StartTurn();
        //CountdownTime.gameObject.SetActive(true);
    }
    private void StopTimer()
    {
        timer.StopTimer();
        //CountdownTime.gameObject.SetActive(false);
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
        UpdatePopulationData();

		// 2025.11.14 Guoning 音声再生
		SoundManager.Instance.PlaySE(SoundSystem.TYPE_SE.SPAWNUNIT);

    }

    private void HandleCardDragCreated(CardType type)
    {
        UpdateUIUnitDataListFromInterface(type);
        UpdateResourcesData();
        UpdatePopulationData();

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

		// 更新单位数据列表
		UpdateUIUnitDataListFromInterface(CardType.Missionary);
        UpdateUIUnitDataListFromInterface(CardType.Soldier);
        UpdateUIUnitDataListFromInterface(CardType.Farmer);
        UpdateUIUnitDataListFromInterface(CardType.Building);
        UpdateUIUnitDataListFromInterface(CardType.Pope);

    }

    private void HandleReligionIconClick()
    {
        // 只有在游戏进程中时才有效
        if(GameManage.Instance.GetIsGamingOrNot())
            ReligionInfoPanel.gameObject.SetActive(true);

	}

    private IEnumerator ShowTurnMessageForOneSecond(bool tf)
    {
        if(tf)
        {
            TurnMessageText.text = "Enemy\n" + "Turn";
            
        }
        else
        {
            TurnMessageText.text = "Your\n" + "Turn";
        }
        TurnMessageObj.SetActive(true);
        yield return new WaitForSeconds(1f);
        TurnMessageObj.SetActive(false);
    }


}







