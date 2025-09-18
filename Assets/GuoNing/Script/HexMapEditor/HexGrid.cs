using System.Collections;
using System.Collections.Generic;
using UnityEditor.MemoryProfiler;
using UnityEngine;
using UnityEngine.UI;

public class HexGrid : MonoBehaviour
{
	int cellCountX, cellCountZ;

	public HexCell cellPrefab;
	HexCell[] cells;
	public Text cellLabelPrefab;    // coordinate text
	Canvas gridCanvas;              // canvas for ui
	HexMesh hexMesh;                // triangle meshes
	public Color defaultColor = Color.white;        // mesh color for default
	public Texture2D noiseSource;
	public int chunkCountX = 4, chunkCountZ = 3;
	public HexGridChunk chunkPrefab;

	HexGridChunk[] chunks;
	void Awake()
	{
		HexMetrics.noiseSource = noiseSource;

		gridCanvas = GetComponentInChildren<Canvas>();
		hexMesh = GetComponentInChildren<HexMesh>();

		cellCountX = chunkCountX * HexMetrics.chunkSizeX;
		cellCountZ = chunkCountZ * HexMetrics.chunkSizeZ;

		CreateChunks();
		CreateCells();
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

	void Start()
	{
	
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
		cell.Color = defaultColor;

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
		label.text = cell.coordinates.ToStringOnSeparateLines();
		cell.uiRect = label.rectTransform;

		// Reset cell elevation
		cell.Elevation = 0;

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



}
