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
    // 配置
    [Header("计时器设置")]
    public float turnTimeLimit = 40f;      // 每回合独立时间
    public float timePoolInitial = 60f;   // 倒计时池初始时间

    // 状态
    private float currentTurnTime;
    private float currentTimePool;
    private bool isRunning = false;
    private bool isUsingTimePool = false;

    // 事件
    public event Action OnTimeOut;          // 时间用完事件
    public event Action OnTimePoolStarted;  // 开始使用倒计时池事件

    private void Awake()
    {
        currentTimePool = timePoolInitial;
    }

    private void Update()
    {
        if (isRunning)
        {
            UpdateTimer();
        }
    }

    public void StartTurn()
    {
        currentTurnTime = turnTimeLimit;
        isUsingTimePool = false;
        isRunning = true;
        Debug.Log($"Timer: 回合开始 - 独立时间: {turnTimeLimit}秒");
    }

    public void StopTimer()
    {
        isRunning = false;
    }

    private void UpdateTimer()
    {
        if (!isUsingTimePool)
        {
            currentTurnTime -= Time.deltaTime;

            if (currentTurnTime <= 0)
            {
                currentTurnTime = 0;
                isUsingTimePool = true;
                OnTimePoolStarted?.Invoke();
                //Debug.Log("Timer: 开始使用倒计时池");
            }
        }
        else
        {
            currentTimePool -= Time.deltaTime;

            if (currentTimePool <= 0)
            {
                currentTimePool = 0;
                isRunning = false;
                OnTimeOut?.Invoke();
                Debug.LogWarning("Timer: 时间耗尽!");
            }
        }
    }

    public float GetTurnTime() => currentTurnTime;
    public float GetTimePool() => currentTimePool;
    public bool IsUsingTimePool() => isUsingTimePool;

    public void SetTurnTimeLimit(int limit)
    {

        turnTimeLimit = limit;

    }

    public void SetTimePoolInitial(int limit)
    {

        timePoolInitial = limit;

    }

}