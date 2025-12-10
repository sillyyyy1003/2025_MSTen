using System;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


// 储存读取的数据
[System.Serializable]
public struct SaveData
{
	// Display
	public bool isGridOn;

	// Resolution
	public int resolutionIndex;
	public int fullscreenIndex;

	// Sound
	public float masterVolume;
	public float bgmVolume;
	public float seVolume;

	// Custom ID
	public string userID;
}


/// <summary>
/// 读取保存各类设置的管理器
/// </summary>

public class SaveLoadManager : MonoBehaviour
{
	public static SaveLoadManager Instance;

	private string savePath;

	private SaveData currentData;
	private bool isLoadData = false;
	public bool IsLoadData => isLoadData;
	public String UserId => CurrentData.userID;
	public SaveData CurrentData
	{
		get => currentData;
		private set => currentData = value;
	}

	public bool HasUserID => !string.IsNullOrEmpty(CurrentData.userID);
	
	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
			DontDestroyOnLoad(gameObject);

			savePath = Path.Combine(Application.persistentDataPath, "GameSettings.json");
		}
		else
		{
			Destroy(gameObject);
		}
	}


	//=====================================================================
	// Load
	//=====================================================================
	public void Load()
	{
		if(isLoadData)
		{
			return;
		}

		if (!File.Exists(savePath))
		{
			CurrentData = new SaveData();
			Save();
			return;
		}

		string json = File.ReadAllText(savePath);
		CurrentData = JsonConvert.DeserializeObject<SaveData>(json);
		
		isLoadData = true;
	}


	//=====================================================================
	// Save
	//=====================================================================
	public void Save()
	{
		string json = JsonConvert.SerializeObject(CurrentData, Formatting.Indented);
		File.WriteAllText(savePath, json);
	}


	//=====================================================================
	// User ID
	//=====================================================================
	public void SetUserID(string id)
	{
		SaveData tmp = CurrentData;
		tmp.userID = id;
		CurrentData = tmp;

		Save();
	}


	//=====================================================================
	// Apply settings from save file to all managers
	//=====================================================================
	public void ApplyLoadedData()
	{
		SaveData d = CurrentData;
		if( DisplayManager.Instance!=null)DisplayManager.Instance.SetGridState(d.isGridOn);
		if (ResolutionManager.Instance != null) ResolutionManager.Instance.ApplyLoadedSettings(d.resolutionIndex, d.fullscreenIndex);
		if (SoundManager.Instance != null) SoundManager.Instance.ApplyLoadedVolume(d.masterVolume, d.bgmVolume, d.seVolume);
	}


	//=====================================================================
	// Update methods (修复 CS1612 核心区域)
	//=====================================================================

	public void UpdateSaveData()
	{
		UpdateGridState(DisplayManager.Instance.IsGridOn);
		UpdateVolume(
			SoundManager.Instance.MasterVolume,
			SoundManager.Instance.BGMVolume,
			SoundManager.Instance.SEVolume
		);
		UpdateResolutionIndex(ResolutionManager.Instance.CurrentResolutionIndex);
		UpdateFullscreenIndex(ResolutionManager.Instance.CurrentFullScreenIndex);
	}

	public void UpdateGridState(bool on)
	{
		SaveData tmp = CurrentData;
		tmp.isGridOn = on;
		CurrentData = tmp;

	}

	public void UpdateResolutionIndex(int index)
	{
		SaveData tmp = CurrentData;
		tmp.resolutionIndex = index;
		CurrentData = tmp;


	}

	public void UpdateFullscreenIndex(int index)
	{
		SaveData tmp = CurrentData;
		tmp.fullscreenIndex = index;
		CurrentData = tmp;


	}

	public void UpdateVolume(float master, float bgm, float se)
	{
		SaveData tmp = CurrentData;
		tmp.masterVolume = master;
		tmp.bgmVolume = bgm;
		tmp.seVolume = se;
		CurrentData = tmp;
	}
}


