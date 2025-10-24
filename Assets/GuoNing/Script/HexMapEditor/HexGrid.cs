using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor.MemoryProfiler;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;




public class HexGrid : MonoBehaviour
{


	/// <summary>
	/// Amount of cells in the X dimension.
	/// </summary>
	public int CellCountX
	{ get; private set; }

	/// <summary>
	/// Amount of cells in the Z dimension.
	/// </summary>
	public int CellCountZ
	{ get; private set; }


	int chunkCountX, chunkCountZ;

	public HexCell cellPrefab;		//单个格子的预制件
	public Text cellLabelPrefab;    // coordinate text
	public Texture2D noiseSource;
	public HexGridChunk chunkPrefab;
	public int seed;

	private HexCell[] cells;
	private HexGridChunk[] chunks;

	int currentPathFromIndex = -1, currentPathToIndex = -1;// 当前搜索起点和终点索引
	bool currentPathExists; // 是否存在路径
	int searchFrontierPhase;
	HexCellPriorityQueue searchFrontier;    // 搜索优先队列

	[Header("MiniMapCamera")]
	public HexMapLoader mapLoader;
	public MinimapCameraController minimapCamController;

	private List<int> startIndex = new List<int>();	// 默认玩家是两个人

	void Start()
	{
		HexMetrics.noiseSource = noiseSource;
		HexMetrics.InitializeHashGrid(seed);
		StartCoroutine(LoadMapOnce());

	}

	/// <summary>
	/// 延迟一帧生成地图，确保 Start 执行完
	/// </summary>
	/// <returns></returns>
	IEnumerator LoadMapOnce()
	{
		yield return null; // 等一帧，确保 Start 执行完
		if (mapLoader) mapLoader.LoadMap();
	}


	public bool CreateMap(int x, int z)
	{
		if (
			x <= 0 || x % HexMetrics.chunkSizeX != 0 ||
			z <= 0 || z % HexMetrics.chunkSizeZ != 0
		)
		{
			Debug.LogError("Unsupported map size.");
			return false;
		}

		ClearPath();

		if (chunks != null)
		{
			for (int i = 0; i < chunks.Length; i++)
			{
				Destroy(chunks[i].gameObject);
			}
		}

		CellCountX = x;
		CellCountZ = z;
		chunkCountX = CellCountX / HexMetrics.chunkSizeX;
		chunkCountZ = CellCountZ / HexMetrics.chunkSizeZ;
		CreateChunks();
		CreateCells();
		ShowUI(true);

		// 25.9.23 RI add GameStart
		//if (!GameManage.Instance.GameInit())
		//{
		//	Debug.LogError("Game Init Failed!");
		//}

		// 调整MiniMap的摄像机位置
		if (minimapCamController)
		{
			minimapCamController.Init();
			minimapCamController.PositionCamera(CellCountX, CellCountZ);
		}

		return true;
	}

	void CreateChunks()
	{
		chunks = new HexGridChunk[chunkCountX * chunkCountZ];

		for (int z = 0, i = 0; z < chunkCountZ; z++)
		{
			for (int x = 0; x < chunkCountX; x++)
			{
				HexGridChunk chunk = chunks[i++] = Instantiate(chunkPrefab);
				chunk.transform.SetParent(transform);
				chunk.Grid = this;
			}
		}
	}

	void CreateCells()
	{
		cells = new HexCell[CellCountZ * CellCountX];

		for (int z = 0, i = 0; z < CellCountZ; z++)
		{
			for (int x = 0; x < CellCountX; x++)
			{
				CreateCell(x, z, i++);
			}
		}
	}



	/// <summary>
	/// find cell according to the given position (hit point)
	/// </summary>
	/// <param name="position"></param>
	/// <returns></returns>
	public HexCell GetCell(Vector3 position)
	{
		position = transform.InverseTransformPoint(position);
		HexCoordinates coordinates = HexCoordinates.FromPosition(position);
		int index = coordinates.X + coordinates.Z * CellCountX + coordinates.Z / 2;
		return cells[index];
	}

	public HexCell GetCell(HexCoordinates coordinates)
	{
		int z = coordinates.Z;
		int x = coordinates.X + z / 2;
		if (z < 0 || z >= CellCountZ || x < 0 || x >= CellCountX)
		{
			return null;
		}
		return cells[x + z * CellCountX];

	}

	public bool TryGetCell(HexCoordinates coordinates, out HexCell cell)
	{
		int z = coordinates.Z;
		int x = coordinates.X + z / 2;
		if (z < 0 || z >= CellCountZ || x < 0 || x >= CellCountX)
		{
			cell = null;
			return false;
		}
		cell = cells[x + z * CellCountX];
		return true;
	}

	//25.10.9 Add Find Cell By ID
	public HexCell GetCell(int id)
    {
		return cells[id];
    }


    public void ShowUI(bool visible)
	{
		for (int i = 0; i < chunks.Length; i++)
		{
			chunks[i].ShowUI(visible);
		}
	}


	/// <summary>
	/// Create cell at the given coordinates
	/// </summary>
	/// <param name="x"></param>
	/// <param name="z"></param>
	/// <param name="i"></param>
	void CreateCell(int x, int z, int i)
	{
		Vector3 position;
		position.x = (x + z * 0.5f - z / 2) * (HexMetrics.innerRadius * 2f);
		position.y = 0f;
		position.z = z * (HexMetrics.outerRadius * 1.5f);

		// Create cell from coordinates
		HexCell cell = cells[i] = Instantiate<HexCell>(cellPrefab);
		cell.Grid = this;
		cell.transform.localPosition = position;
		cell.Coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
		cell.Index = i;
	
		// tile coordinate text label
		Text label = Instantiate<Text>(cellLabelPrefab);
		label.rectTransform.anchoredPosition =
			new Vector2(position.x, position.z);
		
		//label.text = cell.coordinates.ToStringOnSeparateLines();
		cell.uiRect = label.rectTransform;
		
		//2025/10/9 disable all highlight
		cell.DisableHighlight();

		// Reset cell elevation
		cell.Elevation = 0;


		/*
		//===========Set boardInfo 
		// 25.9.23 RI add layer to each cell
		cell.gameObject.layer = LayerMask.NameToLayer("Cell");

		// 25.9.23 RI add cell's serial Number
		cell.id = i;

		// 25.9.23 RI set cell's initial infor
		BoardInfor infor = new BoardInfor();

		infor.Cells2DPos.x = x;
		infor.Cells2DPos.y = z;
		infor.Cells3DPos = position;
		infor.id = i;

		// 25.9.23 RI send cell's Infor to GameManage
		GameManage.Instance.SetGameBoardInfor(infor);
		*/

		AddCellToChunk(x, z, cell);

    }

    void AddCellToChunk(int x, int z, HexCell cell)
	{
		int chunkX = x / HexMetrics.chunkSizeX;
		int chunkZ = z / HexMetrics.chunkSizeZ;
		HexGridChunk chunk = chunks[chunkX + chunkZ * chunkCountX];

		int localX = x - chunkX * HexMetrics.chunkSizeX;
		int localZ = z - chunkZ * HexMetrics.chunkSizeZ;
		chunk.AddCell(localX + localZ * HexMetrics.chunkSizeX, cell);
	}

	void OnEnable()
	{
		if (!HexMetrics.noiseSource)
		{
			HexMetrics.noiseSource = noiseSource;
			HexMetrics.InitializeHashGrid(seed);
		}
	}



	public void Save(BinaryWriter writer)
	{
		writer.Write(CellCountX);
		writer.Write(CellCountZ);
		for (int i = 0; i < cells.Length; i++)
		{
			cells[i].Save(writer);
		}
	}

	public void Load(BinaryReader reader, int header)
	{
		ClearPath();
		int x = 20, z = 15;
		if (header >= 1)
		{
			Debug.Log(header);
			x = reader.ReadInt32();
			z = reader.ReadInt32();
		}

		if (x != CellCountX || z != CellCountZ)
		{
			if (!CreateMap(x, z))
			{
				return;
			}
		}

		// 清除原本保存的Index
		startIndex.Clear();
		
		for (int i = 0; i < cells.Length; i++)
		{
			cells[i].Load(reader, header);
			
			// 判断该格子是否是初始
			if (cells[i].IsStartPos)
			{
				startIndex.Add(i);
			}

			// 2025.10.20 将更新后的Cell信息拷贝到GameManager里
			SetGameBoardInfo(cells[i]);

		}
		for (int i = 0; i < chunks.Length; i++)
		{
			chunks[i].Refresh();
		}
	}



	public void FindPath(HexCell fromCell, HexCell toCell, int speed)
	{
		ClearPath();
		currentPathFromIndex = fromCell.Index;
		currentPathToIndex = toCell.Index;
		currentPathExists = Search(fromCell, toCell, speed);
		ShowPath(speed);
	}

    // 25.10.16 RI Add FindPath By Cell's ID
    public void FindPath(int fromCellID, int toCellID, int speed)
    {
        ClearPath();
        currentPathFromIndex = fromCellID;
        currentPathToIndex = toCellID;
        currentPathExists = Search(GetCell(fromCellID), GetCell(toCellID), speed);
        ShowPath(speed);
    }


    public HexCell GetCell (Ray ray) {
		RaycastHit hit;
		if (Physics.Raycast(ray, out hit)) {
			return GetCell(hit.point);
		}
		return null;
	}

	public HexCell GetCell(int xOffset, int zOffset)
	{
		return cells[xOffset + zOffset * CellCountX];
	}



	/// <summary>
	/// 寻路函数
	/// </summary>
	/// <param name="fromCell">起点</param>
	/// <param name="toCell">终点</param>
	/// <param name="speed">行动力</param>
	/// <returns></returns>
	bool Search(HexCell fromCell, HexCell toCell, int speed)
	{
		searchFrontierPhase += 2;
		if (searchFrontier == null)
		{
			searchFrontier = new HexCellPriorityQueue();
		}
		else
		{
			searchFrontier.Clear();
		}

		//广度优先搜索
		fromCell.SearchPhase = searchFrontierPhase;
		fromCell.Distance = 0;
		searchFrontier.Enqueue(fromCell);

		while (searchFrontier.Count > 0)
		{
			HexCell current = searchFrontier.Dequeue();
			current.SearchPhase += 1;
			//当前单元格是目标单元格 中止搜索
			if (current == toCell)
			{
				return true;
			}

			int currentTurn = (current.Distance - 1) / speed;

			for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
			{
				//HexCell neighbor = current.GetNeighbor(d);

				if (!current.TryGetNeighbor(d, out HexCell neighbor) ||
				    neighbor.SearchPhase > searchFrontierPhase)
				{
					continue;
				}

				// 水域不考虑 且不考虑有单位的格子
				if (neighbor.IsUnderwater || neighbor.Unit)
				{
					continue;
				}

				// 山地不考虑
				HexEdgeType edgeType = current.GetEdgeType(neighbor);
				if (edgeType == HexEdgeType.Cliff)
				{
					continue;
				}

				int moveCost;	
				if (current.HasRoadThroughEdge(d))
				{
					moveCost = 1;
				}
				else
				{
					// 如果是平地，距离+1，如果是斜坡，距离+2 //todo:这个地方需要可以人工修改
					moveCost = edgeType == HexEdgeType.Flat ?2 : 4;

					int ForestEffecetor = 1, FarmEffecetor = 1, PlantEffecetor = 1; //均假设特征影响力为1 之后再进行修改
					moveCost += neighbor.ForestLevel * ForestEffecetor + neighbor.FarmLevel * FarmEffecetor +
					            neighbor.PlantLevel * PlantEffecetor;
					
				}

				int distance = current.Distance + moveCost;
				int turn = (distance - 1) / speed;
				if (turn > currentTurn)
				{
					distance = turn * speed + moveCost;
				}

				if (neighbor.SearchPhase < searchFrontierPhase)
				{
					neighbor.SearchPhase = searchFrontierPhase;
					neighbor.Distance = distance;
					/*
					neighbor.SetLabel(turn.ToString());// 游戏不需要UI表示
					*/
					neighbor.PathFromIndex = current.Index;
					neighbor.SearchHeuristic =
						neighbor.Coordinates.DistanceTo(toCell.Coordinates);
					searchFrontier.Enqueue(neighbor);
				}
				else if (distance < neighbor.Distance)
				{
					int oldPriority = neighbor.SearchPriority;
					neighbor.Distance = distance;
					/*
					neighbor.SetLabel(turn.ToString());	// 游戏不需要UI表示
					*/
					neighbor.PathFromIndex = current.Index;
					searchFrontier.Change(neighbor, oldPriority);
				}

			}
		}

		return false;
	}
	
	/// <summary>
	/// 是否存在路径
	/// </summary>
	public bool HasPath
	{
		get
		{
			return currentPathExists;
		}
	}


	/// <summary>
	/// 目标单元格是否为有效的目的地
	/// </summary>
	/// <param name="cell"></param>
	/// <returns></returns>
	public bool IsValidDestination(HexCell cell)
	{
		// 25.10.20_RI Add create unit test
		Debug.Log("cell under water is "+cell.IsUnderwater+" cell unit is "+cell.Unit+" cell Elevation is "+cell.elevation);
		return !cell.IsUnderwater && !cell.Unit && cell.Elevation <= 5;
	}

	/// <summary>
	/// 显示路径
	/// </summary>
	/// <param name="speed">当前的行动力</param>
	void ShowPath(int speed)
	{
		if (currentPathExists)
		{
			HexCell current = cells[currentPathToIndex];
			while (current.Index != currentPathFromIndex)
			{
				int turn = (current.Distance - 1) / speed;
				current.SetLabel(turn.ToString());
				current.EnableHighlight(Color.white);
				current = cells[current.PathFromIndex];
			}
		}
		cells[currentPathFromIndex].EnableHighlight(Color.blue);
		cells[currentPathToIndex].EnableHighlight(Color.red);
	}

	public void ClearPath()
	{
		if (currentPathExists)
		{
			HexCell current = cells[currentPathToIndex];
			while (current.Index != currentPathFromIndex)
			{
				current.SetLabel(null);
				current.DisableHighlight();
				current = cells[current.PathFromIndex];
			}
			current.DisableHighlight();
			currentPathExists = false;
		}
		else if (currentPathFromIndex >= 0)
		{
			cells[currentPathFromIndex].DisableHighlight();
			cells[currentPathToIndex].DisableHighlight();
		}
		currentPathFromIndex = currentPathToIndex = -1;
	}


	public List<HexCell> GetPathCells()
	{
		if (!currentPathExists)
		{
			return null;
		}

		List<HexCell> path = ListPool<HexCell>.Get();
		for (HexCell c = cells[currentPathToIndex];
		     c.Index != currentPathFromIndex;
		     c = cells[c.PathFromIndex])
		{
			path.Add(c);
		}
		path.Add(cells[currentPathFromIndex]);
		path.Reverse();
		return path;
	}

	/// <summary>
	/// 返回单元格路径
	/// </summary>
	/// <returns></returns>
	public List<int> GetPath()
	{
		if (!currentPathExists)
		{
			return null;
		}
		List<int> path = ListPool<int>.Get();
		for (HexCell c = cells[currentPathToIndex];
		     c.Index != currentPathFromIndex;
		     c = cells[c.PathFromIndex])
		{
			path.Add(c.Index);
		}
		path.Add(currentPathFromIndex);
		path.Reverse();
		return path;
	}

	public int GetPlayerAStartCellIndex()
	{
		if (startIndex.Count < 1)
		{
			Debug.Log("StartPosANotFound");
			return 0;
		}

		return startIndex[0];
	}

	public int GetPlayerBStartCellIndex()
	{
		if (startIndex.Count < 2)
		{
			Debug.Log("StartPosBNotFound");
			return 0;
		}

		return startIndex[1];
	}

	void SetGameBoardInfo(HexCell cell)
	{
		// 25.9.23 RI add layer to each cell
		cell.gameObject.layer = LayerMask.NameToLayer("Cell");

		// 25.9.23 RI set cell's initial infor
		BoardInfor infor = new BoardInfor();

		int x = cell.Coordinates.X + cell.Coordinates.Z / 2;
		int z = cell.Coordinates.Z;

		infor.Cells2DPos.x = x;
		infor.Cells2DPos.y = z;
		infor.Cells3DPos = cell.Position;
		infor.id = cell.Index;


		// 判断是否有水
		if (cell.IsUnderwater)
		{
			infor.type = TerrainType.Water;
		}
		else
		{
			// 如果有高度
			if (cell.Elevation > 2)
			{
				infor.type = TerrainType.Mountain;

			}
			else
			{
				if (cell.ForestLevel > 0)
				{
					infor.type = TerrainType.Forest;
				}
				else
				{
					infor.type = TerrainType.Plain;
				}
			}
		}

		GameManage.Instance.SetGameBoardInfor(infor);
	}
}
