
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

using UnityEngine.UIElements;

// 棋盘每个格子信息的结构体
public struct BoardInfor
{
  
    // 每个格子的二维坐标
    public int2 Cells2DPos;

    // 每个格子的世界坐标
    public Vector3 Cells3DPos;


    // 每个格子的id
    public int id;


};

public struct AllPlayerInfor
{
    int PlayerID;

    // 玩家所有单位的GameOnject
    private GameObject[,] AllPlayerUnits;

    // 当前格子上是否有玩家控制的单位
    private bool[,] bIsHavePlayer;


};

public class GameManage : MonoBehaviour
{
    // 单例
    public static GameManage Instance { get; private set; }

    // *************************
    //          私有属性
    // *************************
 
   

    // 是否在游戏中
    private bool bIsInGaming;

    // 玩家所有单位的GameOnject
    private GameObject[,] AllPlayerUnits;

    // 当前格子上是否有玩家控制的单位
    private bool[,] bIsHavePlayer;


    // 玩家的位置列表
    // 玩家A起始位置二维数组的列表
    private List<int2> PlayerStartPos2D =   new List<int2>();

    // 玩家起始世界位置的列表
    private List<int2> PlayerGamingPos3D = new List<int2>();

    // A玩家拥有单位的格子列表
    private List<int2> PlayerAPos2D = new List<int2>();

    // B玩家拥有单位的格子列表
    private List<int2> PlayerBPos2D = new List<int2>();



    // 棋盘信息List与Dictionary
    private List<BoardInfor> GameBoardInfor=new List<BoardInfor>();
    private Dictionary<int, BoardInfor> GameBoardInforDict=new Dictionary<int, BoardInfor>(); 
    // 返回棋盘信息
    public Dictionary<int, BoardInfor> GetPlayerBoardInfor(){return GameBoardInforDict; }


    // 通过获取实例方式得到脚本
    public PlayerOperationManager _PlayerOperation;


    // *************************
    //         公有属性
    // *************************

    // 设置当前是否在游戏中
    public void SetIsGamingOrNot(bool isGaming) { bIsInGaming = isGaming; }
    public bool GetIsGamingOrNot() {return bIsInGaming; }

 


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
       
    }

    



    // *************************
    //         私有函数
    // *************************





  
    

    // *************************
    //        公有函数
    // *************************

    // 游戏初始化
    public bool GameInit()
    {
        // test
        SetIsGamingOrNot(true);

        if (bIsInGaming)
        {
            bIsHavePlayer = new bool[FindCell(GameBoardInforDict.Count - 1).Cells2DPos.x + 1, FindCell(GameBoardInforDict.Count - 1).Cells2DPos.y + 1];
            AllPlayerUnits = new GameObject[FindCell(GameBoardInforDict.Count - 1).Cells2DPos.x + 1, FindCell(GameBoardInforDict.Count - 1).Cells2DPos.y + 1];
          
            // 初始化本机玩家数据
            _PlayerOperation.InitPlayer();
        }
        return true;
    }

    // 游戏结束
    public bool GameOver()
    {
        PlayerStartPos2D.Clear();
        PlayerGamingPos3D.Clear();

        GameBoardInfor.Clear();
        GameBoardInforDict.Clear();


        SetIsGamingOrNot(false);
        return true;
    }


    public BoardInfor GetBoardInfor(int id)
    {
        return GameBoardInforDict[id];
    }
    public int GetBoardCount()
    {
        return GameBoardInforDict.Count;
    }

    public GameObject GetSelectGameObject(int playerID,int2 pos)
    {
        return AllPlayerUnits[pos.x, pos.y];
    }


    // 得到其他玩家的数据并同步
    public void SetPlayersData(int playerID)
    {

    }
   



    // 根据格子id得到其具体信息
    public BoardInfor FindCell(int id)
    {
        return GameBoardInforDict[id];
    }

    /// <summary>
    /// 查找某个格子上是否有玩家的单位
    /// </summary>
    /// <param name="id">格子id</param>
    /// <returns></returns>
    public bool FindPlayerOnCell(int id)
    {
        if (bIsHavePlayer[FindCell(id).Cells2DPos.x, FindCell(id).Cells2DPos.y])
        {
            return true;
        }
        return false;
    }



    /// <summary>
    /// 设置棋盘结构体信息
    /// </summary>
    /// <param name="infor">格子id</param>
    public void SetGameBoardInfor(BoardInfor infor)
    {
        GameBoardInfor.Add(infor);
        GameBoardInforDict.Add(infor.id, infor);
    }
}
