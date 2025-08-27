using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class HexInputHandler : MonoBehaviour
{
	public Camera cam;
	public Tilemap tilemap;

	[Header("Prefab to spawn on tile click")]
	public GameObject cubePrefab; // 预制件

	[Header("Parent object for spawned cubes")]
	public Transform spawnRoot;   // 空物体作为父节点

	void Update()
	{
		if (Input.GetMouseButtonDown(0))	// Mouse left button clicked
		{
			Ray ray = cam.ScreenPointToRay(Input.mousePosition);

			// Tile map是在水平面上，所以用y=0的平面做射线检测
			Plane plane = new Plane(Vector3.up, Vector3.zero);

			if (plane.Raycast(ray, out float enter))
			{
				// 得到射线和平面的交点
				Vector3 hitPoint = ray.GetPoint(enter);

				// 转换到Tile的坐标
				Vector3Int cellPos = tilemap.WorldToCell(hitPoint);

				// 转换成世界坐标（center of tile)
				Vector3 worldCenter = tilemap.GetCellCenterWorld(cellPos);
				if (cubePrefab != null)
				{
					// 生成Cube表示占领
					Transform parent = spawnRoot ? spawnRoot : null; // 如果没指定父节点就放在场景根
					Instantiate(cubePrefab, worldCenter, Quaternion.identity, parent);
				}
				else
				{
					Debug.LogWarning("Cube Prefab is not assigned!");
				}

			}
		}

	
	}
}
