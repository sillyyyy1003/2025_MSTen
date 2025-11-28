using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using UnityEngine;
using GamePieces;
using GameData;
using GameData.UI;
using Unity.Mathematics;
using TMPro;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Linq;


#if UNITY_EDITOR
using Mono.Cecil.Cil;
#endif
using System.Runtime.Versioning;


// 玩家单位数据接口，负责被外部调用以获取需要数据
public class PlayerUnitDataInterface : MonoBehaviour
{

    // 单例
    public static PlayerUnitDataInterface Instance { get; private set; }
    private int EnemyID;
    private int2 EnemyUnitPos;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
			//2025.11.17 Guoning
			//DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

  
    }

    void Start()
    {
        if (GameManage.Instance._PlayerOperation != null)
        {
            GameManage.Instance._PlayerOperation.OnUnitChoosed += OnUnitChoosed;

        }



    }

    // *****************************
    // ********内部数据处理*********
    // *****************************

    private void OnUnitChoosed(int unitid, CardType unittype)
    {



        if (ButtonMenuManager.Instance.GetCardTypeChoosed() != unittype)
        {
            ButtonMenuManager.Instance.SetCardTypeChoosed(unittype);
            string nextMenuId = ButtonMenuFactory.GetMenuId(GameData.UI.MenuLevel.Second, unittype);
            ButtonMenuManager.Instance.LoadMenu(nextMenuId);
        }

        UnitCardManager.Instance.SetTargetCardType(unittype);
        UnitCardManager.Instance.SetTargetUnitId(unitid);


    }



    // 拿到点击的敌方单位id
    private void GetEmemyUnitID(int unitid)
    {
        EnemyID=unitid;
    }
    // *****************************
    // **********接口部分***********
    // *****************************

    // 获取创建某种宗教的某类棋子所需要的资源值
    public int GetCreateUnitResoursesCost(Religion religion,CardType type)
    {
        if (type == CardType.Building) 
        {
            return GameManage.Instance._BuildingManager.GetBuildingDataByReligion(religion).buildingResourceCost;
		}

		return PieceManager.Instance.GetPieceResourceCost(ConvertCardTypeToPieceType(type), religion);

	}
    // 根据行动类型 获得某种行动所需消耗的行动力
    public int GetUnitOperationCostByType(OperationType type)
    {
        return PieceManager.Instance.GetUnitOperationCostByType(PlayerDataManager.Instance.nowChooseUnitID,type);
    }

    // 拿到教皇移动冷却
    public int2 GetPopeSwapCooldown()
    {
        return PieceManager.Instance.GetPopeSwapCooldown(GameManage.Instance.LocalPlayerID);
    }
    /// <summary>
    /// 拿到某种棋子的已上场的key列表
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public List<int> GetUnitIDListByType(CardType type)
    {
        return PlayerDataManager.Instance.GetActivateUnitKey(type);
    }

    // 拿到一个棋子的数据
    public Piece GetUnitData(int id)
    {
        //PlayerDataManager.Instance.GetUnitDataById(id).Value.PlayerUnitDataSO

        return PieceManager.Instance.GetPiece(id);

    }

    // 拿到所有已经上场的单位数量
    public int GetAllActivatedUnitCount()
    {

        return PlayerDataManager.Instance.GetActivateUnitCount(false);
    }


    // 设置摄像机追踪的棋子id --> 追加
    public void SetFocusedUnitID(int id)
    {



    }
    // 拿到玩家的可用棋子上限数量 --> 追加
    public int GetUnitCountLimit()
    {
        return SceneStateManager.Instance.PlayerUnitLimit;
    }

    // 拿到特定类型单位的所有已经上场的单位数量
    public int GetUnitCountByType(CardType type)
    {

        return PlayerDataManager.Instance.GetActivateUnitKey(type).Count ;
    }


    // 设置敌方棋子的位置 --> 追加
    public void SetEnemyUnitPosition(int2 pos)
    {
        EnemyUnitPos=pos;
    }

    // 拿到敌方棋子的位置 --> 追加
    public Vector3 GetEnemyUnitPosition(int id)
    {
        return PlayerDataManager.Instance.GetUnitPos(id);
    }


    // 拿到特定类型单位的所有未上场的单位数量
    public int GetDeckNumByType(CardType type)
    {

        return PlayerDataManager.Instance.GetUnActivateUnitCount(type);
    }

    // 拿到尚未行动的棋子数量
    public int GetInactiveUnitCount()
    {
        return PlayerDataManager.Instance.GetUnitCanUse();
    }

    // 拿到摄像机追踪的棋子id
    public int GetFocusedUnitID()
    {
        return PlayerDataManager.Instance.nowChooseUnitID;
    }

    // 拿到资源数量
    public int GetResourceNum()
    {
        return PlayerDataManager.Instance.GetPlayerResource();
    }

    // 购买某种单位
    public bool AddDeckNumByType(CardType type)
    {


        int ResourcesCost = PlayerDataManager.Instance.GetCreateUnitResoursesCost(type);

        int ResourcesCount = PlayerDataManager.Instance.GetPlayerResource();
        if (ResourcesCount < ResourcesCost)
        {
            Debug.LogWarning("资源不足!");
            return false;
        }

        // 尝试创建单位
        if (GameUIManager.Instance.AddDeckNumByType(type))
        {

            ResourcesCount -= ResourcesCost;
            PlayerDataManager.Instance.SetPlayerResourses(ResourcesCount);
            return true;

        }


        return false;
    }

    // 将一个单位上场
    public bool ActivateUnitFromDeck(CardType type)
    {

        if (GameUIManager.Instance.GetUIDeckNum(type) <= 0)
        {
            Debug.LogWarning("仓库内无卡牌！");
            return false;
        }

        // 尝试创建单位
        if (GameManage.Instance._PlayerOperation.TryCreateUnit(type))
        {

            GameUIManager.Instance.ActivateDeckCardByType(type);
            return true;

        }
        else
        {
            Debug.LogWarning("创建失败 - 请先选择一个空格子");
            return false;
        }

    }

    // 某棋子使用技能
    public bool UseCardSkill(int id,CardSkill skill)
    {

        switch (skill)
        {
            case CardSkill.Occupy://占領 Missionary
                return true;
            case CardSkill.Conversion://魅惑 Missionary
                return true;
            case CardSkill.NormalAttack://一般攻撃 Military
                return true;
            case CardSkill.SpecialAttack://特殊攻撃 Military
                return true;
            case CardSkill.EnterBuilding://建物に入る Farmer Building
                return true;
            case CardSkill.Construction://建物を建築 Building
                return true;
            case CardSkill.Sacrifice://献祭 AP消費し他駒を回復するスキル Farmer
                return true;
            case CardSkill.SwapPosition://味方駒と位置を交換する Pope
                return true;
            default:
                return false;

        }


    }

    // 获得某种棋子的某一项属性
    public int GetTechTreeLevel(TechTree tech, CardType type)
    {

        switch (tech)
        {
            case TechTree.HP:

                return 1;
            case TechTree.AP:
                return 1;
            case TechTree.Occupy:
                return 1;
            case TechTree.Conversion:
                return 1;
            case TechTree.ATK:
                return 1;
            case TechTree.Sacrifice:
                return 1;
            case TechTree.AttackPosition:
                return 1;
            case TechTree.AltarCount:
                return 1;
            case TechTree.ConstructionCost:
                return 1;
            case TechTree.MovementCD:
                return 1;
            case TechTree.Buff:
                return 1;
            case TechTree.Heresy:
                return 1;
            default:
                return 1;
        }
    }


    // 升级某种棋子的某一项属性
    public bool UpgradeCard(CardType type,TechTree tech)
    {

        Religion playerReligion = GameUIManager.Instance.GetPlayerReligion();

        switch (tech)
        {
            case TechTree.HP:
                return GameManage.Instance._PlayerOperation.UnitUpgrade(TechTree.HP, type);
            case TechTree.AP:
                return GameManage.Instance._PlayerOperation.UnitUpgrade(TechTree.AP, type);
            case TechTree.Occupy:
                return GameManage.Instance._PlayerOperation.UnitUpgrade(TechTree.Occupy, type);
            case TechTree.Conversion:
                return GameManage.Instance._PlayerOperation.UnitUpgrade(TechTree.Conversion, type);
            case TechTree.ATK:

                return GameManage.Instance._PlayerOperation.UnitUpgrade(TechTree.ATK, type);
            case TechTree.Sacrifice:

                return GameManage.Instance._PlayerOperation.UnitUpgrade(TechTree.Sacrifice, type);
            case TechTree.AttackPosition:

                return GameManage.Instance._PlayerOperation.UnitUpgrade(TechTree.AttackPosition, type);
            case TechTree.AltarCount:

                return GameManage.Instance._PlayerOperation.UnitUpgrade(TechTree.AltarCount, type);
            case TechTree.ConstructionCost:

                return GameManage.Instance._PlayerOperation.UnitUpgrade(TechTree.ConstructionCost, type);
            case TechTree.MovementCD:

                return GameManage.Instance._PlayerOperation.UnitUpgrade(TechTree.MovementCD, type);
            case TechTree.Buff:

                return GameManage.Instance._PlayerOperation.UnitUpgrade(TechTree.Buff, type);
            case TechTree.Heresy:

                return GameManage.Instance._PlayerOperation.UnitUpgrade(TechTree.Heresy, type);
            default:
                return false;
        }


    }

    public int GetCurrentPopulation()
    {
        return PlayerDataManager.Instance.NowPopulation;
    }
    public int GetCurrentUnitPopulationCostByType(CardType type)
    {

        return PieceManager.Instance.GetPiecePopulationCost(ConvertCardTypeToPieceType(type), SceneStateManager.Instance.PlayerReligion);

    }
        // 购买某种棋子直接生成到地图
    public bool BuyUnitToMapByType(CardType type)
    {
		// 检查资源是否足够
		int ResourcesCost = PlayerDataManager.Instance.GetCreateUnitResoursesCost(type);
        int ResourcesCount = PlayerDataManager.Instance.GetPlayerResource();
        if (ResourcesCount < ResourcesCost)
        {
            Debug.LogWarning("资源不足!");
            return false;
        }


        // 检查人口是否足够
		// 当前玩家人口上限
		int currentPlayerPopulationTotal = PlayerDataManager.Instance.PopulationCost;
		// 当前玩家已用人口
		int currentPlayerPopulationUsed = PlayerDataManager.Instance.NowPopulation;
		// 棋子所需要的人口
		int currentUnitPopulationCost = 0;
		if (type != CardType.Building)
			currentUnitPopulationCost = PieceManager.Instance.GetPiecePopulationCost(ConvertCardTypeToPieceType(type), SceneStateManager.Instance.PlayerReligion);

		if (currentUnitPopulationCost > currentPlayerPopulationTotal - currentPlayerPopulationUsed)
        {
            Debug.LogWarning("人口不足!");
            return false;
        }
        else
        {
            // 尝试创建单位
            if (GameManage.Instance._PlayerOperation.TryCreateUnit(type))
            {
                ResourcesCount -= ResourcesCost;
                PlayerDataManager.Instance.SetPlayerResourses(ResourcesCount);
                return true;
            }
            else
            {

				Debug.LogWarning("创建失败 - 请先选择一个空格子");
				return false;
			}

		}
    }


	/// <summary>
	/// 购买某种棋子直接生成到地图检测资源是否足够、人口是否足够
	/// </summary>
	/// <param name="type"></param>
	/// <returns></returns>
	public bool TryBuyUnitToMapByType(CardType type)
    {
		// 检查资源是否足够
		Religion playerReligion = SceneStateManager.Instance.PlayerReligion;
        int ResourcesCost = 0;
        if (type == CardType.Building)
        {
            ResourcesCost = GameManage.Instance._BuildingManager.GetBuildingDataByReligion(playerReligion).buildingResourceCost;
        }
        else
        {
			ResourcesCost = PlayerDataManager.Instance.GetCreateUnitResoursesCost(type);
        }
  

        int ResourcesCount = PlayerDataManager.Instance.GetPlayerResource();
		if (ResourcesCount < ResourcesCost)
		{
			Debug.LogWarning("资源不足!");
			return false;
		}

		// 检查人口是否足够
		// 当前玩家人口上限
		int currentPlayerPopulationTotal = PlayerDataManager.Instance.PopulationCost;
		// 当前玩家已用人口
		int currentPlayerPopulationUsed = PlayerDataManager.Instance.NowPopulation;
		// 棋子所需要的人口
		int currentUnitPopulationCost = 0;
		if (type != CardType.Building)
			currentUnitPopulationCost = PieceManager.Instance.GetPiecePopulationCost(ConvertCardTypeToPieceType(type), SceneStateManager.Instance.PlayerReligion);

		if (currentUnitPopulationCost > currentPlayerPopulationTotal - currentPlayerPopulationUsed)
		{
			Debug.LogWarning("人口不足!");
			return false;
		}
		else
		{
			// 尝试创建单位
			if (GameManage.Instance._PlayerOperation.TryCreateUnit(type))
			{
				ResourcesCount -= ResourcesCost;
				PlayerDataManager.Instance.SetPlayerResourses(ResourcesCount);
				return true;
			}
			else
			{
				Debug.LogWarning("创建失败 - 请先选择一个空格子");
				return false;
			}
		}
	}


	// 将 CardType 转换为 PieceType
	public PieceType ConvertCardTypeToPieceType(CardType cardType)
    {
        switch (cardType)
        {
            case CardType.Farmer: return PieceType.Farmer;
            case CardType.Solider: return PieceType.Military;
            case CardType.Missionary: return PieceType.Missionary;
            case CardType.Pope: return PieceType.Pope;
            case CardType.Building: return PieceType.Building;
            default:
                Debug.LogError($"未知的 CardType: {cardType}");
                return PieceType.None;
        }
    }

    // 将 PieceType 转换为 CardType
    public CardType ConvertPieceTypeToCardType(PieceType pieceType)
    {
        switch (pieceType)
        {
            case PieceType.Farmer: return CardType.Farmer;
            case PieceType.Military: return CardType.Solider;
            case PieceType.Missionary: return CardType.Missionary;
            case PieceType.Pope: return CardType.Pope;
            default:
                Debug.LogError($"未知的 PieceType: {pieceType}");
                return CardType.Farmer; // 默认返回Farmer
        }
    }
}
