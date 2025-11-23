using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;


/// <summary>
/// 显示行动
/// </summary>
public class GameOperationPanel : MonoBehaviour
{
	private PlayerDataManager dataManager;
	private PlayerUnitDataInterface unitDataInterface;

	public RectTransform StorePanelTransform;
	public TMP_Text text;
	public RectTransform ActionPanelTransform;
	public Canvas uiCanvas;
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
	}

	void Update()
	{
		if (!EventSystem.current.IsPointerOverGameObject())
		{
			if (Input.GetMouseButton(0))
			{
				HexCell cell = hexGrid.GetCell(Camera.main.ScreenPointToRay(Input.mousePosition));
				ShowBuyCardInfo(cell.Index);
				return;
			}

		}

	}

	/// <summary>随时更新操作面板</summary>
	/// <param name="type">被选中的操作类型</param>
	public void UpdateOperationPanelInfo(CardType type)
	{
		// 没有选中任何我方棋子 则不显示操作面板
		if (PlayerDataManager.Instance.nowChooseUnitID == -1)
		{
			ActionPanelTransform.gameObject.SetActive(false);
			return;
		}

		// 获取现在鼠标所在的HexCell
		var cell = GetCellUnderCursor();
		if (cell == null)
		{
			Debug.LogWarning($"Cell not found.");
			return;
		}

		// 如果没有单位，则显示移动面板
		if (!cell.Unit)
		{
			// 获取移动所需要的资源
			int moveCost = 1;

			// 更新面板信息
			text.text = "移动:" + moveCost;

			// 获取格子所属的玩家
			//int cellOwnerID = PlayerDataManager.Instance.GetCellOwner(cell.Index);



		}
		else
		{
			// 获取棋子所属的玩家
			int ownerID = PlayerDataManager.Instance.GetUnitOwner(GameManage.Instance.GetBoardInfor(cell.Index).Cells2DPos);
		

			bool isLocalPlayer = (ownerID == GameManage.Instance.LocalPlayerID);

			switch (PlayerDataManager.Instance.nowChooseUnitType)
			{
				case CardType.Farmer:
					if (!isLocalPlayer) return;
					// 当选中的是当前农民所在的格子 显示农民治疗面板
					text.text = "todo!!";
					// 或者当选中的是我方的建筑单位时 显示进入建筑面板

					break;
				case CardType.Missionary:
					if (isLocalPlayer)
					{
						// 当选中的是当前教士所在的格子 显示教士占领面板
						int cost = 3;
						text.text = "Occupy" + cost;
					}
					else
					{
						// 当选中的是敌方所在的格子 显示教士魅惑面板
						int cost = 3;
						// 更新魅惑面板信息
						text.text ="Charm:" + cost;
					}
					break;
				case CardType.Pope:
					if (!isLocalPlayer) return;
					// 当选中的是我方单位时 显示交换位置面板
					// 获取交换位置所需回合数
					int round = 2;
					// 更新交换位置面板信息
					text.text="位置交換:" + round;	
					break;
				case CardType.Solider:
					if (!isLocalPlayer)
					{
						// 获得攻击所需行动力
						int attackCost = 2;

						// 更新攻击面板信息
						text.text = "攻击:" + attackCost;


					}
					break;
			}
		}

		// 显示移动面板
		ActionPanelTransform.gameObject.SetActive(true);
		// 调整移动面板的位置
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
		PlayerUnitDataInterface.Instance.BuyUnitToMapByType(CardType.Missionary);
		CloseStorePanel();
	}

	public void BuyFarmer()
	{
		PlayerUnitDataInterface.Instance.BuyUnitToMapByType(CardType.Farmer);
		CloseStorePanel();

	}

	public void BuyArmy()
	{
		PlayerUnitDataInterface.Instance.BuyUnitToMapByType(CardType.Solider);
		CloseStorePanel();

	}


	public void BuyBuilding()
	{
		PlayerUnitDataInterface.Instance.BuyUnitToMapByType(CardType.Building);
		CloseStorePanel();

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

}
