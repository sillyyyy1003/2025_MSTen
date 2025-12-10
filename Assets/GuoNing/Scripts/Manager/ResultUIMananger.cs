using DG.Tweening;
using GameData;
using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
// ReSharper disable All

public class ResultUIManager : MonoBehaviour
{
	public static ResultUIManager Instance;
	[Header("UIImage")]
	public Image ReligionIcon;  // 获胜宗教Icon
	public Image ReusltBar;     // 结果条

	[Header("UILayers")]
	public RectTransform ResultLayer;
	public RectTransform ResultDetail;
	public RectTransform GameUI;
	public RectTransform GameOperationMenu;

	[Header("Buttons")]
	public Button ResultDetailButton;
	public Button GameExitButton;

	[Header("ResultComponent")]
	public ResultDetailUI ResultDetailUIComponent;
	public RectTransform ResultDetailTitle;

	private List<ResultDetailUI> resultDetailUIs = new List<ResultDetailUI>();

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else
		{
			Destroy(gameObject);
		}

	}

	private void Start()
	{
		ResultLayer.gameObject.SetActive(false);

		ResultDetailButton.onClick.AddListener(OnClickResultDetailButton);
		GameExitButton.onClick.AddListener(OnClickGameExitButton);
	}

	private void Update()
	{
		
		
	}

	public void Initialize(int victoryID)
	{
		// 设定胜利还是失败
		int victoryPlayerId = victoryID;	
		int localPlayerId = GameManage.Instance.LocalPlayerID;

		if (victoryPlayerId == localPlayerId)
		{
			ReusltBar.sprite = UISpriteHelper.Instance.GetSubSprite(UISpriteID.Result_Bar, "Result_VictoryBar");
		}
		else
		{
			ReusltBar.sprite = UISpriteHelper.Instance.GetSubSprite(UISpriteID.Result_Bar, "Result_DefeatBar");
		}

		// 设定宗教Icon
		int spriteSerial = (int)GameManage.Instance.GetPlayerData(victoryPlayerId).PlayerReligion - 1;
		ReligionIcon.sprite = UISpriteHelper.Instance.GetSubSprite(UISpriteID.IconList_Religion, spriteSerial);

		//List<ResultData> datas = GameManage.Instance.GetResultDatas();
		// 初始化Detail数据
		//InitResultData(datas);

		// 暂时代用
		List<ResultData> datas = new List<ResultData>();
		ResultData data1 = new ResultData(
			"Player1",
			25,
			15,
			5,
			10,
			2,
			3,
			100,
			80
			);
		datas.Add(data1);
		ResultData data2 = new ResultData(
			"Player2",
			20,
			10,
			3,
			5,
			1,
			1,
			90,
			80
			);
		datas.Add(data2);

		InitResultData(datas);
		

		// 设定演出
		OpenResultPanel();

	}

	private void OnClickResultDetailButton()
	{
		// 动画开始前先隐藏 Detail
		ResultDetail.gameObject.SetActive(false);
		ResultDetailButton.gameObject.SetActive(false);

		// Icon 与 Bar 的 RectTransform
		RectTransform iconRt = ReligionIcon.rectTransform;
		RectTransform barRt = ReusltBar.rectTransform;

		// ---------------------------
		// 目标位置：统一升到 Y = 280
		// ---------------------------
		float targetY = 280f;
		Vector2 iconTargetPos = new Vector2(iconRt.anchoredPosition.x, targetY);
		Vector2 barTargetPos = new Vector2(barRt.anchoredPosition.x, 100);

		// ---------------------------
		// 1) Icon & Bar 动画
		// ---------------------------
		Sequence seq = DOTween.Sequence();

		// Icon：移动 + 缩小
		seq.Append(iconRt.DOAnchorPos(iconTargetPos, 0.75f).SetEase(Ease.OutCubic));
		seq.Join(iconRt.DOScale(0.85f, 0.75f).SetEase(Ease.OutBack));

		// Bar：只移动（不缩放）
		seq.Join(barRt.DOAnchorPos(barTargetPos, 0.75f).SetEase(Ease.OutCubic));

		// ---------------------------
		// Icon & Bar 动画完成 → 显示 Detail
		// ---------------------------
		seq.AppendCallback(() =>
		{
			// 显示Detail节点
			ResultDetail.gameObject.SetActive(true);

			// ----- Title -----
			CanvasGroup titleGroup = ResultDetailTitle.GetComponent<CanvasGroup>();
			if (titleGroup == null) titleGroup = ResultDetailTitle.gameObject.AddComponent<CanvasGroup>();
			titleGroup.alpha = 0f;
			titleGroup.DOFade(1f, 0.5f).SetEase(Ease.OutCubic);

			// ----- DetailUI: 初始化透明 -----
			foreach (var ui in resultDetailUIs)
			{
				CanvasGroup g = ui.GetComponent<CanvasGroup>();
				if (g == null) g = ui.gameObject.AddComponent<CanvasGroup>();
				g.alpha = 0f;
				ui.gameObject.SetActive(true);
			}
		});

		// ---------------------------
		// 逐条淡入 DetailUI
		// ---------------------------
		seq.AppendInterval(0.1f);

		foreach (var ui in resultDetailUIs)
		{
			CanvasGroup g = ui.GetComponent<CanvasGroup>();
			seq.Append(g.DOFade(1f, 0.3f).SetEase(Ease.OutCubic));
			seq.AppendInterval(0.05f);
		}

		seq.Play();

	}

	private void OnClickGameExitButton()
	{
		Debug.Log("Exit to Title Scene");
		SceneController.Instance.SwitchToTitleScene();
	}

	private int GetVictoryPlayerIdBySurrender()
	{
		int localId = GameManage.Instance.LocalPlayerID;
		int enemyId = GameManage.Instance.OtherPlayerID;
		return enemyId;

	}

	public void Surrender()
	{
		// 如果单人模式直接结算 否则发送投降消息
		if (SceneStateManager.Instance.bIsSingle)
		{
			// 初始化ui并进行结算
			Initialize(GameManage.Instance.LocalPlayerID);
		}
		else
		{
			//发送投降消息
			int localId = GameManage.Instance.LocalPlayerID;
			NetGameSystem.Instance.SendGameOverMessage(GetVictoryPlayerIdBySurrender(), localId, "surrender");
		}

	}

	public void OpenResultPanel()
	{
		ResultLayer.gameObject.SetActive(true);

		// 关闭GameUI	
		GameUI.gameObject.SetActive(false);

		// 关闭GameOperationMenu
		GameOperationMenu.gameObject.SetActive(false);

		// 关闭Detail界面
		ResultDetail.gameObject.SetActive(false);

		// ResultButton显示
		ResultDetailButton.gameObject.SetActive(true);
	}

	private void InitResultData(List<ResultData> data)
	{
		// 1. 获取 Title 的 RectTransform
		RectTransform titleRt = ResultDetailTitle.GetComponent<RectTransform>();

		// Title 的高度
		float titleHeight = titleRt.rect.height;

		// 你想在 Title 与第一个 UI 之间留多少距离
		float padding = 20f;

		// UI 的高度（按你的 prefab 实际高度填写）
		float itemHeight = 100f;

		for (int i = 0; i < data.Count; i++)
		{
			// 生成 UI
			ResultDetailUI detailUI = Instantiate(ResultDetailUIComponent, ResultDetail);

			//Religion religion = GameManage.Instance.GetPlayerData(GameManage.Instance.GetAllPlayerIds()[i]).PlayerReligion;
			Religion religion = Religion.RedMoonReligion;
			detailUI.Initialize(data[i], religion);

			resultDetailUIs.Add(detailUI);

			RectTransform rt = detailUI.GetComponent<RectTransform>();

			//------------- 核心：位置计算 -------------
			// y = -(titleHeight + padding) 作为起点
			// 再往下排 itemHeight * i
			float startY = -(titleHeight + padding);
			rt.anchoredPosition = new Vector2(0, startY - itemHeight * i);
			//-----------------------------------------

			// 用于淡入动画
			detailUI.gameObject.SetActive(false);
		}
	}
}

