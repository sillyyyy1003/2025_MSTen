using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using DG.Tweening;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityEngine.UIElements.Experimental;

public class ExtraContentsManager : MonoBehaviour
{
	//--------------------------------------------------------------------------------
	// プロパティ
	//--------------------------------------------------------------------------------
	[Header("Extra Contents UI")]
	public RectTransform ExtraContentsMenu;
	public Button Button_CloseExtraContents;
	public RectTransform Panel_TribeList;
    public RectTransform Panel_MapSummary;

    [Header("Map Detail Display")]
    public RectTransform Panel_BlackOverlay;
    public Image Image_MapDetail;
    public Button Button_CloseMapDetail;

    [Header("Tab Buttons")]
	public Button Button_TribeTab;
    public Button Button_MapTab;

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

	// 現在表示中の部族/マップパネルのScrollRect
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
    // Map Tab System
    //--------------------------------------------------------------------------------
    [Header("Map Buttons")]
    public Button FertileFieldOfTheFanLake; // 扇湖の沃野
    public Button WildFieldsOfDividingRivers; // 分流する沃野
    public Button TheWhiteTundraCitadel; // 白原の孤城
    public Button BasinOfCalamity; // 災厄の盆地
    public Button PrimitivePlains; // 原始の平原
    public Button TheGoldenCorridor; // 黄金の回廊
    public Button TheFertileCore; // 豊穣の中核
    public Button TheBorderSanctuary; // 境地の聖域
    public Button HeartOfTheEmeraldGrove; // 翠林の心臓
    public Button PlainsOfTheTwinLakes; // 双湖の平原

    [Header("Map Detail Images")]
    public Sprite Sprite_FertileFieldOfTheFanLake; // 扇湖の沃野
    public Sprite Sprite_WildFieldsOfDividingRivers; // 分流する沃野
    public Sprite Sprite_TheWhiteTundraCitadel; // 白原の孤城
    public Sprite Sprite_BasinOfCalamity; // 災厄の盆地
    public Sprite Sprite_PrimitivePlains; // 原始の平原
    public Sprite Sprite_TheGoldenCorridor; // 黄金の回廊
    public Sprite Sprite_TheFertileCore; // 豊穣の中核
    public Sprite Sprite_TheBorderSanctuary; // 境地の聖域
    public Sprite Sprite_HeartOfTheEmeraldGrove; // 翠林の心臓
    public Sprite Sprite_PlainsOfTheTwinLakes; // 双湖の平原

	private RectTransform currentMapTab;

    //--------------------------------------------------------------------------------
    // メソッド
    //--------------------------------------------------------------------------------

    void Start()
	{
		// イベント登録
		Button_CloseExtraContents.onClick.AddListener(() => CloseExtraContents());

		//部族/マップのタブボタン登録
		Button_TribeTab.onClick.AddListener(() => OnClickTribeTab());
		Button_MapTab.onClick.AddListener(() => OnClickMapSummary());

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

		// 各マップボタンのイベント登録
		FertileFieldOfTheFanLake.onClick.AddListener(() => ShowMapDetail(Sprite_FertileFieldOfTheFanLake));
		WildFieldsOfDividingRivers.onClick.AddListener(() => ShowMapDetail(Sprite_WildFieldsOfDividingRivers));
		TheWhiteTundraCitadel.onClick.AddListener(() => ShowMapDetail(Sprite_TheWhiteTundraCitadel));
		BasinOfCalamity.onClick.AddListener(() => ShowMapDetail(Sprite_BasinOfCalamity));
		PrimitivePlains.onClick.AddListener(() => ShowMapDetail(Sprite_PrimitivePlains));
		TheGoldenCorridor.onClick.AddListener(() => ShowMapDetail(Sprite_TheGoldenCorridor));
		TheFertileCore.onClick.AddListener(() => ShowMapDetail(Sprite_TheFertileCore));
		TheBorderSanctuary.onClick.AddListener(() => ShowMapDetail(Sprite_TheBorderSanctuary));
		HeartOfTheEmeraldGrove.onClick.AddListener(() => ShowMapDetail(Sprite_HeartOfTheEmeraldGrove));
		PlainsOfTheTwinLakes.onClick.AddListener(() => ShowMapDetail(Sprite_PlainsOfTheTwinLakes));

		// マップ拡大表示の×ボタンのイベント登録
		Button_CloseMapDetail.onClick.AddListener(() => CloseMapDetail());

        // 初期化
        //ExtraContentsMenu.gameObject.SetActive(false);

		// デフォルトで部族タブを表示、マップタブは非表示
		Panel_TribeList.gameObject.SetActive(true);
		Panel_MapSummary.gameObject.SetActive(false);

		// 閉じるボタンは常にアクティブ
		Button_CloseExtraContents.gameObject.SetActive(true);

		// マップ詳細表示UI要素を初期化時に非表示
		Panel_BlackOverlay.gameObject.SetActive(false);
		Image_MapDetail.gameObject.SetActive(false);
		Button_CloseMapDetail.gameObject.SetActive(false);

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
		if (scrollRect != null)
		{
			scrollRect.verticalNormalizedPosition = 1.0f;
		}

		ExtraContentsMenu.gameObject.SetActive(false);

        // タブボタン無効化
        DeactivateTribeTabButtons();

		// 全タブパネル非表示
		HideAllTribeTabPanels();

		// 現在の部族パネルをクリア
		currentTribePanel = null;

		// 取得したScrollRectもクリア
		scrollRect = null;

		Button_BackToTribeList.gameObject.SetActive(false);
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
        ActivateTribeTabButtons();

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
        DeactivateTribeTabButtons();

		// 全タブパネル非表示
		HideAllTribeTabPanels();

		// 現在の部族パネルをクリア
		currentTribePanel = null;

		// 取得したScrollRectもクリア
		scrollRect = null;

		Button_BackToTribeList.gameObject.SetActive(false);

		ActivateTribeButtons();
	}

	/// <summary>
	/// 部族タブから直接マップタブに行った際の後片付け
	/// </summary>
	private void TribeTabCleanUp()
	{
		currentTribePanel = null;
		if (scrollRect != null)
		{
            scrollRect.verticalNormalizedPosition = 1.0f;
            scrollRect = null;
        }
		

        // タブボタン無効化
        DeactivateTribeTabButtons();

        // 全タブパネル非表示
        HideAllTribeTabPanels();

		//四つの部族アイコンボタンを無効化
        DeactivateTribeButtons();
        
		Button_BackToTribeList.gameObject.SetActive(false);
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

	private void ActivateTribeTabButtons()
	{
		Button_Tab_History.gameObject.SetActive(true);
		Button_Tab_Geography.gameObject.SetActive(true);
		Button_Tab_Society.gameObject.SetActive(true);
		Button_Tab_Culture.gameObject.SetActive(true);
	}

	private void DeactivateTribeTabButtons()
	{
		Button_Tab_History.gameObject.SetActive(false);
		Button_Tab_Geography.gameObject.SetActive(false);
		Button_Tab_Society.gameObject.SetActive(false);
		Button_Tab_Culture.gameObject.SetActive(false);
	}

	///<summary>
	///部族タブクリック時の処理
	///</summary>
	private void OnClickTribeTab()
	{
		Debug.Log("TribeTabButton has been clicked!");
		// マップ拡大表示が開いている場合は閉じる
		CloseMapDetail();

		// マップ一覧画面とScrollRectをクリーンアップ
		if (scrollRect != null)
		{
			scrollRect.verticalNormalizedPosition = 1.0f;
			scrollRect = null;
		}

		// パネルを切り替え
		SetPanelActive(Panel_MapSummary, false);
		SetPanelActive(Panel_TribeList, true);

		// 部族ボタンを有効化
		ActivateTribeButtons();

		// ScrollToTopボタンは部族リストでは不要なので非表示
		Button_ScrollToTop.gameObject.SetActive(false);

		// 戻るボタンを非表示
		Button_BackToTribeList.gameObject.SetActive(false);
	}


	///<summary>
	///マップタブクリック時の処理
	///</summary>
	private void OnClickMapSummary()
	{
		TribeTabCleanUp();
        SetPanelActive(Panel_TribeList, false);

		SetPanelActive(Panel_MapSummary, true);
        Button_ScrollToTop.gameObject.SetActive(true);//念の為

		scrollRect = Panel_MapSummary.Find("Scroll View").GetComponent<ScrollRect>();
        Button_ScrollToTop.gameObject.SetActive(true);//ここも念の為
    }

	///<summary>
	///マップ画像を拡大表示
	///</summary>
	private void ShowMapDetail(Sprite mapSprite)
	{
		// 黒幕を表示
		Panel_BlackOverlay.gameObject.SetActive(true);

		// 拡大画像を表示し、Spriteを設定
		Image_MapDetail.gameObject.SetActive(true);
		Image_MapDetail.sprite = mapSprite;

		// ×ボタンを表示
		Button_CloseMapDetail.gameObject.SetActive(true);

		// ToTopボタンを無効化
		Button_ScrollToTop.gameObject.SetActive(false);
	}

	///<summary>
	///マップ拡大画像を閉じる
	///</summary>
	private void CloseMapDetail()
	{
		// 黒幕を非表示
		Panel_BlackOverlay.gameObject.SetActive(false);

		// 拡大画像を非表示
		Image_MapDetail.gameObject.SetActive(false);

		// ×ボタンを非表示
		Button_CloseMapDetail.gameObject.SetActive(false);

		// ToTopボタンを有効化
		Button_ScrollToTop.gameObject.SetActive(true);
	}
}
