using GameData;
using GameData.UI;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class SimpleSkillButton : MonoBehaviour
{
	[Header("UI")]
	public TMP_Text skillNameText;
	public TMP_Text levelText;
	//public TMP_Text costText;
	public Button upgradeButton;

	private PieceType pieceType;
	private TechTree tech;
	private SimpleSkillPanel panel;

	public void Initialize(PieceType type, TechTree techTree, SimpleSkillPanel panel)
	{
		this.pieceType = type;
		this.tech = techTree;
		this.panel = panel;

		//20251207 Lu改变字符取用
		skillNameText.text = PlayerUnitDataInterface.Instance.GetTechNameByTechTree(tech);

		upgradeButton.onClick.AddListener(OnClickUpgrade);

		Refresh();
	}

	/// <summary>
	/// 刷新 UI 显示
	/// </summary>
	public void Refresh()
	{
		int currentLv = SkillTreeUIManager.Instance.GetCurrentLevel(pieceType, tech);

		// 获取最大等级
		int maxLv = GetMaxLevel(pieceType, tech);

		if (currentLv >= maxLv)
		{
			levelText.text = $"MAX";
			levelText.color = Color.white;
            skillNameText.color = Color.white;
            upgradeButton.interactable = false;
			return;
		}

		// 显示下一级等级
		int nextLevel = currentLv + 1;
		levelText.text = $"Lv.{nextLevel}";

		// 获取升级消耗
		int cost = GetUpgradeCost(pieceType, tech, currentLv);

		// 判断资源够不够
		int playerID = GameManage.Instance.LocalPlayerID;
		int resource = PlayerDataManager.Instance.GetPlayerData(playerID).Resources;

		upgradeButton.interactable = resource >= cost;
	}

	private void OnClickUpgrade()
	{
		int currentLv = SkillTreeUIManager.Instance.GetCurrentLevel(pieceType, tech);
		int cost = GetUpgradeCost(pieceType, tech, currentLv);

		// 扣资源
		int playerID = GameManage.Instance.LocalPlayerID;
		int res = PlayerDataManager.Instance.GetPlayerData(playerID).Resources;
		res -= cost;
		PlayerDataManager.Instance.SetPlayerResourses(res);

		// 提升等级
		SkillTreeUIManager.Instance.UpgradeCurrentLevel(pieceType, tech);

		// 升级单位 & 建筑等
		UpgradeRelatedUnits();

		// 刷新所有按钮
		panel.Refresh();

		// 刷新资源 UI
		GameUIManager.Instance.UpdateResourcesData();
	}

	//==============================================================
	// 工具函数：调用你现有的升级函数
	//==============================================================

	private void UpgradeRelatedUnits()
	{
		SkillNode dummy = new SkillNode();
		dummy.UpgradePieces(tech, pieceType);
	}

	private int GetMaxLevel(PieceType type, TechTree techTree)
	{
		if (type != PieceType.Building)
		{

			var so = UnitListTable.Instance.GetPieceDataSO(type,
				new UnitListTable.PieceDetail(type, SceneStateManager.Instance.PlayerReligion));

			switch (techTree)
			{
				case TechTree.HP: return GetActualCount(so.maxHPByLevel);
				case TechTree.AP: return GetActualCount(so.maxAPByLevel);

				case TechTree.Conversion:
					return GetActualCount((so as MissionaryDataSO).convertMissionaryChanceByLevel);

				case TechTree.Occupy:
					if (type == PieceType.Missionary)
						return GetActualCount((so as MissionaryDataSO).occupyEmptySuccessRateByLevel);
					if (type == PieceType.Farmer)
						return GetActualCount((so as FarmerDataSO).apUpgradeCost);
					break;

				case TechTree.ATK:
					return GetActualCount((so as MilitaryDataSO).attackPowerByLevel);

				case TechTree.Sacrifice:
					return GetActualCount((so as FarmerDataSO).maxSacrificeLevel);

				case TechTree.MovementCD:
					return GetActualCount((so as PopeDataSO).maxAPByLevel);

				case TechTree.AltarCount:
					return GetActualCount(GameManage.Instance._BuildingManager
						.GetBuildingDataByReligion(SceneStateManager.Instance.PlayerReligion)
						.slotsUpgradeCost);
			}

			return 1;
		}
		else
		{
			var buildingData =
				GameManage.Instance._BuildingManager.GetBuildingDataByReligion(SceneStateManager.Instance.PlayerReligion);
			switch (techTree)
			{
				case TechTree.HP:
					return GetActualCount(buildingData.maxHpByLevel);
				case TechTree.AltarCount:
					return GetActualCount(buildingData.maxSlotsByLevel);
			}

			return 1;
		}
	}

	private int GetUpgradeCost(PieceType type, TechTree techTree, int level)
	{
		return SkillTreeCostHelper.GetUpgradeCost(type, techTree, level);
	}

	private int GetActualCount<T>(T[] arr)
	{
		if (arr == null || arr.Length == 0) return 1;

		int count = 1;
		var cmp = EqualityComparer<T>.Default;
		for (int i = 1; i < arr.Length; i++)
		{
			if (cmp.Equals(arr[i], arr[i - 1])) break;
			count++;
		}
		return count;
	}
}


public static class SkillTreeCostHelper
{
	public static int GetUpgradeCost(PieceType pieceType, TechTree techTree, int level)
	{
		if (pieceType != PieceType.Building)
		{
			UnitListTable.PieceDetail detail =
				new UnitListTable.PieceDetail(pieceType, SceneStateManager.Instance.PlayerReligion);

			var so = UnitListTable.Instance.GetPieceDataSO(pieceType, detail);

			switch (pieceType)
			{
				case PieceType.Pope:
					var pope = so as PopeDataSO;
					if (techTree == TechTree.HP) return pope.hpUpgradeCost[level];
					if (techTree == TechTree.MovementCD) return pope.swapCooldownUpgradeCost[level];
					return 0;

				case PieceType.Missionary:
					var m = so as MissionaryDataSO;
					if (techTree == TechTree.HP) return m.hpUpgradeCost[level];
					if (techTree == TechTree.AP) return m.apUpgradeCost[level];
					if (techTree == TechTree.Occupy) return m.occupyUpgradeCost[level];
					if (techTree == TechTree.Conversion) return m.convertEnemyUpgradeCost[level];
					return 0;

				case PieceType.Farmer:
					var f = so as FarmerDataSO;
					if (techTree == TechTree.HP) return f.hpUpgradeCost[level];
					if (techTree == TechTree.AP) return f.apUpgradeCost[level];
					if (techTree == TechTree.Sacrifice) return f.sacrificeUpgradeCost[level];
					return 0;

				case PieceType.Military:
					var s = so as MilitaryDataSO;
					if (techTree == TechTree.HP) return s.hpUpgradeCost[level];
					if (techTree == TechTree.AP) return s.apUpgradeCost[level];
					if (techTree == TechTree.ATK) return s.attackPowerUpgradeCost[level];
					return 0;
			}

			return 0;
		}
		else
		{
		
			var b = GameManage.Instance._BuildingManager.GetBuildingDataByReligion(
				SceneStateManager.Instance.PlayerReligion);

			if (techTree == TechTree.HP) return b.hpUpgradeCost[level];
			if (techTree == TechTree.AltarCount) return b.slotsUpgradeCost[level];
			return 0;

		}
	}
	
}
