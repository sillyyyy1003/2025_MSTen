using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// シーン切り替え FadeIn/Out
/// </summary>
public class FadeManager : MonoBehaviour
{
	//======================================
	// メンバ変数
	//======================================
	public static FadeManager Instance;
	[Header("FadeSettings")]
	public Image fadeOverlay;	// Fade in/out mask
	public float fadeDuration;	// time for fade in/out
	public Ease fadeEase = Ease.InOutQuad;  //Easing

	private Sequence currentFadeSequence;


	//======================================
	// メソッド
	//======================================


	void Awake()
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

		// 确保初始状态
		fadeOverlay.gameObject.SetActive(false);
		fadeOverlay.color = new Color(0, 0, 0, 0);
	}

	/// <summary>
	/// Fade in black
	/// </summary>
	/// <param name="duration">Fade duration</param>
	/// <param name="onComplete">Events occur when fade complete</param>
	/// <returns></returns>
	/// Fade in black
	/// </summary>
	/// <param name="duration">Fade duration</param>
	/// <param name="onComplete">Events occur when fade complete</param>
	/// <returns></returns>
	public Tween FadeToBlack(float duration = -1, Action onComplete = null)
	{
		if (duration < 0) duration = fadeDuration;
		fadeOverlay.color = new Color(0, 0, 0, 0);
		fadeOverlay.gameObject.SetActive(true);

		return fadeOverlay.DOFade(1f, duration)
			.SetEase(fadeEase)
			.OnComplete(() => onComplete?.Invoke());
	}



	/// <summary>
	/// Fade out
	/// </summary>
	/// <param name="duration"></param>
	/// <param name="onComplete"></param>
	/// <returns></returns>
	public Tween FadeFromBlack(float duration = -1, Action onComplete = null)
	{
		if (duration < 0) duration = fadeDuration;
		fadeOverlay.color = new Color(0, 0, 0, 1);
		fadeOverlay.gameObject.SetActive(true);

		return fadeOverlay.DOFade(0f, duration)
			.SetEase(fadeEase)
			.OnComplete(() => {
				fadeOverlay.gameObject.SetActive(false);
				onComplete?.Invoke();
			});
	}



	public void TransitionFade(Action onMiddle = null, Action onComplete = null)
	{
		currentFadeSequence?.Kill(true);

		fadeOverlay.gameObject.SetActive(true);

		currentFadeSequence = DOTween.Sequence();

		currentFadeSequence
			.Append(FadeToBlack())
			.AppendCallback(() => onMiddle?.Invoke())
			.Append(FadeFromBlack())
			.OnComplete(() => {
				onComplete?.Invoke();
				currentFadeSequence = null;
			});
	}


}
