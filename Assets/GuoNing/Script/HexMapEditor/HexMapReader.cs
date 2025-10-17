using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;

public class HexMapReader : MonoBehaviour
{
	const int mapFileVersion = 5;
	public HexGrid hexGrid;
	public string mapFileName = "Example.map";

	public void LoadMap()
	{
		string path = Path.Combine(Application.dataPath, "Maps", mapFileName);

		if (!File.Exists(path))
		{
			Debug.LogError($"地图文件不存在: {path}");
			return;
		}

		using (BinaryReader reader = new BinaryReader(File.OpenRead(path)))
		{
			int header = reader.ReadInt32();
			if (header <= mapFileVersion)
			{
				hexGrid.Load(reader, header);

                // 25.10.10 RI 删除Camera相关避免loadMap出错
                //HexMapCamera.ValidatePosition();

                Debug.Log($"地图加载成功: {path}");
			}
			else
			{
				Debug.LogWarning($"未知地图格式: {header}");
			}
		}
	}

}
