using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Unity.Mathematics;
using Newtonsoft.Json.Bson;

/// <summary>
/// 玩家操作管理，负责处理因玩家操作导致的数据变动
/// </summary>
public class PlayerOperationManager : MonoBehaviour
{
    private Camera GameCamera; 
    
    // 是否可进行操作
    private bool bCanContinue = true; 
    
    // 点击到的格子的id
    private int ClickCellid;   
    
    // 射线检测指定为cell层级
    private int RayTestLayerMask = 1 << 6;

    // 当前选择的单位
    private GameObject SelectingUnit;

    // 上一次选择到的格子的id
    private int LastSelectingCellID;

    // 本机玩家保存的棋盘信息
    private Dictionary<int, BoardInfor> PlayerBoardInforDict = new Dictionary<int, BoardInfor>();


    //// 玩家所有单位的GameOnject
    //private GameObject[,] PlayerUnits; 

    //// 当前格子上是否有玩家控制的单位
    //private bool[,] bIsHavePlayer;   

    // 玩家的预制体，后续更改
    public GameObject Framer;

    // 玩家的id，由NetGameSystem统一分配
    public int PlayerID=0;
    public void SetPlayerID(int id) { PlayerID = id; }


    // Start is called before the first frame update
    void Start()
    {
        GameCamera = GameObject.Find("GameCamera").GetComponent<Camera>(); 
    }


  
    // Update is called once per frame
    void Update()
    {
        if(GameManage.Instance.GetIsGamingOrNot())
        {
                // 左键点击
                if (Input.GetMouseButtonDown(0) && bCanContinue)
                {
                    Ray ray = GameCamera.ScreenPointToRay(Input.mousePosition);
                    RaycastHit hit;


                    // 射线检测
                    if (Physics.Raycast(ray, out hit, Mathf.Infinity, RayTestLayerMask))
                    {
                        ClickCellid = hit.collider.gameObject.GetComponent<HexCell>().id;

                        //Debug.Log("Cell's 2Dpos is "+ FindCell(ClickCellid).Cells2DPos.x+
                        //    ","+FindCell(ClickCellid).Cells2DPos.y+
                        //    "\nCell's 3Dpos is " + FindCell(ClickCellid).Cells3DPos);

                        if (!GameManage.Instance.FindPlayerOnCell(ClickCellid))
                        {
                            ReturnToDefault();
                            SelectingUnit = null;
                        }
                        else
                        {
                            int2 clickPos =new int2(GameManage.Instance.FindCell(ClickCellid).Cells2DPos.x,
                            GameManage.Instance.FindCell(ClickCellid).Cells2DPos.y);

                            SelectingUnit = GameManage.Instance.GetSelectGameObject(0, clickPos);
                            SelectingUnit.GetComponent<ChangeMaterial>().Outline();
                            LastSelectingCellID = ClickCellid;
                        }
                    }
                    else
                    {
                        ReturnToDefault();
                        Debug.Log("no object");
                    }
                }
                // 右键点击
                if (Input.GetMouseButtonDown(1) && bCanContinue)
                {
                    Ray ray = GameCamera.ScreenPointToRay(Input.mousePosition);
                    RaycastHit hit;

                    // 射线检测
                    if (Physics.Raycast(ray, out hit, Mathf.Infinity, RayTestLayerMask))
                    {
                        ClickCellid = hit.collider.gameObject.GetComponent<HexCell>().id;
                        if (!GameManage.Instance.FindPlayerOnCell(ClickCellid))
                        {
                            if (SelectingUnit != null)
                                MoveToSelectCell(ClickCellid);
                        }
                    }
                }
            }
    }

    // *************************
    //         公有函数
    // *************************

    /// <summary>
    /// 创建玩家
    /// </summary>
    /// <param name="boardID">玩家初始位置格子id</param>
    public void InitPlayer(int boardID)
    {
        //bIsHavePlayer = new bool[GameManage.Instance.GetBoardInfor(GameManage.Instance.GetBoardCount() - 1).Cells2DPos.x + 1,
        //   GameManage.Instance.GetBoardInfor(GameManage.Instance.GetBoardCount() - 1).Cells2DPos.y + 1];

        //PlayerUnits = new GameObject[GameManage.Instance.GetBoardInfor(GameManage.Instance.GetBoardCount() - 1).Cells2DPos.x + 1,
        //   GameManage.Instance.GetBoardInfor(GameManage.Instance.GetBoardCount() - 1).Cells2DPos.y + 1];


        PlayerBoardInforDict = GameManage.Instance.GetPlayerBoardInfor();

        // 创建玩家拥有的单位
        CreatePlayerObjects();
    }

    public void TurnEnd()
    {

    }

    public void TurnStart()
    {

    }


    // *************************
    //         私有函数
    // *************************



    /// <summary>
    /// 移动到选择的棋盘
    /// </summary>
    /// <param name="id">棋盘id</param>
    private void MoveToSelectCell(int id)
    {
        bCanContinue = false;
        Vector3 newPos = new Vector3(
           PlayerBoardInforDict[id].Cells3DPos.x,
           PlayerBoardInforDict[id].Cells3DPos.y + 2.5f,
           PlayerBoardInforDict[id].Cells3DPos.z);

        SelectingUnit.transform.DOMove(newPos, 1.0f).OnComplete(() => {
            bCanContinue = true;

            //// 对将要移动到的棋盘进行设置
            //bIsHavePlayer[PlayerBoardInforDict[id].Cells2DPos.x,
            //     PlayerBoardInforDict[id].Cells2DPos.y] = true;

            //PlayerUnits[PlayerBoardInforDict[id].Cells2DPos.x,
            //    PlayerBoardInforDict[id].Cells2DPos.y] = SelectingUnit;

            //// 将移动前的棋盘初始化
            //bIsHavePlayer[PlayerBoardInforDict[LastSelectingCellID].Cells2DPos.x,
            //   PlayerBoardInforDict[LastSelectingCellID].Cells2DPos.y] = false;

            //PlayerUnits[PlayerBoardInforDict[LastSelectingCellID].Cells2DPos.x,
            //   PlayerBoardInforDict[LastSelectingCellID].Cells2DPos.y] = null;

            LastSelectingCellID = id;
        }); ;

    }

    /// <summary>
    /// 取消选择单位的描边
    /// </summary>
    private void ReturnToDefault()
    {
        if (SelectingUnit != null)
            SelectingUnit.GetComponent<ChangeMaterial>().Default();

    }

    /// <summary>
    /// 创建玩家单位
    /// </summary>
    private void CreatePlayerObjects()
    {
        int2 start = new int2(0, 0);

        GameObject Player01 = Instantiate(Framer,
           GameManage.Instance.GetBoardInfor(0).Cells3DPos,
             Framer.transform.rotation);

        Player01.transform.position = new Vector3(Player01.transform.position.x,
            Player01.transform.position.y + 2.5f,
            Player01.transform.position.z);

        //bIsHavePlayer[PlayerBoardInforDict[0].Cells2DPos.x,
        //   PlayerBoardInforDict[0].Cells2DPos.y] = true;
        //PlayerUnits[PlayerBoardInforDict[0].Cells2DPos.x,
        //   PlayerBoardInforDict[0].Cells2DPos.y] = Player01;


    }

    /// <summary>
    /// 查找某个格子上是否有玩家的单位
    /// </summary>
    /// <param name="id">格子id</param>
    /// <returns></returns>
    private bool FindPlayerOnCell(int id)
    {
        //if (bIsHavePlayer[PlayerBoardInforDict[id].Cells2DPos.x,
        //    GameManage.Instance.FindCell(id).Cells2DPos.y])
        //{
        //    return true;
        //}
        return false;
    }

   
}
