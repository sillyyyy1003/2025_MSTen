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
		//new ResolutionSetting(1600, 900),	// 900P
		//new ResolutionSetting(1280, 720),	// 720P
	};

	private int currentResolutionIndex;
	private int currentFullScrrenIndex;

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


		Screen.SetResolution(1920, 1080, true);
	}

	public void InitializeResolutionDropDown(TMP_Dropdown dropdown)
	{

		List<string> options = new List<string>();
		for (int i = 0; i < resolutionSettings.Length; i++)
		{
			options.Add($"{resolutionSettings[i].width} x {resolutionSettings[i].height}");
		}

		dropdown.ClearOptions();
		dropdown.AddOptions(options);
		dropdown.value = currentResolutionIndex;
		dropdown.RefreshShownValue();
	}

	public void InitializeFullScreenDropDown(TMP_Dropdown dropdown)
	{
		List<string> fullscreenOptions = new List<string>
		{
			
            "フルスクリーン",  // ExclusiveFullScreen
            //"ウィンドウ",       // Windowed
         
        };
		//   "ボーダーレス"      // FullScreenWindow
		dropdown.ClearOptions();
		dropdown.AddOptions(fullscreenOptions);
		dropdown.value = currentFullScrrenIndex;
		dropdown.RefreshShownValue();
	}



	public void OnChangeResolution(int index)
	{
		//currentResolutionIndex = index;
		//ResolutionSetting setting = resolutionSettings[index];

		//// 保持当前模式不变，仅改变分辨率
		//if(currentFullScrrenIndex == 1)
		//{
		//	// 全屏模式
		//	Screen.SetResolution(setting.width, setting.height, true);
		//}
		//else
		//{
		//	// 窗口模式
		//	Screen.SetResolution(setting.width, setting.height, false);
		//}

		//// 保存设定
		//SaveLoadManager.Instance.UpdateResolutionIndex(currentResolutionIndex);
	}

	public void OnChangeFullScreenMode(int index)
	{
		//currentFullScrrenIndex = index;
		//ResolutionSetting setting = resolutionSettings[currentResolutionIndex];

		//if (index == 1)
		//{
		//	// 使用最兼容的全屏模式
		//	Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
		//	// 全屏必须用 true
		//	Screen.SetResolution(setting.width, setting.height, true);
		//}
		//else
		//{
		//	Screen.fullScreenMode = FullScreenMode.Windowed;
		//	Screen.SetResolution(setting.width, setting.height, false);
		//}
		//// 保存设定
		//SaveLoadManager.Instance.UpdateResolutionIndex(currentFullScrrenIndex);
	}

	public void ApplyLoadedSettings(int resIndex, int fullIndex)
	{
		//currentResolutionIndex = Mathf.Clamp(resIndex, 0, resolutionSettings.Length - 1);
		//currentFullScrrenIndex = Mathf.Clamp(fullIndex, 0, 2);
		currentResolutionIndex = Mathf.Clamp(0, 0, resolutionSettings.Length - 1);
		currentFullScrrenIndex = Mathf.Clamp(0, 0, 2);

		// 先应用 fullscreen 模式
		OnChangeFullScreenMode(currentFullScrrenIndex);

		// 再应用分辨率
		OnChangeResolution(currentResolutionIndex);

	}
}
