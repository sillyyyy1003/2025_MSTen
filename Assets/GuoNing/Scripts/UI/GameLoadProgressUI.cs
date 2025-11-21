using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ゲーム仮ロードUI
/// </summary>
[RequireComponent(typeof(Image))]
public class GameLoadProgressUI : MonoBehaviour
{
	[Header("Start progress(%)")]
	public float startProgress = 0f;

	[Header("Fake progress(%)")]
	public float fakeProgress = 30f;

	[Header("End progress(%)")]
	public float endProgress = 100f;

	[Header("Time limit")]
	public float fakeLimit = 1.2f;
	public float timeLimit = 1f;

	public TextMeshProUGUI text;

	private bool hasStartedRealLoading = false;

	public event Action<bool> OnLoadingEnd;

	private Coroutine loadRoutine;

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
		if (NetGameSystem.Instance != null)
		{
			NetGameSystem.Instance.OnRoomStatusUpdated -= HandleRoomStatusUpdate;
		}
	}

	private void Start()
	{
		text.text = $"{startProgress:F0}%";
		GetComponent<Image>().fillAmount = 0;
	}

	//===========================================================
	// 协程
	//===========================================================

	/// <summary>
	/// 假加载 (0 → fakeProgress)
	/// </summary>
	private IEnumerator FakeLoadingRoutine()
	{
		float timer = 0f;

		while (timer < fakeLimit)
		{
			timer += Time.deltaTime;
			float t = Mathf.Clamp01(timer / fakeLimit);

			float progress = Mathf.Lerp(0, fakeProgress, t);

			text.text = $"{progress:F0}%";
			GetComponent<Image>().fillAmount = progress / endProgress;

			yield return null;

			// 如果玩家已经到齐，则跳过剩余 fake 加载
			if (hasStartedRealLoading)
				yield break;
		}
	}

	/// <summary>
	/// 真加载 (fakeProgress → 100)
	/// </summary>
	private IEnumerator RealLoadingRoutine()
	{
		float timer = 0f;

		while (timer < timeLimit)
		{
			timer += Time.deltaTime;
			float t = Mathf.Clamp01(timer / timeLimit);

			float progress = Mathf.Lerp(fakeProgress, endProgress, t);

			text.text = $"{progress:F0}%";
			GetComponent<Image>().fillAmount = progress / endProgress;

			yield return null;
		}

		// 加载完成
		OnLoadingEnd?.Invoke(true);

		gameObject.SetActive(false);

	}

	//===========================================================
	// 回调
	//===========================================================

	private void HandleRoomStatusUpdate(List<PlayerInfo> players)
	{
		if (!hasStartedRealLoading && players.Count >= 2)
		{
			Debug.Log("[客户端] 玩家到齐，开始真实 Loading");

			hasStartedRealLoading = true;

			// 停掉 fake 协程
			if (loadRoutine != null)
			{
				StopCoroutine(loadRoutine);
			}

			// 启动真实加载
			loadRoutine = StartCoroutine(RealLoadingRoutine());
		}
	}

	public void StartRealLoadingRoutine()
	{
		// 停掉 fake 协程
		if (loadRoutine != null)
		{
			StopCoroutine(loadRoutine);
		}

		// 启动真实加载
		loadRoutine = StartCoroutine(RealLoadingRoutine());
	}
}
