using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Unity.Mathematics;
using Newtonsoft.Json.Bson;
using UnityEngine.EventSystems;
using System.Dynamic;
using GameData;
using GamePieces;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;
using GameData.UI;
using Buildings;


#if UNITY_EDITORR
using static UnityEditor.PlayerSettings;
using Mono.Cecil;
#endif
using UnityEngine.Rendering.Universal;


/// <summary>
/// 玩家操作管理，负责处理因玩家操作导致的数据变动
/// </summary>
public class PlayerOperationManager : MonoBehaviour
{
    //2025.11.13 Guoning
    static readonly int cellHighlightingId = Shader.PropertyToID("_CellHighlighting");

    // HexGrid的引用
    public HexGrid _HexGrid;

    private Camera GameCamera;

    // 是否可进行操作
    private bool bCanContinue = true;

    // 是否是当前玩家的回合
    private bool isMyTurn = false;

    // 点击到的格子的id
    private int ClickCellid;


    // 射线检测指定为cell层级
    private int RayTestLayerMask = 1 << 6;

    // 当前选择的单位
    private GameObject SelectingUnit;

    // 上一次选择到的格子的id
    private int LastSelectingCellID;

    // 当前选择的空格子ID（用于创建单位）
    private int SelectedEmptyCellID = -1;

    // 本机玩家保存的棋盘信息
    private Dictionary<int, BoardInfor> PlayerBoardInforDict = new Dictionary<int, BoardInfor>();

    // 本地玩家数据
    private PlayerData localPlayerData;


    // 本地玩家的所有单位GameObject (key: 位置, value: GameObject)
    private Dictionary<int2, GameObject> localPlayerUnits = new Dictionary<int2, GameObject>();
    
    // 其他玩家的单位GameObject
    private Dictionary<int, Dictionary<int2, GameObject>> otherPlayersUnits = new Dictionary<int, Dictionary<int2, GameObject>>();

    // 建筑废墟id
    private int RuinID = 0;
    // 建筑废墟字典
    private Dictionary<int, Dictionary<int, GameObject>> BuildingRuins = new Dictionary<int, Dictionary<int, GameObject>>();
    
    // 格子list，检测移动范围用
    List<HexCell> HexCellList = new List<HexCell>();


    // 本地玩家ID
    private int localPlayerId = -1;

    private int selectCellID;


    // 保存攻击前的原始位置（用于"移动+攻击"场景）
    private int2? attackerOriginalPosition = null;

    // 双击检测
    // 定义双击的最大时间间隔
    public float doubleClickTimeThreshold = 0.1f;
    private float lastClickTime;
    private int clickCount = 0;

	//2025.11.28 检测右键长按
	public Image fillImage;
	private float longPressThreshold = 1.2f;
	private float rightClickTimer = 0f;
	private bool isPressing = false;




	// === Event 定义区域 ===
	public event System.Action<int, CardType> OnUnitChoosed;


    // 单位相关
    // 教皇移动冷却时间
    private int PopeSwatCooldown;


    // *************************
    //         公有属性
    // *************************

    // 移动速度
    public float MoveSpeed = 0.5f;

    // 玩家的id,由GameManage统一分配
    public int PlayerID
    {
        get { return localPlayerId; }
        private set { localPlayerId = value; }
    }


    // Start is called before the first frame update
    void Start()
    {
        GameCamera = GameObject.Find("GameCamera").GetComponent<Camera>();
    }


    // Update is called once per frame
    void Update()
    {
        // 鼠标判定
        if (GameManage.Instance.GetIsGamingOrNot() && isMyTurn)
        {
            // 2025.12.02 解决点击到UI时还有操作

            if (!EventSystem.current.IsPointerOverGameObject())
            {
                //2025.11.13 Guoning 修改鼠标移动留痕与点击判断
                if (Input.GetMouseButton(0) || Input.GetMouseButton(1) ||
                    Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1))
                    HandleMouseInput();
                else UpdateCellHighlightData(GetCellUnderCursor());
            }

			//HandleMouseInput();

		}


        //// 建造建筑
        //if (Input.GetKeyDown(KeyCode.B)
        //    && PlayerDataManager.Instance.GetPlayerData(localPlayerId).PlayerOwnedCells.Contains(SelectedEmptyCellID))
        //{
        //    CreateBuilding();
        //    //ClickBuildingCellid = ClickCellid;

        //}

        //// 测试建筑升级
        //if (Input.GetKeyDown(KeyCode.B)
        //    && PlayerDataManager.Instance.nowChooseUnitType == CardType.Building)
        //{
        //    UnitUpgrade(TechTree.HP,CardType.Building);
        //    //ClickBuildingCellid = ClickCellid;

        //}
        //// 测试红月被动
        //if (Input.GetKeyDown(KeyCode.R)
        //    && SceneStateManager.Instance.PlayerReligion==Religion.RedMoonReligion)
        //{
        //    PlayerDataManager.Instance.DeadUnitCount = 12 ;

        //    PlayerDataManager.Instance.bRedMoonSkill = true;

        //    for (int i = 0; i < PlayerDataManager.Instance.GetPlayerData(localPlayerId).PlayerUnits.Count; i++)
        //    {
        //        int hp =0;
        //        syncPieceData newData = new syncPieceData();
        //        if (PlayerDataManager.Instance.GetPlayerData(localPlayerId).PlayerUnits[i].UnitType!=CardType.Building)
        //        {
        //            hp = PieceManager.Instance.GetPieceAllHP(PlayerDataManager.Instance.GetPlayerData(localPlayerId).PlayerUnits[i].UnitID) / 5;
        //            if (hp == 0)
        //                hp += 1;
        //            newData = (syncPieceData)PieceManager.Instance.HealPiece(PlayerDataManager.Instance.GetPlayerData(localPlayerId).PlayerUnits[i].UnitID, hp);
                   
        //            PlayerUnitData unit = PlayerDataManager.Instance.GetPlayerData(localPlayerId).PlayerUnits[i];
        //            unit.PlayerUnitDataSO = newData;
        //            PlayerDataManager.Instance.GetPlayerData(localPlayerId).PlayerUnits[i] = unit;
                   
        //            Debug.Log( "Heal HP is " + hp);
        //        }
        //    }
        //    //ClickBuildingCellid = ClickCellid;

        //}


        // 农民献祭
        if (Input.GetKeyDown(KeyCode.T)
            && PlayerDataManager.Instance.nowChooseUnitType == CardType.Farmer)
        {
            FarmerSacrifice();
            //ClickBuildingCellid = ClickCellid;

        }

        if (Input.GetKeyDown(KeyCode.G) && PlayerDataManager.Instance.nowChooseUnitType == CardType.Missionary)
        {
            // 传教士占领
            // 通过PieceManager判断
            if (!PlayerDataManager.Instance.GetPlayerData(localPlayerId).PlayerOwnedCells.Contains(LastSelectingCellID)
                && _HexGrid.SearchCellRange(HexCellList, _HexGrid.GetCell(LastSelectingCellID), 1)
                && PieceManager.Instance.OccupyTerritory(PlayerDataManager.Instance.nowChooseUnitID, PlayerBoardInforDict[selectCellID].Cells3DPos))
            {

                _HexGrid.GetCell(LastSelectingCellID).Walled = true;
                PlayerDataManager.Instance.GetPlayerData(localPlayerId).AddOwnedCell(LastSelectingCellID);
                HexCellList.Add(_HexGrid.GetCell(LastSelectingCellID));

                // 2025.11.14 Guoning 音声再生
                SoundManager.Instance.PlaySE(SoundSystem.TYPE_SE.CHARMED);
            }
            else
            {
                Debug.Log("传教士 ID: " + PlayerDataManager.Instance.nowChooseUnitID + " 占领失败！");
            }
        }
    }

	// *************************
	//       追加高亮选择处理
	// *************************
	void UpdateCellHighlightData(HexCell cell)
    {


        if (cell == null)
        {
            ClearCellHighlightData();
            return;
        }

        // Works up to brush size 6.
        Shader.SetGlobalVector(
            cellHighlightingId,
            new Vector4(
                cell.Coordinates.HexX,
                cell.Coordinates.HexZ,
                0.5f,
                HexMetrics.wrapSize
            )
        );
    }

    void ClearCellHighlightData() =>
        Shader.SetGlobalVector(cellHighlightingId, new Vector4(0f, 0f, -1f, 0f));
    HexCell GetCellUnderCursor() =>
        _HexGrid.GetCell(Camera.main.ScreenPointToRay(Input.mousePosition));
    // *************************
    //       追加高亮选择处理
    // *************************


    // 初始化玩家
    public void InitPlayer(int playerId, int startBoardID)
    {
        localPlayerId = playerId;
        PlayerBoardInforDict = GameManage.Instance.GetPlayerBoardInfor();

        // 从PlayerDataManager获取数据
        localPlayerData = PlayerDataManager.Instance.GetPlayerData(playerId);

        Debug.Log($"PlayerOperationManager: 初始化玩家 {playerId}");



        // 创建玩家拥有的单位
        CreatePlayerPope(startBoardID);

        // 添加数据变化事件
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnUnitAdded += OnUnitAddedHandler;
            PlayerDataManager.Instance.OnUnitRemoved += OnUnitRemovedHandler;
            PlayerDataManager.Instance.OnUnitMoved += OnUnitMovedHandler;
        }

    }



    // *************************
    //        输入处理
    // *************************

    #region =====输入处理=====
    private void HandleMouseInput()
    {
        if (GameManage.Instance.IsPointerOverUIElement())
        {
            return;
        }

        // 2025.11.13 GuoNing 清除高亮数据 
        ClearCellHighlightData();

        // 左键点击 - 选择单位
        if (Input.GetMouseButtonDown(0) && bCanContinue)
        {
            _HexGrid.GetCell(selectCellID).DisableHighlight();

            // 检测鼠标左键点击
            if (Input.GetMouseButtonDown(0))
            {
                float timeSinceLastClick = Time.time - lastClickTime;

                if (timeSinceLastClick <= doubleClickTimeThreshold)
                {
                    // 第二次点击在阈值时间内，是双击
                    clickCount++;
                    if (clickCount == 2)
                    {
                        HandleLeftClick(true);

                        // 重置计数器
                        clickCount = 0;
                    }
                }
                else
                {
                    // 第一次点击或两次点击间隔太长
                    clickCount = 1;

                    HandleLeftClick(false);
                }

                lastClickTime = Time.time;
            }

        }

        //右键点击 - 移动 / 攻击
        //if (Input.GetMouseButtonDown(1) && bCanContinue)
        //{
        //    _HexGrid.GetCell(selectCellID).DisableHighlight();
        //    HandleRightClick();
        //}

        if (Input.GetMouseButtonDown(1) && bCanContinue)
        {
            isPressing = true;
            rightClickTimer = 0f;
            fillImage.gameObject.SetActive(true);
        }

        if (isPressing && Input.GetMouseButton(1) && bCanContinue)
        {
            rightClickTimer += Time.deltaTime;
            fillImage.fillAmount = Mathf.Clamp(rightClickTimer,0,longPressThreshold) / longPressThreshold;
		}


        // 松开
        if (isPressing && Input.GetMouseButtonUp(1) && bCanContinue)
		{
		
			if (rightClickTimer < longPressThreshold) OnRightClickShortPress();
			else OnRightClickLongPress();

			isPressing = false;
			rightClickTimer = 0f;
			fillImage.gameObject.SetActive(false);
		}
    }


    private void OnRightClickShortPress()
    {
     
		if (SelectingUnit == null) return;

		Ray ray = GameCamera.ScreenPointToRay(Input.mousePosition);
		RaycastHit hit;

		// 射线检测
		if (Physics.Raycast(ray, out hit, Mathf.Infinity, RayTestLayerMask))
		{
			ClickCellid = hit.collider.gameObject.GetComponent<HexCell>().Index;
			int2 targetPos = GameManage.Instance.FindCell(ClickCellid).Cells2DPos;

			// 获取当前选中单位的位置
			int2 currentPos = PlayerBoardInforDict[LastSelectingCellID].Cells2DPos;

			// 检查目标位置
			if (PlayerDataManager.Instance.IsPositionOccupied(targetPos))
			{
				int ownerId = PlayerDataManager.Instance.GetUnitOwner(targetPos);

                // Pope交换位置
                if (ownerId == localPlayerId &&
                    PlayerDataManager.Instance.nowChooseUnitType == CardType.Pope)
                {

                    PlayerUnitData? targetUnit = PlayerDataManager.Instance.FindUnit(ownerId, targetPos);
                    if (targetUnit.HasValue && !targetUnit.Value.IsBuilding())
                    {
                        if (!PieceManager.Instance.CanSwapPositions(PlayerDataManager.Instance.nowChooseUnitID, targetUnit.Value.UnitID))
                        {
                            Debug.Log("[Pope交换] 无法交换，尚未冷却");
                            return;
                        }
                        ExecutePopeSwapPosition(currentPos, targetPos, ClickCellid);
                        return;
                    }
                }

                // 农民进入己方建筑
                if (ownerId == localPlayerId && PlayerDataManager.Instance.nowChooseUnitType == CardType.Farmer)
				{
					// 检查目标位置是否是建筑
					PlayerUnitData? targetUnit = PlayerDataManager.Instance.FindUnit(ownerId, targetPos);
                    if (targetUnit.HasValue && targetUnit.Value.IsBuilding() &&
                          GameManage.Instance._BuildingManager.NewEnterBuilding(
                           PlayerDataManager.Instance.GetUnitIDBy2DPos(targetPos),
                           PieceManager.Instance.GetPieceAP(PlayerDataManager.Instance.nowChooseUnitID)
                          ))
                    {
                        Debug.Log("[农民进建筑] 农民尝试进入己方建筑");
						ExecuteFarmerEnterBuilding(currentPos, targetPos, ClickCellid);
						return;
					}
					else
					{

						Debug.LogWarning("[农民进建筑] 格子已满，无法进入!");
					}
				}

				// 传教士魅惑敌方单位
				if (ownerId != localPlayerId && PlayerDataManager.Instance.nowChooseUnitType == CardType.Missionary && IsAdjacentPosition(currentPos, targetPos))
				{
					Debug.Log("[魅惑] 传教士尝试魅惑敌方单位");

					// 检查AP（魅惑需要消耗AP）
					if (!CheckUnitHasEnoughAP(currentPos, 1))
					{
						Debug.Log("[魅惑] AP不足，无法魅惑");
						return;
					}

					ExecuteCharm(targetPos, ownerId);
					return;
				}

				// 军事单位攻击敌方单位
				if (ownerId != localPlayerId && PlayerDataManager.Instance.nowChooseUnitType == CardType.Soldier)
				{
					//  新逻辑：检查是否在攻击范围（相邻格）
					if (IsAdjacentPosition(currentPos, targetPos))
					{
						// 在攻击范围内，直接攻击
						Debug.Log("[攻击] 目标在攻击范围内，执行攻击");

						// 检查AP（攻击需要消耗1点AP）
						if (!CheckUnitHasEnoughAP(currentPos, 1))
						{
							Debug.Log("[攻击] AP不足，无法攻击");
							return;
						}

						ExecuteAttack(targetPos, ClickCellid);
					}
					else
					{
						// 不在攻击范围内，无法攻击也无法移动到敌方单位位置
						Debug.Log("[攻击] 目标不在相邻格，无法攻击。请先移动到敌方单位旁边再攻击。");
						return;
					}

				}
				else
				{
					Debug.Log("不能移动到已占用的位置");
				}
			}
			else
			{
				// 教皇无法移动
				if (PlayerDataManager.Instance.nowChooseUnitType == CardType.Pope)
				{
					return;
				}
				// 传教士移动
				if (PlayerDataManager.Instance.nowChooseUnitType == CardType.Missionary)
				{

					if (_HexGrid.SearchCellRange(HexCellList, _HexGrid.GetCell(targetPos.x, targetPos.y), 3))
					{
						MoveToSelectCell(ClickCellid);
					}
					else
					{
						Debug.LogWarning("Missionary  Cant Move To That Cell!");
					}

				}
				// 农民移动
				else if (PlayerDataManager.Instance.nowChooseUnitType == CardType.Farmer)
				{
					if (PlayerDataManager.Instance.GetPlayerData(localPlayerId).PlayerOwnedCells.Contains(ClickCellid))
					{
						MoveToSelectCell(ClickCellid);
					}
					else
					{
						Debug.LogWarning("Farmer  Cant Move To That Cell!");
					}

				}
				else
					MoveToSelectCell(ClickCellid);
			}
		}

	}

    private void OnRightClickLongPress()
    {
       
		if (PlayerDataManager.Instance.nowChooseUnitType == CardType.Farmer)
		{
			FarmerSacrifice();
			//ClickBuildingCellid = ClickCellid;

		}

		if (PlayerDataManager.Instance.nowChooseUnitType == CardType.Missionary)
		{
			// 传教士占领
			// 通过PieceManager判断
			if (!PlayerDataManager.Instance.GetPlayerData(localPlayerId).PlayerOwnedCells.Contains(LastSelectingCellID)
			    && _HexGrid.SearchCellRange(HexCellList, _HexGrid.GetCell(LastSelectingCellID), 1)
			    && PieceManager.Instance.OccupyTerritory(PlayerDataManager.Instance.nowChooseUnitID, PlayerBoardInforDict[selectCellID].Cells3DPos))
			{

				_HexGrid.GetCell(LastSelectingCellID).Walled = true;
				PlayerDataManager.Instance.GetPlayerData(localPlayerId).AddOwnedCell(LastSelectingCellID);
				HexCellList.Add(_HexGrid.GetCell(LastSelectingCellID));

				// 2025.11.14 Guoning 音声再生
				SoundManager.Instance.PlaySE(SoundSystem.TYPE_SE.CHARMED);
			}
			else
			{
				Debug.Log("传教士 ID: " + PlayerDataManager.Instance.nowChooseUnitID + " 占领失败！");
			}
		}
	}

	private void HandleLeftClick(bool isDoubleClick)
    {
        Ray ray = GameCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // 射线检测
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, RayTestLayerMask))
        {
            ClickCellid = hit.collider.gameObject.GetComponent<HexCell>().Index;
            int2 clickPos = GameManage.Instance.FindCell(ClickCellid).Cells2DPos;

            // 双击后聚焦
            if (isDoubleClick)
            {
                GameManage.Instance._GameCamera.GetPlayerPosition(GameManage.Instance.FindCell(ClickCellid).Cells3DPos);
            }

            // 检查是否点击了自己的单位
            if (localPlayerUnits.ContainsKey(clickPos))
            {
                // 取消之前的选择
                ReturnToDefault();
                SelectedEmptyCellID = -1;


                // 选择新单位
                SelectingUnit = localPlayerUnits[clickPos];
                foreach (Transform child in SelectingUnit.transform)
                {
                    // 运行时动态添加组件
                    if (child.gameObject.GetComponent<ChangeMaterial>() == null)
                    {
                        child.gameObject.AddComponent<ChangeMaterial>();
                        child.gameObject.GetComponent<ChangeMaterial>().OutlineMat = Resources.Load<Material>("RI/OutLineMat");
                        child.gameObject.GetComponent<ChangeMaterial>().InitMat();
                    }
                  
                }
            
                //SelectingUnit.GetComponent<ChangeMaterial>().Outline();
                LastSelectingCellID = ClickCellid;


                PlayerDataManager.Instance.nowChooseUnitID = PlayerDataManager.Instance.GetUnitIDBy2DPos(clickPos);
                PlayerDataManager.Instance.nowChooseUnitType = PlayerDataManager.Instance.GetUnitTypeIDBy2DPos(clickPos);

                OnUnitChoosed?.Invoke(PlayerDataManager.Instance.nowChooseUnitID, PlayerDataManager.Instance.nowChooseUnitType);

                foreach (Transform child in SelectingUnit.transform)
                {
                    
                    child.GetComponent<ChangeMaterial>().Outline();
                    //Debug.Log("add outline");
                }
                Debug.Log($"选择了单位 ID: {PlayerDataManager.Instance.nowChooseUnitID},{PlayerDataManager.Instance.nowChooseUnitType}");
            }

            else if (otherPlayersUnits.Count >= 1 && otherPlayersUnits[localPlayerId == 0 ? 1 : 0].ContainsKey(clickPos))
            {
                Debug.Log("Get Enemy Unit " + clickPos);
                PlayerUnitDataInterface.Instance.SetEnemyUnitPosition(clickPos);
            }
            else
            {
               //Debug.Log("owner Player is "+PlayerDataManager.Instance.GetCellOwner(ClickCellid));
                // 点击了空地或其他玩家单位
                ReturnToDefault();
                SelectingUnit = null;

                if (PlayerDataManager.Instance.nowChooseUnitType != CardType.Farmer)
                {
                    PlayerDataManager.Instance.nowChooseUnitID = -1;
                    PlayerDataManager.Instance.nowChooseUnitType = CardType.None;
                }

                // 检查是否是空格子
                if (!PlayerDataManager.Instance.IsPositionOccupied(clickPos) && _HexGrid.IsValidDestination(_HexGrid.GetCell(ClickCellid)))
                {
                    ChooseEmptyCell(ClickCellid);
                    selectCellID = ClickCellid;
                    SelectedEmptyCellID = ClickCellid; // 保存选中的空格子
                                                       //Debug.Log($"选择了空格子: {clickPos}，可以在此创建单位");

                }
                else
                {

                    SelectedEmptyCellID = -1; // 清除选择
                    Debug.Log("该格子无法创建单位");
                }
            }
        }
        else
        {
            ReturnToDefault();
            SelectingUnit = null;
            SelectedEmptyCellID = -1;
        }
    }
    //private void HandleRightClick()
    //{
    //    if (SelectingUnit == null) return;

    //    Ray ray = GameCamera.ScreenPointToRay(Input.mousePosition);
    //    RaycastHit hit;

    //    // 射线检测
    //    if (Physics.Raycast(ray, out hit, Mathf.Infinity, RayTestLayerMask))
    //    {
    //        ClickCellid = hit.collider.gameObject.GetComponent<HexCell>().Index;
    //        int2 targetPos = GameManage.Instance.FindCell(ClickCellid).Cells2DPos;

    //        // 获取当前选中单位的位置
    //        int2 currentPos = PlayerBoardInforDict[LastSelectingCellID].Cells2DPos;

    //        // 检查目标位置
    //        if (PlayerDataManager.Instance.IsPositionOccupied(targetPos))
    //        {
    //            int ownerId = PlayerDataManager.Instance.GetUnitOwner(targetPos);

    //            // Pope交换位置
    //            if (ownerId == localPlayerId &&
    //                PlayerDataManager.Instance.nowChooseUnitType == CardType.Pope)
    //            {
                  
    //                    PlayerUnitData? targetUnit = PlayerDataManager.Instance.FindUnit(ownerId, targetPos);
    //                    if (targetUnit.HasValue && !targetUnit.Value.IsBuilding())
    //                    {
    //                        if (!PieceManager.Instance.CanSwapPositions(PlayerDataManager.Instance.nowChooseUnitID, targetUnit.Value.UnitID))
    //                        {
    //                            Debug.Log("[Pope交换] 无法交换，尚未冷却");
    //                            return;
    //                        }
                           
    //                        ExecutePopeSwapPosition(currentPos, targetPos, ClickCellid);
    //                        return;
    //                    }



    //            }

    //            // 农民进入己方建筑
    //            if (ownerId == localPlayerId && PlayerDataManager.Instance.nowChooseUnitType == CardType.Farmer)
    //            {
    //                // 检查目标位置是否是建筑
    //                PlayerUnitData? targetUnit = PlayerDataManager.Instance.FindUnit(ownerId, targetPos);
    //                if (targetUnit.HasValue && targetUnit.Value.IsBuilding()&&
    //                      GameManage.Instance._BuildingManager.NewEnterBuilding(
    //                          PlayerDataManager.Instance.GetUnitIDBy2DPos(targetPos), 
    //                      PieceManager.Instance.GetPieceAP(PlayerDataManager.Instance.nowChooseUnitID)
    //                      ))
    //                {
                        
    //                    Debug.Log("[农民进建筑] 农民尝试进入己方建筑");
    //                    ExecuteFarmerEnterBuilding(currentPos, targetPos, ClickCellid);
    //                    return;
    //                }
    //                else
    //                {

    //                    Debug.LogWarning("[农民进建筑] 格子已满，无法进入!");
    //                }
    //            }

    //            // 传教士魅惑敌方单位
    //            if (ownerId != localPlayerId && PlayerDataManager.Instance.nowChooseUnitType == CardType.Missionary && IsAdjacentPosition(currentPos, targetPos))
    //            {
    //                Debug.Log("[魅惑] 传教士尝试魅惑敌方单位");

    //                // 检查AP（魅惑需要消耗AP）
    //                if (!CheckUnitHasEnoughAP(currentPos, 1))
    //                {
    //                    Debug.Log("[魅惑] AP不足，无法魅惑");
    //                    return;
    //                }

    //                ExecuteCharm(targetPos, ownerId);
    //                return;
    //            }

    //            // 军事单位攻击敌方单位
    //            if (ownerId != localPlayerId && PlayerDataManager.Instance.nowChooseUnitType == CardType.Solider)
    //            {
    //                //  新逻辑：检查是否在攻击范围（相邻格）
    //                if (IsAdjacentPosition(currentPos, targetPos))
    //                {
    //                    // 在攻击范围内，直接攻击
    //                    Debug.Log("[攻击] 目标在攻击范围内，执行攻击");

    //                    // 检查AP（攻击需要消耗1点AP）
    //                    if (!CheckUnitHasEnoughAP(currentPos, 1))
    //                    {
    //                        Debug.Log("[攻击] AP不足，无法攻击");
    //                        return;
    //                    }

    //                    ExecuteAttack(targetPos, ClickCellid);
    //                }
    //                else
    //                {
    //                    // 不在攻击范围内，无法攻击也无法移动到敌方单位位置
    //                    Debug.Log("[攻击] 目标不在相邻格，无法攻击。请先移动到敌方单位旁边再攻击。");
    //                    return;
    //                }

    //            }
    //            else
    //            {
    //                Debug.Log("不能移动到已占用的位置");
    //            }
    //        }
    //        else
    //        {
    //            // 教皇无法移动
    //            if (PlayerDataManager.Instance.nowChooseUnitType == CardType.Pope)
    //            {
    //                return;
    //            }
    //            // 传教士移动
    //            if (PlayerDataManager.Instance.nowChooseUnitType == CardType.Missionary)
    //            {

    //                if (_HexGrid.SearchCellRange(HexCellList, _HexGrid.GetCell(targetPos.x, targetPos.y), 3))
    //                {
    //                    MoveToSelectCell(ClickCellid);
    //                }
    //                else
    //                {
    //                    Debug.LogWarning("Missionary  Cant Move To That Cell!");
    //                }

    //            }
    //            // 农民移动
    //            else if (PlayerDataManager.Instance.nowChooseUnitType == CardType.Farmer)
    //            {
    //                if (PlayerDataManager.Instance.GetPlayerData(localPlayerId).PlayerOwnedCells.Contains(ClickCellid))
    //                {
    //                    MoveToSelectCell(ClickCellid);
    //                }
    //                else
    //                {
    //                    Debug.LogWarning("Farmer  Cant Move To That Cell!");
    //                }

    //            }
    //            else
    //                MoveToSelectCell(ClickCellid);
    //        }
    //    }
    //}

    // 返回当前摄像机聚焦的单位id
    public int GetFocusedUnitID()
    {
        return 0;
    }
    #endregion
    // *************************
    //         公有函数
    // *************************



    // 显示当前农民所在位置，等待点选后消耗行动力创建建筑
    private void ShowBuildingPos()
    {

    }
    // 得到建造完成消息
    public void GetBuildingOver()
    {

    }





    // *************************
    //        回合相关
    // *************************

    #region ====回合管理====
    // 回合开始
    public void TurnStart()
    {

        isMyTurn = true;
        bCanContinue = true;

        PieceManager.Instance.ProcessTurnStart(localPlayerId);
      

        // 获取建筑资源
        int res = PlayerDataManager.Instance.GetPlayerData(localPlayerId).Resources;
        res += GameManage.Instance._BuildingManager.ProcessTurnStart();
        PlayerDataManager.Instance.SetPlayerResourses(res);
        PlayerUnitDataInterface.Instance.GetPopeSwapCooldown();
        Debug.Log("你的回合开始!获取资源行动 " + res+" 目前资源: " + PlayerDataManager.Instance.GetPlayerData(localPlayerId).Resources);

        List<PlayerUnitData> buildingsToDestroy = new List<PlayerUnitData>();

        foreach (var unit in PlayerDataManager.Instance.GetPlayerData(localPlayerId).PlayerUnits)
        {
            unit.SetCanDoAction(true);
            Debug.Log("你的回合开始!重置行动！" + "unit name is " + unit.UnitID + "unit type is " + unit.UnitType + " canDo is " + unit.bCanDoAction + " Resource is " + PlayerDataManager.Instance.GetPlayerData(localPlayerId).Resources);
            if (unit.UnitType == CardType.Building)
            {
                bool actived = GameManage.Instance._BuildingManager.GetIsActived(unit.UnitID);
                if (!actived)
                {
                    buildingsToDestroy.Add(unit);
                }
                //Debug.Log("THIS BUILDING ACTIVED IS " + GameManage.Instance._BuildingManager.GetIsActived(unit.UnitID));
            }
        }

        // 步骤2: 遍历完成后,再统一销毁
        foreach (var building in buildingsToDestroy)
        {
            DestroyInactivatedBuilding(building);  // 现在可以安全删除了
        }

       

        // 回合开始计算被动
        if (SceneStateManager.Instance.PlayerReligion==Religion.RedMoonReligion
            &&PlayerDataManager.Instance.bRedMoonSkill)
        {
            PlayerDataManager.Instance.RedMoonSkillCount +=1;
            if (PlayerDataManager.Instance.RedMoonSkillCount >= 3)
            {
                PlayerDataManager.Instance.RedMoonSkillCount = 0;
                PlayerDataManager.Instance.bRedMoonSkill = false;
            }
        }

    }

    // 回合结束
    public void TurnEnd()
    {
        if (!isMyTurn)
        {
            Debug.LogWarning("不是你的回合!");
            return;
        }

        isMyTurn = false;
        bCanContinue = false;

        // 取消当前选择
        _HexGrid.GetCell(selectCellID).DisableHighlight();
        ReturnToDefault();
        SelectingUnit = null;
        SelectedEmptyCellID = -1;

        // 更新NowChoose
        PlayerDataManager.Instance.nowChooseUnitID = -1;
        PlayerDataManager.Instance.nowChooseUnitType = CardType.None;


        //Debug.Log("你的回合结束!");

        // 更新本地玩家数据到PlayerDataManager
        localPlayerData = PlayerDataManager.Instance.GetPlayerData(localPlayerId);

        // 通知GameManage结束回合
        GameManage.Instance.EndTurn();
    }

    // 其他玩家回合禁用输入
    public void DisableInput()
    {
        isMyTurn = false;
        bCanContinue = false;

        // 取消选择
        ReturnToDefault();
        SelectingUnit = null;
        SelectedEmptyCellID = -1;

        Debug.Log("等待其他玩家...");
    }

    // 更新其他玩家的显示
    public void UpdateOtherPlayerDisplay(int playerId, PlayerData data)
    {
        if (playerId == localPlayerId)
        {
            Debug.Log($"playerId 为自己");
            return;
        }

        Debug.LogWarning($"更新玩家 {playerId} 的显示");

        // 如果没有这个玩家的字典,创建一个
        if (!otherPlayersUnits.ContainsKey(playerId))
        {
            otherPlayersUnits[playerId] = new Dictionary<int2, GameObject>();
        }

        // ===== 修复1：更新领地显示 =====
        if (data.PlayerOwnedCells != null && data.PlayerOwnedCells.Count > 0)
        {
            Debug.Log($"[显示更新] 玩家 {playerId} 拥有 {data.PlayerOwnedCells.Count} 个格子");
            foreach (int cellId in data.PlayerOwnedCells)
            {
                if (_HexGrid.GetCell(cellId) != null)
                {
                    _HexGrid.GetCell(cellId).Walled = true;

                   
                }
            }
        }

        // ===== 修复2：清理不存在的单位 =====
        // 创建一个包含所有当前应该存在的位置的集合
        HashSet<int2> currentPositions = new HashSet<int2>();
        foreach (var unit in data.PlayerUnits)
        {
            currentPositions.Add(unit.Position);
        }

        // 找出并删除不应该存在的GameObject
        List<int2> positionsToRemove = new List<int2>();
        foreach (var kvp in otherPlayersUnits[playerId])
        {
            int2 pos = kvp.Key;
            if (!currentPositions.Contains(pos))
            {
                // 这个位置的单位不应该存在，标记删除
                positionsToRemove.Add(pos);
                Debug.Log($"[显示更新] 标记删除过时的单位: ({pos.x},{pos.y})");
            }
        }

        // 执行删除
        foreach (int2 pos in positionsToRemove)
        {
            GameObject oldUnit = otherPlayersUnits[playerId][pos];
            if (oldUnit != null)
            {
                Destroy(oldUnit);
                Debug.Log($"[显示更新] 销毁过时的GameObject at ({pos.x},{pos.y})");
            }
            otherPlayersUnits[playerId].Remove(pos);
            GameManage.Instance.SetCellObject(pos, null);
        }

        // ===== 修复3：更新或创建单位 =====
        for (int i = 0; i < data.PlayerUnits.Count; i++)
        {
            PlayerUnitData unit = data.PlayerUnits[i];

            // 检查该位置是否已有GameObject
            if (otherPlayersUnits[playerId].ContainsKey(unit.Position))
            {
                // 已存在，但需要验证是否是正确的单位
                GameObject existingUnit = otherPlayersUnits[playerId][unit.Position];

                // 可以通过名称或其他方式验证是否是同一个单位
                // 这里简单处理：如果位置已有单位，就跳过
                Debug.Log($"[显示更新] 单位已存在于 ({unit.Position.x},{unit.Position.y})，跳过创建");
                continue;
            }

            // 位置上没有GameObject，创建新的
            Debug.LogWarning($"[显示更新] 创建敌方单位: {unit.PlayerUnitDataSO.piecetype} at ({unit.Position.x},{unit.Position.y}) player ID:{unit.PlayerUnitDataSO.currentPID} unit ID:{unit.PlayerUnitDataSO.pieceID}");

            CreateEnemyUnit(playerId, unit);

		
		}

        Debug.Log($"[显示更新] 玩家 {playerId} 显示更新完成，当前单位数: {otherPlayersUnits[playerId].Count}");
    }


    #endregion
    // *************************
    //        私有函数
    // *************************

    // 获得初始领地
    private void GetStartWall(int cellID)
    {
        List<int> pos = GameManage.Instance.GetBoardNineSquareGrid(cellID, true);
        foreach (var i in pos)
        {
            if (_HexGrid.GetCell(i).enabled)
                _HexGrid.GetCell(i).Walled = true;

            PlayerDataManager.Instance.GetPlayerData(localPlayerId).AddOwnedCell(i);
        }
    }

    // 在指定的格子创建单位实例
    private void CreateUnitAtPosition(CardType unitType, int cellId)
    {
        BoardInfor cellInfo = GameManage.Instance.GetBoardInfor(cellId);
        int2 position = cellInfo.Cells2DPos;
        Vector3 worldPos = cellInfo.Cells3DPos;



        // 选择对应的预制体
        //Piece prefab = null;
        PieceType pieceType = PieceType.None;
        switch (unitType)
        {
            case CardType.Farmer:
                pieceType = PieceType.Farmer;
                PlayerDataManager.Instance.NowPopulation +=
                    PieceManager.Instance.GetPiecePopulationCost(PieceType.Farmer,SceneStateManager.Instance.PlayerReligion);
           

                break;
            case CardType.Soldier:
                pieceType = PieceType.Military;
                PlayerDataManager.Instance.NowPopulation +=
                 PieceManager.Instance.GetPiecePopulationCost(PieceType.Military, SceneStateManager.Instance.PlayerReligion);

             

                break;
            case CardType.Missionary:
                pieceType = PieceType.Missionary;
                PlayerDataManager.Instance.NowPopulation +=
               PieceManager.Instance.GetPiecePopulationCost(PieceType.Missionary, SceneStateManager.Instance.PlayerReligion);
                break;
            case CardType.Pope:
                pieceType = PieceType.Pope; 
                PlayerDataManager.Instance.NowPopulation +=
               PieceManager.Instance.GetPiecePopulationCost(PieceType.Pope, SceneStateManager.Instance.PlayerReligion);
             
                GetStartWall(cellId);

                // init Hex Cell List
                for (int i = 0; i < PlayerDataManager.Instance.GetPlayerData(localPlayerId).PlayerOwnedCells.Count; i++)
                {
                    HexCellList.Add(_HexGrid.GetCell(PlayerDataManager.Instance.GetPlayerData(localPlayerId).PlayerOwnedCells[i]));
                }

                break;
            case CardType.Building:
                // 建筑使用独有逻辑
                CreateBuilding();

                return;
            default:
                Debug.LogError($"未知的单位类型: {unitType}");
                return;
        }

        syncPieceData unitData = (syncPieceData)PieceManager.Instance.CreatePiece(pieceType,
        SceneStateManager.Instance.PlayerReligion,
        GameManage.Instance.LocalPlayerID,
        PlayerDataManager.Instance.GenerateUnitID(),
        worldPos);


        GameObject pieceObj = PieceManager.Instance.GetPieceGameObject();

        // 添加描边效果
        //pieceObj.AddComponent<ChangeMaterial>();


        // 添加到数据管理器
        // 2025.11.26 Guoning 追加HP显示
        //PlayerDataManager.Instance.AddUnit(localPlayerId, unitType, position, unitData);
        // 生成ID，创建单位
        int unitID = PlayerDataManager.Instance.AddUnit(localPlayerId, unitType, position, unitData);
        // 生成StatusUI
        UnitStatusUIManager.Instance.CreateStatusUI(unitID, unitData.currentHP,unitData.currentAP, pieceObj.transform, unitType);

		// 保存本地引用
		localPlayerUnits[position] = pieceObj;
        GameManage.Instance.SetCellObject(position, pieceObj);

        Debug.Log($"在ID:  ({cellId}) 创建了 {unitType}");


        // 发送网络消息
        if (NetGameSystem.Instance != null)
        {
            NetGameSystem.Instance.SendUnitAddMessage(
                localPlayerId,
                unitType,
                position,
                unitData,
                false
            );
        }
        //PlayerDataManager.Instance.GetUnitPos(unitData.pieceID);
    }

    // *************************
    //         单位相关
    // *************************

    #region ====创建单位相关====
    // 创建玩家教皇
    private void CreatePlayerPope(int startBoardID)
    {
        // 清空现有单位
        foreach (var a in localPlayerUnits.Values)
        {
            if (a != null)
                Destroy(a);
        }
        localPlayerUnits.Clear();

        CreateUnitAtPosition(CardType.Pope, startBoardID);
        PopeSwatCooldown = PlayerDataManager.Instance.GetPlayerData(localPlayerId).PlayerUnits[0].PlayerUnitDataSO.swapCooldownLevel;

        GameManage.Instance._GameCamera.GetPlayerPosition(GameManage.Instance.FindCell(startBoardID).Cells3DPos);

    }



    // 尝试在当前选中的空格子创建单位
    public bool TryCreateUnit(CardType unitType)
    {
        // 检查是否选中了空格子或领土内
        if (SelectedEmptyCellID == -1 || !_HexGrid.GetCell(SelectedEmptyCellID).Walled)
        {
            Debug.LogWarning("未选中任何空格子");
            return false;
        }

        // 获取选中格子的信息
        BoardInfor cellInfo = GameManage.Instance.GetBoardInfor(SelectedEmptyCellID);
        int2 cellPos = cellInfo.Cells2DPos;

        // 再次确认该位置是空的
        if (PlayerDataManager.Instance.IsPositionOccupied(cellPos))
        {
            Debug.LogWarning("该格子已有单位");
            SelectedEmptyCellID = -1;
            return false;
        }

        // 创建单位

        CreateUnitAtPosition(unitType, SelectedEmptyCellID);

        // 清除选择
        _HexGrid.GetCell(SelectedEmptyCellID).DisableHighlight();
        SelectedEmptyCellID = -1;

        return true;
    }

    // 创建敌方单位
    private void CreateEnemyUnit(int playerId, PlayerUnitData unitData)
    {
        if (!PlayerBoardInforDict.ContainsKey(0))
        {
            Debug.LogWarning("棋盘信息未初始化");
            return;
        }

        // 找到对应位置的世界坐标
        Vector3 worldPos = Vector3.zero;
        foreach (var board in PlayerBoardInforDict.Values)
        {
            if (board.Cells2DPos.Equals(unitData.Position))
            {
                worldPos = board.Cells3DPos;
                break;
            }
        }

        GameObject unit = null;

        // 检查是否为建筑单位
        if (unitData.IsBuilding() && unitData.BuildingData.HasValue)
        {
            // 这是建筑单位
            syncBuildingData buildingData = unitData.BuildingData.Value;
            Debug.Log($"创建敌方建筑: 玩家{playerId}, 建筑ID={buildingData.buildingID}, Name={buildingData.buildingName}");

            // 使用BuildingManager创建敌方建筑
            bool success = GameManage.Instance._BuildingManager.CreateEnemyBuilding(buildingData);
            if (success)
            {
                // 获取建筑GameObject（需要BuildingManager提供获取方法）
                // 如果BuildingManager没有提供，可以通过位置查找
                unit = GameManage.Instance._BuildingManager.GetBuildingGameObject();
                if (GameManage.Instance._BuildingManager.GetBuildingGameObject() == null)
                {
                    Debug.LogWarning($"无法获取建筑GameObject: BuildingID={buildingData.buildingID}");
                    // 创建一个占位符
                    unit = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    unit.transform.position = worldPos;
                }
                // 创建UI
                UnitStatusUIManager.Instance.CreateStatusUI(unitData.PlayerUnitDataSO.pieceID, unitData.PlayerUnitDataSO.currentHP, 0, transform, unitData.UnitType);
				Debug.Log($"敌方建筑创建成功");
            }
            else
            {
                Debug.LogError("创建敌方建筑失败！");
                return;
            }
        }
        else
        {

            if (unitData.PlayerUnitDataSO.pieceID == 0)
            {
                Debug.Log("创建失败！ syncPieceData为空！");
            }
            Debug.Log("创建敌方单位 :玩家 " + playerId + " 单位: " + unitData.PlayerUnitDataSO.piecetype);
            // 选择预制体
            PieceManager.Instance.CreateEnemyPiece(unitData.PlayerUnitDataSO);

            GameObject prefab = PieceManager.Instance.GetPieceGameObject();
            if (prefab == null)
                prefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            unit = Instantiate(prefab, worldPos, prefab.transform.rotation);
            unit.transform.position = new Vector3(
                unit.transform.position.x,
                unit.transform.position.y ,
                unit.transform.position.z
            );



            // 保存引用
            if (!otherPlayersUnits.ContainsKey(playerId))
            {
                otherPlayersUnits[playerId] = new Dictionary<int2, GameObject>();
            }
            otherPlayersUnits[playerId][unitData.Position] = unit;
            GameManage.Instance.SetCellObject(unitData.Position, unit);

			//Debug.Log("开始查询敌方单位位置");
			//PlayerDataManager.Instance.GetUnitPos(unitData.UnitID);
			// 创建UI
			UnitStatusUIManager.Instance.CreateStatusUI(unitData.PlayerUnitDataSO.pieceID, unitData.PlayerUnitDataSO.currentHP, 0, transform, unitData.UnitType);

		}
    }

    #endregion


    // 单位使用技能
    public void UnitUseSkill(CardType type)
    {
        switch (type)
        {
            case CardType.Farmer:

                break;

            case CardType.Missionary:

                break;


        }

    }
   


	// 单位升级
	public bool UnitUpgrade(TechTree tech, CardType type)
    {
        Debug.Log("进行升级: 科技树: " + tech + " 单位种类: " + type);
        List<PlayerUnitData> list = PlayerDataManager.Instance.GetPlayerData(localPlayerId).PlayerUnits;
        syncPieceData newData = new syncPieceData();
        syncBuildingData newBuildingData = new syncBuildingData();
        List<int> ID = new List<int>();
        for (int i=0;i<list.Count;i++)
        {
            ID.Add(list[i].UnitID);
        }
        switch (tech)
        {
            case TechTree.HP:
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].UnitType != CardType.Building && list[i].UnitType == type)
                    {
                         newData = (syncPieceData)PieceManager.Instance.UpgradePiece(
                            PlayerDataManager.Instance.nowChooseUnitID, PieceUpgradeType.HP);
                        break;
                    }
                    else if (list[i].UnitType == CardType.Building)
                    {
                        // Building Upgrade
                        if(GameManage.Instance._BuildingManager.UpgradeBuilding(PlayerDataManager.Instance.nowChooseUnitID, BuildingUpgradeType.BuildingHP))
                        {

                            newBuildingData = (syncBuildingData)GameManage.Instance._BuildingManager.CreateCompleteSyncData(
                            PlayerDataManager.Instance.nowChooseUnitID);
                            Debug.LogWarning("Building ID : " + newBuildingData.buildingID + " Upgrade HP! "+ newBuildingData.currentHP);

                            break;

                        }
                        else
                        {
                            Debug.LogWarning("Building ID : "+ PlayerDataManager.Instance.nowChooseUnitID+" Upgrade Failed!");
                      
                        }

                    }
                }

                for (int j = 0; j < list.Count; j++)
                {
                    if (list[j].UnitType != CardType.Building && list[j].UnitType == type)
                    {
                        PlayerUnitData unit = list[j];
                        unit.PlayerUnitDataSO = newData;
                        unit.PlayerUnitDataSO.pieceID = ID[j];
                        list[j] = unit;
                        Debug.Log("j= "+j+"Upgrade after Unit ID is " + list[j].PlayerUnitDataSO.pieceID +
                            " dataSO HP is " + list[j].PlayerUnitDataSO.currentHPLevel);
                    }
                    else if (list[j].UnitType == CardType.Building)
                    {
                        PlayerUnitData unit = list[j];
                        unit.BuildingData= newBuildingData;
                        unit.PlayerUnitDataSO.pieceID = ID[j];
                        list[j] = unit;
                        Debug.Log("j= " + j + "Upgrade after Unit ID is " + list[j].PlayerUnitDataSO.pieceID +
                            " dataSO HP is " + list[j].BuildingData.Value.currentHP);

                    }
                }
                   

                return true;
            case TechTree.AP:
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].UnitType != CardType.Building && list[i].UnitType == type)
                    {
                        newData = (syncPieceData)PieceManager.Instance.UpgradePiece(
                           PlayerDataManager.Instance.nowChooseUnitID, PieceUpgradeType.AP);
                        break;
                    }
                    else
                    {
                        //list[i].SetBuildingUnitDataSO((syncBuildingData)GameManage.Instance._BuildingManager.UpgradeBuilding(PlayerDataManager.Instance.nowChooseUnitID, BuildingUpgradeType.HP));

                    }
                }

                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].UnitType != CardType.Building && list[i].UnitType == type)
                    {
                        PlayerUnitData unit = list[i];
                        unit.PlayerUnitDataSO = newData;
                        unit.PlayerUnitDataSO.pieceID = ID[i];
                        list[i] = unit;
                        Debug.Log("j= " + i + " Upgrade after Unit ID is " + PlayerDataManager.Instance.GetPlayerData(localPlayerId).PlayerUnits[i].PlayerUnitDataSO.pieceID +
                        " dataSO AP is " + PlayerDataManager.Instance.GetPlayerData(localPlayerId).PlayerUnits[i].PlayerUnitDataSO.currentHPLevel);

                    }
                    else
                    {

                    }
                }
                //PieceManager.Instance.UpgradePiece(PlayerDataManager.Instance.nowChooseUnitID, PieceUpgradeType.AP);
                return true;
            case TechTree.Occupy:
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].UnitType == CardType.Missionary)
                    {
                        newData = (syncPieceData)PieceManager.Instance.UpgradePieceSpecial(
                            PlayerDataManager.Instance.nowChooseUnitID, SpecialUpgradeType.MissionaryOccupy);
                        break;
                       
                    }
                }


                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].UnitType != CardType.Building && list[i].UnitType == type)
                    {
                        PlayerUnitData unit = list[i];
                        unit.PlayerUnitDataSO = newData;
                        unit.PlayerUnitDataSO.pieceID = ID[i];
                        list[i] = unit;
                      
                    }
                    else
                    {

                    }
                }

                return true;
            case TechTree.Conversion:
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].UnitType == CardType.Missionary)
                    {
                        newData = (syncPieceData)PieceManager.Instance.UpgradePieceSpecial(
                            PlayerDataManager.Instance.nowChooseUnitID, SpecialUpgradeType.MissionaryConvertEnemy);
                        break;
                    }
                }

                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].UnitType == CardType.Missionary)
                    {
                        PlayerUnitData unit = list[i];
                        unit.PlayerUnitDataSO = newData;
                        unit.PlayerUnitDataSO.pieceID = ID[i];
                        list[i] = unit;
                    }
                 
                }

                return true;
            case TechTree.ATK:
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].UnitType == CardType.Soldier)
                    {
                        newData = (syncPieceData)PieceManager.Instance.UpgradePieceSpecial(
                            PlayerDataManager.Instance.nowChooseUnitID, SpecialUpgradeType.MilitaryAttackPower);
                        break;
                    }
                }

                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].UnitType == CardType.Soldier)
                    {
                        PlayerUnitData unit = list[i];
                        unit.PlayerUnitDataSO = newData;
                        unit.PlayerUnitDataSO.pieceID = ID[i];
                        list[i] = unit;
                    }

                }
                return true;
            case TechTree.Sacrifice:
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].UnitType == CardType.Farmer)
                    {
                        newData = (syncPieceData)PieceManager.Instance.UpgradePieceSpecial(
                            PlayerDataManager.Instance.nowChooseUnitID, SpecialUpgradeType.FarmerSacrifice);
                        break;
                     
                    }
                }
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].UnitType == CardType.Farmer)
                    {
                        PlayerUnitData unit = list[i];
                        unit.PlayerUnitDataSO = newData;
                        unit.PlayerUnitDataSO.pieceID = ID[i];
                        list[i] = unit;
                    }

                }
                return true;
            case TechTree.AttackPosition:

                for (int i = 0; i < list.Count; i++)
                {
                     if (list[i].UnitType == CardType.Building)
                    {
                        // Building Upgrade
                        if (GameManage.Instance._BuildingManager.UpgradeBuilding(PlayerDataManager.Instance.nowChooseUnitID, BuildingUpgradeType.attackRange))
                        {
                            newBuildingData = (syncBuildingData)GameManage.Instance._BuildingManager.CreateCompleteSyncData(
                            PlayerDataManager.Instance.nowChooseUnitID);
                            break;

                        }
                        else
                        {
                            Debug.LogWarning("Building ID : " + PlayerDataManager.Instance.nowChooseUnitID + " Upgrade Failed!");

                        }

                    }
                }

                for (int j = 0; j < list.Count; j++)
                {
                    if (list[j].UnitType == CardType.Building)
                    {
                        PlayerUnitData unit = list[j];
                        newBuildingData.buildingID = ID[j];
                        unit.BuildingData = newBuildingData;
                        unit.PlayerUnitDataSO.pieceID = ID[j];
                        list[j] = unit;
                        Debug.Log("j= " + j + "Upgrade after Unit ID is " + list[j].PlayerUnitDataSO.pieceID +
                            " dataSO AttackPosition is " + list[j].BuildingData.Value.attackRangeLevel);

                    }
                }

                return false;
            case TechTree.AltarCount:

                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].UnitType == CardType.Building)
                    {
                        // Building Upgrade
                        if (GameManage.Instance._BuildingManager.UpgradeBuilding(PlayerDataManager.Instance.nowChooseUnitID, BuildingUpgradeType.slotsLevel))
                        {
                            newBuildingData = (syncBuildingData)GameManage.Instance._BuildingManager.CreateCompleteSyncData(
                            PlayerDataManager.Instance.nowChooseUnitID);
                            break;

                        }
                        else
                        {
                            Debug.LogWarning("Building ID : " + PlayerDataManager.Instance.nowChooseUnitID + " Upgrade Failed!");

                        }

                    }
                }

                for (int j = 0; j < list.Count; j++)
                {
                    if (list[j].UnitType == CardType.Building)
                    {
                        PlayerUnitData unit = list[j];
                        newBuildingData.buildingID = ID[j];
                        unit.BuildingData = newBuildingData;
                        unit.PlayerUnitDataSO.pieceID = ID[j];
                        list[j] = unit;
                        Debug.Log("j= " + j + "Upgrade after Unit ID is " + list[j].PlayerUnitDataSO.pieceID +
                            " dataSO AttackPosition is " + list[j].BuildingData.Value.slotsLevel);

                    }
                }
                return true;
            case TechTree.ConstructionCost:
                GameManage.Instance._BuildingManager.UpgradeBuilding(PlayerDataManager.Instance.nowChooseUnitID, BuildingUpgradeType.BuildingHP);
                return true;
            case TechTree.MovementCD:
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].UnitType == CardType.Pope)
                    {
                        newData = (syncPieceData)PieceManager.Instance.UpgradePieceSpecial(
                            PlayerDataManager.Instance.nowChooseUnitID, SpecialUpgradeType.PopeSwapCooldown);
                        break;
                    }
                }
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].UnitType == CardType.Pope)
                    {
                        PlayerUnitData unit = list[i];
                        unit.PlayerUnitDataSO = newData;
                        unit.PlayerUnitDataSO.pieceID = ID[i];
                        list[i] = unit;
                    }

                }
                return true;
            case TechTree.Buff:
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].UnitType == CardType.Pope)
                    {
                        newData = (syncPieceData)PieceManager.Instance.UpgradePieceSpecial(
                            PlayerDataManager.Instance.nowChooseUnitID, SpecialUpgradeType.PopeBuff);
                        break;
                   
                    }
                }
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].UnitType == CardType.Pope)
                    {
                        PlayerUnitData unit = list[i];
                        unit.PlayerUnitDataSO = newData;
                        unit.PlayerUnitDataSO.pieceID = ID[i];
                        list[i] = unit;
                    }

                }
                return true;
            //case TechTree.Heresy:
            //    PieceManager.Instance.UpgradePieceSpecial(PlayerDataManager.Instance.nowChooseUnitID, PieceUpgradeType.AP);

            default:
                return false;
        }
    }




    // 取消选择单位的描边
    private void ReturnToDefault()
    {
        if (SelectingUnit != null)
        {
            foreach (Transform child in SelectingUnit.transform)
            {

                child.GetComponent<ChangeMaterial>().Default();
            }


            //var changeMaterial = SelectingUnit.GetComponent<ChangeMaterial>();

            //if (changeMaterial != null)
            //{
            //    changeMaterial.Default();
            //}
            Debug.Log("unit name: " + SelectingUnit.name);
        }
    }
    private void ChooseEmptyCell(int cell)
    {
        _HexGrid.GetCell(cell).EnableHighlight(Color.red);
    }

    // *************************
    //      单位动作相关
    // *************************


    // 移动到选择的棋盘
    private void MoveToSelectCell(int targetCellId)
    {
        if (SelectingUnit == null) return;

        bCanContinue = false;

        // 获取起始和目标位置
        int2 fromPos = PlayerBoardInforDict[LastSelectingCellID].Cells2DPos;
        int2 toPos = PlayerBoardInforDict[targetCellId].Cells2DPos;

        PlayerUnitData? unitData = PlayerDataManager.Instance.FindUnit(localPlayerId, fromPos);
        Debug.Log("now unit " + unitData.Value.UnitID + "AP is" + PieceManager.Instance.GetPieceAP(unitData.Value.UnitID));
       
        if (PlayerDataManager.Instance.nowChooseUnitType!=CardType.Farmer&&PieceManager.Instance.GetPieceAP(unitData.Value.UnitID) > 0)
        {
            _HexGrid.FindPath(LastSelectingCellID, targetCellId, PieceManager.Instance.GetPieceAP(unitData.Value.UnitID));
        }
        else if (PlayerDataManager.Instance.nowChooseUnitType == CardType.Farmer)
        {
            _HexGrid.FindPath(LastSelectingCellID, targetCellId, PieceManager.Instance.GetPieceAP(unitData.Value.UnitID));
        }
        else
        {
            Debug.Log("该单位AP不足！");
            bCanContinue = true;
            return;
        }

        if (_HexGrid.HasPath)
        {
            List<HexCell> listCellPos = _HexGrid.GetPathCells();
            if (listCellPos.Count - 1 > PieceManager.Instance.GetPieceAP(unitData.Value.UnitID))
            {
                Debug.Log("AP不足");
                _HexGrid.ClearPath();
                bCanContinue = true;
                return;
            }
            Sequence moveSequence = DOTween.Sequence();
            Vector3 currentPos = SelectingUnit.transform.position;
            for (int i = 0; i < listCellPos.Count; i++)
            {
                // 根据路径坐标找到对应的格子信息
                Vector3 waypoint = new Vector3(
                   listCellPos[i].Position.x,
                   listCellPos[i].Position.y,
                    listCellPos[i].Position.z
                    );

                // 计算弧形路径的中间点
                Vector3 midPoint = (currentPos + waypoint) / 2f;
                midPoint.y += 5.0f;

                // 创建从当前位置到目标位置的弧形路径
                Vector3[] path = new Vector3[] { currentPos, midPoint, waypoint };

                moveSequence.Append(SelectingUnit.transform.DOPath(path, MoveSpeed, PathType.CatmullRom)
                  .SetEase(Ease.Linear));
                currentPos = waypoint;
                //Debug.Log("2Dpos is " + PlayerBoardInforDict[i].Cells2DPos+
                //    "3Dpos is "+ PlayerBoardInforDict[i].Cells3DPos);
            }
            moveSequence.OnComplete(() =>
            {
                // 动画完成后更新数据
                bCanContinue = true;

                // 更新本地数据
                PlayerDataManager.Instance.MoveUnit(localPlayerId, fromPos, toPos);

                // 更新本地引用
                localPlayerUnits.Remove(fromPos);
                localPlayerUnits[toPos] = SelectingUnit;

                //_HexGrid.GetCell(fromPos.x, fromPos.y).Unit = false;
                //_HexGrid.GetCell(toPos.x, toPos.y).Unit = true;
                // 更新格子上是否有单位
                //Debug.Log("移动成功！cell x " + toPos.x + " cell y " + toPos.y + "  cell unit is " + _HexGrid.GetCell(toPos.x, toPos.y).Unit);

                // 更新GameManage的格子对象
                GameManage.Instance.MoveCellObject(fromPos, toPos);

                LastSelectingCellID = targetCellId;

                _HexGrid.ClearPath();

                // ============= 移动消耗AP逻辑 ============
                PlayerUnitData? unitData = PlayerDataManager.Instance.FindUnit(localPlayerId, toPos);
                if (unitData.HasValue&&unitData.Value.UnitType!=CardType.Farmer)
                {
                    int pieceID = unitData.Value.PlayerUnitDataSO.pieceID;

                    // 消耗AP
                    bool apConsumed = PieceManager.Instance.ConsumePieceAP(pieceID, listCellPos.Count - 1);

                    if (apConsumed)
                    {
                        Debug.Log($"[移动] 单位 PieceID:{pieceID} 消耗{listCellPos.Count - 1} AP");

                        // 检查AP是否为0
                        Piece piece = PieceManager.Instance.GetPiece(pieceID);
                        if (piece != null && piece.CurrentAP <= 0)
                        {
                            PlayerDataManager.Instance.UpdateUnitCanDoActionByPos(localPlayerId, toPos, false);
                            Debug.Log($"[移动] 单位 PieceID:{pieceID} AP为0，bCanDoAction设置为false");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[移动] 单位 PieceID:{pieceID} AP消耗失败");
                    }
                }


                // 发送网络消息 - 移动
                if (NetGameSystem.Instance != null)
                {
                    UnitMoveMessage moveMsg = new UnitMoveMessage
                    {
                        PlayerId = localPlayerId,
                        FromX = fromPos.x,
                        FromY = fromPos.y,
                        ToX = toPos.x,
                        ToY = toPos.y,
                    };
                    NetGameSystem.Instance.SendMessage(NetworkMessageType.UNIT_MOVE, moveMsg);
                    Debug.Log($"[本地] 已发送移动消息到网络: ({fromPos.x},{fromPos.y}) -> ({toPos.x},{toPos.y})");
                }
            });
        }
        else
        {
            _HexGrid.ClearPath();
            //Debug.Log("移动失败！cell x "+toPos.x+" cell y "+toPos.y+"  cell unit is " + _HexGrid.GetCell(toPos.x, toPos.y).Unit);
            bCanContinue = true;
        }

    }

    // 献祭
    public void FarmerSacrifice()
    {
        List<int> pos = GameManage.Instance.GetBoardNineSquareGrid(selectCellID, false);
        int farmerID = PlayerDataManager.Instance.nowChooseUnitID;
        int2 farmerPos = PlayerDataManager.Instance.GetUnitDataById(farmerID).Value.Position;
        foreach (var i in pos)
        {
            PlayerUnitData? data = PlayerDataManager.Instance.GetPlayerData(localPlayerId).FindUnitAt(GameManage.Instance.FindCell(i).Cells2DPos);

            if (data != null && data.Value.UnitType != CardType.Building)
            {
                Debug.Log("unit is " + data.Value.UnitID + " unit name is " + data.Value.UnitType);
                PieceManager.Instance.SacrificeToPiece(farmerID, data.Value.UnitID);
            }
        }

        {
            // 若有献祭动画，则将此段代码置于动画结束回调
            // 数据处理后让农民消失
            Debug.Log($"[农民献祭] 农民已献祭，开始消失");

            // 1. 从PlayerDataManager移除农民（使用原始位置farmerPos）
            bool removed = PlayerDataManager.Instance.RemoveUnit(localPlayerId, farmerPos);

            if (removed)
            {
                Debug.Log($"[农民献祭] 已从PlayerDataManager移除农民");
            }
            GameObject farmerObj = SelectingUnit;
            // 2. 销毁农民GameObject（不影响建筑）
            if (farmerObj != null)
            {
                // 播放消失动画（淡出效果）
                farmerObj.transform.DOScale(Vector3.zero, 0.5f).OnComplete(() =>
                {
                    Destroy(farmerObj);
                    Debug.Log($"[农民献祭] 农民GameObject已销毁");
                });

                // 从本地单位字典中移除农民（使用原始位置）
                if (localPlayerUnits.ContainsKey(farmerPos))
                {
                    localPlayerUnits.Remove(farmerPos);
                }
            }

            // 3. 从PieceManager移除
            PieceManager.Instance.RemovePiece(farmerID);

            // 4. 更新GameManage的格子对象（将农民从原位置移除，不影响建筑位置）
            GameManage.Instance.SetCellObject(farmerPos, null);

            // 5. 网络同步农民消失（使用农民的原始位置）
            SyncFarmerEnterBuilding(farmerID, farmerPos);

            Debug.Log($"[农民进建筑] 完成 - 农民ID:{farmerID} 已献祭并消失");

            // 重置选择状态
            ReturnToDefault();
            SelectingUnit = null;
            bCanContinue = true;
        }

    }





    // 生成建筑
    private void CreateBuilding()
    {
        syncBuildingData? buildDataNullable = (syncBuildingData)GameManage.Instance._BuildingManager.CreateBuildingByReligion(
                      SceneStateManager.Instance.PlayerReligion, localPlayerId,
                      PlayerDataManager.Instance.GenerateUnitID(),
                      PlayerBoardInforDict[SelectedEmptyCellID].Cells3DPos);

        if (buildDataNullable.HasValue)
        {
            syncBuildingData buildData = buildDataNullable.Value;

            Debug.Log($"建筑创建成功: ID={buildData.buildingID}, Name={buildData.buildingName}, PlayerID={buildData.playerID}");


            // 2. 将建筑作为Unit添加到PlayerData
            int2 buildingPos2D = PlayerBoardInforDict[SelectedEmptyCellID].Cells2DPos;

            // 创建syncPieceData（用于单位系统）
            syncPieceData buildingPieceData = new syncPieceData
            {
                currentPID = localPlayerId,
                pieceID = buildData.buildingID,
                piecetype = PlayerUnitDataInterface.Instance.ConvertCardTypeToPieceType(CardType.Building),  // 假设有Building类型，或用特殊值 
            };

            // 创建PlayerUnitData，包含buildingData
            PlayerUnitData buildingUnit = new PlayerUnitData(
                buildData.buildingID,
                CardType.Building,  // 建筑类型
                buildingPos2D,
                buildingPieceData,
                true,   // 已激活
                false,  // 建筑不需要行动
                false,  // 不被魅惑
                0,      // 魅惑回合
                localPlayerId,     // 原始所有者
                buildData  // 传入完整的建筑数据
            );

            // 添加到PlayerData的PlayerUnits列表（作为特殊单位）
            PlayerData localPlayerData = PlayerDataManager.Instance.GetPlayerData(localPlayerId);
            localPlayerData.PlayerUnits.Add(buildingUnit);

            // 添加到本地单位GameObject字典
            if (GameManage.Instance._BuildingManager.GetBuildingGameObject() != null)
            {
                localPlayerUnits[buildingPos2D] = GameManage.Instance._BuildingManager.GetBuildingGameObject();
                Debug.Log($"建筑GameObject已添加到localPlayerUnits");
            }

            // 添加描边效果
            GameManage.Instance._BuildingManager.GetBuildingGameObject().AddComponent<ChangeMaterial>();

            GameManage.Instance.SetCellObject(buildingPos2D, GameManage.Instance._BuildingManager.GetBuildingGameObject());
            Debug.Log($"建筑已作为Unit添加到PlayerData.PlayerUnits: BuildingID={buildData.buildingID}");

            // 3. 网络同步：使用现有的UNIT_ADD消息
            if (NetGameSystem.Instance != null)
            {
                // 将buildingData序列化存储在syncPieceData中
                // 可以使用tag字段或其他字段存储序列化的buildingData
                NetGameSystem.Instance.SendUnitAddMessage(
                    localPlayerId,
                    CardType.Building,
                    buildingPos2D,
                    buildingPieceData,
                    false,  // isUsed
                    buildData  // buildingData - 传入完整的建筑数据
                );
                Debug.Log($"已发送建筑创建网络消息(UNIT_ADD): BuildingID={buildData.buildingID}");
            }
        }
        else
        {
            Debug.LogError("建筑创建失败！");
        }
    }

    // 检查指定位置的单位是否有足够的AP执行动作
    private bool CheckUnitHasEnoughAP(int2 position, int requiredAP = 1)
    {
        // 获取单位数据
        PlayerUnitData? unitData = PlayerDataManager.Instance.FindUnit(localPlayerId, position);

        if (!unitData.HasValue)
        {
            Debug.LogWarning($"[AP检查] 找不到位置 ({position.x},{position.y}) 的单位");
            return false;
        }

        // 获取pieceID并检查AP
        int pieceID = unitData.Value.PlayerUnitDataSO.pieceID;
        int currentAP = PieceManager.Instance.GetPieceAP(pieceID);

        if (currentAP < requiredAP)
        {
            Debug.Log($"[AP检查] 单位 PieceID:{pieceID} AP不足 (当前:{currentAP}, 需要:{requiredAP})");
            //ShowAPInsufficientMessage($"AP不足！当前AP: {currentAP}，需要: {requiredAP}");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 检查两个位置是否相邻（六边形格子的六个方向）
    /// </summary>
    /// <param name="pos1">位置1</param>
    /// <param name="pos2">位置2</param>
    /// <returns>如果相邻返回true</returns>
    private bool IsAdjacentPosition(int2 a, int2 b)
    {
        // 计算差值
        int d = _HexGrid.GetCell(a.x, a.y).Coordinates.DistanceTo(_HexGrid.GetCell(b.x, b.y).Coordinates);

        //Debug.Log("攻击者位置 " + a + " 被攻击者位置" + b+" 距离: "+d);
        if (d <= 1)
            return true;
        else
            return false;


    }

    // ========== 新增方法：执行Pope交换位置 ==========
    /// <summary>
    /// 执行Pope与己方单位交换位置
    /// </summary>
    /// <param name="popePos">Pope的位置</param>
    /// <param name="targetPos">目标单位的位置</param>
    /// <param name="targetCellId">目标格子的ID</param>
    private void ExecutePopeSwapPosition(int2 popePos, int2 targetPos, int targetCellId)
    {
        Debug.Log($"[Pope交换] 开始执行位置交换: Pope({popePos.x},{popePos.y}) <-> Target({targetPos.x},{targetPos.y})");

        // 获取Pope单位数据
        PlayerUnitData? popeUnitData = PlayerDataManager.Instance.FindUnit(localPlayerId, popePos);
        if (!popeUnitData.HasValue)
        {
            Debug.LogError("[Pope交换] 找不到Pope单位数据");
            return;
        }

        // 获取目标单位数据
        PlayerUnitData? targetUnitData = PlayerDataManager.Instance.FindUnit(localPlayerId, targetPos);
        if (!targetUnitData.HasValue)
        {
            Debug.LogError("[Pope交换] 找不到目标单位数据");
            return;
        }
        //if(!PieceManager.Instance.CanSwapPositions(popeUnitData.Value.UnitID,PlayerDataManager.Instance.GetUnitIDBy2DPos(targetPos)))
        //{
        //    Debug.Log("Piece Pope CantSwap!");
        //    return;
        //}
        // 检查目标是否为建筑
        if (targetUnitData.Value.IsBuilding())
        {
            Debug.LogWarning("[Pope交换] 不能与建筑交换位置");
            return;
        }

        // 禁用输入
        bCanContinue = false;

        // 获取两个单位的GameObject
        GameObject popeObject = SelectingUnit; // 当前选中的就是Pope
        GameObject targetObject = null;

        if (localPlayerUnits.ContainsKey(targetPos))
        {
            targetObject = localPlayerUnits[targetPos];
        }
        else
        {
            Debug.LogError("[Pope交换] 找不到目标单位GameObject");
            bCanContinue = true;
            return;
        }

        // 获取两个位置的3D坐标
        Vector3 popeWorldPos = PlayerBoardInforDict[LastSelectingCellID].Cells3DPos;
        Vector3 targetWorldPos = PlayerBoardInforDict[targetCellId].Cells3DPos;


        // 创建交换动画序列
        Sequence swapSequence = DOTween.Sequence();

        // Pope移动到目标位置
        Vector3 popeMidPoint = (popeObject.transform.position + targetWorldPos) / 2f;
        popeMidPoint.y += 5.0f;
        Vector3[] popePath = new Vector3[] { popeObject.transform.position, popeMidPoint, targetWorldPos };
        swapSequence.Join(popeObject.transform.DOPath(popePath, MoveSpeed, PathType.CatmullRom).SetEase(Ease.Linear));

        // 目标单位移动到Pope位置
        Vector3 targetMidPoint = (targetObject.transform.position + popeWorldPos) / 2f;
        targetMidPoint.y += 5.0f;
        Vector3[] targetPath = new Vector3[] { targetObject.transform.position, targetMidPoint, popeWorldPos };
        swapSequence.Join(targetObject.transform.DOPath(targetPath, MoveSpeed, PathType.CatmullRom).SetEase(Ease.Linear));

        // 动画完成后的回调
        swapSequence.OnComplete(() =>
        {
            Debug.Log("[Pope交换] 动画完成，开始更新数据");

            // ===== 修复：使用正确的交换逻辑 =====

            // 1. 先从PlayerDataManager移除两个单位（保存引用）
            PlayerData playerData = PlayerDataManager.Instance.GetPlayerData(localPlayerId);

            // 找到两个单位在列表中的索引
            int popeIndex = -1;
            int targetIndex = -1;

            for (int i = 0; i < playerData.PlayerUnits.Count; i++)
            {
                if (playerData.PlayerUnits[i].Position.Equals(popePos))
                {
                    popeIndex = i;
                }
                else if (playerData.PlayerUnits[i].Position.Equals(targetPos))
                {
                    targetIndex = i;
                }
            }

            if (popeIndex == -1 || targetIndex == -1)
            {
                Debug.LogError($"[Pope交换] 找不到单位索引: Pope={popeIndex}, Target={targetIndex}");
                bCanContinue = true;
                return;
            }

            // 2. 交换位置数据
            PlayerUnitData popeUnit = playerData.PlayerUnits[popeIndex];
            PlayerUnitData targetUnit = playerData.PlayerUnits[targetIndex];

            // 创建新的单位数据，位置已交换
            PlayerUnitData swappedPopeUnit = new PlayerUnitData(
                popeUnit.UnitID,
                popeUnit.UnitType,
                targetPos,  // Pope的新位置
                popeUnit.PlayerUnitDataSO,
                popeUnit.bUnitIsActivated,
                popeUnit.bCanDoAction,
                popeUnit.bIsCharmed,
                popeUnit.charmedRemainingTurns,
                popeUnit.originalOwnerID,
                popeUnit.BuildingData
            );

            PlayerUnitData swappedTargetUnit = new PlayerUnitData(
                targetUnit.UnitID,
                targetUnit.UnitType,
                popePos,  // 目标单位的新位置
                targetUnit.PlayerUnitDataSO,
                targetUnit.bUnitIsActivated,
                targetUnit.bCanDoAction,
                targetUnit.bIsCharmed,
                targetUnit.charmedRemainingTurns,
                targetUnit.originalOwnerID,
                targetUnit.BuildingData
            );

            // 更新PlayerDataManager
            playerData.PlayerUnits[popeIndex] = swappedPopeUnit;
            playerData.PlayerUnits[targetIndex] = swappedTargetUnit;

            Debug.Log($"[Pope交换] PlayerDataManager数据更新成功");

            // 3. 更新本地GameObject引用
            localPlayerUnits.Remove(popePos);
            localPlayerUnits.Remove(targetPos);
            localPlayerUnits[targetPos] = popeObject;
            localPlayerUnits[popePos] = targetObject;

            // 4. 更新GameManage的格子对象引用
            GameManage.Instance.SetCellObject(popePos, targetObject);
            GameManage.Instance.SetCellObject(targetPos, popeObject);

            // 5. 更新LastSelectingCellID为Pope的新位置
            LastSelectingCellID = targetCellId;

            //// 6. 消耗Pope的AP
            //int popeID = popeUnitData.Value.PlayerUnitDataSO.pieceID;

                //// 检查AP是否为0
                //Piece popePiece = PieceManager.Instance.GetPiece(popeID);
                //if (popePiece != null && popePiece.CurrentAP <= 0)
                //{
                //    PlayerDataManager.Instance.UpdateUnitCanDoActionByPos(localPlayerId, targetPos, false);
                //    Debug.Log($"[Pope交换] Pope AP为0，bCanDoAction设置为false");
                //}
            
          

            // 7. 网络同步 - 发送交换位置消息
            // ===== 关键修复：必须使用交换后的数据 =====
            // Pope现在在targetPos，Soldier现在在popePos
            PlayerUnitData? popeAfterSwap = PlayerDataManager.Instance.FindUnit(localPlayerId, targetPos);
            PlayerUnitData? targetAfterSwap = PlayerDataManager.Instance.FindUnit(localPlayerId, popePos);

            if (popeAfterSwap.HasValue && targetAfterSwap.HasValue)
            {
                // 发送交换后的数据
                SyncPopeSwapPosition(
                    popePos,                                    // Pope的原始位置
                    targetPos,                                  // Target的原始位置
                    popeAfterSwap.Value.PlayerUnitDataSO,      // Pope交换后的数据（现在在targetPos）
                    targetAfterSwap.Value.PlayerUnitDataSO     // Target交换后的数据（现在在popePos）
                );
                Debug.Log($"[Pope交换] 已发送网络同步消息（使用交换后的数据）");
            }
            else
            {
                Debug.LogError($"[Pope交换] 无法获取交换后的单位数据，网络同步失败！");
            }


            // ===== 完整上下文参考 =====

            // 在动画完成回调中的完整代码应该是：

            swapSequence.OnComplete(() =>
            {
                Debug.Log("[Pope交换] 动画完成，开始更新数据");

                // 1-6. 数据更新、GameObject更新、AP消耗等（保持不变）
                // ... 你的现有代码 ...

                // 7. 网络同步 - 发送交换位置消息
                // ===== 关键修复：必须使用交换后的数据 =====
                PlayerUnitData? popeAfterSwap = PlayerDataManager.Instance.FindUnit(localPlayerId, targetPos);
                PlayerUnitData? targetAfterSwap = PlayerDataManager.Instance.FindUnit(localPlayerId, popePos);

                if (popeAfterSwap.HasValue && targetAfterSwap.HasValue)
                {
                    SyncPopeSwapPosition(
                        popePos,
                        targetPos,
                        popeAfterSwap.Value.PlayerUnitDataSO,
                        targetAfterSwap.Value.PlayerUnitDataSO
                    );
                    Debug.Log($"[Pope交换] 已发送网络同步消息（使用交换后的数据）");
                }
                else
                {
                    Debug.LogError($"[Pope交换] 无法获取交换后的单位数据，网络同步失败！");
                }

                // 重新启用输入
                bCanContinue = true;

                Debug.Log($"[Pope交换] 位置交换完成！Pope现在在({targetPos.x},{targetPos.y})，目标单位在({popePos.x},{popePos.y})");
            });
            // 重新启用输入
            bCanContinue = true;

            Debug.Log($"[Pope交换] 位置交换完成！Pope现在在({targetPos.x},{targetPos.y})，目标单位在({popePos.x},{popePos.y})");
        });
    }



       #region ===农民进入建筑===
    // 农民进入建筑
    private void ExecuteFarmerEnterBuilding(int2 farmerPos, int2 buildingPos, int buildingCellId)
    {
        if (SelectingUnit == null) return;

        bCanContinue = false;

        Debug.Log($"[农民进建筑] 开始执行: 农民位置({farmerPos.x},{farmerPos.y}) -> 建筑位置({buildingPos.x},{buildingPos.y})");

        // 获取农民数据
        PlayerUnitData? farmerData = PlayerDataManager.Instance.FindUnit(localPlayerId, farmerPos);
        if (!farmerData.HasValue)
        {
            Debug.LogError("[农民进建筑] 找不到农民数据");
            bCanContinue = true;
            return;
        }

        int farmerID = farmerData.Value.UnitID;
        int farmerPieceID = farmerData.Value.PlayerUnitDataSO.pieceID;
        int ap = PieceManager.Instance.GetPieceAP(farmerID);

        // 保存农民的GameObject引用（在移动前）
        GameObject farmerObj = SelectingUnit;

        // 使用新建立的移动方法让农民移动到建筑格子
        MoveFarmerToBuilding(buildingCellId, farmerPos, () =>
        {
            // 移动完成后的回调：让农民消失
            Debug.Log($"[农民进建筑] 农民已到达建筑，开始消失");

            // 1. 从PlayerDataManager移除农民（使用原始位置farmerPos）
            bool removed = PlayerDataManager.Instance.RemoveUnit(localPlayerId, farmerPos);
            if (removed)
            {
                Debug.Log($"[农民进建筑] 已从PlayerDataManager移除农民");
            }

            // 2. 销毁农民GameObject（不影响建筑）
            if (farmerObj != null)
            {
                // 更新血条显示
                UnitStatusUIManager.Instance.RemoveStatusUI(farmerID);
                PlayerDataManager.Instance.nowChooseUnitID = -1;
                PlayerDataManager.Instance.nowChooseUnitType=CardType.None;
                // 播放消失动画（淡出效果）
                farmerObj.transform.DOScale(Vector3.zero, 0.5f).OnComplete(() =>
                {
                    Destroy(farmerObj);
                    Debug.Log($"[农民进建筑] 农民GameObject已销毁");
                });

                // 从本地单位字典中移除农民（使用原始位置）
                if (localPlayerUnits.ContainsKey(farmerPos))
                {
                    localPlayerUnits.Remove(farmerPos);
                }
            }

          
            // 3. 从PieceManager移除
            PieceManager.Instance.RemovePiece(farmerPieceID);

            // 4. 更新GameManage的格子对象（将农民从原位置移除，不影响建筑位置）
            GameManage.Instance.SetCellObject(farmerPos, null);

         
            // 5. 网络同步农民消失（使用农民的原始位置）
            SyncFarmerEnterBuilding(farmerID, farmerPos);

            Debug.Log($"[农民进建筑] 完成 - 农民ID:{farmerID} 已进入建筑并消失");

            // 重置选择状态
            ReturnToDefault();
            SelectingUnit = null;
            bCanContinue = true;
        });
    }
    private void MoveFarmerToBuilding(int targetCellId, int2 originalFarmerPos, System.Action onComplete)
    {
        if (SelectingUnit == null)
        {
            onComplete?.Invoke();
            return;
        }

        // 获取起始和目标位置
        int2 fromPos = PlayerBoardInforDict[LastSelectingCellID].Cells2DPos;
        int2 toPos = PlayerBoardInforDict[targetCellId].Cells2DPos;

        // 检查AP
        PlayerUnitData? unitData = PlayerDataManager.Instance.FindUnit(localPlayerId, fromPos);
        if (!unitData.HasValue)
        {
            Debug.LogError("[MoveFarmerToBuilding] 找不到单位数据");
            onComplete?.Invoke();
            return;
        }

        int currentAP = PieceManager.Instance.GetPieceAP(unitData.Value.UnitID);
        Debug.Log($"[MoveFarmerToBuilding] 当前AP: {currentAP}");

        if (currentAP <= 0)
        {
            Debug.Log("[MoveFarmerToBuilding] AP不足，无法移动");
            onComplete?.Invoke();
            return;
        }

        // 寻找路径
        _HexGrid.FindPath(LastSelectingCellID, targetCellId, currentAP);

        if (!_HexGrid.HasPath)
        {
            Debug.Log("[MoveFarmerToBuilding] 没有找到路径");
            _HexGrid.ClearPath();
            onComplete?.Invoke();
            return;
        }

        List<HexCell> pathCells = _HexGrid.GetPathCells();
        int pathLength = pathCells.Count - 1;

        if (pathLength > currentAP)
        {
            Debug.Log($"[MoveFarmerToBuilding] 路径长度({pathLength})超过AP({currentAP})");
            _HexGrid.ClearPath();
            onComplete?.Invoke();
            return;
        }

        // 创建移动动画序列 - 只移动到建筑前，不实际进入建筑格子
        Sequence moveSequence = DOTween.Sequence();
        Vector3 currentPos = SelectingUnit.transform.position;

        // 移动到倒数第二个格子（如果路径只有1格就直接消失）
        int moveSteps = Mathf.Max(0, pathCells.Count - 1);

        for (int i = 0; i < moveSteps; i++)
        {
            Vector3 waypoint = new Vector3(
                pathCells[i].Position.x,
                pathCells[i].Position.y + 2.5f,
                pathCells[i].Position.z
            );

            // 创建弧形路径
            Vector3 midPoint = (currentPos + waypoint) / 2f;
            midPoint.y += 5.0f;
            Vector3[] path = new Vector3[] { currentPos, midPoint, waypoint };

            moveSequence.Append(SelectingUnit.transform.DOPath(path, MoveSpeed, PathType.CatmullRom)
                .SetEase(Ease.Linear));
            currentPos = waypoint;
        }

        moveSequence.OnComplete(() =>
        {
            // 注意：这里不更新PlayerDataManager和GameManage，因为农民将直接消失
            // 农民仍然在原始位置的数据将在回调中被清理

            _HexGrid.ClearPath();

            // 消耗AP（移动到建筑附近的步数）
            bool apConsumed = PieceManager.Instance.ConsumePieceAP(unitData.Value.PlayerUnitDataSO.pieceID, pathLength);
            if (apConsumed)
            {
                Debug.Log($"[MoveFarmerToBuilding] 消耗{pathLength} AP");
            }

            // 调用完成回调（此时农民应该在建筑附近，但数据层面仍在原位置）
            onComplete?.Invoke();
        });
    }


    #endregion
    #region ====攻击====

    /// <summary>
    /// 在当前位置执行攻击，目标必须在相邻格
    /// </summary>
    /// <param name="targetPos">目标位置</param>
    /// <param name="targetCellId">目标格子ID</param>
    private void ExecuteAttack(int2 targetPos, int targetCellId)
    {
        if (SelectingUnit == null) return;

        // 获取攻击者位置
        int2 attackerPos = PlayerBoardInforDict[LastSelectingCellID].Cells2DPos;

        //  关键检查：必须在相邻格才能攻击
        if (!IsAdjacentPosition(attackerPos, targetPos))
        {
            Debug.LogError("[ExecuteAttack] 错误：目标不在攻击范围内！");
            bCanContinue = true;
            return;
        }

        // 获取目标拥有者
        int targetOwnerId = PlayerDataManager.Instance.GetUnitOwner(targetPos);

        // 获取攻击者数据
        PlayerUnitData? attackerData = PlayerDataManager.Instance.FindUnit(localPlayerId, attackerPos);
        if (!attackerData.HasValue)
        {
            Debug.LogError("[ExecuteAttack] 找不到攻击者数据");
            bCanContinue = true;
            return;
        }

        // 获取目标数据
        PlayerUnitData? targetData = PlayerDataManager.Instance.FindUnit(targetOwnerId, targetPos);
        if (!targetData.HasValue)
        {
            Debug.LogError("[ExecuteAttack] 找不到目标数据");
            bCanContinue = true;
            return;
        }



        // 获取目标GameObject
        GameObject targetUnit = null;
        if (otherPlayersUnits.ContainsKey(targetOwnerId) &&
            otherPlayersUnits[targetOwnerId].ContainsKey(targetPos))
        {
            targetUnit = otherPlayersUnits[targetOwnerId][targetPos];
        }

        // 获取双方的 PieceID
        int attackerPieceID = attackerData.Value.PlayerUnitDataSO.pieceID;
        int targetPieceID = targetData.Value.PlayerUnitDataSO.pieceID;

        // 攻击的是单位
        if (PieceManager.Instance.AttackPieceOrBuilding(attackerPieceID, targetPieceID))
        {
            Debug.Log($"[ExecuteAttack] 战斗开始 - 攻击者ID:{attackerPieceID} 攻击 目标ID:{targetPieceID}");

            //  在移动完成后才计算战斗结果
            syncPieceData? targetSyncData = PieceManager.Instance.AttackEnemy(attackerPieceID, targetPieceID);

            if (!targetSyncData.HasValue)
            {
                Debug.LogError("[ExecuteAttack] PieceManager.AttackEnemy 调用失败！");
                bCanContinue = true;
                return;
            }

            bool updateSuccess = PlayerDataManager.Instance.UpdateUnitSyncDataByPos(
                    targetOwnerId,
                    targetPos,
                    targetSyncData.Value);

            if (updateSuccess)
            {
                Debug.Log($"[ExecuteAttack] ✓ 已同步目标HP到PlayerDataManager: {targetSyncData.Value.currentHP}");
            }
            else
            {
                Debug.LogError($"[ExecuteAttack] ✗ 同步目标HP失败！");
            }


            // 判断目标是否死亡
            bool targetDied = targetSyncData.Value.currentHP <= 0;
            Debug.Log($"[ExecuteAttack] 攻击完成 - 目标剩余HP: {targetSyncData.Value.currentHP}, 是否死亡: {targetDied} ,单位剩余行动力: {PieceManager.Instance.GetPieceAP(attackerPieceID)}");

            if (targetDied)
            {
                // 目标死亡，攻击者前进到目标位置
                Debug.Log("[ExecuteAttack] 目标死亡，攻击者前进到目标位置");
                ExecuteMoveToDeadTargetPosition(attackerPos, targetPos, targetCellId, targetUnit, targetOwnerId);
            }
            else
            {
                // 目标存活，停在当前位置
                Debug.Log("[ExecuteAttack] 目标存活，攻击者停留在当前位置");

                // 播放受击动画
                if (targetUnit != null)
                {
                    // 震动效果
                    targetUnit.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 5);

                    // 可选：显示伤害数字
                    // ShowDamageNumber(targetUnit.transform.position, damage);
                }

                // 网络同步攻击
                SyncLocalUnitAttack(attackerPos, targetPos, targetOwnerId, false);

                bCanContinue = true;
            }
        }
        // 攻击的是建筑
        else
        {
            // ===== 攻击建筑 - 执行新逻辑 =====
            Debug.Log($"[ExecuteAttack] 攻击建筑 - 攻击者ID:{attackerPieceID} 攻击建筑ID:{targetPieceID}");

            // 从BuildingManager获取建筑实例
            Buildings.Building targetBuilding = GameManage.Instance._BuildingManager.GetBuilding(targetPieceID);
            if (targetBuilding == null)
            {
                Debug.LogError($"[ExecuteAttack] 找不到建筑ID:{targetPieceID}");
                bCanContinue = true;
                return;
            }

            // 执行攻击建筑逻辑
            bool attackSuccess = PieceManager.Instance.AttackBuilding(attackerPieceID, targetBuilding);
            if (!attackSuccess)
            {
                Debug.LogError("[ExecuteAttack] 攻击建筑失败！");
                bCanContinue = true;
                return;
            }

            // 判断建筑是否被摧毁
            bool buildingDestroyed = !targetBuilding.IsAlive || targetBuilding.CurrentHP <= 0;
            Debug.Log($"[ExecuteAttack] 攻击建筑完成 - 建筑剩余HP: {targetBuilding.CurrentHP}, 是否被摧毁: {buildingDestroyed}");

            // 播放建筑受击动画
            if (targetUnit != null)
            {
                targetUnit.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 5);
            }

            if (buildingDestroyed)
            {
                // 建筑被摧毁，攻击者前进到建筑位置
                ExecuteMoveToDestroyedBuildingPosition(attackerPos, targetPos, targetCellId, targetUnit, targetOwnerId, targetBuilding);

                // 创建废墟
                GameObject ruin = Instantiate(UnitListTable.Instance.Ruins[0], GameManage.Instance.FindCell(targetCellId).Cells3DPos, Quaternion.identity);
                // 保存废墟引用
                if (!BuildingRuins.ContainsKey(localPlayerId))
                {
                    BuildingRuins[localPlayerId] = new Dictionary<int, GameObject>();
                }
                BuildingRuins[localPlayerId][RuinID] = ruin;
                RuinID++;

            }
            else
            {
                // 建筑存活，同步攻击建筑消息
                SyncLocalBuildingAttack(attackerPos, targetPos, targetOwnerId, targetPieceID, targetBuilding.CurrentHP, false);
                bCanContinue = true;
            }
        }


    }


    // ============================================
    // 新增方法5：ExecuteMoveToDeadTargetPosition
    // 目标死亡后，攻击者前进到目标位置
    // ============================================

    /// <summary>
    /// 建筑被摧毁后，攻击者前进一格到建筑位置
    /// </summary>
    private void ExecuteMoveToDestroyedBuildingPosition(
        int2 fromPos,
        int2 toPos,
        int targetCellId,
        GameObject targetUnit,
        int targetOwnerId,
        Buildings.Building targetBuilding)
    {
        // 计算移动路径（只有一格的距离）
        Vector3 startPos = SelectingUnit.transform.position;
        Vector3 targetWorldPos = GameManage.Instance.FindCell(targetCellId).Cells3DPos;
        targetWorldPos.y += 2.5f;

        // 创建移动动画
        Sequence moveSequence = DOTween.Sequence();

        // 弧形路径
        Vector3 midPoint = (startPos + targetWorldPos) / 2f;
        midPoint.y += 5.0f;
        Vector3[] path = new Vector3[] { startPos, midPoint, targetWorldPos };

        moveSequence.Append(SelectingUnit.transform.DOPath(path, MoveSpeed, PathType.CatmullRom)
            .SetEase(Ease.Linear));

        // 同时播放建筑摧毁动画
        if (targetUnit != null)
        {
            moveSequence.Join(targetUnit.transform.DOScale(0f, 0.5f));
            moveSequence.Join(targetUnit.transform.DORotate(
                new Vector3(0, 360, 0), 0.5f, RotateMode.FastBeyond360));
        }

        // 动画完成后的处理
        moveSequence.OnComplete(() =>
        {
            // 1. 发送攻击建筑消息
            SyncLocalBuildingAttack(fromPos, toPos, targetOwnerId, targetBuilding.BuildingID, 0, true);

            // 2. 销毁建筑GameObject
            if (targetUnit != null) Destroy(targetUnit);

            // 3. 从BuildingManager移除建筑
            GameManage.Instance._BuildingManager.RemoveBuilding(targetBuilding.BuildingID);

            // 4. 从PlayerDataManager移除建筑数据
            PlayerDataManager.Instance.RemoveUnit(targetOwnerId, toPos);

            // 5. 移动攻击者数据
            PlayerDataManager.Instance.MoveUnit(localPlayerId, fromPos, toPos);

            // 6. 更新本地单位字典
            localPlayerUnits.Remove(fromPos);
            localPlayerUnits[toPos] = SelectingUnit;

            // 7. 从目标玩家的单位字典中移除
            if (otherPlayersUnits.ContainsKey(targetOwnerId))
            {
                otherPlayersUnits[targetOwnerId].Remove(toPos);
            }

            // 8. 更新GameManage的格子对象
            GameManage.Instance.MoveCellObject(fromPos, toPos);

            // 9. 更新选中的格子ID
            LastSelectingCellID = targetCellId;

            bCanContinue = true;
        });
    }


    /// <summary>
    /// 目标死亡后，攻击者前进一格到目标位置
    /// </summary>
    private void ExecuteMoveToDeadTargetPosition(
        int2 fromPos,
        int2 toPos,
        int targetCellId,
        GameObject targetUnit,
        int targetOwnerId)
    {
        // 计算移动路径（只有一格的距离）
        Vector3 startPos = SelectingUnit.transform.position;
        Vector3 targetWorldPos = GameManage.Instance.FindCell(targetCellId).Cells3DPos;

        // 创建移动动画
        Sequence moveSequence = DOTween.Sequence();

        // 弧形路径
        Vector3 midPoint = (startPos + targetWorldPos) / 2f;
        midPoint.y += 5.0f;
        Vector3[] path = new Vector3[] { startPos, midPoint, targetWorldPos };

        moveSequence.Append(SelectingUnit.transform.DOPath(path, MoveSpeed, PathType.CatmullRom)
            .SetEase(Ease.Linear));

        // 同时播放目标死亡动画
        if (targetUnit != null)
        {
            // 缩放到0
            moveSequence.Join(targetUnit.transform.DOScale(0f, 0.5f));

            // 旋转消失
            moveSequence.Join(targetUnit.transform.DORotate(
                new Vector3(0, 360, 0),
                0.5f,
                RotateMode.FastBeyond360
            ));
        }

        // 动画完成后的处理
        moveSequence.OnComplete(() =>
        {
            // 1发送攻击消息（通知目标被击杀）
            SyncLocalUnitAttack(fromPos, toPos, targetOwnerId, true);
            Debug.Log($"[ExecuteMoveToDeadTargetPosition] 已发送攻击消息：击杀目标 at ({toPos.x},{toPos.y})");

            //// 发送移动消息（通知攻击者移动到目标位置）
            //SyncLocalUnitMove(fromPos, toPos);
            //Debug.Log($"[ExecuteMoveToDeadTargetPosition] 已发送移动消息：攻击者 ({fromPos.x},{fromPos.y}) → ({toPos.x},{toPos.y})");



            // 销毁目标GameObject
            if (targetUnit != null)
            {
                Destroy(targetUnit);
            }

            // 先获取目标数据
            PlayerUnitData? targetData = PlayerDataManager.Instance.FindUnit(targetOwnerId, toPos);

            // 从PieceManager移除目标
            if (PieceManager.Instance.DoesPieceExist(targetData.Value.UnitID))
            {
                PieceManager.Instance.RemovePiece(targetData.Value.UnitID);
                Debug.Log($"[ExecuteMoveToDeadTargetPosition] 已从PieceManager移除目标 ID:{targetData.Value.UnitID}");
            }
            else
            {
                Debug.LogWarning($"[ExecuteMoveToDeadTargetPosition] 找不到目标单位数据 at ({toPos.x},{toPos.y})");
            }
            // 3. 从PlayerDataManager移除目标数据
            PlayerDataManager.Instance.RemoveUnit(targetOwnerId, toPos);


            // 移动攻击者数据
            PlayerDataManager.Instance.MoveUnit(localPlayerId, fromPos, toPos);

            // 更新本地单位字典
            localPlayerUnits.Remove(fromPos);
            localPlayerUnits[toPos] = SelectingUnit;

            // 从目标玩家的单位字典中移除
            if (otherPlayersUnits.ContainsKey(targetOwnerId))
            {
                otherPlayersUnits[targetOwnerId].Remove(toPos);
            }

            // 更新GameManage的格子对象
            GameManage.Instance.MoveCellObject(fromPos, toPos);

            // 更新选中的格子ID
            LastSelectingCellID = targetCellId;



            Debug.Log($"[ExecuteAttack] 击杀目标，移动到目标位置: ({toPos.x},{toPos.y})");

            bCanContinue = true;
        });
    }

    // 处理网络建筑攻击消息
    public void HandleNetworkBuildingAttack(BuildingAttackMessage msg)
    {
        if (msg.AttackerPlayerId == localPlayerId)
        {
            Debug.Log("[网络建筑攻击] 这是本地玩家的攻击，已处理");
            return;
        }

        int2 attackerPos = new int2(msg.AttackerPosX, msg.AttackerPosY);
        int2 buildingPos = new int2(msg.BuildingPosX, msg.BuildingPosY);

        Debug.Log($"[网络建筑攻击] 玩家 {msg.AttackerPlayerId} 攻击建筑 ID={msg.BuildingID} at ({buildingPos.x},{buildingPos.y})");
        Debug.Log($"[网络建筑攻击] 攻击者位置: ({attackerPos.x},{attackerPos.y}), 建筑是否被摧毁: {msg.BuildingDestroyed}");

        // 获取攻击者GameObject
        GameObject attackerObj = null;
        if (otherPlayersUnits.ContainsKey(msg.AttackerPlayerId) &&
            otherPlayersUnits[msg.AttackerPlayerId].ContainsKey(attackerPos))
        {
            attackerObj = otherPlayersUnits[msg.AttackerPlayerId][attackerPos];
        }

        if (attackerObj == null)
        {
            Debug.LogWarning($"[网络建筑攻击] 找不到攻击者GameObject at ({attackerPos.x},{attackerPos.y})");
            return;
        }

        // 获取建筑GameObject
        GameObject buildingObj = null;
        if (msg.BuildingOwnerId == localPlayerId && localPlayerUnits.ContainsKey(buildingPos))
        {
            buildingObj = localPlayerUnits[buildingPos];
        }
        else if (otherPlayersUnits.ContainsKey(msg.BuildingOwnerId) &&
                 otherPlayersUnits[msg.BuildingOwnerId].ContainsKey(buildingPos))
        {
            buildingObj = otherPlayersUnits[msg.BuildingOwnerId][buildingPos];
        }

        if (buildingObj == null)
        {
            Debug.LogWarning($"[网络建筑攻击] 找不到建筑GameObject at ({buildingPos.x},{buildingPos.y})");
            return;
        }

        if (msg.BuildingDestroyed)
        {
            // ===== 建筑被摧毁，攻击者前进到建筑位置 =====
            Debug.Log($"[网络建筑攻击] 建筑被摧毁，攻击者将前进到建筑位置");

            // 播放建筑摧毁动画
            Sequence destroySequence = DOTween.Sequence();
            destroySequence.Join(buildingObj.transform.DOScale(0f, 0.5f));
            destroySequence.Join(buildingObj.transform.DORotate(
                new Vector3(0, 360, 0), 0.5f, RotateMode.FastBeyond360));

            destroySequence.OnComplete(() =>
            {
                // 1. 销毁建筑GameObject
                Destroy(buildingObj);
                Debug.Log($"[网络建筑攻击] 建筑GameObject已销毁");

                // 2. 从单位字典中移除建筑
                if (msg.BuildingOwnerId == localPlayerId && localPlayerUnits.ContainsKey(buildingPos))
                {
                    localPlayerUnits.Remove(buildingPos);
                }
                else if (otherPlayersUnits.ContainsKey(msg.BuildingOwnerId) &&
                         otherPlayersUnits[msg.BuildingOwnerId].ContainsKey(buildingPos))
                {
                    otherPlayersUnits[msg.BuildingOwnerId].Remove(buildingPos);
                }

                // 3. 创建废墟
                GameObject ruin = Instantiate(UnitListTable.Instance.Ruins[0],
                    GameManage.Instance.GetCell2D(buildingPos).Cells3DPos,
                    Quaternion.identity);

                // 保存废墟引用
                if (!BuildingRuins.ContainsKey(localPlayerId))
                {
                    BuildingRuins[localPlayerId] = new Dictionary<int, GameObject>();
                }
                BuildingRuins[localPlayerId][RuinID] = ruin;
                RuinID++;

                // 4. 攻击者前进到建筑位置
                HandleAttackerMoveToDestroyedBuilding(
                    attackerObj,
                    msg.AttackerPlayerId,
                    attackerPos,
                    buildingPos
                );
            });
        }
        else
        {
            // ===== 建筑存活，只播放受击动画 =====
            Debug.Log($"[网络建筑攻击] 建筑存活，播放受击动画");

            // 播放受击动画
            buildingObj.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 5);

            // 更新建筑HP
            Buildings.Building building = GameManage.Instance._BuildingManager.GetBuilding(msg.BuildingID);
            if (building != null)
            {
                building.SetHP(msg.BuildingRemainingHP);
                Debug.Log($"[网络建筑攻击] 建筑 {building.BuildingID} 受到攻击，剩余HP: {building.CurrentHP}");
            }
        }

        Debug.Log($"[网络建筑攻击] 攻击处理完成");
    }

    // ========================================
    // 辅助方法：处理攻击者移动到被摧毁建筑的位置
    // ========================================
    private void HandleAttackerMoveToDestroyedBuilding(
        GameObject attackerObj,
        int attackerPlayerId,
        int2 attackerCurrentPos,
        int2 buildingPos)
    {
        Debug.Log($"[HandleAttackerMoveToDestroyedBuilding] 攻击者从 ({attackerCurrentPos.x},{attackerCurrentPos.y}) 前进到 ({buildingPos.x},{buildingPos.y})");

        // 获取目标世界坐标
        Vector3 targetWorldPos = GameManage.Instance.GetCell2D(buildingPos).Cells3DPos;
        targetWorldPos.y += 2.5f;

        // 更新字典：从原位置移除，添加到新位置
        if (attackerPlayerId == localPlayerId)
        {
            localPlayerUnits.Remove(attackerCurrentPos);
            localPlayerUnits[buildingPos] = attackerObj;
        }
        else if (otherPlayersUnits.ContainsKey(attackerPlayerId))
        {
            otherPlayersUnits[attackerPlayerId].Remove(attackerCurrentPos);
            otherPlayersUnits[attackerPlayerId][buildingPos] = attackerObj;
        }

        // 更新GameManage的格子对象
        GameManage.Instance.SetCellObject(attackerCurrentPos, null);
        GameManage.Instance.SetCellObject(buildingPos, attackerObj);

        // 播放前进动画（弧形路径）
        Vector3 startPos = attackerObj.transform.position;
        Vector3 midPoint = (startPos + targetWorldPos) / 2f;
        midPoint.y += 5.0f;

        Sequence moveSeq = DOTween.Sequence();
        moveSeq.Append(attackerObj.transform.DOPath(
            new Vector3[] { startPos, midPoint, targetWorldPos },
            MoveSpeed * 0.5f,
            PathType.CatmullRom
        ).SetEase(Ease.Linear));

        moveSeq.OnComplete(() =>
        {
            Debug.Log($"[HandleAttackerMoveToDestroyedBuilding] 攻击者前进动画完成");
        });
    }

    /// <summary>
    /// 处理网络同步的建筑摧毁消息
    /// </summary>
    public void HandleNetworkBuildingDestruction(BuildingDestructionMessage msg)
    {
        if (msg.BuildingOwnerId == localPlayerId)
        {
            Debug.Log("[网络建筑摧毁] 这是本地玩家的建筑,已处理");
            return;
        }

        int2 buildingPos = new int2(msg.BuildingPosX, msg.BuildingPosY);

        Debug.Log($"[网络建筑摧毁] 玩家 {msg.BuildingOwnerId} 的建筑 ID={msg.BuildingID} at ({buildingPos.x},{buildingPos.y}) 被摧毁");

        // 获取建筑GameObject
        GameObject buildingObj = null;
        if (otherPlayersUnits.ContainsKey(msg.BuildingOwnerId) &&
            otherPlayersUnits[msg.BuildingOwnerId].ContainsKey(buildingPos))
        {
            buildingObj = otherPlayersUnits[msg.BuildingOwnerId][buildingPos];
        }

        if (buildingObj == null)
        {
            Debug.LogWarning($"[网络建筑摧毁] 找不到建筑GameObject at ({buildingPos.x},{buildingPos.y})");
            return;
        }

        // 播放建筑摧毁动画
        Sequence destroySequence = DOTween.Sequence();
        destroySequence.Join(buildingObj.transform.DOScale(0f, 0.5f));
        destroySequence.Join(buildingObj.transform.DORotate(
            new Vector3(0, 360, 0), 0.5f, RotateMode.FastBeyond360));

        destroySequence.OnComplete(() =>
        {
            // 1. 销毁建筑GameObject
            Destroy(buildingObj);
            Debug.Log($"[网络建筑摧毁] 建筑GameObject已销毁");

            // 2. 从单位字典中移除建筑
            if (otherPlayersUnits.ContainsKey(msg.BuildingOwnerId) &&
                otherPlayersUnits[msg.BuildingOwnerId].ContainsKey(buildingPos))
            {
                otherPlayersUnits[msg.BuildingOwnerId].Remove(buildingPos);
            }

            // 3. 创建废墟
            GameObject ruin = Instantiate(UnitListTable.Instance.Ruins[0],
                GameManage.Instance.GetCell2D(buildingPos).Cells3DPos,
                Quaternion.identity);

            // 保存废墟引用
            if (!BuildingRuins.ContainsKey(msg.BuildingOwnerId))
            {
                BuildingRuins[msg.BuildingOwnerId] = new Dictionary<int, GameObject>();
            }
            BuildingRuins[msg.BuildingOwnerId][RuinID] = ruin;
            RuinID++;

            Debug.Log($"[网络建筑摧毁] 废墟已创建");
        });

        Debug.Log($"[网络建筑摧毁] 摧毁处理完成");
    }

    // 处理网络攻击消息
    public void HandleNetworkAttack(UnitAttackMessage msg)
    {
        if (msg.AttackerPlayerId == localPlayerId)
        {
            Debug.Log("[网络攻击] 这是本地玩家的攻击，已处理");
            return;
        }

        int2 attackerPos = new int2(msg.AttackerPosX, msg.AttackerPosY);
        int2 targetPos = new int2(msg.TargetPosX, msg.TargetPosY);

        Debug.Log($"[网络攻击] 玩家 {msg.AttackerPlayerId} 单位 {msg.AttackerSyncData.pieceID} 攻击 玩家 {msg.TargetPlayerId} 单位 {msg.TargetSyncData.Value.pieceID} ");
        Debug.Log($"[网络攻击] 攻击者位置: ({attackerPos.x},{attackerPos.y}), 目标位置: ({targetPos.x},{targetPos.y})");

        // 获取攻击者GameObject
        GameObject attackerObj = null;
        int2 attackerOriginalPos = attackerPos; // 默认原始位置就是当前位置

        // 【修复】首先尝试从原始位置查找攻击者（可能还没移动）
        // 检查消息中是否包含原始位置信息
        if (msg.AttackerOriginalPosX != msg.AttackerPosX || msg.AttackerOriginalPosY != msg.AttackerPosY)
        {
            // 这是一个"移动+攻击"场景
            attackerOriginalPos = new int2(msg.AttackerOriginalPosX, msg.AttackerOriginalPosY);
            Debug.Log($"[网络攻击] 检测到移动+攻击，原始位置: ({attackerOriginalPos.x},{attackerOriginalPos.y})");

            // 从原始位置获取攻击者对象
            if (otherPlayersUnits.ContainsKey(msg.AttackerPlayerId) &&
                otherPlayersUnits[msg.AttackerPlayerId].ContainsKey(attackerOriginalPos))
            {
                attackerObj = otherPlayersUnits[msg.AttackerPlayerId][attackerOriginalPos];
            }
        }
        else
        {
            // 这是一个"直接攻击"场景（攻击者已经在攻击范围内）
            Debug.Log($"[网络攻击] 直接攻击，无需移动");

            if (otherPlayersUnits.ContainsKey(msg.AttackerPlayerId) &&
                otherPlayersUnits[msg.AttackerPlayerId].ContainsKey(attackerPos))
            {
                attackerObj = otherPlayersUnits[msg.AttackerPlayerId][attackerPos];
            }
        }

        if (attackerObj == null)
        {
            Debug.LogWarning($"[网络攻击] 找不到攻击者GameObject");
            return;
        }

        // 获取目标GameObject
        GameObject targetObj = null;
        if (msg.TargetPlayerId == localPlayerId && localPlayerUnits.ContainsKey(targetPos))
        {
            targetObj = localPlayerUnits[targetPos];
        }
        else if (otherPlayersUnits.ContainsKey(msg.TargetPlayerId) &&
                 otherPlayersUnits[msg.TargetPlayerId].ContainsKey(targetPos))
        {
            targetObj = otherPlayersUnits[msg.TargetPlayerId][targetPos];
        }

        // ========================================
        // 【修复】根据场景执行不同的动画流程
        // ========================================

        if (attackerOriginalPos.x != attackerPos.x || attackerOriginalPos.y != attackerPos.y)
        {
            // ===== 场景1：移动+攻击 =====
            Debug.Log($"[网络攻击] 执行移动+攻击动画");

            // 先播放移动动画
            PlayMoveAnimationForAttack(
                attackerObj,
                msg.AttackerPlayerId,
                attackerOriginalPos,
                attackerPos,
                () => {
                    // 移动完成后播放攻击效果
                    Debug.Log($"[网络攻击] 移动完成，播放攻击效果");

                    if (msg.TargetDestroyed)
                    {
                      

                        // 目标死亡，攻击者继续前进到目标位置
                        HandleTargetDestroyedAfterAttack(
                            attackerObj,
                            msg.AttackerPlayerId,
                            attackerPos,
                            targetPos,
                            targetObj,
                            msg.TargetPlayerId
                        );
                    }
                    else
                    {
                        // 目标存活，只播放受击动画
                        HandleTargetSurvivedAfterAttack(targetObj, msg);
                    }
                }
            );
        }
        else
        {
            // ===== 场景2：直接攻击（不需要移动） =====
            Debug.Log($"[网络攻击] 执行直接攻击");

            // 播放攻击动画（可选）
            // PlayAttackAnimation(attackerObj);

            if (msg.TargetDestroyed)
            {
                // 先处理处理血量
                UnitStatusUIManager.Instance.RemoveStatusUI(msg.TargetSyncData.Value.pieceID);

                //计算己方死亡单位 (红月教)
                PlayerDataManager.Instance.DeadUnitCount += 1;

                // 触发回血被动
                if (SceneStateManager.Instance.PlayerReligion == Religion.RedMoonReligion 
                    && PlayerDataManager.Instance.DeadUnitCount >= 12
                    &&!PlayerDataManager.Instance.bRedMoonSkill)
                {
                    PlayerDataManager.Instance.bRedMoonSkill= true;

                    for (int i = 0; i < PlayerDataManager.Instance.GetPlayerData(localPlayerId).PlayerUnits.Count; i++)
                    {
                        if (PlayerDataManager.Instance.GetPlayerData(localPlayerId).PlayerUnits[i].UnitID != msg.TargetSyncData.Value.pieceID)
                        {
                           
                            syncPieceData newData = new syncPieceData();
                            if (PlayerDataManager.Instance.GetPlayerData(localPlayerId).PlayerUnits[i].UnitType != CardType.Building)
                            {
                                int hp = PieceManager.Instance.GetPieceAllHP(PlayerDataManager.Instance.GetPlayerData(localPlayerId).PlayerUnits[i].UnitID) / 5;
                                if (hp == 0)
                                    hp += 1;
                                newData = (syncPieceData)PieceManager.Instance.HealPiece(PlayerDataManager.Instance.GetPlayerData(localPlayerId).PlayerUnits[i].UnitID, hp);

                                PlayerUnitData unit = PlayerDataManager.Instance.GetPlayerData(localPlayerId).PlayerUnits[i];
                                unit.PlayerUnitDataSO = newData;
                                PlayerDataManager.Instance.GetPlayerData(localPlayerId).PlayerUnits[i] = unit;

                                Debug.Log("Heal HP is " + PlayerDataManager.Instance.GetPlayerData(localPlayerId).PlayerUnits[i].PlayerUnitDataSO.currentHP);
                            }
                        }
                    }
                }

                // 目标死亡，攻击者前进到目标位置
                HandleTargetDestroyedAfterAttack(
                    attackerObj,
                    msg.AttackerPlayerId,
                    attackerPos,
                    targetPos,
                    targetObj,
                    msg.TargetPlayerId
                );
            }
            else
            {
                // 目标存活，只播放受击动画
                HandleTargetSurvivedAfterAttack(targetObj, msg);
                // 处理血量
               UnitStatusUIManager.Instance.UpdateHPByID(msg.TargetSyncData.Value.pieceID,msg.TargetSyncData.Value.currentHP);
            }
        }

        Debug.Log($"[网络攻击] 攻击处理完成");
    }

    // ========================================
    // 辅助方法1：播放移动到攻击位置的动画
    // ========================================
    private void PlayMoveAnimationForAttack(
        GameObject attackerObj,
        int attackerPlayerId,
        int2 fromPos,
        int2 toPos,
        System.Action onComplete)
    {
        // 获取目标世界坐标
        Vector3 targetWorldPos = GameManage.Instance.GetCell2D(toPos).Cells3DPos;
        targetWorldPos.y += 2.5f;

        // 更新字典：从原位置移除，添加到新位置
        if (attackerPlayerId == localPlayerId)
        {
            localPlayerUnits.Remove(fromPos);
            localPlayerUnits[toPos] = attackerObj;
        }
        else if (otherPlayersUnits.ContainsKey(attackerPlayerId))
        {
            otherPlayersUnits[attackerPlayerId].Remove(fromPos);
            otherPlayersUnits[attackerPlayerId][toPos] = attackerObj;
        }

        // 更新GameManage的格子对象
        GameManage.Instance.SetCellObject(fromPos, null);
        GameManage.Instance.SetCellObject(toPos, attackerObj);

        // 播放移动动画（可以使用弧形路径）
        Vector3 startPos = attackerObj.transform.position;
        Vector3 midPoint = (startPos + targetWorldPos) / 2f;
        midPoint.y += 5.0f;

        Sequence moveSeq = DOTween.Sequence();
        moveSeq.Append(attackerObj.transform.DOPath(
            new Vector3[] { startPos, midPoint, targetWorldPos },
            MoveSpeed,
            PathType.CatmullRom
        ).SetEase(Ease.Linear));

        moveSeq.OnComplete(() =>
        {
            Debug.Log($"[PlayMoveAnimationForAttack] 移动动画完成");
            onComplete?.Invoke();
        });
    }

    // ========================================
    // 辅助方法2：处理目标死亡的情况
    // ========================================
    private void HandleTargetDestroyedAfterAttack(
        GameObject attackerObj,
        int attackerPlayerId,
        int2 attackerCurrentPos,
        int2 targetPos,
        GameObject targetObj,
        int targetPlayerId)
    {
        Debug.Log($"[HandleTargetDestroyedAfterAttack] 目标被击杀，攻击者前进到目标位置");

        // 移除目标GameObject
        if (targetObj != null)
        {
            // 播放死亡动画（可选）
            targetObj.transform.DOScale(0f, 0.3f).OnComplete(() =>
            {
                if (targetPlayerId == localPlayerId && localPlayerUnits.ContainsKey(targetPos))
                {
                    localPlayerUnits.Remove(targetPos);
                }
                else if (otherPlayersUnits.ContainsKey(targetPlayerId) &&
                         otherPlayersUnits[targetPlayerId].ContainsKey(targetPos))
                {
                    otherPlayersUnits[targetPlayerId].Remove(targetPos);
                }

                Destroy(targetObj);
            });
        }

        // 攻击者前进到目标位置
        Vector3 targetWorldPos = GameManage.Instance.GetCell2D(targetPos).Cells3DPos;

        // 更新字典
        if (attackerPlayerId == localPlayerId)
        {
            localPlayerUnits.Remove(attackerCurrentPos);
            localPlayerUnits[targetPos] = attackerObj;
        }
        else if (otherPlayersUnits.ContainsKey(attackerPlayerId))
        {
            otherPlayersUnits[attackerPlayerId].Remove(attackerCurrentPos);
            otherPlayersUnits[attackerPlayerId][targetPos] = attackerObj;
        }

        // 更新GameManage
        GameManage.Instance.SetCellObject(attackerCurrentPos, null);
        GameManage.Instance.SetCellObject(targetPos, attackerObj);

       

        // 播放前进动画
        attackerObj.transform.DOMove(targetWorldPos, MoveSpeed * 0.5f).OnComplete(() =>
        {
            Debug.Log($"[HandleTargetDestroyedAfterAttack] 攻击者前进动画完成");
        });

        // 从PieceManager中移除被击杀的目标
        PlayerUnitData? deadTargetData = PlayerDataManager.Instance.FindUnit(targetPlayerId, targetPos);
        if (deadTargetData.HasValue && PieceManager.Instance != null)
        {
            PieceManager.Instance.RemovePiece(deadTargetData.Value.UnitID);
            Debug.Log($"[HandleTargetDestroyedAfterAttack] 已从PieceManager移除被击杀单位 ID:{deadTargetData.Value.UnitID}");
        }
    }

    // ========================================
    // 辅助方法3：处理目标存活的情况
    // ========================================
    private void HandleTargetSurvivedAfterAttack(GameObject targetObj, UnitAttackMessage msg)
    {
        Debug.Log($"[HandleTargetSurvivedAfterAttack] 目标存活，当前HP: {msg.TargetSyncData?.currentHP ?? 0}");

        // 更新目标HP数据
        if (msg.TargetSyncData.HasValue)
        {
            int2 targetPos = new int2(msg.TargetPosX, msg.TargetPosY);
            bool updateSuccess = PlayerDataManager.Instance.UpdateUnitSyncDataByPos(
                msg.TargetPlayerId,
                targetPos,
                msg.TargetSyncData.Value
            );

            // 同步HP
            PieceManager.Instance.SyncPieceHP(msg.TargetSyncData.Value);




            if (updateSuccess)
            {
                Debug.Log($"[HandleTargetSurvivedAfterAttack] ✓ 已更新目标HP: {msg.TargetSyncData.Value.currentHP}");
            }
        }

        // 播放受击动画
        if (targetObj != null)
        {
            // 震动效果
            targetObj.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 5);

            // 如果有血条UI，更新血条显示
            // UpdateUnitHPDisplay(targetPos, msg.TargetSyncData.Value.currentHP);
        }
    }



    #endregion


    #region ====魅惑====
    // ============================================
    // ExecuteCharm - 传教士魅惑敌方单位
    // ============================================
    private void ExecuteCharm(int2 targetPos, int targetOwnerId)
    {
        if (SelectingUnit == null) return;

        // 获取传教士位置
        int2 missionaryPos = PlayerBoardInforDict[LastSelectingCellID].Cells2DPos;

        // 必须在相邻格才能魅惑
        if (!IsAdjacentPosition(missionaryPos, targetPos))
        {
            Debug.LogError("[ExecuteCharm] 错误：目标不在魅惑范围内！");
            bCanContinue = true;
            return;
        }

        // 获取传教士数据
        PlayerUnitData? missionaryData = PlayerDataManager.Instance.FindUnit(localPlayerId, missionaryPos);
        if (!missionaryData.HasValue)
        {
            Debug.LogError("[ExecuteCharm] 找不到传教士数据");
            bCanContinue = true;
            return;
        }

        // 获取目标数据
        PlayerUnitData? targetData = PlayerDataManager.Instance.FindUnit(targetOwnerId, targetPos);
        if (!targetData.HasValue)
        {
            Debug.LogError("[ExecuteCharm] 找不到目标数据");
            bCanContinue = true;
            return;
        }
        else
        {

            Debug.Log("已找到对方数据: " + targetData.Value.PlayerUnitDataSO.piecetype);
        }
        // 获取双方的 PieceID
        int missionaryPieceID = missionaryData.Value.PlayerUnitDataSO.pieceID;
        int targetPieceID = targetData.Value.PlayerUnitDataSO.pieceID;

        Debug.Log($"[ExecuteCharm] 魅惑尝试 - 传教士ID:{missionaryPieceID} 魅惑 目标ID:{targetPieceID}");

        // 调用PieceManager的ConvertEnemy方法
        PieceManager.Instance.ConvertEnemy(missionaryPieceID, targetPieceID);
        syncPieceData convertResult = PieceManager.Instance.GetPieceSyncPieceData(targetPieceID);


        Debug.Log("[ExecuteCharm] 魅惑成功！转移单位所有权: " + convertResult.piecetype);

        // 获取目标GameObject（需要转移到本地玩家）
        GameObject targetUnit = null;
        if (otherPlayersUnits.ContainsKey(targetOwnerId) &&
            otherPlayersUnits[targetOwnerId].ContainsKey(targetPos))
        {
            targetUnit = otherPlayersUnits[targetOwnerId][targetPos];
            Debug.Log("获取目标unit!");
        }


        // 1. 在PlayerDataManager中转移单位所有权
        syncPieceData newUnitData = convertResult;
        newUnitData.currentPID = localPlayerId; // 设置为本地玩家

        bool transferSuccess = PlayerDataManager.Instance.TransferUnitOwnership(
            targetOwnerId,      // 从原所有者
            localPlayerId,      // 转移给本地玩家
            targetPos,          // 位置
            newUnitData,        // 更新后的同步数据
            3                   // 魅惑持续3回合
        );

        if (!transferSuccess)
        {
            Debug.LogError("[ExecuteCharm] 转移单位所有权失败");
            bCanContinue = true;
            return;
        }

        // 2. 更新GameObject的字典引用
        if (targetUnit != null)
        {
            // 从敌方字典移除
            if (otherPlayersUnits.ContainsKey(targetOwnerId))
            {
                otherPlayersUnits[targetOwnerId].Remove(targetPos);
            }

            // 添加到本地玩家字典
            localPlayerUnits[targetPos] = targetUnit;


            // 播放魅惑特效
            targetUnit.transform.DOPunchScale(Vector3.one * 0.3f, 0.5f, 5);

            // 2025.11.14 Guoning 添加魅惑音效
            SoundManager.Instance.PlaySE(SoundSystem.TYPE_SE.CHARMED);

            Debug.Log($"[ExecuteCharm] 单位GameObject已转移到本地玩家控制");
        }
        else
        {
            Debug.LogWarning("[ExecuteCharm] 未找到目标GameObject，但数据层转移成功");
        }

        // 3. 网络同步魅惑操作（使用更新后的数据）
        SyncLocalUnitCharm(missionaryPieceID, missionaryPos, targetPieceID, targetOwnerId, targetPos, newUnitData);

        Debug.Log($"[ExecuteCharm] 魅惑完成 - 原所有者:{targetOwnerId}, 新所有者:{localPlayerId}");

        bCanContinue = true;
    }


    // 处理来自网络的魅惑消息
    public void HandleNetworkCharm(UnitCharmMessage msg)
    {
        if (msg.MissionaryPlayerId == localPlayerId)
        {
            Debug.Log("[网络魅惑] 这是本地玩家的魅惑，已处理");
            return;
        }

        int2 missionaryPos = new int2(msg.MissionaryPosX, msg.MissionaryPosY);
        int2 targetPos = new int2(msg.TargetPosX, msg.TargetPosY);

        Debug.Log($"[网络魅惑] 玩家 {msg.MissionaryPlayerId} 魅惑单位 at ({targetPos.x},{targetPos.y})");


        // 1. 在PlayerDataManager中转移单位所有权
        bool transferSuccess = PlayerDataManager.Instance.TransferUnitOwnership(
            msg.TargetPlayerId,         // 从原所有者
            msg.MissionaryPlayerId,     // 转移给魅惑者
            targetPos,                  // 位置
            msg.NewUnitSyncData,        // 更新后的同步数据
            msg.CharmedTurns            // 魅惑持续回合数
        );

        if (!transferSuccess)
        {
            Debug.LogError("[网络魅惑] 转移单位所有权失败");
            return;
        }

        // 2. 更新GameObject的字典引用
        GameObject targetUnit = null;

        // 从原所有者字典中获取单位
        if (msg.TargetPlayerId == localPlayerId && localPlayerUnits.ContainsKey(targetPos))
        {
            targetUnit = localPlayerUnits[targetPos];
            localPlayerUnits.Remove(targetPos);
            Debug.Log("Get Units Local:" + targetUnit.name);
        }
        else if (otherPlayersUnits.ContainsKey(msg.TargetPlayerId) &&
                 otherPlayersUnits[msg.TargetPlayerId].ContainsKey(targetPos))
        {
            targetUnit = otherPlayersUnits[msg.TargetPlayerId][targetPos];
            otherPlayersUnits[msg.TargetPlayerId].Remove(targetPos);
            Debug.Log("Get Units Other:" + targetUnit.name);
        }

        if (targetUnit == null)
        {
            Debug.LogWarning($"[网络魅惑] 未找到目标GameObject at ({targetPos.x},{targetPos.y})，尝试创建");

            // 从PlayerDataManager获取单位数据
            PlayerUnitData? unitData = PlayerDataManager.Instance.FindUnit(msg.MissionaryPlayerId, targetPos);

            if (unitData.HasValue)
            {
                // 创建GameObject
                targetUnit = CreateUnitGameObject(msg.MissionaryPlayerId, unitData.Value);

                if (targetUnit != null)
                {
                    Debug.Log($"[网络魅惑] 成功创建目标单位GameObject");
                }
                else
                {
                    Debug.LogError($"[网络魅惑] 创建GameObject失败");
                    return;
                }
            }
            else
            {
                Debug.LogError($"[网络魅惑] 无法从PlayerDataManager获取单位数据");
                return;
            }
        }

        // 添加到新所有者字典
        if (targetUnit != null)
        {
            if (msg.MissionaryPlayerId == localPlayerId && !localPlayerUnits.ContainsKey(targetPos))
            {
                localPlayerUnits[targetPos] = targetUnit;
                Debug.Log($"[网络魅惑] 单位添加到本地玩家字典");
            }
            else
            {
                if (!otherPlayersUnits.ContainsKey(msg.MissionaryPlayerId))
                {
                    otherPlayersUnits[msg.MissionaryPlayerId] = new Dictionary<int2, GameObject>();
                }
                otherPlayersUnits[msg.MissionaryPlayerId][targetPos] = targetUnit;
                Debug.Log($"[网络魅惑] 单位添加到玩家{msg.MissionaryPlayerId}字典");
            }

            // 播放魅惑特效
            targetUnit.transform.DOPunchScale(Vector3.one * 0.3f, 0.5f, 5);

            Debug.Log($"[网络魅惑] 单位GameObject已转移 - 从玩家{msg.TargetPlayerId}到玩家{msg.MissionaryPlayerId}");
        }
        else
        {
            Debug.LogWarning("[网络魅惑] 未找到目标GameObject，但数据层转移成功");
        }

        Debug.Log($"[网络魅惑] 转移完成 - 新所有者:{msg.MissionaryPlayerId}, 原所有者:{msg.TargetPlayerId}");
    }

    /// <summary>
    /// 创建单位GameObject（用于网络同步时创建缺失的GameObject）
    /// </summary>
    /// <param name="playerId">单位所属玩家ID</param>
    /// <param name="unitData">单位数据</param>
    /// <returns>创建的GameObject，失败返回null</returns>
    private GameObject CreateUnitGameObject(int playerId, PlayerUnitData unitData)
    {
        // 获取单位类型对应的预制体
        PieceType pieceType =PlayerUnitDataInterface.Instance.ConvertCardTypeToPieceType(unitData.UnitType);

        // 使用PieceManager创建单位
        bool success = PieceManager.Instance.CreateEnemyPiece(unitData.PlayerUnitDataSO);

        if (!success)
        {
            Debug.LogError($"[CreateUnitGameObject] PieceManager创建失败: {unitData.UnitType}");
            return null;
        }

        // 获取创建的GameObject
        GameObject unitObj = PieceManager.Instance.GetPieceGameObject();

        if (unitObj == null)
        {
            Debug.LogError($"[CreateUnitGameObject] 获取GameObject失败");
            return null;
        }

        // 设置位置
        Vector3 worldPos = Vector3.zero;
        foreach (var board in PlayerBoardInforDict.Values)
        {
            if (board.Cells2DPos.Equals(unitData.Position))
            {
                worldPos = board.Cells3DPos;
                break;
            }
        }

        unitObj.transform.position = new Vector3(
            worldPos.x,
            worldPos.y + 2.5f,
            worldPos.z
        );

        // 更新GameManage的格子对象
        GameManage.Instance.SetCellObject(unitData.Position, unitObj);

        Debug.Log($"[CreateUnitGameObject] 成功创建单位: {unitData.UnitType} at ({unitData.Position.x},{unitData.Position.y})");

        return unitObj;
    }



    // 处理来自网络的魅惑过期消息
    public void HandleNetworkCharmExpire(CharmExpireMessage msg)
    {
        int2 pos = new int2(msg.PosX, msg.PosY);

        Debug.Log($"[网络魅惑过期] 单位 {msg.UnitID} 归还给玩家 {msg.OriginalOwnerId}");

        // ===== 关键修改：不删除重建GameObject，直接转移所有权 =====

        // 1. 更新同步数据中的playerID
        syncPieceData returnedData = msg.UnitSyncData;
        returnedData.currentPID = msg.OriginalOwnerId;

        // 2. 在PlayerDataManager中转移单位所有权（归还给原所有者，解除魅惑状态）
        bool transferSuccess = PlayerDataManager.Instance.TransferUnitOwnership(
            msg.CurrentOwnerId,     // 从当前控制者
            msg.OriginalOwnerId,    // 归还给原所有者
            pos,                    // 位置
            returnedData,           // 更新后的同步数据
            0                       // 魅惑回合数为0（解除魅惑）
        );

        if (!transferSuccess)
        {
            Debug.LogError("[网络魅惑过期] 转移单位所有权失败");
            return;
        }

        // 3. 更新GameObject的字典引用
        GameObject unitObj = null;

        // 从当前控制者字典中获取单位
        if (msg.CurrentOwnerId == localPlayerId && localPlayerUnits.ContainsKey(pos))
        {
            unitObj = localPlayerUnits[pos];
            localPlayerUnits.Remove(pos);
        }
        else if (otherPlayersUnits.ContainsKey(msg.CurrentOwnerId) &&
                 otherPlayersUnits[msg.CurrentOwnerId].ContainsKey(pos))
        {
            unitObj = otherPlayersUnits[msg.CurrentOwnerId][pos];
            otherPlayersUnits[msg.CurrentOwnerId].Remove(pos);
        }

        // 添加到原所有者字典
        if (unitObj != null)
        {
            if (msg.OriginalOwnerId == localPlayerId)
            {
                localPlayerUnits[pos] = unitObj;
            }
            else
            {
                if (!otherPlayersUnits.ContainsKey(msg.OriginalOwnerId))
                {
                    otherPlayersUnits[msg.OriginalOwnerId] = new Dictionary<int2, GameObject>();
                }
                otherPlayersUnits[msg.OriginalOwnerId][pos] = unitObj;
            }
            PieceManager.Instance.AddConvertedUnit(msg.OriginalOwnerId, msg.UnitID);

            Debug.Log($"[网络魅惑过期] 单位GameObject已归还 - 从玩家{msg.CurrentOwnerId}到玩家{msg.OriginalOwnerId}");
        }
        else
        {
            Debug.LogWarning("[网络魅惑过期] 未找到单位GameObject，但数据层转移成功");
        }

        Debug.Log($"[网络魅惑过期] 归还完成 - 原所有者:{msg.OriginalOwnerId}");
    }

    // 处理本地魅惑过期（不通过网络，直接在本地处理）
    public void HandleCharmExpireLocal(CharmExpireInfo expireInfo)
    {
        int2 pos = expireInfo.Position;

        Debug.Log($"[本地魅惑过期] 单位 {expireInfo.UnitID} 归还给玩家 {expireInfo.OriginalOwnerID}");

        // ===== 关键修改：不删除重建GameObject，直接转移所有权 =====

        // 1. 更新同步数据中的playerID
        syncPieceData returnedData = expireInfo.UnitData.PlayerUnitDataSO;
        returnedData.currentPID = expireInfo.OriginalOwnerID;

        // 2. 在PlayerDataManager中转移单位所有权（已经在UpdateCharmedUnits中处理了移除，这里只需添加回去）
        // 注意：UpdateCharmedUnits已经从当前玩家移除了单位，所以这里直接归还即可
        PlayerDataManager.Instance.ReturnCharmedUnit(expireInfo.OriginalOwnerID, expireInfo.UnitData);

        // 3. 更新GameObject的字典引用
        GameObject unitObj = null;

        // 从本地玩家字典中获取单位
        if (localPlayerUnits.ContainsKey(pos))
        {
            unitObj = localPlayerUnits[pos];
            localPlayerUnits.Remove(pos);
        }

        // 添加到原所有者字典
        if (unitObj != null)
        {
            if (expireInfo.OriginalOwnerID == localPlayerId)
            {
                // 这种情况不应该发生（本地玩家的单位被魅惑后过期，应该通过网络处理）
                Debug.LogWarning("[本地魅惑过期] 单位归还给本地玩家，这种情况不应该发生");
                localPlayerUnits[pos] = unitObj;
            }
            else
            {
                if (!otherPlayersUnits.ContainsKey(expireInfo.OriginalOwnerID))
                {
                    otherPlayersUnits[expireInfo.OriginalOwnerID] = new Dictionary<int2, GameObject>();
                }
                otherPlayersUnits[expireInfo.OriginalOwnerID][pos] = unitObj;
            }

            Debug.Log($"[本地魅惑过期] 单位GameObject已归还给玩家{expireInfo.OriginalOwnerID}");
        }
        else
        {
            Debug.LogWarning("[本地魅惑过期] 未找到单位GameObject，但数据层转移成功");
        }

        // 4. 网络同步魅惑过期
        SyncCharmExpire(expireInfo.UnitID, pos, expireInfo.OriginalOwnerID, returnedData);

        Debug.Log($"[本地魅惑过期] 归还完成 - 原所有者:{expireInfo.OriginalOwnerID}");
    }




    #endregion


    // 处理来自网络的移动消息
    public void HandleNetworkMove(UnitMoveMessage msg)
    {
        // 如果是自己的移动，已经在本地处理过了
        if (msg.PlayerId == localPlayerId)
        {
            Debug.Log("[网络移动] 这是本地玩家的移动，已处理");
            return;
        }

        int2 fromPos = new int2(msg.FromX, msg.FromY);
        int2 toPos = new int2(msg.ToX, msg.ToY);

        Debug.Log($"[网络移动] 玩家 {msg.PlayerId} 移动: ({fromPos.x},{fromPos.y}) -> ({toPos.x},{toPos.y})");

        if (otherPlayersUnits.ContainsKey(msg.PlayerId) &&
      otherPlayersUnits[msg.PlayerId].ContainsKey(fromPos))
        {
            GameObject movingUnit = otherPlayersUnits[msg.PlayerId][fromPos];


            // 选择位置不为空，处理教皇交换逻辑
            if (otherPlayersUnits[msg.PlayerId].ContainsKey(toPos))
            {
                GameObject targetObj = otherPlayersUnits[msg.PlayerId][toPos];

                // 更新本地gameobj字典
                otherPlayersUnits[msg.PlayerId][fromPos] = targetObj;
                otherPlayersUnits[msg.PlayerId][toPos] = movingUnit;

                // 获取目标世界坐标
                Vector3 fromWorldPos = Vector3.zero;
                foreach (var board in PlayerBoardInforDict.Values)
                {
                    if (board.Cells2DPos.Equals(fromPos))
                    {
                        fromWorldPos = new Vector3(
                            board.Cells3DPos.x,
                            board.Cells3DPos.y,
                            board.Cells3DPos.z
                        );
                        break;
                    }
                }

                // 获取目标世界坐标
                Vector3 targetWorldPos = Vector3.zero;
                foreach (var board in PlayerBoardInforDict.Values)
                {
                    if (board.Cells2DPos.Equals(toPos))
                    {
                        targetWorldPos = new Vector3(
                            board.Cells3DPos.x,
                            board.Cells3DPos.y,
                            board.Cells3DPos.z
                        );
                        break;
                    }
                }


                // 执行移动动画
                movingUnit.transform.DOMove(targetWorldPos, MoveSpeed).OnComplete(() =>
                {
                    Debug.Log($"[教皇] 移动动画完成");
                });
                // 执行移动动画
                targetObj.transform.DOMove(fromWorldPos, MoveSpeed).OnComplete(() =>
                {
                    Debug.Log($"[交换单位] 移动动画完成");
                });
            }
            else
            {
                // 更新字典
                otherPlayersUnits[msg.PlayerId].Remove(fromPos);

                otherPlayersUnits[msg.PlayerId][toPos] = movingUnit;



                // 获取目标世界坐标
                Vector3 targetWorldPos = Vector3.zero;
                foreach (var board in PlayerBoardInforDict.Values)
                {
                    if (board.Cells2DPos.Equals(toPos))
                    {
                        targetWorldPos = new Vector3(
                            board.Cells3DPos.x,
                            board.Cells3DPos.y,
                            board.Cells3DPos.z
                        );
                        break;
                    }
                }


                // 执行移动动画
                movingUnit.transform.DOMove(targetWorldPos, MoveSpeed).OnComplete(() =>
                {
                    Debug.Log($"[HandleNetworkMove] 移动动画完成");
                });

                // 更新 GameManage
                GameManage.Instance.SetCellObject(fromPos, null);
                GameManage.Instance.SetCellObject(toPos, movingUnit);

                Debug.Log($"[HandleNetworkMove] 视觉移动完成: ({fromPos.x},{fromPos.y}) -> ({toPos.x},{toPos.y})");
            }

        }
        else
        {
            Debug.LogWarning($"[HandleNetworkMove] 找不到要移动的单位: 玩家{msg.PlayerId} at ({fromPos.x},{fromPos.y})");
        }

    }

    // 处理来自网络的创建单位消息
    public void HandleNetworkAddUnit(UnitAddMessage msg)
    {
        int2 pos = new int2(msg.PosX, msg.PosY);
        CardType unitType = (CardType)msg.UnitType;

        Debug.Log($"[网络创建] 玩家 {msg.PlayerId} 创建单位: {unitType} at ({pos.x},{pos.y})");

        if (unitType == CardType.Building)
        {
            bool success = GameManage.Instance._BuildingManager.CreateEnemyBuilding((syncBuildingData)msg.BuildingData);

            if (success)
            {
                // 获取创建的GameObject
                GameObject unitObj = GameManage.Instance._BuildingManager.GetBuildingGameObject();

                if (unitObj != null)
                {
                    // 确保字典存在
                    if (!otherPlayersUnits.ContainsKey(msg.PlayerId))
                    {
                        otherPlayersUnits[msg.PlayerId] = new Dictionary<int2, GameObject>();
                    }

                    // 保存到其他玩家单位字典
                    otherPlayersUnits[msg.PlayerId][pos] = unitObj;

                    // 更新 GameManage 的格子对象
                    GameManage.Instance.SetCellObject(pos, unitObj);

                    Debug.Log($"[HandleNetworkAddUnit] 成功创建敌方建筑 ID:{msg.NewUnitSyncData.pieceID}");


                }
                else
                {
                    Debug.LogError($"[HandleNetworkAddUnit] 无法获取创建的GameObject");
                }
            }
            else
            {
                Debug.LogError($"[HandleNetworkAddUnit] PieceManager.CreateEnemyPiece 失败");
            }
        }
        // 使用 PieceManager 创建敌方棋子
        else if (PieceManager.Instance != null)
        {
            bool success = PieceManager.Instance.CreateEnemyPiece(msg.NewUnitSyncData);

            if (success)
            {
                // 获取创建的GameObject
                GameObject unitObj = PieceManager.Instance.GetPieceGameObject();

                if (unitObj != null)
                {
                    // 确保字典存在
                    if (!otherPlayersUnits.ContainsKey(msg.PlayerId))
                    {
                        otherPlayersUnits[msg.PlayerId] = new Dictionary<int2, GameObject>();
                    }

                    // 保存到其他玩家单位字典
                    otherPlayersUnits[msg.PlayerId][pos] = unitObj;

                    // 更新 GameManage 的格子对象
                    GameManage.Instance.SetCellObject(pos, unitObj);

                    Debug.Log($"[HandleNetworkAddUnit] 成功创建敌方单位 ID:{msg.NewUnitSyncData.pieceID}");


                }
                else
                {
                    Debug.LogError($"[HandleNetworkAddUnit] 无法获取创建的GameObject");
                }
            }
            else
            {
                Debug.LogError($"[HandleNetworkAddUnit] PieceManager.CreateEnemyPiece 失败");
            }
        }
        else
        {
            Debug.LogError($"[HandleNetworkAddUnit] PieceManager.Instance 为 null");
        }

    }
    public void HandleNetworkRemove(UnitRemoveMessage msg)
    {
        // 1. 跳过本地玩家的消息（本地已处理）
        if (msg.PlayerId == localPlayerId)
        {
            return;
        }

        // 2. 获取位置
        int2 pos = new int2(msg.PosX, msg.PosY);

        // 3. 从敌方单位字典找到并移除GameObject
        if (otherPlayersUnits.ContainsKey(msg.PlayerId) &&
            otherPlayersUnits[msg.PlayerId].ContainsKey(pos))
        {
            GameObject unitObj = otherPlayersUnits[msg.PlayerId][pos];
            otherPlayersUnits[msg.PlayerId].Remove(pos);

            // 4. 播放消失动画并销毁
            unitObj.transform.DOScale(Vector3.zero, 0.5f).OnComplete(() => {
                Destroy(unitObj);
            });
        }

        // 5. 从PieceManager移除
        if (msg.UnitID > 0)
        {
            PieceManager.Instance?.RemovePiece(msg.UnitID);
        }

        // 6. 清空GameManage的格子对象
        GameManage.Instance.SetCellObject(pos, null);
    }
    // 操作同步管理



    /// <summary>
    /// 本地玩家攻击建筑后调用此方法进行网络同步
    /// </summary>
    private void SyncLocalBuildingAttack(
        int2 attackerPos,
        int2 buildingPos,
        int buildingOwnerId,
        int buildingID,
        int remainingHP,
        bool buildingDestroyed)
    {
        // 检查网络连接
        if (NetGameSystem.Instance == null || !NetGameSystem.Instance.bIsConnected)
        {
            return;
        }

        // 获取攻击者数据
        PlayerUnitData? attackerData = PlayerDataManager.Instance.FindUnit(localPlayerId, attackerPos);
        if (!attackerData.HasValue)
        {
            Debug.LogWarning($"[SyncLocalBuildingAttack] 找不到攻击者数据 at ({attackerPos.x},{attackerPos.y})");
            return;
        }

        // 发送网络消息
        NetGameSystem.Instance.SendBuildingAttackMessage(
            localPlayerId,
            attackerPos,
            buildingOwnerId,
            buildingPos,
            buildingID,
            attackerData.Value.PlayerUnitDataSO,
            remainingHP,
            buildingDestroyed
        );
    }

    /// <summary>
    /// 处理建筑的摧毁逻辑
    /// </summary>
    private void DestroyInactivatedBuilding(PlayerUnitData buildingUnit)
    {
        int2 buildingPos = buildingUnit.Position;
        int buildingID = buildingUnit.UnitID;

        Debug.Log($"[建筑摧毁] 建筑 ID={buildingID} 在位置 ({buildingPos.x},{buildingPos.y}) 因未激活而被摧毁");

        // 1. 获取建筑GameObject
        GameObject buildingObj = null;
        if (localPlayerUnits.ContainsKey(buildingPos))
        {
            buildingObj = localPlayerUnits[buildingPos];
        }

        if (buildingObj != null)
        {
            // 2. 播放建筑摧毁动画
            Sequence destroySequence = DOTween.Sequence();
            destroySequence.Join(buildingObj.transform.DOScale(0f, 0.5f));
            destroySequence.Join(buildingObj.transform.DORotate(
                new Vector3(0, 360, 0), 0.5f, RotateMode.FastBeyond360));

            destroySequence.OnComplete(() =>
            {
                // 3. 销毁建筑GameObject
                Destroy(buildingObj);
                Debug.Log($"[建筑摧毁] 建筑GameObject已销毁");

                // 4. 从单位字典中移除建筑
                if (localPlayerUnits.ContainsKey(buildingPos))
                {
                    localPlayerUnits.Remove(buildingPos);
                }

                // 5. 创建废墟
                GameObject ruin = Instantiate(UnitListTable.Instance.Ruins[0],
                    GameManage.Instance.GetCell2D(buildingPos).Cells3DPos,
                    Quaternion.identity);

                // 6. 保存废墟引用
                if (!BuildingRuins.ContainsKey(localPlayerId))
                {
                    BuildingRuins[localPlayerId] = new Dictionary<int, GameObject>();
                }

                // 获取当前cell的ID
                int cellID = GameManage.Instance.GetCell2D(buildingPos).id;

                // 保存废墟
                BuildingRuins[localPlayerId][RuinID] = ruin;

                // 7. 记录cellID到PlayerDataManager (不需要网络同步)
                PlayerDataManager.Instance.AddPlayerRuinCell(cellID);

                RuinID++;

                Debug.Log($"[建筑摧毁] 废墟已创建,cellID: {cellID}");
            });
        }

        // 8. 从PlayerDataManager中移除建筑单位
        PlayerDataManager.Instance.RemoveUnit(localPlayerId, buildingPos);

        // 9. 从BuildingManager中移除建筑
        GameManage.Instance._BuildingManager.RemoveBuilding(buildingID);

        // 10. 网络同步:发送建筑摧毁消息
        SyncBuildingDestruction(buildingPos, buildingID);

        Debug.Log($"[建筑摧毁] 摧毁逻辑处理完成");
    }
    /// <summary>
    /// 同步建筑摧毁到网络
    /// </summary>
    private void SyncBuildingDestruction(int2 buildingPos, int buildingID)
    {
        // 检查网络连接
        if (NetGameSystem.Instance == null || !NetGameSystem.Instance.bIsConnected)
        {
            return; // 单机模式或未连接,不发送
        }

        // 发送网络消息
        NetGameSystem.Instance.SendBuildingDestructionMessage(
            localPlayerId,
            buildingPos,
            buildingID
        );

        Debug.Log($"[SyncBuildingDestruction] 已发送建筑摧毁同步消息");
    }

    /// <summary>
    /// 本地玩家攻击后调用此方法进行网络同步
    /// 在攻击完成后调用
    /// 注意:在新的逻辑中,攻击者必须已在相邻格,不会发生移动
    /// </summary>
    private void SyncLocalUnitAttack(int2 attackerPos, int2 targetPos, int targetPlayerId, bool targetDestroyed)
    {
        // 检查网络连接
        if (NetGameSystem.Instance == null || !NetGameSystem.Instance.bIsConnected)
        {
            return; // 单机模式或未连接，不发送
        }

        // 获取攻击者数据
        PlayerUnitData? attackerData = PlayerDataManager.Instance.FindUnit(localPlayerId, attackerPos);

        if (!attackerData.HasValue)
        {
            Debug.LogWarning($"[SyncLocalUnitAttack] 找不到攻击者数据 at ({attackerPos.x},{attackerPos.y})");
            return;
        }

        // 获取目标数据（如果存活）
        PlayerUnitData? targetData = null;
       
            targetData = PlayerDataManager.Instance.FindUnit(targetPlayerId, targetPos);
        

        // 发送网络消息 - 攻击者位置不变,所以不传递移动参数
        NetGameSystem.Instance.SendUnitAttackMessage(
            localPlayerId,
            attackerPos,
            targetPlayerId,
            targetPos,
            attackerData.Value.PlayerUnitDataSO,
            targetData?.PlayerUnitDataSO,
            targetDestroyed
        // 不传递 attackerOriginalPos 和 hasMoved 参数,使用默认值(无移动)
        );

        Debug.Log($"[SyncLocalUnitAttack] 已发送攻击同步消息，攻击者位置: ({attackerPos.x},{attackerPos.y}), 目标摧毁: {targetDestroyed}");
    }

    private void SyncFarmerEnterBuilding(int farmerID, int2 buildingPos)
    {
        // 检查网络连接
        if (NetGameSystem.Instance == null || !NetGameSystem.Instance.bIsConnected)
        {
            return; // 单机模式或未连接，不发送
        }

        // 发送单位移除消息
        NetGameSystem.Instance.SendUnitRemoveMessage(
            localPlayerId,
            buildingPos,
            farmerID
        );

        Debug.Log($"[SyncFarmerEnterBuilding] 已发送农民消失同步消息");
    }

    /// <summary>
    /// 本地玩家魅惑单位后调用此方法进行网络同步
    /// </summary>
    private void SyncLocalUnitCharm(int missionaryID, int2 missionaryPos, int targetID, int targetOwnerId, int2 targetPos, syncPieceData newUnitSyncData)
    {
        // 检查网络连接
        if (NetGameSystem.Instance == null || !NetGameSystem.Instance.bIsConnected)
        {
            return; // 单机模式或未连接，不发送
        }

        // 发送网络消息
        NetGameSystem.Instance.SendUnitCharmMessage(
            localPlayerId,
            missionaryID,
            missionaryPos,
            targetOwnerId,
            targetID,
            targetPos,
            newUnitSyncData,
            3  // 魅惑持续3回合
        );

        Debug.Log($"[SyncLocalUnitCharm] 已发送魅惑同步消息");
    }

    // 同步Pope交换位置到网络
    private void SyncPopeSwapPosition(int2 popePos, int2 targetPos, syncPieceData popeSyncData, syncPieceData targetSyncData)
    {
        // 检查网络连接
        if (NetGameSystem.Instance == null || !NetGameSystem.Instance.bIsConnected)
        {
            return; // 单机模式或未连接，不发送
        }


        // 发送网络消息 - 移动
        if (NetGameSystem.Instance != null)
        {
            UnitMoveMessage moveMsg = new UnitMoveMessage
            {
                PlayerId = localPlayerId,
                FromX = popePos.x,
                FromY = popePos.y,
                ToX = targetPos.x,
                ToY = targetPos.y
            };
            NetGameSystem.Instance.SendMessage(NetworkMessageType.UNIT_MOVE, moveMsg);
        }

        //// 发送网络消息 - 移动
        //if (NetGameSystem.Instance != null)
        //{
        //    UnitMoveMessage moveMsg = new UnitMoveMessage
        //    {
        //        PlayerId = localPlayerId,
        //        FromX = targetPos.x,
        //        FromY = targetPos.y,
        //        ToX = popePos.x,
        //        ToY = popePos.y
        //    };
        //    NetGameSystem.Instance.SendMessage(NetworkMessageType.UNIT_MOVE, moveMsg);
        //}


        //// 发送两条移动消息来表示交换
        //// 消息1: Pope移动到目标位置
        //NetGameSystem.Instance.SendUnitMoveMessage(
        //    localPlayerId,
        //    popePos,
        //    targetPos,
        //    popeSyncData
        //);

        //// 消息2: 目标单位移动到Pope原来的位置
        //NetGameSystem.Instance.SendUnitMoveMessage(
        //    localPlayerId,
        //    targetPos,
        //    popePos,
        //    targetSyncData
        //);

        Debug.Log($"[SyncPopeSwapPosition] 已发送Pope交换位置同步消息");
    }


    /// <summary>
    /// 本地玩家魅惑过期后调用此方法进行网络同步
    /// </summary>
    private void SyncCharmExpire(int unitID, int2 pos, int originalOwnerId, syncPieceData unitSyncData)
    {
        // 检查网络连接
        if (NetGameSystem.Instance == null || !NetGameSystem.Instance.bIsConnected)
        {
            return;
        }

        // 发送网络消息
        NetGameSystem.Instance.SendCharmExpireMessage(
            localPlayerId,
            originalOwnerId,
            unitID,
            pos,
            unitSyncData
        );

        Debug.Log($"[SyncCharmExpire] 已发送魅惑过期同步消息");
    }






    private void OnUnitAddedHandler(int playerId, PlayerUnitData unitData)
    {

    }

    private void OnUnitRemovedHandler(int playerId, int2 position, bool isCharm)
    {
        Debug.Log($"[事件] OnUnitRemovedHandler: 玩家 {playerId} at ({position.x},{position.y})");

        if (playerId == localPlayerId)
        {
            Debug.Log("[事件] 本地玩家移除单位");
            // 本地玩家移除单位（发生在被攻击时）
            if (localPlayerUnits.ContainsKey(position) && !isCharm)
            {
                Destroy(localPlayerUnits[position]);
                localPlayerUnits.Remove(position);
            }
            else
            {
                Destroy(localPlayerUnits[position]);
                localPlayerUnits.Remove(position);

                // 更新魅惑后的新显示
                UpdateOtherPlayerDisplay(localPlayerId == 1 ? 0 : 1, PlayerDataManager.Instance.GetPlayerData(localPlayerId == 1 ? 0 : 1));
            }
            GameManage.Instance.SetCellObject(position, null);
        }
        else
        {
            Debug.Log("[事件] 其他玩家移除单位由 HandleNetworkAttack 处理，跳过");

            return;
        }
    }

    private void OnUnitMovedHandler(int playerId, int2 fromPos, int2 toPos)
    {

    }



    // *************************
    //        清理
    // *************************

    private void OnDestroy()
    {
        // 取消订阅事件
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnUnitAdded -= OnUnitAddedHandler;
            PlayerDataManager.Instance.OnUnitRemoved -= OnUnitRemovedHandler;
            PlayerDataManager.Instance.OnUnitMoved -= OnUnitMovedHandler;
        }
    }
}