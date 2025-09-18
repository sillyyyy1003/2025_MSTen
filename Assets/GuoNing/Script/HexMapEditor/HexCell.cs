using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
}

public enum HexDirection
{
	NE,	// NorthEast 
	E,	// East
	SE,	// SouthEast
	SW,	// SouthWest
	W,	// West
	NW	// NorthWest
}



public enum HexEdgeType
{
	Flat,	
	Slope, 
	Cliff
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



public class HexCell : MonoBehaviour
{
	public HexCoordinates coordinates;  // Hex coordinate(https://catlikecoding.com/unity/tutorials/hex-map/part-1/hexagonal-coordinates/cube-coordinates.png)

	public RectTransform uiRect;
	public HexGridChunk chunk;


	[SerializeField]
	bool hasIncomingRiver, hasOutgoingRiver;    // has river 
	HexDirection incomingRiver, outgoingRiver;  // river direction
	


	bool isVacancy; //whether this cell is vacant

	public bool IsVacancy
	{
		get
		{
			return isVacancy;
		}
		set
		{
			isVacancy = value;
		}

	}
	public int WaterLevel
	{
		get
		{
			return waterLevel;
		}
		set
		{
			if (waterLevel == value)
			{
				return;
			}
			waterLevel = value;
			Refresh();
		}
	}
	int waterLevel; // water level

	public bool IsUnderwater //whether this hex cell is underwater according to elevation and water level
	{
		get
		{
			return waterLevel > elevation;
		}
	}


	Color color;
	public Color Color		//cell color
	{
		get
		{
			return color;
		}
		set
		{
			if (color == value)
			{
				return;
			}
			color = value;
			Refresh();
		}
	}

	public int Elevation
	{
		get
		{
			return elevation;
		}
		set
		{
			if (elevation == value)
			{
				return;
			}
			elevation = value;
			Vector3 position = transform.localPosition;
			position.y = value * HexMetrics.elevationStep;
			position.y += (HexMetrics.SampleNoise(position).y * 2f - 1f) * HexMetrics.elevationPerturbStrength;//applies this perturbation to the cell's vertical position.
			transform.localPosition = position;

			Vector3 uiPosition = uiRect.localPosition;
			uiPosition.z = -position.y;
			uiRect.localPosition = uiPosition;

			// river
			if (
				hasOutgoingRiver &&
				elevation < GetNeighbor(outgoingRiver).elevation
			)
			{
				RemoveOutgoingRiver();
			}
			if (
				hasIncomingRiver &&
				elevation > GetNeighbor(incomingRiver).elevation
			)
			{
				RemoveIncomingRiver();
			}

			Refresh();  // Refresh the chunk this cell belongs to
		}
	}

	public bool HasIncomingRiver
	{
		get
		{
			return hasIncomingRiver;
		}
	}

	public bool HasOutgoingRiver
	{
		get
		{
			return hasOutgoingRiver;
		}
	}

	public HexDirection IncomingRiver
	{
		get
		{
			return incomingRiver;
		}
	}

	public HexDirection OutgoingRiver
	{
		get
		{
			return outgoingRiver;
		}
	}

	public bool HasRiver
	{
		get
		{
			return hasIncomingRiver || hasOutgoingRiver;
		}
	}
	// is river begin or end
	public bool HasRiverBeginOrEnd
	{
		get
		{
			return hasIncomingRiver != hasOutgoingRiver;
		}
	}

	// has river through edge
	public bool HasRiverThroughEdge(HexDirection direction)
	{
		return
			hasIncomingRiver && incomingRiver == direction ||
			hasOutgoingRiver && outgoingRiver == direction;
	}

	// remove outgoing river;
	public void RemoveOutgoingRiver()
	{
		if (!hasOutgoingRiver)
		{
			return;
		}

		hasOutgoingRiver = false;
		RefreshSelfOnly();

		HexCell neighbor = GetNeighbor(outgoingRiver);
		neighbor.hasIncomingRiver = false;
		neighbor.RefreshSelfOnly();
	}

	//Refresh chunk
	void RefreshSelfOnly()
	{
		chunk.Refresh();
	}

	// remove incoming river
	public void RemoveIncomingRiver()
	{
		if (!hasIncomingRiver)
		{
			return;
		}
		hasIncomingRiver = false;
		RefreshSelfOnly();

		HexCell neighbor = GetNeighbor(incomingRiver);
		neighbor.hasOutgoingRiver = false;
		neighbor.RefreshSelfOnly();
	}

	// remove in/out river
	public void RemoveRiver()
	{
		RemoveOutgoingRiver();
		RemoveIncomingRiver();
	}

	public void SetOutgoingRiver(HexDirection direction)
	{
		// if has river do nothing
		if (hasOutgoingRiver && outgoingRiver == direction)
		{
			return;
		}
		// get neighbor if neighbor is higher than this cell do nothing
		HexCell neighbor = GetNeighbor(direction);
		if (!neighbor || elevation < neighbor.elevation)
		{
			return;
		}

		// remove previous outgoing river
		RemoveOutgoingRiver();
		if (hasIncomingRiver && incomingRiver == direction)
		{
			RemoveIncomingRiver();
		}

		hasOutgoingRiver = true;
		outgoingRiver = direction;
		RefreshSelfOnly();

		// set neighbor in coming river
		neighbor.RemoveIncomingRiver();
		neighbor.hasIncomingRiver = true;
		neighbor.incomingRiver = direction.Opposite();
		neighbor.RefreshSelfOnly();
	}

	public int elevation = int.MinValue;    // hex height

	[SerializeField]
	HexCell[] neighbors;    // 6 neighbors of this cell

	/// <summary>
	/// Find neighbor cell in the given direction
	/// </summary>
	/// <param name="direction"></param>
	/// <returns></returns>
	public HexCell GetNeighbor(HexDirection direction)
	{
		return neighbors[(int)direction];
	}

	public void SetNeighbor(HexDirection direction, HexCell cell)
	{
		neighbors[(int)direction] = cell;
		cell.neighbors[(int)direction.Opposite()] = this;
	}

	/// <summary>
	/// Find edge type between this cell and its neighbor in the given direction
	/// </summary>
	/// <param name="direction"></param>
	/// <returns></returns>
	public HexEdgeType GetEdgeType(HexDirection direction)
	{
		return HexMetrics.GetEdgeType(
			elevation, neighbors[(int)direction].elevation
		);
	}

	public HexEdgeType GetEdgeType(HexCell otherCell)
	{
		return HexMetrics.GetEdgeType(
			elevation, otherCell.elevation
		);
	}

	/// <summary>
	///  retrieve cell's position
	/// </summary>
	public Vector3 Position
	{
		get
		{
			return transform.localPosition;
		}
	}

	public float StreamBedY
	{
		get
		{
			return
				(elevation + HexMetrics.streamBedElevationOffset) *
				HexMetrics.elevationStep;
		}
	}

	void Refresh()
	{
		if (chunk) //error check
		{
			chunk.Refresh();
			for (int i = 0; i < neighbors.Length; i++)
			{
				HexCell neighbor = neighbors[i];
				if (neighbor != null && neighbor.chunk != chunk)
				{
					neighbor.chunk.Refresh();
				}
			}
	

		}
	}

	public float RiverSurfaceY
	{
		get
		{
			return
				(elevation + HexMetrics.waterElevationOffset) *
				HexMetrics.elevationStep;
		}
	}
	public float WaterSurfaceY
	{
		get
		{
			return
				(waterLevel + HexMetrics.waterElevationOffset) *
				HexMetrics.elevationStep;
		}
	}
}

