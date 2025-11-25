using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;


/// <summary>
/// 显示行动
/// </summary>
public class GameOperationPanel : MonoBehaviour
{

	enum BuyType
	{
		Missionary=0,
		Farmer=1,
		Army=2,
		Building=3
	}

	private PlayerDataManager dataManager;
	private PlayerUnitDataInterface unitDataInterface;
	[Header("UITransform")]
	public Canvas uiCanvas;
	public RectTransform StorePanelTransform;
	public RectTransform ActionPanelTransform;
	[Header("Text")]
	public TMP_Text OperationPanelText;
	public TMP_Text[] CostText= new TMP_Text[4];



	[SerializeField]
	private HexGrid hexGrid;
	public Vector2 screenOffset = new Vector2(0, 30);

	HexCell GetCellUnderCursor() =>
		hexGrid.GetCell(Camera.main.ScreenPointToRay(Input.mousePosition));

	void Start()
	{
		dataManager = PlayerDataManager.Instance;
		unitDataInterface = PlayerUnitDataInterface.Instance;

		if (dataManager == null)
		{
			Debug.LogError("dataManager is null");
		}

		if (unitDataInterface == null)
		{
			Debug.Log("UnitDataInterface is null");
		}


		if (StorePanelTransform)
		{
			StorePanelTransform.gameObject.SetActive(false);
		}
		else
		{
			Debug.LogError("StorePanelTransform is null");
			return;
		}


		UpdateCostText();
	}

	void Update()
	{
		
		if(GameManage.Instance.GetIsGamingOrNot() == false)
		{
			// 关闭面板
			StorePanelTransform.gameObject.SetActive(false);
			ActionPanelTransform.gameObject.SetActive(false);
			return;
		}


		if (!EventSystem.current.IsPointerOverGameObject())
		{
			if (Input.GetMouseButton(0))
			{
				HexCell cell = hexGrid.GetCell(Camera.main.ScreenPointToRay(Input.mousePosition));
				ShowBuyCardInfo(cell.Index);
				
				return;
			}

			if (Input.GetMouseButton(1))
			{
				CloseStorePanel();
			}

		}

		// 当选择对象发生变化的时候 更新操作面板
		UpdateOperationPanelInfo();
		
	}

	/// <summary>随时更新操作面板</summary>
	/// <param name="type">被选中的操作类型</param>
	public void UpdateOperationPanelInfo()
	{

		ActionPanelTransform.gameObject.SetActive(false);
		int chooseUnitID = dataManager.nowChooseUnitID;
		if (chooseUnitID == -1)
			return;

		var cell = GetCellUnderCursor();
		if (cell == null)
			return;

		int2 pos = GameManage.Instance.GetBoardInfor(cell.Index).Cells2DPos;
		
		if (!cell.Unit)
		{
			ShowMovePanelIfNeeded(cell);
			UpdatePanelPos();
			return;
		}

		int ownerID = dataManager.GetUnitOwner(pos);
		bool isLocalPlayer = (ownerID == GameManage.Instance.LocalPlayerID);

		switch (dataManager.nowChooseUnitType)
		{
			case CardType.Farmer:
				HandleFarmer(cell, pos, isLocalPlayer);
				break;

			case CardType.Missionary:
				HandleMissionary(cell, pos, isLocalPlayer);
				break;

			case CardType.Pope:
				HandlePope(cell, pos, isLocalPlayer);
				break;

			case CardType.Solider:
				HandleSolider(cell, pos, isLocalPlayer);
				break;
		}

		UpdatePanelPos();
	}


	/// <summary>
	/// 显示购买面板
	/// </summary>
	/// <param name="isUnit">格子是否有棋子</param>
	public void ShowBuyCardInfo(int hexCellID)
	{
		//获取格子的位置
		var cell = hexGrid.GetCell(hexCellID);
		if (cell == null)
		{
			Debug.LogWarning($"Cell {hexCellID} not found.");
			return;
		}

		if (cell.Unit) return;

		Vector3 cellWorldPos = cell.Position;

		//将格子位置转换为屏幕UI的位置
		if (StorePanelTransform == null)
		{
			Debug.LogWarning("uiCanvas or StorePanelTransform not assigned.");
			return;
		}

		StorePanelTransform.position = cellWorldPos + (Vector3)screenOffset; // 注意：screenOffset 在 WorldSpace 下为世界单位，若需更精确可改为 Vector3 worldOffset
		
		Vector3 screenPoint = Camera.main != null
			? Camera.main.WorldToScreenPoint(cellWorldPos)
			: new Vector3(cellWorldPos.x, cellWorldPos.y, 0f);

		RectTransform canvasRect = uiCanvas.GetComponent<RectTransform>();
		Vector2 localPoint;

		// 在 Overlay 模式下，最后一个参数传 null
		bool ok = RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out localPoint);
		if (!ok)
		{
			Debug.LogWarning("ScreenPointToLocalPointInRectangle failed.");
			return;
		}

		Vector2 anchoredPos = localPoint + screenOffset;
		anchoredPos = ClampToCanvas(anchoredPos, canvasRect, StorePanelTransform);

		// 设定面板的位置
		StorePanelTransform.anchoredPosition = anchoredPos;

		// 显示面板
		StorePanelTransform.gameObject.SetActive(true);
	}

	private void CloseStorePanel()
	{
		StorePanelTransform.gameObject.SetActive(false);
	}

	public void BuyMissionary()
	{
		unitDataInterface.TryBuyUnitToMapByType(CardType.Missionary);
		CloseStorePanel();
	}

	public void BuyFarmer()
	{
		unitDataInterface.TryBuyUnitToMapByType(CardType.Farmer);
		CloseStorePanel();

	}

	public void BuyArmy()
	{
		unitDataInterface.TryBuyUnitToMapByType(CardType.Solider);
		CloseStorePanel();
	}


	public void BuyBuilding()
	{
		unitDataInterface.TryBuyUnitToMapByType(CardType.Building);
		CloseStorePanel();

	}

	private void HandleFarmer(HexCell cell, int2 pos, bool isLocal)
	{
		if (isLocal)
		{
			// 如果是建筑 则显示进入建筑
			var target = dataManager.FindUnit(dataManager.GetUnitOwner(pos), pos);
			if (target.HasValue && target.Value.IsBuilding())
			{
				ShowPanel("建物入る.");
			}
			else
			{
				int cost = unitDataInterface.GetUnitOperationCostByType(GameData.OperationType.Cure);
				ShowPanel("治療：" + cost);
			}
		}
	}

	private void HandleMissionary(HexCell cell, int2 pos, bool isLocal)
	{
		if (!isLocal)
		{
			int cost = unitDataInterface.GetUnitOperationCostByType(GameData.OperationType.Charm);
			ShowPanel("伝教：" + cost);
			return;
		}

		// 占领逻辑
		if (dataManager.GetCellIdByUnitId(dataManager.nowChooseUnitID) == cell.Index)
		{
			int cellOwner = dataManager.GetCellOwner(cell.Index);
			if (cellOwner != GameManage.Instance.LocalPlayerID)
			{
				int occupy = unitDataInterface.GetUnitOperationCostByType(GameData.OperationType.Occupy);
				ShowPanel("占領:" + occupy);
			}
		}
	}

	private void HandlePope(HexCell cell, int2 pos, bool isLocal)
	{
		if (!isLocal)
			return;

		// 如果冷却未结束 则不能交换位置则显示冷却信息
		if (!PieceManager.Instance.GetCanPopeSwap(dataManager.nowChooseUnitID))
		{
			ShowPanel("Switch is not ready!");
			return;
		}

		var target = dataManager.FindUnit(dataManager.GetUnitOwner(pos), pos);
		if (target.HasValue && !target.Value.IsBuilding())
		{
			// 教皇无法自己交换自己
			if (dataManager.GetCellIdByUnitId(dataManager.nowChooseUnitID) != cell.Index)
			{
				ShowPanel("位置交換:2");
			}
		}
	}

	private void HandleSolider(HexCell cell, int2 pos, bool isLocal)
	{
		if (!isLocal)
		{
			int cost = unitDataInterface.GetUnitOperationCostByType(GameData.OperationType.Attack);
			ShowPanel("攻撃：" + cost);
		}
	}

	private void ShowPanel(string msg)
	{
		OperationPanelText.text = msg;
		ActionPanelTransform.gameObject.SetActive(true);
	}


	private void ShowMovePanelIfNeeded(HexCell cell)
	{
		// 教皇不能移动
		if (dataManager.nowChooseUnitType == CardType.Pope)
			return;

		// 农民的移动有限制 无法移动到非己方格子
		int cellOwner = dataManager.GetCellOwner(cell.Index);
		if (cellOwner != GameManage.Instance.LocalPlayerID && dataManager.nowChooseUnitType == CardType.Farmer) 
		{
			return;
		}

		int moveCost = unitDataInterface.GetUnitOperationCostByType(GameData.OperationType.Move);
		ShowPanel("移動:" + moveCost);
	}

	private Vector2 ClampToCanvas(Vector2 anchoredPos, RectTransform canvasRect, RectTransform panel)
	{
		Vector2 canvasSize = canvasRect.rect.size;
		Vector2 panelSize = panel.rect.size;

		float minX = -canvasSize.x * 0.5f + panel.pivot.x * panelSize.x;
		float maxX = canvasSize.x * 0.5f - (1 - panel.pivot.x) * panelSize.x;
		float minY = -canvasSize.y * 0.5f + panel.pivot.y * panelSize.y;
		float maxY = canvasSize.y * 0.5f - (1 - panel.pivot.y) * panelSize.y;

		anchoredPos.x = Mathf.Clamp(anchoredPos.x, minX, maxX);
		anchoredPos.y = Mathf.Clamp(anchoredPos.y, minY, maxY);
		return anchoredPos;
	}

	private void UpdatePanelPos()
	{
		if (ActionPanelTransform == null || uiCanvas == null)
			return;

		// 取得 Canvas Rect
		RectTransform canvasRect = uiCanvas.GetComponent<RectTransform>();

		// 获取鼠标屏幕坐标
		Vector2 mouseScreenPos = Input.mousePosition;

		// 转换成 UI 本地坐标
		Vector2 mouseLocalPos;
		RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, mouseScreenPos, null, out mouseLocalPos);

		// 加上偏移，防止挡住鼠标
		Vector2 anchoredPos = mouseLocalPos + screenOffset;

		// 防止 UI 越界
		anchoredPos = ClampToCanvas(anchoredPos, canvasRect, StorePanelTransform);

		// 设置位置
		ActionPanelTransform.anchoredPosition = anchoredPos;

	}

	public void UpdateCostText()
	{
		// Init cost text
		GameData.Religion playerReligion = SceneStateManager.Instance.PlayerReligion;

		CostText[(int)BuyType.Missionary].text = unitDataInterface.GetCreateUnitResoursesCost(playerReligion, CardType.Missionary).ToString();
		CostText[(int)BuyType.Army].text = unitDataInterface.GetCreateUnitResoursesCost(playerReligion, CardType.Solider).ToString();
		CostText[(int)BuyType.Farmer].text = unitDataInterface.GetCreateUnitResoursesCost(playerReligion, CardType.Farmer).ToString();
		//CostText[(int)BuyType.Building].text = unitDataInterface.GetCreateUnitResoursesCost(playerReligion, CardType.Building).ToString();

		CostText[(int)BuyType.Building].text = "10"; // 建筑固定10资源
	}

}
