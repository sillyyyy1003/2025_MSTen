using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using UnityEngine;

// 玩家单位数据接口，负责被外部调用以获取需要数据
public class PlayerUnitDataInterface : MonoBehaviour
{  
    
    // 单例
    public static PlayerUnitDataInterface Instance { get; private set; }


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

    // *****************************
    // ********内部数据处理*********
    // *****************************



    // *****************************
    // **********接口部分***********
    // *****************************

    // 拿到某种棋子的已上场的key列表
    public List<int> GetUnitDListByType(CardType type)
    {
        return PlayerDataManager.Instance.GetActivateUnitKey(type);
    }

    // 拿到一个棋子的数据
    public void GetUnitData(int id)
    {

    }

    // 拿到所有已经上场的单位数量
    public int GetAllActivatedUnitCount()
    {
        return PlayerDataManager.Instance.GetActivateUnitCount(true);
    }


    // 拿到特定类型单位的所有已经上场的单位数量
    public int GetUnitCountByType(CardType type)
    {
        return PlayerDataManager.Instance.GetActivateUnitKey(type).Count ;
    }

    // 拿到特定类型单位的所有未上场的单位数量
    public int GetDeckNumByType(CardType type)
    {
        return PlayerDataManager.Instance.GetUnActivateUnitCount(type);
    }

    // 拿到尚未行动的棋子数量
    public int GetInactiveUnitCount()
    {
        int count = 1;
        return count;
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
    public void AddDeckNumByType(CardType type)
    {

    }

    // 将一个单位上场
    public void ActivateUnitFromDeck(int id)
    {
        
    }

    // 某棋子使用技能
    public void UseCardSkill(int id)
    {

    }

    // 升级某种棋子的某一项属性
    public void UpgradeCard(CardType type)
    {

    }

}
