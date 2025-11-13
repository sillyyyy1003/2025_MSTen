using UnityEngine;
using System.IO;

/// <summary>
/// Immutable three-component hexagonal coordinates.
/// </summary>
[System.Serializable]
public struct HexCoordinates
{
	[SerializeField]
	private int x, z;

	/// <summary>
	/// X coordinate.
	/// </summary>
	public readonly int X => x;

	/// <summary>
	/// Z coordinate.
	/// </summary>
	public readonly int Z => z;

	/// <summary>
	/// Y coordinate, derived from X and Z.
	/// </summary>
	public readonly int Y => -X - Z;

	/// <summary>
	/// X position in hex space,
	/// where the distance between cell centers of east-west neighbors is one unit.
	/// </summary>
	public readonly float HexX => X + Z / 2 + ((Z & 1) == 0 ? 0f : 0.5f);

	/// <summary>
	/// Z position in hex space,
	/// where the distance between cell centers of east-west neighbors is one unit.
	/// </summary>
	public readonly float HexZ => Z * HexMetrics.outerToInner;

	/// <summary>
	/// Create hex coordinates.
	/// </summary>
	/// <param name="x">X coordinate.</param>
	/// <param name="z">Z coordinate.</param>
	public HexCoordinates(int x, int z)
	{
		if (HexMetrics.Wrapping)
		{
			int oX = x + z / 2;
			if (oX < 0)
			{
				x += HexMetrics.wrapSize;
			}
			else if (oX >= HexMetrics.wrapSize)
			{
				x -= HexMetrics.wrapSize;
			}
		}
		this.x = x;
		this.z = z;
	}

	/// <summary>
	/// Determine distance between this and another set of coordinates.
	/// Takes <see cref="HexMetrics.Wrapping"/> into account.
	/// </summary>
	/// <param name="other">Coordinate to determine distance to.</param>
	/// <returns>Distance in cells.</returns>
	public readonly int DistanceTo(HexCoordinates other)
	{
		int xy =
			(x < other.x ? other.x - x : x - other.x) +
			(Y < other.Y ? other.Y - Y : Y - other.Y);

		if (HexMetrics.Wrapping)
		{
			other.x += HexMetrics.wrapSize;
			int xyWrapped =
				(x < other.x ? other.x - x : x - other.x) +
				(Y < other.Y ? other.Y - Y : Y - other.Y);
			if (xyWrapped < xy)
			{
				xy = xyWrapped;
			}
			else
			{
				other.x -= 2 * HexMetrics.wrapSize;
				xyWrapped =
					(x < other.x ? other.x - x : x - other.x) +
					(Y < other.Y ? other.Y - Y : Y - other.Y);
				if (xyWrapped < xy)
				{
					xy = xyWrapped;
				}
			}
		}

		return (xy + (z < other.z ? other.z - z : z - other.z)) / 2;
	}

	/// <summary>
	/// Return (wrapped) coordinates after stepping one cell in a given direction.
	/// </summary>
	/// <param name="direction">Step direction.</param>
	/// <returns>Coordinates.</returns>
	public readonly HexCoordinates Step(HexDirection direction) => direction switch
	{
		HexDirection.NE => new HexCoordinates(x, z + 1),
		HexDirection.E => new HexCoordinates(x + 1, z),
		HexDirection.SE => new HexCoordinates(x + 1, z - 1),
		HexDirection.SW => new HexCoordinates(x, z - 1),
		HexDirection.W => new HexCoordinates(x - 1, z),
		_ => new HexCoordinates(x - 1, z + 1)
	};

	/// <summary>
	/// Create hex coordinates from array offset coordinates.
	/// </summary>
	/// <param name="x">X offset coordinate.</param>
	/// <param name="z">Z offset coordinate.</param>
	/// <returns>Hex coordinates.</returns>
	public static HexCoordinates FromOffsetCoordinates(int x, int z) => new(x - z / 2, z);

	/// <summary>
	/// Create hex coordinates for the cell that contains a position.
	/// </summary>
	/// <param name="position">A 3D position assumed to lie inside the map.</param>
	/// <returns>Hex coordinates.</returns>
	public static HexCoordinates FromPosition(Vector3 position)
	{
		// 将世界坐标的 x 转换为六边形坐标系的 x 值
		float x = position.x / HexMetrics.innerDiameter;
		float y = -x;

		// z 方向上的偏移量，因六边形行列是交错排列的，需要根据 z 值进行修正
		float offset = position.z / (HexMetrics.outerRadius * 3f);
		x -= offset;
		y -= offset;

		// 将浮点数坐标四舍五入为最接近的整数格子坐标
		int iX = Mathf.RoundToInt(x);
		int iY = Mathf.RoundToInt(y);
		int iZ = Mathf.RoundToInt(-x - y);

		// 校正误差：由于浮点数运算和四舍五入造成的精度问题，三个轴的和可能不为 0
		if (iX + iY + iZ != 0)
		{
			// 计算每个方向上的误差大小
			float dX = Mathf.Abs(x - iX);
			float dY = Mathf.Abs(y - iY);
			float dZ = Mathf.Abs(-x - y - iZ);

			// 修正误差最大的那个轴，保证坐标满足 iX + iY + iZ = 0
			if (dX > dY && dX > dZ)
			{
				iX = -iY - iZ;
			}
			else if (dZ > dY)
			{
				iZ = -iX - iY;
			}
		}

		// 将最终的整数六边形坐标（q, r）返回（此实现中使用 XZ 轴）
		return new HexCoordinates(iX, iZ);
	}
	/// <summary>
	/// Create a string representation of the coordinates.
	/// </summary>
	/// <returns>A string of the form (X, Y, Z).</returns>
	public readonly override string ToString() =>
		"(" + X.ToString() + ", " + Y.ToString() + ", " + Z.ToString() + ")";

	/// <summary>
	/// Create a multi-line string representation of the coordinates.
	/// </summary>
	/// <returns>A string of the form X\nY\nZ\n.</returns>
	public readonly string ToStringOnSeparateLines() =>
		X.ToString() + "\n" + Y.ToString() + "\n" + Z.ToString();

	/// <summary>
	/// Save the coordinates.
	/// </summary>
	/// <param name="writer"><see cref="BinaryWriter"/> to use.</param>
	public readonly void Save(BinaryWriter writer)
	{
		writer.Write(x);
		writer.Write(z);
	}

	/// <summary>
	/// Load coordinates.
	/// </summary>
	/// <param name="reader"><see cref="BinaryReader"/> to use.</param>
	/// <returns>The coordinates.</returns>
	public static HexCoordinates Load(BinaryReader reader)
	{
		HexCoordinates c;
		c.x = reader.ReadInt32();
		c.z = reader.ReadInt32();
		return c;
	}
}
