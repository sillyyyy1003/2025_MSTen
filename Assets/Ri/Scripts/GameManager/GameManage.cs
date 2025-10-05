using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

// 棋盘信息结构体
public struct BoardInfor
{

	// 棋盘在二维数组中的坐标
	public int2 Cells2DPos;

	// 棋盘在世界中的坐标
	public Vector3 Cells3DPos;


	// 当前棋盘的序列号
	public int id;

};

public class GameManage : MonoBehaviour
{
	// 单例
	public static GameManage Instance { get; private set; }

	// *************************
	// ————私有属性————
	// *************************
	private Camera GameCamera;
	// 设置射线检测层级
	private int RayTestLayerMask = 1 << 6;

	// 当前点击到的棋盘序列号
	private int ClickCellid;

	// 是否在游戏中
	private bool bIsInGaming;

	// 当前棋盘上是否有玩家控制的棋子
	private bool[,] bIsHavePlayer;

	// 当前棋盘上所有的玩家棋子数组
	private GameObject[,] AllUnits;
	private GameObject SelectingUnit;

	// 上一次选中的拥有单位的格子id
	private int LastPlayerID;

	// 玩家棋子的起始位置
	// 待设定完成后更改
	private List<int2> PlayerStartPos2D = new List<int2>();

	// 玩家棋子在游戏中的位置
	private List<int2> PlayerGamingPos = new List<int2>();

	// 本局的棋盘信息
	private List<BoardInfor> GameBoardInfor = new List<BoardInfor>();
	private Dictionary<int, BoardInfor> GameBoardInforDict = new Dictionary<int, BoardInfor>();

	// 是否可继续进行点击操作
	private bool bCanContinue = true;




	// *************************
	// ————公用属性————
	// *************************

	// 设置是否在游戏中
	public void SetIsGamingOrNot(bool isGaming) { bIsInGaming = isGaming; }

	// 棋子预制体，待棋子制作完成后更换
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
			// 鼠标左键点击
			if (Input.GetMouseButtonDown(0) && bCanContinue)
			{
				Ray ray = GameCamera.ScreenPointToRay(Input.mousePosition);
				RaycastHit hit;


				// 判断点击到的是否是格子
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
			// 鼠标右键点击
			if (Input.GetMouseButtonDown(1) && bCanContinue)
			{
				Ray ray = GameCamera.ScreenPointToRay(Input.mousePosition);
				RaycastHit hit;

				// 判断点击到的是否是格子
				if (Physics.Raycast(ray, out hit, Mathf.Infinity, RayTestLayerMask))
				{
					ClickCellid = hit.collider.gameObject.GetComponent<HexCell>().id;
					if (!FindPlayerOnCell(ClickCellid))
					{
						if (SelectingUnit != null)
							MoveToSelectCell(ClickCellid);
					}
				}
			}
		}
		
	}

	// 游戏结束
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
	// ————私有函数————
	// *************************

	// 进行本局游戏的初始化
	public bool GameInit()
	{
		// test
		SetIsGamingOrNot(true);


		if (bIsInGaming)
		{
			GameCamera = GameObject.Find("GameCamera").GetComponent<Camera>();

			bIsHavePlayer = new bool[FindCell(GameBoardInforDict.Count - 1).Cells2DPos.x + 1, FindCell(GameBoardInforDict.Count - 1).Cells2DPos.y + 1];
			AllUnits = new GameObject[FindCell(GameBoardInforDict.Count - 1).Cells2DPos.x + 1, FindCell(GameBoardInforDict.Count - 1).Cells2DPos.y + 1];


			// 初始化玩家，待设定完成后更改
			CreatePlayerObjects();


		}
		return true;
	}

	// 根据序列号查找某个棋盘
	private BoardInfor FindCell(int id)
	{
		return GameBoardInforDict[id];
	}


	/// <summary>
	/// 创建玩家的单位
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
	/// 寻找某个格子上是否有玩家单位存在
	/// </summary>
	/// <param name="id">格子的id</param>
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
	/// 将玩家单位设置为未选中状态
	/// </summary>
	private void ReturnToDefault()
	{
		if (SelectingUnit != null)
			SelectingUnit.GetComponent<ChangeMaterial>().Default();

	}

	/// <summary>
	/// 移动到选中的格子
	/// </summary>
	/// <param name="id">格子的id</param>
	private void MoveToSelectCell(int id)
	{
		bCanContinue = false;
		Vector3 newPos = new Vector3(FindCell(id).Cells3DPos.x, FindCell(id).Cells3DPos.y + 2.5f, FindCell(id).Cells3DPos.z);
		SelectingUnit.transform.DOMove(newPos, 1.0f).OnComplete(() => {
			bCanContinue = true;

			// 为新移动的格子添加状态
			bIsHavePlayer[FindCell(id).Cells2DPos.x, FindCell(id).Cells2DPos.y] = true;
			AllUnits[FindCell(id).Cells2DPos.x, FindCell(id).Cells2DPos.y] = SelectingUnit;

			// 初始化上一次操作的格子的状态
			bIsHavePlayer[FindCell(LastPlayerID).Cells2DPos.x, FindCell(LastPlayerID).Cells2DPos.y] = false;
			AllUnits[FindCell(LastPlayerID).Cells2DPos.x, FindCell(LastPlayerID).Cells2DPos.y] = null;

			LastPlayerID = id;
		}); ;

	}

	// *************************
	// ————公用函数————
	// *************************

	/// <summary>
	/// 添加格子信息，由 HexGrid脚本中的 CreateCell() 函数调用
	/// </summary>
	/// <param name="infor">格子信息结构体</param>
	public void SetGameBoardInfor(BoardInfor infor)
	{
		GameBoardInfor.Add(infor);
		GameBoardInforDict.Add(infor.id, infor);
	}
}