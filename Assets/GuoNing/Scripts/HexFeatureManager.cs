using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Component that manages the map feature visualizations for a hex grid chunk.
/// </summary>
public class HexFeatureManager : MonoBehaviour
{
	[System.Serializable]
	public struct HexFeatureCollection
	{
		public Transform[] prefabs;
		public readonly Transform Pick(float choice) =>
			prefabs[(int)(choice * prefabs.Length)];
	}

	[SerializeField]
	HexFeatureCollection[] urbanCollections, farmCollections, plantCollections;

	[SerializeField]
	HexMesh walls;

	[SerializeField]
	Transform wallTower, bridge;

	[SerializeField]
	Transform[] special;

	public Transform container { get; private set; }

	List<HexFeature> CellFeatures = new List<HexFeature>();

	/// <summary>
	/// Clear all features.
	/// </summary>
	public void Clear()
	{
		if (container)
		{
			Destroy(container.gameObject);
		}
		container = new GameObject("Features Container").transform;
		container.SetParent(transform, false);
		walls.Clear();
	}

	/// <summary>
	/// Apply triangulation.
	/// </summary>
	public void Apply() => walls.Apply();


	/// <summary>
	/// 从指定的特征集合中挑选一个合适的预制体（prefab）
	///
	/// 逻辑：
	/// 1. 根据 level 决定是否可能生成特征（level 越高，生成概率越高）
	/// 2. 使用随机哈希值 hash 与阈值表 thresholds 比较，确定选中的种类索引
	/// 3. 使用 choice 在该种类集合中随机挑选一个具体的 prefab 实例
	/// </summary>
	/// <param name="collection">特征集合数组（例如不同类型的植物、农场、城市等）</param>
	/// <param name="level">当前格子的等级（控制特征出现的概率，如 PlantLevel、UrbanLevel 等）</param>
	/// <param name="hash">随机哈希值（0~1），用于决定是否生成以及选择哪一种类</param>
	/// <param name="choice">第二个随机值（0~1），用于在选中的集合中挑选具体 prefab</param>
	/// <returns>选中的 prefab，如果未命中阈值则返回 null（表示不生成）</returns>
	Transform PickPrefab(
		HexFeatureCollection[] collection, int level, float hash, float choice, int plantIndex = -1)
	{
		// 只有 level > 0 时才有可能生成特征（0 表示完全不生成）
		if (level > 0)
		{
			// 从 HexMetrics 中获取当前 level 对应的随机阈值表
			// 阈值表通常定义了每种特征的出现几率（数组单调递增，例如 [0.3, 0.6, 0.9]）
			float[] thresholds = HexMetrics.GetFeatureThresholds(level - 1);

			// 遍历所有阈值
			for (int i = 0; i < thresholds.Length; i++)
			{
				// 当 hash 小于某个阈值时，表示命中了这一类特征
				// 举例：
				//   hash=0.45, thresholds={0.3, 0.6, 0.9}
				//   命中第二个阈值 → 选择 collection[1]
				if (hash < thresholds[i])
				{
					int index = Mathf.Clamp(plantIndex, 0, plantCollections.Length - 1);
					return plantCollections[index].Pick(choice);
					// 在该类特征的 prefab 集合中，根据 choice 随机挑选一个具体模型
					//return collection[i].Pick(choice);

				}
			}
		}

		// 如果 level 为 0 或未命中任何阈值，则返回 null（表示不生成特征）
		return null;
	}

	/// <summary>
	/// Add a bridge between two road centers.
	/// </summary>
	/// <param name="roadCenter1">Center position of first road.</param>
	/// <param name="roadCenter2">Center position of second road.</param>
	public void AddBridge(Vector3 roadCenter1, Vector3 roadCenter2)
	{
		roadCenter1 = HexMetrics.Perturb(roadCenter1);
		roadCenter2 = HexMetrics.Perturb(roadCenter2);
		Transform instance = Instantiate(bridge);
		instance.localPosition = (roadCenter1 + roadCenter2) * 0.5f;
		instance.forward = roadCenter2 - roadCenter1;
		float length = Vector3.Distance(roadCenter1, roadCenter2);
		instance.localScale = new Vector3(
			1f,	1f, length * (1f / HexMetrics.bridgeDesignLength));
		instance.SetParent(container, false);
	}

	/// <summary>
	/// Add a feature for a cell.
	/// </summary>
	/// <param name="cell">Cell with one or more features.</param>
	/// <param name="position">Feature position.</param>
	public void AddFeature(HexCell cell, Vector3 position)
	{
		// 特殊地块（如城市中心、道路等）不生成其他特征
		if (cell.IsSpecial)
		{
			return;
		}

		// 从哈希网格中采样随机值（用于随机分布与外观选择）
		HexHash hash = HexMetrics.SampleHashGrid(position);

		// === 第一步：尝试生成城市建筑 ===
		// 根据城市等级 UrbanLevel 与随机数 hash.a，从城市预制体集合中选一个
		Transform prefab = PickPrefab(urbanCollections, cell.UrbanLevel, hash.a, hash.d);

		// === 第二步：尝试生成农场建筑 ===
		// 根据农田等级 FarmLevel 与随机数 hash.b，从农场预制体集合中选一个
		Transform otherPrefab = PickPrefab(farmCollections, cell.FarmLevel, hash.b, hash.d);

		// 用于记录当前被选中的随机值，越小代表优先级越高（更“幸运”）
		float usedHash = hash.a;

		// 若城市 prefab 存在，同时农场 prefab 也存在，则比较哪个随机值更小
		// 较小者“胜出”，使得不同特征间不会同时出现
		if (prefab)
		{
			if (otherPrefab && hash.b < hash.a)
			{
				prefab = otherPrefab;
				usedHash = hash.b;
			}
		}
		// 若城市为空但农场存在，则直接使用农场 prefab
		else if (otherPrefab)
		{
			prefab = otherPrefab;
			usedHash = hash.b;
		}

		// === 第三步：尝试生成植物特征 ===
		// 根据 PlantLevel 与随机数 hash.c，从植物集合中选一个
		otherPrefab = PickPrefab(plantCollections, cell.PlantLevel, hash.c, hash.d,cell.PlantIndex);

		// 若已有 prefab（城市/农场）存在，则比较随机值，优先选择随机值较小的
		if (prefab)
		{
			if (otherPrefab && hash.c < usedHash)
			{
				prefab = otherPrefab;
			}
		}
		// 若之前未选中任何 prefab，但植物存在，则直接使用植物
		else if (otherPrefab)
		{
			prefab = otherPrefab;
		}
		// 若三类特征都为空，则直接返回（不生成任何对象）
		else
		{
			return;
		}

		// === 第四步：实例化并放置对象 ===
		Transform instance = Instantiate(prefab);

		// 根据模型高度调整放置位置，使其位于地面之上
		//position.y += instance.localScale.y * 0.5f;	// 所有的Feature的原点都在脚底，所以y轴位置不需要变动了

		// 对位置进行扰动（防止网格边界过于整齐），并随机旋转角度
		instance.SetLocalPositionAndRotation(
			HexMetrics.Perturb(position), Quaternion.Euler(0f, 360f * hash.e, 0f));

		// 将生成的特征挂在到 container 下，便于场景层级管理
		instance.SetParent(container, false);

		var feature = instance.gameObject.AddComponent<HexFeature>();
		feature.Init(cell.Index);

		CellFeatures.Add(feature);
	}


	/// <summary>
	/// Add a special feature for a cell.
	/// </summary>
	/// <param name="cell">Cell with special feature.</param>
	/// <param name="position">Feature position.</param>
	public void AddSpecialFeature(HexCell cell, Vector3 position)
	{
		HexHash hash = HexMetrics.SampleHashGrid(position);
		Transform instance = Instantiate(special[cell.SpecialIndex - 1]);
		instance.SetLocalPositionAndRotation(
			HexMetrics.Perturb(position), Quaternion.Euler(0f, 360f * hash.e, 0f));
		instance.SetParent(container, false);
	}

	/*
	/// <summary>
	/// Add a wall along the edge between two cells.
	/// </summary>
	/// <param name="near">Near edge.</param>
	/// <param name="nearCell">Near cell.</param>
	/// <param name="far">Far edge.</param>
	/// <param name="farCell">Far cell.</param>
	/// <param name="hasRiver">Whether a river crosses the edge.</param>
	/// <param name="hasRoad">Whether a road crosses the edge.</param>
	public void AddWall(
		EdgeVertices near, HexCell nearCell,
		EdgeVertices far, HexCell farCell,
		bool hasRiver, bool hasRoad)
	{
		if (nearCell.Walled != farCell.Walled &&
			!nearCell.IsUnderwater && !farCell.IsUnderwater &&
			nearCell.GetEdgeType(farCell) != HexEdgeType.Cliff)
		{
			AddWallSegment(near.v1, far.v1, near.v2, far.v2);
			if (hasRiver || hasRoad)
			{
				AddWallCap(near.v2, far.v2);
				AddWallCap(far.v4, near.v4);
			}
			else
			{
				AddWallSegment(near.v2, far.v2, near.v3, far.v3);
				AddWallSegment(near.v3, far.v3, near.v4, far.v4);
			}
			AddWallSegment(near.v4, far.v4, near.v5, far.v5);
		}
	}

	/// <summary>
	/// Add a call though the corner where three cells meet.
	/// </summary>
	/// <param name="c1">First corner position.</param>
	/// <param name="cell1">First corner cell.</param>
	/// <param name="c2">Second corner position.</param>
	/// <param name="cell2">Second corner cell.</param>
	/// <param name="c3">Third corner position.</param>
	/// <param name="cell3">Third corner cell.</param>
	public void AddWall(
		Vector3 c1, HexCell cell1,
		Vector3 c2, HexCell cell2,
		Vector3 c3, HexCell cell3)
	{
		if (cell1.Walled)
		{
			if (cell2.Walled)
			{
				if (!cell3.Walled)
				{
					AddWallSegment(c3, cell3, c1, cell1, c2, cell2);
				}
			}
			else if (cell3.Walled)
			{
				AddWallSegment(c2, cell2, c3, cell3, c1, cell1);
			}
			else
			{
				AddWallSegment(c1, cell1, c2, cell2, c3, cell3);
			}
		}
		else if (cell2.Walled)
		{
			if (cell3.Walled)
			{
				AddWallSegment(c1, cell1, c2, cell2, c3, cell3);
			}
			else
			{
				AddWallSegment(c2, cell2, c3, cell3, c1, cell1);
			}
		}
		else if (cell3.Walled)
		{
			AddWallSegment(c3, cell3, c1, cell1, c2, cell2);
		}
	}

	void AddWallSegment(
		Vector3 nearLeft, Vector3 farLeft, Vector3 nearRight, Vector3 farRight,
		bool addTower = false)
	{
		nearLeft = HexMetrics.Perturb(nearLeft);
		farLeft = HexMetrics.Perturb(farLeft);
		nearRight = HexMetrics.Perturb(nearRight);
		farRight = HexMetrics.Perturb(farRight);

		Vector3 left = HexMetrics.WallLerp(nearLeft, farLeft);
		Vector3 right = HexMetrics.WallLerp(nearRight, farRight);

		Vector3 leftThicknessOffset =
			HexMetrics.WallThicknessOffset(nearLeft, farLeft);
		Vector3 rightThicknessOffset =
			HexMetrics.WallThicknessOffset(nearRight, farRight);

		float leftTop = left.y + HexMetrics.wallHeight;
		float rightTop = right.y + HexMetrics.wallHeight;

		Vector3 v1, v2, v3, v4;
		v1 = v3 = left - leftThicknessOffset;
		v2 = v4 = right - rightThicknessOffset;
		v3.y = leftTop;
		v4.y = rightTop;
		walls.AddQuadUnperturbed(v1, v2, v3, v4);

		Vector3 t1 = v3, t2 = v4;

		v1 = v3 = left + leftThicknessOffset;
		v2 = v4 = right + rightThicknessOffset;
		v3.y = leftTop;
		v4.y = rightTop;
		walls.AddQuadUnperturbed(v2, v1, v4, v3);

		walls.AddQuadUnperturbed(t1, t2, v3, v4);

		if (addTower)
		{
			Transform towerInstance = Instantiate(wallTower);
			towerInstance.transform.localPosition = (left + right) * 0.5f;
			Vector3 rightDirection = right - left;
			rightDirection.y = 0f;
			towerInstance.transform.right = rightDirection;
			towerInstance.SetParent(container, false);
		}
	}

	void AddWallSegment(
		Vector3 pivot, HexCell pivotCell,
		Vector3 left, HexCell leftCell,
		Vector3 right, HexCell rightCell)
	{
		if (pivotCell.IsUnderwater)
		{
			return;
		}

		bool hasLeftWall = !leftCell.IsUnderwater &&
			pivotCell.GetEdgeType(leftCell) != HexEdgeType.Cliff;
		bool hasRighWall = !rightCell.IsUnderwater &&
			pivotCell.GetEdgeType(rightCell) != HexEdgeType.Cliff;


		if (hasLeftWall)
		{
			if (hasRighWall)
			{
				bool hasTower = false;
				if (leftCell.Elevation == rightCell.Elevation)
				{
					HexHash hash = HexMetrics.SampleHashGrid(
						(pivot + left + right) * (1f / 3f));
					hasTower = hash.e < HexMetrics.wallTowerThreshold;
				}
				AddWallSegment(pivot, left, pivot, right, hasTower);
			}
			else if (leftCell.Elevation < rightCell.Elevation)
			{
				AddWallWedge(pivot, left, right);
			}
			else
			{
				AddWallCap(pivot, left);
			}
		}
		else if (hasRighWall)
		{
			if (rightCell.Elevation < leftCell.Elevation)
			{
				AddWallWedge(right, pivot, left);
			}
			else
			{
				AddWallCap(right, pivot);
			}
		}
	}

	void AddWallCap(Vector3 near, Vector3 far)
	{
		near = HexMetrics.Perturb(near);
		far = HexMetrics.Perturb(far);

		Vector3 center = HexMetrics.WallLerp(near, far);
		Vector3 thickness = HexMetrics.WallThicknessOffset(near, far);

		Vector3 v1, v2, v3, v4;

		v1 = v3 = center - thickness;
		v2 = v4 = center + thickness;
		v3.y = v4.y = center.y + HexMetrics.wallHeight;
		walls.AddQuadUnperturbed(v1, v2, v3, v4);
	}

	void AddWallWedge(Vector3 near, Vector3 far, Vector3 point)
	{
		near = HexMetrics.Perturb(near);
		far = HexMetrics.Perturb(far);
		point = HexMetrics.Perturb(point);

		Vector3 center = HexMetrics.WallLerp(near, far);
		Vector3 thickness = HexMetrics.WallThicknessOffset(near, far);

		Vector3 v1, v2, v3, v4;
		Vector3 pointTop = point;
		point.y = center.y;

		v1 = v3 = center - thickness;
		v2 = v4 = center + thickness;
		v3.y = v4.y = pointTop.y = center.y + HexMetrics.wallHeight;

		walls.AddQuadUnperturbed(v1, point, v3, pointTop);
		walls.AddQuadUnperturbed(point, v2, pointTop, v4);
		walls.AddTriangleUnperturbed(pointTop, v3, v4);
	}
	*/
	public void AddWall(
	EdgeVertices near, HexCell nearCell,
	EdgeVertices far, HexCell farCell,
	bool hasRiver, bool hasRoad
)
	{
		if (nearCell.Walled != farCell.Walled)
		{
			AddWallSegment(near.v1, far.v1, near.v2, far.v2);
			if (hasRiver || hasRoad)
			{
				AddWallCap(near.v2, far.v2);
				AddWallCap(far.v4, near.v4);
			}
			else
			{
				AddWallSegment(near.v2, far.v2, near.v3, far.v3);
				AddWallSegment(near.v3, far.v3, near.v4, far.v4);
			}
			AddWallSegment(near.v4, far.v4, near.v5, far.v5);
		}
	}

	void AddWallSegment(
		Vector3 nearLeft, Vector3 farLeft, Vector3 nearRight, Vector3 farRight
	)
	{
		nearLeft = HexMetrics.Perturb(nearLeft);
		farLeft = HexMetrics.Perturb(farLeft);
		nearRight = HexMetrics.Perturb(nearRight);
		farRight = HexMetrics.Perturb(farRight);

		Vector3 left = HexMetrics.WallLerp(nearLeft, farLeft);
		Vector3 right = HexMetrics.WallLerp(nearRight, farRight);

		Vector3 leftThicknessOffset =
			HexMetrics.WallThicknessOffset(nearLeft, farLeft);
		Vector3 rightThicknessOffset =
			HexMetrics.WallThicknessOffset(nearRight, farRight);

		float leftTop = left.y + HexMetrics.wallHeight;
		float rightTop = right.y + HexMetrics.wallHeight;

		Vector3 v1, v2, v3, v4;
		v1 = v3 = left - leftThicknessOffset;
		v2 = v4 = right - rightThicknessOffset;
		v3.y = leftTop;
		v4.y = rightTop;
		walls.AddQuadUnperturbed(v1, v2, v3, v4);

		Vector3 t1 = v3, t2 = v4;
		v1 = v3 = left + leftThicknessOffset;
		v2 = v4 = right + rightThicknessOffset;
		v3.y = leftTop;
		v4.y = rightTop;
		walls.AddQuadUnperturbed(v2, v1, v4, v3);

		walls.AddQuadUnperturbed(t1, t2, v3, v4);
	}

	void AddWallSegment(
		Vector3 pivot, HexCell pivotCell,
		Vector3 left, HexCell leftCell,
		Vector3 right, HexCell rightCell
	)
	{
		AddWallSegment(pivot, left, pivot, right);
	}

	public void AddWall(
		Vector3 c1, HexCell cell1,
		Vector3 c2, HexCell cell2,
		Vector3 c3, HexCell cell3
	)
	{
		if (cell1.Walled)
		{
			if (cell2.Walled)
			{
				if (!cell3.Walled)
				{
					AddWallSegment(c3, cell3, c1, cell1, c2, cell2);
				}
			}
			else if (cell3.Walled)
			{
				AddWallSegment(c2, cell2, c3, cell3, c1, cell1);
			}
			else
			{
				AddWallSegment(c1, cell1, c2, cell2, c3, cell3);
			}
		}
		else if (cell2.Walled)
		{
			if (cell3.Walled)
			{
				AddWallSegment(c1, cell1, c2, cell2, c3, cell3);
			}
			else
			{
				AddWallSegment(c2, cell2, c3, cell3, c1, cell1);
			}
		}
		else if (cell3.Walled)
		{
			AddWallSegment(c3, cell3, c1, cell1, c2, cell2);
		}
	}

	void AddWallCap(Vector3 near, Vector3 far)
	{
		near = HexMetrics.Perturb(near);
		far = HexMetrics.Perturb(far);

		Vector3 center = HexMetrics.WallLerp(near, far);
		Vector3 thickness = HexMetrics.WallThicknessOffset(near, far);

		Vector3 v1, v2, v3, v4;

		v1 = v3 = center - thickness;
		v2 = v4 = center + thickness;
		v3.y = v4.y = center.y + HexMetrics.wallHeight;
		walls.AddQuadUnperturbed(v1, v2, v3, v4);
	}

	public void UpdateFeature(int index,bool transparent)
	{
		for (int i = 0; i < CellFeatures.Count; i++)
		{
			// 可能有多个Feature用同一个CellIndex
			if (CellFeatures[i].HexCellIndex == index)
			{
				CellFeatures[i].SetTransparency(transparent);
			}
		}
	}
}
