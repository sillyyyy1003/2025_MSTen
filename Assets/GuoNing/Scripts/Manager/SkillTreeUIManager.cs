using GameData;
using GameData.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;



[Serializable]
public class SkillLevelData
{
	public Dictionary<TechTree, int> currentLevels = new Dictionary<TechTree, int>();
}
// 负责技能树面板的生成
public class SkillTreeUIManager : MonoBehaviour
{
	[Header("Prefabs")]
	public SkillNode skillNodePrefab;
    public SkillBranch skillBranchprefab;
    public SimpleSkillPanel simpleSkillPanel;

	[Header("SimplePanelRoot")]
	public RectTransform popePanelRoot;
	public RectTransform missionaryPanelRoot;
	public RectTransform farmerPanelRoot;
	public RectTransform soldierPanelRoot;
	public RectTransform buildingPanelRoot;

	[Header("SkillPanel")]
	public RectTransform PopeTransform;
	public RectTransform MissionaryTransform;
	public RectTransform FarmerTransform;
	public RectTransform SoliderTransform;
	public RectTransform BuildingTransform;

	[Header("NaviToggle")]
	public Toggle PopeNavi;
	public Toggle MissionaryNavi;
	public Toggle FarmerNavi;
	public Toggle SoliderNavi;
	public Toggle BuildingNavi;

	public RectTransform levelUpInfoPanel;
	public LevelUpButton levelUpbutton;

	[Header("SkillBranchLayout")]
	public float VerticalSpacing;		// 每个技能树的横向间距
	public float HorizontalSpacing;		// 每个节点的纵向间距
	public Vector3 StartPos = Vector3.zero;
	public float PaddingLeft = 80f;
	public float PaddingRight = 80f;
	public float PaddingTop = 80f;
	public float PaddingBottom = 80f;
	public static SkillTreeUIManager Instance { get; private set; }

	public Dictionary<PieceType, SimpleSkillPanel> SimpleSkillPanels = new Dictionary<PieceType, SimpleSkillPanel>();
    //public Dictionary<CardType, UnitSkillTree> allSkillTrees;
    public Dictionary<PieceType, SkillLevelData> UnitSkillLevels = new Dictionary<PieceType, SkillLevelData>();   // 所有棋子的所有技能的等级

	private void Awake()
	{
		if (Instance == null) Instance = this;
		else Destroy(gameObject);
	}

	private void Start()
	{
		Initialize();

		// 注册事件
		PopeNavi.onValueChanged.AddListener((isOn) => OnToggleChanged(PieceType.Pope, isOn));
		MissionaryNavi.onValueChanged.AddListener((isOn) => OnToggleChanged(PieceType.Missionary, isOn));
		FarmerNavi.onValueChanged.AddListener((isOn) => OnToggleChanged(PieceType.Farmer, isOn));
		SoliderNavi.onValueChanged.AddListener((isOn) => OnToggleChanged(PieceType.Military, isOn));
		BuildingNavi.onValueChanged.AddListener((isOn)=>OnToggleChanged(PieceType.Building,isOn));

		// 默认显示 Pope
		OnToggleChanged(PieceType.Pope, true);

		levelUpInfoPanel.gameObject.SetActive(false);
	}
	public void Initialize()
    {

		InitSkillLevel(PieceType.Pope);
		InitSkillLevel(PieceType.Missionary);
		InitSkillLevel(PieceType.Farmer);
		InitSkillLevel(PieceType.Military);
		InitSkillLevel(PieceType.Building);

		CreatePopeSkillTree();
		CreateMissionarySkillTree();
		CreateFarmerSkillTree();
		CreateSoldierSkillTree();
		CreateBuildingSkillTree();

		SimpleSkillPanels[PieceType.Pope] = Instantiate(simpleSkillPanel, popePanelRoot);
		SimpleSkillPanels[PieceType.Pope].Initialize(PieceType.Pope);

		SimpleSkillPanels[PieceType.Missionary] = Instantiate(simpleSkillPanel, missionaryPanelRoot);
		SimpleSkillPanels[PieceType.Missionary].Initialize(PieceType.Missionary);

		SimpleSkillPanels[PieceType.Farmer] = Instantiate(simpleSkillPanel, farmerPanelRoot);
		SimpleSkillPanels[PieceType.Farmer].Initialize(PieceType.Farmer);

		SimpleSkillPanels[PieceType.Military] = Instantiate(simpleSkillPanel, soldierPanelRoot);
		SimpleSkillPanels[PieceType.Military].Initialize(PieceType.Military);
		
		SimpleSkillPanels[PieceType.Building] = Instantiate(simpleSkillPanel, buildingPanelRoot);
		SimpleSkillPanels[PieceType.Building].Initialize(PieceType.Building);



	}

	//============================================================
	// 1️⃣ 教皇 Pope
	//============================================================
	private void CreatePopeSkillTree()
	{
		UnitListTable.PieceDetail pd =
			new UnitListTable.PieceDetail(PieceType.Pope, SceneStateManager.Instance.PlayerReligion);

		var dataSO = UnitListTable.Instance.GetPieceDataSO(PieceType.Pope,pd);

		int index = 0;

		// HP
		int hpLevel = GetActualLevelCount(dataSO.maxHPByLevel);
		CreateBranch(PopeTransform, TechTree.HP, PieceType.Pope, hpLevel, index++);

		// AP (MovementCD)
		int cdLevel = GetActualLevelCount(dataSO.maxAPByLevel);
		CreateBranch(PopeTransform, TechTree.MovementCD, PieceType.Pope, cdLevel, index++);

		
	}

	//============================================================
	// 2️⃣ 传教士 Missionary
	//============================================================
	private void CreateMissionarySkillTree()
	{
		UnitListTable.PieceDetail pd =
			new UnitListTable.PieceDetail(PieceType.Missionary, SceneStateManager.Instance.PlayerReligion);

		var dataSO = UnitListTable.Instance.GetPieceDataSO(PieceType.Missionary, pd);
		var realData = dataSO as MissionaryDataSO;

		int index = 0;

		// HP
		int hpLevel = GetActualLevelCount(dataSO.maxHPByLevel);
		CreateBranch(MissionaryTransform, TechTree.HP, PieceType.Missionary, hpLevel, index++);

		// AP
		int apLevel = GetActualLevelCount(dataSO.maxAPByLevel);
		CreateBranch(MissionaryTransform, TechTree.AP, PieceType.Missionary, apLevel, index++);

		// Charm（传教士独有）
		int charmLevel = GetActualLevelCount(realData.convertMissionaryChanceByLevel);
		CreateBranch(MissionaryTransform, TechTree.Conversion, PieceType.Missionary, charmLevel, index++);

		// Occupy
		int occupyLevel = GetActualLevelCount(realData.occupyEmptySuccessRateByLevel);
		CreateBranch(MissionaryTransform,TechTree.Occupy, PieceType.Missionary, occupyLevel, index++);
	}

	//============================================================
	// 3️⃣ 农夫 Farmer
	//============================================================
	private void CreateFarmerSkillTree()
	{
		UnitListTable.PieceDetail pd =
			new UnitListTable.PieceDetail(PieceType.Farmer, SceneStateManager.Instance.PlayerReligion);

		var dataSO = UnitListTable.Instance.GetPieceDataSO(PieceType.Farmer, pd);
		var realData = dataSO as FarmerDataSO;
		int index = 0;

		// HP
		int hpLevel = GetActualLevelCount(dataSO.maxHPByLevel);
		CreateBranch(FarmerTransform, TechTree.HP, PieceType.Farmer, hpLevel, index++);

		// AP
		int apLevel = GetActualLevelCount(dataSO.maxAPByLevel);
		CreateBranch(FarmerTransform, TechTree.AP, PieceType.Farmer, apLevel, index++);

		// Sacrifice
		int ocLevel = GetActualLevelCount(realData.maxSacrificeLevel);
		CreateBranch(FarmerTransform, TechTree.Sacrifice, PieceType.Farmer, ocLevel, index++);

	}

	//============================================================
	// 4️⃣ 士兵 Soldier
	//============================================================
	private void CreateSoldierSkillTree()
	{
		UnitListTable.PieceDetail pd =
			new UnitListTable.PieceDetail(PieceType.Military, SceneStateManager.Instance.PlayerReligion);

		var dataSO = UnitListTable.Instance.GetPieceDataSO(PieceType.Military, pd);
		int index = 0;

		// HP
		int hpLevel = GetActualLevelCount(dataSO.maxHPByLevel);
		CreateBranch(SoliderTransform, TechTree.HP, PieceType.Military, hpLevel, index++);

		// AP
		int apLevel = GetActualLevelCount(dataSO.maxAPByLevel);
		CreateBranch(SoliderTransform, TechTree.AP, PieceType.Military, apLevel, index++);

		
		int atkLevel = GetActualLevelCount(dataSO.attackPowerByLevel);
		CreateBranch(SoliderTransform, TechTree.ATK, PieceType.Military, atkLevel, index++);

	}

	private void CreateBuildingSkillTree()
	{
		var dataSO =
			GameManage.Instance._BuildingManager.GetBuildingDataByReligion(SceneStateManager.Instance.PlayerReligion);
		int index = 0;

		// HP
		int hpLevel = GetActualLevelCount(dataSO.maxHpByLevel);
		CreateBranch(BuildingTransform, TechTree.HP, PieceType.Building, hpLevel, index++);

		// Slot
		int slotLevel = GetActualLevelCount(dataSO.maxSlotsByLevel);
		CreateBranch(BuildingTransform, TechTree.AltarCount, PieceType.Building, slotLevel, index++);


	}


	//============================================================
	// 工具函数：创建分支 + 自动水平布局
	//============================================================
	private void CreateBranch(RectTransform parent, TechTree tech, PieceType pieceType, int maxLevel, int index)
	{
		// 1. 创建分支
		SkillBranch branch = Instantiate(skillBranchprefab, parent);

		// 2. 节点图片
		Sprite sprite = GetSprite(tech);

		// 设定纵向布局
		float nodeHeight = skillNodePrefab.GetComponent<RectTransform>().rect.height;
		float nodeSpacing = nodeHeight + VerticalSpacing;

		branch.Initialize(sprite, tech, pieceType, maxLevel, nodeSpacing, levelUpInfoPanel, levelUpbutton);

		if (!UnitSkillLevels[pieceType].currentLevels.ContainsKey(tech))
			UnitSkillLevels[pieceType].currentLevels[tech] = 0;

		// 4. 直接按 index 排列（使用 padding）
		float branchWidth = ((RectTransform)branch.transform).rect.width;

		// 横向位置计算：padding + width/2 + index*(width + spacing)
		float x = PaddingLeft + branchWidth * 0.5f + index * (branchWidth + HorizontalSpacing);

		// 设置位置
		RectTransform rt = branch.GetComponent<RectTransform>();
		rt.anchoredPosition = new Vector2(x, -PaddingTop);
	}



	private int GetActualLevelCount<T>(T[] arr)
	{
		if (arr == null || arr.Length == 0)
			return 0;

		int count = 1;

		var comparer = EqualityComparer<T>.Default;

		for (int i = 1; i < arr.Length; i++)
		{
			if (comparer.Equals(arr[i], arr[i - 1]))
				break;

			count++;
		}

		return count;
	}

	Sprite GetSprite(TechTree type)
	{
		switch (type)
		{
			case TechTree.ATK:


			default:return null;
		}
	}
	private void OnToggleChanged(PieceType type, bool isOn)
	{
		if (!isOn) return; // 只有 toggle 被选中时处理

		// 全部隐藏
		PopeTransform.gameObject.SetActive(false);
		MissionaryTransform.gameObject.SetActive(false);
		FarmerTransform.gameObject.SetActive(false);
		SoliderTransform.gameObject.SetActive(false);
		BuildingTransform.gameObject.SetActive(false);

		// 显示对应 Panel
		switch (type)
		{
			case PieceType.Pope:
				PopeTransform.gameObject.SetActive(true);
				break;

			case PieceType.Missionary:
				MissionaryTransform.gameObject.SetActive(true);
				break;

			case PieceType.Farmer:
				FarmerTransform.gameObject.SetActive(true);
				break;

			case PieceType.Military:
				SoliderTransform.gameObject.SetActive(true);
				break;

			case PieceType.Building:
				BuildingTransform.gameObject.SetActive(true);
				break;
		}
	}

	private void InitSkillLevel(PieceType type)
	{
		UnitSkillLevels[type] = new SkillLevelData()
		{
			currentLevels = new Dictionary<TechTree, int>()
		};

	}

	public int GetCurrentLevel(PieceType type, TechTree tech)
	{
		if (UnitSkillLevels.TryGetValue(type, out var data))
		{
			if (data.currentLevels.TryGetValue(tech, out int lv))
				return lv;
		}
		return 0;
	}

	public void UpgradeCurrentLevel(PieceType type, TechTree tech)
	{
		if (!UnitSkillLevels.TryGetValue(type, out var data))
		{
			Debug.LogWarning("No such type!");
			return;
		}


		if (!data.currentLevels.TryGetValue(tech, out int lv))
			lv = 0;

		lv += 1; // 等级 +1

		data.currentLevels[tech] = lv;   // 写回

		UnitSkillLevels[type] = data;    // struct 必须写回整个 data！

		Debug.Log("[SkillTreeManager]Upgraded!");
	}
}
