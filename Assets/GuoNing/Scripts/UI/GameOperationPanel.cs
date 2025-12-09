using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


/// <summary>
/// 显示行动
/// </summary>
public class GameOperationPanel : MonoBehaviour
{

	enum BuyType
	{
		Missionary = 0,
		Farmer = 1,
		Army = 2,
		Building = 3
	}

	private PlayerDataManager dataManager;
	private PlayerUnitDataInterface unitDataInterface;
	[Header("UITransform")]
	public Canvas uiCanvas;
	public RectTransform StorePanelTransform;
	public RectTransform ActionPanelTransform;
	
	[Header("SpeicalButton")]
	public Button SpecialButton;

	[Header("Images")]
	public Image MouseImage;
	[Header("Text")]
	public TMP_Text OperationPanelText;
	public TMP_Text[] CostText = new TMP_Text[4];
	public TMP_Text ResourceText;

	private event System.Action OnCardTypeBought;

	[SerializeField]
	private HexGrid hexGrid;
	public Vector2 screenOffset = new Vector2(0, 30);

	private int BuyUnitCellID = -1;     // 在哪个格子购买单位
	HexCell GetCellUnderCursor() =>
		hexGrid.GetCell(Camera.main.ScreenPointToRay(Input.mousePosition));
	static readonly int rightClickHighlightId = Shader.PropertyToID("_RightClickHighlight");
	static readonly int rightClickHighlightColorId = Shader.PropertyToID("_RightClickColor");


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

		OnCardTypeBought += HandleResourceUpdate;
		OnCardTypeBought += () => SoundManager.Instance.PlaySE(SoundSystem.TYPE_SE.SPAWNUNIT);
		UpdateCostText();

		// 绑定事件
		SpecialButton.onClick.AddListener(OnSpecialButtonClick);
		// 设定右键点击高光
		Shader.SetGlobalColor(rightClickHighlightColorId, new Color(0f, 0f, 1f, 1f));   // 蓝色 Click
	}

	void Update()
	{

		// 如果不是我的回合 则不处理
		if (!GameManage.Instance._PlayerOperation.IsMyTurn) return;

		if (GameManage.Instance.GetIsGamingOrNot() == false)
		{
			// 关闭面板
			StorePanelTransform.gameObject.SetActive(false);
			ActionPanelTransform.gameObject.SetActive(false);
			return;
		}


		if (!EventSystem.current.IsPointerOverGameObject())
		{
			// 右键显示购买面板
			if (Input.GetMouseButton(1))
			{
				HexCell cell = hexGrid.GetCell(Camera.main.ScreenPointToRay(Input.mousePosition));
				if (cell != null)
				{
					ShowBuyCardInfo(cell);
				}
				return;
			}

			// 点击任意处关闭购买面板
			if (Input.GetMouseButton(0))
			{
				CloseStorePanel();
			}

			// 当选择对象发生变化的时候 更新操作面板
			UpdateOperationPanelInfo();
		}
	}

	/// <summary>随时更新操作面板</summary>
	/// <param name="type">被选中的操作类型</param>
	public void UpdateOperationPanelInfo()
	{
		// 先隐藏面板
		ActionPanelTransform.gameObject.SetActive(false);

		// 如果没有选择单位 则不显示操作面板
		if (dataManager.nowChooseUnitID == -1)
			return;

		var cell = GetCellUnderCursor();
		if (cell == null)
			return;

		int2 pos = GameManage.Instance.GetBoardInfor(cell.Index).Cells2DPos;

		// 如果目标格子没有单位 则不显示
		if (!cell.Unit)
		{
			//ShowMovePanelIfNeeded(cell);
			//UpdatePanelPos();
			return;
		}

		// 如果目标格子有单位 则根据单位类型显示对应的面板
		int ownerId = dataManager.GetUnitOwner(pos);
		bool isLocalPlayer = (ownerId == GameManage.Instance.LocalPlayerID);

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

			case CardType.Soldier:
				HandleSolider(cell, pos, isLocalPlayer);
				break;
		}

		
	}


	/// <summary>
	/// 显示购买面板
	/// </summary>
	/// <param name="cell">格子</param>
	public void ShowBuyCardInfo(HexCell cell)
	{
		// 如果已经选择了单位 则不显示购买面板
		if (PlayerDataManager.Instance.nowChooseUnitID != -1) return;

		// 如果格子上有单位 则不显示购买面板
		if (cell.Unit) return;

		// 如果格子不是我方领地  则不显示购买面板
		int localPlayerId = GameManage.Instance.LocalPlayerID;
		if (PlayerDataManager.Instance.GetCellOwner(cell.Index) != localPlayerId) return;

		BuyUnitCellID = cell.Index;
		Vector3 cellWorldPos = cell.Position;

		//UpdateRightClickHighlight(cell);


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
		//ClearRightClickCellHighlightData();
		StorePanelTransform.gameObject.SetActive(false);
	}

	public void BuyMissionary()
	{
		// 更新Resource
		if (unitDataInterface.TryBuyUnitToMapByType(CardType.Missionary, BuyUnitCellID))
			OnCardTypeBought?.Invoke();

		CloseStorePanel();
	}

	public void BuyFarmer()
	{
		if (unitDataInterface.TryBuyUnitToMapByType(CardType.Farmer, BuyUnitCellID))
			OnCardTypeBought?.Invoke();

		CloseStorePanel();

	}

	public void BuyArmy()
	{
		if (unitDataInterface.TryBuyUnitToMapByType(CardType.Soldier, BuyUnitCellID))
			OnCardTypeBought?.Invoke();
		CloseStorePanel();
	}


	public void BuyBuilding()
	{
		if (unitDataInterface.TryBuyUnitToMapByType(CardType.Building, BuyUnitCellID))
			OnCardTypeBought?.Invoke();
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
				MouseImage.sprite = UISpriteHelper.Instance.GetSubSprite(UISpriteID.MouseInteraction, "RightButtonClick");
				ShowPanel("建物に入る");
				UpdatePanelPos();
			}
			else
			{
				MouseImage.sprite = UISpriteHelper.Instance.GetSubSprite(UISpriteID.MouseInteraction, "RightButtonPress");
				int cost = unitDataInterface.GetUnitOperationCostByType(GameData.OperationType.Cure);
				ShowButtonPanel("治療");

				UpdatePanelPos(cell,true);
			}
		}
	}

	private void HandleMissionary(HexCell cell, int2 pos, bool isLocal)
	{
		//if (!isLocal)
		//{
		//	// 如果目标格子距离选中格子的距离大于1则不显示
		//	if (GetDistanceFurtherThanValue(1, cell)) return;

		//	// 创建面板数据 显示面板
		//	MouseImage.sprite = UISpriteHelper.Instance.GetSubSprite(UISpriteID.MouseInteraction, "RightButtonClick");
		//	int cost = unitDataInterface.GetUnitOperationCostByType(GameData.OperationType.Charm);
		//	ShowPanel("伝教：" + cost);
		//	UpdatePanelPos();
		//	return;
		//}

		// 占领逻辑
		if (dataManager.GetCellIdByUnitId(dataManager.nowChooseUnitID) == cell.Index)
		{
			// 25.12.9 RI 修改update中的Debug
			//Debug.Log("Unit Cell ID:" + dataManager.GetCellIdByUnitId(dataManager.nowChooseUnitID) + "cell id:" + cell.Index);
			int cellOwner = dataManager.GetCellOwner(cell.Index);
			if (cellOwner != GameManage.Instance.LocalPlayerID)
			{
				int occupy = unitDataInterface.GetUnitOperationCostByType(GameData.OperationType.Occupy);
				
				// 如果行动力不足 则按钮无法交互
				if(PieceManager.Instance.GetPieceAP(dataManager.nowChooseUnitID) < occupy)
				{
					ShowButtonPanel("占領: 行動力不足");
					SpecialButton.interactable = false;

				}
				else
				{
					ShowButtonPanel("占領: " + occupy + " 行動力");
					SpecialButton.interactable = true;
				}

				UpdatePanelPos(cell, true);
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
			//MouseImage.sprite = UISpriteHelper.Instance.GetSubSprite(UISpriteID.MouseInteraction, "RightButtonClick");
			//ShowPanel("Switch is not ready!");
			//UpdatePanelPos();
			OperationBroadcastManager.Instance.ShowMessage("スキルはクールダウン中");
			return;
		}

		var target = dataManager.FindUnit(dataManager.GetUnitOwner(pos), pos);
		if (target.HasValue && !target.Value.IsBuilding())
		{
			// 教皇无法自己交换自己
			if (dataManager.GetCellIdByUnitId(dataManager.nowChooseUnitID) != cell.Index)
			{
				MouseImage.sprite = UISpriteHelper.Instance.GetSubSprite(UISpriteID.MouseInteraction, "RightButtonClick");
				ShowPanel("位置交換可能");
				UpdatePanelPos();
			}
		}

		
	}

	private void HandleSolider(HexCell cell, int2 pos, bool isLocal)
	{
		if (!isLocal)
		{
			// 如果目标格子距离选中格子的距离大于1则不显示
			if (GetDistanceFurtherThanValue(1, cell)) return;

			MouseImage.sprite = UISpriteHelper.Instance.GetSubSprite(UISpriteID.MouseInteraction, "RightButtonClick");
			int cost = unitDataInterface.GetUnitOperationCostByType(GameData.OperationType.Attack);
			ShowPanel("攻撃： " + cost+" 行動力");
		}

		UpdatePanelPos();
	}

	private void ShowPanel(string msg)
	{
		// 关闭special button
		SpecialButton.gameObject.SetActive(false);
		// 打开文字面板
		OperationPanelText.gameObject.SetActive(true);
		OperationPanelText.text = msg;
		ActionPanelTransform.gameObject.SetActive(true);
	}

	private void ShowButtonPanel(string msg)
	{
		// 关闭文字面板
		OperationPanelText.gameObject.SetActive(false);
		
		// 显示special button
		SpecialButton.gameObject.SetActive(true);

		// 设定OperationPanel显示的内容
		TMP_Text text = SpecialButton.GetComponentInChildren<TMP_Text>();
		text.text = msg;

		// 显示OperationPanel可见
		ActionPanelTransform.gameObject.SetActive(true);
	}


	private void ShowMovePanelIfNeeded(HexCell cell)
	{
		// 教皇不能移动
		if (dataManager.nowChooseUnitType == CardType.Pope || dataManager.nowChooseUnitType == CardType.Building) 
			return;

		// 农民的移动有限制 无法移动到非己方格子 所以不会在 非己方格子显示移动面板
		int cellOwner = dataManager.GetCellOwner(cell.Index);
		if (cellOwner != GameManage.Instance.LocalPlayerID && dataManager.nowChooseUnitType == CardType.Farmer) 
		{
			return;
		}

		MouseImage.sprite = UISpriteHelper.Instance.GetSubSprite(UISpriteID.MouseInteraction, "RightButtonClick");
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

	private void UpdatePanelPos(HexCell cell = null, bool isSpecial = false)
	{
		if (ActionPanelTransform == null || uiCanvas == null)
			return;

		if (!isSpecial)
		{
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
		else
		{

			// 将格子位置转换为屏幕UI的位置
			if (cell == null)
				return;
			
			Vector3 cellWorldPos = cell.Position;
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
			anchoredPos = ClampToCanvas(anchoredPos, canvasRect, ActionPanelTransform);
			// 设定面板的位置
			ActionPanelTransform.anchoredPosition = anchoredPos;

		}

	}

	public void UpdateCostText()
	{
		// Init cost text
		GameData.Religion playerReligion = SceneStateManager.Instance.PlayerReligion;

		CostText[(int)BuyType.Missionary].text = unitDataInterface.GetCreateUnitResoursesCost(playerReligion, CardType.Missionary).ToString();
		CostText[(int)BuyType.Army].text = unitDataInterface.GetCreateUnitResoursesCost(playerReligion, CardType.Soldier).ToString();
		CostText[(int)BuyType.Farmer].text = unitDataInterface.GetCreateUnitResoursesCost(playerReligion, CardType.Farmer).ToString();
		CostText[(int)BuyType.Building].text =
			unitDataInterface.GetCreateUnitResoursesCost(playerReligion, CardType.Building).ToString();
	}

	private void HandleResourceUpdate()
	{
		// 每次资源更新都刷新一次花费显示
		ResourceText.text = dataManager.GetPlayerResource().ToString();
	}

	private bool GetDistanceFurtherThanValue(int value, HexCell targetCell)
	{
		// 做距离判断
		int selectCellId = GameManage.Instance._PlayerOperation.selectCellID;
		HexCell selectedCell = hexGrid.GetCell(selectCellId);
		int distance = selectedCell.Coordinates.DistanceTo(targetCell.Coordinates);
		// 如果距离大于指定值 则返回伪
		if (distance > value) return false;

		return true;
	}

	private void OnSpecialButtonClick()
	{
		GameManage.Instance._PlayerOperation.OnSpecialSkillButtonClick();
		ActionPanelTransform.gameObject.SetActive(false);
	}

	// *************************
	// 追加高亮选择处理(Right Click)
	// *************************
	//void UpdateRightClickHighlight(HexCell cell)
	//{
	//	if (cell == null)
	//	{
	//		Shader.SetGlobalVector(rightClickHighlightId, new Vector4(0, 0, -1, 0)); // 关闭
	//		return;
	//	}

	//	Shader.SetGlobalVector(
	//		rightClickHighlightId,
	//		new Vector4(
	//			cell.Coordinates.HexX,
	//			cell.Coordinates.HexZ,
	//			0.5f,
	//			HexMetrics.wrapSize
	//		)
	//	);
	//}
	// 清理右键高亮数据
	//void ClearRightClickCellHighlightData() =>
	//Shader.SetGlobalVector(rightClickHighlightId, new Vector4(0f, 0f, -1f, 0f));
}
