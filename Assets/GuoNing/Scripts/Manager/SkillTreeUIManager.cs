using GameData;
using GameData.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public struct UnitSkillTree
{
    public Dictionary<TechTree, SkillBranch> unitSkills;
}
// 负责技能树面板的生成
public class SkillTreeUIManager : MonoBehaviour
{
    public SkillBranch prefab;
	[Header("Spacing Settings")]
	public float HorizontalSpacing = 200f;
	public Vector3 StartPos	= Vector3.zero;

	public RectTransform PopeTranform;
	public RectTransform MissionaryTransform;
	public RectTransform FarmerTransform;
	public RectTransform SoliderTransform;

	public RectTransform levelUpInfoPanel;
	public LevelUpButton levelUpbutton;

	public static SkillTreeUIManager Instance { get; private set; }
    public Dictionary<CardType, UnitSkillTree> allSkillTrees;

	private void Start()
	{
		Initialize();
	}
	public void Initialize()
    {
		CreatePopeSkillTree();
		CreateMissionarySkillTree();
		CreateFarmerSkillTree();
		CreateSoldierSkillTree();


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
		CreateBranch(PopeTranform, TechTree.HP, hpLevel, index++);

		// AP (MovementCD)
		int cdLevel = GetActualLevelCount(dataSO.maxAPByLevel);
		CreateBranch(PopeTranform, TechTree.MovementCD, cdLevel, index++);
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
		CreateBranch(MissionaryTransform, TechTree.HP, hpLevel, index++);

		// AP
		int apLevel = GetActualLevelCount(dataSO.maxAPByLevel);
		CreateBranch(MissionaryTransform, TechTree.MovementCD, apLevel, index++);

		// Charm（传教士独有）
		int charmLevel = GetActualLevelCount(realData.convertMissionaryChanceByLevel);
		CreateBranch(MissionaryTransform, TechTree.Conversion, charmLevel, index++);

		// Occupy
		int occupyLevel = GetActualLevelCount(realData.occupyEmptySuccessRateByLevel);
		CreateBranch(MissionaryTransform,TechTree.Occupy, occupyLevel, index++);

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
		CreateBranch(FarmerTransform, TechTree.HP, hpLevel, index++);

		// AP
		int apLevel = GetActualLevelCount(dataSO.maxAPByLevel);
		CreateBranch(FarmerTransform, TechTree.MovementCD, apLevel, index++);

		// Sacrifice
		int ocLevel = GetActualLevelCount(realData.maxSacrificeLevel);
		CreateBranch(FarmerTransform, TechTree.Sacrifice, ocLevel, index++);
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
		CreateBranch(SoliderTransform, TechTree.HP, hpLevel, index++);

		// AP
		int apLevel = GetActualLevelCount(dataSO.maxAPByLevel);
		CreateBranch(SoliderTransform, TechTree.MovementCD, apLevel, index++);

		
		int atkLevel = GetActualLevelCount(dataSO.attackPowerByLevel);
		CreateBranch(SoliderTransform, TechTree.ATK, atkLevel, index++);
		
	}

	//============================================================
	// 工具函数：创建分支 + 自动水平布局
	//============================================================
	private void CreateBranch(RectTransform parent, TechTree type, int maxLevel, int index)
	{
		SkillBranch branch = Instantiate(prefab, parent);

		Sprite sprite = GetSprite(type);

		branch.Initialize(sprite,type, maxLevel, levelUpInfoPanel, levelUpbutton);

		RectTransform rt = branch.GetComponent<RectTransform>();
		rt.anchoredPosition = new Vector2(index * HorizontalSpacing, 0);
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
}
