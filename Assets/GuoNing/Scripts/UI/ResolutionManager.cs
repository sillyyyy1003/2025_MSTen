using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

struct ResolutionSetting
{
    public int width;
    public int height;

    public ResolutionSetting(int width, int height)
    {
        this.width = width;
        this.height = height;
	}
}


/// <summary>
/// Manager for handling screen resolutions
/// </summary>
public class ResolutionManager : MonoBehaviour
{
	//--------------------------------------------------------------------------------
	// メンバー変数
	//--------------------------------------------------------------------------------

	/// <summary>
	/// 
	/// </summary>
	private int currentIndex;
	/// <summary>
	/// Singleton instance
	/// </summary>
	public static ResolutionManager Instance;

	/// <summary>
	/// Customize resolution settings
	/// </summary>
	private ResolutionSetting[] resolutionSettings = new[]
	{
		new ResolutionSetting(1920, 1080),	// 1080P
		new ResolutionSetting(1600, 900),	// 900P
		new ResolutionSetting(1280, 720),	// 720P
	};

	private int currentResolutionIndex;
	private Resolution resolution;

	bool isFullScreen = true;


	//--------------------------------------------------------------------------------
	// プロパティ
	//--------------------------------------------------------------------------------
	/// <summary>
	/// DropDown UI
	/// </summary>
	public TMP_Dropdown dropdown;
	/// <summary>
	/// Full screen dropdown
	/// </summary>
	public TMP_Dropdown fullscreenModeDropdown;

	//--------------------------------------------------------------------------------
	// メソッド
	//--------------------------------------------------------------------------------

	void Awake()
	{
		// 防止重复
		if (Instance != null)
		{
			Destroy(gameObject);
			return;
		}
		Instance = this;
		DontDestroyOnLoad(gameObject); 
	}


	void Start()
	{
		resolution = new Resolution();
		List<string> options = new List<string>();
		currentIndex = 0;	// default 1080p

		for (int i = 0; i < resolutionSettings.Length; i++)
		{
			string optionLabel = resolutionSettings[i].width + " x " + resolutionSettings[i].height;
			options.Add(optionLabel);
		}


		// todo list：use save load function load settings
		dropdown.ClearOptions();
		dropdown.AddOptions(options);
		dropdown.value = currentIndex;
		dropdown.RefreshShownValue();


		List<string> fullscreenOptions = new List<string> { "フルスクリーン", "ウィンドウ", "ボーダーレス" };
		fullscreenModeDropdown.ClearOptions();
		fullscreenModeDropdown.AddOptions(fullscreenOptions);
		fullscreenModeDropdown.value = 0; // 默认全屏
		fullscreenModeDropdown.RefreshShownValue();


		// 应用设置 (FULL HD/WINDOW)
		//Screen.SetResolution(resolutionSettings[0].width, resolutionSettings[0].height,false);
		//fullScreenToggle.isOn = false;	// Set toggle false

	}

	public void OnChangeResolution(int index)
	{
		Debug.Log(index);
		// Set resolution
		currentResolutionIndex = index;
		ResolutionSetting setting = resolutionSettings[index];

		// set full screen
		Screen.SetResolution(setting.width, setting.height, Screen.fullScreen = isFullScreen);

		Debug.Log("Resolution changed to: " + Screen.width + " x " + Screen.height + ", FullScreen: " + isFullScreen);
	}

	public void OnChangeFullScreenMode(int index)
	{
		FullScreenMode mode = FullScreenMode.Windowed;
		switch (index)
		{
			case 0: mode = FullScreenMode.ExclusiveFullScreen; break;
			case 1: mode = FullScreenMode.Windowed; break;
			case 2: mode = FullScreenMode.FullScreenWindow; break;
		}

		ResolutionSetting setting = resolutionSettings[currentResolutionIndex];
		Screen.fullScreenMode = mode;
	}


}
