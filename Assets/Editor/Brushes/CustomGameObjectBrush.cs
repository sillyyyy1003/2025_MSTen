using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.Tilemaps;

[CreateAssetMenu(fileName = "CustomHexGameObjectBrush", menuName = "Editor/Brushes/Custom Hex GameObject Brush")]
[CustomGridBrush(false, true, false, "Custom Hex GameObject Brush")]
public class CustomHexGameObjectBrush : GameObjectBrush
{
	[Header("Tile Prefab List")]
	public GameObject[] tilePrefabs;   // 预设的 prefab 列表
	public int currentIndex = 0;       // 当前使用的 prefab

	[Header("Hex Offset Correction")]
	public Vector3 correctionOffset = Vector3.zero; // 偏移修正

	public override void Paint(GridLayout grid, GameObject brushTarget, Vector3Int position)
	{
		if (tilePrefabs == null || tilePrefabs.Length == 0) return;
		if (currentIndex < 0 || currentIndex >= tilePrefabs.Length) return;

		Tilemap tilemap = brushTarget.GetComponent<Tilemap>();
		if (tilemap == null) return;

		// cell 世界中心点（Unity 默认算法）
		Vector3 cellWorldPos = grid.CellToWorld(position) + grid.cellSize / 2f;

		// 自动校正（尖顶六边形 + XZY）
		Vector3 correctionOffset = new Vector3(
			-grid.cellSize.x * 0.5f,
			0f,
			-grid.cellSize.z * 0.38f // 这里 0.38f 是你实测的比例
		);

		cellWorldPos += correctionOffset;

		// 实例化 prefab
		GameObject prefab = tilePrefabs[currentIndex];
		if (prefab != null)
		{
			GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, brushTarget.transform);
			instance.transform.position = cellWorldPos;
		}
	}
}