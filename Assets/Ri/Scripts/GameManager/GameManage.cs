using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

// ������Ϣ�ṹ��
public struct BoardInfor
{
  
    // �����ڶ�ά�����е�����
    public int2 Cells2DPos;

    // �����������е�����
    public Vector3 Cells3DPos;


    // ��ǰ���̵����к�
    public int id;

};

public class GameManage : MonoBehaviour
{
    // ����
    public static GameManage Instance { get; private set; }

    // *************************
    // ��������˽�����ԡ�������
    // *************************
    private Camera GameCamera;
    // �������߼��㼶
    private int RayTestLayerMask = 1 << 6;

    // ��ǰ��������������к�
    private int ClickCellid;

    // �Ƿ�����Ϸ��
    private bool bIsInGaming;

    // ��ǰ�������Ƿ�����ҿ��Ƶ�����
    private bool[,] bIsHavePlayer;

    // ��ǰ���������е������������
    private GameObject[,] AllUnits;
    private GameObject SelectingUnit;

    // ��һ��ѡ�е�ӵ�е�λ�ĸ���id
    private int LastPlayerID;

    // ������ӵ���ʼλ��
    // ���趨��ɺ����
    private List<int2> PlayerStartPos2D =   new List<int2>();

    // �����������Ϸ�е�λ��
    private List<int2> PlayerGamingPos = new List<int2>();

    // ���ֵ�������Ϣ
    private List<BoardInfor> GameBoardInfor=new List<BoardInfor>();
    private Dictionary<int, BoardInfor> GameBoardInforDict=new Dictionary<int, BoardInfor>();

    // �Ƿ�ɼ������е������
    private bool bCanContinue = true;




    // *************************
    // ���������������ԡ�������
    // *************************

    // �����Ƿ�����Ϸ��
    public void SetIsGamingOrNot(bool isGaming) { bIsInGaming = isGaming; }

    // ����Ԥ���壬������������ɺ����
    public GameObject Player;


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
        }

    }
 


    // Update is called once per frame
    void Update()
    {
        if (bIsInGaming)
        {
            // ���������
            if (Input.GetMouseButtonDown(0) && bCanContinue)
            {
                Ray ray = GameCamera.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;


                // �жϵ�������Ƿ��Ǹ���
                if (Physics.Raycast(ray, out hit, Mathf.Infinity, RayTestLayerMask))
                {
                    ClickCellid = hit.collider.gameObject.GetComponent<HexCell>().id;

                    //Debug.Log("Cell's 2Dpos is "+ FindCell(ClickCellid).Cells2DPos.x+
                    //    ","+FindCell(ClickCellid).Cells2DPos.y+
                    //    "\nCell's 3Dpos is " + FindCell(ClickCellid).Cells3DPos);

                    if (!FindPlayerOnCell(ClickCellid))
                    {
                        ReturnToDefault();
                        SelectingUnit = null;
                    }
                    else
                    {
                        SelectingUnit = AllUnits[FindCell(ClickCellid).Cells2DPos.x, FindCell(ClickCellid).Cells2DPos.y];
                        SelectingUnit.GetComponent<ChangeMaterial>().Outline();
                        LastPlayerID = ClickCellid;
                    }
                }
                else
                {
                    ReturnToDefault();
                    Debug.Log("no object");
                }
            }
            // ����Ҽ����
            if (Input.GetMouseButtonDown(1) && bCanContinue)
            {
                Ray ray = GameCamera.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                // �жϵ�������Ƿ��Ǹ���
                if (Physics.Raycast(ray, out hit, Mathf.Infinity, RayTestLayerMask))
                {
                    ClickCellid = hit.collider.gameObject.GetComponent<HexCell>().id;
                    if (!FindPlayerOnCell(ClickCellid))
                    {
                        if(SelectingUnit!=null)
                            MoveToSelectCell(ClickCellid);
                    }
                }
            }
        }
    }

    // ��Ϸ����
    public bool GameOver()
    {
        PlayerStartPos2D.Clear(); 
        PlayerGamingPos.Clear();

        GameBoardInfor.Clear();
        GameBoardInforDict.Clear();


        SetIsGamingOrNot(false);
        return true;
    }
    // *************************
    // ��������˽�к�����������
    // *************************

    // ���б�����Ϸ�ĳ�ʼ��
    public bool GameInit()
    {
        // test
        SetIsGamingOrNot(true);


        if (bIsInGaming)
        {
            GameCamera = GameObject.Find("GameCamera").GetComponent<Camera>();
          
            bIsHavePlayer = new bool[FindCell(GameBoardInforDict.Count - 1).Cells2DPos.x+1, FindCell(GameBoardInforDict.Count - 1).Cells2DPos.y + 1];
            AllUnits=new GameObject[FindCell(GameBoardInforDict.Count - 1).Cells2DPos.x + 1, FindCell(GameBoardInforDict.Count - 1).Cells2DPos.y + 1];


            // ��ʼ����ң����趨��ɺ����
            CreatePlayerObjects();
          
            
        }
        return true;
    }

    // �������кŲ���ĳ������
    private BoardInfor FindCell(int id)
    {
        return GameBoardInforDict[id];
    }


    /// <summary>
    /// ������ҵĵ�λ
    /// </summary>
    private void CreatePlayerObjects()
    {
        int2 start = new int2(0, 0);
        PlayerStartPos2D.Add(start);
        GameObject Player01 = Instantiate(Player,
            FindCell(0).Cells3DPos,
            Player.transform.rotation);

        Player01.transform.position = new Vector3(Player01.transform.position.x,
            Player01.transform.position.y + 2.5f,
            Player01.transform.position.z);

        bIsHavePlayer[FindCell(0).Cells2DPos.x, FindCell(0).Cells2DPos.y] = true;
        AllUnits[FindCell(0).Cells2DPos.x, FindCell(0).Cells2DPos.y] = Player01;
    }

    /// <summary>
    /// Ѱ��ĳ���������Ƿ�����ҵ�λ����
    /// </summary>
    /// <param name="id">���ӵ�id</param>
    /// <returns></returns>
    private bool FindPlayerOnCell(int id)
    {
        if (bIsHavePlayer[FindCell(id).Cells2DPos.x, FindCell(id).Cells2DPos.y])
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// ����ҵ�λ����Ϊδѡ��״̬
    /// </summary>
    private void ReturnToDefault()
    {
        if(SelectingUnit!=null)
            SelectingUnit.GetComponent<ChangeMaterial>().Default();
       
    }

    /// <summary>
    /// �ƶ���ѡ�еĸ���
    /// </summary>
    /// <param name="id">���ӵ�id</param>
    private void MoveToSelectCell(int id)
    {
        bCanContinue = false;
        Vector3 newPos = new Vector3(FindCell(id).Cells3DPos.x, FindCell(id).Cells3DPos.y+2.5f, FindCell(id).Cells3DPos.z); 
        SelectingUnit.transform.DOMove(newPos, 1.0f).OnComplete(() => {
            bCanContinue = true;

            // Ϊ���ƶ��ĸ������״̬
            bIsHavePlayer[FindCell(id).Cells2DPos.x, FindCell(id).Cells2DPos.y] =true;
            AllUnits[FindCell(id).Cells2DPos.x, FindCell(id).Cells2DPos.y] = SelectingUnit;

            // ��ʼ����һ�β����ĸ��ӵ�״̬
            bIsHavePlayer[FindCell(LastPlayerID).Cells2DPos.x, FindCell(LastPlayerID).Cells2DPos.y] = false;
            AllUnits[FindCell(LastPlayerID).Cells2DPos.x, FindCell(LastPlayerID).Cells2DPos.y] = null;

            LastPlayerID = id;
        }); ;

    }

    // *************************
    // �����������ú�����������
    // *************************

    /// <summary>
    /// ��Ӹ�����Ϣ���� HexGrid�ű��е� CreateCell() ��������
    /// </summary>
    /// <param name="infor">������Ϣ�ṹ��</param>
    public void SetGameBoardInfor(BoardInfor infor)
    {
        GameBoardInfor.Add(infor);
        GameBoardInforDict.Add(infor.id, infor);
    }
}
