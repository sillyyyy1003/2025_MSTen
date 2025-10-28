using System.IO;
using UnityEngine;

/// <summary>
/// Values that describe the contents of a cell.
/// </summary>
[System.Serializable]
public struct HexValues
{
	/// <summary>
	/// Seven values stored in 32 bits.
	/// <code>
	/// TTTTTTTT SSSSSSSS PPFFUUWW WWWEEEEE
	///
	/// E (5 bits): Elevation（地形高度）
	/// W (3 bits): Water Level（水位）
	/// U (2 bits): Urban Level（城市等级）
	/// F (2 bits): Farm Level（农田等级）
	/// P (2 bits): Plant Level（植被等级）
	/// S (8 bits): Special Index（特殊地形或建筑索引）
	/// T (8 bits): Terrain Type Index（地形类型索引）
	/// </code>
	/// </summary>
	/// <remarks>Not readonly to support hot reloading in Unity.</remarks>
#pragma warning disable IDE0044 // Add readonly modifier
	int values;
#pragma warning restore IDE0044 // Add readonly modifier

	readonly int Get(int mask, int shift) =>
		(int)((uint)values >> shift) & mask;

	readonly HexValues With(int value, int mask, int shift) => new()
	{
		values = (values & ~(mask << shift)) | ((value & mask) << shift)
	};

	public readonly int Elevation => Get(31, 0) - 15;

	public readonly HexValues WithElevation(int value) =>
		With(value + 15, 31, 0);

	public readonly int WaterLevel => Get(31, 5);

	public readonly int ViewElevation => Mathf.Max(Elevation, WaterLevel);

	public readonly bool IsUnderwater => WaterLevel > Elevation;

	public readonly HexValues WithWaterLevel(int value) => With(value, 31, 5);
	
	public readonly int UrbanLevel => Get(3, 10);

	public readonly HexValues WithUrbanLevel(int value) => With(value, 3, 10);

	public readonly int FarmLevel => Get(3, 12);

	public readonly HexValues WithFarmLevel(int value) => With(value, 3, 12);

	public readonly int PlantLevel => Get(3, 14);

	public readonly HexValues WithPlantLevel(int value) => With(value, 3, 14);

	public readonly int SpecialIndex => Get(255, 16);

	public readonly HexValues WithSpecialIndex(int index) =>
		With(index, 255, 16);
	
	public readonly int TerrainTypeIndex => Get(255, 24);
	
	public readonly HexValues WithTerrainTypeIndex(int index) =>
		With(index, 255, 24);

	/// <summary>
	/// Save the values.
	/// </summary>
	/// <param name="writer"><see cref="BinaryWriter"/> to use.</param>
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
	/// <param name="reader"><see cref="BinaryReader"/> to use.</param>
	/// <param name="header">Header version.</param>
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
