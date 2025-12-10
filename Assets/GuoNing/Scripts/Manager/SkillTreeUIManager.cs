using GameData;
using GameData.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Playables;
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

	[Header("LevelUpPanel")]
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
    public Dictionary<PieceType, SkillLevelData> UnitSkillLevels = new Dictionary<PieceType, SkillLevelData>();   // 所有棋子的所有技能的等级

	private void Awake()
	{
		if (Instance == null) Instance = this;
		else Destroy(gameObject);
	}

	private void Start()
	{
		Initialize();



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

		PaddingLeft = -110;

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

        PaddingLeft = -230;

        // HP
        int hpLevel = GetActualLevelCount(dataSO.maxHPByLevel);
		CreateBranch(MissionaryTransform, TechTree.HP, PieceType.Missionary, hpLevel, index++);

		// AP
		int apLevel = GetActualLevelCount(dataSO.maxAPByLevel);
		CreateBranch(MissionaryTransform, TechTree.AP, PieceType.Missionary, apLevel, index++);

		// Occupy
		int occupyLevel = GetActualLevelCount(realData.occupyEmptySuccessRateByLevel);
		CreateBranch(MissionaryTransform,TechTree.Occupy, PieceType.Missionary, occupyLevel, index++);

        // Charm（传教士独有）
        int charmLevel = GetActualLevelCount(realData.convertMissionaryChanceByLevel);
        CreateBranch(MissionaryTransform, TechTree.Conversion, PieceType.Missionary, charmLevel, index++);
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

        PaddingLeft = -170;

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

        PaddingLeft = -170;

        // HP
        int hpLevel = GetActualLevelCount(dataSO.maxHPByLevel);
		CreateBranch(SoliderTransform, TechTree.HP, PieceType.Military, hpLevel, index++);

		// AP
		int apLevel = GetActualLevelCount(dataSO.maxAPByLevel);
		CreateBranch(SoliderTransform, TechTree.AP, PieceType.Military, apLevel, index++);

		
		int atkLevel = GetActualLevelCount(dataSO.attackPowerByLevel);
		CreateBranch(SoliderTransform, TechTree.ATK, PieceType.Military, atkLevel, index++);

	}

    //============================================================
    // 5   建筑 Building
    //============================================================
    private void CreateBuildingSkillTree()
	{
		var dataSO =
			GameManage.Instance._BuildingManager.GetBuildingDataByReligion(SceneStateManager.Instance.PlayerReligion);
		int index = 0;

        PaddingLeft = -110;

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
				return UISpriteHelper.Instance.GetSubSprite(UISpriteID.Icon_SkillIcon, "03attack");
			case TechTree.HP:
				return UISpriteHelper.Instance.GetSubSprite(UISpriteID.Icon_SkillIcon, "01hp");
			case TechTree.AP:
				return UISpriteHelper.Instance.GetSubSprite(UISpriteID.Icon_SkillIcon, "02action");
			case TechTree.AltarCount:
				return UISpriteHelper.Instance.GetSubSprite(UISpriteID.Icon_SkillIcon, "07altar");
			case TechTree.Conversion:
				return UISpriteHelper.Instance.GetSubSprite(UISpriteID.Icon_SkillIcon, "04missionary");
			case TechTree.Occupy:
				return UISpriteHelper.Instance.GetSubSprite(UISpriteID.Icon_SkillIcon, "06occupation");
			case TechTree.MovementCD:
				return UISpriteHelper.Instance.GetSubSprite(UISpriteID.Icon_SkillIcon, "02action");
			case TechTree.Sacrifice:
				return UISpriteHelper.Instance.GetSubSprite(UISpriteID.Icon_SkillIcon, "05service");
            default:
                return UISpriteHelper.Instance.GetSubSprite(UISpriteID.Icon_SkillIcon, "08lock");
        }

		return null;
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

	public void UpdateSimpleSkillPanel(PieceType type)
	{
		if (SimpleSkillPanels.TryGetValue(type, out var panel))
		{
			panel.Refresh();
		}
	}

    //增加树刷新的对外接口
    public void RefreshSkillTreeByPieceType(PieceType pieceType)
	{
        switch (pieceType)
        {
            case PieceType.Pope:
                for (int i = PopeTransform.childCount - 1; i >= 0; i--)
                {
                    Transform child = PopeTransform.GetChild(i);

                    if (child.name.StartsWith("Skill Branch"))
                    {
                        Destroy(child.gameObject);
                    }
                }
				CreatePopeSkillTree();

                break;
            case PieceType.Missionary:
                for (int i = MissionaryTransform.childCount - 1; i >= 0; i--)
                {
                    Transform child = MissionaryTransform.GetChild(i);

                    if (child.name.StartsWith("Skill Branch"))
                    {
                        Destroy(child.gameObject);
                    }
                }
				CreateMissionarySkillTree();

                break;
            case PieceType.Military:
                for (int i = SoliderTransform.childCount - 1; i >= 0; i--)
                {
                    Transform child = SoliderTransform.GetChild(i);

                    if (child.name.StartsWith("Skill Branch"))
                    {
                        Destroy(child.gameObject);
                    }
                }
				CreateSoldierSkillTree();

                break;
            case PieceType.Farmer:
                for (int i = FarmerTransform.childCount - 1; i >= 0; i--)
                {
                    Transform child = FarmerTransform.GetChild(i);

                    if (child.name.StartsWith("Skill Branch"))
                    {
                        Destroy(child.gameObject);
                    }
                }
				CreateFarmerSkillTree();

                break;
            case PieceType.Building:
                for (int i = BuildingTransform.childCount - 1; i >= 0; i--)
                {
                    Transform child = BuildingTransform.GetChild(i);

                    if (child.name.StartsWith("Skill Branch"))
                    {
                        Destroy(child.gameObject);
                    }
                }
				CreateBuildingSkillTree();

                break;

            default:
                break;
	
	
        }
	
	
	
	
    }


    //技能说明生成
    private string GetSingleLevelUpInfo(TechTree tech, PieceType pieceType, bool isUpdate=false)
	{
        int lv = GetCurrentLevel(pieceType, tech);
        int num;
        int next;

        string techName;
        string status;
		string fullString = "";
        techName = PlayerUnitDataInterface.Instance.GetTechNameByTechTree(tech)+" : ";

        UnitListTable.PieceDetail pd =
        new UnitListTable.PieceDetail(pieceType, SceneStateManager.Instance.PlayerReligion);

        if (pieceType == PieceType.Building)
        {
            var BuildingData = GameManage.Instance._BuildingManager
                .GetBuildingDataByReligion(SceneStateManager.Instance.PlayerReligion);
            switch (tech)
            {
                case TechTree.HP:
                    num = BuildingData.GetMaxHpByLevel(lv);

					if(isUpdate)
					{
                        next = BuildingData.GetMaxHpByLevel(lv + 1);
                        status = $"{num}"+ $"<color=#FF0000>+{next-num}</color>";
                    }
                    else
                    {
                        status = $"{num}";
                    }
                    break;
                case TechTree.AltarCount:// "開拓者数";
                    num = BuildingData.GetMaxSlotsByLevel(lv);

                    if (isUpdate)
                    {
                        next = BuildingData.GetMaxSlotsByLevel(lv+1);
                        status = $"{num}" + $"<color=#FF0000>+{next - num}</color>";
                    }
                    else
                    {
                        status = $"{num}";
                    }
                    break;
                case TechTree.AP:// "行動力";
                case TechTree.Occupy:// "占領確率";
                case TechTree.Conversion:// "伝教確率";
                case TechTree.ATK:// "攻撃力";
                case TechTree.Sacrifice:// "奉仕";
                case TechTree.AttackPosition:// "攻撃口";
                case TechTree.ConstructionCost:// "建設費用";
                case TechTree.MovementCD:// "移動CD";
                case TechTree.Buff:// "周囲バフ";//真理研究所　无Buff
                case TechTree.Heresy:// "異端邪説";
                default:
                    return "?";
            }

        }
        else
        {
            var dataSO = UnitListTable.Instance.GetPieceDataSO(pieceType, pd);
            switch (tech)
            {
                case TechTree.HP:
					num=dataSO.maxHPByLevel[lv];
                    if (isUpdate)
                    {
                        int nextLv = Mathf.Clamp(lv + 1, 0, dataSO.maxHPByLevel.Length - 1);
                        next = dataSO.maxHPByLevel[nextLv];
                        int delta = next - num;

                        status = $"{num} <color=#FF0000>+{delta}</color>";
                    }
                    else
                    {
                        status = $"{num}";
                    }
                    break;
                case TechTree.AP:// "行動力";
                    num = dataSO.maxAPByLevel[lv];
                    if (isUpdate)
                    {
                        int nextLv = Mathf.Clamp(lv + 1, 0, dataSO.maxAPByLevel.Length - 1);
                        next = dataSO.maxAPByLevel[nextLv];
                        int delta = next - num;

                        status = $"{num} <color=#FF0000>+{delta}</color>";
                    }
                    else
                    {
                        status = $"{num}";
                    }
                    break;
                case TechTree.Occupy:// "占領確率";

                    if (isUpdate)
					{
                        var MissionaryData = dataSO as MissionaryDataSO;
                        int maxlv = GetActualLevelCount(MissionaryData.occupyEmptySuccessRateByLevel) - 1;
                        next = Mathf.Clamp(lv + 1, 0, maxlv);

                        status = $"<color=#FF0000>Lv.{next + 1}</color>";
                    }
					else
					{
                        status = $"Lv.{lv+1}";
                    }
                    break;
                case TechTree.Conversion:// "洗脳確率";

                    if (isUpdate)
                    {
                        var MissionaryData = dataSO as MissionaryDataSO;
                        int maxlv = GetActualLevelCount(MissionaryData.convertMissionaryChanceByLevel) - 1;
                        next = Mathf.Clamp(lv + 1, 0, maxlv);

                        status = $"<color=#FF0000>Lv.{next + 1}</color>";
                    }
                    else
                    {
                        status = $"Lv.{lv + 1}";
                    }
                    break;
                case TechTree.ATK:// "攻撃力";
                    num = dataSO.attackPowerByLevel[lv];
                    if (isUpdate)
                    {
                        int nextLv = Mathf.Clamp(lv + 1, 0, dataSO.attackPowerByLevel.Length - 1);
                        next = dataSO.attackPowerByLevel[nextLv];
                        int delta = next - num;

                        status = $"{num} <color=#FF0000>+{delta}</color>";
                    }
                    else
                    {
                        status = $"{num}";
                    }
                    break;
                case TechTree.Sacrifice:// "奉仕";
                    var FarmerData = dataSO as FarmerDataSO;
                    if (FarmerData == null)
                    {
                        status = "Error: Not FarmerDataSO";
                        break;
                    }
                    num = FarmerData.maxSacrificeLevel[lv];
                    if (isUpdate)
                    {
                        int nextLv = Mathf.Clamp(lv + 1, 0, FarmerData.maxSacrificeLevel.Length - 1);
                        next = FarmerData.maxSacrificeLevel[nextLv];
                        int delta = next - num;

                        status = $"HP {num} <color=#FF0000>+{delta}</color> 回復";
                    }
                    else
                    {
                        status = $"HP {num} 回復";
                    }
                    break;

                case TechTree.MovementCD:// "移動CD";
                    var PopeData = dataSO as PopeDataSO;
                    num = PopeData.swapCooldown[lv];
                    if (isUpdate)
                    {
                        int nextLv = Mathf.Clamp(lv + 1, 0, PopeData.swapCooldown.Length - 1);
                        next = PopeData.swapCooldown[nextLv];

                        status = $"<color=#FF0000>{next}</color> ターン";
                    }
                    else
                    {
                        status = $"{num} ターン";
                    }
                    break;
                case TechTree.Buff:// "周囲バフ";真理研究所　无Buff
                    string buffname = PlayerUnitDataInterface.Instance.GetBuffNameByReligion(SceneStateManager.Instance.PlayerReligion);
                    if (isUpdate)
                    {
                        int maxlv = 2;
                        next = Mathf.Clamp(lv + 1, 0, maxlv);

                        status = $"<color=#FF0000>Lv.{next + 1}</color>";
                    }
                    else
                    {
                        status = $"Lv.{lv + 1}";
                    }
                    break;
                case TechTree.Heresy:// "異端邪説";
                case TechTree.AttackPosition:// "攻撃口";
                case TechTree.AltarCount:// "開拓者数";
                case TechTree.ConstructionCost:// "建設費用";
                default:
                    status = "";
                    break;
            }

        }


        fullString = techName+ status;




        return fullString;
	}

    public string GetLevelUpInfo(PieceType pieceType, TechTree updateTech = TechTree.None)
	{
		string str01 = "";
		string str02 = "";
		string str03 = "";
		string str04 = "";

		string fulltext;

        switch (pieceType)
        {
            case PieceType.Farmer://return "市民";
				str01 = GetSingleLevelUpInfo(TechTree.HP, PieceType.Farmer, updateTech == TechTree.HP);
                str02 = GetSingleLevelUpInfo(TechTree.AP, PieceType.Farmer, updateTech == TechTree.AP);
                str03 = GetSingleLevelUpInfo(TechTree.Sacrifice, PieceType.Farmer, updateTech == TechTree.Sacrifice);

				fulltext = str01 + "\n" + str02 + "\n" + str03;
                break;

            case PieceType.Military://return "守護者";
                str01 = GetSingleLevelUpInfo(TechTree.HP, PieceType.Military, updateTech == TechTree.HP);
                str02 = GetSingleLevelUpInfo(TechTree.AP, PieceType.Military, updateTech == TechTree.AP);
                str03 = GetSingleLevelUpInfo(TechTree.ATK, PieceType.Military, updateTech == TechTree.ATK);

                fulltext = str01 + "\n" + str02 + "\n" + str03;
				break;
            case PieceType.Missionary://return "启蒙者";
                str01 = GetSingleLevelUpInfo(TechTree.HP, PieceType.Missionary, updateTech == TechTree.HP);
                str02 = GetSingleLevelUpInfo(TechTree.AP, PieceType.Missionary, updateTech == TechTree.AP);
                str03 = GetSingleLevelUpInfo(TechTree.Occupy, PieceType.Missionary, updateTech == TechTree.Occupy);
                str04 = GetSingleLevelUpInfo(TechTree.Conversion, PieceType.Missionary, updateTech == TechTree.Conversion);
                fulltext = str01 + "\n" + str02 + "\n" + str03 + "\n" + str04;

                break;
            case PieceType.Pope://return "统帅者";
                str01 = GetSingleLevelUpInfo(TechTree.HP, PieceType.Pope, updateTech == TechTree.HP);
                str02 = GetSingleLevelUpInfo(TechTree.MovementCD, PieceType.Pope, updateTech == TechTree.MovementCD);
                str03 = GetSingleLevelUpInfo(TechTree.Buff, PieceType.Pope, updateTech == TechTree.Buff);

                fulltext = str01 + "\n" + str02 + "\n" + str03;
                break;
            case PieceType.Building:// return "建物";
                str01 = GetSingleLevelUpInfo(TechTree.HP, PieceType.Building, updateTech == TechTree.HP);
                str02 = GetSingleLevelUpInfo(TechTree.AltarCount, PieceType.Building, updateTech == TechTree.AltarCount);

                fulltext = str01 + "\n" + str02;
                break;
            default:
				fulltext = "";
                break;
        }


		return fulltext;

    }

}
