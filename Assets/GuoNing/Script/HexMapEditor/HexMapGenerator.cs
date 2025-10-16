using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class HexMapGenerator : MonoBehaviour
{

	public HexGrid grid;                        // 引用地图网格（由多个HexCell组成）

	int cellCount, landCells;                   // 单元格总数、陆地单元格数
	int temperatureJitterChannel;               // 噪声通道索引，用于温度扰动
	HexCellPriorityQueue searchFrontier;        // 地形扩展时用的优先队列
	int searchFrontierPhase;                    // 搜索阶段标识（避免重复访问）

	// 温度与湿度分界线（0~1）
	static float[] temperatureBands = { 0.1f, 0.3f, 0.6f };
	static float[] moistureBands = { 0.12f, 0.28f, 0.85f };

	[Range(0f, 0.5f)]
	public float jitterProbability = 0.25f;     // 地形抖动概率（影响地形扩散的随机性）


	//===================【地形生成参数】===================//

	[Header("ChunkSize")]
	[Range(20, 200)]
	public int chunkSizeMin = 30;               // 每次地形生成的最小块大小
	[Range(20, 200)]
	public int chunkSizeMax = 100;              // 每次地形生成的最大块大小

	[Header("LandPercentage")]
	[Range(5, 95)]
	public int landPercentage = 50;             // 地图中陆地的百分比

	[Range(0f, 1f)]
	public float highRiseProbability = 0.25f;   // 大幅度抬升地形的概率
	[Range(0f, 0.4f)]
	public float sinkProbability = 0.2f;        // 下沉地形的概率

	[Header("WaterLevel")]
	[Range(1, 5)]
	public int waterLevel = 3;                  // 水面高度（低于该高度为水）

	[Header("LandElevation")]
	[Range(-4, 0)]
	public int elevationMinimum = -2;           // 地形最低海拔
	[Range(6, 10)]
	public int elevationMaximum = 8;            // 地形最高海拔

	[Header("LandBoard")]
	[Range(0, 20)]
	public int mapBorderX = 5;                  // 地图X方向边界留白
	[Range(0, 20)]
	public int mapBorderZ = 5;                  // 地图Z方向边界留白

	[Header("RegionParam")]
	[Range(1, 4)]
	public int regionCount = 1;                 // 区域数量（用于分区生成）
	[Range(0, 10)]
	public int regionBorder = 5;                // 区域之间的间隔边界

	[Header("Erosion")]
	[Range(0, 100)]
	public int erosionPercentage = 50;          // 地形侵蚀强度（百分比越高 → 山体被削平）

	[Header("MapSeed")]
	public int seed;                            // 随机种子
	public bool useFixedSeed;                   // 是否使用固定种子（否则随机生成）

	[Range(0, 20)]
	public int riverPercentage = 10;            // 河流覆盖率（相对陆地百分比）

	//===================【气候参数】===================//

	[Header("Climate")]
	[Range(0f, 1f)]
	public float evaporationFactor = 0.5f;      // 蒸发系数
	[Range(0f, 1f)]
	public float precipitationFactor = 0.25f;   // 降水系数
	[Range(0f, 1f)]
	public float runoffFactor = 0.25f;          // 地表径流系数
	[Range(0f, 1f)]
	public float seepageFactor = 0.125f;        // 地下渗流系数
	public HexDirection windDirection = HexDirection.NW;    // 主风向
	[Range(1f, 10f)]
	public float windStrength = 4f;             // 风力强度
	[Range(0f, 1f)]
	public float startingMoisture = 0.1f;       // 初始湿度（全局基准）
	[Range(0f, 1f)]
	public float extraLakeProbability = 0.25f;  // 附加生成湖泊的概率
	[Range(0f, 1f)]
	public float lowTemperature = 0f;           // 最低温
	[Range(0f, 1f)]
	public float highTemperature = 1f;          // 最高温

	public enum HemisphereMode { Both, North, South }
	public HemisphereMode hemisphere;           // 半球模式（影响温度分布）

	[Range(0f, 1f)]
	public float temperatureJitter = 0.1f;      // 温度随机扰动强度


	//===================【内部结构体】===================//

	// 地图区域定义
	struct MapRegion
	{
		public int xMin, xMax, zMin, zMax;
	}
	List<MapRegion> regions;                    // 区域列表

	// 气候数据（云量和湿度）
	struct ClimateData
	{
		public float clouds, moisture;
	}
	List<ClimateData> climate = new List<ClimateData>();        // 当前气候状态
	List<ClimateData> nextClimate = new List<ClimateData>();    // 下一轮气候状态
	List<HexDirection> flowDirections = new List<HexDirection>();   // 河流流向缓存

	// 生物群落（地形类型 + 植被等级）
	struct Biome
	{
		public int terrain, plant;
		public Biome(int terrain, int plant)
		{
			this.terrain = terrain;
			this.plant = plant;
		}
	}

	// 各种温度+湿度组合对应的地貌（简化版生物群落表）
	static Biome[] biomes = {
		new Biome(0, 0), new Biome(4, 0), new Biome(4, 0), new Biome(4, 0),
		new Biome(0, 0), new Biome(2, 0), new Biome(2, 1), new Biome(2, 2),
		new Biome(0, 0), new Biome(1, 0), new Biome(1, 1), new Biome(1, 2),
		new Biome(0, 0), new Biome(1, 1), new Biome(1, 2), new Biome(1, 3)
	};


	//===================【主生成流程】===================//

	public void GenerateMap(int x, int z)
	{
		Random.State originalRandomState = Random.state;
		if (!useFixedSeed)
		{
			seed = Random.Range(0, int.MaxValue);
			seed ^= (int)System.DateTime.Now.Ticks;
			seed ^= (int)Time.time;
			seed &= int.MaxValue;
		}
		Random.InitState(seed);

		cellCount = x * z;
		grid.CreateMap(x, z);

		if (searchFrontier == null)
		{
			searchFrontier = new HexCellPriorityQueue();
		}

		for (int i = 0; i < cellCount; i++)
		{
			grid.GetCell(i).WaterLevel = waterLevel;
		}

		CreateRegions();	// 创建区域
		CreateLand();		// 创建陆地
		ErodeLand();		// 创建侵蚀
		CreateClimate();    // 创建气候
		CreateRivers();		// 创建河流

		SetTerrainType();
		for (int i = 0; i < cellCount; i++)
		{
			grid.GetCell(i).SearchPhase = 0;
		}
		Random.state = originalRandomState;
	}

	void CreateLand()
	{
		int landBudget = Mathf.RoundToInt(cellCount * landPercentage * 0.01f);
		landCells = landBudget;

		for (int guard = 0; guard < 10000; guard++)
		{
			bool sink = Random.value < sinkProbability;
			for (int i = 0; i < regions.Count; i++)
			{
				MapRegion region = regions[i];
				int chunkSize = Random.Range(chunkSizeMin, chunkSizeMax - 1);
				//				if (Random.value < sinkProbability) {
				if (sink)
				{
					landBudget = SinkTerrain(chunkSize, landBudget, region);
				}
				else
				{
					landBudget = RaiseTerrain(chunkSize, landBudget, region);
					if (landBudget == 0)
					{
						return;
					}
				}
			}
		}
		if (landBudget > 0)
		{
			Debug.LogWarning("Failed to use up " + landBudget + " land budget.");
			landCells -= landBudget;
		}
	}


	void CreateClimate()
	{
		climate.Clear();
		nextClimate.Clear();

		ClimateData initialData = new ClimateData();
		initialData.moisture = startingMoisture;
		ClimateData clearData = new ClimateData();
		for (int i = 0; i < cellCount; i++)
		{
			climate.Add(initialData);
			nextClimate.Add(clearData);
		}

		for (int cycle = 0; cycle < 40; cycle++)
		{
			for (int i = 0; i < cellCount; i++)
			{
				EvolveClimate(i);
			}
			List<ClimateData> swap = climate;
			climate = nextClimate;
			nextClimate = swap;
		}
	}

	void EvolveClimate(int cellIndex)
	{
		HexCell cell = grid.GetCell(cellIndex);
		ClimateData cellClimate = climate[cellIndex];

		if (cell.IsUnderwater)
		{
			cellClimate.moisture = 1f;
			cellClimate.clouds += evaporationFactor;
		}
		else
		{
			float evaporation = cellClimate.moisture * evaporationFactor;
			cellClimate.moisture -= evaporation;
			cellClimate.clouds += evaporation;
		}


		float precipitation = cellClimate.clouds * precipitationFactor;
		cellClimate.clouds -= precipitation;
		cellClimate.moisture += precipitation;

		float cloudMaximum = 1f - cell.ViewElevation / (elevationMaximum + 1f);
		if (cellClimate.clouds > cloudMaximum)
		{
			cellClimate.moisture += cellClimate.clouds - cloudMaximum;
			cellClimate.clouds = cloudMaximum;
		}

		HexDirection mainDispersalDirection = windDirection.Opposite();
		float cloudDispersal = cellClimate.clouds * (1f / (5f + windStrength));
		float runoff = cellClimate.moisture * runoffFactor * (1f / 6f);
		float seepage = cellClimate.moisture * seepageFactor * (1f / 6f);

		for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
		{
			HexCell neighbor = cell.GetNeighbor(d);
			if (!neighbor)
			{
				continue;
			}
			ClimateData neighborClimate = nextClimate[neighbor.Index];
			if (d == mainDispersalDirection)
			{
				neighborClimate.clouds += cloudDispersal * windStrength;
			}
			else
			{
				neighborClimate.clouds += cloudDispersal;
			}

			int elevationDelta = neighbor.Elevation - cell.Elevation;
			if (elevationDelta < 0)
			{
				cellClimate.moisture -= runoff;
				neighborClimate.moisture += runoff;
			}
			else if (elevationDelta == 0)
			{
				cellClimate.moisture -= seepage;
				neighborClimate.moisture += seepage;
			}

			nextClimate[neighbor.Index] = neighborClimate;
		}

		ClimateData nextCellClimate = nextClimate[cellIndex];
		nextCellClimate.moisture += cellClimate.moisture;
		if (nextCellClimate.moisture > 1f)
		{
			nextCellClimate.moisture = 1f;
		}
		nextClimate[cellIndex] = nextCellClimate;
		climate[cellIndex] = new ClimateData();
	}


	int RaiseTerrain(int chunkSize, int budget, MapRegion region)
	{
		searchFrontierPhase += 1;
		HexCell firstCell = GetRandomCell(region);
		firstCell.SearchPhase = searchFrontierPhase;
		firstCell.Distance = 0;
		firstCell.SearchHeuristic = 0;
		searchFrontier.Enqueue(firstCell);
		HexCoordinates center = firstCell.coordinates;

		int rise = Random.value < highRiseProbability ? 2 : 1;
		int size = 0;
		while (size < chunkSize && searchFrontier.Count > 0)
		{
			HexCell current = searchFrontier.Dequeue();
			int originalElevation = current.Elevation;
			int newElevation = originalElevation + rise;
			if (newElevation > elevationMaximum)
			{
				continue;
			}
			current.Elevation = newElevation;
			if (
				originalElevation < waterLevel &&
				newElevation >= waterLevel && --budget == 0
			)
			{
				break;
			}
			size += 1;

			for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
			{
				HexCell neighbor = current.GetNeighbor(d);
				if (neighbor && neighbor.SearchPhase < searchFrontierPhase)
				{
					neighbor.SearchPhase = searchFrontierPhase;
					neighbor.Distance = neighbor.Distance = neighbor.coordinates.DistanceTo(center);
					neighbor.SearchHeuristic =
						Random.value < jitterProbability ? 1 : 0;
					searchFrontier.Enqueue(neighbor);
				}
			}
		}
		searchFrontier.Clear();
		return budget;
	}


	int SinkTerrain(int chunkSize, int budget, MapRegion region)
	{
		searchFrontierPhase += 1;
		HexCell firstCell = GetRandomCell(region);
		firstCell.SearchPhase = searchFrontierPhase;
		firstCell.Distance = 0;
		firstCell.SearchHeuristic = 0;
		searchFrontier.Enqueue(firstCell);
		HexCoordinates center = firstCell.coordinates;

		int sink = Random.value < highRiseProbability ? 2 : 1;
		int size = 0;
		while (size < chunkSize && searchFrontier.Count > 0)
		{
			HexCell current = searchFrontier.Dequeue();
			int originalElevation = current.Elevation;
			int newElevation = current.Elevation - sink;
			if (newElevation < elevationMinimum)
			{
				continue;
			}
			current.Elevation = newElevation;
			if (
				originalElevation >= waterLevel &&
				newElevation < waterLevel
			)
			{
				budget += 1;
			}
			size += 1;

			for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
			{
				HexCell neighbor = current.GetNeighbor(d);
				if (neighbor && neighbor.SearchPhase < searchFrontierPhase)
				{
					neighbor.SearchPhase = searchFrontierPhase;
					neighbor.Distance = neighbor.Distance = neighbor.coordinates.DistanceTo(center);
					neighbor.SearchHeuristic =
						Random.value < jitterProbability ? 1 : 0;
					searchFrontier.Enqueue(neighbor);
				}
			}
		}
		searchFrontier.Clear();
		return budget;
	}

	void SetTerrainType()
	{
		temperatureJitterChannel = Random.Range(0, 4);
		int rockDesertElevation =
			elevationMaximum - (elevationMaximum - waterLevel) / 2;

		for (int i = 0; i < cellCount; i++)
		{
			HexCell cell = grid.GetCell(i);
			float temperature = DetermineTemperature(cell);
			float moisture = climate[i].moisture;
			if (!cell.IsUnderwater)
			{
				int t = 0;
				for (; t < temperatureBands.Length; t++)
				{
					if (temperature < temperatureBands[t])
					{
						break;
					}
				}
				int m = 0;
				for (; m < moistureBands.Length; m++)
				{
					if (moisture < moistureBands[m])
					{
						break;
					}
				}
				Biome cellBiome = biomes[t * 4 + m];

				if (cellBiome.terrain == 0)
				{
					if (cell.Elevation >= rockDesertElevation)
					{
						cellBiome.terrain = 3;
					}
				}
				else if (cell.Elevation == elevationMaximum)
				{
					cellBiome.terrain = 4;
				}


				if (cellBiome.terrain == 4)
				{
					cellBiome.plant = 0;
				}
				else if (cellBiome.plant < 3 && cell.HasRiver)
				{
					cellBiome.plant += 1;
				}

				cell.TerrainTypeIndex = cellBiome.terrain;
				cell.ForestLevel = cellBiome.plant;
			}
			else
			{
				int terrain;
				if (cell.Elevation == waterLevel - 1)
				{
					int cliffs = 0, slopes = 0;
					for (
						HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++
					)
					{
						HexCell neighbor = cell.GetNeighbor(d);
						if (!neighbor)
						{
							continue;
						}
						int delta = neighbor.Elevation - cell.WaterLevel;
						if (delta == 0)
						{
							slopes += 1;
						}
						else if (delta > 0)
						{
							cliffs += 1;
						}
					}
					if (cliffs + slopes > 3)
					{
						terrain = 1;
					}
					else if (cliffs > 0)
					{
						terrain = 3;
					}
					else if (slopes > 0)
					{
						terrain = 0;
					}
					else
					{
						terrain = 1;
					}
				}
				else if (cell.Elevation >= waterLevel)
				{
					terrain = 1;
				}
				else if (cell.Elevation < 0)
				{
					terrain = 3;
				}
				else
				{
					terrain = 2;
				}

				if (terrain == 1 && temperature < temperatureBands[0])
				{
					terrain = 2;
				}
				cell.TerrainTypeIndex = terrain;
			}
			
		}
	}

	void CreateRivers()
	{
		List<HexCell> riverOrigins = ListPool<HexCell>.Get();
		for (int i = 0; i < cellCount; i++)
		{
			HexCell cell = grid.GetCell(i);
			if (cell.IsUnderwater)
			{
				continue;
			}
			ClimateData data = climate[i];
			float weight =
				data.moisture * (cell.Elevation - waterLevel) /
				(elevationMaximum - waterLevel);
			if (weight > 0.75f)
			{
				riverOrigins.Add(cell);
				riverOrigins.Add(cell);
			}
			if (weight > 0.5f)
			{
				riverOrigins.Add(cell);
			}
			if (weight > 0.25f)
			{
				riverOrigins.Add(cell);
			}
		}
		int riverBudget = Mathf.RoundToInt(landCells * riverPercentage * 0.01f);
		while (riverBudget > 0 && riverOrigins.Count > 0)
		{
			int index = Random.Range(0, riverOrigins.Count);
			int lastIndex = riverOrigins.Count - 1;
			HexCell origin = riverOrigins[index];
			riverOrigins[index] = riverOrigins[lastIndex];
			riverOrigins.RemoveAt(lastIndex);
			if (!origin.HasRiver)
			{
				bool isValidOrigin = true;
				for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
				{
					HexCell neighbor = origin.GetNeighbor(d);
					if (neighbor && (neighbor.HasRiver || neighbor.IsUnderwater))
					{
						isValidOrigin = false;
						break;
					}
				}
				if (isValidOrigin)
				{
					riverBudget -= CreateRiver(origin);
				}
			}
		}

		if (riverBudget > 0)
		{
			Debug.LogWarning("Failed to use up river budget.");
		}

		ListPool<HexCell>.Add(riverOrigins);
	}

	int CreateRiver(HexCell origin)
	{
		int length = 1;
		HexCell cell = origin;
		HexDirection direction = HexDirection.NE;
		while (!cell.IsUnderwater)
		{
			int minNeighborElevation = int.MaxValue;
			flowDirections.Clear();
			for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
			{
				HexCell neighbor = cell.GetNeighbor(d);
				if (!neighbor)
				{
					continue;
				}

				if (neighbor.Elevation < minNeighborElevation)
				{
					minNeighborElevation = neighbor.Elevation;
				}

				if (neighbor == origin || neighbor.HasIncomingRiver)
				{
					continue;
				}
				int delta = neighbor.Elevation - cell.Elevation;
				if (delta > 0)
				{
					continue;
				}

				if (neighbor.HasOutgoingRiver)
				{
					cell.SetOutgoingRiver(d);
					return length;
				}

				if (delta < 0)
				{
					flowDirections.Add(d);
					flowDirections.Add(d);
					flowDirections.Add(d);
				}
				if (
					length == 1 ||
					(d != direction.Next2() && d != direction.Previous2())
				)
				{
					flowDirections.Add(d);
				}

				flowDirections.Add(d);
			}

			if (flowDirections.Count == 0)
			{
				if (length == 1)
				{
					return 0;
				}

				if (minNeighborElevation >= cell.Elevation &&
				    Random.value < extraLakeProbability)
				{
					cell.WaterLevel = minNeighborElevation;
					if (minNeighborElevation == cell.Elevation)
					{
						cell.Elevation = minNeighborElevation - 1;
					}
				}
				break;
			}

			direction = flowDirections[Random.Range(0, flowDirections.Count)];
			cell.SetOutgoingRiver(direction);
			length += 1;

			if (minNeighborElevation >= cell.Elevation)
			{
				cell.WaterLevel = cell.Elevation;
				cell.Elevation -= 1;
			}

			cell = cell.GetNeighbor(direction);
		}
		return length;
	}


	float DetermineTemperature(HexCell cell)
	{
		float latitude = (float)cell.coordinates.Z / grid.cellCountZ;
		float temperature =
			Mathf.LerpUnclamped(lowTemperature, highTemperature, latitude);

		temperature *= 1f - (cell.ViewElevation - waterLevel) /
			(elevationMaximum - waterLevel + 1f);

		float jitter =
			HexMetrics.SampleNoise(cell.Position * 0.1f)[temperatureJitterChannel];

		temperature += (jitter * 2f - 1f) * temperatureJitter;

		return temperature;
	}

	HexCell GetRandomCell(MapRegion region)
	{
		return grid.GetCell(
			Random.Range(region.xMin, region.xMax),
			Random.Range(region.zMin, region.zMax)
		);
	}

	void CreateRegions()
	{
		if (regions == null)
		{
			regions = new List<MapRegion>();
		}
		else
		{
			regions.Clear();
		}

		MapRegion region;
		switch (regionCount)
		{
			default:
				region.xMin = mapBorderX;
				region.xMax = grid.cellCountX - mapBorderX;
				region.zMin = mapBorderZ;
				region.zMax = grid.cellCountZ - mapBorderZ;
				regions.Add(region);
				break;
			case 2:
				if (Random.value < 0.5f)
				{
					region.xMin = mapBorderX;
					region.xMax = grid.cellCountX / 2 - regionBorder;
					region.zMin = mapBorderZ;
					region.zMax = grid.cellCountZ - mapBorderZ;
					regions.Add(region);
					region.xMin = grid.cellCountX / 2 + regionBorder;
					region.xMax = grid.cellCountX - mapBorderX;
					regions.Add(region);
				}
				else
				{
					region.xMin = mapBorderX;
					region.xMax = grid.cellCountX - mapBorderX;
					region.zMin = mapBorderZ;
					region.zMax = grid.cellCountZ / 2 - regionBorder;
					regions.Add(region);
					region.zMin = grid.cellCountZ / 2 + regionBorder;
					region.zMax = grid.cellCountZ - mapBorderZ;
					regions.Add(region);
				}
				break;
			case 3:
				region.xMin = mapBorderX;
				region.xMax = grid.cellCountX / 3 - regionBorder;
				region.zMin = mapBorderZ;
				region.zMax = grid.cellCountZ - mapBorderZ;
				regions.Add(region);
				region.xMin = grid.cellCountX / 3 + regionBorder;
				region.xMax = grid.cellCountX * 2 / 3 - regionBorder;
				regions.Add(region);
				region.xMin = grid.cellCountX * 2 / 3 + regionBorder;
				region.xMax = grid.cellCountX - mapBorderX;
				regions.Add(region);
				break;
			case 4:
				region.xMin = mapBorderX;
				region.xMax = grid.cellCountX / 2 - regionBorder;
				region.zMin = mapBorderZ;
				region.zMax = grid.cellCountZ / 2 - regionBorder;
				regions.Add(region);
				region.xMin = grid.cellCountX / 2 + regionBorder;
				region.xMax = grid.cellCountX - mapBorderX;
				regions.Add(region);
				region.zMin = grid.cellCountZ / 2 + regionBorder;
				region.zMax = grid.cellCountZ - mapBorderZ;
				regions.Add(region);
				region.xMin = mapBorderX;
				region.xMax = grid.cellCountX / 2 - regionBorder;
				regions.Add(region);
				break;
		}
	}


	void ErodeLand()
	{
		List<HexCell> erodibleCells = ListPool<HexCell>.Get();
		for (int i = 0; i < cellCount; i++)
		{
			HexCell cell = grid.GetCell(i);
			if (IsErodible(cell))
			{
				erodibleCells.Add(cell);
			}
		}


		int targetErodibleCount =
			(int)(erodibleCells.Count * (100 - erosionPercentage) * 0.01f);
		while (erodibleCells.Count > targetErodibleCount)
		{
			int index = Random.Range(0, erodibleCells.Count);
			HexCell cell = erodibleCells[index];
			HexCell targetCell = GetErosionTarget(cell);

			cell.Elevation -= 1;
			targetCell.Elevation += 1;

			if (!IsErodible(cell))
			{
				erodibleCells[index] = erodibleCells[erodibleCells.Count - 1];
				erodibleCells.RemoveAt(erodibleCells.Count - 1);
			}

			for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
			{
				HexCell neighbor = cell.GetNeighbor(d);
				if (
					neighbor && neighbor.Elevation == cell.Elevation + 2 &&
					!erodibleCells.Contains(neighbor)
				)
				{
					erodibleCells.Add(neighbor);
				}
			}


			if (IsErodible(targetCell) && !erodibleCells.Contains(targetCell))
			{
				erodibleCells.Add(targetCell);
			}


			for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
			{
				HexCell neighbor = targetCell.GetNeighbor(d);
				if (
					neighbor && neighbor != cell &&
					neighbor.Elevation == targetCell.Elevation + 1 &&
					!IsErodible(neighbor)
				)
				{
					erodibleCells.Remove(neighbor);
				}
			}

		}

		ListPool<HexCell>.Add(erodibleCells);
	}

	bool IsErodible(HexCell cell)
	{
		int erodibleElevation = cell.Elevation - 2;
		for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
		{
			HexCell neighbor = cell.GetNeighbor(d);
			if (neighbor && neighbor.Elevation <= erodibleElevation)
			{
				return true;
			}
		}
		return false;
	}

	HexCell GetErosionTarget(HexCell cell)
	{
		List<HexCell> candidates = ListPool<HexCell>.Get();
		int erodibleElevation = cell.Elevation - 2;
		for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
		{
			HexCell neighbor = cell.GetNeighbor(d);
			if (neighbor && neighbor.Elevation <= erodibleElevation)
			{
				candidates.Add(neighbor);
			}
		}



		HexCell target = candidates[Random.Range(0, candidates.Count)];
		ListPool<HexCell>.Add(candidates);
		return target;
	}
}