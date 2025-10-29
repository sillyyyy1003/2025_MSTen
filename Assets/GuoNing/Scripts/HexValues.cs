using System.IO;
using UnityEngine;
/*
/// <summary>
/// Values that describe the contents of a cell.
/// </summary>
[System.Serializable]
public struct HexValues
{
	/// <summary>
	/// Updated bit layout (32 bits total)
	/// <code>
	/// TTTTTTTT --SSSSSS PPPPUUFF WWWE EEEEE
	/// 
	/// E (5 bits): Elevation（地形高度）
	/// W (3 bits): Water Level（水位）
	/// U (2 bits): Urban Level（城市等级）
	/// F (2 bits): Farm Level（农田等级）
	/// P (4 bits): Plant Level（植被等级）
	/// S (6 bits): Special Index（特殊地形或建筑索引）
	/// T (8 bits): Terrain Type Index（地形类型索引）
	/// </code>
	/// </summary>
	int values;

	readonly int Get(int mask, int shift) =>
		(int)((uint)values >> shift) & mask;

	readonly HexValues With(int value, int mask, int shift) => new()
	{
		values = (values & ~(mask << shift)) | ((value & mask) << shift)
	};

	public readonly int Elevation => Get(31, 0) - 15;

	public readonly HexValues WithElevation(int value) =>
		With(value + 15, 31, 0);

	public readonly int WaterLevel => Get(7, 5);

	public readonly int ViewElevation => Mathf.Max(Elevation, WaterLevel);

	public readonly bool IsUnderwater => WaterLevel > Elevation;

	public readonly HexValues WithWaterLevel(int value) => With(value, 7, 5);

	public readonly int UrbanLevel => Get(3, 8);

	public readonly HexValues WithUrbanLevel(int value) => With(value, 3, 8);

	public readonly int FarmLevel => Get(3, 10);

	public readonly HexValues WithFarmLevel(int value) => With(value, 3, 10);

	public readonly int PlantLevel => Get(15, 12); // 4 bits (0–15)

	public readonly HexValues WithPlantLevel(int value) => With(value, 15, 12);

	public readonly int SpecialIndex => Get(63, 16); // 6 bits (0–63)

	public readonly HexValues WithSpecialIndex(int index) =>
		With(index, 63, 16);

	public readonly int TerrainTypeIndex => Get(255, 24);

	public readonly HexValues WithTerrainTypeIndex(int index) =>
		With(index, 255, 24);

	/// <summary>
	/// Save the values.
	/// </summary>
	public readonly void Save(BinaryWriter writer)
	{
		writer.Write((byte)TerrainTypeIndex);
		writer.Write((byte)(Elevation + 127));
		writer.Write((byte)WaterLevel);
		writer.Write((byte)UrbanLevel);
		writer.Write((byte)FarmLevel);
		writer.Write((byte)PlantLevel);
		writer.Write((byte)SpecialIndex);
	}

	/// <summary>
	/// Load the values.
	/// </summary>
	public static HexValues Load(BinaryReader reader, int header)
	{
		HexValues values = default;
		values = values.WithTerrainTypeIndex(reader.ReadByte());
		int elevation = reader.ReadByte();
		if (header >= 4)
		{
			elevation -= 127;
		}
		values = values.WithElevation(elevation);
		values = values.WithWaterLevel(reader.ReadByte());
		values = values.WithUrbanLevel(reader.ReadByte());
		values = values.WithFarmLevel(reader.ReadByte());
		values = values.WithPlantLevel(reader.ReadByte());
		return values.WithSpecialIndex(reader.ReadByte());
	}
}
*/

/// <summary>
/// Values that describe the contents of a cell.
/// </summary>
[System.Serializable]
public struct HexValues
{
	int values;

	readonly int Get(int mask, int shift) =>
		(int)((uint)values >> shift) & mask;

	readonly HexValues With(int value, int mask, int shift) => new()
	{
		values = (values & ~(mask << shift)) | ((value & mask) << shift)
	};

	// --- Elevation (5 bits) ---
	public readonly int Elevation => Get(31, 0) - 15;
	public readonly HexValues WithElevation(int value) =>
		With(value + 15, 31, 0);

	// --- Water Level (3 bits) ---
	public readonly int WaterLevel => Get(7, 5);
	public readonly HexValues WithWaterLevel(int value) =>
		With(value, 7, 5);

	// --- Urban Level (2 bits) ---
	public readonly int UrbanLevel => Get(3, 8);
	public readonly HexValues WithUrbanLevel(int value) =>
		With(value, 3, 8);

	// --- Farm Level (2 bits) ---
	public readonly int FarmLevel => Get(3, 10);
	public readonly HexValues WithFarmLevel(int value) =>
		With(value, 3, 10);

	// --- Plant Level (4 bits) ---
	public readonly int PlantLevel => Get(15, 12);
	public readonly HexValues WithPlantLevel(int value) =>
		With(value, 15, 12);

	// --- Plant Index (2 bits, new) ---
	public readonly int PlantIndex => Get(3, 16);
	public readonly HexValues WithPlantIndex(int value) =>
		With(value, 3, 16);

	// --- Special Index (6 bits) ---
	public readonly int SpecialIndex => Get(63, 18);
	public readonly HexValues WithSpecialIndex(int index) =>
		With(index, 63, 18);

	// --- Terrain Type Index (6 bits) ---
	public readonly int TerrainTypeIndex => Get(63, 24);
	public readonly HexValues WithTerrainTypeIndex(int index) =>
		With(index, 63, 24);

	// Derived values
	public readonly int ViewElevation => Mathf.Max(Elevation, WaterLevel);
	public readonly bool IsUnderwater => WaterLevel > Elevation;

	/// <summary>
	/// Save the values.
	/// </summary>
	public readonly void Save(BinaryWriter writer)
	{
		writer.Write((byte)TerrainTypeIndex);
		writer.Write((byte)(Elevation + 127));
		writer.Write((byte)WaterLevel);
		writer.Write((byte)UrbanLevel);
		writer.Write((byte)FarmLevel);
		writer.Write((byte)PlantLevel);
		writer.Write((byte)PlantIndex);
		writer.Write((byte)SpecialIndex);
	}

	/// <summary>
	/// Load the values.
	/// </summary>
	public static HexValues Load(BinaryReader reader, int header)
	{
		HexValues values = default;
		values = values.WithTerrainTypeIndex(reader.ReadByte());
		int elevation = reader.ReadByte();
		if (header >= 4)
		{
			elevation -= 127;
		}
		values = values.WithElevation(elevation);
		values = values.WithWaterLevel(reader.ReadByte());
		values = values.WithUrbanLevel(reader.ReadByte());
		values = values.WithFarmLevel(reader.ReadByte());
		values = values.WithPlantLevel(reader.ReadByte());
		values = values.WithPlantIndex(reader.ReadByte());
		return values.WithSpecialIndex(reader.ReadByte());
	}
}
