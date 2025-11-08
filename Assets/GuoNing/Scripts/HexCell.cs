using UnityEngine;
using UnityEngine.UI;
using System.IO;



public enum TerrainType
{
	UnPassable=0,  // 不可通行
	Capable=1,     // 可通行 可占领
	Passable= 2    // 可通行 不可占领

}

public enum TerrainTextureType
{
	Sand=0,				// 海滩
	Desert=1,			// 沙漠
	Forest=2,			// 草地
	RainForest=3,		// 雨林
	FallingForest=4,	// 落叶林
	Lake=5,				// 湖泊
	Swamp=6,			// 沼泽
	Snow=7,				// 雪地

}

public enum SpecialIndexType
{
	Pope = 1, // 教皇的初始位置	
	Gold = 2, // 金矿
	Temple=3, // 特殊建筑
};


public enum PlantType
{
	BigStone = 0,       // 冰原 (大石头)
	Wasteland = 1,    // 荒地 (中石头+草)
	SmallStone = 2,   // 岩石地 (小石头+草)
	DessertGrass = 3,// 沙漠 (小石头+仙人掌)
	Grass = 4,        // 草地 (草)
	Bush = 5,           // 灌木
	Forest = 6,       // 森林 (小树)
	FallingForest = 7,// 落叶林 (落叶树)
	RainForest = 8,   // 雨林 (大树)
	Swamp = 9,        // 沼泽
	DessertTree = 10	// 沙漠树
}



/// <summary>
/// Container component for hex cell data.
/// </summary>
[System.Serializable]
public class HexCell : MonoBehaviour
{
	/// <summary>
	/// Hexagonal coordinates unique to the cell.
	/// </summary>
	public HexCoordinates Coordinates
	{ get; set; }

	/// <summary>
	/// Transform component for the cell's UI visiualization. 
	/// </summary>
	public RectTransform UIRect
	{ get; set; }

	/// <summary>
	/// Grid that contains the cell.
	/// </summary>
	public HexGrid Grid
	{ get; set; }

	/// <summary>
	/// Grid chunk that contains the cell.
	/// </summary>
	public HexGridChunk Chunk
	{ get; set; }

	/// <summary>
	/// Unique global index of the cell.
	/// </summary>
	public int Index
	{ get; set; }

	/// <summary>
	/// Map column index of the cell.
	/// </summary>
	public int ColumnIndex
	{ get; set; }

	/// <summary>
	/// 是否有单位
	/// </summary>
	public bool Unit
	{
		get; set;
	}

	/// <summary>
	/// Surface elevation level.
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
			//Grid.ShaderData.ViewElevationChanged(this);
			RefreshPosition();
			ValidateRivers();

			for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
			{
				if (flags.HasRoad(d) && GetElevationDifference(d) > 1)
				{
					RemoveRoad(d);
				}
			}

			Refresh();
		}
	}

	/// <summary>
	/// Water elevation level.
	/// </summary>
	public int WaterLevel
	{
		get => waterLevel;
		set
		{
			if (waterLevel == value)
			{
				return;
			}
			waterLevel = value;
			//Grid.ShaderData.ViewElevationChanged(this);
			ValidateRivers();
			Refresh();
		}
	}

	/// <summary>
	/// Elevation at which the cell is visible. Highest of surface and water level.
	/// </summary>
	public int ViewElevation => elevation >= waterLevel ? elevation : waterLevel;

	/// <summary>
	/// Whether the cell counts as underwater, which is when water is higher than surface.
	/// </summary>
	public bool IsUnderwater => waterLevel > elevation;

	/// <summary>
	/// Whether there is an incoming river.
	/// </summary>
	public bool HasIncomingRiver => flags.HasAny(HexFlags.RiverIn);

	/// <summary>
	/// Whether there is an outgoing river.
	/// </summary>
	public bool HasOutgoingRiver => flags.HasAny(HexFlags.RiverOut);

	/// <summary>
	/// Whether there is a river, either incoming, outgoing, or both.
	/// </summary>
	public bool HasRiver => flags.HasAny(HexFlags.River);

	/// <summary>
	/// Whether a river begins or ends in the cell.
	/// </summary>
	public bool HasRiverBeginOrEnd => HasIncomingRiver != HasOutgoingRiver;

	/// <summary>
	/// Whether the cell contains roads.
	/// </summary>
	public bool HasRoads => flags.HasAny(HexFlags.Roads);

	/// <summary>
	/// Incoming river direction, if applicable.
	/// </summary>
	public HexDirection IncomingRiver => flags.RiverInDirection();

	/// <summary>
	/// Outgoing river direction, if applicable.
	/// </summary>
	public HexDirection OutgoingRiver => flags.RiverOutDirection();

	/// <summary>
	/// Local position of this cell.
	/// </summary>
	//public Vector3 Position
	//{ get; set; }
	public Vector3 Position
	{
		get { return transform.localPosition; }
	}

	/// <summary>
	/// Vertical positions the the stream bed, if applicable.
	/// </summary>
	public float StreamBedY =>
		(elevation + HexMetrics.streamBedElevationOffset) * HexMetrics.elevationStep;

	/// <summary>
	/// Vertical position of the river's surface, if applicable.
	/// </summary>
	public float RiverSurfaceY =>
		(elevation + HexMetrics.waterElevationOffset) * HexMetrics.elevationStep;

	/// <summary>
	/// Vertical position of the water surface, if applicable.
	/// </summary>
	public float WaterSurfaceY =>
		(waterLevel + HexMetrics.waterElevationOffset) * HexMetrics.elevationStep;

	/// <summary>
	/// Urban feature level.
	/// </summary>
	public int UrbanLevel
	{
		get => urbanLevel;
		set
		{
			if (urbanLevel != value)
			{
				urbanLevel = value;
				RefreshSelfOnly();
			}
		}
	}

	/// <summary>
	/// Farm feature level.
	/// </summary>
	public int FarmLevel
	{
		get => farmLevel;
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
	/// Plant feature level.
	/// </summary>
	public int PlantLevel
	{
		get => plantLevel;
		set
		{
			if (plantLevel != value)
			{
				plantLevel = value;
				RefreshSelfOnly();
			}
		}
	}

	/// <summary>
	/// Plant type
	/// </summary>
	public int PlantIndex
	{
		get=> plantIndex;
		set
		{
			if (plantIndex != value)
			{
				plantIndex = value;
				RefreshSelfOnly();
			}
		}
	}

	/// <summary>
	/// Special feature index.
	/// </summary>
	public int SpecialIndex
	{
		get => specialIndex;
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
	/// Whether the cell contains a special feature.
	/// </summary>
	public bool IsSpecial => specialIndex > 0;

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
	/// Whether the cell is considered inside a walled region.
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
	/// Terrain type index.
	/// </summary>
	public int TerrainTypeIndex
	{
		get => terrainTypeIndex;
		set
		{
			if (terrainTypeIndex != value)
			{
				terrainTypeIndex = value;
				Grid.ShaderData.RefreshTerrain(this);
			}
		}
	}

	/// <summary>
	/// Whether the cell counts as visible.
	/// </summary>
	//public bool IsVisible => visibility > 0 && Explorable;
	public bool IsVisible = true; 

	/// <summary>
	/// Whether the cell is explorable. If not it never counts as explored or visible.
	/// </summary>
	public bool Explorable
	{
		get => flags.HasAny(HexFlags.Explorable);
		set => flags = value ?
			flags.With(HexFlags.Explorable) : flags.Without(HexFlags.Explorable);
	}

	/// <summary>
	/// Distance data used by pathfiding algorithm.
	/// </summary>
	public int Distance
	{
		get => distance;
		set => distance = value;
	}


	/// <summary>
	/// Pathing data used by pathfinding algorithm.
	/// </summary>
	public int PathFromIndex
	{ get; set; }

	/// <summary>
	/// Heuristic data used by pathfinding algorithm.
	/// </summary>
	public int SearchHeuristic
	{ get; set; }

	/// <summary>
	/// Search priority used by pathfinding algorithm.
	/// </summary>
	public int SearchPriority => distance + SearchHeuristic;

	/// <summary>
	/// Search phases data used by pathfinding algorithm.
	/// </summary>
	public int SearchPhase
	{ get; set; }

	/// <summary>
	/// Linked list reference used by <see cref="HexCellPriorityQueue"/> for pathfinding.
	/// </summary>
	[field: System.NonSerialized]
	public HexCell NextWithSamePriority
	{ get; set; }

	/// <summary>
	/// Bit flags for cell data, currently rivers, roads, walls, and exploration.
	/// </summary>
	HexFlags flags;

	int terrainTypeIndex;

	int elevation = int.MinValue;
	int waterLevel;

	int urbanLevel, farmLevel, plantLevel;

	int plantIndex;	//植被种类

	int specialIndex;

	int distance;

	private int visibility = 1;


	/// <summary>
	/// Increment visibility level.
	/// </summary>
	public void IncreaseVisibility()
	{
		visibility += 1;
		if (visibility == 1)
		{
			Grid.ShaderData.RefreshVisibility(this);
		}
	}

	/// <summary>
	/// Decrement visiblility level.
	/// </summary>
	public void DecreaseVisibility()
	{
		visibility -= 1;
		if (visibility == 0)
		{
			Grid.ShaderData.RefreshVisibility(this);
		}
	}

	/// <summary>
	/// Reset visibility level to zero.
	/// </summary>
	public void ResetVisibility()
	{
		if (visibility > 0)
		{
			visibility = 0;
			Grid.ShaderData.RefreshVisibility(this);
		}
	}
	

	/// <summary>
	/// Get one of the neighbor cells. Only valid if that neighbor exists.
	/// </summary>
	/// <param name="direction">Neighbor direction relative to the cell.</param>
	/// <returns>Neighbor cell, if it exists.</returns>
	public HexCell GetNeighbor(HexDirection direction) =>
		Grid.GetCell(Coordinates.Step(direction));

	/// <summary>
	/// Try to get one of the neighbor cells.
	/// </summary>
	/// <param name="direction">Neighbor direction relative to the cell.</param>
	/// <param name="cell">The neighbor cell, if it exists.</param>
	/// <returns>Whether the neighbor exists.</returns>
	public bool TryGetNeighbor(HexDirection direction, out HexCell cell) =>
		Grid.TryGetCell(Coordinates.Step(direction), out cell);

	/// <summary>
	/// Get the <see cref="HexEdgeType"/> of a cell edge.
	/// </summary>
	/// <param name="direction">Edge direction relative to the cell.</param>
	/// <returns><see cref="HexEdgeType"/> based on the neighboring cells.</returns>
	public HexEdgeType GetEdgeType(HexDirection direction) => HexMetrics.GetEdgeType(
		elevation, GetNeighbor(direction).elevation
	);

	/// <summary>
	/// Get the <see cref="HexEdgeType"/> based on this and another cell.
	/// </summary>
	/// <param name="otherCell">Other cell to consider as neighbor.</param>
	/// <returns><see cref="HexEdgeType"/> based on this and the other cell.</returns>
	public HexEdgeType GetEdgeType(HexCell otherCell) => HexMetrics.GetEdgeType(
		elevation, otherCell.elevation
	);

	/// <summary>
	/// Whether a river goes through a specific cell edge.
	/// </summary>
	/// <param name="direction">Edge direction relative to the cell.</param>
	/// <returns>Whether a river goes through the edge.</returns>
	public bool HasRiverThroughEdge(HexDirection direction) =>
		flags.HasRiverIn(direction) || flags.HasRiverOut(direction);

	/// <summary>
	/// Whether an incoming river goes through a specific cell edge.
	/// </summary>
	/// <param name="direction">Edge direction relative to the cell.</param>
	/// <returns>Whether an incoming river goes through the edge.</returns>
	public bool HasIncomingRiverThroughEdge(HexDirection direction) =>
		flags.HasRiverIn(direction);

	/// <summary>
	/// Remove the incoming river, if it exists.
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

	/// <summary>
	/// Remove the outgoing river, if it exists.
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
	/// Remove both incoming and outgoing rivers, if they exist.
	/// </summary>
	public void RemoveRiver()
	{
		RemoveOutgoingRiver();
		RemoveIncomingRiver();
	}

	/// <summary>
	/// Define an outgoing river.
	/// </summary>
	/// <param name="direction">Direction of the river.</param>
	public void SetOutgoingRiver(HexDirection direction)
	{
		if (flags.HasRiverOut(direction))
		{
			return;
		}

		HexCell neighbor = GetNeighbor(direction);
		if (!IsValidRiverDestination(neighbor))
		{
			return;
		}

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
	/// Whether a road goes through a specific cell edge.
	/// </summary>
	/// <param name="direction">Edge direction relative to cell.</param>
	/// <returns>Whether a road goes through the edge.</returns>
	public bool HasRoadThroughEdge(HexDirection direction) => flags.HasRoad(direction);

	/// <summary>
	/// Define a road that goes in a specific direction.
	/// </summary>
	/// <param name="direction">Direction relative to cell.</param>
	public void AddRoad(HexDirection direction)
	{
		if (
			!flags.HasRoad(direction) && !HasRiverThroughEdge(direction) &&
			!IsSpecial && !GetNeighbor(direction).IsSpecial &&
			GetElevationDifference(direction) <= 1
		)
		{
			flags = flags.WithRoad(direction);
			HexCell neighbor = GetNeighbor(direction);
			neighbor.flags = neighbor.flags.WithRoad(direction.Opposite());
			neighbor.RefreshSelfOnly();
			RefreshSelfOnly();
		}
	}

	/// <summary>
	/// Remove all roads from the cell.
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
	/// Get the elevation difference with a neighbor. The indicated neighbor must exist.
	/// </summary>
	/// <param name="direction">Direction to the neighbor, relative to the cell.</param>
	/// <returns>Absolute elevation difference.</returns>
	public int GetElevationDifference(HexDirection direction)
	{
		int difference = elevation - GetNeighbor(direction).elevation;
		return difference >= 0 ? difference : -difference;
	}

	bool IsValidRiverDestination(HexCell neighbor) =>
		neighbor && (elevation >= neighbor.elevation || waterLevel == neighbor.elevation);

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

	void RemoveRoad(HexDirection direction)
	{
		flags = flags.WithoutRoad(direction);
		HexCell neighbor = GetNeighbor(direction);
		neighbor.flags = neighbor.flags.WithoutRoad(direction.Opposite());
		neighbor.RefreshSelfOnly();
		RefreshSelfOnly();
	}

	void RefreshPosition()
	{
		Vector3 position = Position;
		position.y = elevation * HexMetrics.elevationStep;
		position.y +=
			(HexMetrics.SampleNoise(position).y * 2f - 1f) *
			HexMetrics.elevationPerturbStrength;
		transform.localPosition = position;

		Vector3 uiPosition = UIRect.localPosition;
		uiPosition.z = -position.y;
		UIRect.localPosition = uiPosition;
	}

	void Refresh()
	{
		if (Chunk)
		{
			Chunk.Refresh();
			for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
			{
				if (TryGetNeighbor(d, out HexCell neighbor) && neighbor.Chunk != Chunk)
				{
					neighbor.Chunk.Refresh();
				}
			}
		}
	}

	void RefreshSelfOnly()
	{
		Chunk.Refresh();
	}

	/// <summary>
	/// Save the cell data.
	/// </summary>
	/// <param name="writer"><see cref="BinaryWriter"/> to use.</param>
	public void Save(BinaryWriter writer)
	{
		writer.Write((byte)terrainTypeIndex);
		writer.Write((byte)(elevation + 127));
		writer.Write((byte)waterLevel);
		writer.Write((byte)urbanLevel);
		writer.Write((byte)farmLevel);
		writer.Write((byte)plantLevel);
		writer.Write((byte)plantIndex);	// 2025.10.29 
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
		//writer.Write(IsExplored);
	}

	/// <summary>
	/// Load the cell data.
	/// </summary>
	/// <param name="reader"><see cref="BinaryReader"/> to use.</param>
	/// <param name="header">Header version.</param>
	public void Load(BinaryReader reader, int header)
	{
		flags &= HexFlags.Explorable;
		terrainTypeIndex = reader.ReadByte();
		elevation = reader.ReadByte();
		if (header >= 4)
		{
			elevation -= 127;
		}
		RefreshPosition();
		waterLevel = reader.ReadByte();
		urbanLevel = reader.ReadByte();
		farmLevel = reader.ReadByte();
		plantLevel = reader.ReadByte();
		plantIndex = reader.ReadByte();
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

		//IsExplored = header >= 3 && reader.ReadBoolean();
	
		Grid.ShaderData.RefreshTerrain(this);
		Grid.ShaderData.RefreshVisibility(this);
	}

	/// <summary>
	/// Set the cell's UI label.
	/// </summary>
	/// <param name="text">Label text.</param>
	public void SetLabel(string text)
	{
		UnityEngine.UI.Text label = UIRect.GetComponent<Text>();
		label.text = text;
	}

	/// <summary>
	/// Disable the cell's highlight.
	/// </summary>
	public void DisableHighlight()
	{
		Image highlight = UIRect.GetChild(0).GetComponent<Image>();
		highlight.enabled = false;
	}

	/// <summary>
	/// Enable the cell's highlight. 
	/// </summary>
	/// <param name="color">Highlight color.</param>
	public void EnableHighlight(Color color)
	{
		Image highlight = UIRect.GetChild(0).GetComponent<Image>();
		highlight.color = color;
		highlight.enabled = true;
	}

	/// <summary>
	/// Set arbitrary map data for this cell's <see cref="ShaderData"/>.
	/// </summary>
	/// <param name="data">Data value, 0-1 inclusive.</param>
	public void SetMapData(float data) => Grid.ShaderData.SetMapData(this, data);

	/// <summary>
	/// A cell counts as true if it is not null, otherwise as false.
	/// </summary>
	/// <param name="cell">The cell to check.</param>
	public static implicit operator bool(HexCell cell) => cell != null;
}
