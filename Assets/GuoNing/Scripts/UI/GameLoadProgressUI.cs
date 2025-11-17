using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ゲーム仮ロードUI
/// </summary>
[RequireComponent(typeof(Image))]
public class GameLoadProgressUI : MonoBehaviour
{
	[Header("初始进度(%)")]
	public float startProgress = 30f;

	[Header("完成进度(%)")]
	public float endProgress = 100f;

	[Header("加载完成所需时间")]
	public float timeLimit = 3f;

	public TextMeshProUGUI text;

	private float currentTime = 0f;
	private bool isStartLoading = false;

	public event Action<bool> OnLoadingEnd;

	private void Awake()
	{
		// 监听房间状态更新
		NetGameSystem.Instance.OnRoomStatusUpdated += HandleRoomStatusUpdate;
	}

	private void OnDestroy()
	{
		// 记得取消订阅，避免内存泄漏
		NetGameSystem.Instance.OnRoomStatusUpdated -= HandleRoomStatusUpdate;
		
	}

	private void Start()
	{
		text.text = $"{startProgress:F0}%";
	}

	private void Update()
	{
		if (!isStartLoading) return;

		currentTime += Time.deltaTime;

		float t = Mathf.Clamp01(currentTime / timeLimit);
		float progress = Mathf.Lerp(startProgress, endProgress, t);

		text.text = $"{progress:F0}%";

		if (t >= 1f)
		{
			isStartLoading = false;
			OnLoadingEnd?.Invoke(true);
		}
	}

	//=========================================
	// メソッド
	//=========================================

	private void HandleRoomStatusUpdate(List<PlayerInfo> players)
	{
		// 当尚未开始加载且玩家满足数量才会开始调用
		if (!isStartLoading && players.Count >=2)
		{
			Debug.Log("[客户端] 玩家到齐，开始 Loading UI");
			isStartLoading = true;
		}
	}
}
