using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ExtraContentsManager : MonoBehaviour
{
	//--------------------------------------------------------------------------------
	// プロパティ
	//--------------------------------------------------------------------------------
	[Header("Extra Contents UI")]
	public RectTransform ExtraContentsMenu;
	public Button Button_CloseExtraContents;
	public RectTransform Panel_TribeList;

	[Header("Tribe Buttons")]
	public Button Button_Silk;
	public Button Button_RedMoon;
	public Button Button_NGC1300;
	public Button Button_Researcher;

	[Header("Tribe Detail Panels")]
	public Button Button_BackToTribeList;
	public Button Button_ScrollToTop;

	//--------------------------------------------------------------------------------
	// Tribe Detail Tab System
	//--------------------------------------------------------------------------------
	[Header("Tribe Detail Tabs - Buttons")]
	public Button Button_Tab_History;
	public Button Button_Tab_Geography;
	public Button Button_Tab_Society;
	public Button Button_Tab_Culture;

	[Header("Tribe Detail Tabs - Silk")]
	public RectTransform Panel_Silk_History;
	public RectTransform Panel_Silk_Geography;
	public RectTransform Panel_Silk_Society;
	public RectTransform Panel_Silk_Culture;

	[Header("Tribe Detail Tabs - RedMoon")]
	public RectTransform Panel_RedMoon_History;
	public RectTransform Panel_RedMoon_Geography;
	public RectTransform Panel_RedMoon_Society;
	public RectTransform Panel_RedMoon_Culture;

	[Header("Tribe Detail Tabs - NGC1300")]
	public RectTransform Panel_NGC1300_History;
	public RectTransform Panel_NGC1300_Geography;
	public RectTransform Panel_NGC1300_Society;
	public RectTransform Panel_NGC1300_Culture;

	[Header("Tribe Detail Tabs - Researcher")]
	public RectTransform Panel_Researcher_History;
	public RectTransform Panel_Researcher_Geography;
	public RectTransform Panel_Researcher_Society;
	public RectTransform Panel_Researcher_Culture;

	// 現在表示中の部族を追跡
	private RectTransform currentTribePanel;

	// 現在表示中の部族パネルのScrollRect
	private ScrollRect scrollRect;

	// タブの種類を定義
	public enum TribeTabType
	{
		History,
		Geography,
		Society,
		Culture
	}

	//--------------------------------------------------------------------------------
	// メソッド
	//--------------------------------------------------------------------------------

	void Start()
	{
		// イベント登録
		Button_CloseExtraContents.onClick.AddListener(() => CloseExtraContents());

		// 部族ボタンのイベント登録
		Button_Silk.onClick.AddListener(() => ShowTribeDetail(Panel_Silk_History));
		Button_RedMoon.onClick.AddListener(() => ShowTribeDetail(Panel_RedMoon_History));
		Button_NGC1300.onClick.AddListener(() => ShowTribeDetail(Panel_NGC1300_History));
		Button_Researcher.onClick.AddListener(() => ShowTribeDetail(Panel_Researcher_History));
		Button_BackToTribeList.onClick.AddListener(() => BackToTribeList());
		Button_ScrollToTop.onClick.AddListener(() => ScrollToTop());
		Button_ScrollToTop.onClick.AddListener(() => ReturnClicked());

		// タブボタンのイベント登録
		Button_Tab_History.onClick.AddListener(() => SwitchTribeTab(TribeTabType.History));
		Button_Tab_Geography.onClick.AddListener(() => SwitchTribeTab(TribeTabType.Geography));
		Button_Tab_Society.onClick.AddListener(() => SwitchTribeTab(TribeTabType.Society));
		Button_Tab_Culture.onClick.AddListener(() => SwitchTribeTab(TribeTabType.Culture));

        // 初期化
        //ExtraContentsMenu.gameObject.SetActive(false);

        // 全タブパネルを初期化時に非表示
        HideAllTribeTabPanels();

		//一つの単独のシーンとする際は直接↓を実行する
		OpenExtraContents();
    }

	///<summary>
	///Extra Contents ボタンイベント
	///</summary>
	public void OpenExtraContents()
	{
		// 全画面でExtraContentsメニューを表示
		ExtraContentsMenu.gameObject.SetActive(true);

		// 念の為
		Panel_TribeList.gameObject.SetActive(true);

		// ギャラリーを閉じるボタンを有効化
		Button_CloseExtraContents.gameObject.SetActive(true);

		// 各勢力のボタンを有効化
		ActivateTribeButtons();
	}

	private void ReturnClicked()
	{
		Debug.Log("ToTop Button is clicked!!");
	}

	///<summary>
	///Extra Contents を閉じる
	///</summary>
	private void CloseExtraContents()
	{
		//if (scrollRect != null)
		//{
		//	scrollRect.verticalNormalizedPosition = 1.0f;
		//}

		//ExtraContentsMenu.gameObject.SetActive(false);

		//// タブボタン無効化
		//DeactivateTabButtons();

		//// 全タブパネル非表示
		//HideAllTribeTabPanels();

		//// 現在の部族パネルをクリア
		//currentTribePanel = null;

		//// 取得したScrollRectもクリア
		//scrollRect = null;

		//Button_BackToTribeList.gameObject.SetActive(false);
		SceneController.Instance.SwitchToTitleScene();
	}

	///<summary>
	///部族詳細画面を表示
	///</summary>
	private void ShowTribeDetail(RectTransform tribePanel)
	{
		// 各勢力のボタンを無効化
		DeactivateTribeButtons();

		// 指定された部族パネルのみ表示
		tribePanel.gameObject.SetActive(true);

		// 現在の部族パネルを記録
		currentTribePanel = tribePanel;

		// タブボタン有効化
		ActivateTabButtons();

		Button_ScrollToTop.gameObject.SetActive(true);
		scrollRect = currentTribePanel.Find("Scroll View").GetComponent<ScrollRect>();

		Button_BackToTribeList.gameObject.SetActive(true);
	}

	///<summary>
	///部族詳細のタブを切り替える
	///</summary>
	private void SwitchTribeTab(TribeTabType tabType)
	{
		// 前回閲覧したタブのスクロールバー位置を最先頭にリセットする
		scrollRect.verticalNormalizedPosition = 1.0f;

		// 現在の部族に対応する全タブパネルを非表示
		HideCurrentTribeTabPanels();

		// 指定されたタブのパネルのみ表示
		RectTransform targetPanel = GetTabPanel(currentTribePanel, tabType);
		Debug.Log($"targetPanel:{targetPanel}");
		if (targetPanel != null)
		{
			targetPanel.gameObject.SetActive(true);
			scrollRect = targetPanel.Find("Scroll View").GetComponent<ScrollRect>();
			Debug.Log($"totopButtonは生きてる？{Button_ScrollToTop.gameObject.activeSelf}");
			Debug.Log($"ScrollViewはどうなっている？{scrollRect.transform.parent.name}");
		}
	}

	///<summary>
	///文章のスクロールバーを最先頭へ戻す
	///</summary>
	private void ScrollToTop()
	{
		scrollRect.DOVerticalNormalizedPos(1.0f, 0.5f);
	}

	///<summary>
	///現在の部族の全タブパネルを非表示にする
	///</summary>
	private void HideCurrentTribeTabPanels()
	{
		if (currentTribePanel == null) return;

		if (currentTribePanel.gameObject.name.Contains("Silk"))
		{
			SetPanelActive(Panel_Silk_History, false);
			SetPanelActive(Panel_Silk_Geography, false);
			SetPanelActive(Panel_Silk_Society, false);
			SetPanelActive(Panel_Silk_Culture, false);
		}
		else if (currentTribePanel.gameObject.name.Contains("RedMoon"))
		{
			SetPanelActive(Panel_RedMoon_History, false);
			SetPanelActive(Panel_RedMoon_Geography, false);
			SetPanelActive(Panel_RedMoon_Society, false);
			SetPanelActive(Panel_RedMoon_Culture, false);
		}
		else if (currentTribePanel.gameObject.name.Contains("NGC1300"))
		{
			SetPanelActive(Panel_NGC1300_History, false);
			SetPanelActive(Panel_NGC1300_Geography, false);
			SetPanelActive(Panel_NGC1300_Society, false);
			SetPanelActive(Panel_NGC1300_Culture, false);
		}
		else if (currentTribePanel.gameObject.name.Contains("Researcher"))
		{
			SetPanelActive(Panel_Researcher_History, false);
			SetPanelActive(Panel_Researcher_Geography, false);
			SetPanelActive(Panel_Researcher_Society, false);
			SetPanelActive(Panel_Researcher_Culture, false);
		}
	}

	///<summary>
	///全部族の全タブパネルを非表示にする（初期化用）
	///</summary>
	private void HideAllTribeTabPanels()
	{
		// Silk
		SetPanelActive(Panel_Silk_History, false);
		SetPanelActive(Panel_Silk_Geography, false);
		SetPanelActive(Panel_Silk_Society, false);
		SetPanelActive(Panel_Silk_Culture, false);

		// RedMoon
		SetPanelActive(Panel_RedMoon_History, false);
		SetPanelActive(Panel_RedMoon_Geography, false);
		SetPanelActive(Panel_RedMoon_Society, false);
		SetPanelActive(Panel_RedMoon_Culture, false);

		// NGC1300
		SetPanelActive(Panel_NGC1300_History, false);
		SetPanelActive(Panel_NGC1300_Geography, false);
		SetPanelActive(Panel_NGC1300_Society, false);
		SetPanelActive(Panel_NGC1300_Culture, false);

		// Researcher
		SetPanelActive(Panel_Researcher_History, false);
		SetPanelActive(Panel_Researcher_Geography, false);
		SetPanelActive(Panel_Researcher_Society, false);
		SetPanelActive(Panel_Researcher_Culture, false);
	}

	///<summary>
	///部族とタブタイプから対応するパネルを取得
	///</summary>
	private RectTransform GetTabPanel(RectTransform tribePanel, TribeTabType tabType)
	{
		if (currentTribePanel.gameObject.name.Contains("Silk"))
		{
			return tabType switch
			{
				TribeTabType.History => Panel_Silk_History,
				TribeTabType.Geography => Panel_Silk_Geography,
				TribeTabType.Society => Panel_Silk_Society,
				TribeTabType.Culture => Panel_Silk_Culture,
				_ => null
			};
		}
		else if (currentTribePanel.gameObject.name.Contains("RedMoon"))
		{
			return tabType switch
			{
				TribeTabType.History => Panel_RedMoon_History,
				TribeTabType.Geography => Panel_RedMoon_Geography,
				TribeTabType.Society => Panel_RedMoon_Society,
				TribeTabType.Culture => Panel_RedMoon_Culture,
				_ => null
			};
		}
		else if (currentTribePanel.gameObject.name.Contains("NGC1300"))
		{
			return tabType switch
			{
				TribeTabType.History => Panel_NGC1300_History,
				TribeTabType.Geography => Panel_NGC1300_Geography,
				TribeTabType.Society => Panel_NGC1300_Society,
				TribeTabType.Culture => Panel_NGC1300_Culture,
				_ => null
			};
		}
		else if (currentTribePanel.gameObject.name.Contains("Researcher"))
		{
			return tabType switch
			{
				TribeTabType.History => Panel_Researcher_History,
				TribeTabType.Geography => Panel_Researcher_Geography,
				TribeTabType.Society => Panel_Researcher_Society,
				TribeTabType.Culture => Panel_Researcher_Culture,
				_ => null
			};
		}

		return null;
	}

	///<summary>
	///パネルのアクティブ状態を安全に設定（null チェック付き）
	///</summary>
	private void SetPanelActive(RectTransform panel, bool active)
	{
		if (panel != null)
		{
			panel.gameObject.SetActive(active);
		}
	}

	///<summary>
	///部族一覧に戻る
	///</summary>
	private void BackToTribeList()
	{
		// 前回閲覧したタブのスクロールバー位置を最先頭にリセットする
		scrollRect.verticalNormalizedPosition = 1.0f;

		// タブボタン無効化
		DeactivateTabButtons();

		// 全タブパネル非表示
		HideAllTribeTabPanels();

		// 現在の部族パネルをクリア
		currentTribePanel = null;

		// 取得したScrollRectもクリア
		scrollRect = null;

		Button_BackToTribeList.gameObject.SetActive(false);
		Button_CloseExtraContents.gameObject.SetActive(true);

		ActivateTribeButtons();
	}

	private void ActivateTribeButtons()
	{
		Button_Silk.gameObject.SetActive(true);
		Button_RedMoon.gameObject.SetActive(true);
		Button_NGC1300.gameObject.SetActive(true);
		Button_Researcher.gameObject.SetActive(true);
	}

	private void DeactivateTribeButtons()
	{
		Button_Silk.gameObject.SetActive(false);
		Button_RedMoon.gameObject.SetActive(false);
		Button_NGC1300.gameObject.SetActive(false);
		Button_Researcher.gameObject.SetActive(false);
	}

	private void ActivateTabButtons()
	{
		Button_Tab_History.gameObject.SetActive(true);
		Button_Tab_Geography.gameObject.SetActive(true);
		Button_Tab_Society.gameObject.SetActive(true);
		Button_Tab_Culture.gameObject.SetActive(true);
	}

	private void DeactivateTabButtons()
	{
		Button_Tab_History.gameObject.SetActive(false);
		Button_Tab_Geography.gameObject.SetActive(false);
		Button_Tab_Society.gameObject.SetActive(false);
		Button_Tab_Culture.gameObject.SetActive(false);
	}
}
