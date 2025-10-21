using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public struct HexCoordinates
{
	[SerializeField]
	private int x, z;

	public int X
	{
		get
		{
			return x;
		}
	}

	public int Z
	{
		get
		{
			return z;
		}
	}

	public HexCoordinates(int x, int z)
	{
		this.x = x;
		this.z = z;
	}
	public static HexCoordinates FromOffsetCoordinates(int x, int z)
	{
		return new HexCoordinates(x - z / 2, z);
	}
	public int Y
	{
		get
		{
			return -X - Z;
		}
	}

	public override string ToString()
	{
		return "(" +
		       X.ToString() + ", " + Y.ToString() + ", " + Z.ToString() + ")";
	}

	public string ToStringOnSeparateLines()
	{
		return X.ToString() + "\n" + Y.ToString() + "\n" + Z.ToString();
	}

	/// <summary>
	/// 从某个位置获得其所在格子的坐标
	/// </summary>
	/// <param name="position"></param>
	/// <returns></returns>
	public static HexCoordinates FromPosition(Vector3 position)
	{
		float x = position.x / (HexMetrics.innerRadius * 2f);
		float y = -x;
		float offset = position.z / (HexMetrics.outerRadius * 3f);
		x -= offset;
		y -= offset;
		int iX = Mathf.RoundToInt(x);
		int iY = Mathf.RoundToInt(y);
		int iZ = Mathf.RoundToInt(-x - y);

		if (iX + iY + iZ != 0)
		{
			float dX = Mathf.Abs(x - iX);
			float dY = Mathf.Abs(y - iY);
			float dZ = Mathf.Abs(-x - y - iZ);

			if (dX > dY && dX > dZ)
			{
				iX = -iY - iZ;
			}
			else if (dZ > dY)
			{
				iZ = -iX - iY;
			}
		}

		return new HexCoordinates(iX, iZ);
	}

	/// <summary>
	/// 计算单元格之间的距离
	/// </summary>
	/// <param name="other"></param>
	/// <returns></returns>
	public int DistanceTo(HexCoordinates other)
	{
		return ((x < other.x ? other.x - x : x - other.x) +
		        (Y < other.Y ? other.Y - Y : Y - other.Y) +
		        (z < other.z ? other.z - z : z - other.z)) / 2;//返回 X 坐标之间的绝对差值。
	}

	/// <summary>
	/// get the hex coordinates of a direct neighbor
	/// </summary>
	/// <param name="direction"></param>
	/// <returns></returns>
	public HexCoordinates Step(HexDirection direction) => direction switch
	{
		HexDirection.NE => new HexCoordinates(x, z + 1),
		HexDirection.E => new HexCoordinates(x + 1, z),
		HexDirection.SE => new HexCoordinates(x + 1, z - 1),
		HexDirection.SW => new HexCoordinates(x, z - 1),
		HexDirection.W => new HexCoordinates(x - 1, z),
		_ => new HexCoordinates(x - 1, z + 1)
	};
}

/// <summary>
/// 棋子的方位/六角形是尖顶，因此没有南北
/// </summary>
public enum HexDirection
{
	NE,	// NorthEast 
	E,	// East
	SE,	// SouthEast
	SW,	// SouthWest
	W,	// West
	NW	// NorthWest
}

/// <summary>
/// 棋子连结边缘的类型
/// </summary>

public enum HexEdgeType
{
	Flat,	// 平坦
	Slope,	// 缓坡
	Cliff	// 陡坡
}



/// <summary>
/// Hex direction for neighbors in opposite directions.
/// </summary>
public static class HexDirectionExtensions
{
	public static HexDirection Opposite(this HexDirection direction)
	{
		return (int)direction < 3 ? (direction + 3) : (direction - 3);
	}
	public static HexDirection Previous(this HexDirection direction)
	{
		return direction == HexDirection.NE ? HexDirection.NW : (direction - 1);
	}

	public static HexDirection Next(this HexDirection direction)
	{
		return direction == HexDirection.NW ? HexDirection.NE : (direction + 1);
	}

	public static HexDirection Previous2(this HexDirection direction)
	{
		direction -= 2;
		return direction >= HexDirection.NE ? direction : (direction + 6);
	}

	public static HexDirection Next2(this HexDirection direction)
	{
		direction += 2;
		return direction <= HexDirection.NW ? direction : (direction - 6);
	}
}


public enum TerrainType
{
	Forest,		// 森林 可通过不可占领
 	Mountain,	// 山地 不可通过 不可占领 
    Water, 		// 水 不可通过 不可占领
	Plain		// 平原 可通过 可占领
}



public class HexCell : MonoBehaviour
{
	public HexCoordinates Coordinates; // Hex coordinate(https://catlikecoding.com/unity/tutorials/hex-map/part-1/hexagonal-coordinates/cube-coordinates.png)
	public RectTransform uiRect;
	public HexGridChunk Chunk;
	public HexGrid Grid
	{ get; set; }

	private HexFlags flags; // hex flags


	private int distance; // 该单元格和目标单元格的距离
	//public HexCell PathFrom { get; set; }   // 储存路径
	public int PathFromIndex { get; set; }		// 储存路径
	public int SearchHeuristic { get; set; }// 启发式搜索值
	public HexCell NextWithSamePriority { get; set; }	//追踪有相同优先级的单元格
	
	public enum TerrainColor:int
	{
		Sand=0, Grass=1,Mud=2,Stone=3,Snow=4
	}

	// 当前格子所属的玩家ID
	public int playerId { get; set; }
	// 当前格子是否有棋子
	public GameObject Unit { get; set; }


	//public bool IsVacancy
	//{
	//	get { return isVacancy; }
	//	set { isVacancy = value; }
	//}
	//	bool isVacancy; //whether this cell is vacant

	public int ViewElevation
	{
		get
		{
			return elevation >= waterLevel ? elevation : waterLevel;
		}
	}



	/// <summary>
	/// 获取/设定档期按各自的水平面
	/// </summary>
	int waterLevel; // water level

	public int WaterLevel
	{
		get { return waterLevel; }
		set
		{
			if (waterLevel == value)
			{
				return;
			}

			waterLevel = value;

			ValidateRivers();
			Refresh();
		}
	}

	/// <summary>
	/// Unique global index of the cell.
	/// </summary>
	public int Index { get; set; }

	/// <summary>
	/// 这个格子是否有水
	/// </summary>
	public bool IsUnderwater //whether this hex cell is underwater according to elevation and water level
	{
		get { return waterLevel > elevation; }
	}


	/// <summary>
	/// 是否有路
	/// </summary>
	public bool HasRoads => flags.HasAny(HexFlags.Roads);
	public bool HasRoadThroughEdge(HexDirection direction) => flags.HasRoad(direction);


	/// <summary>
	/// 添加道路
	/// </summary>
	/// <param name="direction"></param>
	public void AddRoad(HexDirection direction)
	{
		if (
			!flags.HasRoad(direction) && !HasRiverThroughEdge(direction) &&
			!IsSpecial && !GetNeighbor(direction).IsSpecial &&
			GetElevationDifference(direction) <= 1
		)
		{
			//SetRoad((int)direction, true);
			flags = flags.WithRoad(direction);
			HexCell neighbor = GetNeighbor(direction);
			neighbor.flags = neighbor.flags.WithRoad(direction.Opposite());
			neighbor.RefreshSelfOnly();
			RefreshSelfOnly();
		}
	}

	/// <summary>
	/// 移除所有道路
	/// </summary>
	public void RemoveRoads()
	{
		for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
		{
			if (flags.HasRoad(d))
			{
				RemoveRoad(d);
			}
		}
	}

	/// <summary>
	/// 移除某个方向的道路
	/// </summary>
	/// <param name="direction"></param>

	void RemoveRoad(HexDirection direction)
	{
		flags = flags.WithoutRoad(direction);
		HexCell neighbor = GetNeighbor(direction);
		neighbor.flags = neighbor.flags.WithoutRoad(direction.Opposite());
		neighbor.RefreshSelfOnly();
		RefreshSelfOnly();
	}

	/// <summary>
	/// 获取该格子和某个方向的临近格子之间的高度差
	/// </summary>
	/// <param name="direction"></param>
	/// <returns></returns>

	public int GetElevationDifference(HexDirection direction)
	{
		int difference = elevation - GetNeighbor(direction).elevation;
		return difference >= 0 ? difference : -difference;
	}

	/// <summary>
	/// 改变某个格子的颜色
	/// </summary>
	/// <param name="color"></param>
	public void SetCellColor(TerrainColor color)
	{
		terrainTypeIndex = (int)color;
		Refresh();
	}


	int terrainTypeIndex;

	/// <summary>
	/// 返回这个格子的颜色
	/// </summary>
	public int TerrainTypeIndex
	{
		get { return terrainTypeIndex; }
		set
		{
			if (terrainTypeIndex != value)
			{
				terrainTypeIndex = value;
				Refresh();
			}
		}
	}

	/// <summary>
	/// 是否被有围墙
	/// </summary>
	public bool Walled
	{
		get => flags.HasAny(HexFlags.Walled);
		set
		{
			HexFlags newFlags =
				value ? flags.With(HexFlags.Walled) : flags.Without(HexFlags.Walled);
			if (flags != newFlags)
			{
				flags = newFlags;
				Refresh();
			}
		}
	}


	/// <summary>
	/// 获得该格子的高度
	/// </summary>
	public int Elevation
	{
		get => elevation;
		set
		{
			if (elevation == value)
			{
				return;
			}

			elevation = value;
			RefreshPosition();

			// river
			ValidateRivers();

			// road
			for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
			{
				if (flags.HasRoad(d) && GetElevationDifference(d) > 1)
				{
					RemoveRoad(d);
				}
			}

			Refresh(); // Refresh the chunk this cell belongs to
		}
	}


	/// <summary>
	/// 格子中是否有河流
	/// </summary>
	public bool HasRiver => flags.HasAny(HexFlags.River);
	public bool HasIncomingRiver => flags.HasAny(HexFlags.RiverIn);
	public bool HasOutgoingRiver => flags.HasAny(HexFlags.RiverOut);
	public HexDirection IncomingRiver => flags.RiverInDirection();
	public HexDirection OutgoingRiver => flags.RiverOutDirection();


	/// <summary>
	/// 返回这个格子是否是河流的开头或者结尾
	/// </summary>
	public bool HasRiverBeginOrEnd => HasIncomingRiver != HasOutgoingRiver;
	
	// has river through edge
	public bool HasRiverThroughEdge(HexDirection direction) =>
		flags.HasRiverIn(direction) || flags.HasRiverOut(direction);

	public bool HasIncomingRiverThroughEdge(HexDirection direction) =>
		flags.HasRiverIn(direction);

	/// <summary>
	/// 移除流出河流
	/// </summary>
	public void RemoveOutgoingRiver()
	{
		if (!HasOutgoingRiver)
		{
			return;
		}

		HexCell neighbor = GetNeighbor(OutgoingRiver);
		flags = flags.Without(HexFlags.RiverOut);
		neighbor.flags = neighbor.flags.Without(HexFlags.RiverIn);
		neighbor.RefreshSelfOnly();
		RefreshSelfOnly();
	}

	/// <summary>
	/// 移除流进河流
	/// </summary>
	public void RemoveIncomingRiver()
	{
		if (!HasIncomingRiver)
		{
			return;
		}

		HexCell neighbor = GetNeighbor(IncomingRiver);
		flags = flags.Without(HexFlags.RiverIn);
		neighbor.flags = neighbor.flags.Without(HexFlags.RiverOut);
		neighbor.RefreshSelfOnly();
		RefreshSelfOnly();
	}

	// remove in/out river
	public void RemoveRiver()
	{
		RemoveOutgoingRiver();
		RemoveIncomingRiver();
	}

	/// <summary>
	/// 根据选定的方向选定流出的河流
	/// </summary>
	/// <param name="direction"></param>
	public void SetOutgoingRiver(HexDirection direction)
	{
		// if has river do nothing
		if (flags.HasRiverOut(direction))
		{
			return;
		}

		// get neighbor if neighbor is higher than this cell do nothing
		HexCell neighbor = GetNeighbor(direction);
		if (!IsValidRiverDestination(neighbor))
		{
			return;
		}

		// remove previous outgoing river
		RemoveOutgoingRiver();
		if (flags.HasRiverIn(direction))
		{
			RemoveIncomingRiver();
		}

		flags = flags.WithRiverOut(direction);
		specialIndex = 0;
		neighbor.RemoveIncomingRiver();
		neighbor.flags = neighbor.flags.WithRiverIn(direction.Opposite());
		neighbor.specialIndex = 0;

		RemoveRoad(direction);
	}


	/// <summary>
	/// 忽略对格子周围的影响，只更新该格子
	/// </summary>
	void RefreshSelfOnly()
	{
		Chunk.Refresh();
	}

	public int elevation = int.MinValue; // hex height

	/// <summary>
	/// 获取某个方向上的邻居格子
	/// </summary>
	/// <param name="direction"></param>
	/// <returns></returns>
	public HexCell GetNeighbor(HexDirection direction) =>
		Grid.GetCell(Coordinates.Step(direction));

	public bool TryGetNeighbor(HexDirection direction, out HexCell cell) =>
		Grid.TryGetCell(Coordinates.Step(direction), out cell);

	/// <summary>
	/// 初始化格子周遭的邻居
	/// </summary>
	/// <param name="direction"></param>
	/// <param name="cell"></param>


	/// <summary>
	/// 获取格子边缘的类型
	/// </summary>
	/// <param name="direction"></param>
	/// <returns></returns>
	public HexEdgeType GetEdgeType(HexDirection direction) => HexMetrics.GetEdgeType(
		elevation, GetNeighbor(direction).elevation
	);

	/// <summary>
	/// 根据格子之间的高低差 判断格子边缘的类型
	/// </summary>
	/// <param name="otherCell"></param>
	/// <returns></returns>
	public HexEdgeType GetEdgeType(HexCell otherCell)
	{
		return HexMetrics.GetEdgeType(
			elevation, otherCell.elevation
		);
	}

	/// <summary>
	/// 返回格子的位置
	/// </summary>
	public Vector3 Position
	{
		get { return transform.localPosition; }
	}



	/// <summary>
	/// 特殊内容索引
	/// </summary>
	int specialIndex;
	public int SpecialIndex
	{
		get
		{
			return specialIndex;
		}
		set
		{
			if (specialIndex != value && !HasRiver)
			{
				specialIndex = value;
				RemoveRoads();
				RefreshSelfOnly();
			}
		}
	}

	/// <summary>
	/// 是否是起始位置
	/// </summary>
	public bool IsStartPos
	{
		get
		{
			return specialIndex == (int)SpecialIndexType.Pope;
		}
	}

	/// <summary>
	/// 是否是金矿
	/// </summary>
	public bool IsGoldMine
	{
		get
		{
			return specialIndex == (int)SpecialIndexType.Gold;
		}

	}

	/// <summary>
	/// 判断是否具备特殊近况
	/// </summary>
	public bool IsSpecial
	{
		get
		{
			return specialIndex > 0;
		}
	}

	enum SpecialIndexType
	{
		Pope = 1, // 教皇的初始位置	
		Gold = 2 // 金矿
	};


	/// <summary>
	/// 河床的Y轴高度
	/// </summary>
	public float StreamBedY
	{
		get
		{
			return
				(elevation + HexMetrics.streamBedElevationOffset) *
				HexMetrics.elevationStep;
		}
	}

	/// <summary>
	/// 更新Chunk的格子信息/更新格子及其周遭格子的信息
	/// </summary>
	public void Refresh()
	{
		if (Chunk) //error check
		{
			Chunk.Refresh();
			for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
			{
				//HexCell neighbor = neighbors[i];
				if (TryGetNeighbor(d, out HexCell neighbor) && neighbor.Chunk != Chunk)
				{
					neighbor.Chunk.Refresh();
				}
			}
		}
	}

	/// <summary>
	/// 河的Y轴位置
	/// </summary>
	public float RiverSurfaceY
	{
		get
		{
			return
				(elevation + HexMetrics.waterElevationOffset) *
				HexMetrics.elevationStep;
		}
	}

	/// <summary>
	/// 水面的Y轴位置
	/// </summary>
	public float WaterSurfaceY
	{
		get
		{
			return
				(waterLevel + HexMetrics.waterElevationOffset) *
				HexMetrics.elevationStep;
		}
	}


	/// <summary>
	/// 根据格子的高低差判断是否能延展河流
	/// </summary>
	/// <param name="neighbor"></param>
	/// <returns></returns>
	bool IsValidRiverDestination(HexCell neighbor)
	{
		return neighbor && (
			elevation >= neighbor.elevation || waterLevel == neighbor.elevation
		);
	}

	/// <summary>
	/// 修正格子的河流
	/// </summary>
	void ValidateRivers()
	{
		if (HasOutgoingRiver && !IsValidRiverDestination(GetNeighbor(OutgoingRiver)))
		{
			RemoveOutgoingRiver();
		}
		if (
			HasIncomingRiver && !GetNeighbor(IncomingRiver).IsValidRiverDestination(this)
		)
		{
			RemoveIncomingRiver();
		}
	}

	/// <summary>
	/// 格子的Feature
	/// 1. 控制格子的视觉表现（草/树...)
	/// 2. 0>> 该格子没有某类Feature
	/// </summary>
	int forestLevel, farmLevel, plantLevel;

	public int ForestLevel
	{
		get { return forestLevel; }
		set
		{
			if (forestLevel != value)
			{
				forestLevel = value;
				RefreshSelfOnly();
			}
		}
	}

	public int FarmLevel
	{
		get { return farmLevel; }
		set
		{
			if (farmLevel != value)
			{
				farmLevel = value;
				RefreshSelfOnly();
			}
		}
	}

	/// <summary>
	/// 格子的Plant level
	/// </summary>
	public int PlantLevel
	{
		get { return plantLevel; }
		set
		{
			if (plantLevel != value)
			{
				plantLevel = value;
				RefreshSelfOnly();
			}
		}
	}

	public int SearchPhase { get; set; }

	public void Save(BinaryWriter writer)
	{
		writer.Write((byte)terrainTypeIndex);
		writer.Write((byte)(elevation + 127));
		writer.Write((byte)waterLevel);
		writer.Write((byte)forestLevel);
		writer.Write((byte)farmLevel);
		writer.Write((byte)plantLevel);
		writer.Write((byte)specialIndex);
		writer.Write(Walled);


		if (HasIncomingRiver)
		{
			writer.Write((byte)(IncomingRiver + 128));
		}
		else
		{
			writer.Write((byte)0);
		}

		if (HasOutgoingRiver)
		{
			writer.Write((byte)(OutgoingRiver + 128));
		}
		else
		{
			writer.Write((byte)0);
		}

		writer.Write((byte)(flags & HexFlags.Roads));
	}

	/// <summary>
	/// 读取单个格子的信息
	/// </summary>
	/// <param name="reader"></param>
	public void Load(BinaryReader reader, int header)
	{

		terrainTypeIndex = reader.ReadByte();
		elevation = reader.ReadByte();
		if (header >= 4)
		{
			elevation -= 127;
		}

		RefreshPosition();
		waterLevel = reader.ReadByte(); // 格子的水平面高度
		forestLevel = reader.ReadByte(); // 格子的urban level
		farmLevel = reader.ReadByte(); // 格子的farm level
		plantLevel = reader.ReadByte(); // 格子的plant level
		specialIndex = reader.ReadByte();
		if (reader.ReadBoolean())
		{
			flags = flags.With(HexFlags.Walled);
		}

		byte riverData = reader.ReadByte();
		if (riverData >= 128)
		{
			flags = flags.WithRiverIn((HexDirection)(riverData - 128));
		}

		riverData = reader.ReadByte();
		if (riverData >= 128)
		{
			flags = flags.WithRiverOut((HexDirection)(riverData - 128));
		}
	
		flags |= (HexFlags)reader.ReadByte();

	}

	/// <summary>
	/// 当前格子的位置
	/// </summary>
	void RefreshPosition()
	{
		Vector3 position = transform.localPosition;
		position.y = elevation * HexMetrics.elevationStep;
		position.y +=
			(HexMetrics.SampleNoise(position).y * 2f - 1f) *
			HexMetrics.elevationPerturbStrength;
		transform.localPosition = position;
		
		Vector3 uiPosition = uiRect.localPosition;
		uiPosition.z = -position.y;
		uiRect.localPosition = uiPosition;
	}


	/// <summary>
	/// HexCell距离
	/// </summary>
	public int Distance
	{
		get { return distance; }
		set
		{
			distance = value;
		}
	}

	public void SetLabel(string text)
	{
		UnityEngine.UI.Text label = uiRect.GetComponent<Text>();
		label.text = text;
	}

	public void DisableHighlight()
	{
		Image highlight = uiRect.GetChild(0).GetComponent<Image>();
		highlight.enabled = false;
	}

	public void EnableHighlight(Color color)
	{
		Image highlight = uiRect.GetChild(0).GetComponent<Image>();
		highlight.color = color;
		highlight.enabled = true;
	}

	// 搜索优先级
	public int SearchPriority
	{
		get
		{
			return distance + SearchHeuristic;
		}
	}

}

