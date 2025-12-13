using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class TitleAnimation : MonoBehaviour
{
	public Image titleImage;
	public float fadeDuration = 1f;   // 淡入/淡出时间
	public float stayDuration = 1f;   // 停留时间
	public UserIDPanel userIDPanel;
	void Start()
	{
		SaveLoadManager.Instance.Load();
		SaveLoadManager.Instance.ApplyLoadedData();
		PlayAnimation();
	}

	void PlayAnimation()
	{
		// 初始透明
		titleImage.color = new Color(1, 1, 1, 0);

		Sequence seq = DOTween.Sequence();

		seq.Append(titleImage.DOFade(1f, fadeDuration))   // 0 → 1
		   .AppendInterval(stayDuration)                 // 停顿
		   .Append(titleImage.DOFade(0f, fadeDuration)) // 1 → 0
		   .OnComplete(() =>
		   {
			   if (userIDPanel != null)
			   {
				   userIDPanel.OnAnimationEnd();
			   }
		   });

		seq.Play();
	}
}