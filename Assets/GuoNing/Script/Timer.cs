using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 用于每个玩家的计时器
/// </summary>
public class Timer : MonoBehaviour
{
	private bool m_isTimeOut; // 是否超时
	[SerializeField] private float timeLimitEveryTurn = 30f; // 每回合都会刷新的时间限制
	[SerializeField] private float timePool;	// 每局游戏玩家可以使用的时间池
	private float m_currentTurnTime;	// 当前回合剩余时间
	private float m_currentTimePool;	// 本局游戏剩余时间池
	private bool m_isRunning;			// 计时器是否运行

	public Action OnTimeOut;	// 超时回调
	//      当前回合剩余时间， 时间池剩余时间
	public Action<float, float> OnTimeChanged;	// 时间变化回调


	// Start is called before the first frame update
	void Start()
	{
		InitTimer();
	}

	// Update is called once per frame
	public void SetTime()
	{
		//if (!m_isRunning || m_isTimeOut) return;

		if (m_currentTurnTime > 0)
		{
			m_currentTurnTime -= Time.deltaTime;
			if (m_currentTurnTime < 0) m_currentTurnTime = 0;
		}
		else
		{
			m_currentTimePool -= Time.deltaTime;
			if (m_currentTimePool < 0) 
			{
				m_currentTimePool = 0;
				m_isTimeOut = true;
				m_isRunning = false;
			}
		}

		OnTimeChanged?.Invoke(m_currentTurnTime, m_currentTimePool);

		// 更新UI

        GameSceneUIManager.Instance.SetCountdownTime((int)m_currentTurnTime);
	
        GameSceneUIManager.Instance.SetCountdownTimePool((int)m_currentTimePool);

		// 如果时间池和当前时间都用完了，则超时
		if (m_currentTurnTime <= 0 && timePool <= 0)
		{
			m_isTimeOut = true;
			m_isRunning = false;
			OnTimeOut?.Invoke();
		}
	}

	public void StartTurn()
	{
		if (m_isTimeOut) return;
		m_currentTurnTime = timeLimitEveryTurn;	// 重置时间
		m_isRunning = true;						// 开始计时

	}

	public void EndTurn()
	{
		m_isRunning = false;	// 停止计时
		m_currentTurnTime = 0;	// 当前回合时间归零
	}

	public void InitTimer()
	{
		m_isTimeOut = false;
		m_currentTimePool = timePool;
		EndTurn();	// 重置时间
	}

    public bool IsTimeOut()
    {
        return m_isTimeOut;
	}
}
