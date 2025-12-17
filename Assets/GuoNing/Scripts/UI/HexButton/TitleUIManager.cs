using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleUIManager : MonoBehaviour
{


	//--------------------------------------------------------------------------------
	// プロパティ
	//--------------------------------------------------------------------------------
	[Header("Menus")]
	public RectTransform RightMenu;
	public RectTransform LeftMenu;//12/17 拡張
	public Material mat;
	// World UI
	[Header("Left Menu")]
	public HexButton Button_EndGame;
	public HexButton Button_GameTutorial;
	public HexButton Button_SinglePlayer;
	public HexButton Button_OnlineGame;
	public HexButton Button_Setting;
	public HexButton Button_MapEditor;
	public HexButton Button_ExtraContents;//Gallery用 12/12 張

	[Header("Right Menu")]
	public RectTransform OptionMenu;
	public RectTransform OnlineMenu;
	public RectTransform RightDetailMenu;
	public HexButton Button_CloseRightPanel;

	[Header("Extra Contents")]
	public RectTransform ExtraContentsMenu;
	//public HexButton Button_CloseExtraContents;
	public Button Button_CloseExtraContents;//暫定 12/17 張
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

    //現在表示中の部族パネルのScrollRect
    private ScrollRect scrollRect;

    // タブの種類を定義
    public enum TribeTabType
	{
		History,
		Geography,
		Society,
		Culture
	}

	// Screen UI
	[Header("OnlineButton")]
	public HexButton Button_CreateGame;
	public HexButton Button_AddGame;
	public RectTransform UserID;
	public TMP_Text Text_UserID;


	[Header("Building")]
	public Transform Building;
	public Transform Lighting;
	public Transform pivot;  // 手动设置成与 Building 同位置

	[Header("DisplayComponent")]
	public TMP_Dropdown ResolutionDropdown;
	public TMP_Dropdown FullScreenDropdown;
	public Toggle GridToggle;

	[Header("SoundComponent")]
	public Slider MasterSlider;
	public Slider BGMSlider;
	public Slider SESlider;

	//--------------------------------------------------------------------------------
	// メソッド
	//--------------------------------------------------------------------------------
	



	void Update()
	{
		// Right mouse button click
		if (Input.GetMouseButton(1))
		{
			//  Close all option menu& online menu for next usage
			CloseRightPanel();



		}
		
	}

	// Start is called before the first frame update
	void Start()
	{
		// 将读取的存档信息加载到游戏中
		SaveLoadManager.Instance.Load();	// safe load
		// 读取信息
		SaveLoadManager.Instance.ApplyLoadedData();

		// 绑定按钮事件
		Button_EndGame.onClick.AddListener(() => OnClickEndGame());
		Button_SinglePlayer.onClick.AddListener(() => OnClickSinglePlayer());
		Button_OnlineGame.onClick.AddListener(() => OnClickOnlineGame());
		Button_Setting.onClick.AddListener(() => OnClickSetting());
		Button_MapEditor.onClick.AddListener(() => OnClickMapEditor());
		Button_ExtraContents.onClick.AddListener(() => OnClickExtraContents());//追加内容12/12

		Button_CreateGame.onClick.AddListener(() => OnClickCreateGame());
		Button_AddGame.onClick.AddListener(() => OnClickAddGame());

		Button_CloseRightPanel.onClick.AddListener(() => CloseRightPanel());
		Button_CloseExtraContents.onClick.AddListener(() => CloseExtraContents());

        // 部族ボタンのイベント登録　追加内容12/16
        Button_Silk.onClick.AddListener(() => ShowTribeDetail(Panel_Silk_History));
		Button_RedMoon.onClick.AddListener(() => ShowTribeDetail(Panel_RedMoon_History));
		Button_NGC1300.onClick.AddListener(() => ShowTribeDetail(Panel_NGC1300_History));
		Button_Researcher.onClick.AddListener(() => ShowTribeDetail(Panel_Researcher_History));
		Button_BackToTribeList.onClick.AddListener(() => BackToTribeList());
		Button_ScrollToTop.onClick.AddListener(() => ScrollToTop());
		Button_ScrollToTop.onClick.AddListener(() => ReturnClicked());

        // タブボタンのイベント登録		追加内容12/16
        Button_Tab_History.onClick.AddListener(() => SwitchTribeTab(TribeTabType.History));
		Button_Tab_Geography.onClick.AddListener(() => SwitchTribeTab(TribeTabType.Geography));
		Button_Tab_Society.onClick.AddListener(() => SwitchTribeTab(TribeTabType.Society));
		Button_Tab_Culture.onClick.AddListener(() => SwitchTribeTab(TribeTabType.Culture));
	
		//  Close all option menu& online menu for next usage
		OptionMenu.gameObject.SetActive(false);
		OnlineMenu.gameObject.SetActive(false);
		ExtraContentsMenu.gameObject.SetActive(false);
		UpdateBackground(false);

		// 全タブパネルを初期化時に非表示
		HideAllTribeTabPanels();

		// Reset button state
		Button_Setting.ResetHexButton();
		Button_OnlineGame.ResetHexButton();

		SoundManager.Instance.StopBGM();
		SoundManager.Instance.PlayBGM(SoundSystem.TYPE_BGM.TITLE, loop: true);

		// 更新分辨率设定
		ResolutionManager.Instance.InitializeResolutionDropDown(ResolutionDropdown);
		ResolutionManager.Instance.InitializeFullScreenDropDown(FullScreenDropdown);

		ResolutionDropdown.SetValueWithoutNotify(ResolutionManager.Instance.CurrentResolutionIndex);
		FullScreenDropdown.SetValueWithoutNotify(ResolutionManager.Instance.CurrentFullScreenIndex);

		// 更新Display
		DisplayManager.Instance.InitializeToggle(GridToggle);
		GridToggle.isOn = DisplayManager.Instance.IsGridOn;

		// 更新Slider
		MasterSlider.onValueChanged.AddListener((value) =>
		{
			SoundManager.Instance.SetMasterVolume(value);
			SoundManager.Instance.ApplyVolumes();
		});

		BGMSlider.onValueChanged.AddListener((value) =>
		{
			SoundManager.Instance.SetBGMVolume(value);
			SoundManager.Instance.ApplyVolumes();
		});

		SESlider.onValueChanged.AddListener((value) =>
		{
			SoundManager.Instance.SetSEVolume(value);
			SoundManager.Instance.ApplyVolumes();
		});

		MasterSlider.SetValueWithoutNotify(SoundManager.Instance.MasterVolume);
		BGMSlider.SetValueWithoutNotify(SoundManager.Instance.BGMVolume);
		SESlider.SetValueWithoutNotify(SoundManager.Instance.SEVolume);

		// 更新UserID显示
		if (SaveLoadManager.Instance.HasUserID)
		{
			Text_UserID.text = $"{SaveLoadManager.Instance.CurrentData.userID}";
		}
		else
		{
			Text_UserID.text = "Not Set";
		}


		//=======
		// Building 自转
		if (Building)
		{
			Building.DORotate(
					new Vector3(0, 360, 0),
					18f,
					RotateMode.FastBeyond360
				).SetEase(Ease.Linear)
				.SetLoops(-1);
		}

		// Lighting 围绕 Building 旋转
		if (Lighting)
		{
			pivot = new GameObject("LightingPivot").transform;
			pivot.position = Building.position;
			pivot.rotation = Quaternion.identity;

			Lighting.SetParent(pivot, true);

			// 40°/秒 = 9 秒一圈
			pivot.DORotate(
					new Vector3(0, 360, 0),
					9f,
					RotateMode.FastBeyond360
				).SetEase(Ease.Linear)
				.SetLoops(-1);
		}
	}



	private void CloseRightPanel()
	{
		RightMenu.gameObject.SetActive(false);
		//Button_CloseRightPanel.gameObject.SetActive(false);

		//OptionMenu.gameObject.SetActive(false);
		//OnlineMenu.gameObject.SetActive(false); 
		RightDetailMenu.gameObject.SetActive(false);

		UpdateBackground(false);

		//  Open UserID display
		UserID.gameObject.SetActive(true);

		Button_Setting.ResetHexButton();
		Button_OnlineGame.ResetHexButton();
	}



	private void OnClickMapEditor()
	{
		SceneController.Instance.SwitchScene("MapEditor", null);
	}

	/// <summary>
	/// End Game button event
	/// </summary>
	private void OnClickEndGame()
	{
		SaveLoadManager.Instance.UpdateSaveData();
		SaveLoadManager.Instance.Save();

#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
	#else
		Application.Quit();
	#endif
	}

	/// <summary>
	/// Single play button event
	/// </summary>
	private void OnClickSinglePlayer()
	{
		SceneStateManager.Instance.bIsSingle = true;
		SceneStateManager.Instance.StartSingleGameWithRandomMapAndReligion();
		SceneController.Instance.SwitchScene("MainGame", null);
	}

	/// <summary>
	/// Online game button event
	/// </summary>
	private void OnClickOnlineGame()
	{
		RightMenu.gameObject.SetActive(true);
		Button_CloseRightPanel.gameObject.SetActive(true);

		//  Set option menu active
		OnlineMenu.gameObject.SetActive(true);
		OptionMenu.gameObject.SetActive(false);

		// 关闭UserID显示
		UserID.gameObject.SetActive(false);

		// Change material
		UpdateBackground(true);
	}

	/// <summary>
	/// Setting event
	/// </summary>
	private void OnClickSetting()
	{
		RightMenu.gameObject.SetActive(true);
		//  Set option menu active
		OptionMenu.gameObject.SetActive(true);
		OnlineMenu.gameObject.SetActive(false);

		Button_CloseRightPanel.gameObject.SetActive(true);

		// Change material
		UpdateBackground(true);

		// 关闭UserID显示
		UserID.gameObject.SetActive(false);
	}

	/// <summary>
	/// Create online game button
	/// </summary>
	private void OnClickCreateGame()
	{
		if (SceneStateManager.Instance != null)
		{
			SceneStateManager.Instance.SetAsServer(true);
		}
		else
		{
			Debug.LogError("SceneStateManager.Instance ﾎｪｿﾕ!");
		}


		SceneController.Instance.SwitchScene("MainGame");
	}

	private void ActivateTribeButtons(){
		Button_Silk.gameObject.SetActive(true);
		Button_RedMoon.gameObject.SetActive(true);
		Button_NGC1300.gameObject.SetActive(true);
		Button_Researcher.gameObject.SetActive(true);	
	}

	private void DeactivateTribeButtons(){
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
    private void ActivateLeftMenu()
    {
        LeftMenu.gameObject.SetActive(true);
    }

    private void DeactivateLeftMenu()
	{
		LeftMenu.gameObject.SetActive(false);
	}

    ///<summary>
    ///Extra Contents ボタンイベント 12/12 張
    ///</summary>
    private void OnClickExtraContents()
	{
		// 全画面でExtraContentsメニューを表示
		ExtraContentsMenu.gameObject.SetActive(true);

		//念の為
		Panel_TribeList.gameObject.SetActive(true);
        DeactivateLeftMenu();

        //ギャラリーを閉じるボタンを有効化
        Button_CloseExtraContents.gameObject.SetActive(true);

		//各勢力のボタンを有効化
		ActivateTribeButtons();

        UserID.gameObject.SetActive(false);

        //Button_CloseExtraContents.ResetHexButton();// 12/17廃止
    }


	private void ReturnClicked(){
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

        //タブボタン無効化
        DeactivateTabButtons();

        // 全タブパネル非表示
        HideAllTribeTabPanels();

        // 現在の部族パネルをクリア
        currentTribePanel = null;

        //取得したScrollRectもクリア
        scrollRect = null;

        Button_BackToTribeList.gameObject.SetActive(false);

		ActivateLeftMenu();

        UserID.gameObject.SetActive(true);
    }

	///<summary>
	///部族詳細画面を表示
	///</summary>
	private void ShowTribeDetail(RectTransform tribePanel)
	{
		//Button_CloseExtraContents.gameObject.SetActive(false);
		//何時でもメインメニューに戻れるようにした

        //各勢力のボタンを無効化
        DeactivateTribeButtons();

        // 指定された部族パネルのみ表示
        tribePanel.gameObject.SetActive(true);

		// 現在の部族パネルを記録
		currentTribePanel = tribePanel;

		//タブボタン有効化
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
		//前回閲覧したタブのスクロールバー位置を最先頭にリセットする
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
        //前回閲覧したタブのスクロールバー位置を最先頭にリセットする
        scrollRect.verticalNormalizedPosition = 1.0f;

        //タブボタン無効化
        DeactivateTabButtons();

		// 全タブパネル非表示
		HideAllTribeTabPanels();

		// 現在の部族パネルをクリア
		currentTribePanel = null;

		//取得したScrollRectもクリア
		scrollRect=null;

        Button_BackToTribeList.gameObject.SetActive(false);
        Button_CloseExtraContents.gameObject.SetActive(true);//これ無くてもいいんだが

		ActivateTribeButtons();
        //Button_CloseExtraContents.ResetHexButton();
    }


    /// <summary>
    /// Join game button event
    /// </summary>
    private void OnClickAddGame()
	{
		
		if (SceneStateManager.Instance != null)
		{
			SceneStateManager.Instance.SetAsServer(false);
		}
		else
		{
			Debug.LogError("SceneStateManager.Instance is null!");
		}

		SceneController.Instance.SwitchScene("MainGame");

	}


	/// <summary>
	/// Update game background blur effect
	/// </summary>
	/// <param name="isOn"></param>
	private void UpdateBackground(bool isOn)
	{
		if (isOn)
		{
			mat.SetFloat("_UseMask", 1);
			mat.SetFloat("_BlurSize", 5);
		}
		else
		{
			mat.SetFloat("_UseMask", 0);
			mat.SetFloat("_BlurSize", 0);
		}
	}

}
