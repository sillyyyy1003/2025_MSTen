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
	public Material mat;
	// World UI
	[Header("Left Menu")]
	public HexButton Button_EndGame;
	public HexButton Button_GameTutorial;
	public HexButton Button_SinglePlayer;
	public HexButton Button_OnlineGame;
	public HexButton Button_Setting;
	public HexButton Button_MapEditor;

	[Header("Right Menu")]
	public RectTransform OptionMenu;
	public RectTransform OnlineMenu;
	public RectTransform RightDetailMenu;
	public HexButton Button_CloseRightPanel;

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

		Button_CreateGame.onClick.AddListener(() => OnClickCreateGame());
		Button_AddGame.onClick.AddListener(() => OnClickAddGame());

		Button_CloseRightPanel.onClick.AddListener(() => CloseRightPanel());
	
		//  Close all option menu& online menu for next usage
		OptionMenu.gameObject.SetActive(false);
		OnlineMenu.gameObject.SetActive(false);
		UpdateBackground(false);

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


