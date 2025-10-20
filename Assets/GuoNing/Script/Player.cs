using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 用于管理每个玩家的信息
/// </summary>

public class Player : MonoBehaviour
{
	private List<GameObject> m_pieces; // 玩家目前拥有的棋子 之后替换成棋子
	private List<GameObject> m_buildings; // 玩家目前拥有的建筑 之后替换成建筑
	[SerializeField] private Timer m_timer; // 计时器

	/// <summary>
	/// 棋子预制件 之后替换成棋子
	/// </summary>
	[SerializeField] GameObject farmerPrefab; // 农民预制件
	[SerializeField] GameObject SoliderPrefab; // 士兵预制件
	[SerializeField] GameObject MissionaryPrefab; // 传教士预制件
	[SerializeField] GameObject PopePrefabe; // 教皇预制件

	private float m_currentResource; // 当前拥有的资源
	[SerializeField] private bool isYourTurn; // 是否是该玩家的回合		



	// Start is called before the first frame update
	void Start()
	{

	}

	// Update is called once per frame
	void Update()
	{
		//// 如果计时器超时 则结束回合
		////if (m_timer.IsTimeOut())
		//{
		//	EndTurn();
		//}
	}


	/// <summary>
	/// 在指定位置生成士兵
	/// </summary>
	public void GenerateSolider(Vector3 generatePos)
	{
		// todo：替换成棋子之后 消耗棋子所需的资源
		float soliderResourceNeeded = 1f; // 生成士兵所需资源
		if (m_currentResource < soliderResourceNeeded) return; // 如果资源不足则无法生成

		m_currentResource -= soliderResourceNeeded; // 扣除资源
		GameObject newSolider = Instantiate(SoliderPrefab, generatePos, Quaternion.identity); // 生成棋子
		m_pieces.Add(newSolider); // 添加到玩家的棋子列表中
	}

	/// <summary>
	/// 在指定位子生成农民
	/// </summary>
	public void GenerateFarmer(Vector3 generatePos)
	{
		float farmerResourceNeeded = 1f;
		if (m_currentResource < farmerResourceNeeded) return; // 如果资源不足则无法生成
		m_currentResource -= farmerResourceNeeded; // 扣除资源
		GameObject newFarmer = Instantiate(farmerPrefab, generatePos, Quaternion.identity); // 生成棋子
		m_pieces.Add(newFarmer); // 添加到玩家的棋子列表中
	}

	/// <summary>
	/// 在指定位置生成传教士
	/// </summary>
	public void GenerateMissionary(Vector3 generatePos)
	{
		float missionaryResourceNeeded = 1f;
		if (m_currentResource < missionaryResourceNeeded) return; // 如果资源不足则无法生成
		m_currentResource -= missionaryResourceNeeded; // 扣除资源
		GameObject newMissionary = Instantiate(MissionaryPrefab, generatePos, Quaternion.identity); // 生成棋子
		m_pieces.Add(newMissionary); // 添加到玩家的棋子列表中
	}


	/// <summary>
	/// 从当前拥有的建筑中获取资源
	/// </summary>
	public void GetResource()
	{
		foreach (var building in m_buildings)
		{
			float resource = 1f;// 每个建筑每回合产生的资源
			m_currentResource += resource;
		}
	}


	/// <summary>
	/// 开始回合
	/// </summary>
	public void StartTurn()
	{
		if(m_timer)m_timer.StartTurn();

		isYourTurn = true;
	}

	/// <summary>
	/// 结束回合
	/// </summary>
	public void EndTurn()
	{
		//if(m_timer)m_timer.EndTurn();
	
		//isYourTurn = false;
	}
}
