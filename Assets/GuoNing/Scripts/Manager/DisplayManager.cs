using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Bson;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 表示関連の管理
/// </summary>
public class DisplayManager : MonoBehaviour
{
	//--------------------------------------------------------------------------------
	// メンバ変数
	//--------------------------------------------------------------------------------
	public static DisplayManager Instance;
	private bool isGridOn = false; // 是否开启网格显示


	public bool IsGridOn => isGridOn;
	


	//--------------------------------------------------------------------------------
	// プロパティ
	//--------------------------------------------------------------------------------
	[SerializeField]
	Material terrainMaterial;


	//--------------------------------------------------------------------------------
	// メソッド
	//--------------------------------------------------------------------------------

	private void Awake()
	{
		// シングルトン設定
		if (Instance == null)
		{
			Instance = this;
			DontDestroyOnLoad(gameObject); // This object should persist across scenes
		}
		else
		{
			Destroy(gameObject);
		}
	}

	private void Start()
	{
		ShowGrid();
	}

	public void ShowGrid()
	{
		if (isGridOn)
		{
			terrainMaterial.EnableKeyword("_SHOW_GRID");
		}
		else
		{
			terrainMaterial.DisableKeyword("_SHOW_GRID");
		}

	}

	public void InitializeToggle(Toggle toggle)
	{
		toggle.onValueChanged.AddListener(OnToggleChanged);
	}

	private void OnToggleChanged(bool isOn)
	{
		isGridOn = isOn;
		ShowGrid();
	}

	public void SetGridState(bool on)
	{
		isGridOn = on;
		ShowGrid();
	}

}
