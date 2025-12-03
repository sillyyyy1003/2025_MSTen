using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

/// <summary>
/// ゲーム仮ロードUI
/// </summary>
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
	public SpriteRenderer spriteRender;


	private bool hasStartedRealLoading = false;
	private Tween fakeLoadingTween;
	private Tween realLoadingTween;

	//public event Action<bool> OnLoadingEnd;

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

	}



	//===========================================================
	// 协程
	//===========================================================

	public void StartFakeLoading(bool isSingle = false)
	{

		// 防止重复播放
		fakeLoadingTween?.Kill();
		fakeLoadingTween = DOTween.To(
				() => 0f,                   // 起点
				value =>
				{
					// 更新 UI 数字与进度
					text.text = $"{value:F0}%";
				},
				fakeProgress,               // 终点
				fakeLimit                   // 时间
			)
			.SetEase(Ease.Linear)
			.OnComplete(() =>
			{
				Debug.Log("[客户端] 假 Loading 完成，等待玩家到齐");
				if (isSingle) StartRealLoading(isSingle);
			});
	}



	public void StartRealLoading(bool isSingle=false)
	{
		// 防止重复播放
		realLoadingTween?.Kill();
		GameManage.Instance._GameCamera.SetCanUseCamera(true);  // 设置摄像头可用
		float start = fakeProgress;
		realLoadingTween = DOTween.To(
				() => start,
				value =>
				{
					start = value;

					// 更新 UI
					text.text = $"{value:F0}%";
				},
				endProgress,
				timeLimit
			)
			.SetEase(Ease.Linear)
			.OnComplete(() =>
			{
				GameManage.Instance._GameCamera.SetCanUseCamera(false);
				Debug.Log("[客户端] 真实 Loading 完成，开始淡出动画");
				gameObject.SetActive(false);					// 关闭Loading数字
				spriteRender.gameObject.SetActive(false);		// 关闭黑屏遮罩
				GameSceneUIManager.Instance.OnGameStarted();   // 通知游戏场景UI管理器游戏开始 显示游戏UI

				FadeManager.Instance.FadeFromBlack(1, () =>
				{

					GameManage.Instance.SetIsGamingOrNot(true);             // 设置为游戏中状态
					GameManage.Instance._GameCamera.SetCanUseCamera(true);  // 设置摄像头可用
					if(!isSingle)GameManage.Instance.StartFirstTurn();      // 开始第一回合
					//======在这里追加其他的游戏开始操作

					// 2025.11.14 Guoning 播放音乐
					SoundManager.Instance.PlayGameMusic();
				});


			});
	}





	//===========================================================
	// 回调
	//===========================================================

	/// <summary>
	/// 当NetSystem房间状态更新时调用一次，让客户端玩家准备完成
	/// </summary>
	/// <param name="players"></param>
	private void HandleRoomStatusUpdate(List<PlayerInfo> players)
	{
		if (!hasStartedRealLoading && players.Count >= 2)
		{
			Debug.Log("[客户端] 玩家到齐，开始真实 Loading");

			hasStartedRealLoading = true;

			NetGameSystem.Instance?.SetReadyStatus(true);
		}
	}



}
