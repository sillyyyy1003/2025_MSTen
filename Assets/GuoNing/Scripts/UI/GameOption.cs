using System.Collections;
using System.Collections.Generic;
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

	[Header("UI Component")]
	public RectTransform ResulotionMenu;
	public Button ResolutionButton;
	public RectTransform SoundMunu;
	public RectTransform OtherMenu;

	[Header("Sound Slider")]
	public Slider MasterSlider;
	public Slider BGMSlider;
	public Slider SESlider;

	[Header("Surrender Window")]
	public Button SurrenderConfirmButton;
	public RectTransform SurrenderComponent;

	[Header("SynchroMenu")] 
	public TMP_Dropdown resolutionDropdown;
	public TMP_Dropdown fullScreenDropdown;
	public Toggle gridToggle;

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

	
	}
	private void Update()
	{
		// Press Escape to open/close menu
		if (Input.GetKeyUp(KeyCode.Escape))
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
	}

	public void CloseSecondLayer()
	{
		FirstLayer.gameObject.SetActive(true);
		SecondLayer.gameObject.SetActive(false);
	}


	public void OpenResolutionMenu()
	{
		ResolutionButton.Select();
		ResulotionMenu.gameObject.SetActive(true);
		SoundMunu.gameObject.SetActive(false);
		OtherMenu.gameObject.SetActive(false);
	}

	public void OpenSoundMenu()
	{
		ResulotionMenu.gameObject.SetActive(false);
		SoundMunu.gameObject.SetActive(true);
		OtherMenu.gameObject.SetActive(false);
	}

	public void OpenOtherMenu()
	{
		ResulotionMenu.gameObject.SetActive(false);
		SoundMunu.gameObject.SetActive(false);
		OtherMenu.gameObject.SetActive(true);
	}

	public void OpenSettingMenu()
	{
		FirstLayer.gameObject.SetActive(false);
		SecondLayer.gameObject.SetActive(true);

		OpenResolutionMenu();	// Open default menu

	}
}
