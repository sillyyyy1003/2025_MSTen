using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using UnityEngine;

// ��ҵ�λ���ݽӿڣ������ⲿ�����Ի�ȡ��Ҫ����
public class PlayerUnitDataInterface : MonoBehaviour
{  
    
    // ����
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
    // ********�ڲ����ݴ���*********
    // *****************************



    // *****************************
    // **********�ӿڲ���***********
    // *****************************

    /// <summary>
    /// �õ�ĳ�����ӵ����ϳ���key�б�
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public List<int> GetUnitDListByType(CardType type)
    {
        return PlayerDataManager.Instance.GetActivateUnitKey(type);
    }

    // �õ�һ�����ӵ�����
    public void GetUnitData(int id)
    {

    }

    // �õ������Ѿ��ϳ��ĵ�λ����
    public int GetAllActivatedUnitCount()
    {
        return PlayerDataManager.Instance.GetActivateUnitCount(true);
    }


    // �õ��ض����͵�λ�������Ѿ��ϳ��ĵ�λ����
    public int GetUnitCountByType(CardType type)
    {
        return PlayerDataManager.Instance.GetActivateUnitKey(type).Count ;
    }

    // �õ��ض����͵�λ������δ�ϳ��ĵ�λ����
    public int GetDeckNumByType(CardType type)
    {
        return PlayerDataManager.Instance.GetUnActivateUnitCount(type);
    }

    // �õ���δ�ж�����������
    public int GetInactiveUnitCount()
    {
        int count = 1;
        return count;
    }

    // �õ������׷�ٵ�����id
    public int GetFocusedUnitID()
    {
        return PlayerDataManager.Instance.nowChooseUnitID;
    }

    // �õ���Դ����
    public int GetResourceNum()
    {
        return PlayerDataManager.Instance.GetPlayerResource();
    }

    // ����ĳ�ֵ�λ
    public void AddDeckNumByType(CardType type)
    {

    }

    // ��һ����λ�ϳ�
    public void ActivateUnitFromDeck(int id)
    {
        
    }

    // ĳ����ʹ�ü���
    public void UseCardSkill(int id)
    {

    }

    // ����ĳ�����ӵ�ĳһ������
    public void UpgradeCard(CardType type)
    {

    }

}
