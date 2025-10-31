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







}
