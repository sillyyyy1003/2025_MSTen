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
	[Range(1, 10)]
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

	[Range(0, 40)]
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

	//湿度 ↑
	//m=0   m=1   m=2   m=3
	//┌────┬────┬────┬────┐
	//│砂  │雪   │雪  │雪  │ t=0  → 极寒区（雪线以上）
	//├────┼────┼────┼────┤
	//│砂  │泥  │泥   │泥  │ t=1  → 寒冷湿地/冻土带
	//├────┼────┼────┼────┤
	//│砂  │草  │草   │草  │ t=2  → 温带草原
	//├────┼────┼────┼────┤
	//│砂  │草  │草   │草  │ t=3  → 热带草原/雨林边缘
	//└────┴────┴────┴────┘



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

	/// <summary>
	/// 创建气候
	/// </summary>

	void CreateClimate()
	{
		climate.Clear();
		nextClimate.Clear();

		// 初始气候状态，每个格子的初始湿度值相同
		ClimateData initialData = new ClimateData();
		initialData.moisture = startingMoisture;

		// 清空气候状态模板
		ClimateData clearData = new ClimateData();

		// 为每个格子分配初始气候数据
		for (int i = 0; i < cellCount; i++)
		{
			climate.Add(initialData);   // 当前循环使用的数据
			nextClimate.Add(clearData); // 存放下一轮演化结果
		}

		// 进行40轮气候演化循环
		for (int cycle = 0; cycle < 40; cycle++)
		{
			for (int i = 0; i < cellCount; i++)
			{
				EvolveClimate(i); // 对每个格子执行一次气候演化
			}

			// 交换引用：将 nextClimate 作为新的气候状态
			List<ClimateData> swap = climate;
			climate = nextClimate;
			nextClimate = swap;
		}
	}

	/// <summary>
	/// 气候演化
	/// </summary>
	/// <param name="cellIndex"></param>
	void EvolveClimate(int cellIndex)
	{
		HexCell cell = grid.GetCell(cellIndex);
		ClimateData cellClimate = climate[cellIndex];

		// 1️⃣ 海洋或水下格子：恒定湿润
		if (cell.IsUnderwater)
		{
			cellClimate.moisture = 1f;                  // 满湿度
			cellClimate.clouds += evaporationFactor;    // 蒸发产生云
		}
		else
		{
			// 陆地蒸发：湿度变少 → 云增加
			float evaporation = cellClimate.moisture * evaporationFactor;
			cellClimate.moisture -= evaporation;
			cellClimate.clouds += evaporation;
		}

		// 2️ 云降雨：部分云量转化为降水，湿度上升
		float precipitation = cellClimate.clouds * precipitationFactor;
		cellClimate.clouds -= precipitation;
		cellClimate.moisture += precipitation;

		// 3️⃣ 云层高度限制：海拔越高，云量上限越小
		float cloudMaximum = 1f - cell.ViewElevation / (elevationMaximum + 1f);
		if (cellClimate.clouds > cloudMaximum)
		{
			// 超出云上限部分 → 转化为降水
			cellClimate.moisture += cellClimate.clouds - cloudMaximum;
			cellClimate.clouds = cloudMaximum;
		}

		// 4️⃣ 风向传播：云向风向相反方向扩散
		HexDirection mainDispersalDirection = windDirection.Opposite();
		float cloudDispersal = cellClimate.clouds * (1f / (5f + windStrength));

		// 5️⃣ 地表水流动（runoff & seepage）
		float runoff = cellClimate.moisture * runoffFactor * (1f / 6f);
		float seepage = cellClimate.moisture * seepageFactor * (1f / 6f);

		// 向六个方向扩散湿度与云
		for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
		{
			HexCell neighbor = cell.GetNeighbor(d);
			if (!neighbor)
			{
				continue;
			}
			ClimateData neighborClimate = nextClimate[neighbor.Index];

			// 🌬️ 主风向传播更强
			if (d == mainDispersalDirection)
			{
				neighborClimate.clouds += cloudDispersal * windStrength;
			}
			else
			{
				neighborClimate.clouds += cloudDispersal;
			}

			// 🏔️ 水分流动规则
			int elevationDelta = neighbor.Elevation - cell.Elevation;
			if (elevationDelta < 0) // 向低处流动（径流 runoff）
			{
				cellClimate.moisture -= runoff;
				neighborClimate.moisture += runoff;
			}
			else if (elevationDelta == 0) // 水平扩散（渗流 seepage）
			{
				cellClimate.moisture -= seepage;
				neighborClimate.moisture += seepage;
			}

			nextClimate[neighbor.Index] = neighborClimate;
		}

		// 6️⃣ 更新当前格子的下一轮数据
		ClimateData nextCellClimate = nextClimate[cellIndex];
		nextCellClimate.moisture += cellClimate.moisture;

		if (nextCellClimate.moisture > 1f)
		{
			nextCellClimate.moisture = 1f;
		}

		nextClimate[cellIndex] = nextCellClimate;

		// 当前循环数据清空，为下次迭代准备
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
		HexCoordinates center = firstCell.Coordinates;

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
				//HexCell neighbor = current.GetNeighbor(d);
				if (current.TryGetNeighbor(d, out HexCell neighbor) &&
				    neighbor.SearchPhase < searchFrontierPhase)
				{
					neighbor.SearchPhase = searchFrontierPhase;
					neighbor.Distance = neighbor.Distance = neighbor.Coordinates.DistanceTo(center);
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
		HexCoordinates center = firstCell.Coordinates;

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
					neighbor.Distance = neighbor.Distance = neighbor.Coordinates.DistanceTo(center);
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
		// 随机选择一个通道用来为温度扰动（Noise）选择偏移——与 DetermineTemperature 相关
		temperatureJitterChannel = Random.Range(0, 4);

		// 计算“岩石沙漠（rock desert）”出现的高度阈值：
		// 当高度高于 rockDesertElevation 时，原本是“无纹理(0)”的气候格子会被改为石头（3）
		// 公式：在最大高度和水位之间取中点偏向最高（等同于：elevationMaximum - (range/2)）
		int rockDesertElevation =
			elevationMaximum - (elevationMaximum - waterLevel) / 2;

		// 遍历每个单元格并为其设置地形类型索引
		for (int i = 0; i < cellCount; i++)
		{
			HexCell cell = grid.GetCell(i);

			// 根据当前格子计算温度（可能使用噪声与高度等因素）
			float temperature = DetermineTemperature(cell);

			// 从 climate 数组中读取当前格子的湿度（假设 climate 长度 == cellCount）
			float moisture = climate[i].moisture;

			// 如果不是水下格子，按生物群系（温度/湿度分段）进行地形/植被判定
			if (!cell.IsUnderwater)
			{
				// 找到 temperature 所在的温度段索引 t
				int t = 0;
				for (; t < temperatureBands.Length; t++)
				{
					if (temperature < temperatureBands[t])
					{
						break;
					}
				}

				// 找到 moisture 所在的湿度段索引 m
				int m = 0;
				for (; m < moistureBands.Length; m++)
				{
					if (moisture < moistureBands[m])
					{
						break;
					}
				}

				// biomes 以 (temperature index * 4 + moisture index) 存储（4 == 湿度段数或固定列数）
				// 把对应的生物群系取出来（包含 terrain 与 plant 信息）
				Biome cellBiome = biomes[t * 4 + m];

				// 当 biome.terrain 为 0（代表“无纹理/沙？”或“默认”）时，
				// 如果当前格子高度高于 rockDesertElevation，视为岩石（Stone -> index 3）
				if (cellBiome.terrain == 0)
				{
					if (cell.Elevation >= rockDesertElevation)
					{
						cellBiome.terrain = 3; // Stone（或岩石/荒漠）
					}
				}
				// 如果格子恰好在最高海拔（elevationMaximum），则强制设为雪（4）
				else if (cell.Elevation == elevationMaximum)
				{
					cellBiome.terrain = 4; // Snow（积雪）
				}

				// 根据最终的 terrain 决定植物等级变化：
				// 如果是雪地（4），则没有植被（plant = 0）。
				// 否则：如果当前 biome 的 plant 等级 < 3 且该格子有河流，则植物等级 +1（河边植物更丰富）
				if (cellBiome.terrain == 4)
				{
					cellBiome.plant = 0;
				}
				else if (cellBiome.plant < 3 && cell.HasRiver)
				{
					cellBiome.plant += 1;
				}

				// 将决定好的地形索引写回格子
				cell.TerrainTypeIndex = cellBiome.terrain;

				// 如果你想把 plant 等级也存回格子（目前被注释掉），可以解除下一行注释
				// 设定植物等级：数值越低，植被越稀疏
				// cell.PlantLevel = cellBiome.plant;
			}
			// 水下（或海岸）格子的处理逻辑
			else
			{
				int terrain;

				// 如果该水下格子的高度恰好为 waterLevel - 1，表示它是海岸带（浅滩/近岸）
				if (cell.Elevation == waterLevel - 1)
				{
					// 计算周围六个邻居中有多少是“悬崖(cliffs)”或“斜坡(slopes)”。
					// 依据邻居高度相对于当前格子水位（cell.WaterLevel）来判定。
					int cliffs = 0, slopes = 0;
					for (
						HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++
					)
					{
						HexCell neighbor = cell.GetNeighbor(d);
						if (!neighbor)
						{
							// 如果没有邻居（地图边缘），跳过
							continue;
						}
						// delta = 邻居高度 - 当前格子的水位
						int delta = neighbor.Elevation - cell.WaterLevel;
						if (delta == 0)
						{
							// 如果邻居与水位相等，视为斜坡（连着浅水区）
							slopes += 1;
						}
						else if (delta > 0)
						{
							// 邻居比水位高：可能是悬崖/岸线
							cliffs += 1;
						}
					}

					// 根据周围 cliff/slopes 数决定海岸类型：
					// 如果 cliffs + slopes > 3（邻居中多数是斜坡或悬崖），设为 terrain = 1（比如“沙/岸/浅滩”）
					// 否则若存在 cliffs 把 terrain 设为 3（比如“石岸/悬崖”）
					// 否则若存在 slopes 把 terrain 设为 0（平缓的泥滩/浅滩）
					// 最后默认回到 terrain = 1
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
				// 如果海拔 >= waterLevel（理论上不应该发生在 IsUnderwater 为 true 的分支里，但为了安全处理）
				else if (cell.Elevation >= waterLevel)
				{
					terrain = 1;
				}
				// 如果海拔 < 0（非常低的海底，深海）的处理
				else if (cell.Elevation < 0)
				{
					terrain = 3;
				}
				// 其它水下情况（中等深度）
				else
				{
					terrain = 2;
				}

				// 特殊规则：如果判定为 terrain == 1（某种近岸/沙）但温度非常低（低于最冷温度段）
				// 则把 terrain 改为 2（例如：冰/寒冷泥地/冻土）
				if (terrain == 1 && temperature < temperatureBands[0])
				{
					terrain = 2;
				}

				// 写回格子的地形类型
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


	/// <summary>
	/// 根据地块纬度、海拔与噪声扰动计算格子的温度值（0~1）
	/// </summary>
	float DetermineTemperature(HexCell cell)
	{
		// 1️ 纬度系数（Z 方向）：用于模拟南北温差
		//    cell.Coordinates.Z 越大代表越靠北或越靠南
		//    latitude 范围通常是 0 ~ 1，对应地图从底部到顶部
		float latitude = (float)cell.Coordinates.Z / grid.CellCountZ;

		// 2️ 基础温度：根据纬度在 [lowTemperature, highTemperature] 区间插值
		//    通常 lowTemperature = 0（极地），highTemperature = 1（赤道）
		//    Mathf.LerpUnclamped 允许 latitude 超出 [0,1] 也能计算（用于噪声偏移或极端气候）
		float temperature =
			Mathf.LerpUnclamped(lowTemperature, highTemperature, latitude);

		// 3️ 海拔修正：海拔越高温度越低
		//    (cell.ViewElevation - waterLevel) 越大 → 温度衰减越多
		//    elevationMaximum - waterLevel + 1f 是归一化的高度范围
		temperature *= 1f - (cell.ViewElevation - waterLevel) /
			(elevationMaximum - waterLevel + 1f);

		// 4️ 噪声扰动（Noise Jitter）：
		//    从噪声贴图采样随机值，让温度分布更自然、非线性
		//    temperatureJitterChannel 表示采样噪声的哪个通道（0~3）
		//    *0.1f 缩放 world position，控制噪声图案大小
		float jitter =
			HexMetrics.SampleNoise(cell.Position * 0.1f)[temperatureJitterChannel];

		// 5️ 将噪声扰动应用到温度上：
		//    jitter 原始值为 0~1 → (jitter*2f - 1f) 映射为 -1~+1
		//    temperatureJitter 控制扰动强度（例如 0.2 表示 ±20% 温度变化）
		temperature += (jitter * 2f - 1f) * temperatureJitter;

		// 6️ 返回最终温度值（一般在 0~1 之间，但不强制 Clamp）
		//    不进行 Mathf.Clamp01，可以保留极端值差异以生成更多地貌变化
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
				region.xMax = grid.CellCountX - mapBorderX;
				region.zMin = mapBorderZ;
				region.zMax = grid.CellCountZ - mapBorderZ;
				regions.Add(region);
				break;
			case 2:
				if (Random.value < 0.5f)
				{
					region.xMin = mapBorderX;
					region.xMax = grid.CellCountX / 2 - regionBorder;
					region.zMin = mapBorderZ;
					region.zMax = grid.CellCountZ - mapBorderZ;
					regions.Add(region);
					region.xMin = grid.CellCountX / 2 + regionBorder;
					region.xMax = grid.CellCountX - mapBorderX;
					regions.Add(region);
				}
				else
				{
					region.xMin = mapBorderX;
					region.xMax = grid.CellCountX - mapBorderX;
					region.zMin = mapBorderZ;
					region.zMax = grid.CellCountZ / 2 - regionBorder;
					regions.Add(region);
					region.zMin = grid.CellCountZ / 2 + regionBorder;
					region.zMax = grid.CellCountZ - mapBorderZ;
					regions.Add(region);
				}
				break;
			case 3:
				region.xMin = mapBorderX;
				region.xMax = grid.CellCountX / 3 - regionBorder;
				region.zMin = mapBorderZ;
				region.zMax = grid.CellCountZ - mapBorderZ;
				regions.Add(region);
				region.xMin = grid.CellCountX / 3 + regionBorder;
				region.xMax = grid.CellCountX * 2 / 3 - regionBorder;
				regions.Add(region);
				region.xMin = grid.CellCountX * 2 / 3 + regionBorder;
				region.xMax = grid.CellCountX - mapBorderX;
				regions.Add(region);
				break;
			case 4:
				region.xMin = mapBorderX;
				region.xMax = grid.CellCountX / 2 - regionBorder;
				region.zMin = mapBorderZ;
				region.zMax = grid.CellCountZ / 2 - regionBorder;
				regions.Add(region);
				region.xMin = grid.CellCountX / 2 + regionBorder;
				region.xMax = grid.CellCountX - mapBorderX;
				regions.Add(region);
				region.zMin = grid.CellCountZ / 2 + regionBorder;
				region.zMax = grid.CellCountZ - mapBorderZ;
				regions.Add(region);
				region.xMin = mapBorderX;
				region.xMax = grid.CellCountX / 2 - regionBorder;
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