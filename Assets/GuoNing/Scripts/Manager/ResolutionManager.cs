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
	private int currentFullScrrenIndex;

	bool isFullScreen = true;

	public int CurrentResolutionIndex=> currentResolutionIndex;
	public int CurrentFullScreenIndex=> currentFullScrrenIndex;


	//--------------------------------------------------------------------------------
	// プロパティ
	//--------------------------------------------------------------------------------
	/// <summary>
	/// DropDown UI
	/// </summary>
	//public TMP_Dropdown dropdown;
	/// <summary>
	/// Full screen dropdown
	/// </summary>
	//public TMP_Dropdown fullscreenModeDropdown;

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

		currentResolutionIndex = 0;			// default 1080p
		currentFullScrrenIndex = 0; // default full screen

		// 应用设置 (FULL HD/WINDOW)
		Screen.SetResolution(resolutionSettings[0].width, resolutionSettings[0].height, false);

	}

	public void InitializeResolutionDropDown(TMP_Dropdown dropdown)
	{

		List<string> options = new List<string>();

		for (int i = 0; i < resolutionSettings.Length; i++)
		{
			string optionLabel = resolutionSettings[i].width + " x " + resolutionSettings[i].height;
			options.Add(optionLabel);
		}

		dropdown.ClearOptions();
		dropdown.AddOptions(options);
		dropdown.value = currentResolutionIndex;
		dropdown.RefreshShownValue();
	}

	public void InitializeFullScreenDropDown(TMP_Dropdown dropdown)
	{
		List<string> fullscreenOptions = new List<string> { "フルスクリーン", "ウィンドウ", "ボーダーレス" };
		dropdown.ClearOptions();
		dropdown.AddOptions(fullscreenOptions);
		dropdown.value = currentFullScrrenIndex; // 默认全屏
		dropdown.RefreshShownValue();
	}



	public void OnChangeResolution(int index)
	{
		// Set resolution
		currentResolutionIndex = index;
		ResolutionSetting setting = resolutionSettings[index];

		// set full screen
		Screen.SetResolution(setting.width, setting.height, Screen.fullScreen = isFullScreen);
	}

	public void OnChangeFullScreenMode(int index)
	{
		FullScreenMode mode = FullScreenMode.Windowed;
		currentFullScrrenIndex = index;
		switch (currentFullScrrenIndex)
		{
			case 0: mode = FullScreenMode.FullScreenWindow; break;
			case 1: mode = FullScreenMode.Windowed; break;
			case 2: mode = FullScreenMode.ExclusiveFullScreen; break;
		}
		Screen.fullScreenMode = mode;
	}

	public void ApplyLoadedSettings(int resIndex, int fullIndex)
	{
		currentResolutionIndex = Mathf.Clamp(resIndex, 0, 2);
		currentFullScrrenIndex = Mathf.Clamp(fullIndex, 0, 2);

		ResolutionSetting setting = resolutionSettings[currentResolutionIndex];
		Screen.SetResolution(setting.width, setting.height, false);

		OnChangeFullScreenMode(currentFullScrrenIndex);
	}
}
