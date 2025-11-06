using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using UnityEngine;
using GamePieces;
using GameData;
using GameData.UI;
using Unity.Mathematics;
using TMPro;


// 玩家单位数据接口，负责被外部调用以获取需要数据
public class PlayerUnitDataInterface : MonoBehaviour
{
    public PlayerOperationManager _PlayerOpManager;

    // 单例
    public static PlayerUnitDataInterface Instance { get; private set; }
    private int EnemyID;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

  
    }

    void Start()
    {
        if (_PlayerOpManager!=null)
        {
            _PlayerOpManager.OnUnitChoosed += OnUnitChoosed;

        }

    }

    // *****************************
    // ********内部数据处理*********
    // *****************************

    private void OnUnitChoosed(int unitid, CardType unittype)
    {


        UnitCardManager.Instance.SetTargetCardType(unittype);
        UnitCardManager.Instance.SetTargetUnitId(unitid);

        if (ButtonMenuManager.Instance.GetCardTypeChoosed() != unittype)
        {
            ButtonMenuManager.Instance.SetCardTypeChoosed(unittype);
            string nextMenuId = ButtonMenuFactory.GetMenuId(GameData.UI.MenuLevel.Second, unittype);
            ButtonMenuManager.Instance.LoadMenu(nextMenuId);
        }


    }

    // 拿到点击的敌方单位id
    private void GetEmemyUnitID(int unitid)
    {
        EnemyID=unitid;
    }
        // *****************************
        // **********接口部分***********
        // *****************************

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
        //PlayerDataManager.Instance.GetUnitDataById(id).Value.PlayerUnitDataSO;


        return PieceManager.Instance.GetPiece(id);

    }

    // 拿到所有已经上场的单位数量
    public int GetAllActivatedUnitCount()
    {

        return PlayerDataManager.Instance.GetActivateUnitCount(false);
    }


    // 拿到摄像机追踪的棋子id --> 追加
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

    // 拿到敌方棋子的位置 --> 追加
    public int2 GetEnemyUnitPosition(int2 pos)
    {
        return pos;

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


        return true;
    }

    // 将一个单位上场
    public bool ActivateUnitFromDeck(CardType type)
    {




        return true;
    }

    // 某棋子使用技能
    public bool UseCardSkill(int id,CardSkill skill)
    {




        return true;
    }

    // 升级某种棋子的某一项属性
    public bool UpgradeCard(CardType type,TechTree tech)
    {




        return true;
    }

    // 购买某种棋子直接生成到地图
    public bool BuyUnitToMapByType(CardType type)
    {
        //int ResourcesCount = PlayerDataManager.Instance.GetPlayerResource();
        //if (ResourcesCount < 10)
        //{
        //    Debug.LogWarning("资源不足!");
        //    return false;
        //}

        // 尝试创建单位
        if (_PlayerOpManager.TryCreateUnit(type))
        {
            return true;

        }
        else
        {
            Debug.LogWarning("创建失败 - 请先选择一个空格子");
            return false;
        }

    }

}
