using System.Collections;
using System.Collections.Generic;
using UnityEditor.MemoryProfiler;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using Unity.Mathematics;
using Unity.VisualScripting;



public class HexGrid : MonoBehaviour
{
	public int cellCountX = 20, cellCountZ = 15;    // 棋盘大小 CellCountX: 横向格子数 cellCountZ：纵向格子数
	int chunkCountX, chunkCountZ;

	public HexCell cellPrefab;		//单个格子的预制件
	HexCell[] cells;
	public Text cellLabelPrefab;    // coordinate text
	public Texture2D noiseSource;
	public HexGridChunk chunkPrefab;

	int searchFrontierPhase;

	public int seed;
	//public Color[] colors;

	HexGridChunk[] chunks;
	HexCellPriorityQueue searchFrontier;    // 搜索优先队列

	public HexCell currentPathFrom, currentPathTo;	// 当前搜索起点和终点
	bool currentPathExists; // 是否存在路径


	void Start()
	{
		HexMetrics.noiseSource = noiseSource;
		HexMetrics.InitializeHashGrid(seed);
		CreateMap(cellCountX, cellCountZ);
		
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

		cellCountX = x;
		cellCountZ = z;
		chunkCountX = cellCountX / HexMetrics.chunkSizeX;
		chunkCountZ = cellCountZ / HexMetrics.chunkSizeZ;
		CreateChunks();
		CreateCells();
		ShowUI(true);

		// 25.9.23 RI add GameStart
		//if (!GameManage.Instance.GameInit())
		//{
		//	Debug.LogError("Game Init Failed!");
		//}


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
			}
		}
	}

	void CreateCells()
	{
		cells = new HexCell[cellCountZ * cellCountX];

		for (int z = 0, i = 0; z < cellCountZ; z++)
		{
			for (int x = 0; x < cellCountX; x++)
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
		int index = coordinates.X + coordinates.Z * cellCountX + coordinates.Z / 2;
		return cells[index];
	}

	public HexCell GetCell(HexCoordinates coordinates)
	{
		int z = coordinates.Z;
		if (z < 0 || z >= cellCountZ)
		{
			return null;
		}
		int x = coordinates.X + z / 2;
		if (x < 0 || x >= cellCountX)
		{
			return null;
		}
		return cells[x + z * cellCountX];
		
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
		cell.transform.localPosition = position;
		cell.coordinates = HexCoordinates.FromOffsetCoordinates(x, z);

        // Set cell neighbors in west direction
        if (x > 0)
		{
			cell.SetNeighbor(HexDirection.W, cells[i - 1]);
		}
		if (z > 0)
		{
			if ((z & 1) == 0)// even rows 偶数排
			{
				//Connecting from NW to SE on even rows.
				cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX]);
				if (x > 0)  //Connecting from NE to SW on even rows.
				{
					cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX - 1]);
				}
			}
			else // Odds rows 奇数排
			{
				cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX]);
				if (x < cellCountX - 1)
				{
					cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX + 1]);
				}
			}
		}

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

        // 25.9.23 RI add layer to each cell
        cell.gameObject.layer = LayerMask.NameToLayer("Cell");

		

        // 25.9.23 RI add cell's serial Number
        cell.id = i;

        // 25.9.23 RI set cell's initial infor
        BoardInfor infor = new BoardInfor();

		infor.Cells2DPos.x = x; 
        infor.Cells2DPos.y= z; 
        infor.Cells3DPos = position;
        infor.id = i;

		// 25.9.23 RI send cell's Infor to GameManage
        GameManage.Instance.SetGameBoardInfor(infor);

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
		writer.Write(cellCountX);
		writer.Write(cellCountZ);
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
			x = reader.ReadInt32();
			z = reader.ReadInt32();
		}

		if (x != cellCountX || z != cellCountZ)
		{
			if (!CreateMap(x, z))
			{
				return;
			}
           
		}

		for (int i = 0; i < cells.Length; i++)
		{
			cells[i].Load(reader, header);
			
			// 该格子的类型，是否可通过，是否可占领的信息
			CellInfo cellInfo = new CellInfo();

			// 判断是否有水
			if (cells[i].IsUnderwater)
			{
				cellInfo.isCapturable = false; // 不可占领
				cellInfo.isPassalbe = false; // 不可通过
				cellInfo.type = TerrainType.Water;
			}
			else
			{
				// 如果有高度
				if (cells[i].Elevation > 2)
				{
					cellInfo.isCapturable = false; // 不可占领
					cellInfo.isPassalbe = false; // 不可通过
					cellInfo.type = TerrainType.Mountain;

				}
				else
				{
					if (cells[i].ForestLevel > 0)
					{
						cellInfo.isCapturable = false;
						cellInfo.isPassalbe = true;
						cellInfo.type = TerrainType.Forest;
					}
					else
					{
						cellInfo.isCapturable = true;
						cellInfo.isPassalbe = true;
						cellInfo.type = TerrainType.Plain;
					}
				}
			}
		}
		for (int i = 0; i < chunks.Length; i++)
		{
			chunks[i].Refresh();
		}
	}



	public void FindPath(HexCell fromCell, HexCell toCell, int speed)
	{
		ClearPath();
		currentPathFrom = fromCell;
		currentPathTo = toCell;
		currentPathExists = Search(fromCell, toCell, speed);
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
		return cells[xOffset + zOffset * cellCountX];
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
				HexCell neighbor = current.GetNeighbor(d);

				if (neighbor == null ||
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
					neighbor.SetLabel(turn.ToString());
					neighbor.PathFrom = current;
					neighbor.SearchHeuristic =
						neighbor.coordinates.DistanceTo(toCell.coordinates);
					searchFrontier.Enqueue(neighbor);
				}
				else if (distance < neighbor.Distance)
				{
					int oldPriority = neighbor.SearchPriority;
					neighbor.Distance = distance;
					neighbor.SetLabel(turn.ToString());
					neighbor.PathFrom = current;
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
		return !cell.IsUnderwater && !cell.Unit && cell.Elevation < 2;
	}

	/// <summary>
	/// 显示路径
	/// </summary>
	/// <param name="speed">当前的行动力</param>
	void ShowPath(int speed)
	{
		if (currentPathExists)
		{
			HexCell current = currentPathTo;
			while (current != currentPathFrom)
			{
				int turn = (current.Distance - 1) / speed;
				current.SetLabel(turn.ToString());
				current.EnableHighlight(Color.white);
				current = current.PathFrom;
			}
		}
		currentPathFrom.EnableHighlight(Color.blue);
		currentPathTo.EnableHighlight(Color.red);
	}

	public void ClearPath()
	{
		if (currentPathExists)
		{
			HexCell current = currentPathTo;
			while (current != currentPathFrom)
			{
				current.SetLabel(null);
				current.DisableHighlight();
				current = current.PathFrom;
			}
			current.DisableHighlight();
			currentPathExists = false;
		}
		else if (currentPathFrom)
		{
			currentPathFrom.DisableHighlight();
			currentPathTo.DisableHighlight();
		}
		currentPathFrom = currentPathTo = null;
	}


	/// <summary>
	/// 返回单元格路径
	/// </summary>
	/// <returns></returns>
	public List<HexCell> GetPath()
	{
		if (!currentPathExists)
		{
			return null;
		}
		List<HexCell> path = ListPool<HexCell>.Get();
		for (HexCell c = currentPathTo; c != currentPathFrom; c = c.PathFrom)
		{
			path.Add(c);
		}
		path.Add(currentPathFrom);
		path.Reverse();
		return path;
	}


	/// <summary>
	/// 返回二维数列
	/// </summary>
	/// <returns></returns>
	public List<HexCoordinates> GetPathCoordinate()
	{
		// RI test
		if (!currentPathExists)
		{
			return null;
		}

		List<HexCoordinates> coordinates = ListPool<HexCoordinates>.Get();
		for (HexCell c = currentPathTo; c != currentPathFrom; c = c.PathFrom)
		{
			coordinates.Add(c.coordinates);
		}
		coordinates.Add(currentPathFrom.coordinates);
		coordinates.Reverse();
		return coordinates;
	}


}
