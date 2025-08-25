using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class HexInputHandler : MonoBehaviour
{
	public Camera camera;
	public Tilemap tilemap;

	[Header("Prefab to spawn on tile click")]
	public GameObject cubePrefab; // 预制件

	[Header("Parent object for spawned cubes")]
	public Transform spawnRoot;   // 空物体作为父节点

	void Update()
	{
		if (Input.GetMouseButtonDown(0))	// Mouse left button clicked
		{
			Ray ray = camera.ScreenPointToRay(Input.mousePosition);

			// 因为你的 Tilemap 在 XZ 平面，所以平面法向量是 Vector3.up (y轴正方向)，平面过 y=0
			Plane plane = new Plane(Vector3.up, Vector3.zero);

			if (plane.Raycast(ray, out float enter))
			{
				// 得到射线和平面的交点
				Vector3 hitPoint = ray.GetPoint(enter);

				// 转换到格子坐标
				Vector3Int cellPos = tilemap.WorldToCell(hitPoint);

				Vector3 worldCenter = tilemap.GetCellCenterWorld(cellPos);
				if (cubePrefab != null)
				{
					Transform parent = spawnRoot ? spawnRoot : null; // 如果没指定父节点就放在场景根
					Instantiate(cubePrefab, worldCenter, Quaternion.identity, parent);
				}
				else
				{
					Debug.LogWarning("Cube Prefab is not assigned!");
				}




				Debug.Log($"[Click] mouseWorld={hitPoint}, cell={cellPos}");
				HexTile tile = TileManager.Instance.GetTile(cellPos);
				if (tile)
				{
					Debug.Log($"Tile type: {tile.tileType}, Cost: {tile.cost}");
				}
				else
				{
					Debug.Log($"Clicked null cell: {cellPos}");
				}
			}
		}

	
	}
}
