using System.Collections;
using System.Collections.Generic;
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
	public RectTransform SoundMunu;
	public RectTransform OtherMenu;

	[Header("Sound Slider")]
	public Slider MasterSlider;
	public Slider BGMSlider;
	public Slider SESlider;

	[Header("Surrendor Window")]
	public RectTransform SurrenderComponent;

	private bool isOpen = false;

	void Start()
	{
		Menu.gameObject.SetActive(false);
		FirstLayer.gameObject.SetActive(false);
		SecondLayer.gameObject.SetActive(false);

		MasterSlider.SetValueWithoutNotify(SoundManager.Instance.MasterVolume);
		BGMSlider.SetValueWithoutNotify(SoundManager.Instance.BGMVolume);
		SESlider.SetValueWithoutNotify(SoundManager.Instance.SEVolume);
	}
	private void Update()
	{
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
		Menu.gameObject.SetActive(false);
	}


	public void OpenResolutionMenu()
	{
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
