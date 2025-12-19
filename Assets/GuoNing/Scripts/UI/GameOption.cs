using System;
using System.Collections;
using System.Collections.Generic;
using SoundSystem;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class GameOption : MonoBehaviour
{
	public RectTransform Menu;

	[Header("Layers")]
	public RectTransform FirstLayer;
	public RectTransform SecondLayer;

	[Header("Buttons")]
	public Button SettingButton;
	public Button SurrenderButton;
	public Button BackToFirstLayerButton;
	public Button BackToGameButton;
	public Button BackToSelectSceneButton;
	
	public Toggle ResolutionButton;
	public Toggle SoundButton;
	public Toggle OtherButton;

	[Header("UI Component")]
	public RectTransform ResolutionMenu;
	public RectTransform SoundMenu;
	public RectTransform OtherMenu;


	[Header("Sound Slider")]
	public Slider MasterSlider;
	public Slider BGMSlider;
	public Slider SESlider;

	[Header("Surrender Window")]
	public RectTransform SurrenderComponent;

	[Header("BackToMainSceneWindow")]
	public RectTransform BackToSelectSceneComponent;

	[Header("SynchroMenu")] 
	public TMP_Dropdown resolutionDropdown;
	public TMP_Dropdown fullScreenDropdown;
	public Toggle gridToggle;

	public RectTransform EndTurnButton;
	private bool isOpen = false;

	void Start()
	{
		Menu.gameObject.SetActive(false);
		FirstLayer.gameObject.SetActive(false);
		SecondLayer.gameObject.SetActive(false);

		// 同步音量Slider
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

		// 同步Grid
		DisplayManager.Instance.InitializeToggle(gridToggle);
		gridToggle.SetIsOnWithoutNotify(DisplayManager.Instance.IsGridOn);

		// 同步resolution drop down
		ResolutionManager.Instance.InitializeResolutionDropDown(resolutionDropdown);
		resolutionDropdown.SetValueWithoutNotify(ResolutionManager.Instance.CurrentResolutionIndex);

		// 同步full screen drop down
		ResolutionManager.Instance.InitializeFullScreenDropDown(fullScreenDropdown);
		fullScreenDropdown.SetValueWithoutNotify(ResolutionManager.Instance.CurrentFullScreenIndex);

		// 绑定按钮
		SettingButton.onClick.AddListener(OpenSettingMenu);
		SurrenderButton.onClick.AddListener(OpenSurrenderMenu);
		BackToGameButton.onClick.AddListener(CloseMenu);
		BackToFirstLayerButton.onClick.AddListener(CloseSecondLayer);
		BackToSelectSceneButton.onClick.AddListener(OpenBackToMainSceneMenu);


		SoundButton.onValueChanged.AddListener(OnSoundToggleValueChanged);
		ResolutionButton.onValueChanged.AddListener(OnResolutionToggleValueChanged);
		OtherButton.onValueChanged.AddListener(OnOtherToggleValueChanged);
	

	}
	private void Update()
	{
		// Press Escape to open/close menu
		if (Input.GetKeyUp(KeyCode.Escape))
		{
			DoMenu();
		}
	}
	
	/// <summary>
	/// Open Menu first layer
	/// </summary>
	public void OpenMenu()
	{
		isOpen = !isOpen;

		GameManage.Instance.SetIsGamingOrNot(false);
		Debug.Log(GameManage.Instance.GetIsGamingOrNot());

		Menu.gameObject.SetActive(true);
		FirstLayer.gameObject.SetActive(true);
		SecondLayer.gameObject.SetActive(false);
		EndTurnButton.gameObject.SetActive(false);
	}

	/// <summary>
	/// Close this menu
	/// </summary>
	public void CloseMenu()
	{
		isOpen = !isOpen;
		GameManage.Instance.SetIsGamingOrNot(true);
		Debug.Log(GameManage.Instance!=null);
		Menu.gameObject.SetActive(false);
		EndTurnButton.gameObject.SetActive(true);

		// PlaySe
		SoundManager.Instance.PlaySE(TYPE_SE.BUTTONCLICKED);
	}

	public void DoMenu()
	{
		if (isOpen)
		{
			CloseMenu();
		}
		else
		{
			OpenMenu();
		}
	}

	public void CloseSecondLayer()
	{
		FirstLayer.gameObject.SetActive(true);
		SecondLayer.gameObject.SetActive(false);
		// PlaySe
		SoundManager.Instance.PlaySE(TYPE_SE.BUTTONCLICKED);
	}

	

	private void OnResolutionToggleValueChanged(bool isOn)
	{
		ResolutionMenu.gameObject.SetActive(isOn);
		SoundMenu.gameObject.SetActive(!isOn);
		OtherMenu.gameObject.SetActive(!isOn);

		// PlaySe
		SoundManager.Instance.PlaySE(TYPE_SE.BUTTONCLICKED);
	}

	private void OnSoundToggleValueChanged(bool isOn)
	{
		ResolutionMenu.gameObject.SetActive(!isOn);
		SoundMenu.gameObject.SetActive(isOn);
		OtherMenu.gameObject.SetActive(!isOn);

		// PlaySe
		SoundManager.Instance.PlaySE(TYPE_SE.BUTTONCLICKED);
	}

	private void OnOtherToggleValueChanged(bool isOn)
	{
		ResolutionMenu.gameObject.SetActive(!isOn);
		SoundMenu.gameObject.SetActive(!isOn);
		OtherMenu.gameObject.SetActive(isOn);

		// PlaySe
		SoundManager.Instance.PlaySE(TYPE_SE.BUTTONCLICKED);
	}

	public void OpenResolutionMenu()
	{
		ResolutionButton.Select();
		ResolutionMenu.gameObject.SetActive(true);
		SoundMenu.gameObject.SetActive(false);
		OtherMenu.gameObject.SetActive(false);

	}

	private void OpenSoundMenu()
	{
		ResolutionMenu.gameObject.SetActive(false);
		SoundMenu.gameObject.SetActive(true);
		OtherMenu.gameObject.SetActive(false);
	}

	private void OpenOtherMenu()
	{
		ResolutionMenu.gameObject.SetActive(false);
		SoundMenu.gameObject.SetActive(false);
		OtherMenu.gameObject.SetActive(true);
	}

	private void OpenSettingMenu()
	{
		FirstLayer.gameObject.SetActive(false);
		SecondLayer.gameObject.SetActive(true);

		OpenResolutionMenu();	// Open default menu

		// PlaySe
		SoundManager.Instance.PlaySE(TYPE_SE.BUTTONCLICKED);
	}

	private void OpenSurrenderMenu()
	{
		SurrenderComponent.gameObject.SetActive(true);
		// PlaySe
		SoundManager.Instance.PlaySE(TYPE_SE.BUTTONCLICKED);
	}

	private void OpenBackToMainSceneMenu()
	{
		BackToSelectSceneComponent.gameObject.SetActive(true);
		// PlaySe
		SoundManager.Instance.PlaySE(TYPE_SE.BUTTONCLICKED);
	}
}
