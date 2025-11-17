using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ゲーム仮ロードUI
/// </summary>
[RequireComponent(typeof(Image))]
public class GameLoadProgressUI : MonoBehaviour
{
	[Header("Start progress(%)")]
	public float startProgress =0f;

	[Header("Fake progress(%)")]
	public float fakeProgress = 30f;

	[Header("End progress(%)")]
	public float endProgress = 100f;

	[Header("Time limit")]
	public float fakeLimit = 1.2f;
	public float timeLimit = 3f;

	public TextMeshProUGUI text;

	private float currentTime = 0f;
	private bool isStartLoading = false;

	public event Action<bool> OnLoadingEnd;

	private void Awake()
	{
		// 监听房间状态更新
		if (NetGameSystem.Instance != null)
		{
			NetGameSystem.Instance.OnRoomStatusUpdated += HandleRoomStatusUpdate;
		}
	}

	private void OnDestroy()
	{
		// 监听房间状态更新
		if (NetGameSystem.Instance != null)
		{
			NetGameSystem.Instance.OnRoomStatusUpdated -= HandleRoomStatusUpdate;
		}
	
		
	}

	private void Start()
	{
		text.text = $"{startProgress:F0}%";
		GetComponent<Image>().fillAmount =0;
	}

	private void Update()
	{
		if (Input.GetKeyUp(KeyCode.Space))
		{
			currentTime = 0;
			isStartLoading = true ;
		}

		if (!isStartLoading){

			currentTime += Time.deltaTime;
			float t = Mathf.Clamp01(currentTime / fakeLimit);
			if (t >= 1) return;

			float progress = Mathf.Lerp(0, fakeProgress, t);
			text.text = $"{progress:F0}%";

			Debug.Log(GetComponent<Image>().fillAmount);
			GetComponent<Image>().fillAmount = progress / endProgress;
		}
		else
		{
			currentTime += Time.deltaTime;

			float t = Mathf.Clamp01(currentTime / timeLimit);
			float progress = Mathf.Lerp(fakeProgress, endProgress, t);

			text.text = $"{progress:F0}%";

			if (t >= 1f)
			{
				isStartLoading = false;
				OnLoadingEnd?.Invoke(true);
				return;
			}

			GetComponent<Image>().fillAmount = progress / endProgress;
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
			currentTime = 0;
		}
	}
}
