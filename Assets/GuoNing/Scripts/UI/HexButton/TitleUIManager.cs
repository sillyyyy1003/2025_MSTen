using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
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
	public RectTransform LeftMenu;
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

	// Screen UI
	[Header("OnlineButton")]
	public HexButton Button_CreateGame;
	public HexButton Button_AddGame;

	[Header("Building")]
	/// <summary>
	/// 画面に表示する建物モデル
	/// </summary>
	public Transform Building;
	public Transform Lighting;

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
			OptionMenu.gameObject.SetActive(false);
			OnlineMenu.gameObject.SetActive(false);
			UpdateBackground(false);

			// Reset button state
			Button_Setting.ResetHexButton();
			Button_OnlineGame.ResetHexButton();
			SoundManager.Instance.PlaySE(SoundSystem.TYPE_SE.CHARMED);
		}

		//=========Building model update
		if (Building)
		{
			Building.Rotate(Vector3.up, 20f * Time.deltaTime);
		}

		if (Lighting)
		{
			Lighting.RotateAround(Building.position, Vector3.up, 40f * Time.deltaTime);
		}

		
	}

	// Start is called before the first frame update
	void Start()
	{
		Button_EndGame.onClick.AddListener(() => OnClickEndGame());
		Button_SinglePlayer.onClick.AddListener(() => OnClickSinglePlayer());
		Button_OnlineGame.onClick.AddListener(() => OnClickOnlineGame());
		Button_Setting.onClick.AddListener(() => OnClickSetting());
		Button_MapEditor.onClick.AddListener(() => OnClickMapEditor());

		Button_CreateGame.onClick.AddListener(() => OnClickCreateGame());
		Button_AddGame.onClick.AddListener(() => OnClickAddGame());
	
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
		Debug.Log("EndGame");
	}

	/// <summary>
	/// Single play button event
	/// </summary>
	private void OnClickSinglePlayer()
	{
		SceneStateManager.Instance.bIsSingle = true;
		SceneController.Instance.SwitchScene("MainGame", null);
	}

	/// <summary>
	/// Online game button event
	/// </summary>
	private void OnClickOnlineGame()
	{

		//  Set option menu active
		OnlineMenu.gameObject.SetActive(true);

		// Change material
		UpdateBackground(true);
	}

	/// <summary>
	/// Setting event
	/// </summary>
	private void OnClickSetting()
	{
		
		//  Set option menu active
		OptionMenu.gameObject.SetActive(true);
		OnlineMenu.gameObject.SetActive(false);

		// Change material
		UpdateBackground(true);

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


		SceneManager.LoadScene("MainGame");
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
		SceneManager.LoadScene("MainGame");

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


