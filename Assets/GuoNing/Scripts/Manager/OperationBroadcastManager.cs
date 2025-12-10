using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OperationBroadcastManager : MonoBehaviour
{
	public static OperationBroadcastManager Instance { get; private set; }


	[Header("UI References")]
	[SerializeField] private RectTransform panelGroup;
	[SerializeField] private Image iconImage;         // 新增：图标
	[SerializeField] private TMP_Text messageText;    // 文本

	[Header("Settings")]
	public float fadeDuration = 0.25f;
	public float displayTime = 1.1f;
	public float interval = 0.2f;

	// 图标额外动画（可关）
	public bool playIconPunch = true;

	private Queue<string> messageQueue = new Queue<string>();
	private bool isPlaying = false;

	private string lastMsg = "";
	private float lastTime = 0;

	private void Awake()
	{
		if (Instance == null) Instance = this;
		else { Destroy(gameObject); return; }

		
		panelGroup.gameObject.SetActive(false);

		if (iconImage != null)
			iconImage.color = new Color(1, 1, 1, 0);
	}

	// -------------------------------------------------------
	//   外部调用入口（其他脚本用）
	// -------------------------------------------------------
	public void ShowMessage(string msg)
	{
		// 防止 spam
		if (Time.time - lastTime < 0.8f && msg == lastMsg)
			return;

		lastMsg = msg;
		lastTime = Time.time;

		messageQueue.Enqueue(msg);

		if (!isPlaying)
			StartCoroutine(PlayQueue());
	}

	// -------------------------------------------------------
	//   队列处理主协程
	// -------------------------------------------------------
	private IEnumerator PlayQueue()
	{
		isPlaying = true;

		while (messageQueue.Count > 0)
		{
			string msg = messageQueue.Dequeue();
			panelGroup.gameObject.SetActive(true);

			messageText.text = msg;

			// 图标 & 文本一开始是透明的
			iconImage.color = new Color(iconImage.color.r, iconImage.color.g, iconImage.color.b, 0);
			messageText.alpha = 0;

			// 创建动画序列
			Sequence seq = DOTween.Sequence();

			// ① 淡入图标 & 文本
			seq.Join(iconImage.DOFade(1, fadeDuration));
			seq.Join(messageText.DOFade(1, fadeDuration));

			// Icon Punch 效果（可选）
			if (playIconPunch)
			{
				iconImage.transform.localScale = Vector3.one * 0.75f;
				seq.Join(iconImage.transform.DOScale(1f, 0.35f).SetEase(Ease.OutBack));
			}

			// ② 停留
			seq.AppendInterval(displayTime);

			// ③ 淡出
			seq.Append(iconImage.DOFade(0, fadeDuration));
			seq.Join(messageText.DOFade(0, fadeDuration));

			// 等待播放完毕
			yield return seq.WaitForCompletion();

			panelGroup.gameObject.SetActive(false);
			yield return new WaitForSeconds(interval);
		}

		isPlaying = false;
	}
}
