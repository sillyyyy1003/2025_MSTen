using GameData;
using GameData.UI;
using GamePieces;
using SoundSystem;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillNode : MonoBehaviour
{
	//=========================================
	//メンバー変数
	//=========================================
	// 用于判定当前等级所需的变量
	PieceType pieceType;
	TechTree techTree;
	int skillIndex;
	public int SkillIndex => skillIndex;

	//=========================================
	//プロパティ
	//=========================================
	[SerializeField]
	private Button skillButton;
	[SerializeField]
	private Image mask;
	[SerializeField]
	private TMP_Text text;		// 用于显示简易技能说明
	[SerializeField]	
	private Image iconImage;    // 用于显示技能图标
    [SerializeField]
    private Image bgImage;    // 背景图标


    private RectTransform levelUpPanel;
	private LevelUpButton levelUpButton;



	private void Start()
	{
		skillButton.onClick.AddListener(OnSkillButtonClick);
	}

	public void Initialize(Sprite sprite, string skillDescription, int _skillIndex, PieceType _pieceType, TechTree _techTree, RectTransform _levelUpPanel, LevelUpButton _button)
	{
		iconImage.sprite = sprite;
		text.text = skillDescription;
	
		skillIndex = _skillIndex;
		pieceType = _pieceType;
		techTree = _techTree;

		levelUpPanel = _levelUpPanel;
		levelUpButton = _button;

		if (techTree == TechTree.Conversion && skillIndex == 0)
		{
			bgImage.sprite = UISpriteHelper.Instance.GetSkillTreeSprite(TechTree.None);
			iconImage.color = new Color(1f, 1f, 1f, 0f);
            text.color = new Color(1f, 1f, 1f, 0f);

        }

        if (SkillTreeUIManager.Instance.GetCurrentLevel(pieceType,techTree) >= _skillIndex)
		{
			UnlockSkillNode();
		}

	}

	public void UnlockSkillNode()
	{
		mask.gameObject.SetActive(false);
	}

	private void OnSkillButtonClick()
	{
		// 显示面板
		levelUpPanel.gameObject.SetActive(true);
        levelUpButton.gameObject.SetActive(true);
        levelUpButton.GetComponent<Button>().interactable = false;
		// 先判断是不是当前级别的
		int currentLevel = SkillTreeUIManager.Instance.GetCurrentLevel(pieceType, techTree);


        //20251207 Lu 更新Button的详细组件调用
        TMP_Text label = levelUpButton.transform.Find("LevelUp").GetComponent<TMP_Text>();
        TMP_Text costNum = levelUpButton.transform.Find("Count").GetComponent<TMP_Text>();
		GameObject costImage= levelUpButton.transform.Find("Image").gameObject;
        TMP_Text PieceName = levelUpPanel.transform.Find("LevelUpPiece/PieceName").GetComponent<TMP_Text>();
        TMP_Text LevelUpDescription = levelUpPanel.transform.Find("LevelUpPiece/LevelUpDescription").GetComponent<TMP_Text>();

        if (PieceName) PieceName.text = PlayerUnitDataInterface.Instance.GetPieceNameByPieceType(pieceType);

		if (LevelUpDescription) LevelUpDescription.text = SkillTreeUIManager.Instance.GetLevelUpInfo(pieceType);

        Debug.Log("[SkillNode]CurrentLevel" + currentLevel);
		Debug.Log("[SkillNode]Skill level:" + SkillIndex);
		// ------------------------------------------------
		// ⭐ ① index=0：初始等级，永远解锁，不需要升级
		// ------------------------------------------------
		if (skillIndex == 0)
		{
			if (label) { label.alignment = TMPro.TextAlignmentOptions.Center; label.text = "レベルアップ済"; label.color = Color.white; }
            if (costNum) costNum.text = $"";
            if (costImage) costImage.SetActive(false);
            return;
		}

		// ------------------------------------------------
		// ⭐ ② 已解锁等级：skillIndex < currentLevel
		// ------------------------------------------------
		if (skillIndex <= currentLevel)
		{
			if (label) { label.alignment = TMPro.TextAlignmentOptions.Center; label.text = "レベルアップ済"; label.color = Color.white; }
            if (costNum) costNum.text = $"";
            if (costImage) costImage.SetActive(false);
            return;
		}

		// ------------------------------------------------
		// ⭐ ③ “当前可升级等级”：skillIndex == currentLevel
		// ------------------------------------------------
		if (skillIndex == currentLevel + 1)
		{
			int cost = GetUpgradeCostByTechType(skillIndex - 1);
			if (label) { label.alignment = TMPro.TextAlignmentOptions.Left; label.text = $"レベルアップ"; label.color = Color.black; }
            if (costNum) costNum.text = $"{cost}";
            if (costImage) costImage.SetActive(true);

            int resource = PlayerDataManager.Instance.GetPlayerResource();
			if (resource >= cost)
			{
				levelUpButton.GetComponent<Button>().interactable = true;
				levelUpButton.SetButton(OnLevelUpButtonClicked);


            }
			else
			{
                label.color = Color.white;
                costNum.color = Color.white;
            }
            if (LevelUpDescription) LevelUpDescription.text = SkillTreeUIManager.Instance.GetLevelUpInfo(pieceType, techTree);

            return;
		}

		// ------------------------------------------------
		// ⭐ ④ 未来等级：skillIndex > currentLevel
		// ------------------------------------------------
		if (label) levelUpButton.gameObject.SetActive(false);
        if (costNum) costNum.text = $"";
        if (costImage) costImage.SetActive(false);
    }

	public void OnLevelUpButtonClicked()
	{
		SkillTreeUIManager.Instance.UpgradeCurrentLevel(pieceType,techTree);

		UnlockSkillNode();

		// Update 棋子数据和UIBar数据
		UpgradePieces(techTree, pieceType);

        // 消耗资源
        //25.12.16 ri change skillindex
        int cost = GetUpgradeCostByTechType(skillIndex-1);
		//Debug.Log("upgrade cost is "+cost);

        //int cost = GetUpgradeCostByTechType(skillIndex);
		int playerId = GameManage.Instance.LocalPlayerID;
		int res = PlayerDataManager.Instance.GetPlayerData(playerId).Resources;
		res -= cost;
		PlayerDataManager.Instance.SetPlayerResourses(res);
		GameUIManager.Instance.UpdateResourcesData();

        //25.12.10 RI 添加结局数据
        PlayerDataManager.Instance.Result_ResourceUsed += cost;

        // UpdateUI
        SkillTreeUIManager.Instance.UpdateSimpleSkillPanel(pieceType);
        GameUIManager.Instance.UpdateSimplePanelInfo();


		//刷新
		OnSkillButtonClick();
    }


	// 单位升级
	public bool UpgradePieces(TechTree tech, PieceType type)
	{
		Debug.Log("进行升级: 科技树: " + tech + " 单位种类: " + type);

		int playerID = GameManage.Instance.LocalPlayerID;
		List<PlayerUnitData> list = PlayerDataManager.Instance.GetPlayerData(playerID).PlayerUnits;
		List<int> ID = new List<int>();
		for (int i = 0; i < list.Count; i++)
		{
			ID.Add(list[i].UnitID);
		}

		// 只需要播放一遍
		SoundManager.Instance.PlaySE(TYPE_SE.UPGRADE);

		// --- 执行 Upgrade ---
		switch (tech)
		{
			case TechTree.HP:
				if (type != PieceType.Building)
				{
					return PieceManager.Instance.UpgradePiece(type, PieceUpgradeType.HP);
				}
				else
				{
					return GameManage.Instance._BuildingManager.UpgradeBuilding(BuildingUpgradeType.BuildingHP);
				}
			case TechTree.AP:
				return PieceManager.Instance.UpgradePiece(type, PieceUpgradeType.AP);
			case TechTree.Occupy:
				return PieceManager.Instance.UpgradePieceSpecial(type, SpecialUpgradeType.MissionaryOccupy);
				
			case TechTree.Conversion:
				return PieceManager.Instance.UpgradePieceSpecial(type, SpecialUpgradeType.MissionaryConvertEnemy);
			case TechTree.ATK:
				return PieceManager.Instance.UpgradePieceSpecial(type, SpecialUpgradeType.MilitaryAttackPower);

			case TechTree.Sacrifice:
				return PieceManager.Instance.UpgradePieceSpecial(type, SpecialUpgradeType.FarmerSacrifice);

			case TechTree.MovementCD:
				return PieceManager.Instance.UpgradePieceSpecial(type, SpecialUpgradeType.PopeSwapCooldown);

			// --- Building Upgrade ---
			case TechTree.AttackPosition:
				return GameManage.Instance._BuildingManager.UpgradeBuilding(BuildingUpgradeType.attackRange);

			case TechTree.AltarCount:
				return GameManage.Instance._BuildingManager.UpgradeBuilding( BuildingUpgradeType.slotsLevel);
		
			default:
				Debug.LogError("Unsupported TechTree: " + tech);
				return false;
		}

	}

	private int GetUpgradeCostByTechType(int level)
	{
		UnitListTable.PieceDetail pd =
		new UnitListTable.PieceDetail(pieceType, SceneStateManager.Instance.PlayerReligion);

		var so = ScriptableObject.CreateInstance<PieceDataSO>();

        if (pieceType != PieceType.Building) so = UnitListTable.Instance.GetPieceDataSO(pieceType, pd);

        switch (pieceType)
		{ 
			case PieceType.Pope:
				{
					var popeData = so as PopeDataSO;
				
					if (techTree == TechTree.HP)
						return popeData.hpUpgradeCost[level];

					if (techTree == TechTree.MovementCD)
						return popeData.swapCooldownUpgradeCost[level];

					return 0;
				}
			case PieceType.Missionary:

				{
					var MissionaryData = so as MissionaryDataSO;
				
					if (techTree == TechTree.HP)
						return MissionaryData.hpUpgradeCost[level];

					if (techTree == TechTree.AP)
						return  MissionaryData.apUpgradeCost[level];

					if(techTree ==TechTree.Occupy)
						return MissionaryData.occupyUpgradeCost[level];

					if(techTree ==TechTree.Conversion)
						return MissionaryData.convertEnemyUpgradeCost[level];

					return 0;
				}
			case PieceType.Farmer:
				{
					var FarmerData= so as FarmerDataSO;
				
					if(techTree == TechTree.HP)
						return FarmerData.hpUpgradeCost[level];
					if(techTree==TechTree.Occupy)
						return FarmerData.apUpgradeCost[level];
					if(techTree==TechTree.Sacrifice)
						return FarmerData.sacrificeUpgradeCost[level];
					return 0;
				}
			case PieceType.Military:
				{
					var MilitaryData = so as MilitaryDataSO;
				
					if(techTree == TechTree.HP)
						return MilitaryData.hpUpgradeCost[level];
					if (techTree == TechTree.AP)
						return MilitaryData.apUpgradeCost[level];
					if(techTree==TechTree.ATK)
						return MilitaryData.attackPowerUpgradeCost[level];
					return 0;
				}

			case PieceType.Building:
				{
					BuildingDataSO buildingData = GameManage.Instance._BuildingManager.GetBuildingDataByReligion(SceneStateManager.Instance.PlayerReligion);
				
					if (techTree== TechTree.HP)
						return buildingData.hpUpgradeCost[level];
					if (techTree == TechTree.AltarCount)
						return buildingData.slotsUpgradeCost[level];
					return 0;
				}
			default:	return 0;
		}
		
	}




}
