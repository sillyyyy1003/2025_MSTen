using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// 用于加载地图
/// </summary>
public class HexMapLoader : MonoBehaviour
{

	public HexMapGenerator mapGenerator;    // 随机地图生成器
	public HexGrid hexGrid;                 // 地图
    public bool isLoadMap;                  // 是否加载指定地图
    public string mapFileName;				// 地图文件名

	public enum MapSize{Small, Medium, Large}
	[Header("Random Generate Map Param")]
	public MapSize mapSize; // 随机生成的地图大小

	private int mapFileVersion = 5;			// 地图文件版本


	public void LoadMap()
	{
		if (isLoadMap)
		{
			string path = Path.Combine(Application.dataPath, "Maps", mapFileName);
			if (!File.Exists(path))
			{
				// 如果文件不存在 则创建一个默认地图
				Debug.LogError($"地图文件不存在: {path}");
				hexGrid.CreateMap(20, 15,false);  // 创建一个默认地图
				return;
			}
			using (BinaryReader reader = new BinaryReader(File.OpenRead(path)))
			{
				int header = reader.ReadInt32();
				if (header <= mapFileVersion)
				{
					hexGrid.Load(reader, header);
					Debug.Log($"地图加载成功: {path}");
				}
				else
				{
					Debug.LogWarning($"未知地图格式: {header}");
				}
			}
		}
		else
		{
			switch (mapSize)
			{
				case MapSize.Small:
					mapGenerator.GenerateMap(20, 15); // 生成一个随机地图
					break;
				case MapSize.Medium:
					mapGenerator.GenerateMap(40, 30); // 生成一个随机地图
					break;
				case MapSize.Large:
					mapGenerator.GenerateMap(80, 60); // 生成一个随机地图
					break;
			}

		}

	}
}
