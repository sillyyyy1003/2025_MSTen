using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public struct MapConfig//todo:未来也许需要加载地图缩略图等其他数据
{
	public int serialNumber;    // 地图序号 唯一标识地图
	public string displayName;  // 地图显示名称
	public string description;  // 地图描述（可选，用于UI显示）
	public string fileName;     // 地图文件路径，用于存储地图数据
}

[System.Serializable]
public class MapConfigList
{
	public List<MapConfig> maps = new List<MapConfig>();
}

/// <summary>
/// 全局单例：用于管理地图的加载与保存
/// </summary>
public class HexMapManager : MonoBehaviour
{
	public static HexMapManager Instance { get; private set; }

	public HexMapGenerator mapGenerator;    // 随机地图生成器
	public HexGrid hexGrid;                 // 地图网格

	private MapConfigList configList;       // 配置列表 配置文件地址：Assets\Resources\Config\maps.json
	[SerializeField]
	private int serialNumber;				 // 当前地图序列号

	/// <summary> 提供外部访问配置列表 </summary>
	public List<MapConfig> MapConfigs => configList.maps;

	private void Awake()
	{
		if (Instance != null)
		{
			Destroy(gameObject);
			return;
		}
		Instance = this;

		LoadConfig(); // 加载地图配置
	}

	public void InitHexMapManager()
	{
		serialNumber = SceneStateManager.Instance.mapSerialNumber;
		Debug.Log(SceneStateManager.Instance.mapSerialNumber);
		// 暂时放在Start里 如果有需要 注释掉这里即可
		LoadMap(serialNumber);
	}

	private void OnDestroy()
	{
		if (Instance == this)
		{
			Instance = null;
			Debug.Log("Manager已销毁");
		}
	}

	//--------------------------------------------------------------------------------
	// メソッド
	//--------------------------------------------------------------------------------


	/// <summary>
	/// 地图的初始化
	/// </summary>
	public void Initialization(int number)
	{
		LoadMap(number);
	}

	/// <summary>
	/// 从Resources中加载配置文件
	/// </summary>
	private void LoadConfig()
	{
		TextAsset json = Resources.Load<TextAsset>("Config/maps");
		if (json != null)
		{
			configList = JsonUtility.FromJson<MapConfigList>(json.text);
			Debug.Log($"Loaded map config: {configList.maps.Count} maps");
		}
		else
		{
			Debug.LogWarning("Map config not found, creating new list.");
			configList = new MapConfigList { maps = new List<MapConfig>() };
		}
	}

	/// <summary>
	/// 加载指定地图
	/// </summary>
	/// <param name="mapSeed">地图种子</param>
	/// <returns>是否成功加载</returns>
	public bool LoadMap(int serialNumber)
	{
		int index = configList.maps.FindIndex(m => m.serialNumber == serialNumber);
		if (index < 0)
		{
			Debug.LogError($"Map not found in config: serialNumber ={serialNumber}");
			return false;
		}

		MapConfig config = configList.maps[index];
		//string path = Path.Combine(Application.dataPath, "Maps", config.fileName);
		// 通过StreamingAssetsPath路径加载地图文件，避免打包后找不到文件的问题
		string path = Path.Combine(Application.streamingAssetsPath, "Maps", config.fileName);

		if (!File.Exists(path))
		{
			Debug.LogError($"地图文件不存在: {path}");
			return false;
		}

		using (BinaryReader reader = new BinaryReader(File.OpenRead(path)))
		{
			int header = reader.ReadInt32();
			if (header <= 5)
			{
				hexGrid.Load(reader, header);
				Debug.Log($"地图加载成功: {config.displayName} ({path})");
			}
			else
			{
				Debug.LogWarning($"未知地图格式: {header}");
				return false;
			}
		}

		return true;
	}

	/// <summary>
	/// 生成随机地图
	/// </summary>
	public void LoadRandomMap(int x, int z)
	{
		mapGenerator.GenerateMap(x, z);
		Debug.Log($"🧩 Generated random map {x}x{z}");
	}

	/// <summary>
	/// 向配置中添加新地图 todo:进一步修改成 变量为MapName自动生成序列号
	/// </summary>
	public bool AddMap(MapConfig map)
	{
		if (MapConfigs.Exists(m => m.serialNumber == map.serialNumber))
		{
			Debug.Log($"Map already exists: {map.displayName}");
			return false;
		}
		MapConfigs.Add(map);
		Debug.Log($"Map added: {map.displayName}");
		return true;
	}

	/// <summary>
	/// 从配置中移除地图
	/// </summary>
	public bool RemoveMap(int serialNumber)
	{
		int index = MapConfigs.FindIndex(m => m.serialNumber == serialNumber);
		if (index >= 0)
		{
			Debug.Log($"Map removed: {MapConfigs[index].displayName}");
			MapConfigs.RemoveAt(index);
			return true;
		}
		Debug.LogWarning($"Map not found: seed={serialNumber}");
		return false;
	}

	
}
