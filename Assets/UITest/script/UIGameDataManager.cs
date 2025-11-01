using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameData;
using Unity.Mathematics;


[System.Serializable]
public struct UIUnitData
{
    // ��λ��Id
    public int UnitId;
    // ��λ������
    public CardType UnitType;
    // ��λ��2ά����
    public int2 Position;
    // HP
    public int HP;
    // AP
    public int AP;

}

[System.Serializable]
public struct UIPlayerData
{
    // ��ҵ����
    public int PlayerID;
    // ���ͷ��(δ֪�洢����)
    public int avatarSpriteId;
    // ����Ƿ���
    public bool isAlive;
    // ����Ƿ����ڲ���
    public bool isOperating;
    // ����ڽ�
    public Religion religion;

}

public class UIGameDataManager : MonoBehaviour
{
    public int localPlayerId;    // ���Id

    [System.Serializable]
    public struct UIPlayerData
    {
        public int playerId;    // ���Id
        public int avatarSpriteId;  // ���ͷ��(δ֪�洢����)


        public Religion religion;//�ڽ�
        public int Resources;             //��Դ
        public int AllUnitCount;       // ��ǰ���˿�
        public int AllUnitCountLimit;   // ���˿�����
        public int ActivateMissionaryCount;//����ʿ������
        public int ActivateSoliderCount;//ʿ��������
        public int ActivateFarmerCount;//ũ�񼤻���
        //public int ActivateBuildingCount;//����������
        public int UnusedUnitCount;//δʹ�õĸ�����


        public int DeckMissionaryCount;//����ʿ��ɽ��
        public int DeckSoliderCount;//ʿ����ɽ��
        public int DeckFarmerCount;//ũ����ɽ��
        public int DeckBuildingCount;//������ɽ��



        




    }

    [System.Serializable]
    public struct UIUnitData
    {

        // ��λ��Id
        public int UnitId;
        // ��λ������
        public CardType UnitType;
        // ��λ��2ά����
        public int2 Position;

    }
    public Religion playerReligion;//�ڽ�
    public int Resources;             //��Դ
    public int AllUnitCount;       // ��ǰ���˿�
    public int AllUnitCountLimit;   // ���˿�����
    public int ActivateMissionaryCount;//����ʿ������
    public int ActivateSoliderCount;//ʿ��������
    public int ActivateFarmerCount;//ũ�񼤻���
    public int ActivateBuildingCount;//����������
    public int UnusedUnitCount;//δʹ�õĸ�����


    public int DeckMissionaryCount;//����ʿ��ɽ��
    public int DeckSoliderCount;//ʿ����ɽ��
    public int DeckFarmerCount;//ũ����ɽ��
    public int DeckBuildingCount;//������ɽ��


    // �����õ��������ݼ�
    public List<UIUnitData> MissionaryUnits;
    public List<UIUnitData> SoliderUnits;
    public List<UIUnitData> FarmerUnits;
    public List<UIUnitData> BuildingUnits;

    // ���List
    private Dictionary<int, UIPlayerData> allPlayersData;



    [Header("UI Elements")]
    public Image religionIcon;//�ڽ�ͼ��
    public TextMeshProUGUI resourcesValue;             //��Դ��
    public TextMeshProUGUI activateMissionaryValue;//����ʿ������
    public TextMeshProUGUI activateSoliderValue;//ʿ��������
    public TextMeshProUGUI activateFarmerValue;//ũ�񼤻���
    public TextMeshProUGUI allUnitValue;       // ��ǰ���˿�/�˿�����
    public TextMeshProUGUI unusedUnitValue;//δʹ�õĸ�����

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




    }


    public static List<UIUnitData> GetUIUnitDataList(CardType type)
    {
        List<int> UnitIDs = PlayerUnitDataInterface.Instance.GetUnitIDListByType(type);
        List<UIUnitData> uiList = new List<UIUnitData>();

        foreach (int id in UnitIDs)
        {
            PlayerUnitData? unitData = PlayerUnitDataInterface.Instance.GetUnitData(id);

            uiList.Add(new UIUnitData
            {
                UnitId = id,
                UnitType = unitData.Value.UnitType,
                Position = unitData.Value.Position,
                HP = 3,
                AP = 5,
            }); 
        }

        return uiList;
    }








}







