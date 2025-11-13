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
	public TMP_Dropdown dropdown;

	bool isFullScreen = true;


	//--------------------------------------------------------------------------------
	// プロパティ
	//--------------------------------------------------------------------------------
	/// <summary>
	/// DropDown UI
	/// </summary>
	//public Dropdown resolutionDropdown;
	/// <summary>
	/// Full screen toggle
	/// </summary>
	public Toggle fullScreenToggle;

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

	public void OnChangeFullScreen(bool isFullScreen)
	{
		this.isFullScreen = isFullScreen;
		ResolutionSetting setting = resolutionSettings[currentResolutionIndex];
		Screen.SetResolution(setting.width, setting.height, Screen.fullScreen = isFullScreen);

		Debug.Log("Resolution changed to: " + Screen.width + " x " + Screen.height + ", FullScreen: " + Screen.fullScreen);
	}
	

}
