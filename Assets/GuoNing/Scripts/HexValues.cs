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
/// 表示单个六边形格子的各类数值数据（使用位字段压缩储存）。
/// </summary>
[System.Serializable]
public struct HexValues
{
	// 用一个 int 打包存储所有字段，便于序列化与高效比较
	int values;

	/// <summary>
	/// 通用读取函数：根据掩码与位移取出指定字段的值。
	/// </summary>
	readonly int Get(int mask, int shift) =>
		(int)((uint)values >> shift) & mask;

	/// <summary>
	/// 通用写入函数：将指定值写入对应的位段并返回新的结构。
	/// </summary>
	readonly HexValues With(int value, int mask, int shift) => new()
	{
		values = (values & ~(mask << shift)) | ((value & mask) << shift)
	};

	// -----------------------------------------------------------
	// 各字段位分布说明（共计 27 bits）
	// -----------------------------------------------------------
	// Elevation         (5 bits)  位移 0   - 高度（允许负值，通过偏移储存）
	// WaterLevel        (3 bits)  位移 5   - 水位
	// UrbanLevel        (2 bits)  位移 8   - 城市等级
	// FarmLevel         (2 bits)  位移 10  - 农田等级
	// PlantLevel        (2 bits)  位移 12  - 植被密度等级（0~3）
	// PlantIndex        (6 bits)  位移 14  - 植被类型索引（0~63）
	// SpecialIndex      (3 bits)  位移 20  - 特殊地形索引（0~7）
	// TerrainTypeIndex  (6 bits)  位移 23  - 地表类型索引（0~63）

	// --- 高度（Elevation，5 bits，-15 ~ +15） ---
	public readonly int Elevation => Get(31, 0) - 15;
	public readonly HexValues WithElevation(int value) =>
		With(value + 15, 31, 0);

	// --- 水位（Water Level，3 bits，0~7） ---
	public readonly int WaterLevel => Get(7, 5);
	public readonly HexValues WithWaterLevel(int value) =>
		With(value, 7, 5);

	// --- 城市等级（Urban Level，2 bits，0~3） ---
	public readonly int UrbanLevel => Get(3, 8);
	public readonly HexValues WithUrbanLevel(int value) =>
		With(value, 3, 8);

	// --- 农田等级（Farm Level，2 bits，0~3） ---
	public readonly int FarmLevel => Get(3, 10);
	public readonly HexValues WithFarmLevel(int value) =>
		With(value, 3, 10);

	// --- 植被密度（Plant Level，2 bits，0~3） ---
	public readonly int PlantLevel => Get(3, 12);
	public readonly HexValues WithPlantLevel(int value) =>
		With(value, 3, 12);

	// --- 植被类型索引（Plant Index，6 bits，0~63） ---
	public readonly int PlantIndex => Get(63, 14);
	public readonly HexValues WithPlantIndex(int value) =>
		With(value, 63, 14);

	// --- 特殊地形索引（Special Index，3 bits，0~7） ---
	public readonly int SpecialIndex => Get(7, 20);
	public readonly HexValues WithSpecialIndex(int index) =>
		With(index, 7, 20);

	// --- 地表类型索引（Terrain Type Index，6 bits，0~63） ---
	public readonly int TerrainTypeIndex => Get(63, 23);
	public readonly HexValues WithTerrainTypeIndex(int index) =>
		With(index, 63, 23);

	// -----------------------------------------------------------
	// 派生属性
	// -----------------------------------------------------------

	/// <summary>
	/// 视图高度：通常用于渲染时的视觉高度（取高度与水位的较大值）。
	/// </summary>
	public readonly int ViewElevation => Mathf.Max(Elevation, WaterLevel);

	/// <summary>
	/// 是否在水下（水位高于地形高度）。
	/// </summary>
	public readonly bool IsUnderwater => WaterLevel > Elevation;

	// -----------------------------------------------------------
	// 存档与读档
	// -----------------------------------------------------------

	/// <summary>
	/// 将当前值序列化写入二进制流。
	/// </summary>
	public readonly void Save(BinaryWriter writer)
	{
		writer.Write((byte)TerrainTypeIndex);
		writer.Write((byte)(Elevation + 127)); // 保证Elevation可写入byte
		writer.Write((byte)WaterLevel);
		writer.Write((byte)UrbanLevel);
		writer.Write((byte)FarmLevel);
		writer.Write((byte)PlantLevel);
		writer.Write((byte)PlantIndex);
		writer.Write((byte)SpecialIndex);
	}

	/// <summary>
	/// 从二进制流读取数据。
	/// header用于判断版本（例如旧版格式兼容）。
	/// </summary>
	public static HexValues Load(BinaryReader reader, int header)
	{
		HexValues values = default;

		// 地表类型
		values = values.WithTerrainTypeIndex(reader.ReadByte());

		// 高度（新版存储时Elevation + 127）
		int elevation = reader.ReadByte();
		if (header >= 4)
		{
			elevation -= 127;
		}
		values = values.WithElevation(elevation);

		// 依次读取其余字段
		values = values.WithWaterLevel(reader.ReadByte());
		values = values.WithUrbanLevel(reader.ReadByte());
		values = values.WithFarmLevel(reader.ReadByte());
		values = values.WithPlantLevel(reader.ReadByte());
		values = values.WithPlantIndex(reader.ReadByte());
		return values.WithSpecialIndex(reader.ReadByte());
	}
}
