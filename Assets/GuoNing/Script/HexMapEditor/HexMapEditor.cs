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
	int activeForestLevel, activeFarmLevel, activePlantLevel, activeSpecialIndex;



	bool applyElevation;
	bool applyWaterLevel;
	bool applyForestnLevel, applyFarmLevel, applyPlantLevel, applySpecialIndex;

	int brushSize;
	int activeTerrainTypeIndex;



	enum OptionalToggle
	{
		Ignore, Yes, No
	}

	OptionalToggle riverMode, roadMode,walledMode;

	bool isDrag;
	HexDirection dragDirection;
	int previousCellIndex = -1;

	void Awake()
	{
		terrainMaterial.DisableKeyword("GRID_ON");
		SetEditMode(true);
	}

	void Update()
	{
		if (
			Input.GetMouseButton(0) &&
			!EventSystem.current.IsPointerOverGameObject()
		)
		{
			HandleInput();
		}
		else
		{
			previousCellIndex = -1;
		}
	}

	void HandleInput()
	{
		Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
		RaycastHit hit;
		if (Physics.Raycast(inputRay, out hit))
		{
			HexCell currentCell = hexGrid.GetCell(hit.point);
			if (previousCellIndex >= 0 && previousCellIndex != currentCell.Index)
			{
				ValidateDrag(currentCell);
			}
			else
			{
				isDrag = false;
			}

	
			EditCells(currentCell);
			previousCellIndex = currentCell.Index;
		}
		else
		{
			previousCellIndex = -1;
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
			if (hexGrid.GetCell(previousCellIndex).GetNeighbor(dragDirection) ==
			    currentCell)
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
			if (walledMode != OptionalToggle.Ignore)
			{
				cell.Walled = walledMode == OptionalToggle.Yes;
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
			if (applySpecialIndex)
			{
				cell.SpecialIndex = activeSpecialIndex;
			}
			if (isDrag &&
			    cell.TryGetNeighbor(dragDirection.Opposite(), out HexCell otherCell))
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

	/// <summary>
	/// 是否是编辑模式
	/// </summary>
	/// <param name="toggle"></param>
	public void SetEditMode(bool toggle)
	{
		enabled = toggle;
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
		int centerX = center.Coordinates.X;
		int centerZ = center.Coordinates.Z;

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

	public void SetWalledMode(int mode)
	{
		walledMode = (OptionalToggle)mode;
	}

	public void SetApplySpecialIndex(bool toggle)
	{
		applySpecialIndex = toggle;
	}

	public void SetSpecialIndex(float index)
	{
		activeSpecialIndex = (int)index;
	}

	HexCell GetCellUnderCursor()
	{
		return
			hexGrid.GetCell(Camera.main.ScreenPointToRay(Input.mousePosition));
	}

}