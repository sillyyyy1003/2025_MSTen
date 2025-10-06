using UnityEngine;
using UnityEngine.EventSystems;
using System.IO;

public class HexMapEditor : MonoBehaviour
{
	public bool isActive = true;	// 是否有效
	public HexGrid hexGrid;
	public Material terrainMaterial;	// 网格材质->这个之后会需要加入系统 显示网格

	private int activeElevation;
	private int activeWaterLevel;
	int activeForestLevel, activeFarmLevel, activePlantLevel;


	bool applyElevation;
	bool applyWaterLevel;
	bool applyForestnLevel, applyFarmLevel, applyPlantLevel;

	int brushSize;

	int activeTerrainTypeIndex;

	

	enum OptionalToggle
	{
		Ignore, Yes, No
	}

	OptionalToggle riverMode, roadMode;

	bool isDrag;
	bool editMode ;	// 是否是编辑模式
	HexDirection dragDirection;
	HexCell previousCell, searchFromCell, searchToCell;

	void Awake()
	{
		terrainMaterial.DisableKeyword("GRID_ON");

	}

	void Update()
	{
		if (!isActive) return;
		if (
			Input.GetMouseButton(0) &&
			!EventSystem.current.IsPointerOverGameObject()
		)
		{
			HandleInput();
		}
		else
		{
			previousCell = null;
		}
	}

	void HandleInput()
	{
		Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
		RaycastHit hit;
		if (Physics.Raycast(inputRay, out hit))
		{
			HexCell currentCell = hexGrid.GetCell(hit.point);
			if (previousCell && previousCell != currentCell)
			{
				ValidateDrag(currentCell);
			}
			else
			{
				isDrag = false;
			}

			if (editMode)
			{
				//如果是编辑模式 编辑单元格
				EditCells(currentCell);
			}
			else if (Input.GetKey(KeyCode.LeftShift) && searchToCell != currentCell)
			{
				// 选中某个单元格 显示所有的路径
				if (searchFromCell)
				{
					searchFromCell.DisableHighlight();
				}
				searchFromCell = currentCell;
				searchFromCell.EnableHighlight(Color.blue);

				if (searchToCell)
				{
					hexGrid.FindPath(searchFromCell, searchToCell);
				}
			}
			else if (searchFromCell && searchFromCell != currentCell)
			{
				// 选中目标单元格和起始单元格
				searchToCell = currentCell;
				hexGrid.FindPath(searchFromCell, searchToCell);
			}

			previousCell = currentCell;
		}
		else
		{
			previousCell = null;
		}
	}

	void ValidateDrag(HexCell currentCell)
	{
		for (
			dragDirection = HexDirection.NE;
			dragDirection <= HexDirection.NW;
			dragDirection++
		)
		{
			if (previousCell.GetNeighbor(dragDirection) == currentCell)
			{
				isDrag = true;
				return;
			}
		}
		isDrag = false;
	}

	void EditCell(HexCell cell)
	{
		if (cell)
		{
			if (activeTerrainTypeIndex >= 0)
			{
				cell.TerrainTypeIndex = activeTerrainTypeIndex;
			}
			if (applyElevation)
			{
				cell.Elevation = activeElevation;
			}
			if (applyWaterLevel)
			{
				cell.WaterLevel = activeWaterLevel;
			}
			if (applyForestnLevel)
			{
				cell.ForestLevel = activeForestLevel;
			}
			if (applyFarmLevel)
			{
				cell.FarmLevel = activeFarmLevel;
			}
			if (applyPlantLevel)
			{
				cell.PlantLevel = activePlantLevel;
			}
			if (riverMode == OptionalToggle.No)
			{
				cell.RemoveRiver();
			}
			if (roadMode == OptionalToggle.No)
			{
				cell.RemoveRoads();
			}
			if (isDrag)
			{
				HexCell otherCell = cell.GetNeighbor(dragDirection.Opposite());
				if (otherCell)
				{
					if (riverMode == OptionalToggle.Yes)
					{
						otherCell.SetOutgoingRiver(dragDirection);
					}
					if (roadMode == OptionalToggle.Yes)
					{
						otherCell.AddRoad(dragDirection);
					}
				}
			}
		}
	}

	/// <summary>
	/// 是否是编辑模式
	/// </summary>
	/// <param name="toggle"></param>
	public void SetEditMode(bool toggle)
	{
		editMode = toggle;
		hexGrid.ShowUI(!toggle);
	}

	/// <summary>
	/// 是否显示网格 
	/// </summary>
	/// <param name="visible"></param>
	public void ShowGrid(bool visible)
	{
		if(visible)terrainMaterial.EnableKeyword("GRID_ON");
		else terrainMaterial.DisableKeyword("GRID_ON");
	}

	void EditCells(HexCell center)
	{
		int centerX = center.coordinates.X;
		int centerZ = center.coordinates.Z;

		for (int r = 0, z = centerZ - brushSize; z <= centerZ; z++, r++)
		{
			for (int x = centerX - r; x <= centerX + brushSize; x++)
			{
				EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
			}
		}
		for (int r = 0, z = centerZ + brushSize; z > centerZ; z--, r++)
		{
			for (int x = centerX - brushSize; x <= centerX + r; x++)
			{
				EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
			}
		}
	}

	public void SetElevation(float elevation)
	{
		activeElevation = (int)elevation;
	}
	public void SetApplyElevation(bool toggle)
	{
		applyElevation = toggle;
	}
	public void SetBrushSize(float size)
	{
		brushSize = (int)size;
	}

	public void SetRiverMode(int mode)
	{
		riverMode = (OptionalToggle)mode;
	}

	public void SetRoadMode(int mode)
	{
		roadMode = (OptionalToggle)mode;
	}

	public void SetActive(bool _isActive)
	{
		isActive = _isActive;
	}

	public void SetApplyWaterLevel(bool toggle)
	{
		applyWaterLevel = toggle;
	}

	public void SetWaterLevel(float level)
	{
		activeWaterLevel = (int)level;
	}

	public void SetApplyForestLevel(bool toggle)
	{
		applyForestnLevel = toggle;
	}

	public void SetForestLevel(float level)
	{
		activeForestLevel = (int)level;
	}

	public void SetApplyFarmLevel(bool toggle)
	{
		applyFarmLevel = toggle;
	}

	public void SetFarmLevel(float level)
	{
		activeFarmLevel = (int)level;
	}

	public void SetApplyPlantLevel(bool toggle)
	{
		applyPlantLevel = toggle;
	}

	public void SetPlantLevel(float level)
	{
		activePlantLevel = (int)level;
	}

	public void SetTerrainTypeIndex(int index)
	{
		activeTerrainTypeIndex = index;
	}



}